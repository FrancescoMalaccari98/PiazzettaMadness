namespace BasketPdfStats.Core.Models;

public sealed class LayoutCropZone
{
    public string ZoneId { get; set; } = string.Empty;
    public string ZoneType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string CropType { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string ExpectedContentType { get; set; } = string.Empty;
    public int PageIndex { get; set; }
    public List<string> FieldIds { get; set; } = [];
    public List<string> StatKeys { get; set; } = [];
    public string SemanticRole { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string ParentZoneId { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
}
