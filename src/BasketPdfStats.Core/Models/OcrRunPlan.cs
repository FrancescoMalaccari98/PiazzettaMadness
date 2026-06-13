namespace BasketPdfStats.Core.Models;

public sealed class OcrRunPlan
{
    public string PdfPath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string DocumentHash { get; set; } = string.Empty;
    public OcrRunSelection Selection { get; set; } = new();
    public List<string> SelectedOcrNames { get; set; } = [];
    public bool NeedsLayoutCrops { get; set; }
    public bool NeedsTesseractPreprocessing { get; set; }
    public string LayoutCropFolder { get; set; } = string.Empty;
    public string LayoutImageFolder { get; set; } = string.Empty;
    public string TesseractPreprocessedFolder { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = [];
}
