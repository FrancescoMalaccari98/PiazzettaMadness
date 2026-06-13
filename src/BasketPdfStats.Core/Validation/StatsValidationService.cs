using System.Globalization;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Core.Validation;

public sealed class StatsValidationService : IStatsValidator
{
    public void Validate(ProcessingResult result)
    {
        foreach (var group in result.Stats
            .Where(x => x.Scope is StatScope.Team or StatScope.Player)
            .GroupBy(x => new { x.Scope, x.EntityId, x.Side }))
        {
            ValidateEntity(result, group.ToList());
        }

        ValidatePeriodTotals(result);
        ResolveValidationStatus(result);
    }

    private static void ValidateEntity(ProcessingResult result, IReadOnlyList<StatValue> stats)
    {
        ValidateSumRule(result, "math.pointsFormula", stats, "points", ["twoPoints.made", "threePoints.made", "freeThrows.made"],
            values => values[0] * 2 + values[1] * 3 + values[2],
            "Punti non coerenti con 2P, 3P e tiri liberi realizzati.");
        ValidateSumRule(result, "math.fieldGoalsMade", stats, "fieldGoals.made", ["twoPoints.made", "threePoints.made"],
            values => values[0] + values[1],
            "Tiri dal campo realizzati non coerenti con 2P+3P.");
        ValidateSumRule(result, "math.fieldGoalsAttempted", stats, "fieldGoals.attempted", ["twoPoints.attempted", "threePoints.attempted"],
            values => values[0] + values[1],
            "Tiri dal campo tentati non coerenti con 2P+3P.");

        if (stats.Any(x => x.Scope == StatScope.Team))
        {
            ValidateTeamReboundsDiagnostic(result, stats);
        }
        else
        {
            ValidateSumRule(result, "math.reboundsTotal", stats, "rebounds.total", ["rebounds.offensive", "rebounds.defensive"],
                values => values[0] + values[1],
                "Rimbalzi totali non coerenti con offensivi+difensivi.");
        }
    }

    private static void ValidateSumRule(
        ProcessingResult result,
        string ruleId,
        IReadOnlyList<StatValue> stats,
        string targetKey,
        IReadOnlyList<string> operandKeys,
        Func<IReadOnlyList<double>, double> expectedFactory,
        string message)
    {
        var target = First(stats, targetKey);
        var operands = operandKeys.Select(key => First(stats, key)).ToArray();
        if (target is null || operands.Any(x => x is null))
        {
            return;
        }

        if (!TryNumber(target.Value, out var actual))
        {
            return;
        }

        var values = new List<double>();
        foreach (var operand in operands)
        {
            if (!TryNumber(operand!.Value, out var value))
            {
                return;
            }

            values.Add(value);
        }

        var expected = expectedFactory(values);
        var involved = operands.Prepend(target).Where(x => x is not null).Cast<StatValue>().ToArray();
        if (Math.Abs(actual - expected) > 0.001)
        {
            AddFailure(result, involved, ruleId, $"{message} Valore={Format(actual)}, atteso={Format(expected)}.");
            return;
        }

        MarkPassed(involved);
    }

    private static void ValidatePeriodTotals(ProcessingResult result)
    {
        foreach (var side in new[] { "Home", "Away" })
        {
            var resultPoints = result.Stats.FirstOrDefault(x =>
                x.Scope == StatScope.Result &&
                string.Equals(x.Side, side, StringComparison.OrdinalIgnoreCase) &&
                x.StatKey == "points");
            var periodPoints = result.Stats
                .Where(x => x.Scope == StatScope.Result &&
                            string.Equals(x.Side, side, StringComparison.OrdinalIgnoreCase) &&
                            x.StatKey == "period.points")
                .ToArray();

            if (resultPoints is null || periodPoints.Length == 0 || !TryNumber(resultPoints.Value, out var actual))
            {
                continue;
            }

            var sum = 0d;
            foreach (var period in periodPoints)
            {
                if (!TryNumber(period.Value, out var value))
                {
                    return;
                }

                sum += value;
            }

            var involved = periodPoints.Prepend(resultPoints).ToArray();
            if (Math.Abs(actual - sum) > 0.001)
            {
                AddFailure(result, involved, "math.periodSum", $"Somma periodi {side} non coerente con risultato finale. Valore={Format(actual)}, atteso={Format(sum)}.");
                continue;
            }

            MarkPassed(involved);
        }
    }

    private static StatValue? First(IReadOnlyList<StatValue> stats, string statKey)
    {
        return stats.FirstOrDefault(x => string.Equals(x.StatKey, statKey, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryNumber(object? value, out double number)
    {
        number = 0;
        return value switch
        {
            null => false,
            int i => Set(i, out number),
            long l => Set(l, out number),
            float f => Set(f, out number),
            double d => Set(d, out number),
            decimal m => Set((double)m, out number),
            string s => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out number),
            _ => double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
        };
    }

    private static void ValidateTeamReboundsDiagnostic(ProcessingResult result, IReadOnlyList<StatValue> stats)
    {
        var total = First(stats, "rebounds.total");
        var offensive = First(stats, "rebounds.offensive");
        var defensive = First(stats, "rebounds.defensive");
        if (total is null || offensive is null || defensive is null ||
            !TryNumber(total.Value, out var actual) ||
            !TryNumber(offensive.Value, out var offensiveValue) ||
            !TryNumber(defensive.Value, out var defensiveValue))
        {
            return;
        }

        var expected = offensiveValue + defensiveValue;
        var involved = new[] { total, offensive, defensive };
        if (Math.Abs(actual - expected) > 0.001)
        {
            AddSoftDiagnostic(result, involved, "math.reboundsTotal",
                $"Rimbalzi squadra potenzialmente ambigui: totale={Format(actual)}, offensivi+difensivi={Format(expected)}. Il PDF potrebbe includere rimbalzi di squadra non separati.");
            return;
        }

        MarkPassed(involved);
    }

    private static bool Set(double value, out double number)
    {
        number = value;
        return true;
    }

    private static void AddFailure(ProcessingResult result, IReadOnlyList<StatValue> stats, string ruleId, string message)
    {
        if (!result.Validation.FailedRules.Contains(ruleId, StringComparer.OrdinalIgnoreCase))
        {
            result.Validation.FailedRules.Add(ruleId);
        }

        var warning = new ValidationWarning
        {
            RuleId = ruleId,
            Severity = "Warning",
            FieldId = stats.FirstOrDefault()?.FieldId,
            FieldIds = stats.Select(x => x.FieldId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Message = message
        };
        AddValidationWarning(result, warning);

        foreach (var stat in stats)
        {
            if (!stat.FailedRules.Contains(ruleId, StringComparer.OrdinalIgnoreCase))
            {
                stat.FailedRules.Add(ruleId);
            }

            stat.Warnings.Add(warning);
            stat.Score = Math.Min(stat.Score, 0.40);
            stat.Status = StatValidationStatus.NonValidato;
        }
    }

    private static void AddSoftDiagnostic(ProcessingResult result, IReadOnlyList<StatValue> stats, string ruleId, string message)
    {
        var warning = new ValidationWarning
        {
            RuleId = ruleId,
            Severity = "Info",
            FieldId = stats.FirstOrDefault()?.FieldId,
            FieldIds = stats.Select(x => x.FieldId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Message = message
        };
        AddValidationWarning(result, warning);

        foreach (var stat in stats)
        {
            stat.Warnings.Add(warning);
            stat.Score = Math.Min(stat.Score, 0.70);
        }
    }

    private static void AddValidationWarning(ProcessingResult result, ValidationWarning warning)
    {
        if (result.Validation.Warnings.Any(existing =>
                string.Equals(existing.RuleId, warning.RuleId, StringComparison.OrdinalIgnoreCase) &&
                existing.FieldIds.SequenceEqual(warning.FieldIds, StringComparer.OrdinalIgnoreCase) &&
                string.Equals(existing.Message, warning.Message, StringComparison.Ordinal)))
        {
            return;
        }

        result.Validation.Warnings.Add(warning);
    }

    private static void MarkPassed(IEnumerable<StatValue> stats)
    {
        foreach (var stat in stats)
        {
            if (stat.Warnings.Count == 0 && stat.FailedRules.Count == 0)
            {
                stat.Score = Math.Max(stat.Score, 0.85);
                stat.Status = StatValidationStatus.Validato;
            }
        }
    }

    private static void ResolveValidationStatus(ProcessingResult result)
    {
        if (result.Validation.Warnings.Any(x => string.Equals(x.Severity, "Error", StringComparison.OrdinalIgnoreCase)))
        {
            result.Validation.Status = FileProcessingStatus.CompletedNotValidated;
            return;
        }

        result.Validation.Status = result.Validation.Warnings.Count == 0
            ? FileProcessingStatus.CompletedValidated
            : FileProcessingStatus.CompletedWithWarnings;
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
