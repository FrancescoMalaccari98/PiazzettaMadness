using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Infrastructure.Preparation;

public interface ILayoutCropPreparer
{
    Task<LayoutCropPreparationResult> PrepareAsync(OcrProcessingRequest request, LayoutCropOptions options, CancellationToken cancellationToken = default);
}
