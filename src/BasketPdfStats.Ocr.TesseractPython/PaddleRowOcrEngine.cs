using System.Diagnostics;
using System.Text;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Core.Validation;
using BasketPdfStats.Infrastructure.Evidence;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Ocr.TesseractPython;

/// <summary>
/// Channel 4 (ocr.paddle.row): runs PaddleOCR (in .venv-paddle) on the player-table
/// crops and parses each player row into per-player evidence. Reuses
/// <see cref="PaddleCropOptions"/> (same venv) with the row worker module/paths.
/// </summary>
public sealed class PaddleRowOcrEngine : IOcrEngine
{
    private readonly PaddleCropOptions _options;
    private readonly PythonProcessRunner _runner;
    private readonly EvidenceRecordReader _evidenceReader;
    private readonly EvidenceRecordMapper _evidenceMapper;
    private readonly IStatsValidator _validator;

    public PaddleRowOcrEngine(PaddleCropOptions options)
        : this(options, new PythonProcessRunner(), new StatsValidationService())
    {
    }

    public PaddleRowOcrEngine(PaddleCropOptions options, PythonProcessRunner runner, IStatsValidator? validator = null)
    {
        _options = options;
        _runner = runner;
        _validator = validator ?? new StatsValidationService();
        _evidenceReader = new EvidenceRecordReader();
        _evidenceMapper = new EvidenceRecordMapper();
    }

    public string EngineName => OcrStrategyNames.PaddleTableRows;

    public async Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        if (!_options.Enabled)
        {
            return EngineOnlyResult(OcrRunStatus.NotConfigured, stopwatch.ElapsedMilliseconds, "PaddleTableRows is disabled in configuration.");
        }

        var preparation = request.DocumentPreparation;
        if (preparation is null || !preparation.LayoutCalibrated || !preparation.LayoutCropsAvailable || string.IsNullOrWhiteSpace(preparation.MetadataPath))
        {
            return EngineOnlyResult(OcrRunStatus.Failed, stopwatch.ElapsedMilliseconds, "Calibrated generic layout crops are unavailable.");
        }

        var projectDirectory = ResolveProjectDirectory();
        if (projectDirectory is null)
        {
            return EngineOnlyResult(OcrRunStatus.NotConfigured, stopwatch.ElapsedMilliseconds, $"Paddle row worker not found from '{_options.PythonProjectPath}'.");
        }

        var python = ResolvePython();
        if (python is null)
        {
            return EngineOnlyResult(OcrRunStatus.NotConfigured, stopwatch.ElapsedMilliseconds, $"PaddleOCR Python executable not found: {_options.PythonExecutablePath}");
        }

        var rawPath = _options.RowRawOutputPath(request);
        var evidencePath = _options.RowEvidenceOutputPath(request);
        Directory.CreateDirectory(Path.GetDirectoryName(rawPath)!);

        var processResult = await _runner.RunAsync(
            python,
            BuildArguments(preparation.MetadataPath, rawPath, evidencePath),
            projectDirectory,
            BuildEnvironment(projectDirectory),
            TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)),
            cancellationToken);

        if (!processResult.Started)
        {
            return EngineOnlyResult(OcrRunStatus.NotConfigured, stopwatch.ElapsedMilliseconds, processResult.ErrorMessage ?? "PaddleOCR process did not start.");
        }

        if (processResult.TimedOut)
        {
            return EngineOnlyResult(OcrRunStatus.Timeout, stopwatch.ElapsedMilliseconds, CombineProcessError(processResult));
        }

        if (processResult.ExitCode != 0 || !File.Exists(evidencePath))
        {
            return EngineOnlyResult(OcrRunStatus.Failed, stopwatch.ElapsedMilliseconds, CombineProcessError(processResult));
        }

        try
        {
            var envelope = await _evidenceReader.ReadAsync(evidencePath, cancellationToken);
            var mapped = _evidenceMapper.Map(envelope);
            _validator.Validate(mapped);
            stopwatch.Stop();
            var run = mapped.OcrRuns.FirstOrDefault(r => r.Status == OcrRunStatus.Success);
            if (run is not null)
            {
                run.DurationMs = stopwatch.ElapsedMilliseconds;
                run.Error = string.IsNullOrWhiteSpace(processResult.Stderr) ? null : processResult.Stderr.Trim();
            }

            return mapped;
        }
        catch (Exception ex)
        {
            return EngineOnlyResult(OcrRunStatus.Failed, stopwatch.ElapsedMilliseconds, $"Paddle row evidence output missing or not parsable: {ex.Message}");
        }
    }

    public string BuildArguments(string metadataPath, string rawOutputPath, string evidenceOutputPath)
    {
        var args = new StringBuilder($"-m {_options.RowWorkerModule}");
        args.Append($" --metadata {Quote(metadataPath)} --output {Quote(rawOutputPath)}");
        args.Append($" --evidence-output {Quote(evidenceOutputPath)}");
        args.Append($" --lang {_options.Lang}");
        if (!string.IsNullOrWhiteSpace(_options.ModelDir)) args.Append($" --paddle-model-dir {Quote(_options.ResolvePath(_options.ModelDir))}");
        return args.ToString();
    }

    private string? ResolveProjectDirectory()
    {
        var configured = _options.ResolvePath(_options.PythonProjectPath);
        var workerRelative = _options.RowWorkerModule.Replace('.', Path.DirectorySeparatorChar) + ".py";
        return new[] { configured, Path.Combine(configured, "fiba_pdf_to_json") }
            .FirstOrDefault(path => File.Exists(Path.Combine(path, workerRelative)));
    }

    private string? ResolvePython()
    {
        if (string.IsNullOrWhiteSpace(_options.PythonExecutablePath))
        {
            return null;
        }

        var configured = IsCommandName(_options.PythonExecutablePath)
            ? _options.PythonExecutablePath
            : _options.ResolvePath(_options.PythonExecutablePath);
        return IsCommandName(configured) || File.Exists(configured) ? configured : null;
    }

    private static IReadOnlyDictionary<string, string?> BuildEnvironment(string projectDirectory)
    {
        var existing = Environment.GetEnvironmentVariable("PYTHONPATH");
        var pythonPath = string.IsNullOrWhiteSpace(existing) ? projectDirectory : projectDirectory + Path.PathSeparator + existing;
        return new Dictionary<string, string?> { ["PYTHONPATH"] = pythonPath };
    }

    private static ProcessingResult EngineOnlyResult(OcrRunStatus status, long durationMs, string? error)
    {
        return new ProcessingResult
        {
            OcrRuns = [new OcrRunResult { Engine = OcrStrategyNames.PaddleTableRows, Status = status, DurationMs = durationMs, Error = error }],
            Validation = new ValidationResult
            {
                Status = FileProcessingStatus.CompletedNotValidated,
                Warnings =
                [
                    new ValidationWarning
                    {
                        RuleId = $"ocr.paddleTableRows.{status}",
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
