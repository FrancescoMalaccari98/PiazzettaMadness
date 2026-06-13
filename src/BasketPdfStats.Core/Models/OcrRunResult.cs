using BasketPdfStats.Core.Enums;

namespace BasketPdfStats.Core.Models;

public sealed class OcrRunResult
{
    public string Engine { get; set; } = string.Empty;
    public OcrRunStatus Status { get; set; }
    public long DurationMs { get; set; }
    public string? Error { get; set; }
}
