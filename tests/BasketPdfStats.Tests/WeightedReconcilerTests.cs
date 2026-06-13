using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Infrastructure.Reconciliation;

namespace BasketPdfStats.Tests;

public sealed class WeightedReconcilerTests
{
    [Fact]
    public void Crop_channel_outweighs_full_page_on_conflicting_final_score()
    {
        // Full-page and crop disagree on the final score. Crop has a higher
        // score-weight, so the crop value must win (no blind majority needed).
        var fullPage = ResultWith("ocr.tesseract.fullpage", "game.finalScore", StatScope.Game, "51-99", score: 0.60);
        var crop = ResultWith("ocr.tesseract.crop", "game.finalScore", StatScope.Game, "51-53", score: 0.60);

        var result = new NormalizedOcrReconciler().Reconcile([fullPage, crop]);

        var stat = Assert.Single(result.Stats, s => s.FieldId == "game.finalScore");
        Assert.Equal("51-53", stat.Value);
        Assert.Equal("ocr.tesseract.crop", stat.Reconciliation!.SelectedProvider);
    }

    [Fact]
    public void Equal_weights_fall_back_to_provider_order()
    {
        // Two unknown providers (default weight 1.0) with equal score: the first
        // provider wins by order — behaviour unchanged for generic providers.
        var a = ResultWith("providerA", "team:Home:points", StatScope.Team, 40, score: 0.6);
        var b = ResultWith("providerB", "team:Home:points", StatScope.Team, 41, score: 0.6);

        var result = new NormalizedOcrReconciler().Reconcile([a, b]);

        var stat = Assert.Single(result.Stats, s => s.FieldId == "team:Home:points");
        Assert.Equal(40, stat.Value);
    }

    [Fact]
    public void Math_consistent_minority_wins_over_higher_weighted_channel()
    {
        // Crop (higher weight) reads points=10 for a player, but 2P/3P/FT imply
        // 2*4 + 3*2 + 4 = 18. Full-page (lower weight) read 18. Math must win:
        // even though the crop is weighted higher, the formula-consistent value
        // from the full-page candidate is selected.
        var crop = PlayerStats("ocr.tesseract.crop",
            ("points", 10), ("twoPoints.made", 4), ("threePoints.made", 2), ("freeThrows.made", 4));
        var fullPage = PlayerStats("ocr.tesseract.fullpage",
            ("points", 18), ("twoPoints.made", 4), ("threePoints.made", 2), ("freeThrows.made", 4));

        var result = new NormalizedOcrReconciler().Reconcile([crop, fullPage]);

        var points = Assert.Single(result.Stats, s => s.StatKey == "points");
        Assert.Equal(18d, Convert.ToDouble(points.Value));
        Assert.Contains(result.Validation.Warnings, w => w.RuleId == "ocr.reconciliation.mathTiebreak");
    }

    [Fact]
    public void Team_and_final_score_points_are_corrected_to_player_sum()
    {
        // The away total/final score reads 2 but the away players sum to 52
        // (the "29-2 should be 29-52" bug). The reconciler must correct it.
        var result = new ProcessingResult
        {
            OcrRuns = [new OcrRunResult { Engine = "ocr.tesseract.fullpage", Status = OcrRunStatus.Success }],
            Game = new GameStats { FinalScore = "29-2" }
        };
        // away players (>= 5, a realistic roster): 12 + 10 + 10 + 10 + 10 = 52
        AddPoints(result, StatScope.Player, "Away", "player:Away:jersey:1", 12);
        AddPoints(result, StatScope.Player, "Away", "player:Away:jersey:2", 10);
        AddPoints(result, StatScope.Player, "Away", "player:Away:jersey:3", 10);
        AddPoints(result, StatScope.Player, "Away", "player:Away:jersey:4", 10);
        AddPoints(result, StatScope.Player, "Away", "player:Away:jersey:5", 10);
        AddPoints(result, StatScope.Team, "Away", "team:Away", 2);
        AddPoints(result, StatScope.Result, "Away", "game:x", 2);
        // home result kept as-is (29)
        AddPoints(result, StatScope.Result, "Home", "game:x", 29);

        var reconciled = new NormalizedOcrReconciler().Reconcile([result]);

        var teamAway = Assert.Single(reconciled.Stats, s => s.Scope == StatScope.Team && s.Side == "Away" && s.StatKey == "points");
        Assert.Equal(52, Convert.ToInt32(teamAway.Value));
        Assert.Equal("29-52", reconciled.Game.FinalScore);
        Assert.Contains(reconciled.Validation.Warnings, w => w.RuleId == "ocr.reconciliation.pointsFromPlayerSum");
    }

    private static void AddPoints(ProcessingResult result, StatScope scope, string side, string entityId, int value)
    {
        result.Stats.Add(new StatValue
        {
            Scope = scope,
            EntityId = entityId,
            Side = side,
            StatKey = "points",
            FieldId = $"{entityId}:points",
            Value = value,
            Score = 0.6,
            Candidates = [new OcrCandidate { Engine = "ocr.tesseract.fullpage", FieldId = $"{entityId}:points", Normalized = value, Confidence = 0.6 }]
        });
    }

    private static ProcessingResult PlayerStats(string provider, params (string key, int value)[] stats)
    {
        const string entityId = "player:Home:jersey:0";
        var result = new ProcessingResult
        {
            OcrRuns = [new OcrRunResult { Engine = provider, Status = OcrRunStatus.Success }]
        };
        foreach (var (key, value) in stats)
        {
            result.Stats.Add(new StatValue
            {
                Scope = StatScope.Player,
                EntityId = entityId,
                Side = "Home",
                StatKey = key,
                FieldId = $"{entityId}:{key}",
                Value = value,
                Score = 0.6,
                Candidates = [new OcrCandidate { Engine = provider, SourceId = provider, FieldId = $"{entityId}:{key}", Normalized = value, Confidence = 0.6 }]
            });
        }

        return result;
    }

    private static ProcessingResult ResultWith(string provider, string fieldId, StatScope scope, object value, double score)
    {
        var entityId = scope == StatScope.Game ? "game:x" : "team:Home";
        return new ProcessingResult
        {
            OcrRuns = [new OcrRunResult { Engine = provider, Status = OcrRunStatus.Success }],
            Stats =
            [
                new StatValue
                {
                    Scope = scope,
                    EntityId = entityId,
                    Side = scope == StatScope.Game ? null : "Home",
                    StatKey = fieldId.Contains("finalScore") ? "finalScore" : "points",
                    FieldId = fieldId,
                    Value = value,
                    Score = score,
                    Candidates = [new OcrCandidate { Engine = provider, FieldId = fieldId, Normalized = value, Confidence = score }]
                }
            ]
        };
    }
}
