using BasketPdfStats.Core.Models;
using BasketPdfStats.Infrastructure.FileSystem;

namespace BasketPdfStats.Ocr.TesseractPython;

public static class TesseractLayoutCropsArtifactPaths
{
    public static string RawOutputPath(TesseractPythonOptions options, OcrProcessingRequest request)
    {
        var folder = options.ResolvePath(options.CropRawOutputFolder);
        var stem = Path.GetFileNameWithoutExtension(request.OriginalFileName);
        return Path.Combine(folder, $"{stem}.{DocumentHasher.ShortHash(request.DocumentHash)}.tesseract-layout-crops.raw.json");
    }

    public static string EvidenceOutputPath(TesseractPythonOptions options, OcrProcessingRequest request)
    {
        var folder = options.ResolvePath(options.CropRawOutputFolder);
        var stem = Path.GetFileNameWithoutExtension(request.OriginalFileName);
        return Path.Combine(folder, $"{stem}.{DocumentHasher.ShortHash(request.DocumentHash)}.tesseract-layout-crops.evidence.json");
    }
}
