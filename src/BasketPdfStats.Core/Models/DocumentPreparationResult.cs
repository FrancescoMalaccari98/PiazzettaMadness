namespace BasketPdfStats.Core.Models;

public sealed class DocumentPreparationResult
{
    public bool LayoutCropsRequested { get; set; }
    public bool LayoutCropsAvailable { get; set; }
    public bool LayoutCropsPrepared { get; set; }
    public bool LayoutCalibrated { get; set; }
    public string MetadataPath { get; set; } = string.Empty;
    public string LayoutCalibrationReportPath { get; set; } = string.Empty;
    public string LayoutCropFolder { get; set; } = string.Empty;
    public string LayoutImageFolder { get; set; } = string.Empty;
    public List<LayoutCropZone> LayoutCrops { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}
