using System.IO;
using BasketPdfStats.App.Services;
using BasketPdfStats.App.ViewModels;
using BasketPdfStats.Core.Pipeline;
using BasketPdfStats.Infrastructure.Configuration;

namespace BasketPdfStats.App;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();

        var root = AppComposition.FindRuntimeRoot();
        var settings = AppSettingsLoader.Load(Path.Combine(root, "Config", "appsettings.json"));
        var composition = AppComposition.Build(settings, root);

        var alreadyProcessedPdfSelection = new AlreadyProcessedPdfSelectionService(
            composition.AlreadyProcessedPdfDetector,
            new WpfAlreadyProcessedPdfDecisionService());
        var viewModel = new MainViewModel(
            composition.Pipeline,
            new WpfFilePicker(),
            new WinFormsProcessingResultPresenter(),
            alreadyProcessedPdfSelection,
            composition.ImportService,
            new WpfTeamMismatchConfirmationService(),
            new WpfIdentityReviewService());
        DataContext = viewModel;

        Loaded += (_, _) =>
        {
            if (viewModel.LoadMatchesCommand.CanExecute(null))
            {
                viewModel.LoadMatchesCommand.Execute(null);
            }
        };
    }
}
