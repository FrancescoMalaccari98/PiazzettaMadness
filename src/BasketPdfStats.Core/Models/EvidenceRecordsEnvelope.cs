namespace BasketPdfStats.Core.Models;

/// <summary>
/// Top-level container of one OCR evidence channel's output for a document/page.
/// sourceId/engine/granularity are channel-level (shared by all records inside).
/// </summary>
public sealed class EvidenceRecordsEnvelope
{
    public string SchemaVersion { get; set; } = string.Empty;
    public string OutputKind { get; set; } = string.Empty;
    public bool IsFinalNormalizedJson { get; set; }
    public string CreatedAtUtc { get; set; } = string.Empty;

    /// <summary>e.g. ocr.tesseract.fullpage, ocr.tesseract.crop, ocr.paddle.crop, ocr.paddle.row.</summary>
    public string SourceId { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;

    /// <summary>fullpage | table | row | cell.</summary>
    public string Granularity { get; set; } = string.Empty;

    public string DocumentFileName { get; set; } = string.Empty;
    public string DocumentHash { get; set; } = string.Empty;
    public int PageIndex { get; set; }
    public int? Dpi { get; set; }
    public int? RenderWidth { get; set; }
    public int? RenderHeight { get; set; }
    public List<EvidenceRecord> Records { get; set; } = [];
}
