using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Ocr.TesseractPython;

namespace BasketPdfStats.Tests;

public sealed class TesseractPythonOcrEngineTests
{
    [Fact]
    public async Task Returns_not_configured_when_python_project_is_missing()
    {
        var engine = new TesseractPythonOcrEngine(new TesseractPythonOptions
        {
            Enabled = true,
            RuntimeRoot = Path.GetTempPath(),
            PythonProjectPath = Path.Combine("missing", Guid.NewGuid().ToString("N"))
        });

        var result = await engine.ProcessAsync(new OcrProcessingRequest
        {
            PdfPath = "missing.pdf",
            WorkingPath = "missing.pdf",
            OriginalFileName = "missing.pdf",
            DocumentHash = "sha256:abc"
        });

        Assert.Contains(result.OcrRuns, x => x.Engine == OcrStrategyNames.TesseractFullPage && x.Status == OcrRunStatus.NotConfigured);
    }

    [Fact]
    public void Explicit_full_page_wrapper_uses_full_page_strategy_name()
    {
        var engine = new TesseractFullPageOcrEngine(new TesseractPythonOptions());

        Assert.Equal(OcrStrategyNames.TesseractFullPage, engine.EngineName);
    }

    [Fact]
    public void Full_page_raw_artifact_path_uses_strategy_specific_filename()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        var options = new TesseractPythonOptions
        {
            RuntimeRoot = root,
            RawOutputFolder = Path.Combine("Dataset", "OcrRaw", OcrStrategyNames.TesseractFullPage)
        };

        var path = TesseractFullPageArtifactPaths.RawOutputPath(options, new OcrProcessingRequest
        {
            OriginalFileName = "sample game.pdf",
            DocumentHash = "sha256:abcdef1234567890"
        });

        Assert.Equal(
            Path.Combine(root, "Dataset", "OcrRaw", OcrStrategyNames.TesseractFullPage, "sample game.abcdef123456.tesseract-full-page.raw.json"),
            path);
    }

    [Fact]
    public void Full_page_worker_arguments_export_layout_geometry_for_calibration()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        var options = new TesseractPythonOptions
        {
            RuntimeRoot = root,
            LayoutDebugOutputFolder = Path.Combine("Dataset", "LayoutDebug")
        };
        var request = new OcrProcessingRequest
        {
            OriginalFileName = "sample game.pdf",
            DocumentHash = "sha256:abcdef1234567890"
        };

        var args = new TesseractPythonOcrEngine(options).BuildArguments("sample.pdf", "raw.json", request);

        Assert.Contains("--layout-debug-dir", args, StringComparison.Ordinal);
        Assert.Contains(Path.Combine("Dataset", "LayoutDebug", "sample game.abcdef123456"), args, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--document-hash \"sha256:abcdef1234567890\"", args, StringComparison.Ordinal);
    }
}
