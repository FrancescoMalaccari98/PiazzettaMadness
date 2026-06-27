using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Identity;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.App.ViewModels;

/// <summary>
/// Vista "Risultati Telecronaca": presentazione da commentatore dei dati FINALI riconciliati
/// (niente provider/CH/diagnostica OCR). Header partita, due squadre affiancate, tabella compatta,
/// dettaglio giocatore, top player e spunti telecronaca. Costruita da un <see cref="ProcessingResult"/>.
/// </summary>
public sealed class TelecronacaViewModel : INotifyPropertyChanged
{
    private TelecronacaPlayer? _selectedHomePlayer;
    private TelecronacaPlayer? _selectedAwayPlayer;
    private TelecronacaPlayer? _selectedPlayer;

    public TelecronacaViewModel(
        ProcessingResult result,
        OcrMatchOption? matchOption = null,
        string? mismatchMessage = null,
        bool importable = false)
    {
        ArgumentNullException.ThrowIfNull(result);

        var homeName = TeamName(result, "Home", matchOption?.HomeTeam.Name) ?? "Casa";
        var awayName = TeamName(result, "Away", matchOption?.AwayTeam.Name) ?? "Ospite";
        HomeTeam = homeName;
        AwayTeam = awayName;

        var (homeScore, awayScore) = Scores(result);
        HomeScore = homeScore;
        AwayScore = awayScore;

        MatchIdText = matchOption is not null ? matchOption.MatchId.ToString(CultureInfo.InvariantCulture) : "-";
        DateTimeText = BuildDateTime(result, matchOption);
        PhaseText = BuildPhase(matchOption);

        ReviewRequired = result.ProcessedFile.Status == FileProcessingStatus.CompletedWithReviewRequired
                         || result.IdentityReview.Any(i => i.Reason is IdentityReviewReason.Conflict or IdentityReviewReason.NotInPdf);
        MismatchMessage = mismatchMessage;
        Importable = importable;

        // Su mismatch NON si mostrano le tabelle (eviti una vista risultati fuorviante).
        HasPlayers = mismatchMessage is null && result.Players.Count > 0;

        (StatusText, StatusKind) = BuildStatus(result, mismatchMessage, ReviewRequired);

        if (HasPlayers)
        {
            HomePlayers = BuildPlayers(result, "Home");
            AwayPlayers = BuildPlayers(result, "Away");
            Highlights = BuildHighlights(HomePlayers.Concat(AwayPlayers).ToList());
            Notes = BuildNotes(result, homeName, awayName, HomePlayers, AwayPlayers);
        }
        else
        {
            HomePlayers = new ObservableCollection<TelecronacaPlayer>();
            AwayPlayers = new ObservableCollection<TelecronacaPlayer>();
            Highlights = [];
            Notes = [];
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string HomeTeam { get; }
    public string AwayTeam { get; }
    public string HomeScore { get; }
    public string AwayScore { get; }
    public string MatchIdText { get; }
    public string DateTimeText { get; }
    public string PhaseText { get; }
    public string StatusText { get; }
    public string StatusKind { get; } // Ok | Review | Error
    public bool ReviewRequired { get; }
    public bool Importable { get; }
    public string? MismatchMessage { get; }
    public bool HasPlayers { get; }

    public ObservableCollection<TelecronacaPlayer> HomePlayers { get; }
    public ObservableCollection<TelecronacaPlayer> AwayPlayers { get; }
    public IReadOnlyList<string> Highlights { get; }
    public IReadOnlyList<string> Notes { get; }

    public bool HasHighlights => Highlights.Count > 0;
    public bool HasNotes => Notes.Count > 0;

    public TelecronacaPlayer? SelectedHomePlayer
    {
        get => _selectedHomePlayer;
        set
        {
            if (SetField(ref _selectedHomePlayer, value) && value is not null)
            {
                SelectedAwayPlayer = null;
                SelectedPlayer = value;
            }
        }
    }

    public TelecronacaPlayer? SelectedAwayPlayer
    {
        get => _selectedAwayPlayer;
        set
        {
            if (SetField(ref _selectedAwayPlayer, value) && value is not null)
            {
                SelectedHomePlayer = null;
                SelectedPlayer = value;
            }
        }
    }

    public TelecronacaPlayer? SelectedPlayer
    {
        get => _selectedPlayer;
        private set
        {
            if (SetField(ref _selectedPlayer, value))
            {
                RaisePropertyChanged(nameof(HasSelectedPlayer));
            }
        }
    }

    public bool HasSelectedPlayer => _selectedPlayer is not null;

    // ---------- costruzione righe ----------

    private static ObservableCollection<TelecronacaPlayer> BuildPlayers(ProcessingResult result, string side)
    {
        var rows = result.Players
            .Where(p => string.Equals(p.Side, side, StringComparison.OrdinalIgnoreCase))
            .Select(p => BuildPlayer(result, p))
            // Default: numero maglia crescente; i N.E./senza statistiche in fondo.
            .OrderBy(r => r.DidNotPlay ? 1 : 0)
            .ThenBy(r => r.NumberSort)
            .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new ObservableCollection<TelecronacaPlayer>(rows);
    }

    private static TelecronacaPlayer BuildPlayer(ProcessingResult result, PlayerStats player)
    {
        var stats = result.Stats
            .Where(s => s.Scope == StatScope.Player && string.Equals(s.EntityId, player.EntityId, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(s => s.StatKey, s => s.Value, StringComparer.OrdinalIgnoreCase);

        int? Num(string key) => TryInt(stats.GetValueOrDefault(key), out var v) ? v : null;

        var points = Num("points");
        var reb = Num("rebounds.total");
        var ast = Num("assists");
        var stl = Num("steals");
        var fouls = Num("fouls.committed");
        var eval = Num("evaluation");
        var hasAnyStat = stats.Values.Any(v => v is not null);
        var didNotPlay = player.DidNotPlay || !hasAnyStat;

        return new TelecronacaPlayer
        {
            Number = string.IsNullOrWhiteSpace(player.Number) ? "-" : player.Number!,
            NumberSort = JerseyNumber(player.Number),
            DisplayName = player.FullName ?? player.EntityId,
            DidNotPlay = didNotPlay,
            NeLabel = didNotPlay ? "N.E." : string.Empty,

            Points = FormatInt(points),
            Two = Shooting(Num("twoPoints.made"), Num("twoPoints.attempted")),
            Three = Shooting(Num("threePoints.made"), Num("threePoints.attempted")),
            Ft = Shooting(Num("freeThrows.made"), Num("freeThrows.attempted")),
            Rebounds = FormatInt(reb),
            Assists = FormatInt(ast),
            Steals = FormatInt(stl),
            Fouls = FormatInt(fouls),

            PointsSort = points ?? -1,
            ReboundsSort = reb ?? -1,
            AssistsSort = ast ?? -1,
            StealsSort = stl ?? -1,
            FoulsSort = fouls ?? -1,
            TwoMadeSort = Num("twoPoints.made") ?? -1,
            ThreeMadeSort = Num("threePoints.made") ?? -1,
            FtMadeSort = Num("freeThrows.made") ?? -1,

            PointsValue = points,
            ReboundsValue = reb,
            AssistsValue = ast,
            EvaluationValue = eval,

            Minutes = Minutes(stats.GetValueOrDefault("minutes")),
            TwoDetail = ShootingDetail(Num("twoPoints.made"), Num("twoPoints.attempted")),
            ThreeDetail = ShootingDetail(Num("threePoints.made"), Num("threePoints.attempted")),
            FtDetail = ShootingDetail(Num("freeThrows.made"), Num("freeThrows.attempted")),
            ReboundsOffensive = FormatInt(Num("rebounds.offensive")),
            ReboundsDefensive = FormatInt(Num("rebounds.defensive")),
            ReboundsTotal = FormatInt(reb),
            Blocks = FormatInt(Num("blocks")),
            Turnovers = FormatInt(Num("turnovers")),
            FoulsDrawn = FormatInt(Num("fouls.drawn")),
            PlusMinus = FormatInt(Num("plusMinus")),
            Evaluation = FormatInt(eval),
        };
    }

    // ---------- top player ----------

    private static IReadOnlyList<string> BuildHighlights(IReadOnlyList<TelecronacaPlayer> players)
    {
        var highlights = new List<string>();
        AddTop(highlights, players, "Top scorer", p => p.PointsValue, "PTS");
        AddTop(highlights, players, "Miglior valutazione", p => p.EvaluationValue, "VAL");
        AddTop(highlights, players, "Rimbalzi", p => p.ReboundsValue, "REB");
        AddTop(highlights, players, "Assist", p => p.AssistsValue, "AST");
        return highlights;
    }

    private static void AddTop(List<string> sink, IReadOnlyList<TelecronacaPlayer> players, string label, Func<TelecronacaPlayer, int?> selector, string unit)
    {
        var best = players
            .Where(p => selector(p) is > 0)
            .OrderByDescending(p => selector(p)!.Value)
            .FirstOrDefault();
        if (best is not null)
        {
            sink.Add($"{label}: {best.DisplayName} — {selector(best)!.Value} {unit}");
        }
    }

    // ---------- spunti telecronaca ----------

    private static IReadOnlyList<string> BuildNotes(
        ProcessingResult result, string homeName, string awayName,
        IReadOnlyList<TelecronacaPlayer> home, IReadOnlyList<TelecronacaPlayer> away)
    {
        var notes = new List<string>();

        var homeDouble = home.Count(p => p.PointsValue is >= 10);
        if (homeDouble > 0)
        {
            notes.Add($"{homeName} ha {homeDouble} giocator{(homeDouble == 1 ? "e" : "i")} in doppia cifra.");
        }

        var awayDouble = away.Count(p => p.PointsValue is >= 10);
        if (awayDouble > 0)
        {
            notes.Add($"{awayName} ha {awayDouble} giocator{(awayDouble == 1 ? "e" : "i")} in doppia cifra.");
        }

        AddTeamThreeNote(result, "Home", homeName, notes);
        AddTeamThreeNote(result, "Away", awayName, notes);

        var bestEval = home.Concat(away).Where(p => p.EvaluationValue is > 0).OrderByDescending(p => p.EvaluationValue).FirstOrDefault();
        if (bestEval is not null)
        {
            notes.Add($"Miglior valutazione: {bestEval.DisplayName} con {bestEval.EvaluationValue}.");
        }

        return notes.Take(5).ToList();
    }

    private static void AddTeamThreeNote(ProcessingResult result, string side, string teamName, List<string> notes)
    {
        var made = TeamStat(result, side, "threePoints.made");
        var attempted = TeamStat(result, side, "threePoints.attempted");
        if (made is not null && attempted is > 0)
        {
            notes.Add($"{teamName} tira {made}/{attempted} da tre.");
        }
    }

    private static int? TeamStat(ProcessingResult result, string side, string key)
    {
        var stat = result.Stats.FirstOrDefault(s =>
            s.Scope == StatScope.Team &&
            string.Equals(s.Side, side, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.StatKey, key, StringComparison.OrdinalIgnoreCase));
        return stat is not null && TryInt(stat.Value, out var v) ? v : null;
    }

    // ---------- header / stato ----------

    private static (string Text, string Kind) BuildStatus(ProcessingResult result, string? mismatch, bool reviewRequired)
    {
        if (mismatch is not null)
        {
            return ("PDF non corrispondente al match selezionato", "Error");
        }

        if (result.ProcessedFile.Status == FileProcessingStatus.Failed)
        {
            return ("Elaborazione fallita", "Error");
        }

        if (reviewRequired)
        {
            return ("Revisione richiesta", "Review");
        }

        if (result.ProcessedFile.Status == FileProcessingStatus.CompletedNotValidated)
        {
            return ("Completato (non validato)", "Error");
        }

        return ("Completato", "Ok");
    }

    private static string? TeamName(ProcessingResult result, string side, string? dbName)
    {
        var ocr = result.Teams.FirstOrDefault(t => string.Equals(t.Side, side, StringComparison.OrdinalIgnoreCase))?.Name;
        return !string.IsNullOrWhiteSpace(dbName) ? dbName
            : !string.IsNullOrWhiteSpace(ocr) ? ocr
            : null;
    }

    private static (string Home, string Away) Scores(ProcessingResult result)
    {
        var home = result.Stats.FirstOrDefault(s => s.Scope == StatScope.Result && string.Equals(s.Side, "Home", StringComparison.OrdinalIgnoreCase) && s.StatKey == "points")?.Value;
        var away = result.Stats.FirstOrDefault(s => s.Scope == StatScope.Result && string.Equals(s.Side, "Away", StringComparison.OrdinalIgnoreCase) && s.StatKey == "points")?.Value;
        if (home is not null || away is not null)
        {
            return (ScoreText(home), ScoreText(away));
        }

        var parts = result.Game.FinalScore?.Split('-', 2, StringSplitOptions.TrimEntries);
        return parts?.Length == 2 ? (parts[0], parts[1]) : ("-", "-");
    }

    private static string ScoreText(object? value) => TryInt(value, out var v) ? v.ToString(CultureInfo.InvariantCulture) : "-";

    private static string BuildDateTime(ProcessingResult result, OcrMatchOption? matchOption)
    {
        var raw = matchOption?.ScheduledStartAt;
        if (!string.IsNullOrWhiteSpace(raw) &&
            DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        return Join(result.Game.Date, result.Game.Time);
    }

    private static string BuildPhase(OcrMatchOption? matchOption)
    {
        if (matchOption is null)
        {
            return string.Empty;
        }

        return string.Join(" - ", new[] { matchOption.Phase, matchOption.Round }.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    // ---------- formattazione ----------

    private static string Shooting(int? made, int? attempted)
    {
        if (made is null && attempted is null)
        {
            return "-";
        }

        var m = made ?? 0;
        var a = attempted ?? 0;
        return a > 0 ? $"{m}/{a} {(int)Math.Round(100.0 * m / a)}%" : $"{m}/{a}";
    }

    private static string ShootingDetail(int? made, int? attempted)
    {
        if (made is null && attempted is null)
        {
            return "-";
        }

        var m = made ?? 0;
        var a = attempted ?? 0;
        return a > 0 ? $"{m}/{a} ({(int)Math.Round(100.0 * m / a)}%)" : $"{m}/{a}";
    }

    private static string FormatInt(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "-";

    private static string Minutes(object? value)
    {
        if (value is null)
        {
            return "-";
        }

        if (TryInt(value, out var seconds) && value is not string)
        {
            return $"{seconds / 60}:{seconds % 60:00}";
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(text) ? "-" : text;
    }

    private static string Join(params string?[] values)
    {
        var text = string.Join(" ", values.Where(v => !string.IsNullOrWhiteSpace(v))).Trim();
        return text;
    }

    private static int JerseyNumber(string? value) =>
        int.TryParse((value ?? string.Empty).TrimStart('*'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : int.MaxValue;

    private static bool TryInt(object? value, out int number)
    {
        number = 0;
        switch (value)
        {
            case null:
                return false;
            case int i:
                number = i;
                return true;
            case long l:
                number = (int)l;
                return true;
            case double d:
                number = (int)Math.Round(d);
                return true;
            case decimal m:
                number = (int)Math.Round(m);
                return true;
            case JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var je):
                number = je;
                return true;
            case JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var jd):
                number = (int)Math.Round(jd);
                return true;
            case string s:
                return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
            default:
                return false;
        }
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

/// <summary>Riga giocatore per la vista telecronaca (dati finali, "-" se mancanti).</summary>
public sealed class TelecronacaPlayer
{
    public string Number { get; init; } = "-";
    public int NumberSort { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public bool DidNotPlay { get; init; }
    public string NeLabel { get; init; } = string.Empty;

    // Colonne compatte
    public string Points { get; init; } = "-";
    public string Two { get; init; } = "-";
    public string Three { get; init; } = "-";
    public string Ft { get; init; } = "-";
    public string Rebounds { get; init; } = "-";
    public string Assists { get; init; } = "-";
    public string Steals { get; init; } = "-";
    public string Fouls { get; init; } = "-";

    // Chiavi di ordinamento numerico (DataGrid SortMemberPath)
    public int PointsSort { get; init; }
    public int ReboundsSort { get; init; }
    public int AssistsSort { get; init; }
    public int StealsSort { get; init; }
    public int FoulsSort { get; init; }
    public int TwoMadeSort { get; init; }
    public int ThreeMadeSort { get; init; }
    public int FtMadeSort { get; init; }

    // Valori numerici per calcolo top player
    public int? PointsValue { get; init; }
    public int? ReboundsValue { get; init; }
    public int? AssistsValue { get; init; }
    public int? EvaluationValue { get; init; }

    // Dettaglio
    public string Minutes { get; init; } = "-";
    public string TwoDetail { get; init; } = "-";
    public string ThreeDetail { get; init; } = "-";
    public string FtDetail { get; init; } = "-";
    public string ReboundsOffensive { get; init; } = "-";
    public string ReboundsDefensive { get; init; } = "-";
    public string ReboundsTotal { get; init; } = "-";
    public string Blocks { get; init; } = "-";
    public string Turnovers { get; init; } = "-";
    public string FoulsDrawn { get; init; } = "-";
    public string PlusMinus { get; init; } = "-";
    public string Evaluation { get; init; } = "-";
}
