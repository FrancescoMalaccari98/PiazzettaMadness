using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
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
    private readonly ITeamMismatchConfirmationService? _teamMismatchConfirmation;
    private string? _selectedPdfPath;
    private string _warningText = string.Empty;
    private string _outputJsonPath = string.Empty;
    private string _statusText = "Pronto";
    private string _resultWindowStatusText = string.Empty;
    private string _importStatusText = string.Empty;
    private string _matchIdText = string.Empty;
    private OcrMatchOption? _selectedMatchOption;
    private bool _isBusy;
    private ProcessingResult? _lastResult;

    public MainViewModel(
        IPdfProcessingPipeline pipeline,
        IFilePicker filePicker,
        IProcessingResultPresenter? resultPresenter = null,
        AlreadyProcessedPdfSelectionService? alreadyProcessedPdfSelection = null,
        IOcrImportService? importService = null,
        ITeamMismatchConfirmationService? teamMismatchConfirmation = null)
    {
        _pipeline = pipeline;
        _filePicker = filePicker;
        _resultPresentation = new ProcessingResultPresentationService(resultPresenter);
        _alreadyProcessedPdfSelection = alreadyProcessedPdfSelection;
        _importService = importService;
        _teamMismatchConfirmation = teamMismatchConfirmation;
        SelectPdfCommand = new AsyncRelayCommand(SelectPdfAsync);
        ProcessPdfCommand = new AsyncRelayCommand(ProcessSelectedPdfAsync, () => !string.IsNullOrWhiteSpace(SelectedPdfPath));
        LoadMatchesCommand = new AsyncRelayCommand(LoadTodayMatchesAsync, () => HasImportService);
        ImportToDbCommand = new AsyncRelayCommand(ImportToDbAsync, CanImport);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ProcessedFileRowViewModel> ProcessedFiles { get; } = [];
    public ObservableCollection<OcrMatchOption> MatchOptions { get; } = [];
    public AsyncRelayCommand SelectPdfCommand { get; }
    public AsyncRelayCommand ProcessPdfCommand { get; }
    public AsyncRelayCommand LoadMatchesCommand { get; }
    public AsyncRelayCommand ImportToDbCommand { get; }

    public bool HasImportService => _importService is not null;

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

    public OcrMatchOption? SelectedMatchOption
    {
        get => _selectedMatchOption;
        set
        {
            if (SetField(ref _selectedMatchOption, value) && value is not null)
            {
                MatchIdText = value.MatchId.ToString(CultureInfo.InvariantCulture);
            }
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
        selection = new OcrRunSelection { UseTesseract = true, UsePaddle = true };
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

    private async Task LoadTodayMatchesAsync()
    {
        if (_importService is null)
        {
            ImportStatusText = "Import DB disattivato.";
            return;
        }

        try
        {
            IsBusy = true;
            ImportStatusText = "Carico partite di oggi...";
            var previousSelectedMatchId = SelectedMatchOption?.MatchId;
            var lookup = await _importService.GetTodayMatchesAsync();

            MatchOptions.Clear();
            SelectedMatchOption = null;
            if (previousSelectedMatchId is int previousId &&
                string.Equals(_matchIdText, previousId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                MatchIdText = string.Empty;
            }

            if (!lookup.Success)
            {
                ImportStatusText = $"Errore partite: {lookup.ErrorMessage}";
                return;
            }

            foreach (var match in lookup.Matches)
            {
                MatchOptions.Add(match);
            }

            if (MatchOptions.Count == 1)
            {
                SelectedMatchOption = MatchOptions[0];
            }

            var gameDay = string.IsNullOrWhiteSpace(lookup.GameDay)
                ? string.Empty
                : $" ({lookup.GameDay})";
            ImportStatusText = MatchOptions.Count == 0
                ? $"Nessuna partita trovata{gameDay}."
                : $"Partite caricate: {MatchOptions.Count}{gameDay}.";
        }
        catch (Exception ex)
        {
            ImportStatusText = $"Errore partite: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
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
            var matchOption = FindMatchOption(matchId);
            var selectedMatchLabel = matchOption is not null
                ? matchOption.DisplayName
                : $"match_id={matchId}";
            var teamGuard = CheckTeamMismatchBeforeImport(matchOption, matchId);
            if (!teamGuard.ShouldImport)
            {
                return;
            }

            ImportStatusText = $"Import in corso per {selectedMatchLabel}...";
            var importResult = await _importService.ImportAsync(
                matchId,
                _lastResult,
                teamGuard.AllowTeamMismatch);

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

    private OcrMatchOption? FindMatchOption(int matchId) =>
        SelectedMatchOption?.MatchId == matchId
            ? SelectedMatchOption
            : MatchOptions.FirstOrDefault(match => match.MatchId == matchId);

    private TeamImportGuard CheckTeamMismatchBeforeImport(OcrMatchOption? matchOption, int matchId)
    {
        if (_lastResult is null)
        {
            return TeamImportGuard.Allow();
        }

        if (matchOption is null)
        {
            if (MatchOptions.Count > 0)
            {
                ImportStatusText = $"Import annullato: seleziona una partita da Match Teams per match_id={matchId}.";
                return TeamImportGuard.Block();
            }

            return TeamImportGuard.Allow();
        }

        var homeTeam = FindTeamBySide(_lastResult, "Home");
        var awayTeam = FindTeamBySide(_lastResult, "Away");
        var homeMatches = TeamMatches(homeTeam, matchOption.HomeTeam);
        var awayMatches = TeamMatches(awayTeam, matchOption.AwayTeam);

        if (homeMatches && awayMatches)
        {
            return TeamImportGuard.Allow();
        }

        var warning = new TeamMismatchWarning(
            PdfHomeTeam: FormatTeamName(homeTeam),
            PdfAwayTeam: FormatTeamName(awayTeam),
            MatchHomeTeam: FormatMatchTeamName(matchOption.HomeTeam),
            MatchAwayTeam: FormatMatchTeamName(matchOption.AwayTeam));

        if (_teamMismatchConfirmation is null)
        {
            ImportStatusText =
                $"Import bloccato: squadre non corrispondenti. PDF: {warning.PdfHomeTeam} vs {warning.PdfAwayTeam}. " +
                $"Match Teams: {warning.MatchHomeTeam} vs {warning.MatchAwayTeam}.";
            return TeamImportGuard.Block();
        }

        if (_teamMismatchConfirmation.ShouldContinue(warning))
        {
            ImportStatusText = "Import confermato con squadre non corrispondenti.";
            return TeamImportGuard.AllowConfirmedMismatch();
        }

        ImportStatusText = "Import annullato: squadre non corrispondenti.";
        return TeamImportGuard.Block();
    }

    private readonly record struct TeamImportGuard(bool ShouldImport, bool AllowTeamMismatch)
    {
        public static TeamImportGuard Allow() => new(ShouldImport: true, AllowTeamMismatch: false);
        public static TeamImportGuard AllowConfirmedMismatch() => new(ShouldImport: true, AllowTeamMismatch: true);
        public static TeamImportGuard Block() => new(ShouldImport: false, AllowTeamMismatch: false);
    }

    private static TeamStats? FindTeamBySide(ProcessingResult result, string side) =>
        result.Teams.FirstOrDefault(team => string.Equals(team.Side, side, StringComparison.OrdinalIgnoreCase));

    private static bool TeamMatches(TeamStats? pdfTeam, OcrMatchTeam selectedTeam)
    {
        var pdfNames = new[]
            {
                pdfTeam?.Name,
                pdfTeam?.Abbreviation
            }
            .Select(NormalizeTeamName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        var selectedNames = new[]
            {
                selectedTeam.Name,
                selectedTeam.ShortName
            }
            .Select(NormalizeTeamName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        return pdfNames.Length > 0 &&
            selectedNames.Length > 0 &&
            pdfNames.Any(pdfName => selectedNames.Contains(pdfName, StringComparer.Ordinal));
    }

    private static string NormalizeTeamName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var normalized = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                normalized.Append(char.ToLowerInvariant(character));
            }
        }

        return normalized.ToString();
    }

    private static string FormatTeamName(TeamStats? team)
    {
        if (team is null)
        {
            return "non letta";
        }

        var name = string.IsNullOrWhiteSpace(team.Name) ? "non letta" : team.Name!;
        return string.IsNullOrWhiteSpace(team.Abbreviation)
            ? name
            : $"{name} ({team.Abbreviation})";
    }

    private static string FormatMatchTeamName(OcrMatchTeam team)
    {
        return string.IsNullOrWhiteSpace(team.ShortName)
            ? team.Name
            : $"{team.Name} ({team.ShortName})";
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

        lines.AddRange(result.OcrRuns
            .Where(x => !string.IsNullOrWhiteSpace(x.Error))
            .Select(x => $"{x.Engine}: {x.Status} - {TruncateWarning(x.Error!)}"));
        lines.AddRange(result.Validation.Warnings.Select(x => $"{x.Severity}: {x.Message}"));
        lines.AddRange(result.Stats.SelectMany(x => x.Warnings).Select(x => $"{x.Severity}: {x.Message}"));
        return string.Join(Environment.NewLine, lines);
    }

    private static string TruncateWarning(string value)
    {
        const int maxLength = 600;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
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
