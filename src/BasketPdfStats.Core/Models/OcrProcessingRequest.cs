namespace BasketPdfStats.Core.Models;

public sealed class OcrProcessingRequest
{
    public string PdfPath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string DocumentHash { get; set; } = string.Empty;
    public string WorkingPath { get; set; } = string.Empty;
    public OcrRunPlan? RunPlan { get; set; }
    public DocumentPreparationResult? DocumentPreparation { get; set; }
}
