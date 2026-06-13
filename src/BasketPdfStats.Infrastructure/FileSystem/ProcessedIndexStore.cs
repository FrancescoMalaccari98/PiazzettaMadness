using System.Text.Json;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Infrastructure.FileSystem;

public sealed class ProcessedIndexStore
{
    private readonly string _indexPath;

    public ProcessedIndexStore(string indexPath)
    {
        _indexPath = indexPath;
    }

    public async Task<ProcessedIndex> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_indexPath))
        {
            return new ProcessedIndex();
        }

        await using var stream = File.OpenRead(_indexPath);
        return await JsonSerializer.DeserializeAsync<ProcessedIndex>(stream, JsonDefaults.Options, cancellationToken)
            ?? new ProcessedIndex();
    }

    public async Task<ProcessedIndexEntry?> FindByHashAsync(string documentHash, CancellationToken cancellationToken = default)
    {
        var index = await LoadAsync(cancellationToken);
        return index.Documents.FirstOrDefault(x => string.Equals(x.DocumentHash, documentHash, StringComparison.OrdinalIgnoreCase));
    }

    public async Task UpsertAsync(ProcessingResult result, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_indexPath)!);
        var index = await LoadAsync(cancellationToken);
        var existing = index.Documents.FirstOrDefault(x => string.Equals(x.DocumentHash, result.ProcessedFile.DocumentHash, StringComparison.OrdinalIgnoreCase));
        var entry = new ProcessedIndexEntry
        {
            DocumentHash = result.ProcessedFile.DocumentHash,
            FileName = result.ProcessedFile.FileName,
            Status = result.ProcessedFile.Status,
            OutputJsonPath = result.ProcessedFile.OutputJsonPath,
            FinalPath = result.ProcessedFile.FinalPath,
            ProcessedAt = result.ProcessedFile.CompletedAt ?? DateTimeOffset.Now
        };

        if (existing is null)
        {
            index.Documents.Add(entry);
        }
        else
        {
            var position = index.Documents.IndexOf(existing);
            index.Documents[position] = entry;
        }

        await using var stream = File.Create(_indexPath);
        await JsonSerializer.SerializeAsync(stream, index, JsonDefaults.Options, cancellationToken);
    }
}
