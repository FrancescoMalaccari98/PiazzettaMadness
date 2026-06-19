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
        var review = new StubReviewService(confirm: true);
        var vm = await ArrangeReviewRequiredAsync(review);

        // L'elaborazione richiede revisione → import bloccato.
        Assert.False(vm.ImportToDbCommand.CanExecute(null));
        Assert.True(vm.ReviewIdentityCommand.CanExecute(null));

        vm.ReviewIdentityCommand.Execute(null);

        Assert.True(review.Invoked);
        Assert.True(vm.ImportToDbCommand.CanExecute(null));
    }

    [Fact]
    public async Task Import_stays_blocked_when_review_cancelled()
    {
        var review = new StubReviewService(confirm: false);
        var vm = await ArrangeReviewRequiredAsync(review);

        vm.ReviewIdentityCommand.Execute(null);

        Assert.True(review.Invoked);
        Assert.False(vm.ImportToDbCommand.CanExecute(null));
    }

    private static async Task<MainViewModel> ArrangeReviewRequiredAsync(IIdentityReviewService review)
    {
        var reviewResult = new ProcessingResult
        {
            ProcessedFile = new ProcessedFile { FileName = "x.pdf", Status = FileProcessingStatus.CompletedWithReviewRequired },
            IdentityReview =
            [
                new IdentityReviewItem { Side = "Home", Reason = IdentityReviewReason.Conflict, OcrJersey = "5", OcrName = "X" },
            ],
        };

        var importService = new StubImportService();
        var vm = new MainViewModel(
            new StubPipeline(reviewResult),
            new StubFilePicker(),
            importService: importService,
            identityReviewService: review);

        vm.SelectedMatchOption = new OcrMatchOption { MatchId = 7, DisplayName = "m" };
        await vm.ActiveContextLoad!; // carica il contesto (sblocca ProcessPdf)
        vm.SelectedPdfPath = "C:\\tmp\\x.pdf";

        Assert.True(vm.ProcessPdfCommand.CanExecute(null));
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
                    HomeTeam = new OcrContextTeam { TeamId = 1, Name = "Home", Players = [new OcrRosterPlayer { PlayerId = 1, TeamId = 1, FirstName = "Mario", LastName = "Rossi", JerseyNumber = 5 }] },
                    AwayTeam = new OcrContextTeam { TeamId = 2, Name = "Away", Players = [new OcrRosterPlayer { PlayerId = 2, TeamId = 2, FirstName = "Luca", LastName = "Bianchi", JerseyNumber = 7 }] },
                },
            });

        public Task<OcrImportResult> ImportAsync(int matchId, ImportPayload payload, bool allowTeamMismatch = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OcrImportResult { Success = true, MatchId = matchId });
    }

    private sealed class StubReviewService : IIdentityReviewService
    {
        private readonly bool _confirm;
        public StubReviewService(bool confirm) => _confirm = confirm;
        public bool Invoked { get; private set; }

        public IReadOnlyDictionary<string, int>? ReviewAndConfirm(IReadOnlyList<IdentityReviewItem> items, OcrMatchContext context)
        {
            Invoked = true;
            return _confirm ? new Dictionary<string, int>() : null;
        }
    }

    private sealed class StubFilePicker : IFilePicker
    {
        public string? PickPdf() => null;
    }
}
