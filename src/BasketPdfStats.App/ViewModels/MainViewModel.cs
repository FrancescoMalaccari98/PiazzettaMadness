using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using BasketPdfStats.App.Services;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Identity;
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
    private readonly IIdentityReviewService? _identityReviewService;
    private readonly ImportPayloadBuilder _importPayloadBuilder = new();
    private bool _reviewResolved = true;
    private IReadOnlyDictionary<string, int>? _reviewOverrides;
    private string? _selectedPdfPath;
    private string _warningText = string.Empty;
    private string _outputJsonPath = string.Empty;
    private string _statusText = "Pronto";
    private string _resultWindowStatusText = string.Empty;
    private string _importStatusText = string.Empty;
    private string _matchIdText = string.Empty;
    private OcrMatchOption? _selectedMatchOption;
    private bool _isBusy;
    private bool _isLoadingMatches;
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Today);
    private CancellationTokenSource? _matchLoadCts;
    private Task? _activeMatchLoad;
    private OcrMatchContext? _selectedMatchContext;
    private Task? _activeContextLoad;
    private ProcessingResult? _lastResult;

    public MainViewModel(
        IPdfProcessingPipeline pipeline,
        IFilePicker filePicker,
        IProcessingResultPresenter? resultPresenter = null,
        AlreadyProcessedPdfSelectionService? alreadyProcessedPdfSelection = null,
        IOcrImportService? importService = null,
        ITeamMismatchConfirmationService? teamMismatchConfirmation = null,
        IIdentityReviewService? identityReviewService = null)
    {
        _pipeline = pipeline;
        _filePicker = filePicker;
        _resultPresentation = new ProcessingResultPresentationService(resultPresenter);
        _alreadyProcessedPdfSelection = alreadyProcessedPdfSelection;
        _importService = importService;
        _teamMismatchConfirmation = teamMismatchConfirmation;
        _identityReviewService = identityReviewService;
        SelectPdfCommand = new AsyncRelayCommand(SelectPdfAsync);
        ProcessPdfCommand = new AsyncRelayCommand(
            ProcessSelectedPdfAsync,
            () => !string.IsNullOrWhiteSpace(SelectedPdfPath) && !IsLoadingMatches && SelectedMatchContext is not null);
        LoadMatchesCommand = new AsyncRelayCommand(LoadMatchesForDateAsync, () => HasImportService);
        ReviewIdentityCommand = new AsyncRelayCommand(ReviewIdentityAsync, CanReviewIdentity);
        ImportToDbCommand = new AsyncRelayCommand(ImportToDbAsync, CanImport);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ProcessedFileRowViewModel> ProcessedFiles { get; } = [];
    public ObservableCollection<OcrMatchOption> MatchOptions { get; } = [];
    public AsyncRelayCommand SelectPdfCommand { get; }
    public AsyncRelayCommand ProcessPdfCommand { get; }
    public AsyncRelayCommand LoadMatchesCommand { get; }
    public AsyncRelayCommand ReviewIdentityCommand { get; }
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

    public bool IsLoadingMatches
    {
        get => _isLoadingMatches;
        private set
        {
            if (SetField(ref _isLoadingMatches, value))
            {
                ProcessPdfCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Data canonica selezionata per il lookup delle partite. Default: oggi.
    /// La modifica avvia un ricaricamento con cancellazione della richiesta precedente.
    /// </summary>
    public DateOnly SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetField(ref _selectedDate, value))
            {
                RaisePropertyChanged(nameof(SelectedDateValue));
                _activeMatchLoad = LoadMatchesForDateAsync();
            }
        }
    }

    /// <summary>
    /// Adattatore per il binding del DatePicker WPF (SelectedDate è DateTime?).
    /// </summary>
    public DateTime? SelectedDateValue
    {
        get => _selectedDate.ToDateTime(TimeOnly.MinValue);
        set
        {
            if (value is DateTime dateTime)
            {
                SelectedDate = DateOnly.FromDateTime(dateTime);
            }
        }
    }

    /// <summary>
    /// Task del caricamento partite in corso. Esposto per i test (await deterministico).
    /// </summary>
    public Task? ActiveMatchLoad => _activeMatchLoad;

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
            if (SetField(ref _selectedMatchOption, value))
            {
                if (value is not null)
                {
                    MatchIdText = value.MatchId.ToString(CultureInfo.InvariantCulture);
                    _activeContextLoad = LoadMatchContextAsync(value.MatchId);
                }
                else
                {
                    SelectedMatchContext = null;
                }
            }
        }
    }

    /// <summary>
    /// Contesto canonico della partita selezionata (squadre + roster dal DB).
    /// Null finché non è caricato: senza contesto l'elaborazione è bloccata.
    /// </summary>
    public OcrMatchContext? SelectedMatchContext
    {
        get => _selectedMatchContext;
        private set
        {
            if (SetField(ref _selectedMatchContext, value))
            {
                ProcessPdfCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Task del caricamento contesto in corso. Esposto per i test (await deterministico).
    /// </summary>
    public Task? ActiveContextLoad => _activeContextLoad;

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

        if (SelectedMatchContext is null)
        {
            WarningText = "Carica prima il contesto della partita dal database (seleziona data e partita).";
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

            var result = await _pipeline.ProcessPdfAsync(SelectedPdfPath, selection, SelectedMatchContext);
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
        // Se l'elaborazione richiede revisione, l'export resta bloccato finché non è risolta.
        _reviewResolved = result.ProcessedFile.Status != FileProcessingStatus.CompletedWithReviewRequired;
        _reviewOverrides = null;
        ProcessedFiles.Insert(0, new ProcessedFileRowViewModel(result.ProcessedFile));
        OutputJsonPath = result.ProcessedFile.OutputJsonPath ?? string.Empty;
        WarningText = BuildWarnings(result);
        ImportStatusText = _reviewResolved
            ? string.Empty
            : $"Revisione richiesta: {result.IdentityReview.Count(i => i.Reason is IdentityReviewReason.Conflict or IdentityReviewReason.NotInPdf)} conflitti obbligatori. Usa 'Revisiona'.";
        ReviewIdentityCommand.RaiseCanExecuteChanged();
        ImportToDbCommand.RaiseCanExecuteChanged();
        _resultPresentation.TryPresent(result, AppendResultViewerStatus);
    }

    private bool CanReviewIdentity() =>
        _identityReviewService is not null &&
        _lastResult is not null &&
        _lastResult.IdentityReview.Count > 0 &&
        SelectedMatchContext is not null;

    private Task ReviewIdentityAsync()
    {
        if (!CanReviewIdentity() || _lastResult is null || SelectedMatchContext is null || _identityReviewService is null)
        {
            return Task.CompletedTask;
        }

        var resolutions = _identityReviewService.ReviewAndConfirm(_lastResult.IdentityReview, SelectedMatchContext);
        if (resolutions is not null)
        {
            _reviewOverrides = resolutions;
            _reviewResolved = true;
            ImportStatusText = "Revisione completata: export sbloccato.";
        }
        else
        {
            ImportStatusText = "Revisione annullata: export ancora bloccato.";
        }

        ImportToDbCommand.RaiseCanExecuteChanged();
        return Task.CompletedTask;
    }

    public async Task LoadMatchesForDateAsync()
    {
        if (_importService is null)
        {
            ImportStatusText = "Import DB disattivato.";
            return;
        }

        // Cancella la richiesta precedente (cambio rapido data) e diventa la richiesta corrente.
        // La CTS non viene disposta esplicitamente: una richiesta successiva potrebbe ancora
        // referenziarla per cancellarla. Sono oggetti leggeri e di breve vita (GC).
        var cts = new CancellationTokenSource();
        var previousCts = _matchLoadCts;
        _matchLoadCts = cts;
        previousCts?.Cancel();

        var requestDate = _selectedDate;

        // Azzera la selezione: i dati precedenti non sono più validi per la nuova data.
        MatchOptions.Clear();
        SelectedMatchOption = null;
        MatchIdText = string.Empty;

        try
        {
            IsLoadingMatches = true;
            ImportStatusText = $"Carico partite del {requestDate:dd/MM/yyyy}...";
            var lookup = await _importService.GetMatchesForDateAsync(requestDate, cts.Token);

            // Stale-response guard: una richiesta più recente ha già preso il controllo.
            if (!ReferenceEquals(_matchLoadCts, cts))
            {
                return;
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

            ImportStatusText = MatchOptions.Count == 0
                ? $"Nessuna partita trovata per il {requestDate:dd/MM/yyyy}."
                : $"Partite caricate: {MatchOptions.Count} ({requestDate:dd/MM/yyyy}).";
        }
        catch (OperationCanceledException)
        {
            // Cancellazione volontaria: nessun errore mostrato all'utente.
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(_matchLoadCts, cts))
            {
                ImportStatusText = $"Errore partite: {ex.Message}";
            }
        }
        finally
        {
            if (ReferenceEquals(_matchLoadCts, cts))
            {
                IsLoadingMatches = false;
            }
        }
    }

    public async Task LoadMatchContextAsync(int matchId)
    {
        if (_importService is null)
        {
            return;
        }

        // Il contesto precedente non è più valido finché il nuovo non è caricato.
        SelectedMatchContext = null;

        OcrMatchContextResult result;
        try
        {
            ImportStatusText = $"Carico contesto partita (match_id={matchId})...";
            result = await _importService.GetMatchContextAsync(matchId);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            if (SelectedMatchOption?.MatchId == matchId)
            {
                ImportStatusText = $"Errore contesto: {ex.Message}";
            }
            return;
        }

        // La selezione potrebbe essere cambiata durante l'attesa: applica solo se ancora corrente.
        if (SelectedMatchOption?.MatchId != matchId)
        {
            return;
        }

        if (!result.Success || result.Context is null)
        {
            ImportStatusText = $"Errore contesto: {result.ErrorMessage}";
            return;
        }

        SelectedMatchContext = result.Context;
        var playerCount = result.Context.HomeTeam.Players.Count + result.Context.AwayTeam.Players.Count;
        ImportStatusText = $"Contesto caricato: {result.Context.HomeTeam.Name} vs {result.Context.AwayTeam.Name} ({playerCount} giocatori).";
    }

    private bool CanImport() =>
        _importService is not null &&
        _lastResult is not null &&
        int.TryParse(_matchIdText, out var id) && id > 0 &&
        _reviewResolved;

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

            if (SelectedMatchContext is null)
            {
                ImportStatusText = "Contesto partita non disponibile: impossibile costruire il payload canonico.";
                return;
            }

            ImportStatusText = $"Import in corso per {selectedMatchLabel}...";
            var payload = _importPayloadBuilder.Build(matchId, _lastResult, SelectedMatchContext, _reviewOverrides);
            var importResult = await _importService.ImportAsync(
                matchId,
                payload,
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

    private void RaisePropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

}
