using BasketPdfStats.App.ViewModels;
using BasketPdfStats.App.Views;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Identity;

namespace BasketPdfStats.App.Services;

public sealed class WpfIdentityReviewService : IIdentityReviewService
{
    public IReadOnlyDictionary<string, int>? ReviewAndConfirm(IReadOnlyList<IdentityReviewItem> items, OcrMatchContext context)
    {
        var viewModel = new IdentityReviewViewModel(items, context);
        var window = new IdentityReviewWindow(viewModel)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        return window.ShowDialog() == true ? viewModel.BuildResolutions() : null;
    }
}
