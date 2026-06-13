using BasketPdfStats.Core.Enums;
using BasketPdfStats.Infrastructure.FileSystem;
using BasketPdfStats.Infrastructure.Configuration;
using BasketPdfStats.Infrastructure.Pipeline;
using BasketPdfStats.Ocr.Mock;

namespace BasketPdfStats.Tests;

public sealed class DuplicateDetectionTests
{
    [Fact]
    public async Task Pipeline_reprocesses_duplicate_document_hash_when_it_is_called()
    {
        var root = CreateTempRoot();
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var pipeline = new MockPdfProcessingPipeline(options, new MockOcrEngine());

            var firstPdf = Path.Combine(options.InputPath, "first.pdf");
            var secondPdf = Path.Combine(options.InputPath, "second.pdf");
            Directory.CreateDirectory(options.InputPath);
            await File.WriteAllBytesAsync(firstPdf, [1, 2, 3, 4, 5]);
            await File.WriteAllBytesAsync(secondPdf, [1, 2, 3, 4, 5]);

            var first = await pipeline.ProcessPdfAsync(firstPdf);
            var second = await pipeline.ProcessPdfAsync(secondPdf);

            Assert.Equal(FileProcessingStatus.CompletedValidated, first.ProcessedFile.Status);
            Assert.Equal(FileProcessingStatus.CompletedValidated, second.ProcessedFile.Status);
            Assert.True(File.Exists(Path.Combine(options.OutputJsonPath, "processed-index.json")));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task Detector_reports_processed_index_match_without_skipping_automatically()
    {
        var root = CreateTempRoot();
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var pipeline = new MockPdfProcessingPipeline(options, new MockOcrEngine());
            Directory.CreateDirectory(options.InputPath);
            var firstPdf = Path.Combine(options.InputPath, "first.pdf");
            await File.WriteAllBytesAsync(firstPdf, [1, 2, 3, 4, 5]);
            await pipeline.ProcessPdfAsync(firstPdf);

            var duplicatePdf = Path.Combine(options.InputPath, "duplicate.pdf");
            await File.WriteAllBytesAsync(duplicatePdf, [1, 2, 3, 4, 5]);

            var detection = await new AlreadyProcessedPdfDetector(options).DetectAsync(duplicatePdf);

            Assert.True(detection.IsAlreadyProcessed);
            Assert.Contains("processed-index.json", detection.Evidence);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task Detector_reports_output_json_match_using_current_naming_policy()
    {
        var root = CreateTempRoot();
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            Directory.CreateDirectory(options.InputPath);
            Directory.CreateDirectory(options.OutputJsonPath);
            var pdf = Path.Combine(options.InputPath, "sample.pdf");
            await File.WriteAllBytesAsync(pdf, [1, 2, 3, 4, 5]);
            var hash = await DocumentHasher.ComputeSha256Async(pdf);
            await File.WriteAllTextAsync(
                Path.Combine(options.OutputJsonPath, $"sample.{DocumentHasher.ShortHash(hash)}.json"),
                "{}");

            var detection = await new AlreadyProcessedPdfDetector(options).DetectAsync(pdf);

            Assert.True(detection.IsAlreadyProcessed);
            Assert.Contains("OutputJson", detection.Evidence);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task Detector_reports_matching_pdf_in_processed_folder()
    {
        var root = CreateTempRoot();
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            Directory.CreateDirectory(options.InputPath);
            Directory.CreateDirectory(options.ProcessedPath);
            var pdf = Path.Combine(options.InputPath, "sample.pdf");
            var processedPdf = Path.Combine(options.ProcessedPath, "sample.pdf");
            await File.WriteAllBytesAsync(pdf, [1, 2, 3, 4, 5]);
            await File.WriteAllBytesAsync(processedPdf, [1, 2, 3, 4, 5]);

            var detection = await new AlreadyProcessedPdfDetector(options).DetectAsync(pdf);

            Assert.True(detection.IsAlreadyProcessed);
            Assert.Contains("Elaborati", detection.Evidence);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    private static string CreateTempRoot()
    {
        return Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
    }

    private static void DeleteTempRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
