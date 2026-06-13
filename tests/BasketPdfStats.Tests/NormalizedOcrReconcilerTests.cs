using System.Text.Json;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Infrastructure.Reconciliation;

namespace BasketPdfStats.Tests;

public sealed class NormalizedOcrReconcilerTests
{
    [Fact]
    public void Single_provider_produces_final_result_with_source_metadata_and_final_validation()
    {
        var result = new NormalizedOcrReconciler().Reconcile([Result(OcrStrategyNames.TesseractFullPage, Stat("points", 12, 0.90))]);

        Assert.True(result.Reconciliation!.Enabled);
        Assert.Equal([OcrStrategyNames.TesseractFullPage], result.Reconciliation.Providers);
        Assert.Equal(OcrStrategyNames.TesseractFullPage, Assert.Single(result.Stats).Reconciliation!.SelectedProvider);
    }

    [Fact]
    public void Equal_values_record_agreed_providers_and_high_confidence()
    {
        var result = new NormalizedOcrReconciler().Reconcile(
        [
            Result(OcrStrategyNames.TesseractFullPage, Stat("points", 12, 0.80)),
            Result("TesseractLayoutCrops", Stat("points", 12, 0.85))
        ]);

        var stat = Assert.Single(result.Stats);
        Assert.IsAssignableFrom<IConvertible>(stat.Value);
        Assert.IsNotType<JsonElement>(stat.Value);
        Assert.Equal(12L, Convert.ToInt64(stat.Value));
        Assert.Equal(0.95, stat.Score);
        Assert.Equal([OcrStrategyNames.TesseractFullPage, "TesseractLayoutCrops"], stat.Reconciliation!.AgreedProviders);
        Assert.Empty(result.Validation.Warnings.Where(warning => warning.RuleId == "ocr.reconciliation.conflict"));
    }

    [Fact]
    public void Missing_provider_value_keeps_present_value_with_warning()
    {
        var result = new NormalizedOcrReconciler().Reconcile(
        [
            Result(OcrStrategyNames.TesseractFullPage, Stat("points", 12, 0.90)),
            Result("TesseractLayoutCrops")
        ]);

        var stat = Assert.Single(result.Stats);
        Assert.IsAssignableFrom<IConvertible>(stat.Value);
        Assert.IsNotType<JsonElement>(stat.Value);
        Assert.Equal(12L, Convert.ToInt64(stat.Value));
        Assert.Equal(OcrStrategyNames.TesseractFullPage, stat.Reconciliation!.SelectedProvider);
        Assert.Contains(stat.Warnings, warning => warning.RuleId == "ocr.reconciliation.missingProviderValue");
    }

    [Fact]
    public void Conflict_records_provider_values_and_prefers_higher_confidence()
    {
        var result = new NormalizedOcrReconciler().Reconcile(
        [
            Result(OcrStrategyNames.TesseractFullPage, Stat("points", 12, 0.70)),
            Result("TesseractLayoutCrops", Stat("points", 14, 0.90))
        ]);

        var stat = Assert.Single(result.Stats);
        Assert.IsAssignableFrom<IConvertible>(stat.Value);
        Assert.IsNotType<JsonElement>(stat.Value);
        Assert.Equal(14L, Convert.ToInt64(stat.Value));
        Assert.Equal("TesseractLayoutCrops", stat.Reconciliation!.SelectedProvider);
        Assert.Equal(12L, Convert.ToInt64(stat.Reconciliation.ProviderValues[OcrStrategyNames.TesseractFullPage]));
        Assert.Equal(14L, Convert.ToInt64(stat.Reconciliation.ProviderValues["TesseractLayoutCrops"]));
        Assert.All(stat.Reconciliation.ProviderValues.Values, value => Assert.IsNotType<JsonElement>(value));
        Assert.Contains(stat.Warnings, warning => warning.RuleId == "ocr.reconciliation.conflict");
    }

    [Fact]
    public void Conflict_prefers_provider_without_relevant_failed_rule()
    {
        var tesseract = Stat("points", 12, 0.99);
        tesseract.FailedRules.Add("math.pointsFormula");
        var result = new NormalizedOcrReconciler().Reconcile(
        [
            Result(OcrStrategyNames.TesseractFullPage, tesseract),
            Result("TesseractLayoutCrops", Stat("points", 14, 0.70))
        ]);

        Assert.Equal("TesseractLayoutCrops", Assert.Single(result.Stats).Reconciliation!.SelectedProvider);
    }

    [Fact]
    public void Final_math_validation_runs_after_reconciliation()
    {
        var result = new NormalizedOcrReconciler().Reconcile(
        [
            Result(OcrStrategyNames.TesseractFullPage,
                Stat("points", 20),
                Stat("twoPoints.made", 5),
                Stat("threePoints.made", 2),
                Stat("freeThrows.made", 1))
        ]);

        Assert.Contains(result.Validation.FailedRules, rule => rule == "math.pointsFormula");
        Assert.Contains(result.Stats.Single(stat => stat.StatKey == "points").FailedRules, rule => rule == "math.pointsFormula");
    }

    [Fact]
    public void Canonical_player_stat_agrees_across_tesseract_and_adobe()
    {
        var result = new NormalizedOcrReconciler().Reconcile(
        [
            Result(OcrStrategyNames.TesseractFullPage, PlayerStat("player:Home:jersey:7", "points", 12)),
            Result("TesseractLayoutCrops", PlayerStat("player:Home:jersey:7", "points", 12))
        ]);

        var stat = Assert.Single(result.Stats);
        Assert.Equal([OcrStrategyNames.TesseractFullPage, "TesseractLayoutCrops"], stat.Reconciliation!.AgreedProviders);
        Assert.DoesNotContain(stat.Warnings, warning => warning.RuleId == "ocr.reconciliation.missingProviderValue");
    }

    [Fact]
    public void Canonical_player_stat_conflict_is_not_reported_as_two_missing_values()
    {
        var result = new NormalizedOcrReconciler().Reconcile(
        [
            Result(OcrStrategyNames.TesseractFullPage, PlayerStat("player:Away:jersey:8", "points", 9)),
            Result("TesseractLayoutCrops", PlayerStat("player:Away:jersey:8", "points", 11))
        ]);

        var stat = Assert.Single(result.Stats);
        Assert.Contains(stat.Warnings, warning => warning.RuleId == "ocr.reconciliation.conflict");
        Assert.DoesNotContain(stat.Warnings, warning => warning.RuleId == "ocr.reconciliation.missingProviderValue");
    }

    [Fact]
    public void Players_with_same_side_and_jersey_but_different_entity_ids_are_not_merged()
    {
        var result = new NormalizedOcrReconciler().Reconcile(
        [
            Result(OcrStrategyNames.TesseractFullPage,
                [Player("player:Home:jersey:4", "4", "Alberto Ortolan")]),
            Result("TesseractLayoutCrops",
                [Player("player:Home:name:filippo-perugini", "4", "Filippo Perugini")])
        ]);

        Assert.Equal(2, result.Players.Count);
        Assert.Contains(result.Players, player => player.EntityId == "player:Home:jersey:4");
        Assert.Contains(result.Players, player => player.EntityId == "player:Home:name:filippo-perugini");
    }

    [Fact]
    public void Players_with_same_entity_id_are_merged_normally()
    {
        var result = new NormalizedOcrReconciler().Reconcile(
        [
            Result(OcrStrategyNames.TesseractFullPage,
                [Player("player:Away:jersey:7", "7", "Mario Rossi")]),
            Result("TesseractLayoutCrops",
                [Player("player:Away:jersey:7", "7", "Mario Rossi", sourceEntityId: "player:MIA:row:9")])
        ]);

        var player = Assert.Single(result.Players);
        Assert.Equal("player:Away:jersey:7", player.EntityId);
        Assert.Equal("player:MIA:row:9", player.SourceEntityId);
    }

    [Fact]
    public void Player_without_entity_id_uses_legacy_fallback_only_when_needed()
    {
        var result = new NormalizedOcrReconciler().Reconcile(
        [
            Result(OcrStrategyNames.TesseractFullPage,
                [Player(string.Empty, "12", "Legacy Player")]),
            Result("TesseractLayoutCrops",
                [Player(string.Empty, "12", "Legacy Player")])
        ]);

        Assert.Single(result.Players);
    }

    [Fact]
    public void Duplicate_jersey_players_keep_stats_linked_by_canonical_entity_id()
    {
        var jerseyPlayer = "player:Home:jersey:4";
        var fallbackPlayer = "player:Home:name:filippo-perugini";
        var result = new NormalizedOcrReconciler().Reconcile(
        [
            Result(OcrStrategyNames.TesseractFullPage,
                [Player(jerseyPlayer, "4", "Alberto Ortolan")],
                PlayerStat(jerseyPlayer, "points", 8),
                PlayerStat(fallbackPlayer, "points", 11)),
            Result("TesseractLayoutCrops",
                [Player(fallbackPlayer, "4", "Filippo Perugini")])
        ]);

        Assert.Equal(2, result.Players.Count);
        Assert.Contains(result.Players, player => player.EntityId == jerseyPlayer);
        Assert.Contains(result.Players, player => player.EntityId == fallbackPlayer);
        Assert.Contains(result.Stats, stat => stat.EntityId == jerseyPlayer && Convert.ToInt64(stat.Value) == 8);
        Assert.Contains(result.Stats, stat => stat.EntityId == fallbackPlayer && Convert.ToInt64(stat.Value) == 11);
    }

    private static ProcessingResult Result(string providerName, params StatValue[] stats)
    {
        return new ProcessingResult
        {
            OcrRuns = [new OcrRunResult { Engine = providerName, Status = OcrRunStatus.Success }],
            Stats = stats.ToList()
        };
    }

    private static ProcessingResult Result(string providerName, IEnumerable<PlayerStats> players, params StatValue[] stats)
    {
        return new ProcessingResult
        {
            OcrRuns = [new OcrRunResult { Engine = providerName, Status = OcrRunStatus.Success }],
            Players = players.ToList(),
            Stats = stats.ToList()
        };
    }

    private static StatValue Stat(string statKey, object value, double score = 0.90)
    {
        return new StatValue
        {
            Scope = StatScope.Team,
            Side = "Home",
            EntityId = "team:Home",
            FieldId = $"team:Home:{statKey}",
            StatKey = statKey,
            Value = value,
            Score = score,
            Status = StatValidationStatus.Validato
        };
    }

    private static StatValue PlayerStat(string playerId, string statKey, object value)
    {
        return new StatValue
        {
            Scope = StatScope.Player,
            Side = playerId.Contains(":Home:", StringComparison.OrdinalIgnoreCase) ? "Home" : "Away",
            EntityId = playerId,
            FieldId = $"{playerId}:{statKey}",
            StatKey = statKey,
            Value = value,
            Score = 0.90,
            Status = StatValidationStatus.Validato
        };
    }

    private static PlayerStats Player(string entityId, string number, string fullName, string? sourceEntityId = null)
    {
        var side = entityId.Contains(":Away:", StringComparison.OrdinalIgnoreCase) ? "Away" : "Home";
        return new PlayerStats
        {
            EntityId = entityId,
            SourceEntityId = sourceEntityId,
            TeamId = $"team:{side}",
            Side = side,
            Number = number,
            FullName = fullName
        };
    }
}
