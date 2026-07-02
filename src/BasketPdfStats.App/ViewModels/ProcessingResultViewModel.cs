using System.Globalization;
using System.Text.Json;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.App.ViewModels;

public sealed class ProcessingResultViewModel
{
    internal static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["points"] = "Punti",
        ["minutes"] = "Minuti",
        ["fieldGoals.made"] = "Tiri dal campo segnati",
        ["fieldGoals.attempted"] = "Tiri dal campo tentati",
        ["fieldGoals.percentage"] = "Tiri dal campo %",
        ["twoPoints.made"] = "2 punti segnati",
        ["twoPoints.attempted"] = "2 punti tentati",
        ["twoPoints.percentage"] = "2 punti %",
        ["threePoints.made"] = "3 punti segnati",
        ["threePoints.attempted"] = "3 punti tentati",
        ["threePoints.percentage"] = "3 punti %",
        ["freeThrows.made"] = "Tiri liberi segnati",
        ["freeThrows.attempted"] = "Tiri liberi tentati",
        ["freeThrows.percentage"] = "Tiri liberi %",
        ["rebounds.offensive"] = "Rimbalzi offensivi",
        ["rebounds.defensive"] = "Rimbalzi difensivi",
        ["rebounds.total"] = "Rimbalzi totali",
        ["assists"] = "Assist",
        ["turnovers"] = "Palle perse",
        ["steals"] = "Recuperi",
        ["blocks"] = "Stoppate",
        ["fouls.committed"] = "Falli commessi",
        ["fouls.drawn"] = "Falli subiti",
        ["plusMinus"] = "+/-",
        ["efficiency"] = "Valutazione",
        ["evaluation"] = "Valutazione",
        ["comparative.pointsFromTurnovers"] = "Punti da palle perse",
        ["comparative.pointsInThePaint"] = "Punti in area",
        ["comparative.secondChancePoints"] = "Punti da secondi tiri",
        ["comparative.fastBreakPoints"] = "Punti in contropiede",
        ["comparative.fastBreakPointsFromTurnovers"] = "Fast break da palle perse",
        ["comparative.benchPoints"] = "Punti panchina",
        ["comparative.largestLead"] = "Massimo vantaggio",
        ["comparative.biggestRun"] = "Massimo parziale",
        ["comparative.pointsPerPossession"] = "Punti per possesso",
        ["comparative.leadChanges"] = "Cambi di guida",
        ["comparative.timesTied"] = "Parita",
        ["comparative.timeInLead"] = "Tempo in vantaggio"
    };

    private static readonly string[] TeamTotalKeys =
    [
        "points", "fieldGoals.made", "fieldGoals.attempted", "fieldGoals.percentage",
        "twoPoints.made", "twoPoints.attempted", "twoPoints.percentage",
        "threePoints.made", "threePoints.attempted", "threePoints.percentage",
        "freeThrows.made", "freeThrows.attempted", "freeThrows.percentage",
        "rebounds.offensive", "rebounds.defensive", "rebounds.total",
        "assists", "turnovers", "steals", "blocks", "fouls.committed", "fouls.drawn",
        "plusMinus", "evaluation", "minutes"
    ];

    public ProcessingResultViewModel(ProcessingResult result)
    {
        FileName = result.ProcessedFile.FileName;
        Competition = result.Game.Competition ?? "-";
        DateAndTime = Join(result.Game.Date, result.Game.Time);
        Providers = string.Join(", ", result.Reconciliation?.Providers ??
            result.OcrRuns.Where(run => run.Status == OcrRunStatus.Success).Select(run => run.Engine).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        Providers = string.IsNullOrWhiteSpace(Providers) ? "-" : Providers;
        ReconciliationEnabled = result.Reconciliation?.Enabled == true ? "Si" : "No";
        var homeTeam = Team(result, "Home");
        var awayTeam = Team(result, "Away");
        HomeTeam = TeamName(homeTeam, result.Game.HomeTeamId, "Home");
        AwayTeam = TeamName(awayTeam, result.Game.AwayTeamId, "Away");
        (HomeScore, AwayScore) = Scores(result);
        Periods = result.Game.Periods.Select(period => new PeriodDisplayRow(period.Period, Value(period.Home), Value(period.Away))).ToList();
        TeamCount = result.Teams.Count;
        PlayerCount = result.Players.Count;
        StatCount = result.Stats.Count;
        WarningRows = Warnings(result);
        WarningCount = WarningRows.Count(row => !string.Equals(row.Severity, "FailedRule", StringComparison.OrdinalIgnoreCase));
        FailedRuleCount = result.Validation.FailedRules.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        HomeTeamTotals = TeamTotals(result, "Home");
        AwayTeamTotals = TeamTotals(result, "Away");
        HomePlayers = Players(result, "Home");
        AwayPlayers = Players(result, "Away");
        ComparativeStats = Comparatives(result);
    }

    public string FileName { get; }
    public string Competition { get; }
    public string DateAndTime { get; }
    public string Providers { get; }
    public string ReconciliationEnabled { get; }
    public string HomeTeam { get; }
    public string AwayTeam { get; }
    public string HomeScore { get; }
    public string AwayScore { get; }
    public int TeamCount { get; }
    public int PlayerCount { get; }
    public int StatCount { get; }
    public int WarningCount { get; }
    public int FailedRuleCount { get; }
    public IReadOnlyList<PeriodDisplayRow> Periods { get; }
    public IReadOnlyList<StatDisplayRow> HomeTeamTotals { get; }
    public IReadOnlyList<StatDisplayRow> AwayTeamTotals { get; }
    public IReadOnlyList<PlayerDisplayRow> HomePlayers { get; }
    public IReadOnlyList<PlayerDisplayRow> AwayPlayers { get; }
    public IReadOnlyList<ComparativeDisplayRow> ComparativeStats { get; }
    public IReadOnlyList<WarningDisplayRow> WarningRows { get; }

    private static IReadOnlyList<StatDisplayRow> TeamTotals(ProcessingResult result, string side)
    {
        var stats = result.Stats.Where(stat => stat.Scope == StatScope.Team && EqualsSide(stat.Side, side)).ToList();
        return TeamTotalKeys
            .Select(key => stats.FirstOrDefault(stat => string.Equals(stat.StatKey, key, StringComparison.OrdinalIgnoreCase)))
            .Select((stat, index) => stat is null
                ? new StatDisplayRow(Label(TeamTotalKeys[index]), "-", "-", "-")
                : new StatDisplayRow(Label(stat.StatKey), DisplayStat(stat), Score(stat.Score), stat.Status.ToString()))
            .ToList();
    }

    private static IReadOnlyList<PlayerDisplayRow> Players(ProcessingResult result, string side)
    {
        return result.Players
            .Where(player => EqualsSide(player.Side, side))
            .Select(player =>
            {
                var stats = result.Stats.Where(stat => stat.Scope == StatScope.Player && string.Equals(stat.EntityId, player.EntityId, StringComparison.OrdinalIgnoreCase)).ToList();
                return new PlayerDisplayRow(
                    player.Number ?? "-",
                    player.FullName ?? player.EntityId,
                    Stat(stats, "minutes", minutes: true),
                    Ratio(stats, "fieldGoals"),
                    Stat(stats, "fieldGoals.percentage"),
                    Ratio(stats, "twoPoints"),
                    Stat(stats, "twoPoints.percentage"),
                    Ratio(stats, "threePoints"),
                    Stat(stats, "threePoints.percentage"),
                    Ratio(stats, "freeThrows"),
                    Stat(stats, "freeThrows.percentage"),
                    Stat(stats, "rebounds.offensive"),
                    Stat(stats, "rebounds.defensive"),
                    Stat(stats, "rebounds.total"),
                    Stat(stats, "assists"),
                    Stat(stats, "turnovers"),
                    Stat(stats, "steals"),
                    Stat(stats, "blocks"),
                    Stat(stats, "fouls.committed"),
                    Stat(stats, "fouls.drawn"),
                    Stat(stats, "plusMinus"),
                    Stat(stats, "evaluation"),
                    Stat(stats, "points"),
                    ProviderSummary(stats),
                    WarningSummary(stats));
            })
            .OrderBy(player => JerseyNumber(player.Number))
            .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<ComparativeDisplayRow> Comparatives(ProcessingResult result)
    {
        return result.Stats
            .Where(stat => stat.Scope == StatScope.Comparative)
            .GroupBy(stat => stat.StatKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ComparativeDisplayRow(
                Label(group.Key),
                Value(group.FirstOrDefault(stat => EqualsSide(stat.Side, "Home"))?.Value),
                Value(group.FirstOrDefault(stat => EqualsSide(stat.Side, "Away"))?.Value),
                Value(group.FirstOrDefault(stat => string.IsNullOrWhiteSpace(stat.Side))?.Value)))
            .OrderBy(row => row.Statistic, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<WarningDisplayRow> Warnings(ProcessingResult result)
    {
        var warnings = result.Validation.Warnings
            .Concat(result.Stats.SelectMany(stat => stat.Warnings))
            .Select(warning => new WarningDisplayRow(warning.RuleId, warning.Severity, warning.FieldId ?? string.Empty, warning.Message))
            .Concat(result.Stats.SelectMany(stat => stat.FailedRules.Select(rule => new WarningDisplayRow(rule, "FailedRule", stat.FieldId, "Regola di validazione non soddisfatta."))))
            .Concat(result.Validation.FailedRules.Select(rule => new WarningDisplayRow(rule, "FailedRule", string.Empty, "Regola di validazione non soddisfatta.")))
            .DistinctBy(row => $"{row.RuleId}|{row.Severity}|{row.FieldId}|{row.Message}", StringComparer.OrdinalIgnoreCase)
            .ToList();
        return warnings;
    }

    private static TeamStats? Team(ProcessingResult result, string side) =>
        result.Teams.FirstOrDefault(team => EqualsSide(team.Side, side));

    private static string TeamName(TeamStats? team, string? gameTeamId, string fallback) =>
        team?.Name ?? team?.Abbreviation ?? team?.TeamId ?? gameTeamId ?? fallback;

    private static (string Home, string Away) Scores(ProcessingResult result)
    {
        var home = result.Stats.FirstOrDefault(stat => stat.Scope == StatScope.Result && EqualsSide(stat.Side, "Home") && stat.StatKey == "points")?.Value;
        var away = result.Stats.FirstOrDefault(stat => stat.Scope == StatScope.Result && EqualsSide(stat.Side, "Away") && stat.StatKey == "points")?.Value;
        if (home is not null || away is not null)
        {
            return (Value(home), Value(away));
        }

        var parts = result.Game.FinalScore?.Split('-', 2, StringSplitOptions.TrimEntries);
        return parts?.Length == 2 ? (parts[0], parts[1]) : ("-", "-");
    }

    private static string Stat(IReadOnlyList<StatValue> stats, string key, bool minutes = false)
    {
        var value = stats.FirstOrDefault(stat => string.Equals(stat.StatKey, key, StringComparison.OrdinalIgnoreCase))?.Value;
        return minutes ? Minutes(value) : Value(value);
    }

    private static string Ratio(IReadOnlyList<StatValue> stats, string prefix)
    {
        var made = Stat(stats, $"{prefix}.made");
        var attempted = Stat(stats, $"{prefix}.attempted");
        return made == "-" && attempted == "-" ? "-" : $"{made}/{attempted}";
    }

    private static string ProviderSummary(IReadOnlyList<StatValue> stats)
    {
        var providers = stats
            .SelectMany(stat => !string.IsNullOrWhiteSpace(stat.Reconciliation?.SelectedProvider)
                ? [stat.Reconciliation!.SelectedProvider]
                : stat.Candidates.Select(candidate => candidate.Engine))
            .Where(provider => !string.IsNullOrWhiteSpace(provider))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return providers.Length switch
        {
            0 => "-",
            1 => providers[0],
            _ => "Mixed"
        };
    }

    private static string WarningSummary(IReadOnlyList<StatValue> stats)
    {
        var ruleIds = stats
            .SelectMany(stat => stat.Warnings.Select(warning => warning.RuleId).Concat(stat.FailedRules))
            .Where(ruleId => !string.IsNullOrWhiteSpace(ruleId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(ruleId => ruleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return ruleIds.Length switch
        {
            0 => "-",
            <= 3 => string.Join(", ", ruleIds),
            _ => $"{string.Join(", ", ruleIds.Take(3))} (+{ruleIds.Length - 3})"
        };
    }

    private static string DisplayStat(StatValue stat) =>
        stat.StatKey == "minutes" ? Minutes(stat.Value) : Value(stat.Value);

    private static string Minutes(object? value)
    {
        return TryLong(value, out var seconds)
            ? $"{seconds / 60}:{seconds % 60:00}"
            : Value(value);
    }

    private static string Value(object? value)
    {
        return value switch
        {
            null => "-",
            JsonElement element => Value(JsonValue(element)),
            double number => number.ToString("0.##", CultureInfo.InvariantCulture),
            decimal number => number.ToString("0.##", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "-"
        };
    }

    private static object? JsonValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };

    private static bool TryLong(object? value, out long number) =>
        long.TryParse(Value(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out number);

    private static bool EqualsSide(string? actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static int JerseyNumber(string value) =>
        int.TryParse(value.TrimStart('*'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : int.MaxValue;

    private static string Label(string key) => Labels.TryGetValue(key, out var label) ? label : key;
    private static string Score(double score) => score.ToString("0.00", CultureInfo.InvariantCulture);
    private static string Join(params string?[] values)
    {
        var text = string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
        return text.Length > 0 ? text : "-";
    }
}

public sealed record PeriodDisplayRow(string Period, string Home, string Away);
public sealed record StatDisplayRow(string Statistic, string Value, string Confidence, string Status);
public sealed record PlayerDisplayRow(
    string Number,
    string Name,
    string Minutes,
    string FieldGoals,
    string FieldGoalsPercentage,
    string TwoPoints,
    string TwoPointsPercentage,
    string ThreePoints,
    string ThreePointsPercentage,
    string FreeThrows,
    string FreeThrowsPercentage,
    string ReboundsOffensive,
    string ReboundsDefensive,
    string Rebounds,
    string Assists,
    string Turnovers,
    string Steals,
    string Blocks,
    string Fouls,
    string FoulsDrawn,
    string PlusMinus,
    string Evaluation,
    string Points,
    string Provider,
    string Warnings);
public sealed record ComparativeDisplayRow(string Statistic, string Home, string Away, string Value);
public sealed record WarningDisplayRow(string RuleId, string Severity, string FieldId, string Message);
