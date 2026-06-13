using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Tests;

public sealed class EvidenceRecordReaderTests
{
    private const string SampleEvidence = """
    {
      "schemaVersion": "1.0",
      "outputKind": "EvidenceRecords",
      "isFinalNormalizedJson": false,
      "createdAtUtc": "2026-06-02T13:59:14Z",
      "sourceId": "ocr.tesseract.fullpage",
      "engine": "tesseract",
      "granularity": "fullpage",
      "documentFileName": "ATL-MIN TABELLINO.pdf",
      "documentHash": "sha256:atlmin123456",
      "pageIndex": 0,
      "dpi": null,
      "renderWidth": null,
      "renderHeight": null,
      "records": [
        {
          "recordId": "50ba714335ec",
          "fieldId": "player:Home:jersey:0:minutes",
          "entityId": "player:Home:jersey:0",
          "entityType": "player",
          "side": "Home",
          "rowIndex": null,
          "columnId": null,
          "statKey": "minutes",
          "scope": "Player",
          "cropId": "",
          "zoneId": "",
          "valueRaw": "24:00",
          "valueNormalized": "24:00",
          "confidenceRaw": 0.65,
          "confidenceNormalized": 0.65,
          "mapperId": "fiba_pdf_to_json.evidence_mapper.fullpage",
          "warnings": [],
          "rectNormalized": null,
          "rectPixels": null
        },
        {
          "recordId": "abc123",
          "fieldId": "player:Home:jersey:0:points",
          "entityId": "player:Home:jersey:0",
          "entityType": "player",
          "side": "Home",
          "statKey": "points",
          "scope": "Player",
          "valueRaw": "5",
          "valueNormalized": 5,
          "confidenceRaw": 0.65,
          "confidenceNormalized": 0.65,
          "mapperId": "fiba_pdf_to_json.evidence_mapper.fullpage",
          "warnings": []
        }
      ]
    }
    """;

    [Fact]
    public void Parses_envelope_channel_metadata()
    {
        var envelope = new EvidenceRecordReader().Parse(SampleEvidence);

        Assert.Equal("ocr.tesseract.fullpage", envelope.SourceId);
        Assert.Equal("tesseract", envelope.Engine);
        Assert.Equal("fullpage", envelope.Granularity);
        Assert.Equal("ATL-MIN TABELLINO.pdf", envelope.DocumentFileName);
        Assert.Equal(2, envelope.Records.Count);
    }

    [Fact]
    public void Parses_string_and_numeric_normalized_values_with_provenance()
    {
        var envelope = new EvidenceRecordReader().Parse(SampleEvidence);

        var minutes = Assert.Single(envelope.Records, r => r.StatKey == "minutes");
        Assert.Equal("player:Home:jersey:0", minutes.EntityId);
        Assert.Equal("24:00", minutes.ValueNormalized!.Value.GetString());
        Assert.Equal(0.65, minutes.ConfidenceNormalized);
        Assert.Equal("fiba_pdf_to_json.evidence_mapper.fullpage", minutes.MapperId);

        var points = Assert.Single(envelope.Records, r => r.StatKey == "points");
        Assert.Equal(5, points.ValueNormalized!.Value.GetInt32());
        Assert.Equal("Player", points.Scope);
    }
}
