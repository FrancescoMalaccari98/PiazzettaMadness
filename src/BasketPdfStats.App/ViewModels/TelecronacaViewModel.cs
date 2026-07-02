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
            HomeTotals = BuildTotals(result, "Home");
            AwayTotals = BuildTotals(result, "Away");
            HomeRows = BuildRows(HomePlayers, HomeTotals);
            AwayRows = BuildRows(AwayPlayers, AwayTotals);
            TeamComparison = BuildComparison(result);
            HasTeamComparison = TeamComparison.Count > 0;
            var half = (TeamComparison.Count + 1) / 2;
            TeamComparisonLeft = TeamComparison.Take(half).ToList();
            TeamComparisonRight = TeamComparison.Skip(half).ToList();
        }
        else
        {
            HomePlayers = new ObservableCollection<TelecronacaPlayer>();
            AwayPlayers = new ObservableCollection<TelecronacaPlayer>();
            HomeRows = new ObservableCollection<TelecronacaPlayer>();
            AwayRows = new ObservableCollection<TelecronacaPlayer>();
            Highlights = [];
            Notes = [];
            HomeTotals = TelecronacaTeamTotals.Empty;
            AwayTotals = TelecronacaTeamTotals.Empty;
            TeamComparison = [];
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

    /// <summary>Solo giocatori (per calcoli/test).</summary>
    public ObservableCollection<TelecronacaPlayer> HomePlayers { get; }
    public ObservableCollection<TelecronacaPlayer> AwayPlayers { get; }

    /// <summary>Righe della tabella: giocatori + riga vuota + riga Totali (bind dei DataGrid).</summary>
    public ObservableCollection<TelecronacaPlayer> HomeRows { get; } = new();
    public ObservableCollection<TelecronacaPlayer> AwayRows { get; } = new();

    public IReadOnlyList<string> Highlights { get; }
    public IReadOnlyList<string> Notes { get; }

    /// <summary>Riga totali di squadra (mostrata sotto la lista giocatori, evidenziata).</summary>
    public TelecronacaTeamTotals HomeTotals { get; } = TelecronacaTeamTotals.Empty;
    public TelecronacaTeamTotals AwayTotals { get; } = TelecronacaTeamTotals.Empty;

    /// <summary>Confronto statistiche di squadra + comparative (scheda "Risultati squadre").</summary>
    public IReadOnlyList<TeamComparisonRow> TeamComparison { get; } = [];
    public bool HasTeamComparison { get; }

    /// <summary>Confronto suddiviso in due colonne (per stare in una schermata senza scroll).</summary>
    public IReadOnlyList<TeamComparisonRow> TeamComparisonLeft { get; } = [];
    public IReadOnlyList<TeamComparisonRow> TeamComparisonRight { get; } = [];

    public bool HasHighlights => Highlights.Count > 0;
    public bool HasNotes => Notes.Count > 0;

    /// <summary>True solo per stati che richiedono attenzione (Review/Error): niente banner verde "Completato".</summary>
    public bool HasImportantStatus => !string.Equals(StatusKind, "Ok", StringComparison.Ordinal);

    public TelecronacaPlayer? SelectedHomePlayer
    {
        get => _selectedHomePlayer;
        set
        {
            if (SetField(ref _selectedHomePlayer, value) && value is { IsPlayer: true })
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
            if (SetField(ref _selectedAwayPlayer, value) && value is { IsPlayer: true })
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

        // Valori del dettaglio (anche per la tabella dettaglio).
        var minutes = Minutes(stats.GetValueOrDefault("minutes"));
        var twoDetail = ShootingDetail(Num("twoPoints.made"), Num("twoPoints.attempted"));
        var threeDetail = ShootingDetail(Num("threePoints.made"), Num("threePoints.attempted"));
        var ftDetail = ShootingDetail(Num("freeThrows.made"), Num("freeThrows.attempted"));
        var rebOff = FormatInt(Num("rebounds.offensive"));
        var rebDef = FormatInt(Num("rebounds.defensive"));
        var rebTot = FormatInt(reb);
        var blocks = FormatInt(Num("blocks"));
        var turnovers = FormatInt(Num("turnovers"));
        var foulsDrawn = FormatInt(Num("fouls.drawn"));
        var plusMinus = FormatInt(Num("plusMinus"));
        var evaluation = FormatInt(eval);
        var pointsText = FormatInt(points);
        var assistsText = FormatInt(ast);
        var stealsText = FormatInt(stl);
        var foulsText = FormatInt(fouls);

        // Dettaglio a tabella: 3 coppie (etichetta, valore) per riga. I Minuti sono nell'intestazione.
        var detailPairs = BuildDetailPairs(
        [
            ("Punti", pointsText), ("2PT", twoDetail), ("3PT", threeDetail),
            ("TL", ftDetail), ("Valutazione", evaluation), ("Rimb. off", rebOff),
            ("Rimb. dif", rebDef), ("Rimb. tot", rebTot), ("+/-", plusMinus),
            ("Assist", assistsText), ("Recuperi", stealsText), ("Stoppate", blocks),
            ("Palle perse", turnovers), ("Falli fatti", foulsText), ("Falli subiti", foulsDrawn),
        ]);

        return new TelecronacaPlayer
        {
            Number = string.IsNullOrWhiteSpace(player.Number) ? "-" : player.Number!,
            NumberSort = JerseyNumber(player.Number),
            DisplayName = player.FullName ?? player.EntityId,
            DidNotPlay = didNotPlay,
            NeLabel = didNotPlay ? "N.E." : string.Empty,

            Points = pointsText,
            Two = Shooting(Num("twoPoints.made"), Num("twoPoints.attempted")),
            Three = Shooting(Num("threePoints.made"), Num("threePoints.attempted")),
            Ft = Shooting(Num("freeThrows.made"), Num("freeThrows.attempted")),
            Rebounds = rebTot,
            Assists = assistsText,
            Steals = stealsText,
            Fouls = foulsText,

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

            Minutes = minutes,
            TwoDetail = twoDetail,
            ThreeDetail = threeDetail,
            FtDetail = ftDetail,
            ReboundsOffensive = rebOff,
            ReboundsDefensive = rebDef,
            ReboundsTotal = rebTot,
            Blocks = blocks,
            Turnovers = turnovers,
            FoulsDrawn = foulsDrawn,
            PlusMinus = plusMinus,
            Evaluation = evaluation,
            DetailPairs = detailPairs,
        };
    }

    private static IReadOnlyList<TelecronacaDetailRow> BuildDetailPairs(IReadOnlyList<(string Label, string Value)> stats)
    {
        (string Label, string Value) At(int idx) =>
            idx < stats.Count ? stats[idx] : (Label: string.Empty, Value: string.Empty);

        var rows = new List<TelecronacaDetailRow>();
        for (var i = 0; i < stats.Count; i += 3) // 3 coppie per riga
        {
            var p1 = At(i);
            var p2 = At(i + 1);
            var p3 = At(i + 2);
            rows.Add(new TelecronacaDetailRow(p1.Label, p1.Value, p2.Label, p2.Value, p3.Label, p3.Value));
        }

        return rows;
    }

    private static ObservableCollection<TelecronacaPlayer> BuildRows(IReadOnlyList<TelecronacaPlayer> players, TelecronacaTeamTotals totals)
    {
        var rows = new ObservableCollection<TelecronacaPlayer>(players);
        // Riga vuota di separazione + riga Totali, sempre in fondo alla tabella.
        rows.Add(new TelecronacaPlayer
        {
            IsSeparator = true,
            Number = string.Empty, DisplayName = string.Empty, Points = string.Empty, Two = string.Empty,
            Three = string.Empty, Ft = string.Empty, Rebounds = string.Empty, Assists = string.Empty,
            Steals = string.Empty, Fouls = string.Empty,
        });
        rows.Add(new TelecronacaPlayer
        {
            IsTotals = true,
            Number = string.Empty,
            DisplayName = "Totali",
            Points = totals.Points,
            Two = totals.Two,
            Three = totals.Three,
            Ft = totals.Ft,
            Rebounds = totals.Rebounds,
            Assists = totals.Assists,
            Steals = totals.Steals,
            Fouls = totals.Fouls,
        });
        return rows;
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

    // ---------- totali e confronto squadra ----------

    private static Dictionary<string, object?> TeamStatsMap(ProcessingResult result, string side) =>
        result.Stats
            .Where(s => s.Scope == StatScope.Team && string.Equals(s.Side, side, StringComparison.OrdinalIgnoreCase))
            .GroupBy(s => s.StatKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.OrdinalIgnoreCase);

    private static TelecronacaTeamTotals BuildTotals(ProcessingResult result, string side)
    {
        var map = TeamStatsMap(result, side);
        int? Num(string key) => TryInt(map.GetValueOrDefault(key), out var v) ? v : null;
        return new TelecronacaTeamTotals
        {
            Points = FormatInt(Num("points")),
            Two = Shooting(Num("twoPoints.made"), Num("twoPoints.attempted")),
            Three = Shooting(Num("threePoints.made"), Num("threePoints.attempted")),
            Ft = Shooting(Num("freeThrows.made"), Num("freeThrows.attempted")),
            Rebounds = FormatInt(Num("rebounds.total")),
            Assists = FormatInt(Num("assists")),
            Steals = FormatInt(Num("steals")),
            Fouls = FormatInt(Num("fouls.committed")),
        };
    }

    private static IReadOnlyList<TeamComparisonRow> BuildComparison(ProcessingResult result)
    {
        var home = TeamStatsMap(result, "Home");
        var away = TeamStatsMap(result, "Away");
        if (home.Count == 0 && away.Count == 0)
        {
            return [];
        }

        var rows = new List<TeamComparisonRow>();
        var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Single(string label, string key)
        {
            covered.Add(key);
            rows.Add(new TeamComparisonRow(label, IntText(home, key), IntText(away, key)));
        }
        void Shoot(string label, string prefix)
        {
            covered.Add($"{prefix}.made");
            covered.Add($"{prefix}.attempted");
            covered.Add($"{prefix}.percentage");
            rows.Add(new TeamComparisonRow(label, ShootText(home, prefix), ShootText(away, prefix)));
        }

        // Righe principali (curate) — tiri combinati made/att/%.
        Single("Punti", "points");
        Shoot("Tiri dal campo", "fieldGoals");
        Shoot("Tiri da 2", "twoPoints");
        Shoot("Tiri da 3", "threePoints");
        Shoot("Tiri liberi", "freeThrows");
        Single("Rimbalzi offensivi", "rebounds.offensive");
        Single("Rimbalzi difensivi", "rebounds.defensive");
        Single("Rimbalzi totali", "rebounds.total");
        Single("Assist", "assists");
        Single("Palle perse", "turnovers");
        Single("Recuperi", "steals");
        Single("Stoppate", "blocks");
        Single("Falli fatti", "fouls.committed");
        Single("Falli subiti", "fouls.drawn");
        Single("+/-", "plusMinus");
        Single("Valutazione", "evaluation");

        // Tutti gli ALTRI campi squadra estratti dal PDF (percentuali, minuti, ecc.) non già coperti.
        var extraKeys = home.Keys.Concat(away.Keys)
            .Where(k => !covered.Contains(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => Label(k), StringComparer.OrdinalIgnoreCase);
        foreach (var key in extraKeys)
        {
            rows.Add(new TeamComparisonRow(Label(key), IntText(home, key), IntText(away, key)));
        }

        // Righe squadra con almeno un valore + statistiche comparative (punti da palle perse, in area, ecc.).
        var teamRows = rows.Where(r => r.Home != "-" || r.Away != "-").ToList();
        teamRows.AddRange(BuildComparatives(result));
        return teamRows;
    }

    /// <summary>
    /// Statistiche comparative estratte dal PDF (scope Comparative): punti da palle perse, punti in area,
    /// secondi tiri, contropiede, panchina, massimo vantaggio/parziale, cambi di guida, parità, ecc.
    /// </summary>
    private static IReadOnlyList<TeamComparisonRow> BuildComparatives(ProcessingResult result)
    {
        var rows = new List<TeamComparisonRow>();
        var groups = result.Stats
            .Where(s => s.Scope == StatScope.Comparative)
            .GroupBy(s => s.StatKey, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => Label(g.Key), StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var home = group.FirstOrDefault(s => string.Equals(s.Side, "Home", StringComparison.OrdinalIgnoreCase))?.Value;
            var away = group.FirstOrDefault(s => string.Equals(s.Side, "Away", StringComparison.OrdinalIgnoreCase))?.Value;
            var neutral = group.FirstOrDefault(s => string.IsNullOrWhiteSpace(s.Side))?.Value;
            var homeText = ValueText(home ?? neutral);
            var awayText = ValueText(away ?? neutral);
            if (homeText == "-" && awayText == "-")
            {
                continue;
            }

            rows.Add(new TeamComparisonRow(Label(group.Key), homeText, awayText));
        }

        return rows;
    }

    private static string ValueText(object? value)
    {
        if (value is null)
        {
            return "-";
        }

        if (TryInt(value, out var i) && value is not string)
        {
            return i.ToString(CultureInfo.InvariantCulture);
        }

        if (value is double d)
        {
            return d.ToString("0.##", CultureInfo.InvariantCulture);
        }

        if (value is decimal m)
        {
            return m.ToString("0.##", CultureInfo.InvariantCulture);
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(text) ? "-" : text;
    }

    private static string Label(string key) =>
        ProcessingResultViewModel.Labels.TryGetValue(key, out var label) ? label : key;

    private static string IntText(Dictionary<string, object?> map, string key) =>
        TryInt(map.GetValueOrDefault(key), out var v) ? v.ToString(CultureInfo.InvariantCulture) : "-";

    private static string ShootText(Dictionary<string, object?> map, string prefix)
    {
        int? made = TryInt(map.GetValueOrDefault($"{prefix}.made"), out var m) ? m : null;
        int? att = TryInt(map.GetValueOrDefault($"{prefix}.attempted"), out var a) ? a : null;
        return Shooting(made, att);
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

    /// <summary>Riga totali di squadra (in fondo alla tabella, evidenziata).</summary>
    public bool IsTotals { get; init; }
    /// <summary>Riga vuota di separazione tra giocatori e totali.</summary>
    public bool IsSeparator { get; init; }
    /// <summary>True per una vera riga giocatore (non totali/separatore): solo queste sono selezionabili.</summary>
    public bool IsPlayer => !IsTotals && !IsSeparator;

    /// <summary>Dettaglio in forma di tabella: coppie etichetta/valore su righe da due.</summary>
    public IReadOnlyList<TelecronacaDetailRow> DetailPairs { get; init; } = [];

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

/// <summary>Totali di squadra mostrati sotto la lista giocatori (riga evidenziata).</summary>
public sealed class TelecronacaTeamTotals
{
    public static readonly TelecronacaTeamTotals Empty = new();
    public string Points { get; init; } = "-";
    public string Two { get; init; } = "-";
    public string Three { get; init; } = "-";
    public string Ft { get; init; } = "-";
    public string Rebounds { get; init; } = "-";
    public string Assists { get; init; } = "-";
    public string Steals { get; init; } = "-";
    public string Fouls { get; init; } = "-";
}

/// <summary>Riga di confronto statistico tra le due squadre (scheda "Risultati squadre").</summary>
public sealed record TeamComparisonRow(string Statistica, string Home, string Away);

/// <summary>Riga della tabella dettaglio giocatore: tre coppie etichetta/valore affiancate
/// (i minuti sono mostrati nell'intestazione, non in tabella).</summary>
public sealed record TelecronacaDetailRow(
    string Label1, string Value1,
    string Label2, string Value2,
    string Label3, string Value3);
