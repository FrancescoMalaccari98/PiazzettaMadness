using BasketPdfStats.App.Services;
using BasketPdfStats.App.ViewModels;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Identity;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Pipeline;

namespace BasketPdfStats.Tests;

/// <summary>
/// Bugfix: il PDF elaborato deve corrispondere al match selezionato. Se le squadre OCR del PDF
/// non corrispondono a quelle del match scelto, import e revisione sono bloccati e nessuna chiamata
/// di import parte. Home/Away invertiti non sono un errore.
/// Il controllo è in <c>MainViewModel.DetectTeamMismatch</c> (eseguito in <c>AddResult</c>, dopo l'OCR).
/// </summary>
public sealed class MainViewModelTeamMismatchTests
{
    [Fact]
    public void Mismatch_blocks_import_and_shows_message()
    {
        var (vm, import) = Arrange(
            ctxHome: "Hydra Ravens", ctxAway: "Quantum Bulls",
            pdfHome: "Aurora Lynx", pdfAway: "Nebula Bears");

        Assert.False(vm.ImportToDbCommand.CanExecute(null));
        Assert.Equal("PDF non corrispondente", vm.WorkflowStatus);
        Assert.Contains("non corrisponde al match", vm.ImportStatusText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hydra Ravens vs Quantum Bulls", vm.ImportStatusText, StringComparison.Ordinal);
        Assert.Contains("Aurora Lynx vs Nebula Bears", vm.ImportStatusText, StringComparison.Ordinal);
        Assert.Equal(0, import.ImportCalls);
    }

    [Fact]
    public void Matching_teams_allow_import()
    {
        var (vm, _) = Arrange(
            ctxHome: "Aurora Lynx", ctxAway: "Nebula Bears",
            pdfHome: "Aurora Lynx", pdfAway: "Nebula Bears");

        Assert.True(vm.ImportToDbCommand.CanExecute(null));
        Assert.Equal("Completato", vm.WorkflowStatus);
    }

    [Fact]
    public void Inverted_home_away_is_not_a_mismatch()
    {
        var (vm, _) = Arrange(
            ctxHome: "Aurora Lynx", ctxAway: "Nebula Bears",
            pdfHome: "Nebula Bears", pdfAway: "Aurora Lynx");

        Assert.True(vm.ImportToDbCommand.CanExecute(null));
        Assert.NotEqual("PDF non corrispondente", vm.WorkflowStatus);
    }

    [Fact]
    public void Mismatch_does_not_trigger_backend_import_even_if_invoked()
    {
        var (vm, import) = Arrange(
            ctxHome: "Hydra Ravens", ctxAway: "Quantum Bulls",
            pdfHome: "Aurora Lynx", pdfAway: "Nebula Bears");

        // Anche forzando l'esecuzione del comando, l'import non deve partire.
        vm.ImportToDbCommand.Execute(null);

        Assert.Equal(0, import.ImportCalls);
    }

    [Fact]
    public void Mismatch_blocks_review_too()
    {
        var (vm, _) = Arrange(
            ctxHome: "Hydra Ravens", ctxAway: "Quantum Bulls",
            pdfHome: "Aurora Lynx", pdfAway: "Nebula Bears",
            review: [new IdentityReviewItem { Side = "Home", Reason = IdentityReviewReason.Conflict, OcrJersey = "5", OcrName = "X" }],
            withReviewService: true);

        Assert.False(vm.ReviewIdentityCommand.CanExecute(null));
    }

    private static (MainViewModel vm, FakeImportService import) Arrange(
        string ctxHome, string ctxAway, string pdfHome, string pdfAway,
        IdentityReviewItem[]? review = null, bool withReviewService = false)
    {
        var status = review is { Length: > 0 } ? FileProcessingStatus.CompletedWithReviewRequired : FileProcessingStatus.CompletedWithWarnings;
        var result = new ProcessingResult
        {
            ProcessedFile = new ProcessedFile { FileName = "x.pdf", Status = status },
            Teams =
            [
                new TeamStats { Side = "Home", Name = pdfHome },
                new TeamStats { Side = "Away", Name = pdfAway },
            ],
            IdentityReview = review?.ToList() ?? [],
        };

        var import = new FakeImportService(ctxHome, ctxAway);
        var vm = new MainViewModel(
            new StubPipeline(result),
            new StubFilePicker(),
            importService: import,
            manualReviewService: withReviewService ? new StubManualReviewService() : null);

        vm.SelectedMatchOption = new OcrMatchOption
        {
            MatchId = 64,
            DisplayName = $"{ctxHome} vs {ctxAway}",
            HomeTeam = new OcrMatchTeam { Name = ctxHome },
            AwayTeam = new OcrMatchTeam { Name = ctxAway },
        };
        vm.ActiveContextLoad!.GetAwaiter().GetResult();
        vm.SelectedPdfPath = "C:\\tmp\\x.pdf";
        vm.ProcessPdfCommand.Execute(null); // completa in modo sincrono (stub)

        return (vm, import);
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

    private sealed class FakeImportService : IOcrImportService
    {
        private readonly string _ctxHome;
        private readonly string _ctxAway;
        public int ImportCalls { get; private set; }

        public FakeImportService(string ctxHome, string ctxAway)
        {
            _ctxHome = ctxHome;
            _ctxAway = ctxAway;
        }

        public Task<OcrMatchLookupResult> GetMatchesForDateAsync(DateOnly date, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OcrMatchLookupResult { Success = true });

        public Task<OcrMatchContextResult> GetMatchContextAsync(int matchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OcrMatchContextResult
            {
                Success = true,
                Context = new OcrMatchContext
                {
                    MatchId = matchId,
                    HomeTeam = new OcrContextTeam { TeamId = 1, Name = _ctxHome, Players = [new OcrRosterPlayer { PlayerId = 1, TeamId = 1, FirstName = "A", LastName = "B", JerseyNumber = 4 }] },
                    AwayTeam = new OcrContextTeam { TeamId = 2, Name = _ctxAway, Players = [new OcrRosterPlayer { PlayerId = 2, TeamId = 2, FirstName = "C", LastName = "D", JerseyNumber = 5 }] },
                },
            });

        public Task<OcrImportResult> ImportAsync(int matchId, ImportPayload payload, bool allowTeamMismatch = false, CancellationToken cancellationToken = default)
        {
            ImportCalls++;
            return Task.FromResult(new OcrImportResult { Success = true, MatchId = matchId });
        }
    }

    private sealed class StubManualReviewService : IManualReviewService
    {
        public ManualEditSet? ReviewAndEdit(ProcessingResult result, OcrMatchContext? context) =>
            new ManualEditSet([], new Dictionary<string, int>());
    }

    private sealed class StubFilePicker : IFilePicker
    {
        public string? PickPdf() => null;
    }
}
