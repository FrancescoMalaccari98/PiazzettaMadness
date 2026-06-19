using System.Text.Json;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Identity;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Core.Pipeline;
using BasketPdfStats.Infrastructure.Configuration;
using BasketPdfStats.Infrastructure.FileSystem;
using BasketPdfStats.Infrastructure.Preparation;
using BasketPdfStats.Infrastructure.Reconciliation;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Infrastructure.Pipeline;

public class PdfProcessingPipeline : IPdfProcessingPipeline
{
    private readonly RuntimeOptions _options;
    private readonly IReadOnlyList<IOcrEngine> _ocrEngines;
    private readonly ProcessedIndexStore _indexStore;
    private readonly OcrRunPlanBuilder? _runPlanBuilder;
    private readonly DocumentPreparationStage? _documentPreparationStage;
    private readonly NormalizedOcrResultWriter _normalizedOcrResultWriter;
    private readonly NormalizedOcrReconciler _normalizedOcrReconciler;
    private readonly TeamIdentityMatcher _teamIdentityMatcher = new();
    private readonly IdentityReviewBuilder _identityReviewBuilder = new();

    public PdfProcessingPipeline(
        RuntimeOptions options,
        IEnumerable<IOcrEngine> ocrEngines,
        OcrRunPlanBuilder? runPlanBuilder = null,
        DocumentPreparationStage? documentPreparationStage = null,
        NormalizedOcrResultWriter? normalizedOcrResultWriter = null,
        NormalizedOcrReconciler? normalizedOcrReconciler = null)
    {
        _options = options;
        _ocrEngines = ocrEngines.ToArray();
        _indexStore = new ProcessedIndexStore(_options.ProcessedIndexPath);
        _runPlanBuilder = runPlanBuilder;
        _documentPreparationStage = documentPreparationStage;
        _normalizedOcrResultWriter = normalizedOcrResultWriter ?? new NormalizedOcrResultWriter(_options);
        _normalizedOcrReconciler = normalizedOcrReconciler ?? new NormalizedOcrReconciler();
    }

    public async Task<ProcessingResult> ProcessPdfAsync(string pdfPath, CancellationToken cancellationToken = default)
    {
        return await ProcessPdfInternalAsync(pdfPath, null, null, cancellationToken);
    }

    public async Task<ProcessingResult> ProcessPdfAsync(
        string pdfPath,
        OcrRunSelection selection,
        OcrMatchContext? matchContext = null,
        CancellationToken cancellationToken = default)
    {
        return await ProcessPdfInternalAsync(pdfPath, selection, matchContext, cancellationToken);
    }

    private async Task<ProcessingResult> ProcessPdfInternalAsync(string pdfPath, OcrRunSelection? selection, OcrMatchContext? matchContext, CancellationToken cancellationToken)
    {
        RuntimeFolderInitializer.EnsureCreated(_options);
        var startedAt = DateTimeOffset.Now;
        var sourcePath = Path.GetFullPath(pdfPath);
        var fileName = Path.GetFileName(sourcePath);
        var result = CreateBaseResult(fileName, sourcePath, startedAt);
        string? workingPath = null;

        try
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("PDF input not found.", sourcePath);
            }

            if (selection is not null && !selection.TryValidate(out var selectionError))
            {
                throw new InvalidOperationException(selectionError);
            }

            var documentHash = await DocumentHasher.ComputeSha256Async(sourcePath, cancellationToken);
            result.ProcessedFile.DocumentHash = documentHash;
            result.ProcessedFile.Status = FileProcessingStatus.Processing;
            workingPath = CopyToWorking(sourcePath, documentHash);
            result.ProcessedFile.WorkingPath = workingPath;

            var request = new OcrProcessingRequest
            {
                PdfPath = sourcePath,
                WorkingPath = workingPath,
                OriginalFileName = fileName,
                DocumentHash = documentHash,
                MatchContext = matchContext
            };
            if (selection is not null && _runPlanBuilder is not null)
            {
                request.RunPlan = _runPlanBuilder.Build(request, selection);
            }

            var engineResults = await RunEnginesAsync(request, selection, cancellationToken);
            var normalizedResults = engineResults.Where(HasSuccessfulRun).ToArray();

            var reconciledResult = normalizedResults.Length == 0
                ? null
                : _normalizedOcrReconciler.Reconcile(normalizedResults);

            if (reconciledResult is null)
            {
                result.OcrRuns = engineResults.SelectMany(x => x.OcrRuns).ToList();
                result.ProcessedFile.Status = FileProcessingStatus.Failed;
                result.Validation.Status = FileProcessingStatus.Failed;
                result.Validation.FailedRules.Add("ocr.noSuccessfulEngine");
                result.Validation.Warnings.Add(new ValidationWarning
                {
                    RuleId = "ocr.noSuccessfulEngine",
                    Severity = "Error",
                    Message = "Nessun motore OCR configurato o completato con successo."
                });
                result.ProcessedFile.OutputJsonPath = await SaveResultJsonAsync(result, cancellationToken);
                result.ProcessedFile.FinalPath = sourcePath;
                result.ProcessedFile.CompletedAt = DateTimeOffset.Now;
                return result;
            }

            MergePrimaryResult(result, reconciledResult, engineResults);
            ApplyTeamIdentity(result, matchContext);
            if (matchContext is not null)
            {
                result.IdentityReview = _identityReviewBuilder.Build(result, matchContext);
            }
            MergePreparationDiagnostics(result, request);
            result.ProcessedFile.Status = ResolveCompletedStatus(result);
            result.Validation.Status = result.ProcessedFile.Status;
            result.ProcessedFile.OutputJsonPath = await SaveResultJsonAsync(result, cancellationToken);
            result.ProcessedFile.FinalPath = sourcePath;
            result.ProcessedFile.CompletedAt = DateTimeOffset.Now;
            await _indexStore.UpsertAsync(result, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            result.ProcessedFile.Status = FileProcessingStatus.Failed;
            result.ProcessedFile.ErrorMessage = ex.Message;
            result.ProcessedFile.CompletedAt = DateTimeOffset.Now;
            result.Validation.Status = FileProcessingStatus.Failed;
            result.Validation.FailedRules.Add("pipeline.fatalError");
            result.Validation.Warnings.Add(new ValidationWarning
            {
                RuleId = "pipeline.fatalError",
                Severity = "Error",
                Message = ex.Message
            });
            result.ProcessedFile.OutputJsonPath = await SaveResultJsonAsync(result, cancellationToken);
            result.ProcessedFile.FinalPath = sourcePath;
            return result;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(workingPath))
            {
                CleanupWorkingFile(workingPath);
            }
        }
    }

    private async Task<List<ProcessingResult>> RunEnginesAsync(OcrProcessingRequest request, OcrRunSelection? selection, CancellationToken cancellationToken)
    {
        var results = new List<ProcessingResult>();
        var engines = _ocrEngines.Where(engine => ShouldRunEngine(engine, selection)).ToArray();

        // TesseractFullPage runs first: it exports word-box geometry needed by LayoutCalibration.
        foreach (var engine in engines.Where(engine => string.Equals(engine.EngineName, OcrStrategyNames.TesseractFullPage, StringComparison.OrdinalIgnoreCase)))
        {
            results.Add(await RunEngineAsync(engine, request, cancellationToken));
        }

        if (request.RunPlan?.NeedsLayoutCrops == true && _documentPreparationStage is not null)
        {
            request.DocumentPreparation = await _documentPreparationStage.PrepareAsync(request, request.RunPlan, cancellationToken);
        }

        foreach (var engine in engines.Where(engine => !string.Equals(engine.EngineName, OcrStrategyNames.TesseractFullPage, StringComparison.OrdinalIgnoreCase)))
        {
            results.Add(await RunEngineAsync(engine, request, cancellationToken));
        }

        if (results.Count == 0)
        {
            results.Add(new ProcessingResult
            {
                OcrRuns = [new OcrRunResult { Engine = "None", Status = OcrRunStatus.NotConfigured, Error = "No OCR engines configured." }]
            });
        }

        return results;
    }

    private async Task<ProcessingResult> RunEngineAsync(IOcrEngine engine, OcrProcessingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await engine.ProcessAsync(request, cancellationToken);
            if (HasSuccessfulRun(result))
            {
                await _normalizedOcrResultWriter.WriteAsync(engine.EngineName, request, result, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            return new ProcessingResult
            {
                OcrRuns =
                [
                    new OcrRunResult
                    {
                        Engine = engine.EngineName,
                        Status = OcrRunStatus.Failed,
                        Error = ex.Message
                    }
                ],
                Validation = new ValidationResult
                {
                    Status = FileProcessingStatus.CompletedNotValidated,
                    Warnings =
                    [
                        new ValidationWarning
                        {
                            RuleId = "ocr.engineException",
                            Severity = "Warning",
                            Message = $"{engine.EngineName}: {ex.Message}"
                        }
                    ]
                }
            };
        }
    }

    private static bool ShouldRunEngine(IOcrEngine engine, OcrRunSelection? selection)
    {
        if (selection is null)
        {
            return true;
        }

        return engine.EngineName switch
        {
            OcrStrategyNames.TesseractFullPage or
            OcrStrategyNames.TesseractLayoutCrops => selection.UseTesseract,
            OcrStrategyNames.PaddleLayoutCrops or
            OcrStrategyNames.PaddleTableRows => selection.UsePaddle,
            _ => false
        };
    }

    private string CopyToWorking(string sourcePath, string documentHash)
    {
        var shortHash = DocumentHasher.ShortHash(documentHash);
        var workingPath = Path.Combine(_options.WorkingPath, $"{shortHash}_{Path.GetFileName(sourcePath)}");
        workingPath = SafeFileMover.UniquePath(workingPath);
        File.Copy(sourcePath, workingPath, overwrite: false);
        return workingPath;
    }

    private async Task<string> SaveResultJsonAsync(ProcessingResult result, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_options.OutputJsonPath);
        var stem = Path.GetFileNameWithoutExtension(result.ProcessedFile.FileName);
        var shortHash = string.IsNullOrWhiteSpace(result.ProcessedFile.DocumentHash)
            ? "nohash"
            : DocumentHasher.ShortHash(result.ProcessedFile.DocumentHash);
        var outputPath = result.ProcessedFile.OutputJsonPath;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = SafeFileMover.UniquePath(Path.Combine(_options.OutputJsonPath, $"{stem}.{shortHash}.json"));
        }

        await using var stream = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(stream, result, JsonDefaults.Options, cancellationToken);
        return outputPath;
    }

    private static ProcessingResult CreateBaseResult(string fileName, string sourcePath, DateTimeOffset startedAt)
    {
        return new ProcessingResult
        {
            ProcessedFile = new ProcessedFile
            {
                FileName = fileName,
                SourcePath = sourcePath,
                Status = FileProcessingStatus.Pending,
                StartedAt = startedAt
            }
        };
    }

    /// <summary>
    /// Confronta le squadre lette dall'OCR con il contesto DB (dopo CH1/riconciliazione).
    /// Inversione → riallineamento automatico + flag; mismatch → warning. No-op senza contesto.
    /// </summary>
    private void ApplyTeamIdentity(ProcessingResult result, OcrMatchContext? matchContext)
    {
        if (matchContext is null)
        {
            return;
        }

        var homeOcr = result.Teams.FirstOrDefault(t => string.Equals(t.Side, "Home", StringComparison.OrdinalIgnoreCase))?.Name;
        var awayOcr = result.Teams.FirstOrDefault(t => string.Equals(t.Side, "Away", StringComparison.OrdinalIgnoreCase))?.Name;
        var homeDb = matchContext.HomeTeam.Name;
        var awayDb = matchContext.AwayTeam.Name;

        // Servono entrambi i nomi OCR e DB per un confronto significativo.
        if (string.IsNullOrWhiteSpace(homeOcr) || string.IsNullOrWhiteSpace(awayOcr) ||
            string.IsNullOrWhiteSpace(homeDb) || string.IsNullOrWhiteSpace(awayDb))
        {
            return;
        }

        var match = _teamIdentityMatcher.Match(homeOcr, awayOcr, matchContext);
        switch (match.Outcome)
        {
            case TeamMatchOutcome.Inverted:
                SideInversion.Apply(result);
                result.Reconciliation ??= new ReconciliationMetadata();
                result.Reconciliation.SideInversionApplied = true;
                result.Validation.Warnings.Add(new ValidationWarning
                {
                    RuleId = "team.sideInversionApplied",
                    Severity = "Info",
                    Message = $"Inversione Home/Away rilevata e corretta. Squadre DB: {homeDb} (Home) vs {awayDb} (Away)."
                });
                break;

            case TeamMatchOutcome.Mismatch:
                result.Validation.Warnings.Add(new ValidationWarning
                {
                    RuleId = "team.mismatch",
                    Severity = "Warning",
                    Message = $"Squadre OCR non corrispondono al DB. Attese: {homeDb} vs {awayDb}; rilevate: {homeOcr} vs {awayOcr}."
                });
                break;
        }
    }

    private static void MergePrimaryResult(ProcessingResult target, ProcessingResult primaryResult, IReadOnlyList<ProcessingResult> engineResults)
    {
        target.Game = primaryResult.Game;
        target.Teams = primaryResult.Teams;
        target.Players = primaryResult.Players;
        target.Stats = primaryResult.Stats;
        target.Validation = primaryResult.Validation;
        target.Reconciliation = primaryResult.Reconciliation;
        target.OcrRuns = engineResults.SelectMany(x => x.OcrRuns).ToList();

        foreach (var failedRun in target.OcrRuns.Where(x => x.Status is OcrRunStatus.Failed or OcrRunStatus.Timeout or OcrRunStatus.NotConfigured))
        {
            target.Validation.Warnings.Add(new ValidationWarning
            {
                RuleId = $"ocr.{failedRun.Status}",
                Severity = failedRun.Status == OcrRunStatus.NotConfigured ? "Info" : "Warning",
                Message = string.IsNullOrWhiteSpace(failedRun.Error)
                    ? $"{failedRun.Engine}: {failedRun.Status}"
                    : $"{failedRun.Engine}: {failedRun.Error}"
            });
        }
    }

    private static void MergePreparationDiagnostics(ProcessingResult target, OcrProcessingRequest request)
    {
        if (request.RunPlan is not null)
        {
            target.Validation.Warnings.AddRange(request.RunPlan.Warnings.Select(message => new ValidationWarning
            {
                RuleId = "ocr.plan.warning",
                Severity = "Info",
                Message = message
            }));
        }

        if (request.DocumentPreparation is null)
        {
            return;
        }

        target.Validation.Warnings.AddRange(request.DocumentPreparation.Warnings.Select(message => new ValidationWarning
        {
            RuleId = "ocr.layoutCrops.warning",
            Severity = "Info",
            Message = message
        }));
        target.Validation.Warnings.AddRange(request.DocumentPreparation.Errors.Select(message => new ValidationWarning
        {
            RuleId = "ocr.layoutCrops.error",
            Severity = "Warning",
            Message = message
        }));
    }

    private static bool HasSuccessfulRun(ProcessingResult result)
    {
        return result.OcrRuns.Any(x => x.Status == OcrRunStatus.Success);
    }

    private static FileProcessingStatus ResolveCompletedStatus(ProcessingResult result)
    {
        if (result.Validation.Warnings.Any(x => string.Equals(x.Severity, "Error", StringComparison.OrdinalIgnoreCase)))
        {
            return FileProcessingStatus.CompletedNotValidated;
        }

        if (result.IdentityReview.Any(r => r.Reason is IdentityReviewReason.Conflict or IdentityReviewReason.NotInPdf))
        {
            return FileProcessingStatus.CompletedWithReviewRequired;
        }

        if (result.Validation.Warnings.Any(x => string.Equals(x.Severity, "Warning", StringComparison.OrdinalIgnoreCase)) ||
            result.Stats.Any(x => x.Status == StatValidationStatus.NonValidato))
        {
            return FileProcessingStatus.CompletedWithWarnings;
        }

        return FileProcessingStatus.CompletedValidated;
    }

    private static void CleanupWorkingFile(string workingPath)
    {
        if (File.Exists(workingPath))
        {
            File.Delete(workingPath);
        }
    }
}
