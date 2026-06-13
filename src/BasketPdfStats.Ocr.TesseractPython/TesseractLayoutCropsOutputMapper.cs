using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Normalization;
using BasketPdfStats.Core.Ocr;

namespace BasketPdfStats.Ocr.TesseractPython;

public sealed partial class TesseractLayoutCropsOutputMapper
{
    public ProcessingResult Map(JsonDocument document, OcrProcessingRequest request)
    {
        var result = new ProcessingResult
        {
            Game = new GameStats
            {
                GameId = $"game:{ShortHash(request.DocumentHash)}",
                HomeTeamId = CanonicalEntityKeyBuilder.Team("Home"),
                AwayTeamId = CanonicalEntityKeyBuilder.Team("Away")
            },
            Validation = new ValidationResult { Status = FileProcessingStatus.CompletedWithWarnings }
        };

        var root = document.RootElement;
        MapWorkerDiagnostics(root, result);
        if (TryGetZone(root, "header.finalScore", out var finalScoreZone))
        {
            MapFinalScore(finalScoreZone, result);
        }
        else
        {
            AddWarning(result, "ocr.tesseractLayoutCrops.finalScoreCropMissing", "The final-score crop was not present in the raw crop OCR artifact.");
        }

        AddWarning(
            result,
            "ocr.tesseractLayoutCrops.partialMapping",
            "TesseractLayoutCrops currently maps only conservative crop-level values. Player-table row and cell extraction remain future TesseractTableRows work.",
            "Info");
        return result;
    }

    private static void MapFinalScore(JsonElement zone, ProcessingResult result)
    {
        var rawText = String(zone, "rawText");
        var matches = ScorePattern().Matches(rawText).Cast<Match>().ToArray();
        var match = matches.FirstOrDefault(candidate => !IsInsideParentheses(rawText, candidate.Index)) ?? matches.FirstOrDefault();
        if (match is null ||
            !int.TryParse(match.Groups["home"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var home) ||
            !int.TryParse(match.Groups["away"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var away))
        {
            AddWarning(result, "ocr.tesseractLayoutCrops.finalScoreUnreadable", "The final-score crop did not contain an unambiguous score.");
            return;
        }

        result.Game.FinalScore = $"{home}-{away}";
        AddResultPoints(result, "Home", home, rawText);
        AddResultPoints(result, "Away", away, rawText);
    }

    private static bool IsInsideParentheses(string value, int index)
    {
        var open = value.LastIndexOf('(', Math.Max(0, index));
        var close = value.LastIndexOf(')', Math.Max(0, index));
        return open > close;
    }

    private static void AddResultPoints(ProcessingResult result, string side, int value, string raw)
    {
        var fieldId = $"result.{side.ToLowerInvariant()}.points";
        result.Stats.Add(new StatValue
        {
            Scope = StatScope.Result,
            EntityId = result.Game.GameId,
            Side = side,
            StatKey = "points",
            FieldId = fieldId,
            Value = value,
            Score = 0.65,
            Status = StatValidationStatus.NonValidato,
            Candidates =
            [
                new OcrCandidate
                {
                    Engine = OcrStrategyNames.TesseractLayoutCrops,
                    FieldId = fieldId,
                    SourceFieldId = "header.finalScore",
                    StatKey = "points",
                    ZoneId = "header.finalScore",
                    Raw = raw,
                    Normalized = value,
                    Confidence = 0.65,
                    Status = OcrRunStatus.Success
                }
            ]
        });
    }

    private static void MapWorkerDiagnostics(JsonElement root, ProcessingResult result)
    {
        foreach (var warning in Strings(root, "warnings"))
        {
            AddWarning(result, "ocr.tesseractLayoutCrops.workerWarning", warning, "Info");
        }

        foreach (var error in Strings(root, "errors"))
        {
            AddWarning(result, "ocr.tesseractLayoutCrops.workerError", error);
        }
    }

    private static bool TryGetZone(JsonElement root, string zoneId, out JsonElement zone)
    {
        if (root.TryGetProperty("zones", out var zones) && zones.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in zones.EnumerateArray())
            {
                if (string.Equals(String(item, "zoneId"), zoneId, StringComparison.OrdinalIgnoreCase))
                {
                    zone = item;
                    return true;
                }
            }
        }

        zone = default;
        return false;
    }

    private static IEnumerable<string> Strings(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()!).Where(value => !string.IsNullOrWhiteSpace(value))
            : [];
    }

    private static string String(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static void AddWarning(ProcessingResult result, string ruleId, string message, string severity = "Warning")
    {
        result.Validation.Warnings.Add(new ValidationWarning { RuleId = ruleId, Severity = severity, Message = message });
    }

    private static string ShortHash(string documentHash)
    {
        var normalized = documentHash.Replace("sha256:", string.Empty, StringComparison.OrdinalIgnoreCase);
        return normalized.Length <= 12 ? normalized : normalized[..12];
    }

    [GeneratedRegex(@"(?<!\d)(?<home>\d{1,3})\s*(?:-|–|—|â€”)\s*(?<away>\d{1,3})(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex ScorePattern();
}
