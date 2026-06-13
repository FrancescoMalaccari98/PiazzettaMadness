using BasketPdfStats.Core.Enums;

namespace BasketPdfStats.Infrastructure.FileSystem;

public sealed class ProcessedIndex
{
    public List<ProcessedIndexEntry> Documents { get; set; } = [];
}

public sealed class ProcessedIndexEntry
{
    public string DocumentHash { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public FileProcessingStatus Status { get; set; }
    public string? OutputJsonPath { get; set; }
    public string? FinalPath { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
