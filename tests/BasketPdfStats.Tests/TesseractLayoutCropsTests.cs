using System.Text.Json;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Ocr.TesseractPython;

namespace BasketPdfStats.Tests;

public sealed class TesseractLayoutCropsTests
{
    [Fact]
    public void Uses_independent_layout_crops_strategy_name()
    {
        var engine = new TesseractLayoutCropsOcrEngine(new TesseractPythonOptions());

        Assert.Equal(OcrStrategyNames.TesseractLayoutCrops, engine.EngineName);
        Assert.NotEqual(OcrStrategyNames.TesseractFullPage, engine.EngineName);
    }

    [Fact]
    public void Raw_artifact_path_uses_layout_crops_strategy_filename()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        var options = new TesseractPythonOptions
        {
            RuntimeRoot = root,
            CropRawOutputFolder = Path.Combine("Dataset", "OcrRaw", OcrStrategyNames.TesseractLayoutCrops)
        };

        var path = TesseractLayoutCropsArtifactPaths.RawOutputPath(options, Request("sample game.pdf", "sha256:abcdef1234567890"));

        Assert.Equal(
            Path.Combine(root, "Dataset", "OcrRaw", OcrStrategyNames.TesseractLayoutCrops, "sample game.abcdef123456.tesseract-layout-crops.raw.json"),
            path);
    }

    [Fact]
    public async Task Does_not_fall_back_to_full_page_when_layout_crops_are_missing()
    {
        var engine = new TesseractLayoutCropsOcrEngine(new TesseractPythonOptions
        {
            Enabled = true,
            Mode = "Crops",
            UseLayoutCrops = true
        });

        var result = await engine.ProcessAsync(Request("missing.pdf", "sha256:missing"));

        var run = Assert.Single(result.OcrRuns);
        Assert.Equal(OcrStrategyNames.TesseractLayoutCrops, run.Engine);
        Assert.Equal(OcrRunStatus.Failed, run.Status);
        Assert.DoesNotContain(result.OcrRuns, item => item.Engine == OcrStrategyNames.TesseractFullPage);
    }

    [Fact]
    public void Mapper_materializes_unambiguous_final_score_with_crop_provenance()
    {
        using var raw = JsonDocument.Parse("""
        {
          "engine": "TesseractLayoutCrops",
          "zones": [
            {
              "zoneId": "header.finalScore",
              "rawText": "Atlanta Robba 52 - 39 Miami Spritz",
              "warnings": [],
              "errors": []
            }
          ],
          "warnings": [],
          "errors": []
        }
        """);

        var result = new TesseractLayoutCropsOutputMapper().Map(raw, Request("game.pdf", "sha256:1234567890abcdef"));

        Assert.Equal("52-39", result.Game.FinalScore);
        Assert.Equal("team:Home", result.Game.HomeTeamId);
        Assert.Equal("team:Away", result.Game.AwayTeamId);
        Assert.Collection(
            result.Stats.OrderBy(stat => stat.Side),
            away =>
            {
                Assert.Equal("Away", away.Side);
                Assert.Equal("points", away.StatKey);
                Assert.Equal(39, away.Value);
                Assert.Equal("header.finalScore", Assert.Single(away.Candidates).ZoneId);
                Assert.Equal(OcrStrategyNames.TesseractLayoutCrops, Assert.Single(away.Candidates).Engine);
            },
            home =>
            {
                Assert.Equal("Home", home.Side);
                Assert.Equal("points", home.StatKey);
                Assert.Equal(52, home.Value);
                Assert.Equal("header.finalScore", Assert.Single(home.Candidates).ZoneId);
                Assert.Equal(OcrStrategyNames.TesseractLayoutCrops, Assert.Single(home.Candidates).Engine);
            });
    }

    [Fact]
    public void Mapper_keeps_partial_result_valid_when_final_score_is_unreadable()
    {
        using var raw = JsonDocument.Parse("""
        {
          "engine": "TesseractLayoutCrops",
          "zones": [
            {
              "zoneId": "header.finalScore",
              "rawText": "score unreadable",
              "warnings": [],
              "errors": []
            }
          ],
          "warnings": [],
          "errors": []
        }
        """);

        var result = new TesseractLayoutCropsOutputMapper().Map(raw, Request("game.pdf", "sha256:1234567890abcdef"));

        Assert.Empty(result.Stats);
        Assert.Contains(result.Validation.Warnings, warning => warning.RuleId == "ocr.tesseractLayoutCrops.finalScoreUnreadable");
        Assert.Contains(result.Validation.Warnings, warning => warning.RuleId == "ocr.tesseractLayoutCrops.partialMapping");
    }

    [Fact]
    public void Mapper_prefers_typographic_final_score_outside_parentheses_over_period_scores()
    {
        using var raw = JsonDocument.Parse("""
        {
          "engine": "TesseractLayoutCrops",
          "zones": [
            {
              "zoneId": "header.finalScore",
              "rawText": "Miami Spritz 97 — 99 Saluta Andonio Spurs\n(21-31, 30-22)",
              "warnings": [],
              "errors": []
            }
          ],
          "warnings": [],
          "errors": []
        }
        """);

        var result = new TesseractLayoutCropsOutputMapper().Map(raw, Request("game.pdf", "sha256:1234567890abcdef"));

        Assert.Equal("97-99", result.Game.FinalScore);
        Assert.Equal([97, 99], result.Stats.OrderBy(stat => stat.Side).Select(stat => (int)stat.Value!).Order().ToArray());
    }

    private static OcrProcessingRequest Request(string fileName, string hash) => new()
    {
        PdfPath = fileName,
        WorkingPath = fileName,
        OriginalFileName = fileName,
        DocumentHash = hash
    };
}
