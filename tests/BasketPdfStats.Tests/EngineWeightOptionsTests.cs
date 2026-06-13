using System.Text.Json;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Infrastructure.Reconciliation;

namespace BasketPdfStats.Tests;

public sealed class EngineWeightOptionsTests
{
    [Fact]
    public void Defaults_weight_full_page_low_and_crop_high_on_stats()
    {
        var options = EngineWeightOptions.Defaults();

        Assert.Equal(0.5, options.WeightFor("TesseractFullPage", StatScope.Player));
        Assert.Equal(0.85, options.WeightFor("ocr.tesseract.crop", StatScope.Team));
        Assert.Equal(1.0, options.WeightFor("ocr.tesseract.crop", StatScope.Game));
        // unknown provider -> neutral default
        Assert.Equal(1.0, options.WeightFor("whatever", StatScope.Player));
    }

    [Fact]
    public void From_json_overrides_only_provided_engines()
    {
        using var doc = JsonDocument.Parse("""
        {
          "engineWeights": {
            "defaultWeight": 0.5,
            "engines": {
              "ocr.tesseract.crop": { "statWeight": 0.99, "scoreWeight": 0.99, "defaultWeight": 0.99 }
            }
          }
        }
        """);

        var options = EngineWeightOptions.FromJsonElement(doc.RootElement);

        Assert.Equal(0.5, options.DefaultWeight);
        Assert.Equal(0.99, options.WeightFor("ocr.tesseract.crop", StatScope.Player));
        // an engine not in the override keeps its default value
        Assert.Equal(0.5, options.WeightFor("TesseractFullPage", StatScope.Player));
    }

    [Fact]
    public void Absent_section_yields_defaults()
    {
        using var doc = JsonDocument.Parse("{}");

        var options = EngineWeightOptions.FromJsonElement(doc.RootElement);

        Assert.Equal(0.5, options.WeightFor("TesseractFullPage", StatScope.Player));
    }
}
