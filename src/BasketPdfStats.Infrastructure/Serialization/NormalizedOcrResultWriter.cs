using System.Text.Json;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Infrastructure.Configuration;
using BasketPdfStats.Infrastructure.FileSystem;

namespace BasketPdfStats.Infrastructure.Serialization;

public sealed class NormalizedOcrResultWriter
{
    private readonly RuntimeOptions _options;

    public NormalizedOcrResultWriter(RuntimeOptions options)
    {
        _options = options;
    }

    public async Task<string> WriteAsync(
        string providerName,
        OcrProcessingRequest request,
        ProcessingResult result,
        CancellationToken cancellationToken = default)
    {
        var providerFolder = Path.Combine(_options.NormalizedOcrPath, SanitizePathSegment(providerName));
        Directory.CreateDirectory(providerFolder);
        PopulateProcessedFile(request, result);
        var stem = Path.GetFileNameWithoutExtension(request.OriginalFileName);
        var shortHash = DocumentHasher.ShortHash(request.DocumentHash);
        var outputPath = Path.Combine(providerFolder, $"{stem}.{shortHash}.normalized.json");
        await using var stream = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(stream, result, JsonDefaults.Options, cancellationToken);
        return outputPath;
    }

    private static void PopulateProcessedFile(OcrProcessingRequest request, ProcessingResult result)
    {
        result.ProcessedFile.FileName = request.OriginalFileName;
        result.ProcessedFile.SourcePath = request.PdfPath;
        result.ProcessedFile.WorkingPath = request.WorkingPath;
        result.ProcessedFile.DocumentHash = request.DocumentHash;
        result.ProcessedFile.Status = result.Validation.Status;
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(value.Where(character => !invalid.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "UnknownProvider" : clean;
    }
}
