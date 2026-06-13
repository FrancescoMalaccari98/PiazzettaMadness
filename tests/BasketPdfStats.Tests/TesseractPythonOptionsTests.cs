using System.Text.Json;
using BasketPdfStats.Ocr.TesseractPython;

namespace BasketPdfStats.Tests;

public sealed class TesseractPythonOptionsTests
{
    [Fact]
    public void Deserializes_tesseract_options_from_config_shape()
    {
        using var document = JsonDocument.Parse("""
        {
          "enabled": true,
          "pythonExecutablePath": "",
          "pythonProjectPath": "fiba_pdf_to_json_programma_v2",
          "workerModule": "fiba_pdf_to_json.worker",
          "timeoutSeconds": 180,
          "fallbackScale": 4,
          "useNativeImages": true,
          "verbose": false,
          "rawOutputFolder": "Dataset/OcrRaw/TesseractPython",
          "debugOutputFolder": "Dataset/TesseractDebug"
        }
        """);

        var options = TesseractPythonOptions.FromJsonElement(document.RootElement, ".");

        Assert.True(options.Enabled);
        Assert.Equal("fiba_pdf_to_json_programma_v2", options.PythonProjectPath);
        Assert.Equal("fiba_pdf_to_json.worker", options.WorkerModule);
        Assert.Equal(180, options.TimeoutSeconds);
        Assert.True(options.UseNativeImages);
    }

    [Fact]
    public void Default_options_are_full_page_and_do_not_enable_crop_prepass()
    {
        var options = new TesseractPythonOptions();

        Assert.Equal("FullPage", options.Mode);
        Assert.False(options.UseLayoutCrops);
        Assert.False(options.PreprocessImages);
        Assert.False(options.SavePreprocessedImages);
    }
}
