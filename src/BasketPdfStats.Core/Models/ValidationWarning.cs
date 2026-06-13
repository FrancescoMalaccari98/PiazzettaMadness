namespace BasketPdfStats.Core.Models;

public sealed class ValidationWarning
{
    public string RuleId { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public string? FieldId { get; set; }
    public List<string> FieldIds { get; set; } = [];
    public string Message { get; set; } = string.Empty;
}
