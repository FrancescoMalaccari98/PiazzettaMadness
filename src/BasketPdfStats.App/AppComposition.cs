using System.IO;
using BasketPdfStats.Core.Database;
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

/// <summary>
/// Composition root non-UI: costruisce il grafo dei servizi (engine OCR, pipeline,
/// client DB, detector) a partire da <see cref="AppSettings"/>. Non dipende da WPF/WinForms,
/// così può essere testata senza UI. Gli adapter WPF (file picker, presenter, dialog) restano
/// in <c>MainWindow</c>.
/// </summary>
public sealed class AppComposition
{
    public required IPdfProcessingPipeline Pipeline { get; init; }
    public required IOcrImportService? ImportService { get; init; }
    public required IAlreadyProcessedPdfDetector AlreadyProcessedPdfDetector { get; init; }

    public static AppComposition Build(AppSettings settings, string projectRoot, ITeamMismatchProcessingDecider? teamMismatchDecider = null)
    {
        var preparation = CreateDocumentPreparation(settings, projectRoot);
        var engineWeights = EngineWeightOptions.FromJsonElement(settings.Reconciliation);
        var pipeline = new PdfProcessingPipeline(
            settings.Runtime,
            CreateEngines(settings, projectRoot),
            preparation.RunPlanBuilder,
            preparation.Stage,
            normalizedOcrReconciler: new NormalizedOcrReconciler(weights: engineWeights),
            teamMismatchDecider: teamMismatchDecider);

        var importService = string.IsNullOrWhiteSpace(settings.OcrApi.BaseUrl)
            ? null
            : new OcrImportService(settings.OcrApi);

        return new AppComposition
        {
            Pipeline = pipeline,
            ImportService = importService,
            AlreadyProcessedPdfDetector = new AlreadyProcessedPdfDetector(settings.Runtime),
        };
    }

    /// <summary>
    /// Risale dalla directory corrente (o dalla base dell'app) fino a trovare
    /// <c>Config/appsettings.json</c>. Bootstrap dell'app, indipendente dalla UI.
    /// </summary>
    public static string FindRuntimeRoot()
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
}
