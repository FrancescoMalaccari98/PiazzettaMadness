using System.Text.Json;
using System.Text.Json.Serialization;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Ocr.TesseractPython;

public sealed class TesseractPythonOptions
{
    public bool Enabled { get; set; }
    public string PythonExecutablePath { get; set; } = string.Empty;
    public string PythonProjectPath { get; set; } = "fiba_pdf_to_json_programma_v2";
    public string WorkerModule { get; set; } = "fiba_pdf_to_json.worker";
    public int TimeoutSeconds { get; set; } = 180;
    public int FallbackScale { get; set; } = 4;
    public bool UseNativeImages { get; set; } = true;
    public bool Verbose { get; set; }
    public string Mode { get; set; } = "FullPage";
    public bool UseLayoutCrops { get; set; }
    public bool PreprocessImages { get; set; }
    public bool SavePreprocessedImages { get; set; }
    public int PreprocessScale { get; set; } = 2;
    public int PreprocessThreshold { get; set; } = 170;
    public bool PreprocessSharpen { get; set; } = true;
    public string CropWorkerModule { get; set; } = "fiba_pdf_to_json.crop_worker";
    public string CropRawOutputFolder { get; set; } = "runtime/Dataset/OcrRaw/TesseractLayoutCrops";
    public string PreprocessedCropOutputFolder { get; set; } = "runtime/Dataset/Preprocessed/TesseractCrops";
    public string RawOutputFolder { get; set; } = "Dataset/OcrRaw/TesseractFullPage";
    public string DebugOutputFolder { get; set; } = "Dataset/TesseractFullPageDebug";
    public string LayoutDebugOutputFolder { get; set; } = "runtime/Dataset/LayoutDebug";

    [JsonIgnore]
    public string RuntimeRoot { get; set; } = ".";

    public static TesseractPythonOptions FromJsonElement(JsonElement element, string runtimeRoot)
    {
        var options = element.ValueKind is JsonValueKind.Object
            ? element.Deserialize<TesseractPythonOptions>(JsonDefaults.Options) ?? new TesseractPythonOptions()
            : new TesseractPythonOptions();
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
