using BasketPdfStats.App.ViewModels;
using BasketPdfStats.App.Views;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.App.Services;

public sealed class WpfManualReviewService : IManualReviewService
{
    public ManualEditSet? ReviewAndEdit(ProcessingResult result, OcrMatchContext? context)
    {
        var viewModel = new ManualReviewViewModel(result, context);
        var window = new ManualReviewWindow(viewModel)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        return window.ShowDialog() == true ? viewModel.BuildResult() : null;
    }
}
