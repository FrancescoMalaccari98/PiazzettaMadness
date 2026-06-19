using BasketPdfStats.App.ViewModels;

namespace BasketPdfStats.App.Views;

public partial class IdentityReviewWindow : System.Windows.Window
{
    public IdentityReviewWindow(IdentityReviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnConfirmExport(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
