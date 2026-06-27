using BasketPdfStats.App.Services;
using BasketPdfStats.App.ViewModels;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Pipeline;

namespace BasketPdfStats.Tests;

/// <summary>
/// Modalità normale "pulita": la UI mostra solo messaggi importanti (niente stderr dei run riusciti,
/// niente warning OCR diagnostici), il riepilogo conta errori/revisioni/math, e l'import è consentito
/// solo se non ci sono errori bloccanti.
/// </summary>
public sealed class MainViewModelNoiseTests
{
    [Fact]
    public void Successful_run_stderr_is_not_shown_but_failed_run_is()
    {
        var result = ResultWith(
            status: FileProcessingStatus.CompletedWithWarnings,
            ocrRuns:
            [
                new OcrRunResult { Engine = "TesseractFullPage", Status = OcrRunStatus.Success, Error = "PADDLE NOISE LOG: roster dinamico attivo" },
                new OcrRunResult { Engine = "PaddleTableRows", Status = OcrRunStatus.Failed, Error = "real boom" },
            ],
            warnings: [Warning("math.pointsFormula", "Warning", "Punti non coerenti.")]);

        var vm = Process(result);

        Assert.DoesNotContain("PADDLE NOISE", vm.WarningText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("real boom", vm.WarningText, StringComparison.Ordinal);
        Assert.Contains("Punti non coerenti", vm.WarningText, StringComparison.Ordinal);
    }

    [Fact]
    public void Processing_summary_counts_blocking_reviews_and_math()
    {
        var result = ResultWith(
            status: FileProcessingStatus.CompletedWithWarnings,
            warnings: [Warning("math.pointsFormula", "Warning", "Punti non coerenti.")]);

        var vm = Process(result);

        Assert.Contains("0 errori bloccanti", vm.ProcessingSummary, StringComparison.Ordinal);
        Assert.Contains("0 revisioni richieste", vm.ProcessingSummary, StringComparison.Ordinal);
        Assert.Contains("1 avvisi matematici", vm.ProcessingSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void CanImport_is_true_with_only_math_warnings()
    {
        var result = ResultWith(
            status: FileProcessingStatus.CompletedWithWarnings,
            warnings: [Warning("math.pointsFormula", "Warning", "Punti non coerenti.")]);

        var vm = Process(result);

        Assert.True(vm.ImportToDbCommand.CanExecute(null));
    }

    [Fact]
    public void CanImport_is_false_with_blocking_error_warning()
    {
        var result = ResultWith(
            status: FileProcessingStatus.CompletedNotValidated,
            warnings: [Warning("ocr.noSuccessfulEngine", "Error", "Errore grave.")]);

        var vm = Process(result);

        Assert.False(vm.ImportToDbCommand.CanExecute(null));
        Assert.Contains("1 errori bloccanti", vm.ProcessingSummary, StringComparison.Ordinal);
    }

    private static ProcessingResult ResultWith(
        FileProcessingStatus status,
        List<OcrRunResult>? ocrRuns = null,
        List<ValidationWarning>? warnings = null)
    {
        return new ProcessingResult
        {
            ProcessedFile = new ProcessedFile { FileName = "x.pdf", Status = status },
            OcrRuns = ocrRuns ?? [],
            // Teams vuote: nessun mismatch PDF↔match (il controllo non si attiva senza nomi OCR).
            Teams = [],
            Validation = new ValidationResult { Status = status, Warnings = warnings ?? [] },
        };
    }

    private static ValidationWarning Warning(string ruleId, string severity, string message) =>
        new() { RuleId = ruleId, Severity = severity, Message = message };

    private static MainViewModel Process(ProcessingResult result)
    {
        var vm = new MainViewModel(new StubPipeline(result), new StubFilePicker(), importService: new StubImportService());
        vm.SelectedMatchOption = new OcrMatchOption { MatchId = 63, DisplayName = "m" };
        vm.ActiveContextLoad!.GetAwaiter().GetResult();
        vm.SelectedPdfPath = "C:\\tmp\\x.pdf";
        vm.ProcessPdfCommand.Execute(null); // completa in modo sincrono (stub)
        return vm;
    }

    private sealed class StubPipeline : IPdfProcessingPipeline
    {
        private readonly ProcessingResult _result;
        public StubPipeline(ProcessingResult result) => _result = result;

        public Task<ProcessingResult> ProcessPdfAsync(string pdfPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);

        public Task<ProcessingResult> ProcessPdfAsync(string pdfPath, OcrRunSelection selection, OcrMatchContext? matchContext = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);
    }

    private sealed class StubImportService : IOcrImportService
    {
        public Task<OcrMatchLookupResult> GetMatchesForDateAsync(DateOnly date, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OcrMatchLookupResult { Success = true });

        public Task<OcrMatchContextResult> GetMatchContextAsync(int matchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OcrMatchContextResult
            {
                Success = true,
                Context = new OcrMatchContext
                {
                    MatchId = matchId,
                    HomeTeam = new OcrContextTeam { TeamId = 1, Name = "Home", Players = [new OcrRosterPlayer { PlayerId = 1, TeamId = 1, FirstName = "A", LastName = "B", JerseyNumber = 4 }] },
                    AwayTeam = new OcrContextTeam { TeamId = 2, Name = "Away", Players = [new OcrRosterPlayer { PlayerId = 2, TeamId = 2, FirstName = "C", LastName = "D", JerseyNumber = 5 }] },
                },
            });

        public Task<OcrImportResult> ImportAsync(int matchId, ImportPayload payload, bool allowTeamMismatch = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OcrImportResult { Success = true, MatchId = matchId });
    }

    private sealed class StubFilePicker : IFilePicker
    {
        public string? PickPdf() => null;
    }
}
