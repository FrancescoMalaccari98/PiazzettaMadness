using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BasketPdfStats.App.Services;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.App.ViewModels;

/// <summary>Cella editabile per una singola statistica (fieldId → valore modificabile).</summary>
public sealed class EditableStatRow : INotifyPropertyChanged
{
    private string _editValue;
    private bool _isValid = true;
    private bool _isModified;

    public EditableStatRow(string fieldId, string statKey, string label, object? originalValue)
    {
        FieldId = fieldId;
        StatKey = statKey;
        Label = label;
        OriginalValue = originalValue;
        _editValue = FormatValue(originalValue);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public Action? OnChanged { get; set; }

    public string FieldId { get; }
    public string StatKey { get; }
    public string Label { get; }
    public object? OriginalValue { get; }

    public string EditValue
    {
        get => _editValue;
        set
        {
            if (!SetField(ref _editValue, value)) return;
            var modified = !string.Equals(FormatValue(OriginalValue), value, StringComparison.Ordinal);
            SetField(ref _isModified, modified, nameof(IsModified));
            Validate();
            OnChanged?.Invoke();
        }
    }

    public bool IsValid { get => _isValid; private set => SetField(ref _isValid, value); }
    public bool IsModified { get => _isModified; private set => SetField(ref _isModified, value); }

    public object? ParsedValue => IsValid ? ParseValue(_editValue, StatKey) : OriginalValue;

    private void Validate()
    {
        var raw = _editValue.Trim();
        if (string.IsNullOrWhiteSpace(raw)) { IsValid = true; return; }
        if (IsStringTypeStat(StatKey)) { IsValid = true; return; }
        if (string.Equals(StatKey, "minutes", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(StatKey, "comparative.timeInLead", StringComparison.OrdinalIgnoreCase))
        {
            IsValid = Regex.IsMatch(raw, @"^\d{1,3}:\d{2}$");
            return;
        }
        if (IsDecimalStat(StatKey))
        {
            IsValid = double.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _);
            return;
        }
        IsValid = int.TryParse(raw, out _);
    }

    private static bool IsStringTypeStat(string key) =>
        key is "comparative.largestLead" or "comparative.biggestRun";

    private static bool IsDecimalStat(string key) =>
        key is "comparative.pointsPerPossession";

    private static string FormatValue(object? v) => v?.ToString() ?? string.Empty;

    private static object? ParseValue(string raw, string statKey)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (string.Equals(statKey, "minutes", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(statKey, "comparative.timeInLead", StringComparison.OrdinalIgnoreCase) ||
            IsStringTypeStat(statKey))
            return raw.Trim();
        if (IsDecimalStat(statKey))
            return double.TryParse(raw.Trim(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d) ? (object)d : null;
        return int.TryParse(raw.Trim(), out var i) ? (object)i : null;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}

/// <summary>Gruppo tematico di stat (es. "Rimbalzi") con colore di sfondo dedicato.</summary>
public sealed class EditableStatGroup
{
    public EditableStatGroup(string groupName, string background, IReadOnlyList<EditableStatRow> rows)
    {
        GroupName = groupName;
        Background = background;
        Rows = rows;
    }

    public string GroupName { get; }
    public string Background { get; }
    public IReadOnlyList<EditableStatRow> Rows { get; }
}

/// <summary>Tutte le stat editabili di un giocatore, suddivise per gruppo tematico.</summary>
public sealed class EditablePlayerSection
{
    public EditablePlayerSection(string entityId, string side, string displayName, IReadOnlyList<EditableStatGroup> groups)
    {
        EntityId = entityId;
        Side = side;
        DisplayName = displayName;
        Groups = groups;
    }

    public string EntityId { get; }
    public string Side { get; }
    public string DisplayName { get; }
    public IReadOnlyList<EditableStatGroup> Groups { get; }
}

/// <summary>Riga editabile di un parziale (Q1/Q2/…).</summary>
public sealed class EditablePeriodRow : INotifyPropertyChanged
{
    private string _editHome;
    private string _editAway;

    public EditablePeriodRow(PeriodScore period)
    {
        Period = period.Period;
        _editHome = period.Home?.ToString() ?? string.Empty;
        _editAway = period.Away?.ToString() ?? string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Period { get; }
    public string EditHome { get => _editHome; set => SetField(ref _editHome, value); }
    public string EditAway { get => _editAway; set => SetField(ref _editAway, value); }
    public int? ParsedHome => int.TryParse(_editHome.Trim(), out var i) ? i : null;
    public int? ParsedAway => int.TryParse(_editAway.Trim(), out var i) ? i : null;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}

/// <summary>
/// ViewModel della finestra di revisione manuale completa: statistiche giocatori,
/// statistiche squadra, info partita, conflitti identità.
/// </summary>
public sealed class ManualReviewViewModel : INotifyPropertyChanged
{
    private bool _canConfirm;
    private string _editableFinalScore;
    private readonly string _originalFinalScore;

    public ManualReviewViewModel(ProcessingResult result, OcrMatchContext? context)
    {
        var home = result.Teams.FirstOrDefault(t => string.Equals(t.Side, "Home", StringComparison.OrdinalIgnoreCase));
        var away = result.Teams.FirstOrDefault(t => string.Equals(t.Side, "Away", StringComparison.OrdinalIgnoreCase));
        HomeTeamName = home?.Name ?? "Casa";
        AwayTeamName = away?.Name ?? "Ospite";

        HomePlayerSections = BuildPlayerSections(result, "Home");
        AwayPlayerSections = BuildPlayerSections(result, "Away");
        HomeTeamRows = BuildTeamRows(result, "Home");
        AwayTeamRows = BuildTeamRows(result, "Away");
        GameRows = BuildGameRows(result);
        Periods = result.Game.Periods.Select(p => new EditablePeriodRow(p)).ToList();
        _originalFinalScore = result.Game.FinalScore ?? string.Empty;
        _editableFinalScore = _originalFinalScore;

        if (context is not null && result.IdentityReview.Count > 0)
        {
            IdentityReview = new IdentityReviewViewModel(result.IdentityReview, context);
            HasIdentityItems = true;
            IdentityReview.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IdentityReviewViewModel.CanExport))
                    RecalcCanConfirm();
            };
        }

        foreach (var row in AllStatRows())
            row.OnChanged = RecalcCanConfirm;

        RecalcCanConfirm();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string HomeTeamName { get; }
    public string AwayTeamName { get; }
    public IReadOnlyList<EditablePlayerSection> HomePlayerSections { get; }
    public IReadOnlyList<EditablePlayerSection> AwayPlayerSections { get; }
    public IReadOnlyList<EditableStatRow> HomeTeamRows { get; }
    public IReadOnlyList<EditableStatRow> AwayTeamRows { get; }
    public IReadOnlyList<EditablePeriodRow> Periods { get; }
    public bool HasPeriods => Periods.Count > 0;
    public IReadOnlyList<EditableStatRow> GameRows { get; }
    public bool HasGameRows => GameRows.Count > 0;

    public string EditableFinalScore
    {
        get => _editableFinalScore;
        set => SetField(ref _editableFinalScore, value);
    }

    public IdentityReviewViewModel? IdentityReview { get; }
    public bool HasIdentityItems { get; }

    public bool CanConfirm { get => _canConfirm; private set => SetField(ref _canConfirm, value); }

    public ManualEditSet BuildResult()
    {
        var statEdits = AllStatRows()
            .Where(r => r.IsModified && r.IsValid)
            .Select(r => new StatEdit(r.FieldId, r.ParsedValue))
            .ToList();

        var identityOverrides = IdentityReview?.BuildResolutions() ?? (IReadOnlyDictionary<string, int>)new Dictionary<string, int>();

        var trimmed = _editableFinalScore.Trim();
        var finalScoreOverride = string.Equals(trimmed, _originalFinalScore, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(trimmed)
            ? null
            : trimmed;

        return new ManualEditSet(statEdits, identityOverrides, finalScoreOverride);
    }

    private IEnumerable<EditableStatRow> AllStatRows()
        => HomePlayerSections.Concat(AwayPlayerSections)
            .SelectMany(s => s.Groups).SelectMany(g => g.Rows)
            .Concat(HomeTeamRows).Concat(AwayTeamRows).Concat(GameRows);

    private void RecalcCanConfirm()
    {
        var allValid = AllStatRows().All(r => r.IsValid);
        var identityOk = IdentityReview is null || IdentityReview.CanExport;
        CanConfirm = allValid && identityOk;
    }

    // Gruppi tematici: ordine, etichetta, colore, stat key incluse.
    private static readonly (string Name, string Background, string[] Keys)[] PlayerStatGroups =
    [
        ("Minuti",       "#E8F4FD", ["minutes"]),
        ("Punti",        "#FEF9E7", ["points"]),
        ("Tiri da 2",    "#E8F8F5", ["twoPoints.made",    "twoPoints.attempted",    "twoPoints.percentage"]),
        ("Tiri da 3",    "#F0F3FD", ["threePoints.made",  "threePoints.attempted",  "threePoints.percentage"]),
        ("Tiri liberi",  "#F5EEF8", ["freeThrows.made",   "freeThrows.attempted",   "freeThrows.percentage"]),
        ("Tiri totali",  "#F8F9FA", ["fieldGoals.made",   "fieldGoals.attempted",   "fieldGoals.percentage"]),
        ("Rimbalzi",     "#EAFAF1", ["rebounds.offensive", "rebounds.defensive",     "rebounds.total"]),
        ("Gara",         "#FEF5E7", ["assists", "turnovers", "steals", "blocks"]),
        ("Falli",        "#FDEDEC", ["fouls.committed",   "fouls.drawn"]),
        ("Valutazione",  "#F4ECF7", ["plusMinus", "efficiency", "evaluation"]),
    ];

    private static IReadOnlyList<EditablePlayerSection> BuildPlayerSections(ProcessingResult result, string side)
    {
        var players = result.Players
            .Where(p => string.Equals(p.Side, side, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => int.TryParse(p.Number, out var n) ? n : int.MaxValue);

        return players.Select(p =>
        {
            var statsByKey = result.Stats
                .Where(s => s.Scope == StatScope.Player &&
                            string.Equals(s.EntityId, p.EntityId, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(s => s.StatKey, StringComparer.OrdinalIgnoreCase);

            var assignedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var groups = new List<EditableStatGroup>();

            foreach (var (name, bg, keys) in PlayerStatGroups)
            {
                var rows = keys
                    .Where(k => statsByKey.ContainsKey(k))
                    .Select(k => { assignedKeys.Add(k); return new EditableStatRow(statsByKey[k].FieldId, k, Label(k), statsByKey[k].Value); })
                    .ToList();
                if (rows.Count > 0)
                    groups.Add(new EditableStatGroup(name, bg, rows));
            }

            // Stat non censite in nessun gruppo: coda senza colore.
            var extra = statsByKey
                .Where(kv => !assignedKeys.Contains(kv.Key))
                .Select(kv => new EditableStatRow(kv.Value.FieldId, kv.Key, Label(kv.Key), kv.Value.Value))
                .ToList();
            if (extra.Count > 0)
                groups.Add(new EditableStatGroup("Altro", "#F2F3F4", extra));

            var suffix = p.DidNotPlay ? " (N.E.)" : string.Empty;
            return new EditablePlayerSection(p.EntityId, side, $"#{p.Number} {p.FullName}{suffix}", groups);
        }).ToList();
    }

    private static IReadOnlyList<EditableStatRow> BuildTeamRows(ProcessingResult result, string side)
        => result.Stats
            .Where(s => (s.Scope == StatScope.Team || s.Scope == StatScope.Comparative) &&
                        string.Equals(s.Side, side, StringComparison.OrdinalIgnoreCase))
            .Select(s => new EditableStatRow(s.FieldId, s.StatKey, Label(s.StatKey), s.Value))
            .ToList();

    private static IReadOnlyList<EditableStatRow> BuildGameRows(ProcessingResult result)
        => result.Stats
            .Where(s => s.Scope == StatScope.Comparative &&
                        string.IsNullOrEmpty(s.Side))
            .Select(s => new EditableStatRow(s.FieldId, s.StatKey, Label(s.StatKey), s.Value))
            .ToList();

    private static string Label(string statKey)
        => ProcessingResultViewModel.Labels.TryGetValue(statKey, out var l) ? l : statKey;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
