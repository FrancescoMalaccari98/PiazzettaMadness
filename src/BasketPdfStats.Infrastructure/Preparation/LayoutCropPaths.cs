using BasketPdfStats.Core.Models;
using BasketPdfStats.Infrastructure.FileSystem;

namespace BasketPdfStats.Infrastructure.Preparation;

public static class LayoutCropPaths
{
    public static string DocumentCropFolder(OcrProcessingRequest request, LayoutCropOptions options)
    {
        return Path.Combine(options.ResolvePath(options.CropOutputFolder), DocumentFolderName(request));
    }

    public static string DocumentImageFolder(OcrProcessingRequest request, LayoutCropOptions options)
    {
        return Path.Combine(options.ResolvePath(options.ImageOutputFolder), DocumentFolderName(request));
    }

    public static string DocumentDebugFolder(OcrProcessingRequest request, LayoutCropOptions options)
    {
        return Path.Combine(options.ResolvePath(options.LayoutDebugOutputFolder), DocumentFolderName(request));
    }

    public static string FullPageWordBoxesPath(OcrProcessingRequest request, LayoutCropOptions options)
    {
        return Path.Combine(DocumentDebugFolder(request, options), "full-page-word-boxes.json");
    }

    public static string DocumentFolderName(OcrProcessingRequest request)
    {
        var name = SafeName(Path.GetFileNameWithoutExtension(request.OriginalFileName));
        var hash = DocumentHasher.ShortHash(request.DocumentHash);
        return string.IsNullOrWhiteSpace(hash) ? name : $"{name}.{hash}";
    }

    public static string SafeName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var chars = value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        var safe = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "document" : safe;
    }
}
