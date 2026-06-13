using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Infrastructure.Configuration;
using BasketPdfStats.Infrastructure.Pipeline;
using BasketPdfStats.Ocr.Mock;

namespace BasketPdfStats.Tests;

public sealed class MultiEnginePipelineTests
{
    [Fact]
    public async Task Failed_engine_does_not_block_successful_mock_engine()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "sample.pdf");
            await File.WriteAllBytesAsync(pdf, [9, 8, 7, 6]);

            var pipeline = new PdfProcessingPipeline(options, [new FailedOcrEngine(), new MockOcrEngine()]);
            var result = await pipeline.ProcessPdfAsync(pdf);

            Assert.NotEqual(FileProcessingStatus.Failed, result.ProcessedFile.Status);
            Assert.Contains(result.OcrRuns, x => x.Engine == "FailedOcr" && x.Status == OcrRunStatus.Failed);
            Assert.Contains(result.OcrRuns, x => x.Engine == "MockOcr" && x.Status == OcrRunStatus.Success);
            Assert.True(File.Exists(result.ProcessedFile.OutputJsonPath));
            Assert.True(File.Exists(result.ProcessedFile.FinalPath));
            Assert.Equal(pdf, result.ProcessedFile.FinalPath);
            Assert.True(File.Exists(pdf));
            Assert.False(ContainsPdf(options.ProcessedPath));
            Assert.Empty(Directory.EnumerateFiles(options.WorkingPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Failed_processing_preserves_original_selected_pdf()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "failed.pdf");
            await File.WriteAllBytesAsync(pdf, [9, 8, 7, 6]);

            var pipeline = new PdfProcessingPipeline(options, [new FailedOcrEngine()]);
            var result = await pipeline.ProcessPdfAsync(pdf);

            Assert.Equal(FileProcessingStatus.Failed, result.ProcessedFile.Status);
            Assert.Equal(pdf, result.ProcessedFile.FinalPath);
            Assert.True(File.Exists(pdf));
            Assert.False(ContainsPdf(options.ErrorPath));
            Assert.Empty(Directory.EnumerateFiles(options.WorkingPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static bool ContainsPdf(string folder) =>
        Directory.Exists(folder) && Directory.EnumerateFiles(folder, "*.pdf").Any();

    private sealed class FailedOcrEngine : IOcrEngine
    {
        public string EngineName => "FailedOcr";

        public Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProcessingResult
            {
                OcrRuns =
                [
                    new OcrRunResult
                    {
                        Engine = EngineName,
                        Status = OcrRunStatus.Failed,
                        Error = "simulated failure"
                    }
                ]
            });
        }
    }
}
