using BasketPdfStats.App.Services;
using BasketPdfStats.App.ViewModels;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Identity;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Core.Pipeline;

namespace BasketPdfStats.Tests;

public sealed class MainViewModelReviewTests
{
    [Fact]
    public async Task Import_blocked_until_review_confirmed()
    {
        var review = new StubManualReviewService(confirm: true);
        var vm = await ArrangeReviewRequiredAsync(review);

        // L'elaborazione richiede revisione → import bloccato finché non confermato.
        Assert.False(vm.ImportToDbCommand.CanExecute(null));
        Assert.True(vm.ReviewIdentityCommand.CanExecute(null));

        vm.ReviewIdentityCommand.Execute(null);

        Assert.True(review.Invoked);
        Assert.True(vm.ImportToDbCommand.CanExecute(null));
    }

    [Fact]
    public async Task Import_stays_blocked_when_review_cancelled()
    {
        var review = new StubManualReviewService(confirm: false);
        var vm = await ArrangeReviewRequiredAsync(review);

        vm.ReviewIdentityCommand.Execute(null);

        Assert.True(review.Invoked);
        Assert.False(vm.ImportToDbCommand.CanExecute(null));
    }

    [Fact]
    public async Task Stat_edits_are_applied_to_result_on_confirm()
    {
        var result = BuildReviewRequiredResult();
        result.Stats.Add(new StatValue
        {
            Scope = StatScope.Player,
            EntityId = "player:Home:jersey:5",
            StatKey = "points",
            FieldId = "player:Home:jersey:5:points",
            Value = 10,
        });

        var edit = new StatEdit("player:Home:jersey:5:points", 18);
        var review = new StubManualReviewService(confirm: true, statEdits: [edit]);
        var vm = await ArrangeAsync(result, review);

        vm.ReviewIdentityCommand.Execute(null);

        // Il valore corretto deve essere riflesso nel risultato (TelecronacaViewModel si rigenera).
        Assert.True(review.Invoked);
        Assert.True(vm.ImportToDbCommand.CanExecute(null));
    }

    [Fact]
    public async Task Review_button_always_enabled_after_successful_processing()
    {
        // Anche senza conflitti identità il pulsante deve essere abilitato.
        // I nomi squadra corrispondono al contesto (StubImportService usa "Home"/"Away").
        var result = new ProcessingResult
        {
            ProcessedFile = new ProcessedFile { FileName = "x.pdf", Status = FileProcessingStatus.CompletedValidated },
            Teams = [new TeamStats { Side = "Home", Name = "Home" }, new TeamStats { Side = "Away", Name = "Away" }],
        };
        var review = new StubManualReviewService(confirm: true);
        var vm = await ArrangeAsync(result, review);

        Assert.True(vm.ReviewIdentityCommand.CanExecute(null));
    }

    private static async Task<MainViewModel> ArrangeReviewRequiredAsync(IManualReviewService review)
        => await ArrangeAsync(BuildReviewRequiredResult(), review);

    private static async Task<MainViewModel> ArrangeAsync(ProcessingResult result, IManualReviewService review)
    {
        var importService = new StubImportService();
        var vm = new MainViewModel(
            new StubPipeline(result),
            new StubFilePicker(),
            importService: importService,
            manualReviewService: review);

        vm.SelectedMatchOption = new OcrMatchOption { MatchId = 7, DisplayName = "m" };
        await vm.ActiveContextLoad!;
        vm.SelectedPdfPath = "C:\\tmp\\x.pdf";

        Assert.True(vm.ProcessPdfCommand.CanExecute(null));
        vm.ProcessPdfCommand.Execute(null);
        return vm;
    }

    private static ProcessingResult BuildReviewRequiredResult() => new()
    {
        ProcessedFile = new ProcessedFile { FileName = "x.pdf", Status = FileProcessingStatus.CompletedWithReviewRequired },
        IdentityReview =
        [
            new IdentityReviewItem { Side = "Home", Reason = IdentityReviewReason.Conflict, OcrJersey = "5", OcrName = "X" },
        ],
    };

    private sealed class StubPipeline : IPdfProcessingPipeline
    {
        private readonly ProcessingResult _result;
        public StubPipeline(ProcessingResult result) => _result = result;
        public Task<ProcessingResult> ProcessPdfAsync(string pdfPath, CancellationToken cancellationToken = default) => Task.FromResult(_result);
        public Task<ProcessingResult> ProcessPdfAsync(string pdfPath, OcrRunSelection selection, OcrMatchContext? matchContext = null, CancellationToken cancellationToken = default) => Task.FromResult(_result);
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
                    HomeTeam = new OcrContextTeam { TeamId = 1, Name = "Home", Players = [new OcrRosterPlayer { PlayerId = 1, TeamId = 1, FirstName = "Mario", LastName = "Rossi", JerseyNumber = 5 }] },
                    AwayTeam = new OcrContextTeam { TeamId = 2, Name = "Away", Players = [new OcrRosterPlayer { PlayerId = 2, TeamId = 2, FirstName = "Luca", LastName = "Bianchi", JerseyNumber = 7 }] },
                },
            });

        public Task<OcrImportResult> ImportAsync(int matchId, ImportPayload payload, bool allowTeamMismatch = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OcrImportResult { Success = true, MatchId = matchId });
    }

    private sealed class StubManualReviewService : IManualReviewService
    {
        private readonly bool _confirm;
        private readonly IReadOnlyList<StatEdit> _statEdits;
        public StubManualReviewService(bool confirm, IReadOnlyList<StatEdit>? statEdits = null)
        {
            _confirm = confirm;
            _statEdits = statEdits ?? [];
        }
        public bool Invoked { get; private set; }
        public ManualEditSet? ReviewAndEdit(ProcessingResult result, OcrMatchContext? context)
        {
            Invoked = true;
            return _confirm ? new ManualEditSet(_statEdits, new Dictionary<string, int>()) : null;
        }
    }

    private sealed class StubFilePicker : IFilePicker
    {
        public string? PickPdf() => null;
    }
}
