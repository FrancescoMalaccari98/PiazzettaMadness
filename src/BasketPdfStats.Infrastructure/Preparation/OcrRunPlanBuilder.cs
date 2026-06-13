using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;

namespace BasketPdfStats.Infrastructure.Preparation;

public sealed class OcrRunPlanBuilder
{
    private readonly LayoutCropOptions _cropOptions;
    private readonly OcrRunPlanBuilderOptions _options;

    public OcrRunPlanBuilder(LayoutCropOptions cropOptions, OcrRunPlanBuilderOptions options)
    {
        _cropOptions = cropOptions;
        _options = options;
    }

    public OcrRunPlan Build(OcrProcessingRequest request, OcrRunSelection selection)
    {
        var useTesseractCrops = selection.UseTesseract && _options.TesseractEnabled && _options.TesseractUseLayoutCrops;
        var plan = new OcrRunPlan
        {
            PdfPath = request.PdfPath,
            OriginalFileName = request.OriginalFileName,
            DocumentHash = request.DocumentHash,
            Selection = selection,
            SelectedOcrNames =
            [
                .. selection.UseTesseract ? [OcrStrategyNames.TesseractFullPage] : Array.Empty<string>(),
                .. useTesseractCrops ? [OcrStrategyNames.TesseractLayoutCrops] : Array.Empty<string>(),
                .. selection.UsePaddle ? [OcrStrategyNames.PaddleLayoutCrops, OcrStrategyNames.PaddleTableRows] : Array.Empty<string>()
            ],
            NeedsLayoutCrops = useTesseractCrops || selection.UsePaddle,
            NeedsTesseractPreprocessing = useTesseractCrops && _options.TesseractPreprocessImages,
            LayoutCropFolder = LayoutCropPaths.DocumentCropFolder(request, _cropOptions),
            LayoutImageFolder = LayoutCropPaths.DocumentImageFolder(request, _cropOptions),
            TesseractPreprocessedFolder = Path.Combine(_options.ResolvePath(_options.TesseractPreprocessedFolder), LayoutCropPaths.SafeName(Path.GetFileNameWithoutExtension(request.OriginalFileName)))
        };
        return plan;
    }
}
