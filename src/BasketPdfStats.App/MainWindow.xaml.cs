using System.IO;
using BasketPdfStats.App.Services;
using BasketPdfStats.App.ViewModels;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Core.Pipeline;
using BasketPdfStats.Infrastructure.Configuration;
using BasketPdfStats.Infrastructure.Database;
using BasketPdfStats.Infrastructure.FileSystem;
using BasketPdfStats.Infrastructure.Pipeline;
using BasketPdfStats.Infrastructure.Preparation;
using BasketPdfStats.Infrastructure.Reconciliation;
using BasketPdfStats.Ocr.Mock;
using BasketPdfStats.Ocr.TesseractPython;

namespace BasketPdfStats.App;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();
        var root = FindRuntimeRoot();
        var settings = AppSettingsLoader.Load(Path.Combine(root, "Config", "appsettings.json"));

        var preparation = CreateDocumentPreparation(settings, root);
        var engineWeights = EngineWeightOptions.FromJsonElement(settings.Reconciliation);
        var pipeline = new PdfProcessingPipeline(
            settings.Runtime,
            CreateEngines(settings, root),
            preparation.RunPlanBuilder,
            preparation.Stage,
            normalizedOcrReconciler: new NormalizedOcrReconciler(weights: engineWeights));
        var alreadyProcessedPdfSelection = new AlreadyProcessedPdfSelectionService(
            new AlreadyProcessedPdfDetector(settings.Runtime),
            new WpfAlreadyProcessedPdfDecisionService());
        var importService = string.IsNullOrWhiteSpace(settings.OcrApi.BaseUrl)
            ? null
            : new OcrImportService(settings.OcrApi);
        var viewModel = new MainViewModel(
            pipeline,
            new WpfFilePicker(),
            new WinFormsProcessingResultPresenter(),
            alreadyProcessedPdfSelection,
            importService,
            new WpfTeamMismatchConfirmationService());
        DataContext = viewModel;

        Loaded += (_, _) =>
        {
            if (viewModel.LoadMatchesCommand.CanExecute(null))
            {
                viewModel.LoadMatchesCommand.Execute(null);
            }
        };
    }

    private static IReadOnlyList<IOcrEngine> CreateEngines(AppSettings settings, string projectRoot)
    {
        var engines = new List<IOcrEngine>();
        foreach (var engineName in settings.Ocr.EnabledEngines)
        {
            if (string.Equals(engineName, "MockOcr", StringComparison.OrdinalIgnoreCase))
            {
                engines.Add(new MockOcrEngine());
            }
            else if (string.Equals(engineName, OcrStrategyNames.TesseractFullPage, StringComparison.OrdinalIgnoreCase))
            {
                var options = TesseractPythonOptions.FromJsonElement(settings.Ocr.TesseractPython, projectRoot);
                engines.Add(new TesseractFullPageOcrEngine(options));
            }
            else if (string.Equals(engineName, OcrStrategyNames.TesseractLayoutCrops, StringComparison.OrdinalIgnoreCase))
            {
                var options = TesseractPythonOptions.FromJsonElement(settings.Ocr.TesseractPython, projectRoot);
                options.Mode = "Crops";
                options.UseLayoutCrops = true;
                engines.Add(new TesseractLayoutCropsOcrEngine(options));
            }
            else if (string.Equals(engineName, OcrStrategyNames.PaddleLayoutCrops, StringComparison.OrdinalIgnoreCase))
            {
                var options = PaddleCropOptions.FromJsonElement(settings.Ocr.PaddleCrop, projectRoot);
                options.Enabled = true;
                engines.Add(new PaddleCropOcrEngine(options));
            }
            else if (string.Equals(engineName, OcrStrategyNames.PaddleTableRows, StringComparison.OrdinalIgnoreCase))
            {
                var options = PaddleCropOptions.FromJsonElement(settings.Ocr.PaddleCrop, projectRoot);
                options.Enabled = true;
                engines.Add(new PaddleRowOcrEngine(options));
            }
        }

        return engines.Count == 0 ? [new MockOcrEngine()] : engines;
    }

    private static (OcrRunPlanBuilder RunPlanBuilder, DocumentPreparationStage Stage) CreateDocumentPreparation(AppSettings settings, string projectRoot)
    {
        var cropOptions = LayoutCropOptions.FromJsonElement(settings.LayoutPreparation, projectRoot);
        var tesseractOptions = TesseractPythonOptions.FromJsonElement(settings.Ocr.TesseractPython, projectRoot);
        var runPlanOptions = new OcrRunPlanBuilderOptions
        {
            RuntimeRoot = projectRoot,
            TesseractEnabled = tesseractOptions.Enabled,
            TesseractUseLayoutCrops = settings.Ocr.EnabledEngines.Any(engine =>
                string.Equals(engine, OcrStrategyNames.TesseractLayoutCrops, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(engine, OcrStrategyNames.PaddleLayoutCrops, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(engine, OcrStrategyNames.PaddleTableRows, StringComparison.OrdinalIgnoreCase)),
            TesseractPreprocessImages = tesseractOptions.PreprocessImages,
            TesseractPreprocessedFolder = tesseractOptions.PreprocessedCropOutputFolder
        };

        return (new OcrRunPlanBuilder(cropOptions, runPlanOptions), new DocumentPreparationStage(cropOptions));
    }

    private static string FindRuntimeRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var current = new DirectoryInfo(start);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Config", "appsettings.json")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        return Directory.GetCurrentDirectory();
    }
}
