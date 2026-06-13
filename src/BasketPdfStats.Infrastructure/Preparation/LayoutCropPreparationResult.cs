namespace BasketPdfStats.Infrastructure.Preparation;

public sealed class LayoutCropPreparationResult
{
    public bool Success { get; set; }
    public string OutputFolder { get; set; } = string.Empty;
    public string MetadataPath { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public long DurationMs { get; set; }
}
