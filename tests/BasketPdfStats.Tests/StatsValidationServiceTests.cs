using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Validation;

namespace BasketPdfStats.Tests;

public sealed class StatsValidationServiceTests
{
    [Fact]
    public void Validates_points_formula_and_marks_involved_stats_valid()
    {
        var result = new ProcessingResult
        {
            Stats =
            [
                Stat("player:MIA:row:1", "points", 12),
                Stat("player:MIA:row:1", "twoPoints.made", 3),
                Stat("player:MIA:row:1", "threePoints.made", 1),
                Stat("player:MIA:row:1", "freeThrows.made", 3)
            ]
        };

        new StatsValidationService().Validate(result);

        Assert.Empty(result.Validation.Warnings);
        Assert.All(result.Stats, stat => Assert.Equal(StatValidationStatus.Validato, stat.Status));
        Assert.All(result.Stats, stat => Assert.True(stat.Score >= 0.85));
    }

    [Fact]
    public void Adds_failed_rules_when_points_formula_fails()
    {
        var result = new ProcessingResult
        {
            Stats =
            [
                Stat("player:MIA:row:1", "points", 10),
                Stat("player:MIA:row:1", "twoPoints.made", 3),
                Stat("player:MIA:row:1", "threePoints.made", 1),
                Stat("player:MIA:row:1", "freeThrows.made", 3)
            ]
        };

        new StatsValidationService().Validate(result);

        Assert.Contains(result.Validation.Warnings, x => x.RuleId == "math.pointsFormula");
        Assert.Contains(result.Stats.Single(x => x.StatKey == "points").FailedRules, x => x == "math.pointsFormula");
        Assert.Equal(StatValidationStatus.NonValidato, result.Stats.Single(x => x.StatKey == "points").Status);
        Assert.True(result.Stats.Single(x => x.StatKey == "points").Score <= 0.40);
    }

    [Fact]
    public void Counts_logical_failed_rules_separately_from_failed_stat_links()
    {
        var result = new ProcessingResult
        {
            Stats =
            [
                Stat("player:MIA:row:1", "points", 10),
                Stat("player:MIA:row:1", "twoPoints.made", 3),
                Stat("player:MIA:row:1", "threePoints.made", 1),
                Stat("player:MIA:row:1", "freeThrows.made", 3)
            ]
        };

        new StatsValidationService().Validate(result);

        var logicalFailedRuleCount = result.Validation.FailedRules.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var failedStatLinksCount = result.Stats.Sum(x => x.FailedRules.Count);

        Assert.Equal(1, logicalFailedRuleCount);
        Assert.Equal(4, failedStatLinksCount);
    }

    [Fact]
    public void Points_formula_failures_on_two_entities_are_two_logical_instances()
    {
        var result = new ProcessingResult
        {
            Stats =
            [
                Stat("player:MIA:row:1", "points", 10),
                Stat("player:MIA:row:1", "twoPoints.made", 3),
                Stat("player:MIA:row:1", "threePoints.made", 1),
                Stat("player:MIA:row:1", "freeThrows.made", 3),
                Stat("team:SPR", "points", 53),
                Stat("team:SPR", "twoPoints.made", 14),
                Stat("team:SPR", "threePoints.made", 9),
                Stat("team:SPR", "freeThrows.made", 10)
            ]
        };

        new StatsValidationService().Validate(result);

        var logicalFailedRuleCount = result.Validation.Warnings
            .Where(x => x.RuleId == "math.pointsFormula")
            .Select(x => x.FieldIds.First())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var distinctFailedRuleTypeCount = result.Validation.FailedRules.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var failedStatLinksCount = result.Stats.Sum(x => x.FailedRules.Count);

        Assert.Equal(2, logicalFailedRuleCount);
        Assert.Equal(1, distinctFailedRuleTypeCount);
        Assert.Equal(8, failedStatLinksCount);
    }

    [Fact]
    public void Validates_field_goal_made_and_attempted_formulas()
    {
        var result = new ProcessingResult
        {
            Stats =
            [
                Stat("team:MIA", "fieldGoals.made", 4),
                Stat("team:MIA", "twoPoints.made", 3),
                Stat("team:MIA", "threePoints.made", 1),
                Stat("team:MIA", "fieldGoals.attempted", 9),
                Stat("team:MIA", "twoPoints.attempted", 5),
                Stat("team:MIA", "threePoints.attempted", 4)
            ]
        };

        new StatsValidationService().Validate(result);

        Assert.DoesNotContain(result.Validation.Warnings, x => x.RuleId is "math.fieldGoalsMade" or "math.fieldGoalsAttempted");
    }

    [Fact]
    public void Treats_team_rebounds_total_mismatch_as_soft_diagnostic()
    {
        var result = new ProcessingResult
        {
            Stats =
            [
                Stat("team:MIA", "rebounds.offensive", 8),
                Stat("team:MIA", "rebounds.defensive", 21),
                Stat("team:MIA", "rebounds.total", 30)
            ]
        };

        new StatsValidationService().Validate(result);

        var warning = Assert.Single(result.Validation.Warnings, x => x.RuleId == "math.reboundsTotal");
        Assert.Equal("Info", warning.Severity);
        Assert.Empty(result.Validation.FailedRules);
        Assert.DoesNotContain(result.Stats.Single(x => x.StatKey == "rebounds.total").FailedRules, x => x == "math.reboundsTotal");
        Assert.True(result.Stats.Single(x => x.StatKey == "rebounds.total").Score <= 0.70);
    }

    [Fact]
    public void Validates_player_rebounds_total_formula_as_strong_rule()
    {
        var result = new ProcessingResult
        {
            Stats =
            [
                Stat("player:MIA:row:1", "rebounds.offensive", 2),
                Stat("player:MIA:row:1", "rebounds.defensive", 5),
                Stat("player:MIA:row:1", "rebounds.total", 9)
            ]
        };

        new StatsValidationService().Validate(result);

        var warning = Assert.Single(result.Validation.Warnings, x => x.RuleId == "math.reboundsTotal");
        Assert.Equal("Warning", warning.Severity);
        Assert.Contains("math.reboundsTotal", result.Validation.FailedRules);
        Assert.Contains(result.Stats.Single(x => x.StatKey == "rebounds.total").FailedRules, x => x == "math.reboundsTotal");
    }

    [Fact]
    public void Validates_period_sum_against_final_result()
    {
        var result = new ProcessingResult
        {
            Stats =
            [
                ResultStat("Home", "points", "result.home.points", 51),
                ResultStat("Home", "period.points", "result.period.Q1.home", 21),
                ResultStat("Home", "period.points", "result.period.Q2.home", 30)
            ]
        };

        new StatsValidationService().Validate(result);

        Assert.Empty(result.Validation.Warnings);
        Assert.All(result.Stats, stat => Assert.Equal(StatValidationStatus.Validato, stat.Status));
    }

    [Fact]
    public void Uses_stable_rule_id_for_period_sum_failures()
    {
        var result = new ProcessingResult
        {
            Stats =
            [
                ResultStat("Home", "points", "result.home.points", 51),
                ResultStat("Home", "period.points", "result.period.Q1.home", 21),
                ResultStat("Home", "period.points", "result.period.Q2.home", 20)
            ]
        };

        new StatsValidationService().Validate(result);

        Assert.Contains(result.Validation.Warnings, x => x.RuleId == "math.periodSum");
        Assert.Contains("math.periodSum", result.Validation.FailedRules);
    }

    private static StatValue Stat(string entityId, string statKey, object value)
    {
        return new StatValue
        {
            Scope = entityId.StartsWith("team:", StringComparison.OrdinalIgnoreCase) ? StatScope.Team : StatScope.Player,
            EntityId = entityId,
            FieldId = $"{entityId}:{statKey}",
            StatKey = statKey,
            Value = value,
            Score = 0.65,
            Status = StatValidationStatus.NonValidato
        };
    }

    private static StatValue ResultStat(string side, string statKey, string fieldId, object value)
    {
        return new StatValue
        {
            Scope = StatScope.Result,
            EntityId = "game:test",
            Side = side,
            FieldId = fieldId,
            StatKey = statKey,
            Value = value,
            Score = 0.65,
            Status = StatValidationStatus.NonValidato
        };
    }
}
