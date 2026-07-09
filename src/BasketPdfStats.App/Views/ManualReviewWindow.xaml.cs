using BasketPdfStats.App.ViewModels;

namespace BasketPdfStats.App.Views;

public partial class ManualReviewWindow : System.Windows.Window
{
    public ManualReviewWindow(ManualReviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnConfirm(object sender, System.Windows.RoutedEventArgs e)
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
