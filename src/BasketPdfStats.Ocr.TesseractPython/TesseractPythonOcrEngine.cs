using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Core.Validation;

namespace BasketPdfStats.Ocr.TesseractPython;

public sealed class TesseractPythonOcrEngine : IOcrEngine
{
    private readonly TesseractPythonOptions _options;
    private readonly PythonProcessRunner _runner;
    private readonly PythonJsonReader _jsonReader;
    private readonly PythonOutputMapper _mapper;
    private readonly IStatsValidator _validator;

    public TesseractPythonOcrEngine(TesseractPythonOptions options)
        : this(options, new PythonProcessRunner(), new PythonJsonReader(), new PythonOutputMapper(), new StatsValidationService())
    {
    }

    public TesseractPythonOcrEngine(TesseractPythonOptions options, PythonProcessRunner runner, PythonJsonReader jsonReader, PythonOutputMapper mapper, IStatsValidator? validator = null)
    {
        _options = options;
        _runner = runner;
        _jsonReader = jsonReader;
        _mapper = mapper;
        _validator = validator ?? new StatsValidationService();
    }

    public string EngineName => OcrStrategyNames.TesseractFullPage;

    public async Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        if (!_options.Enabled)
        {
            return EngineOnlyResult(OcrRunStatus.NotConfigured, stopwatch.ElapsedMilliseconds, "TesseractFullPage is disabled in configuration.");
        }

        var projectResolution = ResolvePythonProjectDirectory();
        if (!projectResolution.IsConfigured)
        {
            return EngineOnlyResult(OcrRunStatus.NotConfigured, stopwatch.ElapsedMilliseconds, projectResolution.Error);
        }

        var pythonCandidates = ResolvePythonCandidates(projectResolution.ProjectDirectory!);
        if (pythonCandidates.Count == 0)
        {
            return EngineOnlyResult(OcrRunStatus.NotConfigured, stopwatch.ElapsedMilliseconds, "No Python executable candidates found.");
        }

        var inputPdf = !string.IsNullOrWhiteSpace(request.WorkingPath) ? request.WorkingPath : request.PdfPath;
        if (!File.Exists(inputPdf))
        {
            return EngineOnlyResult(OcrRunStatus.Failed, stopwatch.ElapsedMilliseconds, $"Input PDF not found: {inputPdf}");
        }

        var rawOutputPath = TesseractFullPageArtifactPaths.RawOutputPath(_options, request);
        var outputDirectory = Path.GetDirectoryName(rawOutputPath)!;
        Directory.CreateDirectory(outputDirectory);
        if (!string.IsNullOrWhiteSpace(_options.DebugOutputFolder))
        {
            Directory.CreateDirectory(_options.ResolvePath(_options.DebugOutputFolder));
        }

        // Roster dinamico dal DB: scritto su file e passato a CH1 come supporto al parsing.
        // Se assente, CH1 procede senza roster (nessun fallback hardcoded dopo Fase 9):
        // le identità si risolvono comunque in C# (PlayerIdentityMatcher).
        var rosterJsonPath = TryWriteRosterContext(request, outputDirectory);

        PythonProcessResult? processResult = null;
        string? attemptedExecutables = null;
        foreach (var candidate in pythonCandidates)
        {
            attemptedExecutables = string.IsNullOrWhiteSpace(attemptedExecutables) ? candidate : attemptedExecutables + "; " + candidate;
            var arguments = BuildArguments(inputPdf, rawOutputPath, request, rosterJsonPath);
            processResult = await _runner.RunAsync(
                candidate,
                arguments,
                projectResolution.ProjectDirectory!,
                BuildEnvironment(projectResolution.ProjectDirectory!),
                TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)),
                cancellationToken);

            if (processResult.Started)
            {
                break;
            }
        }

        if (processResult is null || !processResult.Started)
        {
            return EngineOnlyResult(OcrRunStatus.NotConfigured, stopwatch.ElapsedMilliseconds, processResult?.ErrorMessage ?? $"Python not found. Tried: {attemptedExecutables}");
        }

        if (processResult.TimedOut)
        {
            return EngineOnlyResult(OcrRunStatus.Timeout, stopwatch.ElapsedMilliseconds, CombineProcessError(processResult));
        }

        if (processResult.ExitCode != 0)
        {
            return EngineOnlyResult(OcrRunStatus.Failed, stopwatch.ElapsedMilliseconds, CombineProcessError(processResult));
        }

        if (!File.Exists(rawOutputPath))
        {
            return EngineOnlyResult(OcrRunStatus.Failed, stopwatch.ElapsedMilliseconds, $"Python worker completed but did not create JSON output. Stdout: {processResult.Stdout} Stderr: {processResult.Stderr}");
        }

        try
        {
            using var json = await _jsonReader.ReadAsync(rawOutputPath, cancellationToken);
            var mapped = _mapper.Map(json, request);
            _validator.Validate(mapped);
            stopwatch.Stop();
            mapped.OcrRuns.Insert(0, new OcrRunResult
            {
                Engine = EngineName,
                Status = OcrRunStatus.Success,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Error = string.IsNullOrWhiteSpace(processResult.Stderr) ? null : processResult.Stderr.Trim()
            });
            return mapped;
        }
        catch (Exception ex)
        {
            return EngineOnlyResult(OcrRunStatus.Failed, stopwatch.ElapsedMilliseconds, $"Python JSON output missing or not parsable: {ex.Message}");
        }
    }

    private ProjectResolution ResolvePythonProjectDirectory()
    {
        var configuredPath = _options.ResolvePath(_options.PythonProjectPath);
        var candidates = new[]
        {
            configuredPath,
            Path.Combine(configuredPath, "fiba_pdf_to_json"),
            Directory.GetParent(configuredPath)?.FullName ?? configuredPath
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate) || !Directory.Exists(candidate))
            {
                continue;
            }

            if (WorkerExists(candidate))
            {
                return ProjectResolution.Configured(candidate);
            }

            var child = Path.Combine(candidate, "fiba_pdf_to_json");
            if (Directory.Exists(child) && WorkerExists(child))
            {
                return ProjectResolution.Configured(child);
            }
        }

        return ProjectResolution.NotConfigured($"Python project or worker module not found from '{_options.PythonProjectPath}'. Expected module '{_options.WorkerModule}'.");
    }

    private bool WorkerExists(string projectDirectory)
    {
        var workerPath = Path.Combine(projectDirectory, _options.WorkerModule.Replace('.', Path.DirectorySeparatorChar) + ".py");
        return File.Exists(workerPath);
    }

    private List<string> ResolvePythonCandidates(string projectDirectory)
    {
        if (!string.IsNullOrWhiteSpace(_options.PythonExecutablePath))
        {
            if (IsCommandName(_options.PythonExecutablePath))
            {
                return [_options.PythonExecutablePath];
            }

            var explicitPath = _options.ResolvePath(_options.PythonExecutablePath);
            return File.Exists(explicitPath) ? [explicitPath] : [];
        }

        var candidates = new List<string>();
        var venvPython = Path.Combine(projectDirectory, ".venv", "Scripts", "python.exe");
        if (File.Exists(venvPython))
        {
            candidates.Add(venvPython);
        }

        candidates.Add("python");
        candidates.Add("py");
        return candidates;
    }

    private static bool IsCommandName(string value)
    {
        return !Path.IsPathRooted(value) &&
               !value.Contains(Path.DirectorySeparatorChar) &&
               !value.Contains(Path.AltDirectorySeparatorChar);
    }

    public string BuildArguments(string inputPdf, string outputJson, OcrProcessingRequest request, string? rosterJsonPath = null)
    {
        var args = new StringBuilder();
        args.Append("-m ");
        args.Append(_options.WorkerModule);
        args.Append(' ');
        args.Append(Quote(inputPdf));
        args.Append(' ');
        args.Append(Quote(outputJson));
        args.Append(" --fallback-scale ");
        args.Append(_options.FallbackScale);
        if (!_options.UseNativeImages)
        {
            args.Append(" --no-native-images");
        }

        if (!string.IsNullOrWhiteSpace(rosterJsonPath))
        {
            args.Append(" --roster-json ");
            args.Append(Quote(rosterJsonPath));
        }

        if (!string.IsNullOrWhiteSpace(_options.DebugOutputFolder))
        {
            args.Append(" --debug-dir ");
            args.Append(Quote(_options.ResolvePath(_options.DebugOutputFolder)));
        }

        if (!string.IsNullOrWhiteSpace(_options.LayoutDebugOutputFolder))
        {
            args.Append(" --layout-debug-dir ");
            args.Append(Quote(TesseractFullPageGeometryArtifactPaths.DebugFolder(_options, request)));
            args.Append(" --document-hash ");
            args.Append(Quote(request.DocumentHash));
        }

        if (_options.Verbose)
        {
            args.Append(" --verbose");
        }

        return args.ToString();
    }

    private static readonly JsonSerializerOptions RosterJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializza il contesto partita (roster) su file per passarlo al worker via --roster-json.
    /// Errori di scrittura non bloccano l'OCR: il worker userà il fallback legacy.
    /// </summary>
    private static string? TryWriteRosterContext(OcrProcessingRequest request, string outputDirectory)
    {
        if (request.MatchContext is null)
        {
            return null;
        }

        try
        {
            var path = Path.Combine(outputDirectory, "roster-context.json");
            var json = JsonSerializer.Serialize(request.MatchContext, RosterJsonOptions);
            File.WriteAllText(path, json);
            return path;
        }
        catch
        {
            return null;
        }
    }

    private IReadOnlyDictionary<string, string?> BuildEnvironment(string projectDirectory)
    {
        var environment = new Dictionary<string, string?>();
        var existing = Environment.GetEnvironmentVariable("PYTHONPATH");
        var pythonPath = string.IsNullOrWhiteSpace(existing)
            ? projectDirectory
            : projectDirectory + Path.PathSeparator + existing;
        environment["PYTHONPATH"] = pythonPath;

        AddTesseractEnvironment(environment);
        return environment;
    }

    private void AddTesseractEnvironment(Dictionary<string, string?> environment)
    {
        var tesseractFolder = _options.ResolvePath(_options.TesseractExecutableFolder);
        if (Directory.Exists(tesseractFolder))
        {
            var existingPath = Environment.GetEnvironmentVariable("PATH");
            environment["PATH"] = string.IsNullOrWhiteSpace(existingPath)
                ? tesseractFolder
                : tesseractFolder + Path.PathSeparator + existingPath;
        }

        var tessdata = _options.ResolvePath(_options.TessdataPrefix);
        if (Directory.Exists(tessdata))
        {
            environment["TESSDATA_PREFIX"] = tessdata;
        }
    }

    private static ProcessingResult EngineOnlyResult(OcrRunStatus status, long durationMs, string? error)
    {
        return new ProcessingResult
        {
            OcrRuns =
            [
                new OcrRunResult
                {
                    Engine = OcrStrategyNames.TesseractFullPage,
                    Status = status,
                    DurationMs = durationMs,
                    Error = error
                }
            ],
            Validation = new ValidationResult
            {
                Status = FileProcessingStatus.CompletedNotValidated,
                Warnings =
                [
                    new ValidationWarning
                    {
                        RuleId = $"ocr.tesseractFullPage.{status}",
                        Severity = status == OcrRunStatus.NotConfigured ? "Info" : "Warning",
                        Message = error ?? status.ToString()
                    }
                ]
            }
        };
    }

    private static string CombineProcessError(PythonProcessResult result)
    {
        var parts = new[] { result.ErrorMessage, result.Stderr, result.Stdout }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim());
        return string.Join(Environment.NewLine, parts);
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private sealed record ProjectResolution(bool IsConfigured, string? ProjectDirectory, string? Error)
    {
        public static ProjectResolution Configured(string projectDirectory) => new(true, projectDirectory, null);
        public static ProjectResolution NotConfigured(string error) => new(false, null, error);
    }
}
