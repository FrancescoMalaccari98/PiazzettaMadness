using System.Text.Json;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Infrastructure.Reconciliation;

/// <summary>
/// Per-channel reconciliation weights. The reconciler multiplies a candidate's
/// confidence/score by these weights so selection is NOT a blind majority vote:
/// full-page OCR is weak on statistical cells, crops are stronger, and row OCR
/// is strongest on player rows. Unknown providers fall back to <see cref="DefaultWeight"/>
/// (= 1.0), so legacy/generic providers keep their previous behaviour.
/// </summary>
public sealed class EngineWeightOptions
{
    public double DefaultWeight { get; set; } = 1.0;

    /// <summary>Keyed by provider/source id (case-insensitive).</summary>
    public Dictionary<string, EngineWeight> Engines { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public double WeightFor(string providerName, StatScope scope)
    {
        if (string.IsNullOrWhiteSpace(providerName) || !Engines.TryGetValue(providerName, out var weight))
        {
            return DefaultWeight;
        }

        return scope switch
        {
            StatScope.Player or StatScope.Team => weight.StatWeight,
            StatScope.Game or StatScope.Result => weight.ScoreWeight,
            _ => weight.DefaultWeight,
        };
    }

    /// <summary>
    /// Build from the optional "reconciliation.engineWeights" config section,
    /// starting from <see cref="Defaults"/> and overriding only what is provided.
    /// An absent/empty section yields the built-in defaults.
    /// </summary>
    public static EngineWeightOptions FromJsonElement(JsonElement element)
    {
        var options = Defaults();
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty("engineWeights", out var weights) ||
            weights.ValueKind != JsonValueKind.Object)
        {
            return options;
        }

        if (weights.TryGetProperty("defaultWeight", out var defaultWeight) &&
            defaultWeight.TryGetDouble(out var defaultValue))
        {
            options.DefaultWeight = defaultValue;
        }

        if (weights.TryGetProperty("engines", out var engines) && engines.ValueKind == JsonValueKind.Object)
        {
            foreach (var engine in engines.EnumerateObject())
            {
                var parsed = engine.Value.Deserialize<EngineWeight>(JsonDefaults.Options);
                if (parsed is not null)
                {
                    options.Engines[engine.Name] = parsed;
                }
            }
        }

        return options;
    }

    /// <summary>
    /// Built-in defaults. The final score is read most reliably from crops; the
    /// statistical cells are read least reliably from the full page.
    /// </summary>
    public static EngineWeightOptions Defaults() => new()
    {
        Engines = new(StringComparer.OrdinalIgnoreCase)
        {
            ["TesseractFullPage"] = new EngineWeight { StatWeight = 0.5, ScoreWeight = 0.7, DefaultWeight = 0.6 },
            ["ocr.tesseract.fullpage"] = new EngineWeight { StatWeight = 0.5, ScoreWeight = 0.7, DefaultWeight = 0.6 },
            ["ocr.tesseract.crop"] = new EngineWeight { StatWeight = 0.85, ScoreWeight = 1.0, DefaultWeight = 0.85 },
            ["ocr.paddle.crop"] = new EngineWeight { StatWeight = 0.9, ScoreWeight = 0.95, DefaultWeight = 0.9 },
            ["ocr.paddle.row"] = new EngineWeight { StatWeight = 1.0, ScoreWeight = 0.8, DefaultWeight = 1.0 },
        },
    };
}

public sealed class EngineWeight
{
    /// <summary>Weight for Player/Team statistical cells.</summary>
    public double StatWeight { get; set; } = 1.0;
    /// <summary>Weight for Game/Result values (e.g. final score, period scores).</summary>
    public double ScoreWeight { get; set; } = 1.0;
    /// <summary>Weight for any other scope (e.g. Comparative).</summary>
    public double DefaultWeight { get; set; } = 1.0;
}
