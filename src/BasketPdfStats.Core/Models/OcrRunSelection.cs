namespace BasketPdfStats.Core.Models;

public sealed class OcrRunSelection
{
    public bool UseTesseract { get; set; } = true;
    public bool UsePaddle { get; set; }
    public bool HasAnySelected => UseTesseract || UsePaddle;

    public bool TryValidate(out string error)
    {
        error = HasAnySelected ? string.Empty : "Seleziona almeno un OCR da utilizzare.";
        return string.IsNullOrWhiteSpace(error);
    }
}
