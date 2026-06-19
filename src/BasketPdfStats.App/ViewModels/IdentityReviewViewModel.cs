using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Identity;

namespace BasketPdfStats.App.ViewModels;

/// <summary>
/// Riga di revisione: una voce <see cref="IdentityReviewItem"/> con i candidati selezionabili
/// (solo dalla squadra corretta) e lo stato di risoluzione.
/// </summary>
public sealed class IdentityReviewRowViewModel : INotifyPropertyChanged
{
    private OcrRosterPlayer? _selectedCandidate;
    private bool _resolved;

    public IdentityReviewRowViewModel(IdentityReviewItem item, IReadOnlyList<OcrRosterPlayer> sideRoster)
    {
        Item = item;
        Candidates = new ObservableCollection<OcrRosterPlayer>(sideRoster);
        IsBlocking = item.Reason is IdentityReviewReason.Conflict or IdentityReviewReason.NotInPdf;

        if (item.CandidatePlayerId is int candidateId)
        {
            _selectedCandidate = Candidates.FirstOrDefault(p => p.PlayerId == candidateId);
        }

        // Le voci non bloccanti (ProbableMatch/NumberNotFound/Unmatched) non richiedono azione.
        _resolved = !IsBlocking;

        ConfirmCommand = new AsyncRelayCommand(
            () => { Confirm(); return Task.CompletedTask; },
            () => CanConfirm && !Resolved);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AsyncRelayCommand ConfirmCommand { get; }

    public IdentityReviewItem Item { get; }
    public string Side => Item.Side;
    public IdentityReviewReason Reason => Item.Reason;
    public string? OcrJersey => Item.OcrJersey;
    public string? OcrName => Item.OcrName;
    public string? ProposedCandidateName => Item.CandidateName;
    public double ConfidenceScore => Item.ConfidenceScore;
    public ObservableCollection<OcrRosterPlayer> Candidates { get; }
    public bool IsBlocking { get; }

    public OcrRosterPlayer? SelectedCandidate
    {
        get => _selectedCandidate;
        set
        {
            if (SetField(ref _selectedCandidate, value))
            {
                RaisePropertyChanged(nameof(CanConfirm));
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool Resolved
    {
        get => _resolved;
        private set
        {
            if (SetField(ref _resolved, value))
            {
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Un conflitto richiede la scelta di un candidato; NotInPdf si conferma come assenza.</summary>
    public bool CanConfirm => Reason switch
    {
        IdentityReviewReason.Conflict => SelectedCandidate is not null,
        _ => true
    };

    public void Confirm()
    {
        if (CanConfirm)
        {
            Resolved = true;
        }
    }

    private void RaisePropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        RaisePropertyChanged(name);
        return true;
    }
}

/// <summary>
/// ViewModel della revisione manuale: elenca le voci, espone i candidati per lato corretto e
/// blocca l'export finché restano conflitti obbligatori (Conflict/NotInPdf) non risolti.
/// </summary>
public sealed class IdentityReviewViewModel : INotifyPropertyChanged
{
    public IdentityReviewViewModel(IReadOnlyList<IdentityReviewItem> items, OcrMatchContext context)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(context);

        Rows = [];
        foreach (var item in items)
        {
            var roster = string.Equals(item.Side, "Away", StringComparison.OrdinalIgnoreCase)
                ? context.AwayTeam.Players
                : context.HomeTeam.Players;
            var row = new IdentityReviewRowViewModel(item, roster);
            row.PropertyChanged += OnRowChanged;
            Rows.Add(row);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<IdentityReviewRowViewModel> Rows { get; }

    public bool HasReviewItems => Rows.Count > 0;

    public int OpenBlockingCount => Rows.Count(r => r.IsBlocking && !r.Resolved);

    /// <summary>Export consentito solo se non restano conflitti obbligatori aperti.</summary>
    public bool CanExport => OpenBlockingCount == 0;

    /// <summary>
    /// Scelte di risoluzione: entityId OCR → playerId canonico, per le righe con un candidato scelto.
    /// </summary>
    public IReadOnlyDictionary<string, int> BuildResolutions()
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in Rows)
        {
            if (!string.IsNullOrEmpty(row.Item.OcrEntityId) && row.SelectedCandidate is not null)
            {
                map[row.Item.OcrEntityId!] = row.SelectedCandidate.PlayerId;
            }
        }

        return map;
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IdentityReviewRowViewModel.Resolved))
        {
            RaisePropertyChanged(nameof(OpenBlockingCount));
            RaisePropertyChanged(nameof(CanExport));
        }
    }

    private void RaisePropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
