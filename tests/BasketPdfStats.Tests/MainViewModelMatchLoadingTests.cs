using BasketPdfStats.App.Services;
using BasketPdfStats.App.ViewModels;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Pipeline;

namespace BasketPdfStats.Tests;

public sealed class MainViewModelMatchLoadingTests
{
    [Fact]
    public void SelectedDate_default_is_today()
    {
        var vm = CreateViewModel(out _, out _);

        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), vm.SelectedDate);
        Assert.Null(vm.ActiveMatchLoad);
    }

    [Fact]
    public async Task LoadMatchesForDateAsync_queries_with_selected_date()
    {
        var vm = CreateViewModel(out var importService, out _);
        var date = new DateOnly(2026, 7, 8);

        vm.SelectedDate = date;
        await vm.ActiveMatchLoad!;

        Assert.Equal(date, Assert.Single(importService.RequestedDates));
    }

    [Fact]
    public async Task LoadMatchesForDateAsync_shows_no_matches_message_on_empty_list()
    {
        var vm = CreateViewModel(out var importService, out _);
        importService.DefaultResult = _ => new OcrMatchLookupResult { Success = true };

        await vm.LoadMatchesForDateAsync();

        Assert.Empty(vm.MatchOptions);
        Assert.Contains("Nessuna partita trovata", vm.ImportStatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadMatchesForDateAsync_shows_error_on_api_failure()
    {
        var vm = CreateViewModel(out var importService, out _);
        importService.DefaultResult = _ => new OcrMatchLookupResult
        {
            Success = false,
            ErrorMessage = "Internal database error"
        };

        await vm.LoadMatchesForDateAsync();

        Assert.Empty(vm.MatchOptions);
        Assert.Contains("Errore partite", vm.ImportStatusText, StringComparison.Ordinal);
        Assert.Contains("Internal database error", vm.ImportStatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Changing_date_triggers_match_reload()
    {
        var vm = CreateViewModel(out var importService, out _);

        vm.SelectedDate = new DateOnly(2026, 7, 9);
        await vm.ActiveMatchLoad!;

        Assert.Contains(new DateOnly(2026, 7, 9), importService.RequestedDates);
    }

    [Fact]
    public async Task Changing_date_clears_selected_match()
    {
        var vm = CreateViewModel(out var importService, out _);
        importService.DefaultResult = _ => new OcrMatchLookupResult
        {
            Success = true,
            Matches = [Match(10, "A vs B")]
        };

        // Prima data: una sola partita → selezione automatica.
        vm.SelectedDate = new DateOnly(2026, 7, 8);
        await vm.ActiveMatchLoad!;
        Assert.NotNull(vm.SelectedMatchOption);

        // Cambio data con risposta vuota: la selezione precedente va azzerata.
        importService.DefaultResult = _ => new OcrMatchLookupResult { Success = true };
        vm.SelectedDate = new DateOnly(2026, 7, 9);
        await vm.ActiveMatchLoad!;

        Assert.Null(vm.SelectedMatchOption);
        Assert.Empty(vm.MatchOptions);
        Assert.Equal(string.Empty, vm.MatchIdText);
    }

    [Fact]
    public async Task Rapid_date_change_cancels_previous_request()
    {
        var vm = CreateViewModel(out var importService, out _);
        var gate1 = importService.EnqueueGate();
        var gate2 = importService.EnqueueGate();

        vm.SelectedDate = new DateOnly(2026, 7, 8);
        var firstLoad = vm.ActiveMatchLoad!;

        vm.SelectedDate = new DateOnly(2026, 7, 9);
        var secondLoad = vm.ActiveMatchLoad!;

        // Il token della prima richiesta è stato cancellato dal secondo cambio data.
        Assert.True(importService.ReceivedTokens[0].IsCancellationRequested);

        await firstLoad; // termina silenziosamente (cancellata)

        gate2.SetResult(new OcrMatchLookupResult { Success = true, Matches = [Match(20, "C vs D")] });
        await secondLoad;

        Assert.Single(vm.MatchOptions);
        Assert.DoesNotContain("Errore", vm.ImportStatusText, StringComparison.Ordinal);

        gate1.TrySetCanceled();
    }

    [Fact]
    public async Task Stale_response_is_ignored_when_superseded()
    {
        var vm = CreateViewModel(out var importService, out _);
        importService.HonorCancellation = false; // il servizio risponde anche se cancellato
        var gate1 = importService.EnqueueGate();
        var gate2 = importService.EnqueueGate();

        vm.SelectedDate = new DateOnly(2026, 7, 8);
        var firstLoad = vm.ActiveMatchLoad!;

        vm.SelectedDate = new DateOnly(2026, 7, 9);
        var secondLoad = vm.ActiveMatchLoad!;

        // La prima risposta arriva tardi: deve essere ignorata (stale guard).
        gate1.SetResult(new OcrMatchLookupResult { Success = true, Matches = [Match(1, "stale")] });
        await firstLoad;
        Assert.Empty(vm.MatchOptions);

        // La seconda risposta (corrente) viene applicata.
        gate2.SetResult(new OcrMatchLookupResult
        {
            Success = true,
            Matches = [Match(2, "fresh-1"), Match(3, "fresh-2")]
        });
        await secondLoad;

        Assert.Equal(2, vm.MatchOptions.Count);
    }

    [Fact]
    public async Task IsLoadingMatches_is_true_during_load_and_false_after()
    {
        var vm = CreateViewModel(out var importService, out _);
        var gate = importService.EnqueueGate();

        var load = vm.LoadMatchesForDateAsync();
        Assert.True(vm.IsLoadingMatches);

        gate.SetResult(new OcrMatchLookupResult { Success = true });
        await load;

        Assert.False(vm.IsLoadingMatches);
    }

    [Fact]
    public async Task ProcessPdf_command_disabled_while_loading_matches()
    {
        var vm = CreateViewModel(out var importService, out _);
        await SelectMatchWithContext(vm, 7);
        vm.SelectedPdfPath = "C:\\tmp\\sample.pdf";
        Assert.True(vm.ProcessPdfCommand.CanExecute(null));

        var gate = importService.EnqueueGate();
        var load = vm.LoadMatchesForDateAsync();

        Assert.True(vm.IsLoadingMatches);
        Assert.False(vm.ProcessPdfCommand.CanExecute(null));

        gate.SetResult(new OcrMatchLookupResult { Success = true });
        await load;

        Assert.False(vm.IsLoadingMatches);
    }

    [Fact]
    public async Task Selecting_match_loads_context()
    {
        var vm = CreateViewModel(out var importService, out _);

        vm.SelectedMatchOption = new OcrMatchOption { MatchId = 42, DisplayName = "x" };
        await vm.ActiveContextLoad!;

        Assert.Equal(42, Assert.Single(importService.RequestedContextMatchIds));
        Assert.NotNull(vm.SelectedMatchContext);
        Assert.Equal(42, vm.SelectedMatchContext!.MatchId);
    }

    [Fact]
    public void ProcessPdf_command_disabled_without_context()
    {
        var vm = CreateViewModel(out _, out _);
        vm.SelectedPdfPath = "C:\\tmp\\sample.pdf";

        Assert.Null(vm.SelectedMatchContext);
        Assert.False(vm.ProcessPdfCommand.CanExecute(null));
    }

    [Fact]
    public async Task ProcessPdf_command_enabled_after_context_loaded()
    {
        var vm = CreateViewModel(out _, out _);
        vm.SelectedPdfPath = "C:\\tmp\\sample.pdf";
        Assert.False(vm.ProcessPdfCommand.CanExecute(null));

        await SelectMatchWithContext(vm, 7);

        Assert.NotNull(vm.SelectedMatchContext);
        Assert.True(vm.ProcessPdfCommand.CanExecute(null));
    }

    [Fact]
    public async Task Context_error_keeps_process_disabled()
    {
        var vm = CreateViewModel(out var importService, out _);
        importService.ContextResult = _ => new OcrMatchContextResult
        {
            Success = false,
            ErrorMessage = "Roster incompleto per la partita."
        };
        vm.SelectedPdfPath = "C:\\tmp\\sample.pdf";

        vm.SelectedMatchOption = new OcrMatchOption { MatchId = 7, DisplayName = "x" };
        await vm.ActiveContextLoad!;

        Assert.Null(vm.SelectedMatchContext);
        Assert.False(vm.ProcessPdfCommand.CanExecute(null));
        Assert.Contains("Errore contesto", vm.ImportStatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Clearing_match_clears_context()
    {
        var vm = CreateViewModel(out _, out _);
        await SelectMatchWithContext(vm, 7);
        Assert.NotNull(vm.SelectedMatchContext);

        vm.SelectedMatchOption = null;

        Assert.Null(vm.SelectedMatchContext);
    }

    [Fact]
    public async Task Refresh_command_reloads_current_selected_date()
    {
        var vm = CreateViewModel(out var importService, out _);
        vm.SelectedDate = new DateOnly(2026, 7, 8);
        await vm.ActiveMatchLoad!;
        importService.RequestedDates.Clear();

        // Il pulsante "Aggiorna" è abilitato solo con import service presente.
        Assert.True(vm.LoadMatchesCommand.CanExecute(null));

        // Ricarica la stessa data selezionata.
        await vm.LoadMatchesForDateAsync();

        Assert.Equal(new DateOnly(2026, 7, 8), Assert.Single(importService.RequestedDates));
    }

    [Fact]
    public void Refresh_command_disabled_without_import_service()
    {
        var pipeline = new FakePipeline();
        var vm = new MainViewModel(pipeline, new FakeFilePicker());

        Assert.False(vm.LoadMatchesCommand.CanExecute(null));
    }

    [Fact]
    public async Task Loading_matches_does_not_invoke_ocr_pipeline()
    {
        var vm = CreateViewModel(out _, out var pipeline);

        await vm.LoadMatchesForDateAsync();

        Assert.Equal(0, pipeline.ProcessCalls);
    }

    private static MainViewModel CreateViewModel(out FakeImportService importService, out FakePipeline pipeline)
    {
        importService = new FakeImportService();
        pipeline = new FakePipeline();
        return new MainViewModel(pipeline, new FakeFilePicker(), importService: importService);
    }

    private static OcrMatchOption Match(int id, string name) =>
        new() { MatchId = id, DisplayName = name };

    private static async Task SelectMatchWithContext(MainViewModel vm, int matchId)
    {
        vm.SelectedMatchOption = new OcrMatchOption { MatchId = matchId, DisplayName = "m" };
        await vm.ActiveContextLoad!;
    }

    private sealed class FakeImportService : IOcrImportService
    {
        private readonly Queue<TaskCompletionSource<OcrMatchLookupResult>> _gates = new();

        public List<DateOnly> RequestedDates { get; } = [];
        public List<CancellationToken> ReceivedTokens { get; } = [];
        public List<int> RequestedContextMatchIds { get; } = [];
        public bool HonorCancellation { get; set; } = true;
        public Func<DateOnly, OcrMatchLookupResult> DefaultResult { get; set; } =
            _ => new OcrMatchLookupResult { Success = true };
        public Func<int, OcrMatchContextResult> ContextResult { get; set; } =
            matchId => new OcrMatchContextResult { Success = true, Context = ValidContext(matchId) };

        public TaskCompletionSource<OcrMatchLookupResult> EnqueueGate()
        {
            var tcs = new TaskCompletionSource<OcrMatchLookupResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _gates.Enqueue(tcs);
            return tcs;
        }

        public async Task<OcrMatchLookupResult> GetMatchesForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
        {
            RequestedDates.Add(date);
            ReceivedTokens.Add(cancellationToken);

            if (_gates.Count > 0)
            {
                var tcs = _gates.Dequeue();
                return HonorCancellation
                    ? await tcs.Task.WaitAsync(cancellationToken)
                    : await tcs.Task;
            }

            return DefaultResult(date);
        }

        public Task<OcrMatchContextResult> GetMatchContextAsync(int matchId, CancellationToken cancellationToken = default)
        {
            RequestedContextMatchIds.Add(matchId);
            return Task.FromResult(ContextResult(matchId));
        }

        public Task<OcrImportResult> ImportAsync(
            int matchId, ImportPayload payload, bool allowTeamMismatch = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new OcrImportResult { Success = true, MatchId = matchId });

        public static OcrMatchContext ValidContext(int matchId) => new()
        {
            MatchId = matchId,
            HomeTeam = new OcrContextTeam
            {
                TeamId = 1,
                Name = "Home",
                Players = [new OcrRosterPlayer { PlayerId = 11, TeamId = 1, FirstName = "A", LastName = "Rossi", JerseyNumber = 4 }],
            },
            AwayTeam = new OcrContextTeam
            {
                TeamId = 2,
                Name = "Away",
                Players = [new OcrRosterPlayer { PlayerId = 21, TeamId = 2, FirstName = "B", LastName = "Bianchi", JerseyNumber = 5 }],
            },
        };
    }

    private sealed class FakePipeline : IPdfProcessingPipeline
    {
        public int ProcessCalls { get; private set; }

        public Task<ProcessingResult> ProcessPdfAsync(string pdfPath, CancellationToken cancellationToken = default) =>
            ProcessPdfAsync(pdfPath, new OcrRunSelection(), null, cancellationToken);

        public Task<ProcessingResult> ProcessPdfAsync(
            string pdfPath,
            OcrRunSelection selection,
            OcrMatchContext? matchContext = null,
            CancellationToken cancellationToken = default)
        {
            ProcessCalls++;
            return Task.FromResult(new ProcessingResult());
        }
    }

    private sealed class FakeFilePicker : IFilePicker
    {
        public string? PickPdf() => null;
    }
}
