using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Ocr.TesseractPython;

public static class TesseractFullPageArtifactPaths
{
    public static string RawOutputPath(TesseractPythonOptions options, OcrProcessingRequest request)
    {
        var folder = options.ResolvePath(options.RawOutputFolder);
        var stem = Path.GetFileNameWithoutExtension(request.OriginalFileName);
        return Path.Combine(folder, $"{stem}.{ShortHash(request.DocumentHash)}.tesseract-full-page.raw.json");
    }

    private static string ShortHash(string documentHash)
    {
        var normalized = documentHash.Replace("sha256:", string.Empty, StringComparison.OrdinalIgnoreCase);
        return normalized.Length <= 12 ? normalized : normalized[..12];
    }
}
