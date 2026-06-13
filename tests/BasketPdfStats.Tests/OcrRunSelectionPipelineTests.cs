using System.Text.Json;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Infrastructure.Configuration;
using BasketPdfStats.Infrastructure.Pipeline;
using BasketPdfStats.Infrastructure.Serialization;
using BasketPdfStats.Ocr.Mock;

namespace BasketPdfStats.Tests;

public sealed class OcrRunSelectionPipelineTests
{
    [Fact]
    public void Default_selection_enables_tesseract_only()
    {
        var selection = new OcrRunSelection();

        Assert.True(selection.UseTesseract);
        Assert.False(selection.UsePaddle);
        Assert.True(selection.TryValidate(out var error));
        Assert.Empty(error);
    }

    [Fact]
    public void Empty_selection_is_rejected_with_clear_message()
    {
        var selection = new OcrRunSelection
        {
            UseTesseract = false,
            UsePaddle = false
        };

        Assert.False(selection.TryValidate(out var error));
        Assert.Equal("Seleziona almeno un OCR da utilizzare.", error);
    }

    [Fact]
    public async Task Tesseract_only_runs_and_produces_final_json()
    {
        var root = CreateTempRoot();
        try
        {
            var tesseract = new RecordingTesseractEngine();
            var pipeline = new PdfProcessingPipeline(Runtime(root), [tesseract]);

            var result = await pipeline.ProcessPdfAsync(InputPdf(root), new OcrRunSelection { UseTesseract = true });

            Assert.Equal(FileProcessingStatus.CompletedValidated, result.ProcessedFile.Status);
            Assert.Equal(1, tesseract.CallCount);
            var normalizedPath = Assert.Single(NormalizedFiles(root, OcrStrategyNames.TesseractFullPage));
            var normalized = JsonSerializer.Deserialize<ProcessingResult>(await File.ReadAllTextAsync(normalizedPath), JsonDefaults.Options)!;
            Assert.Contains(normalized.OcrRuns, run => run.Engine == OcrStrategyNames.TesseractFullPage && run.Status == OcrRunStatus.Success);
            Assert.Single(result.Reconciliation!.Providers);
            Assert.Equal([OcrStrategyNames.TesseractFullPage], result.Reconciliation.Providers);
            var final = JsonSerializer.Deserialize<ProcessingResult>(await File.ReadAllTextAsync(result.ProcessedFile.OutputJsonPath!), JsonDefaults.Options)!;
            Assert.Equal(normalized.SchemaVersion, final.SchemaVersion);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task Empty_selection_is_rejected_by_pipeline_before_any_engine_runs()
    {
        var root = CreateTempRoot();
        try
        {
            var tesseract = new RecordingTesseractEngine();
            var pipeline = new PdfProcessingPipeline(Runtime(root), [tesseract]);

            var result = await pipeline.ProcessPdfAsync(InputPdf(root), new OcrRunSelection
            {
                UseTesseract = false,
                UsePaddle = false
            });

            Assert.Equal(FileProcessingStatus.Failed, result.ProcessedFile.Status);
            Assert.Equal("Seleziona almeno un OCR da utilizzare.", result.ProcessedFile.ErrorMessage);
            Assert.Equal(0, tesseract.CallCount);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task Two_engines_are_reconciled_into_one_final_json()
    {
        var root = CreateTempRoot();
        try
        {
            var tesseract = new RecordingTesseractEngine(OcrStrategyNames.TesseractFullPage);
            var crops = new RecordingTesseractEngine(OcrStrategyNames.TesseractLayoutCrops);
            var pipeline = new PdfProcessingPipeline(Runtime(root), [tesseract, crops]);

            var result = await pipeline.ProcessPdfAsync(InputPdf(root), new OcrRunSelection { UseTesseract = true });

            Assert.Equal(1, tesseract.CallCount);
            Assert.Equal(1, crops.CallCount);
            Assert.True(result.Reconciliation!.Enabled);
            Assert.Equal(2, result.Reconciliation.Providers.Count);
            Assert.Single(Directory.EnumerateFiles(Runtime(root).OutputJsonPath, "*.json"),
                path => Path.GetFileName(path) != "processed-index.json");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    private static RuntimeOptions Runtime(string root) => new() { RuntimeRoot = root };

    private static string[] NormalizedFiles(string root, string providerName)
    {
        var folder = Path.Combine(Runtime(root).NormalizedOcrPath, providerName);
        return Directory.Exists(folder) ? Directory.GetFiles(folder, "*.normalized.json") : [];
    }

    private static string InputPdf(string root)
    {
        var runtime = Runtime(root);
        Directory.CreateDirectory(runtime.InputPath);
        var path = Path.Combine(runtime.InputPath, "sample.pdf");
        File.WriteAllBytes(path, [1, 2, 3, 4]);
        return path;
    }

    private static string CreateTempRoot() =>
        Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));

    private static void DeleteTempRoot(string root)
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    private sealed class RecordingTesseractEngine : IOcrEngine
    {
        private readonly string _name;
        public RecordingTesseractEngine(string name = OcrStrategyNames.TesseractFullPage) => _name = name;
        public string EngineName => _name;
        public int CallCount { get; private set; }

        public async Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            var result = await new MockOcrEngine().ProcessAsync(request, cancellationToken);
            result.OcrRuns = [new OcrRunResult { Engine = _name, Status = OcrRunStatus.Success }];
            foreach (var candidate in result.Stats.SelectMany(stat => stat.Candidates))
            {
                candidate.Engine = _name;
            }

            return result;
        }
    }
}
