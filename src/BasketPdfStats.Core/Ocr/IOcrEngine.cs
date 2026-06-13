using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Core.Ocr;

public interface IOcrEngine
{
    string EngineName { get; }

    Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default);
}
