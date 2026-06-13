namespace BasketPdfStats.Core.Pipeline;

public interface IAlreadyProcessedPdfDecisionService
{
    bool ShouldReprocess(string pdfPath);
}
