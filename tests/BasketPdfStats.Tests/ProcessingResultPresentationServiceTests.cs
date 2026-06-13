using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Core.Presentation;

namespace BasketPdfStats.Tests;

public sealed class ProcessingResultPresentationServiceTests
{
    [Theory]
    [InlineData(FileProcessingStatus.CompletedValidated)]
    [InlineData(FileProcessingStatus.CompletedWithWarnings)]
    [InlineData(FileProcessingStatus.CompletedNotValidated)]
    public void Successful_result_is_presented(FileProcessingStatus status)
    {
        var presenter = new RecordingPresenter();
        var service = new ProcessingResultPresentationService(presenter);
        var result = Result("sample.pdf", status);
        var statusMessages = new List<string>();

        var presented = service.TryPresent(result, statusMessages.Add);

        Assert.True(presented);
        Assert.Same(result, Assert.Single(presenter.Results));
        Assert.Equal(
            new[]
            {
                "Processing completed for sample.pdf",
                "Opening WinForms result viewer for sample.pdf",
                "WinForms result viewer shown for sample.pdf"
            },
            statusMessages);
    }

    [Theory]
    [InlineData(FileProcessingStatus.Failed)]
    [InlineData(FileProcessingStatus.SkippedDuplicate)]
    public void Failed_or_skipped_result_is_not_presented(FileProcessingStatus status)
    {
        var presenter = new RecordingPresenter();
        var service = new ProcessingResultPresentationService(presenter);

        var presented = service.TryPresent(Result("sample.pdf", status));

        Assert.False(presented);
        Assert.Empty(presenter.Results);
    }

    [Fact]
    public void Batch_presents_each_successful_result_once()
    {
        var presenter = new RecordingPresenter();
        var service = new ProcessingResultPresentationService(presenter);
        var results = new[]
        {
            Result("first.pdf", FileProcessingStatus.CompletedValidated),
            Result("second.pdf", FileProcessingStatus.CompletedWithWarnings)
        };

        foreach (var result in results)
        {
            service.TryPresent(result);
        }

        Assert.Equal(2, presenter.Results.Count);
        Assert.Same(results[0], presenter.Results[0]);
        Assert.Same(results[1], presenter.Results[1]);
    }

    [Fact]
    public void Presenter_receives_the_final_reconciled_processing_result_instance()
    {
        var presenter = new RecordingPresenter();
        var service = new ProcessingResultPresentationService(presenter);
        var result = Result("reconciled.pdf", FileProcessingStatus.CompletedValidated);
        result.Reconciliation = new ReconciliationMetadata
        {
            Enabled = true,
            Providers = [OcrStrategyNames.TesseractFullPage, OcrStrategyNames.TesseractLayoutCrops],
            Strategy = "MVP field-by-field"
        };

        service.TryPresent(result);

        var presented = Assert.Single(presenter.Results);
        Assert.Same(result, presented);
        var reconciliation = Assert.IsType<ReconciliationMetadata>(presented.Reconciliation);
        Assert.True(reconciliation.Enabled);
        Assert.Equal(new[] { OcrStrategyNames.TesseractFullPage, OcrStrategyNames.TesseractLayoutCrops }, reconciliation.Providers);
    }

    [Fact]
    public void Presenter_failure_is_reported_without_escaping()
    {
        var service = new ProcessingResultPresentationService(new ThrowingPresenter());
        var statusMessages = new List<string>();

        var presented = service.TryPresent(Result("broken.pdf", FileProcessingStatus.CompletedValidated), statusMessages.Add);

        Assert.False(presented);
        Assert.Equal("Failed to show result viewer: Viewer failed.", statusMessages[^1]);
    }

    [Fact]
    public void Main_window_composition_uses_winforms_presenter()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "BasketPdfStats.App", "MainWindow.xaml.cs"));

        Assert.Contains("new WinFormsProcessingResultPresenter()", source);
        Assert.DoesNotContain("new WpfProcessingResultPresenter()", source);
    }

    [Fact]
    public void Result_viewer_links_player_rows_to_stats_by_canonical_entity_id()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "BasketPdfStats.App", "ViewModels", "ProcessingResultViewModel.cs"));

        Assert.Contains("stat.EntityId, player.EntityId", source);
        Assert.DoesNotContain("stat.EntityId, player.PlayerId", source);
    }

    private static ProcessingResult Result(string fileName, FileProcessingStatus status) =>
        new()
        {
            ProcessedFile = new ProcessedFile
            {
                FileName = fileName,
                Status = status
            }
        };

    private sealed class RecordingPresenter : IProcessingResultPresenter
    {
        public List<ProcessingResult> Results { get; } = [];

        public void Show(ProcessingResult result)
        {
            Results.Add(result);
        }
    }

    private sealed class ThrowingPresenter : IProcessingResultPresenter
    {
        public void Show(ProcessingResult result)
        {
            throw new InvalidOperationException("Viewer failed.");
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
