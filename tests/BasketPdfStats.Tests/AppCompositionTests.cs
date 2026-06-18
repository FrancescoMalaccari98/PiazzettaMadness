using BasketPdfStats.App;
using BasketPdfStats.Infrastructure.Configuration;

namespace BasketPdfStats.Tests;

public sealed class AppCompositionTests
{
    [Fact]
    public void Build_creates_pipeline_and_detector_without_ui()
    {
        var settings = new AppSettings();
        settings.Ocr.EnabledEngines = ["MockOcr"];

        var composition = AppComposition.Build(settings, AppContext.BaseDirectory);

        Assert.NotNull(composition.Pipeline);
        Assert.NotNull(composition.AlreadyProcessedPdfDetector);
    }

    [Fact]
    public void Build_returns_null_import_service_when_base_url_missing()
    {
        var settings = new AppSettings();
        settings.Ocr.EnabledEngines = ["MockOcr"];
        settings.OcrApi.BaseUrl = string.Empty;

        var composition = AppComposition.Build(settings, AppContext.BaseDirectory);

        Assert.Null(composition.ImportService);
    }

    [Fact]
    public void Build_creates_import_service_when_base_url_present()
    {
        var settings = new AppSettings();
        settings.Ocr.EnabledEngines = ["MockOcr"];
        settings.OcrApi.BaseUrl = "https://example.test/api-ocr";

        var composition = AppComposition.Build(settings, AppContext.BaseDirectory);

        Assert.NotNull(composition.ImportService);
    }

    [Fact]
    public void FindRuntimeRoot_returns_a_directory()
    {
        var root = AppComposition.FindRuntimeRoot();

        Assert.False(string.IsNullOrWhiteSpace(root));
    }
}
