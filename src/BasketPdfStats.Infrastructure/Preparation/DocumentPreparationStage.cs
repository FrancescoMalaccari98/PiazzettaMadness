using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Infrastructure.Preparation;

public sealed class DocumentPreparationStage
{
    private readonly LayoutCropOptions _options;
    private readonly LayoutCropResolver _resolver;
    private readonly ILayoutCropPreparer _preparer;

    public DocumentPreparationStage(LayoutCropOptions options)
        : this(options, new LayoutCropResolver(), new LayoutCropPreparer())
    {
    }

    public DocumentPreparationStage(LayoutCropOptions options, LayoutCropResolver resolver, ILayoutCropPreparer preparer)
    {
        _options = options;
        _resolver = resolver;
        _preparer = preparer;
    }

    public async Task<DocumentPreparationResult> PrepareAsync(OcrProcessingRequest request, OcrRunPlan plan, CancellationToken cancellationToken = default)
    {
        if (!plan.NeedsLayoutCrops)
        {
            return new DocumentPreparationResult
            {
                LayoutCropsRequested = false,
                LayoutCropFolder = plan.LayoutCropFolder,
                LayoutImageFolder = plan.LayoutImageFolder
            };
        }

        var resolved = _resolver.Resolve(request, _options);
        if (resolved.LayoutCropsAvailable || !_options.PrepareCropsIfMissing)
        {
            return resolved;
        }

        var prepared = await _preparer.PrepareAsync(request, _options, cancellationToken);
        if (!prepared.Success)
        {
            resolved.Errors.AddRange(prepared.Errors);
            resolved.Warnings.AddRange(prepared.Warnings);
            return resolved;
        }

        var refreshed = _resolver.Resolve(request, _options);
        refreshed.LayoutCropsPrepared = true;
        refreshed.Warnings.InsertRange(0, resolved.Warnings);
        refreshed.Warnings.AddRange(prepared.Warnings);
        return refreshed;
    }
}
