using BasketPdfStats.Core.Models;

namespace BasketPdfStats.App.ViewModels;

public sealed class ProcessedFileRowViewModel
{
    public ProcessedFileRowViewModel(ProcessedFile processedFile)
    {
        FileName = processedFile.FileName;
        Status = processedFile.Status.ToString();
        OutputJsonPath = processedFile.OutputJsonPath ?? string.Empty;
        ShortHash = BuildShortHash(processedFile.DocumentHash);
    }

    public string FileName { get; }
    public string Status { get; }
    public string ShortHash { get; }
    public string OutputJsonPath { get; }

    private static string BuildShortHash(string hash)
    {
        var normalized = hash.Replace("sha256:", string.Empty, StringComparison.OrdinalIgnoreCase);
        return normalized.Length <= 12 ? normalized : normalized[..12];
    }
}
