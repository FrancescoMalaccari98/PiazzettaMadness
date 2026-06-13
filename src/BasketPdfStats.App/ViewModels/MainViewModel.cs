using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using BasketPdfStats.App.Services;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Pipeline;
using BasketPdfStats.Core.Presentation;

namespace BasketPdfStats.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IPdfProcessingPipeline _pipeline;
    private readonly IFilePicker _filePicker;
    private readonly ProcessingResultPresentationService _resultPresentation;
    private readonly AlreadyProcessedPdfSelectionService? _alreadyProcessedPdfSelection;
    private readonly IOcrImportService? _importService;
    private string? _selectedPdfPath;
    private string _warningText = string.Empty;
    private string _outputJsonPath = string.Empty;
    private string _statusText = "Pronto";
    private string _resultWindowStatusText = string.Empty;
    private string _importStatusText = string.Empty;
    private string _matchIdText = string.Empty;
    private bool _isBusy;
    private ProcessingResult? _lastResult;

    public MainViewModel(
        IPdfProcessingPipeline pipeline,
        IFilePicker filePicker,
        IProcessingResultPresenter? resultPresenter = null,
        AlreadyProcessedPdfSelectionService? alreadyProcessedPdfSelection = null,
        IOcrImportService? importService = null)
    {
        _pipeline = pipeline;
        _filePicker = filePicker;
        _resultPresentation = new ProcessingResultPresentationService(resultPresenter);
        _alreadyProcessedPdfSelection = alreadyProcessedPdfSelection;
        _importService = importService;
        SelectPdfCommand = new AsyncRelayCommand(SelectPdfAsync);
        ProcessPdfCommand = new AsyncRelayCommand(ProcessSelectedPdfAsync, () => !string.IsNullOrWhiteSpace(SelectedPdfPath));
        ImportToDbCommand = new AsyncRelayCommand(ImportToDbAsync, CanImport);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ProcessedFileRowViewModel> ProcessedFiles { get; } = [];
    public AsyncRelayCommand SelectPdfCommand { get; }
    public AsyncRelayCommand ProcessPdfCommand { get; }
    public AsyncRelayCommand ImportToDbCommand { get; }

    public string? SelectedPdfPath
    {
        get => _selectedPdfPath;
        set
        {
            if (SetField(ref _selectedPdfPath, value))
            {
                ProcessPdfCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string WarningText
    {
        get => _warningText;
        set => SetField(ref _warningText, value);
    }

    public string OutputJsonPath
    {
        get => _outputJsonPath;
        set => SetField(ref _outputJsonPath, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public string ResultWindowStatusText
    {
        get => _resultWindowStatusText;
        set => SetField(ref _resultWindowStatusText, value);
    }

    public string MatchIdText
    {
        get => _matchIdText;
        set
        {
            if (SetField(ref _matchIdText, value))
                ImportToDbCommand.RaiseCanExecuteChanged();
        }
    }

    public string ImportStatusText
    {
        get => _importStatusText;
        set => SetField(ref _importStatusText, value);
    }

    private Task SelectPdfAsync()
    {
        var pdf = _filePicker.PickPdf();
        if (!string.IsNullOrWhiteSpace(pdf))
        {
            SelectedPdfPath = pdf;
            StatusText = "PDF selezionato";
        }

        return Task.CompletedTask;
    }

    private async Task ProcessSelectedPdfAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedPdfPath))
        {
            WarningText = "Seleziona prima un PDF.";
            return;
        }

        if (!TryBuildSelection(out var selection))
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            StatusText = "Elaborazione PDF in corso...";
            if (!await ShouldProcessAsync(SelectedPdfPath))
            {
                return;
            }

            var result = await _pipeline.ProcessPdfAsync(SelectedPdfPath, selection);
            AddResult(result);
            StatusText = $"Completato: {result.ProcessedFile.Status}";
        });
    }

    private async Task<bool> ShouldProcessAsync(string pdfPath)
    {
        return _alreadyProcessedPdfSelection is null ||
            await _alreadyProcessedPdfSelection.ShouldProcessAsync(pdfPath, AppendResultViewerStatus);
    }

    private bool TryBuildSelection(out OcrRunSelection selection)
    {
        selection = new OcrRunSelection { UseTesseract = true };
        if (selection.TryValidate(out var error))
        {
            return true;
        }

        WarningText = error;
        StatusText = "Nessun OCR selezionato";
        return false;
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        try
        {
            IsBusy = true;
            WarningText = string.Empty;
            await action();
        }
        catch (Exception ex)
        {
            WarningText = ex.Message;
            StatusText = "Errore";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AddResult(ProcessingResult result)
    {
        _lastResult = result;
        ProcessedFiles.Insert(0, new ProcessedFileRowViewModel(result.ProcessedFile));
        OutputJsonPath = result.ProcessedFile.OutputJsonPath ?? string.Empty;
        WarningText = BuildWarnings(result);
        ImportStatusText = string.Empty;
        ImportToDbCommand.RaiseCanExecuteChanged();
        _resultPresentation.TryPresent(result, AppendResultViewerStatus);
    }

    private bool CanImport() =>
        _importService is not null &&
        _lastResult is not null &&
        int.TryParse(_matchIdText, out var id) && id > 0;

    private async Task ImportToDbAsync()
    {
        if (!CanImport() || _lastResult is null || _importService is null) return;
        if (!int.TryParse(_matchIdText, out var matchId) || matchId <= 0)
        {
            ImportStatusText = "Match ID non valido.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            ImportStatusText = $"Import in corso per match_id={matchId}...";
            var importResult = await _importService.ImportAsync(matchId, _lastResult);

            if (importResult.Success)
            {
                var warnings = importResult.Warnings.Count > 0
                    ? $" | Warning: {string.Join("; ", importResult.Warnings)}"
                    : string.Empty;
                ImportStatusText = $"Importati {importResult.ImportedPlayers} giocatori{warnings}";
            }
            else
            {
                ImportStatusText = $"Errore: {importResult.ErrorMessage}";
            }
        });
    }

    private void AppendResultViewerStatus(string message)
    {
        ResultWindowStatusText = string.IsNullOrWhiteSpace(ResultWindowStatusText)
            ? message
            : $"{ResultWindowStatusText}{Environment.NewLine}{message}";
        StatusText = message;
    }

    private static string BuildWarnings(ProcessingResult result)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(result.ProcessedFile.ErrorMessage))
        {
            lines.Add(result.ProcessedFile.ErrorMessage);
        }

        lines.AddRange(result.Validation.Warnings.Select(x => $"{x.Severity}: {x.Message}"));
        lines.AddRange(result.Stats.SelectMany(x => x.Warnings).Select(x => $"{x.Severity}: {x.Message}"));
        return string.Join(Environment.NewLine, lines);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

}
