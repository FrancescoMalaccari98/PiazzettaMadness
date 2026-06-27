using BasketPdfStats.App.Services;
using BasketPdfStats.App.ViewModels;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Pipeline;

namespace BasketPdfStats.Tests;

/// <summary>
/// Copre le proprietà calcolate aggiunte per la UI a step (stato workflow, riepilogo match, hint).
/// Solo stato del ViewModel: nessuna dipendenza da WPF.
/// </summary>
public sealed class MainViewModelUiStateTests
{
    [Fact]
    public void WorkflowStatus_default_is_pronto()
    {
        var vm = CreateViewModel(out _);
        Assert.Equal("Pronto", vm.WorkflowStatus);
    }

    [Fact]
    public void ProcessHint_without_match_asks_for_match()
    {
        var vm = CreateViewModel(out _);
        vm.SelectedPdfPath = "C:\\tmp\\sample.pdf";

        Assert.False(vm.IsContextReady);
        Assert.Contains("partita", vm.ProcessHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessHint_with_match_but_no_pdf_asks_for_pdf()
    {
        var vm = CreateViewModel(out _);
        await SelectMatchWithContext(vm, 63);

        Assert.True(vm.IsContextReady);
        Assert.Contains("PDF", vm.ProcessHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessHint_ready_when_match_and_pdf_present()
    {
        var vm = CreateViewModel(out _);
        await SelectMatchWithContext(vm, 63);
        vm.SelectedPdfPath = "C:\\tmp\\sample.pdf";

        Assert.Equal("Pronto per elaborare.", vm.ProcessHint);
    }

    [Fact]
    public async Task Match_summary_properties_reflect_selected_option()
    {
        var vm = CreateViewModel(out _);

        vm.SelectedMatchOption = new OcrMatchOption
        {
            MatchId = 63,
            DisplayName = "Aurora Lynx vs Nebula Bears",
            HomeTeam = new OcrMatchTeam { Name = "Aurora Lynx" },
            AwayTeam = new OcrMatchTeam { Name = "Nebula Bears" },
            ScheduledStartAt = "2026-07-08 20:30:00",
            Phase = "GroupStage",
            Round = "Giornata 1"
        };
        await vm.ActiveContextLoad!;

        Assert.True(vm.HasSelectedMatch);
        Assert.Equal("Aurora Lynx vs Nebula Bears", vm.SelectedMatchTeams);
        Assert.Equal("08/07/2026 20:30", vm.SelectedMatchTime);
        Assert.Equal("GroupStage — Giornata 1", vm.SelectedMatchPhase);
        Assert.Equal("63", vm.MatchIdText);
    }

    [Fact]
    public void Match_summary_empty_without_selection()
    {
        var vm = CreateViewModel(out _);

        Assert.False(vm.HasSelectedMatch);
        Assert.Equal(string.Empty, vm.SelectedMatchTeams);
        Assert.Equal(string.Empty, vm.SelectedMatchTime);
        Assert.Equal(string.Empty, vm.SelectedMatchPhase);
    }

    [Fact]
    public async Task IsContextReady_becomes_true_after_context_load()
    {
        var vm = CreateViewModel(out _);
        Assert.False(vm.IsContextReady);

        await SelectMatchWithContext(vm, 63);

        Assert.True(vm.IsContextReady);
    }

    private static MainViewModel CreateViewModel(out FakeImportService importService)
    {
        importService = new FakeImportService();
        return new MainViewModel(new FakePipeline(), new FakeFilePicker(), importService: importService);
    }

    private static async Task SelectMatchWithContext(MainViewModel vm, int matchId)
    {
        vm.SelectedMatchOption = new OcrMatchOption { MatchId = matchId, DisplayName = "m" };
        await vm.ActiveContextLoad!;
    }

    private sealed class FakeImportService : IOcrImportService
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
                    AwayTeam = new OcrContextTeam { TeamId = 2, Name = "Away", Players = [new OcrRosterPlayer { PlayerId = 2, TeamId = 2, FirstName = "C", LastName = "D", JerseyNumber = 5 }] }
                }
            });

        public Task<OcrImportResult> ImportAsync(int matchId, ImportPayload payload, bool allowTeamMismatch = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OcrImportResult { Success = true, MatchId = matchId });
    }

    private sealed class FakePipeline : IPdfProcessingPipeline
    {
        public Task<ProcessingResult> ProcessPdfAsync(string pdfPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProcessingResult());

        public Task<ProcessingResult> ProcessPdfAsync(string pdfPath, OcrRunSelection selection, OcrMatchContext? matchContext = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProcessingResult());
    }

    private sealed class FakeFilePicker : IFilePicker
    {
        public string? PickPdf() => null;
    }
}
