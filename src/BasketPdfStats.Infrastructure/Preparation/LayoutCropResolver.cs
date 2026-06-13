using System.Text.Json;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Infrastructure.Preparation;

public class LayoutCropResolver
{
    public virtual DocumentPreparationResult Resolve(OcrProcessingRequest request, LayoutCropOptions options)
    {
        var result = new DocumentPreparationResult
        {
            LayoutCropsRequested = true,
            LayoutCropFolder = LayoutCropPaths.DocumentCropFolder(request, options),
            LayoutImageFolder = LayoutCropPaths.DocumentImageFolder(request, options)
        };
        result.MetadataPath = Path.Combine(result.LayoutCropFolder, "crop-metadata.json");
        if (!File.Exists(result.MetadataPath))
        {
            result.Warnings.Add($"Layout crop metadata not found for current document: {result.MetadataPath}");
            return result;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(result.MetadataPath));
            var root = document.RootElement;
            if (!MetadataMatchesRequest(root, request, result.MetadataPath, result.Warnings))
            {
                return result;
            }

            if (!TryGetCalibration(root, out var calibration))
            {
                result.Warnings.Add($"Ignored uncalibrated layout crop metadata: {result.MetadataPath}");
                return result;
            }

            result.LayoutCalibrated = true;
            result.LayoutCalibrationReportPath = String(calibration, "reportPath");
            if (string.Equals(String(calibration, "status"), "Partial", StringComparison.OrdinalIgnoreCase))
            {
                result.Warnings.Add($"Layout calibration is partial; only usable crops will be loaded: {result.MetadataPath}");
            }

            if (!TryGetCrops(root, out var crops))
            {
                result.Warnings.Add($"Layout crop metadata does not contain crops array: {result.MetadataPath}");
                return result;
            }

            foreach (var crop in crops.EnumerateArray())
            {
                if (!IsUsableCrop(crop))
                {
                    result.Warnings.Add($"Ignored unusable layout crop: {FirstString(crop, "cropId", "zoneId")}");
                    continue;
                }

                var imagePath = ResolveCropPath(String(crop, "imagePath"), result.MetadataPath);
                if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath) || !IsWithin(imagePath, result.LayoutCropFolder))
                {
                    result.Warnings.Add($"Ignored layout crop outside the current document folder or missing: {imagePath}");
                    continue;
                }

                result.LayoutCrops.Add(new LayoutCropZone
                {
                    ZoneId = FirstString(crop, "cropId", "zoneId"),
                    ZoneType = FirstString(crop, "cropType", "zoneType"),
                    FileName = String(crop, "fileName"),
                    ImagePath = imagePath,
                    Tier = String(crop, "tier"),
                    CropType = FirstString(crop, "cropType", "zoneType"),
                    Side = String(crop, "side"),
                    ExpectedContentType = String(crop, "expectedContentType"),
                    PageIndex = Int32(crop, "pageIndex"),
                    FieldIds = StringArray(crop, "fieldIds"),
                    StatKeys = StringArray(crop, "statKeys"),
                    SemanticRole = String(crop, "semanticRole"),
                    Source = String(crop, "source"),
                    ParentZoneId = String(crop, "parentZoneId"),
                    Prompt = String(crop, "prompt")
                });
            }

            result.LayoutCropsAvailable = result.LayoutCrops.Count > 0;
            return result;
        }
        catch (JsonException ex)
        {
            result.Warnings.Add($"Layout crop metadata is malformed: {ex.Message}");
            return result;
        }
    }

    private static bool MetadataMatchesRequest(JsonElement root, OcrProcessingRequest request, string metadataPath, List<string> warnings)
    {
        var originalFileName = String(root, "originalFileName");
        if (string.IsNullOrWhiteSpace(originalFileName) || !string.Equals(originalFileName, request.OriginalFileName, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"Ignored layout crop metadata for another document: originalFileName mismatch in {metadataPath}");
            return false;
        }

        var metadataHash = String(root, "sourceHash");
        if (string.IsNullOrWhiteSpace(metadataHash)) metadataHash = String(root, "documentHash");
        if (string.IsNullOrWhiteSpace(metadataHash) || !string.Equals(NormalizeHash(metadataHash), NormalizeHash(request.DocumentHash), StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"Ignored layout crop metadata for another document: sourceHash/documentHash mismatch in {metadataPath}");
            return false;
        }

        return true;
    }

    private static string ResolveCropPath(string path, string metadataPath)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        return Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(metadataPath)!, path));
    }

    private static bool IsWithin(string path, string directory)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(directory), Path.GetFullPath(path));
        return relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !Path.IsPathRooted(relative);
    }

    private static string NormalizeHash(string value) => value.Replace("sha256:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
    private static bool TryGetCrops(JsonElement root, out JsonElement crops)
    {
        if (root.TryGetProperty("crops", out crops) && crops.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        return root.TryGetProperty("zones", out crops) && crops.ValueKind == JsonValueKind.Array;
    }
    private static bool TryGetCalibration(JsonElement root, out JsonElement calibration)
    {
        if (root.TryGetProperty("layoutCalibration", out calibration) &&
            calibration.ValueKind == JsonValueKind.Object &&
            (string.Equals(String(calibration, "status"), "Calibrated", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(String(calibration, "status"), "Partial", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        calibration = default;
        return false;
    }
    private static bool IsUsableCrop(JsonElement crop)
    {
        return !crop.TryGetProperty("usable", out var usable) ||
               usable.ValueKind != JsonValueKind.False;
    }
    private static string FirstString(JsonElement element, string primaryPropertyName, string fallbackPropertyName)
    {
        var primary = String(element, primaryPropertyName);
        return string.IsNullOrWhiteSpace(primary) ? String(element, fallbackPropertyName) : primary;
    }
    private static string String(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
    private static int Int32(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var number) ? number : 0;
    private static List<string> StringArray(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray().Select(item => item.GetString()).Where(item => !string.IsNullOrWhiteSpace(item)).Cast<string>().ToList()
            : [];
}
