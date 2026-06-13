using BasketPdfStats.Core.Enums;

namespace BasketPdfStats.Core.Models;

public sealed class ProcessedFile
{
    public string FileName { get; set; } = string.Empty;
    public string DocumentHash { get; set; } = string.Empty;
    public FileProcessingStatus Status { get; set; } = FileProcessingStatus.Pending;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? SourcePath { get; set; }
    public string? WorkingPath { get; set; }
    public string? OutputJsonPath { get; set; }
    public string? FinalPath { get; set; }
    public string? ErrorMessage { get; set; }
}
