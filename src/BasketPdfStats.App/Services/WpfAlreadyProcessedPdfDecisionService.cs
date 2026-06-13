using System.IO;
using BasketPdfStats.Core.Pipeline;

namespace BasketPdfStats.App.Services;

public sealed class WpfAlreadyProcessedPdfDecisionService : IAlreadyProcessedPdfDecisionService
{
    public bool ShouldReprocess(string pdfPath)
    {
        return System.Windows.MessageBox.Show(
            $"Il PDF '{Path.GetFileName(pdfPath)}' risulta gia elaborato. Vuoi rielaborarlo?",
            "PDF gia elaborato",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;
    }
}
