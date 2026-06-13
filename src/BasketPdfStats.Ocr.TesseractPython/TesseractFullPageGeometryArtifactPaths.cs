using BasketPdfStats.Core.Models;
using BasketPdfStats.Infrastructure.FileSystem;
using BasketPdfStats.Infrastructure.Preparation;

namespace BasketPdfStats.Ocr.TesseractPython;

public static class TesseractFullPageGeometryArtifactPaths
{
    public static string DebugFolder(TesseractPythonOptions options, OcrProcessingRequest request)
    {
        return Path.Combine(
            options.ResolvePath(options.LayoutDebugOutputFolder),
            LayoutCropPaths.DocumentFolderName(request));
    }

    public static string WordBoxesPath(TesseractPythonOptions options, OcrProcessingRequest request) =>
        Path.Combine(DebugFolder(options, request), "full-page-word-boxes.json");

    public static string WordOverlayPath(TesseractPythonOptions options, OcrProcessingRequest request) =>
        Path.Combine(DebugFolder(options, request), "full-page-word-overlay.png");

    public static string RenderedPagePath(TesseractPythonOptions options, OcrProcessingRequest request) =>
        Path.Combine(DebugFolder(options, request), "full-page-rendered.png");
}
