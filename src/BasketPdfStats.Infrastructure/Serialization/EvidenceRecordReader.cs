using System.Text.Json;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Infrastructure.Serialization;

/// <summary>
/// Reads a shared evidence_records.json file (any OCR channel) into the
/// EvidenceRecordsEnvelope model. Deserialization only — no normalization or
/// reconciliation here.
/// </summary>
public sealed class EvidenceRecordReader
{
    public async Task<EvidenceRecordsEnvelope> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var envelope = await JsonSerializer.DeserializeAsync<EvidenceRecordsEnvelope>(
            stream, JsonDefaults.Options, cancellationToken);
        return envelope ?? throw new InvalidOperationException($"Evidence records file is empty or invalid: {path}");
    }

    public EvidenceRecordsEnvelope Parse(string json)
    {
        var envelope = JsonSerializer.Deserialize<EvidenceRecordsEnvelope>(json, JsonDefaults.Options);
        return envelope ?? throw new InvalidOperationException("Evidence records JSON is empty or invalid.");
    }
}
