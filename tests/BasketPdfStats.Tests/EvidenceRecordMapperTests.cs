using BasketPdfStats.Core.Enums;
using BasketPdfStats.Infrastructure.Evidence;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Tests;

public sealed class EvidenceRecordMapperTests
{
    private const string SampleEvidence = """
    {
      "schemaVersion": "1.0",
      "outputKind": "EvidenceRecords",
      "sourceId": "ocr.tesseract.fullpage",
      "engine": "tesseract",
      "granularity": "fullpage",
      "documentHash": "sha256:atlmin123456",
      "records": [
        {
          "fieldId": "player:Home:jersey:0:points", "entityId": "player:Home:jersey:0",
          "entityType": "player", "side": "Home", "statKey": "points", "scope": "Player",
          "valueRaw": "5", "valueNormalized": 5, "confidenceRaw": 0.65, "confidenceNormalized": 0.65,
          "mapperId": "fiba_pdf_to_json.evidence_mapper.fullpage", "warnings": []
        },
        {
          "fieldId": "player:Home:jersey:0:minutes", "entityId": "player:Home:jersey:0",
          "entityType": "player", "side": "Home", "statKey": "minutes", "scope": "Player",
          "valueRaw": "24:00", "valueNormalized": "24:00", "confidenceNormalized": 0.65,
          "mapperId": "m", "warnings": []
        },
        {
          "fieldId": "team:Away:points", "entityId": "team:Away",
          "entityType": "team", "side": "Away", "statKey": "points", "scope": "Team",
          "valueRaw": "29", "valueNormalized": 29, "confidenceNormalized": 0.65, "warnings": []
        },
        {
          "fieldId": "game.finalScore", "entityId": "game:atlmin123456",
          "entityType": "game", "statKey": "finalScore", "scope": "Game",
          "valueRaw": "31-29", "valueNormalized": "31-29", "confidenceNormalized": 0.65, "warnings": []
        }
      ]
    }
    """;

    [Fact]
    public void Maps_final_score_to_game_and_records_provider_run()
    {
        var envelope = new EvidenceRecordReader().Parse(SampleEvidence);

        var result = new EvidenceRecordMapper().Map(envelope);

        Assert.Equal("31-29", result.Game.FinalScore);
        Assert.Equal("game:atlmin123456", result.Game.GameId);
        Assert.Contains(result.OcrRuns, run => run.Engine == "ocr.tesseract.fullpage");
        // final score is not also a stat row
        Assert.DoesNotContain(result.Stats, s => s.StatKey == "finalScore");
    }

    [Fact]
    public void Maps_player_and_team_stats_with_channel_provenance()
    {
        var envelope = new EvidenceRecordReader().Parse(SampleEvidence);

        var result = new EvidenceRecordMapper().Map(envelope);

        var points = Assert.Single(result.Stats, s => s.Scope == StatScope.Player && s.StatKey == "points");
        Assert.Equal("player:Home:jersey:0", points.EntityId);
        Assert.Equal(5, points.Value);
        var candidate = Assert.Single(points.Candidates);
        Assert.Equal("ocr.tesseract.fullpage", candidate.SourceId);
        Assert.Equal("fullpage", candidate.Granularity);
        Assert.Equal(0.65, candidate.ConfidenceNormalized);
        Assert.Equal("fiba_pdf_to_json.evidence_mapper.fullpage", candidate.MapperId);

        var minutes = Assert.Single(result.Stats, s => s.StatKey == "minutes");
        Assert.Equal("24:00", minutes.Value);

        Assert.Contains(result.Stats, s => s.Scope == StatScope.Team && s.EntityId == "team:Away");
    }
}
