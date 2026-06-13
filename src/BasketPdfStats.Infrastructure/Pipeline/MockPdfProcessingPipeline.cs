using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Infrastructure.Configuration;

namespace BasketPdfStats.Infrastructure.Pipeline;

public sealed class MockPdfProcessingPipeline : PdfProcessingPipeline
{
    public MockPdfProcessingPipeline(RuntimeOptions options, IOcrEngine ocrEngine)
        : base(options, [ocrEngine])
    {
    }
}
