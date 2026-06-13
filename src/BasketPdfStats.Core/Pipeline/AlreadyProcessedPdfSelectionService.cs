namespace BasketPdfStats.Core.Pipeline;

public sealed class AlreadyProcessedPdfSelectionService
{
    private readonly IAlreadyProcessedPdfDetector _detector;
    private readonly IAlreadyProcessedPdfDecisionService _decisionService;

    public AlreadyProcessedPdfSelectionService(
        IAlreadyProcessedPdfDetector detector,
        IAlreadyProcessedPdfDecisionService decisionService)
    {
        _detector = detector;
        _decisionService = decisionService;
    }

    public async Task<bool> ShouldProcessAsync(
        string pdfPath,
        Action<string>? reportStatus = null,
        CancellationToken cancellationToken = default)
    {
        var detection = await _detector.DetectAsync(pdfPath, cancellationToken);
        if (!detection.IsAlreadyProcessed)
        {
            return true;
        }

        var fileName = Path.GetFileName(pdfPath);
        reportStatus?.Invoke($"PDF already processed: {fileName}");
        if (_decisionService.ShouldReprocess(pdfPath))
        {
            reportStatus?.Invoke($"User chose to reprocess: {fileName}");
            return true;
        }

        reportStatus?.Invoke($"User chose to skip already processed PDF: {fileName}");
        return false;
    }
}
