using System.Text.Json;

namespace BasketPdfStats.Core.Models;

/// <summary>
/// One piece of OCR evidence for a single field, shared by all evidence channels
/// (full-page, tesseract crop, paddle crop, paddle row). Produced by the Python
/// evidence_record.py schema and consumed by the reconciler. It is NOT a final
/// value: it is a candidate with full provenance.
/// </summary>
public sealed class EvidenceRecord
{
    public string RecordId { get; set; } = string.Empty;
    public string FieldId { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? Side { get; set; }
    public int? RowIndex { get; set; }
    public string? ColumnId { get; set; }
    public string StatKey { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string CropId { get; set; } = string.Empty;
    public string ZoneId { get; set; } = string.Empty;
    public string ValueRaw { get; set; } = string.Empty;

    /// <summary>Normalized value: number, string, or null. Kept as JsonElement to defer typing.</summary>
    public JsonElement? ValueNormalized { get; set; }

    public double? ConfidenceRaw { get; set; }
    public double? ConfidenceNormalized { get; set; }
    public string MapperId { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = [];
    public Dictionary<string, double>? RectNormalized { get; set; }
    public Dictionary<string, int>? RectPixels { get; set; }
}
