using BasketPdfStats.Core.Pipeline;

namespace BasketPdfStats.Tests;

public sealed class AlreadyProcessedPdfSelectionServiceTests
{
    [Fact]
    public async Task New_pdf_enters_pipeline_without_prompt()
    {
        var prompt = new RecordingDecisionService(shouldReprocess: false);
        var service = new AlreadyProcessedPdfSelectionService(new StubDetector(isAlreadyProcessed: false), prompt);

        Assert.True(await service.ShouldProcessAsync("new.pdf"));
        Assert.Equal(0, prompt.CallCount);
    }

    [Fact]
    public async Task Already_processed_pdf_prompts_and_reprocess_choice_enters_pipeline()
    {
        var prompt = new RecordingDecisionService(shouldReprocess: true);
        var status = new List<string>();
        var service = new AlreadyProcessedPdfSelectionService(new StubDetector(isAlreadyProcessed: true), prompt);

        Assert.True(await service.ShouldProcessAsync("sample.pdf", status.Add));
        Assert.Equal(1, prompt.CallCount);
        Assert.Equal(
            ["PDF already processed: sample.pdf", "User chose to reprocess: sample.pdf"],
            status);
    }

    [Fact]
    public async Task Already_processed_pdf_prompts_and_skip_choice_does_not_enter_pipeline()
    {
        var prompt = new RecordingDecisionService(shouldReprocess: false);
        var status = new List<string>();
        var service = new AlreadyProcessedPdfSelectionService(new StubDetector(isAlreadyProcessed: true), prompt);

        Assert.False(await service.ShouldProcessAsync("sample.pdf", status.Add));
        Assert.Equal(1, prompt.CallCount);
        Assert.Equal(
            ["PDF already processed: sample.pdf", "User chose to skip already processed PDF: sample.pdf"],
            status);
    }

    [Fact]
    public void App_composition_builds_detector_and_pipeline()
    {
        // Fase 2: la costruzione del grafo non-UI è in AppComposition (WPF-free).
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "BasketPdfStats.App", "AppComposition.cs"));

        Assert.Contains("new AlreadyProcessedPdfDetector(settings.Runtime)", source);
        Assert.Contains("new PdfProcessingPipeline(", source);
        Assert.Contains("new OcrImportService(settings.OcrApi)", source);
    }

    [Fact]
    public void Main_window_wires_composition_and_wpf_adapters()
    {
        // Fase 2: MainWindow non conosce più engine/opzioni; usa AppComposition + adapter WPF.
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "BasketPdfStats.App", "MainWindow.xaml.cs"));

        Assert.Contains("AppComposition.Build(settings, root)", source);
        Assert.Contains("composition.AlreadyProcessedPdfDetector", source);
        Assert.Contains("new WpfAlreadyProcessedPdfDecisionService()", source);
        Assert.Contains("new WpfTeamMismatchConfirmationService()", source);
        // MainWindow non deve più conoscere le opzioni specifiche dei motori OCR.
        Assert.DoesNotContain("TesseractPythonOptions", source);
        Assert.DoesNotContain("PaddleCropOptions", source);
        Assert.DoesNotContain("EngineWeightOptions", source);
    }

    [Fact]
    public void Main_view_model_processes_only_the_selected_pdf_after_duplicate_check()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "BasketPdfStats.App", "ViewModels", "MainViewModel.cs"));

        Assert.Contains("if (!await ShouldProcessAsync(SelectedPdfPath))", source);
        Assert.Contains("await _pipeline.ProcessPdfAsync(SelectedPdfPath, selection)", source);
        Assert.Contains("UseTesseract = true", source);
        Assert.Contains("UsePaddle = true", source);
        Assert.Contains("CheckTeamMismatchBeforeImport(matchOption, matchId)", source);
        Assert.Contains("Import bloccato: squadre non corrispondenti.", source);
        Assert.Contains("_importService.ImportAsync(", source);
        Assert.Contains("teamGuard.AllowTeamMismatch", source);
        Assert.DoesNotContain("ProcessInputFolder", source);
        Assert.DoesNotContain("Directory.EnumerateFiles", source);
    }

    [Fact]
    public void Main_window_exposes_single_pdf_workflow_only()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "BasketPdfStats.App", "MainWindow.xaml"));

        Assert.Contains("Seleziona PDF", source);
        Assert.Contains("Elabora PDF", source);
        Assert.DoesNotContain("Elabora cartella Input", source);
        Assert.DoesNotContain("<CheckBox", source);
    }

    private sealed class StubDetector : IAlreadyProcessedPdfDetector
    {
        private readonly bool _isAlreadyProcessed;

        public StubDetector(bool isAlreadyProcessed)
        {
            _isAlreadyProcessed = isAlreadyProcessed;
        }

        public Task<AlreadyProcessedPdfDetection> DetectAsync(string pdfPath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AlreadyProcessedPdfDetection { IsAlreadyProcessed = _isAlreadyProcessed });
        }
    }

    private sealed class RecordingDecisionService : IAlreadyProcessedPdfDecisionService
    {
        private readonly bool _shouldReprocess;

        public RecordingDecisionService(bool shouldReprocess)
        {
            _shouldReprocess = shouldReprocess;
        }

        public int CallCount { get; private set; }

        public bool ShouldReprocess(string pdfPath)
        {
            CallCount++;
            return _shouldReprocess;
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "BasketPdfStats.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate BasketPdfStats.sln.");
    }
}
