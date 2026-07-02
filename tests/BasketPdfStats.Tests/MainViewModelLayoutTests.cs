using BasketPdfStats.App.Services;
using BasketPdfStats.App.ViewModels;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Pipeline;
using BasketPdfStats.Core.Presentation;

namespace BasketPdfStats.Tests;

/// <summary>
/// Flusso post-elaborazione e layout: niente popup, auto-tab telecronaca, scheda PDF, import in alto,
/// nessun badge "Completato" inutile.
/// </summary>
public sealed class MainViewModelLayoutTests
{
    [Fact]
    public void No_result_popup_is_shown_after_processing()
    {
        var presenter = new SpyPresenter();
        var vm = Process(OkResult(), presenter);

        Assert.Equal(0, presenter.ShowCount);
    }

    [Fact]
    public void Valid_processing_switches_to_telecronaca_tab()
    {
        var vm = Process(OkResult());

        Assert.Equal(1, vm.SelectedTabIndex);
        Assert.NotNull(vm.Telecronaca);
    }

    [Fact]
    public void Mismatch_keeps_processing_tab_and_sets_header_status()
    {
        var result = new ProcessingResult
        {
            ProcessedFile = new ProcessedFile { FileName = "x.pdf", Status = FileProcessingStatus.CompletedWithWarnings },
            Teams =
            [
                new TeamStats { Side = "Home", Name = "Lakers" },
                new TeamStats { Side = "Away", Name = "Celtics" },
            ],
        };

        var vm = Process(result); // contesto stub: squadre "Home"/"Away" → mismatch

        Assert.Equal(0, vm.SelectedTabIndex);
        Assert.Equal("PDF non corrispondente al match", vm.HeaderStatus);
        Assert.False(vm.ImportToDbCommand.CanExecute(null));
    }

    [Fact]
    public void Header_status_is_empty_on_ok_result_no_green_badge()
    {
        var vm = Process(OkResult());

        Assert.Equal(string.Empty, vm.HeaderStatus);          // niente "Completato" in alto
        Assert.False(vm.Telecronaca!.HasImportantStatus);     // niente banner verde nella telecronaca
        Assert.Equal("Ok", vm.Telecronaca.StatusKind);
    }

    [Fact]
    public void Header_status_shows_review_required()
    {
        var result = OkResult();
        result.ProcessedFile.Status = FileProcessingStatus.CompletedWithReviewRequired;

        var vm = Process(result);

        Assert.Equal("Revisione richiesta", vm.HeaderStatus);
    }

    [Fact]
    public void Full_result_summary_is_populated_after_processing()
    {
        var vm = Process(OkResult());

        Assert.True(vm.HasFullResult);
        Assert.NotNull(vm.FullResult);
    }

    [Fact]
    public void Open_pdf_command_enabled_only_with_selected_pdf()
    {
        var vm = new MainViewModel(new StubPipeline(OkResult()), new StubFilePicker(), importService: new StubImportService());
        Assert.False(vm.OpenPdfCommand.CanExecute(null));

        vm.SelectedPdfPath = "C:\\tmp\\x.pdf";
        Assert.True(vm.OpenPdfCommand.CanExecute(null));
    }

    [Fact]
    public void Import_enabled_only_when_importable()
    {
        // Risultato ok + match selezionato → importabile.
        var vm = Process(OkResult());
        Assert.True(vm.ImportToDbCommand.CanExecute(null));

        // Errore bloccante → non importabile.
        var blocked = OkResult();
        blocked.ProcessedFile.Status = FileProcessingStatus.CompletedNotValidated;
        blocked.Validation = new ValidationResult { Warnings = [new ValidationWarning { RuleId = "x", Severity = "Error", Message = "boom" }] };
        var vm2 = Process(blocked);
        Assert.False(vm2.ImportToDbCommand.CanExecute(null));
    }

    private static ProcessingResult OkResult() => new()
    {
        ProcessedFile = new ProcessedFile { FileName = "x.pdf", Status = FileProcessingStatus.CompletedWithWarnings },
        Teams = [], // niente squadre OCR → nessun mismatch
    };

    private static MainViewModel Process(ProcessingResult result, IProcessingResultPresenter? presenter = null)
    {
        var vm = new MainViewModel(new StubPipeline(result), new StubFilePicker(),
            resultPresenter: presenter, importService: new StubImportService());
        vm.SelectedMatchOption = new OcrMatchOption { MatchId = 63, DisplayName = "m" };
        vm.ActiveContextLoad!.GetAwaiter().GetResult();
        vm.SelectedPdfPath = "C:\\tmp\\x.pdf";
        vm.ProcessPdfCommand.Execute(null);
        return vm;
    }

    private sealed class SpyPresenter : IProcessingResultPresenter
    {
        public int ShowCount { get; private set; }
        public void Show(ProcessingResult result) => ShowCount++;
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
