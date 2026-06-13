using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Core.Pipeline;

public interface IPdfProcessingPipeline
{
    Task<ProcessingResult> ProcessPdfAsync(string pdfPath, CancellationToken cancellationToken = default);

    Task<ProcessingResult> ProcessPdfAsync(string pdfPath, OcrRunSelection selection, CancellationToken cancellationToken = default);
}
