using System.Text.Json;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Infrastructure.Evidence;

/// <summary>
/// Converts a shared evidence_records.json envelope (any OCR channel) into a
/// ProcessingResult that the reconciler can compare field-by-field.
///
/// Scope of responsibility: it materializes Stats (with full per-candidate
/// provenance) and the final score. It does NOT rebuild player/team identity
/// (names, captain, DNP) — that anagraphic data stays with the full-page
/// channel; evidence channels contribute statistical evidence only.
/// </summary>
public sealed class EvidenceRecordMapper
{
    public ProcessingResult Map(EvidenceRecordsEnvelope envelope)
    {
        var result = new ProcessingResult();
        result.OcrRuns.Add(new OcrRunResult
        {
            Engine = string.IsNullOrWhiteSpace(envelope.SourceId) ? envelope.Engine : envelope.SourceId,
            Status = OcrRunStatus.Success
        });

        foreach (var record in envelope.Records)
        {
            if (IsFinalScore(record))
            {
                result.Game.FinalScore = ValueAsString(record.ValueNormalized) ?? record.ValueRaw;
                if (!string.IsNullOrWhiteSpace(record.EntityId))
                {
                    result.Game.GameId = record.EntityId;
                }

                continue;
            }

            result.Stats.Add(BuildStat(envelope, record));
        }

        return result;
    }

    private static bool IsFinalScore(EvidenceRecord record) =>
        string.Equals(record.Scope, "Game", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(record.StatKey, "finalScore", StringComparison.OrdinalIgnoreCase);

    private static StatValue BuildStat(EvidenceRecordsEnvelope envelope, EvidenceRecord record)
    {
        var value = NormalizeJsonValue(record.ValueNormalized);
        var confidence = record.ConfidenceNormalized ?? record.ConfidenceRaw ?? 0.0;
        return new StatValue
        {
            Scope = ParseScope(record.Scope),
            EntityId = record.EntityId,
            Side = record.Side,
            StatKey = record.StatKey,
            FieldId = record.FieldId,
            Value = value,
            Score = confidence,
            Status = StatValidationStatus.NonValidato,
            Candidates =
            [
                new OcrCandidate
                {
                    Engine = envelope.Engine,
                    SourceId = envelope.SourceId,
                    Granularity = envelope.Granularity,
                    FieldId = record.FieldId,
                    StatKey = record.StatKey,
                    SourceEntityId = record.EntityId,
                    ZoneId = string.IsNullOrWhiteSpace(record.CropId) ? record.ZoneId : record.CropId,
                    Raw = record.ValueRaw,
                    Normalized = value,
                    Confidence = record.ConfidenceRaw,
                    ConfidenceNormalized = record.ConfidenceNormalized,
                    MapperId = record.MapperId,
                    Warnings = [.. record.Warnings],
                    Status = OcrRunStatus.Success
                }
            ]
        };
    }

    private static StatScope ParseScope(string scope) => scope?.Trim().ToLowerInvariant() switch
    {
        "player" => StatScope.Player,
        "team" => StatScope.Team,
        "result" => StatScope.Result,
        "comparative" => StatScope.Comparative,
        _ => StatScope.Game
    };

    private static string? ValueAsString(JsonElement? element)
    {
        if (element is null)
        {
            return null;
        }

        var value = element.Value;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static object? NormalizeJsonValue(JsonElement? element)
    {
        if (element is null)
        {
            return null;
        }

        var value = element.Value;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt32(out var integer) => integer,
            JsonValueKind.Number when value.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }
}
