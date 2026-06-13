using System.Text.Json;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Infrastructure.Preparation;
using BasketPdfStats.Ocr.TesseractPython;

namespace BasketPdfStats.Tests;

public sealed class DocumentPreparationStageTests
{
    [Fact]
    public void Run_plan_for_tesseract_requests_layout_crops_when_configured()
    {
        var root = CreateTempRoot();
        var builder = Builder(root, tesseractCrops: true);
        var request = Request(root, "tesseract.pdf", "sha256:tesseract");

        var plan = builder.Build(request, new OcrRunSelection { UseTesseract = true });

        Assert.True(plan.NeedsLayoutCrops);
        Assert.True(plan.NeedsTesseractPreprocessing);
    }

    [Fact]
    public void Run_plan_for_paddle_requests_layout_crops()
    {
        var root = CreateTempRoot();
        var builder = Builder(root, tesseractCrops: false);
        var request = Request(root, "paddle.pdf", "sha256:paddle");

        var plan = builder.Build(request, new OcrRunSelection { UseTesseract = false, UsePaddle = true });

        Assert.True(plan.NeedsLayoutCrops);
        Assert.Contains(OcrStrategyNames.PaddleLayoutCrops, plan.SelectedOcrNames);
    }

    [Fact]
    public void Default_full_page_tesseract_plan_does_not_request_layout_crops()
    {
        var root = CreateTempRoot();
        var builder = new OcrRunPlanBuilder(Options(root), new OcrRunPlanBuilderOptions
        {
            RuntimeRoot = root,
            TesseractEnabled = true
        });
        var request = Request(root, "tesseract-full-page.pdf", "sha256:tesseract-full-page");

        var plan = builder.Build(request, new OcrRunSelection { UseTesseract = true });

        Assert.Contains(OcrStrategyNames.TesseractFullPage, plan.SelectedOcrNames);
        Assert.False(plan.NeedsLayoutCrops);
        Assert.False(plan.NeedsTesseractPreprocessing);
    }

    [Fact]
    public async Task Preparation_stage_does_nothing_when_plan_does_not_need_crops()
    {
        var root = CreateTempRoot();
        try
        {
            var options = Options(root);
            var preparer = new RecordingLayoutCropPreparer();
            var stage = new DocumentPreparationStage(options, new LayoutCropResolver(), preparer);
            var request = Request(root, "fullpage-only.pdf", "sha256:fullpage");
            // Full-page only (no crops): TesseractEnabled=true but TesseractUseLayoutCrops=false
            var plan = new OcrRunPlanBuilder(options, new OcrRunPlanBuilderOptions
            {
                RuntimeRoot = root,
                TesseractEnabled = true,
                TesseractUseLayoutCrops = false
            }).Build(request, new OcrRunSelection { UseTesseract = true });

            var result = await stage.PrepareAsync(request, plan);

            Assert.False(result.LayoutCropsRequested);
            Assert.Equal(0, preparer.CallCount);
            Assert.False(Directory.Exists(result.LayoutCropFolder));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task Preparation_stage_reuses_valid_crops_without_regenerating()
    {
        var root = CreateTempRoot();
        try
        {
            var options = Options(root);
            var request = Request(root, "partita 2 q2.pdf", "sha256:partita2");
            WriteValidMetadata(request, options);
            var preparer = new RecordingLayoutCropPreparer();
            var stage = new DocumentPreparationStage(options, new LayoutCropResolver(), preparer);
            var plan = Builder(root, tesseractCrops: true)
                .Build(request, new OcrRunSelection { UseTesseract = true, UsePaddle = false });

            var result = await stage.PrepareAsync(request, plan);

            Assert.True(result.LayoutCropsAvailable);
            Assert.Equal(0, preparer.CallCount);
            Assert.All(result.LayoutCrops, zone => Assert.Contains("partita 2 q2", zone.ImagePath, StringComparison.OrdinalIgnoreCase));
            var finalScore = Assert.Single(result.LayoutCrops);
            Assert.Equal("header.finalScore", finalScore.ZoneId);
            Assert.Equal("semantic", finalScore.Tier);
            Assert.Equal("finalScore", finalScore.ExpectedContentType);
            Assert.Equal("102-header.finalScore.png", finalScore.FileName);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task Preparation_stage_ignores_mismatched_metadata_and_regenerates_current_document()
    {
        var root = CreateTempRoot();
        try
        {
            var options = Options(root);
            var request = Request(root, "partita 2 q2.pdf", "sha256:partita2");
            WriteMetadata(request, options, "SEMIFINALE 1.pdf", "sha256:semifinale1");
            var preparer = new RecordingLayoutCropPreparer { WriteMetadata = true };
            var stage = new DocumentPreparationStage(options, new LayoutCropResolver(), preparer);
            var plan = Builder(root, tesseractCrops: true)
                .Build(request, new OcrRunSelection { UseTesseract = true, UsePaddle = false });

            var result = await stage.PrepareAsync(request, plan);

            Assert.Equal(1, preparer.CallCount);
            Assert.True(result.LayoutCropsPrepared);
            Assert.True(result.LayoutCropsAvailable);
            Assert.Contains(result.Warnings, warning => warning.Contains("another document", StringComparison.OrdinalIgnoreCase));
            Assert.All(result.LayoutCrops, zone => Assert.Contains("partita 2 q2", zone.ImagePath, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(result.LayoutCrops, zone => zone.ImagePath.Contains("SEMIFINALE 1", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void Resolver_ignores_uncalibrated_crop_metadata()
    {
        var root = CreateTempRoot();
        try
        {
            var options = Options(root);
            var request = Request(root, "uncalibrated.pdf", "sha256:uncalibrated");
            var folder = LayoutCropPaths.DocumentCropFolder(request, options);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "crop-metadata.json"), JsonSerializer.Serialize(new
            {
                originalFileName = request.OriginalFileName,
                sourceHash = request.DocumentHash,
                crops = Array.Empty<object>()
            }));

            var result = new LayoutCropResolver().Resolve(request, options);

            Assert.False(result.LayoutCalibrated);
            Assert.False(result.LayoutCropsAvailable);
            Assert.Contains(result.Warnings, warning => warning.Contains("uncalibrated", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task Layout_crop_preparer_requires_full_page_geometry_before_starting_python()
    {
        var root = CreateTempRoot();
        try
        {
            var options = Options(root);
            options.RequireFullPageGeometry = true;
            var request = Request(root, "geometry-required.pdf", "sha256:geometry-required");

            var result = await new LayoutCropPreparer().PrepareAsync(request, options);

            Assert.False(result.Success);
            Assert.Contains(result.Errors, error => error.Contains("Full-page OCR geometry not found", StringComparison.Ordinal));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void Tesseract_layout_crops_arguments_apply_optional_preprocessing_to_all_metadata_crops()
    {
        var options = new TesseractPythonOptions
        {
            PreprocessImages = true,
            SavePreprocessedImages = true,
            PreprocessSharpen = true,
            PreprocessScale = 3,
            PreprocessThreshold = 180
        };
        var engine = new TesseractLayoutCropsOcrEngine(options);

        var args = engine.BuildArguments("crop-metadata.json", "raw.json", "preprocessed");

        Assert.Contains("--preprocess", args, StringComparison.Ordinal);
        Assert.Contains("--save-preprocessed-images", args, StringComparison.Ordinal);
        Assert.Contains("--sharpen", args, StringComparison.Ordinal);
        Assert.Contains("--scale 3", args, StringComparison.Ordinal);
        Assert.Contains("--threshold 180", args, StringComparison.Ordinal);
        Assert.DoesNotContain("--zone-id-filter", args, StringComparison.Ordinal);
    }

    [Fact]
    public void Tesseract_crop_preprocessing_can_be_disabled()
    {
        var options = new TesseractPythonOptions
        {
            PreprocessImages = false,
            SavePreprocessedImages = false,
            PreprocessSharpen = false
        };
        var engine = new TesseractLayoutCropsOcrEngine(options);

        var args = engine.BuildArguments("crop-metadata.json", "raw.json", "preprocessed");

        Assert.DoesNotMatch(@"(?:^|\s)--preprocess(?:\s|$)", args);
        Assert.DoesNotContain("--save-preprocessed-images", args, StringComparison.Ordinal);
        Assert.DoesNotContain("--sharpen", args, StringComparison.Ordinal);
    }

    [Fact]
    public void Layout_crop_folders_include_document_name_and_short_hash()
    {
        var root = CreateTempRoot();
        try
        {
            var options = Options(root);
            var request = Request(root, "partita 2 q2.pdf", "sha256:1234567890abcdef");

            var folder = LayoutCropPaths.DocumentCropFolder(request, options);

            Assert.EndsWith(Path.Combine("runtime", "Dataset", "LayoutCrops", "partita 2 q2.1234567890ab"), folder, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void Default_layout_crop_options_use_generic_runner_and_relocated_layout_map()
    {
        var options = new LayoutCropOptions();

        Assert.Equal("pdf_crop_runner", options.WorkingDirectory);
        Assert.Equal("pdf_crop_runner.calibrate_layout", options.WorkerModule);
        Assert.Equal("pdf-structure/layout-map.default.json", options.LayoutMapPath);
        Assert.Equal("pdf-structure/layout-calibration-rules.json", options.CalibrationRulesPath);
    }

    [Fact]
    public void Layout_crop_preparer_builds_generic_crop_only_command()
    {
        var root = CreateTempRoot();
        try
        {
            var options = Options(root);
            var request = Request(root, "partita 2 q2.pdf", "sha256:1234567890abcdef");

            var args = new LayoutCropPreparer().BuildArguments(request, options);

            Assert.Contains("-m pdf_crop_runner.calibrate_layout", args, StringComparison.Ordinal);
            Assert.Contains("pdf-structure", args, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("layout-map.default.json", args, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--calibration-rules", args, StringComparison.Ordinal);
            Assert.Contains("layout-calibration-rules.json", args, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("partita 2 q2.1234567890ab", args, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--word-boxes", args, StringComparison.Ordinal);
            Assert.Contains("--layout-debug-folder", args, StringComparison.Ordinal);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void Resolver_accepts_partial_metadata_but_ignores_explicitly_unusable_crops()
    {
        var root = CreateTempRoot();
        try
        {
            var options = Options(root);
            var request = Request(root, "partial.pdf", "sha256:partial");
            var folder = LayoutCropPaths.DocumentCropFolder(request, options);
            Directory.CreateDirectory(folder);
            var usablePath = Path.Combine(folder, "101-header.full.png");
            var unusablePath = Path.Combine(folder, "102-header.finalScore.png");
            File.WriteAllBytes(usablePath, [1, 2, 3]);
            File.WriteAllBytes(unusablePath, [1, 2, 3]);
            File.WriteAllText(Path.Combine(folder, "crop-metadata.json"), JsonSerializer.Serialize(new
            {
                originalFileName = request.OriginalFileName,
                sourceHash = request.DocumentHash,
                layoutCalibration = new { status = "Partial", reportPath = "report.json" },
                crops = new object[]
                {
                    new { cropId = "header.full", imagePath = usablePath, usable = true },
                    new { cropId = "header.finalScore", imagePath = unusablePath, usable = false }
                }
            }));

            var result = new LayoutCropResolver().Resolve(request, options);

            Assert.True(result.LayoutCalibrated);
            Assert.True(result.LayoutCropsAvailable);
            Assert.Equal("header.full", Assert.Single(result.LayoutCrops).ZoneId);
            Assert.Contains(result.Warnings, warning => warning.Contains("partial", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.Warnings, warning => warning.Contains("unusable", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    private static OcrRunPlanBuilder Builder(string root, bool tesseractCrops)
    {
        return new OcrRunPlanBuilder(Options(root), new OcrRunPlanBuilderOptions
        {
            RuntimeRoot = root,
            TesseractEnabled = true,
            TesseractUseLayoutCrops = tesseractCrops,
            TesseractPreprocessImages = true
        });
    }

    private static LayoutCropOptions Options(string root) => new()
    {
        RuntimeRoot = root,
        CropOutputFolder = "runtime/Dataset/LayoutCrops",
        ImageOutputFolder = "runtime/Dataset/LayoutImages",
        RequireFullPageGeometry = false
    };

    private static OcrProcessingRequest Request(string root, string fileName, string hash)
    {
        var path = Path.Combine(root, fileName);
        Directory.CreateDirectory(root);
        File.WriteAllBytes(path, [1, 2, 3]);
        return new OcrProcessingRequest
        {
            PdfPath = path,
            WorkingPath = path,
            OriginalFileName = fileName,
            DocumentHash = hash
        };
    }

    private static void WriteValidMetadata(OcrProcessingRequest request, LayoutCropOptions options)
    {
        WriteMetadata(request, options, request.OriginalFileName, request.DocumentHash);
    }

    private static void WriteMetadata(OcrProcessingRequest request, LayoutCropOptions options, string originalFileName, string hash)
    {
        var folder = LayoutCropPaths.DocumentCropFolder(request, options);
        Directory.CreateDirectory(folder);
        var cropPath = Path.Combine(folder, "102-header.finalScore.png");
        File.WriteAllBytes(cropPath, [1, 2, 3]);
        File.WriteAllText(Path.Combine(folder, "crop-metadata.json"), JsonSerializer.Serialize(new
        {
            schemaVersion = "1.0",
            outputKind = "LayoutCropPreparation",
            isFinalNormalizedJson = false,
            originalFileName,
            sourcePdf = request.PdfPath,
            sourceHash = hash,
            layoutCalibration = new
            {
                status = "Calibrated",
                mode = "FullPageGeometryCoordinateFrame",
                reportPath = Path.Combine(LayoutCropPaths.DocumentDebugFolder(request, options), "layout-calibration-report.json")
            },
            crops = new[]
            {
                new
                {
                    cropId = "header.finalScore",
                    fileName = "102-header.finalScore.png",
                    zoneId = "header.finalScore",
                    cropType = "header.finalScore",
                    tier = "semantic",
                    expectedContentType = "finalScore",
                    pageIndex = 0,
                    imagePath = cropPath,
                    fieldIds = new[] { "result.home.points", "result.away.points" },
                    statKeys = new[] { "points" }
                }
            }
        }));
    }

    private static string CreateTempRoot() => Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));

    private static void DeleteTempRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class RecordingLayoutCropPreparer : ILayoutCropPreparer
    {
        public int CallCount { get; private set; }
        public bool WriteMetadata { get; set; }

        public Task<LayoutCropPreparationResult> PrepareAsync(OcrProcessingRequest request, LayoutCropOptions options, CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (WriteMetadata)
            {
                WriteValidMetadata(request, options);
            }

            return Task.FromResult(new LayoutCropPreparationResult
            {
                Success = WriteMetadata,
                OutputFolder = LayoutCropPaths.DocumentCropFolder(request, options),
                MetadataPath = Path.Combine(LayoutCropPaths.DocumentCropFolder(request, options), "crop-metadata.json")
            });
        }
    }
}
