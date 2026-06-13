using BasketPdfStats.Core.Pipeline;
using BasketPdfStats.Infrastructure.Configuration;

namespace BasketPdfStats.Infrastructure.FileSystem;

public sealed class AlreadyProcessedPdfDetector : IAlreadyProcessedPdfDetector
{
    private readonly RuntimeOptions _options;
    private readonly ProcessedIndexStore _indexStore;

    public AlreadyProcessedPdfDetector(RuntimeOptions options)
    {
        _options = options;
        _indexStore = new ProcessedIndexStore(options.ProcessedIndexPath);
    }

    public async Task<AlreadyProcessedPdfDetection> DetectAsync(
        string pdfPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(pdfPath))
        {
            return new AlreadyProcessedPdfDetection();
        }

        var documentHash = await DocumentHasher.ComputeSha256Async(pdfPath, cancellationToken);
        if (await _indexStore.FindByHashAsync(documentHash, cancellationToken) is not null)
        {
            return Detected("processed-index.json contains the same document hash");
        }

        if (HasMatchingOutputJson(pdfPath, documentHash))
        {
            return Detected("OutputJson contains a final JSON matching the current naming policy");
        }

        if (await HasMatchingProcessedPdfAsync(pdfPath, documentHash, cancellationToken))
        {
            return Detected("Elaborati contains a PDF with the same file name and document hash");
        }

        return new AlreadyProcessedPdfDetection();
    }

    private bool HasMatchingOutputJson(string pdfPath, string documentHash)
    {
        if (!Directory.Exists(_options.OutputJsonPath))
        {
            return false;
        }

        var stem = Path.GetFileNameWithoutExtension(pdfPath);
        var expectedPrefix = $"{stem}.{DocumentHasher.ShortHash(documentHash)}";
        return Directory.EnumerateFiles(_options.OutputJsonPath, "*.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Any(fileName => fileName is not null &&
                fileName.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> HasMatchingProcessedPdfAsync(
        string pdfPath,
        string documentHash,
        CancellationToken cancellationToken)
    {
        var processedPdf = Path.Combine(_options.ProcessedPath, Path.GetFileName(pdfPath));
        return File.Exists(processedPdf) &&
            string.Equals(
                await DocumentHasher.ComputeSha256Async(processedPdf, cancellationToken),
                documentHash,
                StringComparison.OrdinalIgnoreCase);
    }

    private static AlreadyProcessedPdfDetection Detected(string evidence)
    {
        return new AlreadyProcessedPdfDetection
        {
            IsAlreadyProcessed = true,
            Evidence = evidence
        };
    }
}
