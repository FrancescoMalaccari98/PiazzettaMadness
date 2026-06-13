namespace BasketPdfStats.Core.Models;

public sealed class ProcessingResult
{
    public string SchemaVersion { get; set; } = "1.0";
    public ProcessedFile ProcessedFile { get; set; } = new();
    public List<OcrRunResult> OcrRuns { get; set; } = [];
    public GameStats Game { get; set; } = new();
    public List<TeamStats> Teams { get; set; } = [];
    public List<PlayerStats> Players { get; set; } = [];
    public List<StatValue> Stats { get; set; } = [];
    public ValidationResult Validation { get; set; } = new();
    public ReconciliationMetadata? Reconciliation { get; set; }
}
