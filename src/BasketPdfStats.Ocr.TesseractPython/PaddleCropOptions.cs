using System.Text.Json;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Infrastructure.FileSystem;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Ocr.TesseractPython;

/// <summary>
/// Options for the PaddleOCR crop channel (ocr.paddle.crop). PaddleOCR runs in a
/// dedicated virtual environment (.venv-paddle) on the same calibrated crops.
/// </summary>
public sealed class PaddleCropOptions
{
    public bool Enabled { get; set; }
    public string PythonExecutablePath { get; set; } = ".venv-paddle\\Scripts\\python.exe";
    public string PythonProjectPath { get; set; } = "fiba_pdf_to_json_programma_v2";
    public string WorkerModule { get; set; } = "fiba_pdf_to_json.paddle_crop_worker";
    public string RowWorkerModule { get; set; } = "fiba_pdf_to_json.paddle_row_worker";
    public string Lang { get; set; } = "en";
    public string ModelDir { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 300;
    public string RawOutputFolder { get; set; } = "runtime/Dataset/OcrRaw/PaddleLayoutCrops";
    public string RowRawOutputFolder { get; set; } = "runtime/Dataset/OcrRaw/PaddleTableRows";

    public string RuntimeRoot { get; set; } = ".";

    public static PaddleCropOptions FromJsonElement(JsonElement element, string runtimeRoot)
    {
        var options = element.ValueKind == JsonValueKind.Object
            ? element.Deserialize<PaddleCropOptions>(JsonDefaults.Options) ?? new PaddleCropOptions()
            : new PaddleCropOptions();
        options.RuntimeRoot = runtimeRoot;
        return options;
    }

    public string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(RuntimeRoot, path));
    }

    public string RawOutputPath(OcrProcessingRequest request)
    {
        var folder = ResolvePath(RawOutputFolder);
        var stem = Path.GetFileNameWithoutExtension(request.OriginalFileName);
        return Path.Combine(folder, $"{stem}.{DocumentHasher.ShortHash(request.DocumentHash)}.paddle-layout-crops.raw.json");
    }

    public string EvidenceOutputPath(OcrProcessingRequest request)
    {
        var folder = ResolvePath(RawOutputFolder);
        var stem = Path.GetFileNameWithoutExtension(request.OriginalFileName);
        return Path.Combine(folder, $"{stem}.{DocumentHasher.ShortHash(request.DocumentHash)}.paddle-layout-crops.evidence.json");
    }

    public string RowRawOutputPath(OcrProcessingRequest request)
    {
        var folder = ResolvePath(RowRawOutputFolder);
        var stem = Path.GetFileNameWithoutExtension(request.OriginalFileName);
        return Path.Combine(folder, $"{stem}.{DocumentHasher.ShortHash(request.DocumentHash)}.paddle-table-rows.raw.json");
    }

    public string RowEvidenceOutputPath(OcrProcessingRequest request)
    {
        var folder = ResolvePath(RowRawOutputFolder);
        var stem = Path.GetFileNameWithoutExtension(request.OriginalFileName);
        return Path.Combine(folder, $"{stem}.{DocumentHasher.ShortHash(request.DocumentHash)}.paddle-table-rows.evidence.json");
    }
}
