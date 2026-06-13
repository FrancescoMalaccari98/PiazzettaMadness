using System.Text.Json;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Infrastructure.Preparation;

public sealed class LayoutCropOptions
{
    public bool Enabled { get; set; } = true;
    public bool PrepareCropsIfMissing { get; set; } = true;
    public string PythonExecutablePath { get; set; } = ".venv-crop\\Scripts\\python.exe";
    public string WorkingDirectory { get; set; } = "pdf_crop_runner";
    public string WorkerModule { get; set; } = "pdf_crop_runner.calibrate_layout";
    public string LayoutMapPath { get; set; } = "pdf-structure/layout-map.default.json";
    public string CalibrationRulesPath { get; set; } = "pdf-structure/layout-calibration-rules.json";
    public string CropOutputFolder { get; set; } = "runtime/Dataset/LayoutCrops";
    public string ImageOutputFolder { get; set; } = "runtime/Dataset/LayoutImages";
    public string LayoutDebugOutputFolder { get; set; } = "runtime/Dataset/LayoutDebug";
    public bool RequireFullPageGeometry { get; set; } = true;
    public int MaxPages { get; set; } = 1;
    public int TimeoutSeconds { get; set; } = 120;
    public string RuntimeRoot { get; set; } = ".";

    public static LayoutCropOptions FromJsonElement(JsonElement element, string runtimeRoot)
    {
        var options = element.ValueKind == JsonValueKind.Object
            ? element.Deserialize<LayoutCropOptions>(JsonDefaults.Options) ?? new LayoutCropOptions()
            : new LayoutCropOptions();
        options.RuntimeRoot = runtimeRoot;
        return options;
    }

    public string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(RuntimeRoot, path));
    }
}
