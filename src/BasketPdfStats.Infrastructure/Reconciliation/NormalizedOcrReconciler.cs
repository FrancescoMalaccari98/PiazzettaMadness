using System.Globalization;
using System.Text.Json;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Validation;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Infrastructure.Reconciliation;

public sealed class NormalizedOcrReconciler
{
    private const string Strategy = "Weighted field-by-field";
    private readonly IStatsValidator _validator;
    private readonly EngineWeightOptions _weights;

    public NormalizedOcrReconciler(IStatsValidator? validator = null, EngineWeightOptions? weights = null)
    {
        _validator = validator ?? new StatsValidationService();
        _weights = weights ?? EngineWeightOptions.Defaults();
    }

    public ProcessingResult Reconcile(IReadOnlyList<ProcessingResult> normalizedResults)
    {
        var sources = normalizedResults
            .Select(result => new ProviderResult(ProviderName(result), result))
            .Where(source => !string.IsNullOrWhiteSpace(source.ProviderName))
            .DistinctBy(source => source.ProviderName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (sources.Length == 0)
        {
            throw new InvalidOperationException("At least one normalized OCR provider result is required for reconciliation.");
        }

        var final = Clone(sources[0].Result);
        final.Reconciliation = new ReconciliationMetadata
        {
            Enabled = true,
            Providers = sources.Select(source => source.ProviderName).ToList(),
            Strategy = Strategy
        };
        final.Validation = CreateBaseValidation(sources);
        final.OcrRuns = sources
            .SelectMany(source => source.Result.OcrRuns)
            .DistinctBy(run => $"{run.Engine}|{run.Status}|{run.Error}", StringComparer.OrdinalIgnoreCase)
            .Select(run => Clone(run))
            .ToList();
        MergeGame(final, sources);
        MergeTeams(final, sources);
        MergePlayers(final, sources);
        final.Stats = MergeStats(final, sources, _weights);
        ApplyMathTiebreak(final);
        ApplyTeamPointsConsistency(final);
        _validator.Validate(final);
        return final;
    }

    private static List<StatValue> MergeStats(ProcessingResult final, IReadOnlyList<ProviderResult> sources, EngineWeightOptions weights)
    {
        var fields = sources
            .SelectMany(source => source.Result.Stats.Select(stat => new ProviderStat(source.ProviderName, stat)))
            .GroupBy(item => Key(item.Stat), StringComparer.OrdinalIgnoreCase);
        var merged = new List<StatValue>();
        foreach (var group in fields)
        {
            var values = group
                .GroupBy(item => item.ProviderName, StringComparer.OrdinalIgnoreCase)
                .Select(provider => provider
                    .OrderBy(item => RelevantFailureCount(item.Stat))
                    .ThenByDescending(item => item.Stat.Score)
                    .First())
                .ToArray();
            var valueGroups = values.GroupBy(item => Canonical(item.Stat.Value), StringComparer.Ordinal).ToArray();
            // Agreement weighted by channel: an agreeing group of strong channels
            // outweighs a larger group of weak ones (no blind majority vote).
            var agreed = valueGroups
                .OrderByDescending(value => value.Sum(item => WeightedScore(item, weights)))
                .ThenByDescending(value => value.Count())
                .First();
            var hasConflict = valueGroups.Length > 1;
            var missingProviders = sources
                .Select(source => source.ProviderName)
                .Except(values.Select(value => value.ProviderName), StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var winner = hasConflict
                ? values
                    .OrderBy(item => RelevantFailureCount(item.Stat))
                    .ThenByDescending(item => WeightedScore(item, weights))
                    .ThenBy(item => ProviderOrder(item.ProviderName, sources))
                    .First()
                : agreed
                    .OrderByDescending(item => WeightedScore(item, weights))
                    .ThenBy(item => ProviderOrder(item.ProviderName, sources))
                    .First();
            var stat = Clone(winner.Stat);
            stat.Value = NormalizeJsonValue(stat.Value);
            ResetMathDiagnostics(stat);
            stat.Candidates = values
                .SelectMany(item => item.Stat.Candidates)
                .Select(candidate =>
                {
                    var cloned = Clone(candidate);
                    cloned.Normalized = NormalizeJsonValue(cloned.Normalized);
                    return cloned;
                })
                .ToList();
            stat.Reconciliation = new StatReconciliationMetadata
            {
                SelectedProvider = winner.ProviderName,
                AgreedProviders = agreed.Select(item => item.ProviderName).ToList(),
                ProviderValues = values.ToDictionary(item => item.ProviderName, item => NormalizeJsonValue(item.Stat.Value), StringComparer.OrdinalIgnoreCase)
            };
            if (!hasConflict && missingProviders.Length == 0 && sources.Count > 1)
            {
                stat.Score = Math.Max(stat.Score, 0.95);
            }
            else if (missingProviders.Length > 0)
            {
                stat.Score = Math.Min(stat.Score, 0.75);
                AddWarning(final, stat, "ocr.reconciliation.missingProviderValue",
                    $"{stat.FieldId}: value selected from {winner.ProviderName}; missing value from {string.Join(", ", missingProviders)}.");
            }

            if (hasConflict)
            {
                stat.Score = Math.Min(stat.Score, 0.60);
                AddWarning(final, stat, "ocr.reconciliation.conflict",
                    $"{stat.FieldId}: conflicting OCR values ({ProviderValues(values)}); selected {winner.ProviderName}.");
            }

            merged.Add(stat);
        }

        return merged;
    }

    /// <summary>
    /// After weighted selection, override a conflicting field with a candidate that
    /// makes a basketball formula consistent — even if that candidate is a minority
    /// or lower-weighted channel. Math, not the vote, is the final arbiter.
    /// </summary>
    private static void ApplyMathTiebreak(ProcessingResult final)
    {
        var entities = final.Stats
            .Where(stat => stat.Scope is StatScope.Player or StatScope.Team)
            .GroupBy(stat => $"{stat.Scope}|{stat.EntityId}");
        foreach (var entity in entities)
        {
            var map = new Dictionary<string, StatValue>(StringComparer.OrdinalIgnoreCase);
            foreach (var stat in entity)
            {
                map.TryAdd(stat.StatKey, stat);
            }

            TryFormulaTiebreak(final, map, "points", ["twoPoints.made", "threePoints.made", "freeThrows.made"], v => 2 * v[0] + 3 * v[1] + v[2]);
            TryFormulaTiebreak(final, map, "fieldGoals.made", ["twoPoints.made", "threePoints.made"], v => v[0] + v[1]);
            TryFormulaTiebreak(final, map, "fieldGoals.attempted", ["twoPoints.attempted", "threePoints.attempted"], v => v[0] + v[1]);
            TryFormulaTiebreak(final, map, "rebounds.total", ["rebounds.offensive", "rebounds.defensive"], v => v[0] + v[1]);
        }
    }

    private static void TryFormulaTiebreak(
        ProcessingResult final,
        Dictionary<string, StatValue> map,
        string targetKey,
        IReadOnlyList<string> operandKeys,
        Func<double[], double> formula)
    {
        if (!map.TryGetValue(targetKey, out var target) || !TryNumber(target.Value, out var actual))
        {
            return;
        }

        var operandValues = new double[operandKeys.Count];
        for (var i = 0; i < operandKeys.Count; i++)
        {
            if (!map.TryGetValue(operandKeys[i], out var operand) || !TryNumber(operand.Value, out operandValues[i]))
            {
                return;
            }
        }

        var expected = formula(operandValues);
        if (Math.Abs(actual - expected) < 0.001)
        {
            return; // already consistent — nothing to override
        }

        // Look for a candidate value (from any channel) that satisfies the formula.
        var match = target.Candidates.FirstOrDefault(candidate =>
            TryNumber(NormalizeJsonValue(candidate.Normalized), out var value) && Math.Abs(value - expected) < 0.001);
        if (match is null)
        {
            return;
        }

        var selectedFrom = match.SourceId ?? match.Engine;
        target.Value = NormalizeJsonValue(match.Normalized);
        target.Reconciliation ??= new StatReconciliationMetadata();
        target.Reconciliation.SelectedProvider = selectedFrom;
        AddWarning(final, target, "ocr.reconciliation.mathTiebreak",
            $"{target.FieldId}: selected math-consistent value {Format(expected)} from {selectedFrom} (weighted pick was {Format(actual)}).");
    }

    /// <summary>
    /// Cross-entity check: team points and the final score must equal the sum of
    /// player points. Player rows (read from the table) are more reliable than the
    /// small totals cells, so an inconsistent team/result value is corrected to the
    /// player sum, with a traceable warning. There are no extra "team points", so
    /// the sum is the mathematical truth.
    /// </summary>
    private static void ApplyTeamPointsConsistency(ProcessingResult final)
    {
        foreach (var side in new[] { "Home", "Away" })
        {
            var playerPoints = final.Stats
                .Where(stat => stat.Scope == StatScope.Player &&
                               string.Equals(stat.Side, side, StringComparison.OrdinalIgnoreCase) &&
                               stat.StatKey == "points")
                .Select(stat => TryNumber(stat.Value, out var value) ? (double?)value : null)
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();
            // Require a realistic roster before trusting the player-points sum: a
            // FIBA side always has at least five players. Below that the points are
            // too partial to override the totals.
            if (playerPoints.Count < 5)
            {
                continue;
            }

            var sum = playerPoints.Sum();
            CorrectPointsToSum(final, StatScope.Team, side, sum, "team");
            CorrectPointsToSum(final, StatScope.Result, side, sum, "final-score");
        }

        RebuildFinalScore(final);
    }

    private static void CorrectPointsToSum(ProcessingResult final, StatScope scope, string side, double sum, string label)
    {
        var stat = final.Stats.FirstOrDefault(item =>
            item.Scope == scope &&
            string.Equals(item.Side, side, StringComparison.OrdinalIgnoreCase) &&
            item.StatKey == "points");
        if (stat is null || !TryNumber(stat.Value, out var current) || Math.Abs(current - sum) < 0.001)
        {
            return;
        }

        stat.Value = (int)Math.Round(sum);
        AddWarning(final, stat, "ocr.reconciliation.pointsFromPlayerSum",
            $"{label} {side} points {Format(current)} is inconsistent with the player-points sum {Format(sum)}; corrected to {Format(sum)}.");
    }

    private static void RebuildFinalScore(ProcessingResult final)
    {
        var home = final.Stats.FirstOrDefault(s => s.Scope == StatScope.Result && string.Equals(s.Side, "Home", StringComparison.OrdinalIgnoreCase) && s.StatKey == "points");
        var away = final.Stats.FirstOrDefault(s => s.Scope == StatScope.Result && string.Equals(s.Side, "Away", StringComparison.OrdinalIgnoreCase) && s.StatKey == "points");
        if (home is not null && away is not null && TryNumber(home.Value, out var h) && TryNumber(away.Value, out var a))
        {
            final.Game.FinalScore = $"{(int)Math.Round(h)}-{(int)Math.Round(a)}";
        }
    }

    private static bool TryNumber(object? value, out double number)
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
                number = l;
                return true;
            case double d:
                number = d;
                return true;
            case decimal m:
                number = (double)m;
                return true;
            case float f:
                number = f;
                return true;
            default:
                return double.TryParse(
                    Convert.ToString(value, CultureInfo.InvariantCulture),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out number);
        }
    }

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static ValidationResult CreateBaseValidation(IEnumerable<ProviderResult> sources)
    {
        var validation = new ValidationResult();
        foreach (var warning in sources
            .SelectMany(source => source.Result.Validation.Warnings)
            .Where(warning => !IsMathRule(warning.RuleId))
            .DistinctBy(warning => $"{warning.RuleId}|{warning.FieldId}|{warning.Message}", StringComparer.OrdinalIgnoreCase))
        {
            validation.Warnings.Add(Clone(warning));
        }

        return validation;
    }

    private static void MergeGame(ProcessingResult final, IReadOnlyList<ProviderResult> sources)
    {
        final.Game = Clone(sources[0].Result.Game);
        foreach (var source in sources.Skip(1))
        {
            final.Game.Competition = MergeText(final, "game.competition", final.Game.Competition, source.Result.Game.Competition);
            final.Game.Venue = MergeText(final, "game.venue", final.Game.Venue, source.Result.Game.Venue);
            final.Game.Date = MergeText(final, "game.date", final.Game.Date, source.Result.Game.Date);
            final.Game.Time = MergeText(final, "game.time", final.Game.Time, source.Result.Game.Time);
            final.Game.Number = MergeText(final, "game.number", final.Game.Number, source.Result.Game.Number);
            final.Game.Spectators ??= source.Result.Game.Spectators;
            final.Game.Duration = MergeText(final, "game.duration", final.Game.Duration, source.Result.Game.Duration);
            final.Game.ReportCreated = MergeText(final, "game.reportCreated", final.Game.ReportCreated, source.Result.Game.ReportCreated);
            final.Game.HomeTeamId = MergeText(final, "game.homeTeamId", final.Game.HomeTeamId, source.Result.Game.HomeTeamId);
            final.Game.AwayTeamId = MergeText(final, "game.awayTeamId", final.Game.AwayTeamId, source.Result.Game.AwayTeamId);
            final.Game.FinalScore = MergeText(final, "game.finalScore", final.Game.FinalScore, source.Result.Game.FinalScore);
            foreach (var referee in source.Result.Game.Referees.Where(referee => !final.Game.Referees.Contains(referee, StringComparer.OrdinalIgnoreCase)))
            {
                final.Game.Referees.Add(referee);
            }

            foreach (var period in source.Result.Game.Periods)
            {
                var existing = final.Game.Periods.FirstOrDefault(candidate => string.Equals(candidate.Period, period.Period, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    final.Game.Periods.Add(Clone(period));
                }
                else
                {
                    AddObjectConflictWarning(final, $"game.period.{period.Period}.home", existing.Home, period.Home);
                    AddObjectConflictWarning(final, $"game.period.{period.Period}.away", existing.Away, period.Away);
                    existing.Home ??= period.Home;
                    existing.Away ??= period.Away;
                }
            }
        }
    }

    private static void MergeTeams(ProcessingResult final, IReadOnlyList<ProviderResult> sources)
    {
        final.Teams = [];
        foreach (var team in sources.SelectMany(source => source.Result.Teams))
        {
            var existing = final.Teams.FirstOrDefault(candidate => string.Equals(candidate.Side, team.Side, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                final.Teams.Add(Clone(team));
                continue;
            }

            existing.Name = MergeText(final, $"team.{existing.Side}.name", existing.Name, team.Name);
            existing.Abbreviation = MergeText(final, $"team.{existing.Side}.abbreviation", existing.Abbreviation, team.Abbreviation);
            existing.Coach = MergeText(final, $"team.{existing.Side}.coach", existing.Coach, team.Coach);
            existing.AssistantCoach = MergeText(final, $"team.{existing.Side}.assistantCoach", existing.AssistantCoach, team.AssistantCoach);
        }
    }

    private static void MergePlayers(ProcessingResult final, IReadOnlyList<ProviderResult> sources)
    {
        final.Players = [];
        foreach (var player in sources.SelectMany(source => source.Result.Players))
        {
            var existing = final.Players.FirstOrDefault(candidate => string.Equals(PlayerKey(candidate), PlayerKey(player), StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                final.Players.Add(Clone(player));
                continue;
            }

            existing.Number = MergeText(final, $"{PlayerKey(existing)}.number", existing.Number, player.Number);
            existing.FullName = MergeText(final, $"{PlayerKey(existing)}.fullName", existing.FullName, player.FullName);
            existing.FirstName = MergeText(final, $"{PlayerKey(existing)}.firstName", existing.FirstName, player.FirstName);
            existing.LastName = MergeText(final, $"{PlayerKey(existing)}.lastName", existing.LastName, player.LastName);
            existing.SourceEntityId ??= player.SourceEntityId;
            existing.Captain |= player.Captain;
            existing.DidNotPlay &= player.DidNotPlay;
        }
    }

    private static string PlayerKey(PlayerStats player)
    {
        var side = player.Side ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(player.EntityId))
        {
            return $"{side}|id:{player.EntityId}";
        }

        if (!string.IsNullOrWhiteSpace(player.Number))
        {
            return $"{side}|jersey:{player.Number}";
        }

        return $"{side}|name:{Normalize(player.FullName)}";
    }

    private static string Key(StatValue stat)
    {
        var discriminant = string.IsNullOrWhiteSpace(stat.FieldId) ? stat.StatKey : stat.FieldId;
        return $"{stat.Scope}|{stat.Side}|{stat.EntityId}|{discriminant}";
    }

    private static int RelevantFailureCount(StatValue stat)
    {
        return stat.FailedRules.Count + stat.Warnings.Count(warning => IsMathRule(warning.RuleId));
    }

    private static double WeightedScore(ProviderStat item, EngineWeightOptions weights)
    {
        return item.Stat.Score * weights.WeightFor(item.ProviderName, item.Stat.Scope);
    }

    private static void ResetMathDiagnostics(StatValue stat)
    {
        stat.FailedRules.RemoveAll(IsMathRule);
        stat.Warnings.RemoveAll(warning => IsMathRule(warning.RuleId));
    }

    private static bool IsMathRule(string ruleId)
    {
        return ruleId.StartsWith("math.", StringComparison.OrdinalIgnoreCase);
    }

    private static string ProviderName(ProcessingResult result)
    {
        return result.OcrRuns.FirstOrDefault(run => run.Status == OcrRunStatus.Success)?.Engine
               ?? result.OcrRuns.FirstOrDefault()?.Engine
               ?? string.Empty;
    }

    private static int ProviderOrder(string providerName, IReadOnlyList<ProviderResult> sources)
    {
        return sources.ToList().FindIndex(source => string.Equals(source.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));
    }

    private static string ProviderValues(IEnumerable<ProviderStat> values)
    {
        return string.Join(", ", values.Select(value => $"{value.ProviderName}={Canonical(value.Stat.Value)}"));
    }

    private static string Canonical(object? value)
    {
        return value is null ? "null" : JsonSerializer.Serialize(NormalizeJsonValue(value), JsonDefaults.Options);
    }

    private static object? NormalizeJsonValue(object? value)
    {
        if (value is not JsonElement element)
        {
            return value;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var integer) => integer,
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number when element.TryGetDecimal(out var number) => number,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }

    private static string Normalize(string? value)
    {
        return new string((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    private static void AddWarning(ProcessingResult result, StatValue stat, string ruleId, string message)
    {
        var warning = new ValidationWarning
        {
            RuleId = ruleId,
            Severity = "Warning",
            FieldId = stat.FieldId,
            FieldIds = [stat.FieldId],
            Message = message
        };
        stat.Warnings.Add(warning);
        stat.Status = StatValidationStatus.NonValidato;
        result.Validation.Warnings.Add(warning);
    }

    private static string? MergeText(ProcessingResult result, string field, string? current, string? incoming)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return incoming;
        }

        AddObjectConflictWarning(result, field, current, incoming);
        return current;
    }

    private static void AddObjectConflictWarning(ProcessingResult result, string field, object? current, object? incoming)
    {
        if (current is null || incoming is null || string.Equals(Canonical(current), Canonical(incoming), StringComparison.Ordinal))
        {
            return;
        }

        result.Validation.Warnings.Add(new ValidationWarning
        {
            RuleId = "ocr.reconciliation.objectConflict",
            Severity = "Warning",
            Message = $"{field}: normalized OCR providers returned different values; the first selected provider value was preserved."
        });
    }

    private static T Clone<T>(T value)
    {
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, JsonDefaults.Options), JsonDefaults.Options)!;
    }

    private sealed record ProviderResult(string ProviderName, ProcessingResult Result);
    private sealed record ProviderStat(string ProviderName, StatValue Stat);
}
