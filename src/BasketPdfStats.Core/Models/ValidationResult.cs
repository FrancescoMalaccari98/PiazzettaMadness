using BasketPdfStats.Core.Enums;

namespace BasketPdfStats.Core.Models;

public sealed class ValidationResult
{
    public FileProcessingStatus Status { get; set; } = FileProcessingStatus.Pending;
    public List<string> FailedRules { get; set; } = [];
    public List<ValidationWarning> Warnings { get; set; } = [];
}
