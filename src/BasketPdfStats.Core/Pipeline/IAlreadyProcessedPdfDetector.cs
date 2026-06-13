namespace BasketPdfStats.Core.Pipeline;

public interface IAlreadyProcessedPdfDetector
{
    Task<AlreadyProcessedPdfDetection> DetectAsync(string pdfPath, CancellationToken cancellationToken = default);
}
