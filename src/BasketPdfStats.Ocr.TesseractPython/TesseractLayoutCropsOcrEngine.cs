using System.Diagnostics;
using System.Text;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Core.Validation;
using BasketPdfStats.Infrastructure.Evidence;
using BasketPdfStats.Infrastructure.Preparation;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Ocr.TesseractPython;

public sealed class TesseractLayoutCropsOcrEngine : IOcrEngine
{
    private readonly TesseractPythonOptions _options;
    private readonly PythonProcessRunner _runner;
    private readonly PythonJsonReader _jsonReader;
    private readonly TesseractLayoutCropsOutputMapper _mapper;
    private readonly IStatsValidator _validator;
    private readonly EvidenceRecordReader _evidenceReader;
    private readonly EvidenceRecordMapper _evidenceMapper;

    public TesseractLayoutCropsOcrEngine(TesseractPythonOptions options)
        : this(options, new PythonProcessRunner(), new PythonJsonReader(), new TesseractLayoutCropsOutputMapper(), new StatsValidationService())
    {
    }

    public TesseractLayoutCropsOcrEngine(
        TesseractPythonOptions options,
        PythonProcessRunner runner,
        PythonJsonReader jsonReader,
        TesseractLayoutCropsOutputMapper mapper,
        IStatsValidator? validator = null)
    {
        _options = options;
        _runner = runner;
        _jsonReader = jsonReader;
        _mapper = mapper;
        _validator = validator ?? new StatsValidationService();
        _evidenceReader = new EvidenceRecordReader();
        _evidenceMapper = new EvidenceRecordMapper();
    }

    public string EngineName => OcrStrategyNames.TesseractLayoutCrops;

    public async Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        if (!_options.Enabled || !_options.UseLayoutCrops || !string.Equals(_options.Mode, "Crops", StringComparison.OrdinalIgnoreCase))
        {
            return EngineOnlyResult(OcrRunStatus.NotConfigured, stopwatch.ElapsedMilliseconds, "TesseractLayoutCrops is disabled in configuration.");
        }

        var preparation = request.DocumentPreparation;
        if (preparation is null || !preparation.LayoutCalibrated || !preparation.LayoutCropsAvailable || string.IsNullOrWhiteSpace(preparation.MetadataPath))
        {
            return EngineOnlyResult(OcrRunStatus.Failed, stopwatch.ElapsedMilliseconds, "Calibrated generic layout crops are unavailable.");
        }

        var projectDirectory = ResolveProjectDirectory();
        if (projectDirectory is null)
        {
            return EngineOnlyResult(OcrRunStatus.NotConfigured, stopwatch.ElapsedMilliseconds, $"Tesseract Python project not found from '{_options.PythonProjectPath}'.");
        }

        var pythonCandidates = ResolvePythonCandidates(projectDirectory);
        if (pythonCandidates.Count == 0)
        {
            return EngineOnlyResult(OcrRunStatus.NotConfigured, stopwatch.ElapsedMilliseconds, "No Python executable candidates found.");
        }

        var rawPath = TesseractLayoutCropsArtifactPaths.RawOutputPath(_options, request);
        var evidencePath = TesseractLayoutCropsArtifactPaths.EvidenceOutputPath(_options, request);
        Directory.CreateDirectory(Path.GetDirectoryName(rawPath)!);
        var preprocessedFolder = Path.Combine(
            _options.ResolvePath(_options.PreprocessedCropOutputFolder),
            LayoutCropPaths.DocumentFolderName(request));

        PythonProcessResult? processResult = null;
        foreach (var python in pythonCandidates)
        {
            processResult = await _runner.RunAsync(
                python,
                BuildArguments(preparation.MetadataPath, rawPath, preprocessedFolder, evidencePath),
                projectDirectory,
                BuildEnvironment(projectDirectory),
                TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)),
                cancellationToken);
            if (processResult.Started)
            {
                break;
            }
        }

        if (processResult is null || !processResult.Started)
        {
            return EngineOnlyResult(OcrRunStatus.NotConfigured, stopwatch.ElapsedMilliseconds, processResult?.ErrorMessage ?? "Python executable not found.");
        }

        if (processResult.TimedOut)
        {
            return EngineOnlyResult(OcrRunStatus.Timeout, stopwatch.ElapsedMilliseconds, CombineProcessError(processResult));
        }

        if (processResult.ExitCode != 0)
        {
            return EngineOnlyResult(OcrRunStatus.Failed, stopwatch.ElapsedMilliseconds, CombineProcessError(processResult));
        }

        if (!File.Exists(rawPath))
        {
            return EngineOnlyResult(OcrRunStatus.Failed, stopwatch.ElapsedMilliseconds, $"Crop worker completed but did not create JSON output: {rawPath}");
        }

        try
        {
            var stderr = string.IsNullOrWhiteSpace(processResult.Stderr) ? null : processResult.Stderr.Trim();
            // Prefer the shared evidence schema (channel-tagged provenance). Fall
            // back to the legacy partial mapper if no evidence file was produced.
            if (File.Exists(evidencePath))
            {
                var envelope = await _evidenceReader.ReadAsync(evidencePath, cancellationToken);
                var mappedEvidence = _evidenceMapper.Map(envelope);
                _validator.Validate(mappedEvidence);
                stopwatch.Stop();
                StampDuration(mappedEvidence, stopwatch.ElapsedMilliseconds, stderr);
                return mappedEvidence;
            }

            using var json = await _jsonReader.ReadAsync(rawPath, cancellationToken);
            var mapped = _mapper.Map(json, request);
            _validator.Validate(mapped);
            stopwatch.Stop();
            mapped.OcrRuns.Insert(0, new OcrRunResult
            {
                Engine = EngineName,
                Status = OcrRunStatus.Success,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Error = stderr
            });
            return mapped;
        }
        catch (Exception ex)
        {
            return EngineOnlyResult(OcrRunStatus.Failed, stopwatch.ElapsedMilliseconds, $"Crop OCR JSON output missing or not parsable: {ex.Message}");
        }
    }

    private static void StampDuration(ProcessingResult result, long durationMs, string? stderr)
    {
        var run = result.OcrRuns.FirstOrDefault(r => r.Status == OcrRunStatus.Success);
        if (run is null)
        {
            return;
        }

        run.DurationMs = durationMs;
        run.Error = stderr;
    }

    public string BuildArguments(string metadataPath, string rawOutputPath, string preprocessedFolder, string evidenceOutputPath = "")
    {
        var args = new StringBuilder($"-m {_options.CropWorkerModule}");
        args.Append($" --metadata {Quote(metadataPath)} --output {Quote(rawOutputPath)}");
        args.Append($" --preprocessed-output-folder {Quote(preprocessedFolder)}");
        args.Append($" --scale {Math.Max(1, _options.PreprocessScale)} --threshold {_options.PreprocessThreshold}");
        if (!string.IsNullOrWhiteSpace(evidenceOutputPath)) args.Append($" --evidence-output {Quote(evidenceOutputPath)}");
        if (_options.PreprocessImages) args.Append(" --preprocess");
        if (_options.SavePreprocessedImages) args.Append(" --save-preprocessed-images");
        if (_options.PreprocessSharpen) args.Append(" --sharpen");
        return args.ToString();
    }

    private string? ResolveProjectDirectory()
    {
        var configured = _options.ResolvePath(_options.PythonProjectPath);
        return new[] { configured, Path.Combine(configured, "fiba_pdf_to_json") }
            .FirstOrDefault(path => File.Exists(Path.Combine(path, _options.CropWorkerModule.Replace('.', Path.DirectorySeparatorChar) + ".py")));
    }

    private List<string> ResolvePythonCandidates(string projectDirectory)
    {
        if (!string.IsNullOrWhiteSpace(_options.PythonExecutablePath))
        {
            var configured = IsCommandName(_options.PythonExecutablePath)
                ? _options.PythonExecutablePath
                : _options.ResolvePath(_options.PythonExecutablePath);
            return IsCommandName(configured) || File.Exists(configured) ? [configured] : [];
        }

        var candidates = new List<string>();
        var venv = Path.Combine(projectDirectory, ".venv", "Scripts", "python.exe");
        if (File.Exists(venv)) candidates.Add(venv);
        candidates.Add("python");
        candidates.Add("py");
        return candidates;
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
            OcrRuns = [new OcrRunResult { Engine = OcrStrategyNames.TesseractLayoutCrops, Status = status, DurationMs = durationMs, Error = error }],
            Validation = new ValidationResult
            {
                Status = FileProcessingStatus.CompletedNotValidated,
                Warnings =
                [
                    new ValidationWarning
                    {
                        RuleId = $"ocr.tesseractLayoutCrops.{status}",
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
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());
        return string.Join(Environment.NewLine, parts);
    }

    private static bool IsCommandName(string value) =>
        !Path.IsPathRooted(value) && !value.Contains(Path.DirectorySeparatorChar) && !value.Contains(Path.AltDirectorySeparatorChar);

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
}
