using BasketPdfStats.Core.Enums;

namespace BasketPdfStats.Core.Models;

public sealed class StatValue
{
    public StatScope Scope { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string? Side { get; set; }
    public string StatKey { get; set; } = string.Empty;
    public string FieldId { get; set; } = string.Empty;
    public object? Value { get; set; }
    public StatValidationStatus Status { get; set; } = StatValidationStatus.NonValidato;
    public double Score { get; set; }
    public List<OcrCandidate> Candidates { get; set; } = [];
    public List<ValidationWarning> Warnings { get; set; } = [];
    public List<string> FailedRules { get; set; } = [];
    public StatReconciliationMetadata? Reconciliation { get; set; }
}
