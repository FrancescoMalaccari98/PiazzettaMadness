using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Ocr.Mock;

namespace BasketPdfStats.Tests;

public sealed class MockOcrEngineTests
{
    [Fact]
    public async Task Mock_engine_returns_coherent_processing_result()
    {
        var engine = new MockOcrEngine();
        var result = await engine.ProcessAsync(new OcrProcessingRequest
        {
            PdfPath = "sample.pdf",
            OriginalFileName = "sample.pdf",
            DocumentHash = "sha256:abcdef123456"
        });

        Assert.Equal("Piazzetta Madness", result.Game.Competition);
        Assert.Equal("48-26", result.Game.FinalScore);
        Assert.Contains(result.Teams, x => x.TeamId == "MIA");
        Assert.Contains(result.Stats, x => x.FieldId == "team:MIA:points" && x.StatKey == "points");
        Assert.Contains(result.Stats.SelectMany(x => x.Candidates), x => x.Engine == "MockOcr" && x.Status == OcrRunStatus.Success);
        Assert.Equal(FileProcessingStatus.CompletedValidated, result.Validation.Status);
    }
}
