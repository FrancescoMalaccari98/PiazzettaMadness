using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
    private readonly AlreadyProcessedPdfSelectionService? _alreadyProcessedPdfSelection;
    private readonly IOcrImportService? _importService;
    private readonly ITeamMismatchConfirmationService? _teamMismatchConfirmation;
    private readonly IIdentityReviewService? _identityReviewService;
    private readonly ImportPayloadBuilder _importPayloadBuilder = new();
    private readonly TeamIdentityMatcher _teamIdentityMatcher = new();
    private bool _reviewResolved = true;
    // Messaggio di blocco quando le squadre del PDF non corrispondono al match selezionato.
    // null = nessun mismatch. Quando valorizzato, import e revisione sono bloccati.
    private string? _teamMismatchMessage;
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
    private TelecronacaViewModel? _telecronaca;
    private ProcessingResultViewModel? _fullResult;
    private int _selectedTabIndex;

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
        _ = resultPresenter; // popup post-elaborazione disattivata: i risultati restano nell'interfaccia.
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
        OpenPdfCommand = new AsyncRelayCommand(OpenSelectedPdfAsync, () => HasSelectedPdf);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ProcessedFileRowViewModel> ProcessedFiles { get; } = [];
    public ObservableCollection<OcrMatchOption> MatchOptions { get; } = [];
    public AsyncRelayCommand SelectPdfCommand { get; }
    public AsyncRelayCommand ProcessPdfCommand { get; }
    public AsyncRelayCommand LoadMatchesCommand { get; }
    public AsyncRelayCommand ReviewIdentityCommand { get; }
    public AsyncRelayCommand ImportToDbCommand { get; }
    public AsyncRelayCommand OpenPdfCommand { get; }

    public bool HasImportService => _importService is not null;

    /// <summary>
    /// Stato importante per l'intestazione: vuoto quando tutto è ok (niente badge "Completato"),
    /// altrimenti il motivo che richiede attenzione. Mostrato in alto, compatto.
    /// </summary>
    public string HeaderStatus
    {
        get
        {
            if (_teamMismatchMessage is not null)
            {
                return "PDF non corrispondente al match";
            }

            if (_lastResult is null)
            {
                return string.Empty;
            }

            if (_lastResult.ProcessedFile.Status == FileProcessingStatus.CompletedWithReviewRequired)
            {
                return "Revisione richiesta";
            }

            return HasBlockingError() ? "Non importabile" : string.Empty;
        }
    }

    /// <summary>
    /// Avviso mostrato quando il backend non è configurato: senza <c>ocrApi.baseUrl</c>/<c>token</c>
    /// in <c>Config/appsettings.json</c> non si possono caricare le partite né importare. La data resta
    /// comunque selezionabile. Stringa vuota quando il backend è configurato.
    /// </summary>
    public string BackendStatusHint => HasImportService
        ? string.Empty
        : "Backend non configurato: imposta ocrApi.baseUrl e token in Config/appsettings.json per caricare le partite e importare nel DB.";

    public string? SelectedPdfPath
    {
        get => _selectedPdfPath;
        set
        {
            if (SetField(ref _selectedPdfPath, value))
            {
                ProcessPdfCommand.RaiseCanExecuteChanged();
                OpenPdfCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(HasSelectedPdf));
                RaisePropertyChanged(nameof(ProcessHint));
            }
        }
    }

    /// <summary>True quando un PDF è stato selezionato (per la UI a step).</summary>
    public bool HasSelectedPdf => !string.IsNullOrWhiteSpace(_selectedPdfPath);

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
        set
        {
            if (SetField(ref _isBusy, value))
            {
                RaiseWorkflowChanged();
            }
        }
    }

    public bool IsLoadingMatches
    {
        get => _isLoadingMatches;
        private set
        {
            if (SetField(ref _isLoadingMatches, value))
            {
                ProcessPdfCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(WorkflowStatus));
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

                RaiseMatchInfoChanged();
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
                RaisePropertyChanged(nameof(IsContextReady));
                RaiseWorkflowChanged();
            }
        }
    }

    /// <summary>True quando il contesto canonico della partita è caricato (passo 1 completo).</summary>
    public bool IsContextReady => _selectedMatchContext is not null;

    /// <summary>
    /// Task del caricamento contesto in corso. Esposto per i test (await deterministico).
    /// </summary>
    public Task? ActiveContextLoad => _activeContextLoad;

    public string ImportStatusText
    {
        get => _importStatusText;
        set => SetField(ref _importStatusText, value);
    }

    /// <summary>
    /// Vista "Risultati Telecronaca" dell'ultima elaborazione (dati finali). Null finché non si elabora.
    /// </summary>
    public TelecronacaViewModel? Telecronaca
    {
        get => _telecronaca;
        private set
        {
            if (SetField(ref _telecronaca, value))
            {
                RaisePropertyChanged(nameof(HasTelecronaca));
            }
        }
    }

    public bool HasTelecronaca => _telecronaca is not null;

    /// <summary>
    /// Riepilogo schematico di tutto ciò che è stato estratto dal PDF (scheda "PDF completo").
    /// Null finché non si elabora.
    /// </summary>
    public ProcessingResultViewModel? FullResult
    {
        get => _fullResult;
        private set
        {
            if (SetField(ref _fullResult, value))
            {
                RaisePropertyChanged(nameof(HasFullResult));
            }
        }
    }

    public bool HasFullResult => _fullResult is not null;

    /// <summary>Tab attivo: 0 = Elaborazione, 1 = Risultati Telecronaca. Dopo l'elaborazione passa a 1.</summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetField(ref _selectedTabIndex, value);
    }

    /// <summary>True quando una partita è selezionata: alimenta la visibilità del blocco info match.</summary>
    public bool HasSelectedMatch => _selectedMatchOption is not null;

    /// <summary>Squadre della partita selezionata ("Casa vs Ospite"), per il riepilogo a video.</summary>
    public string SelectedMatchTeams
    {
        get
        {
            var match = _selectedMatchOption;
            if (match is null)
            {
                return string.Empty;
            }

            var home = string.IsNullOrWhiteSpace(match.HomeTeam.Name) ? null : match.HomeTeam.Name;
            var away = string.IsNullOrWhiteSpace(match.AwayTeam.Name) ? null : match.AwayTeam.Name;
            if (home is null && away is null)
            {
                return match.MatchName ?? match.DisplayName;
            }

            return $"{home ?? "?"} vs {away ?? "?"}";
        }
    }

    /// <summary>Orario di inizio programmato della partita selezionata, formattato se possibile.</summary>
    public string SelectedMatchTime
    {
        get
        {
            var raw = _selectedMatchOption?.ScheduledStartAt;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? parsed.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                : raw;
        }
    }

    /// <summary>
    /// Fase/turno della partita selezionata (es. "GroupStage — Giornata 1"). Il girone non è esposto
    /// dall'endpoint di lookup: si mostra ciò che è disponibile, senza inventare.
    /// </summary>
    public string SelectedMatchPhase
    {
        get
        {
            var match = _selectedMatchOption;
            if (match is null)
            {
                return string.Empty;
            }

            var parts = new[] { match.Phase, match.Round }
                .Where(part => !string.IsNullOrWhiteSpace(part));
            return string.Join(" — ", parts);
        }
    }

    /// <summary>
    /// Stato generale del flusso per l'intestazione: Pronto / Caricamento match... / Elaborazione... /
    /// Completato / Errore / Revisione richiesta.
    /// </summary>
    public string WorkflowStatus
    {
        get
        {
            if (_isLoadingMatches)
            {
                return "Caricamento match...";
            }

            if (_isBusy)
            {
                return "Elaborazione...";
            }

            if (_teamMismatchMessage is not null)
            {
                return "PDF non corrispondente";
            }

            if (_lastResult is null)
            {
                return "Pronto";
            }

            return _lastResult.ProcessedFile.Status switch
            {
                FileProcessingStatus.CompletedWithReviewRequired => "Revisione richiesta",
                FileProcessingStatus.Failed => "Errore",
                _ => "Completato"
            };
        }
    }

    /// <summary>Suggerimento contestuale sul perché "Elabora PDF" non è ancora disponibile.</summary>
    public string ProcessHint
    {
        get
        {
            if (_isBusy)
            {
                return "Elaborazione in corso...";
            }

            if (_selectedMatchContext is null)
            {
                return "Seleziona prima una partita (passo 1).";
            }

            if (string.IsNullOrWhiteSpace(_selectedPdfPath))
            {
                return "Seleziona un PDF (passo 2).";
            }

            return "Pronto per elaborare.";
        }
    }

    /// <summary>
    /// Riepilogo sintetico dell'ultima elaborazione: solo conteggi importanti (errori bloccanti,
    /// revisioni richieste, avvisi matematici), invece di centinaia di warning tecnici.
    /// </summary>
    public string ProcessingSummary
    {
        get
        {
            if (_lastResult is null)
            {
                return string.Empty;
            }

            var warnings = _lastResult.Validation.Warnings;
            var blocking =
                (_teamMismatchMessage is not null ? 1 : 0) +
                warnings.Count(w => string.Equals(w.Severity, "Error", StringComparison.OrdinalIgnoreCase));
            if (_lastResult.ProcessedFile.Status == FileProcessingStatus.Failed)
            {
                blocking = Math.Max(blocking, 1);
            }

            var reviews = _lastResult.IdentityReview.Count(i => i.Reason is IdentityReviewReason.Conflict or IdentityReviewReason.NotInPdf);
            var math = warnings.Count(w => w.RuleId.StartsWith("math.", StringComparison.OrdinalIgnoreCase));

            var header = _teamMismatchMessage is not null
                ? "Elaborazione completata (PDF non corrispondente al match)."
                : "Elaborazione completata.";
            return string.Join(Environment.NewLine,
                header,
                $"{blocking} errori bloccanti.",
                $"{reviews} revisioni richieste.",
                $"{math} avvisi matematici.");
        }
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

    /// <summary>Apre il PDF selezionato con l'applicazione predefinita di Windows (scheda "PDF completo").</summary>
    private Task OpenSelectedPdfAsync()
    {
        var path = _selectedPdfPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            ImportStatusText = "PDF non disponibile da aprire.";
            return Task.CompletedTask;
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ImportStatusText = $"Impossibile aprire il PDF: {ex.Message}";
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
            // In caso di mismatch PDF↔match, AddResult ha già impostato lo stato di blocco: non sovrascriverlo
            // con "Completato" (sarebbe fuorviante — l'import è bloccato).
            StatusText = _teamMismatchMessage is not null
                ? "PDF NON corrispondente al match: import bloccato."
                : $"Completato: {result.ProcessedFile.Status}";
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
        // Controllo bloccante: le squadre lette dal PDF devono corrispondere al match selezionato.
        _teamMismatchMessage = DetectTeamMismatch(result);
        ProcessedFiles.Insert(0, new ProcessedFileRowViewModel(result.ProcessedFile));
        OutputJsonPath = result.ProcessedFile.OutputJsonPath ?? string.Empty;

        if (_teamMismatchMessage is not null)
        {
            // Mismatch: errore in evidenza, import/revisione bloccati (CanImport/CanReviewIdentity).
            WarningText = _teamMismatchMessage + Environment.NewLine + Environment.NewLine + BuildWarnings(result);
            ImportStatusText = _teamMismatchMessage;
            StatusText = "PDF non corrispondente al match selezionato";
        }
        else
        {
            WarningText = BuildWarnings(result);
            ImportStatusText = _reviewResolved
                ? string.Empty
                : $"Revisione richiesta: {result.IdentityReview.Count(i => i.Reason is IdentityReviewReason.Conflict or IdentityReviewReason.NotInPdf)} conflitti obbligatori. Usa 'Revisiona'.";
        }

        ReviewIdentityCommand.RaiseCanExecuteChanged();
        ImportToDbCommand.RaiseCanExecuteChanged();
        RaiseWorkflowChanged();
        RaisePropertyChanged(nameof(ProcessingSummary));

        // Vista telecronaca dai dati finali. Su mismatch mostra solo l'errore (niente tabelle fuorvianti).
        var importable = _teamMismatchMessage is null && _reviewResolved && !HasBlockingError();
        Telecronaca = new TelecronacaViewModel(result, SelectedMatchOption, _teamMismatchMessage, importable);
        FullResult = new ProcessingResultViewModel(result);

        // Nessuna popup: i risultati restano nell'interfaccia principale.
        // Su mismatch resta sulla scheda "Elaborazione" (errore chiaro); altrimenti va alla telecronaca.
        SelectedTabIndex = _teamMismatchMessage is not null ? 0 : 1;
    }

    /// <summary>
    /// Confronta le squadre lette dall'OCR con quelle del match selezionato (contesto DB).
    /// Ritorna un messaggio di errore se NON corrispondono (mismatch bloccante), altrimenti null.
    /// Home/Away invertiti NON sono un errore (gestiti come side inversion). Se l'OCR non ha letto
    /// alcun nome squadra non si afferma un mismatch (si evita un falso positivo).
    /// </summary>
    private string? DetectTeamMismatch(ProcessingResult result)
    {
        if (SelectedMatchContext is null || result.ProcessedFile.Status == FileProcessingStatus.Failed)
        {
            return null;
        }

        var homeOcr = FindTeamBySide(result, "Home")?.Name;
        var awayOcr = FindTeamBySide(result, "Away")?.Name;
        if (string.IsNullOrWhiteSpace(homeOcr) && string.IsNullOrWhiteSpace(awayOcr))
        {
            return null;
        }

        var match = _teamIdentityMatcher.Match(homeOcr, awayOcr, SelectedMatchContext);
        if (match.Outcome != TeamMatchOutcome.Mismatch)
        {
            return null; // CorrectOrder o Inverted → corrispondenza valida.
        }

        var selected = $"{SelectedMatchContext.HomeTeam.Name} vs {SelectedMatchContext.AwayTeam.Name}";
        var pdf = FormatOcrTeamPair(homeOcr, awayOcr);
        return "Il PDF selezionato non corrisponde al match scelto." + Environment.NewLine +
               $"Match selezionato: {selected}" + Environment.NewLine +
               $"PDF rilevato: {pdf}" + Environment.NewLine +
               "Seleziona il match corretto o scegli un altro PDF.";
    }

    private static string FormatOcrTeamPair(string? home, string? away)
    {
        var h = string.IsNullOrWhiteSpace(home) ? "?" : home!;
        var a = string.IsNullOrWhiteSpace(away) ? "?" : away!;
        return h == "?" && a == "?" ? "(squadre non leggibili dal PDF)" : $"{h} vs {a}";
    }

    private bool CanReviewIdentity() =>
        _identityReviewService is not null &&
        _lastResult is not null &&
        _lastResult.IdentityReview.Count > 0 &&
        SelectedMatchContext is not null &&
        _teamMismatchMessage is null; // PDF non corrispondente: niente revisione (match sbagliato).

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
        _reviewResolved &&
        _teamMismatchMessage is null && // PDF non corrispondente al match: import bloccato.
        !HasBlockingError(); // errore OCR grave / non validato / errore: import bloccato.

    /// <summary>
    /// Errore bloccante che impedisce l'import: elaborazione fallita/non validata o un warning di
    /// severità Error. I warning non bloccanti (math.*, info) NON bloccano l'import.
    /// </summary>
    private bool HasBlockingError() =>
        _lastResult is not null &&
        (_lastResult.ProcessedFile.Status is FileProcessingStatus.Failed or FileProcessingStatus.CompletedNotValidated ||
         _lastResult.Validation.Warnings.Any(w => string.Equals(w.Severity, "Error", StringComparison.OrdinalIgnoreCase)));

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

        // Solo i run OCR realmente falliti: lo stderr dei run riusciti (log Paddle/Tesseract) è rumore.
        lines.AddRange(result.OcrRuns
            .Where(x => x.Status is OcrRunStatus.Failed or OcrRunStatus.Timeout && !string.IsNullOrWhiteSpace(x.Error))
            .Select(x => $"{x.Engine}: {x.Status} - {TruncateWarning(x.Error!)}"));
        // I warning importanti (math.*, team.*, errori) sono già aggregati in Validation.Warnings;
        // le copie per-stat sarebbero duplicati, quindi non si ri-elencano.
        lines.AddRange(result.Validation.Warnings.Select(x => $"{x.Severity}: {x.Message}"));
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

    private void RaiseMatchInfoChanged()
    {
        RaisePropertyChanged(nameof(HasSelectedMatch));
        RaisePropertyChanged(nameof(SelectedMatchTeams));
        RaisePropertyChanged(nameof(SelectedMatchTime));
        RaisePropertyChanged(nameof(SelectedMatchPhase));
    }

    private void RaiseWorkflowChanged()
    {
        RaisePropertyChanged(nameof(WorkflowStatus));
        RaisePropertyChanged(nameof(ProcessHint));
        RaisePropertyChanged(nameof(HeaderStatus));
    }

}
