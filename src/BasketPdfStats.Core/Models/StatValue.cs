using System.Text.Json.Serialization;
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

    /// <summary>
    /// Candidati OCR grezzi per-provider: usati solo in-memory dalla riconciliazione (es. math
    /// tiebreak). Diagnostici, esclusi dal JSON finale per non appesantirlo.
    /// </summary>
    [JsonIgnore]
    public List<OcrCandidate> Candidates { get; set; } = [];

    public List<ValidationWarning> Warnings { get; set; } = [];
    public List<string> FailedRules { get; set; } = [];

    /// <summary>
    /// Metadati di riconciliazione per-campo (provider selezionato, providerValues): diagnostici,
    /// usati in-memory/test, esclusi dal JSON finale.
    /// </summary>
    [JsonIgnore]
    public StatReconciliationMetadata? Reconciliation { get; set; }
}
