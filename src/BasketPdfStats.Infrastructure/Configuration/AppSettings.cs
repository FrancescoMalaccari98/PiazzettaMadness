namespace BasketPdfStats.Infrastructure.Configuration;

using System.Text.Json;

public sealed class AppSettings
{
    public RuntimeOptions Runtime { get; set; } = new();
    public OcrOptions Ocr { get; set; } = new();
    public OcrApiOptions OcrApi { get; set; } = new();
    public JsonElement LayoutPreparation { get; set; }
    public JsonElement Reconciliation { get; set; }
}

public sealed class OcrApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public sealed class OcrOptions
{
    public List<string> EnabledEngines { get; set; } = ["MockOcr"];
    public bool ContinueOnEngineFailure { get; set; } = true;
    public JsonElement TesseractPython { get; set; }
    public JsonElement PaddleCrop { get; set; }
}
