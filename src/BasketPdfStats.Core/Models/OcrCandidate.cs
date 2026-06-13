using BasketPdfStats.Core.Enums;

namespace BasketPdfStats.Core.Models;

public sealed class OcrCandidate
{
    public string Engine { get; set; } = string.Empty;
    public string FieldId { get; set; } = string.Empty;
    public string StatKey { get; set; } = string.Empty;
    public string? SourceFieldId { get; set; }
    public string? SourceEntityId { get; set; }
    public string? ZoneId { get; set; }
    public string? Raw { get; set; }
    public object? Normalized { get; set; }
    public double? Confidence { get; set; }
    public OcrRunStatus Status { get; set; } = OcrRunStatus.Success;

    // Evidence-channel provenance (set when the candidate comes from an
    // evidence_records.json source). Optional: legacy candidates leave them null.
    /// <summary>e.g. ocr.tesseract.fullpage, ocr.tesseract.crop, ocr.paddle.crop, ocr.paddle.row.</summary>
    public string? SourceId { get; set; }
    /// <summary>fullpage | table | row | cell.</summary>
    public string? Granularity { get; set; }
    public double? ConfidenceNormalized { get; set; }
    public string? MapperId { get; set; }
    public List<string> Warnings { get; set; } = [];
}
