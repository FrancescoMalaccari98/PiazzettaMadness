using System.Diagnostics;
using System.Text.Json;
using BasketPdfStats.App.ViewModels;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Core.Stats;
using BasketPdfStats.Infrastructure.Configuration;
using BasketPdfStats.Infrastructure.FileSystem;
using BasketPdfStats.Infrastructure.Pipeline;
using BasketPdfStats.Infrastructure.Serialization;
using BasketPdfStats.Ocr.TesseractPython;
using Xunit.Abstractions;

namespace BasketPdfStats.Tests;

public sealed class TesseractPythonSampleRegressionTests
{
    private readonly ITestOutputHelper _output;

    public TesseractPythonSampleRegressionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Processes_authorized_sample_with_tesseract_full_page_and_preserves_runtime_artifacts()
    {
        var repoRoot = FindRepositoryRoot();
        var samplePdf = Path.Combine(repoRoot, "samples", "pdf", "TABELLINO FINALE 1-2 POSTO.pdf");
        Assert.True(File.Exists(samplePdf), $"Authorized sample PDF not found: {samplePdf}");

        var availability = CheckAvailability(repoRoot);
        Assert.True(availability.Available, availability.Reason);

        var runtime = new RuntimeOptions { RuntimeRoot = Path.Combine(repoRoot, "runtime") };
        var options = new TesseractPythonOptions
        {
            Enabled = true,
            RuntimeRoot = repoRoot,
            PythonExecutablePath = availability.PythonPath!,
            PythonProjectPath = Path.Combine(repoRoot, "fiba_pdf_to_json_programma_v2"),
            WorkerModule = "fiba_pdf_to_json.worker",
            TimeoutSeconds = 180,
            FallbackScale = 4,
            UseNativeImages = true,
            RawOutputFolder = Path.Combine(repoRoot, "runtime", "Dataset", "OcrRaw", OcrStrategyNames.TesseractFullPage),
            DebugOutputFolder = string.Empty
        };
        var pipeline = new PdfProcessingPipeline(runtime, [new TesseractFullPageOcrEngine(options)]);

        var result = await pipeline.ProcessPdfAsync(samplePdf, new OcrRunSelection
        {
            UseTesseract = true,
            UsePaddle = false
        });

        var rawOutputPath = TesseractFullPageArtifactPaths.RawOutputPath(options, new OcrProcessingRequest
        {
            OriginalFileName = Path.GetFileName(samplePdf),
            DocumentHash = result.ProcessedFile.DocumentHash
        });
        var normalizedOutputPath = Path.Combine(
            runtime.NormalizedOcrPath,
            OcrStrategyNames.TesseractFullPage,
            $"{Path.GetFileNameWithoutExtension(samplePdf)}.{DocumentHasher.ShortHash(result.ProcessedFile.DocumentHash)}.normalized.json");

        _output.WriteLine($"PDF: {samplePdf}");
        _output.WriteLine($"Raw: {rawOutputPath}");
        _output.WriteLine($"Normalized: {normalizedOutputPath}");
        _output.WriteLine($"Final JSON: {result.ProcessedFile.OutputJsonPath}");
        _output.WriteLine($"Status: {result.ProcessedFile.Status}; teams={result.Teams.Count}; players={result.Players.Count}; playerStats={result.Stats.Count(stat => stat.Scope == StatScope.Player)}; stats={result.Stats.Count}; warnings={result.Validation.Warnings.Count}; failedRules={result.Validation.FailedRules.Count}");

        Assert.True(File.Exists(rawOutputPath), $"Raw diagnostic output not found: {rawOutputPath}");
        Assert.True(File.Exists(normalizedOutputPath), $"Normalized diagnostic output not found: {normalizedOutputPath}");
        Assert.True(File.Exists(result.ProcessedFile.OutputJsonPath), $"Final JSON not found: {result.ProcessedFile.OutputJsonPath}");
        Assert.Contains(result.OcrRuns, run => run.Engine == OcrStrategyNames.TesseractFullPage && run.Status == OcrRunStatus.Success);
        Assert.Equal([OcrStrategyNames.TesseractFullPage], result.Reconciliation!.Providers);
        Assert.Equal(2, result.Teams.Count);
        Assert.NotEmpty(result.Players);
        Assert.NotEmpty(result.Stats);
        Assert.All(result.Stats, stat => Assert.True(StatKeyRegistry.Contains(stat.StatKey), $"Unknown statKey: {stat.StatKey}"));
        Assert.All(result.Stats.Where(stat => stat.Scope == StatScope.Player), stat =>
            Assert.Contains(result.Players, player => string.Equals(player.EntityId, stat.EntityId, StringComparison.OrdinalIgnoreCase)));

        var viewer = new ProcessingResultViewModel(result);
        Assert.Equal(result.Players.Count, viewer.HomePlayers.Count + viewer.AwayPlayers.Count);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Processes_all_sample_pdfs_with_tesseract_python()
    {
        var repoRoot = FindRepositoryRoot();
        var samplesPath = Path.Combine(repoRoot, "samples", "pdf");
        var samplePdfs = Directory.Exists(samplesPath)
            ? Directory.EnumerateFiles(samplesPath, "*.pdf", SearchOption.TopDirectoryOnly).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()
            : [];

        Assert.NotEmpty(samplePdfs);

        var availability = CheckAvailability(repoRoot);
        if (!availability.Available)
        {
            _output.WriteLine($"TesseractPython integration regression skipped: {availability.Reason}");
            return;
        }

        var tempRoot = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", "TesseractPythonRegression", Guid.NewGuid().ToString("N"));
        var reportRows = new List<RegressionReportRow>();
        try
        {
            var runtime = new RuntimeOptions { RuntimeRoot = tempRoot };
            var options = new TesseractPythonOptions
            {
                Enabled = true,
                RuntimeRoot = repoRoot,
                PythonExecutablePath = availability.PythonPath!,
                PythonProjectPath = Path.Combine(repoRoot, "fiba_pdf_to_json_programma_v2"),
                WorkerModule = "fiba_pdf_to_json.worker",
                TimeoutSeconds = 180,
                FallbackScale = 4,
                UseNativeImages = true,
                RawOutputFolder = Path.Combine(tempRoot, "Dataset", "OcrRaw", "TesseractFullPage"),
                DebugOutputFolder = Path.Combine(tempRoot, "Dataset", "TesseractFullPageDebug")
            };
            var pipeline = new PdfProcessingPipeline(runtime, [new TesseractFullPageOcrEngine(options)]);

            foreach (var samplePdf in samplePdfs)
            {
                var inputPdf = Path.Combine(runtime.InputPath, Path.GetFileName(samplePdf));
                Directory.CreateDirectory(runtime.InputPath);
                File.Copy(samplePdf, inputPdf, overwrite: true);

                var result = await pipeline.ProcessPdfAsync(inputPdf);
                var rawWarningCount = ReadRawWarningCount(options.RawOutputFolder, samplePdf);
                var duplicateFieldIds = result.Stats
                    .GroupBy(x => x.FieldId, StringComparer.OrdinalIgnoreCase)
                    .Where(x => x.Count() > 1)
                    .Select(x => x.Key)
                    .ToArray();
                var warningCategories = CountBy(result.Validation.Warnings.Select(x => x.RuleId));
                var logicalFailedRules = GetLogicalFailedRules(result);
                var failedRuleCategories = CountBy(logicalFailedRules.Select(x => x.RuleId));
                var distinctFailedRuleTypeCount = failedRuleCategories.Count;
                var logicalFailedRuleCount = logicalFailedRules.Count;
                var failedStatLinksCount = result.Stats.Sum(x => x.FailedRules.Count);
                var failedRuleFieldIds = result.Stats
                    .Where(x => x.FailedRules.Count > 0)
                    .Select(x => x.FieldId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var row = new RegressionReportRow(
                    Path.GetFileName(samplePdf),
                    string.Join(" vs ", result.Teams.Select(x => x.Name ?? x.TeamId)),
                    result.Game.FinalScore ?? string.Empty,
                    result.Players.Count,
                    result.Stats.Count,
                    result.Validation.Warnings.Count,
                    logicalFailedRuleCount,
                    distinctFailedRuleTypeCount,
                    failedStatLinksCount,
                    duplicateFieldIds.Length,
                    warningCategories,
                    failedRuleCategories,
                    logicalFailedRules,
                    failedRuleFieldIds,
                    result.ProcessedFile.Status.ToString());
                reportRows.Add(row);

                _output.WriteLine($"{row.PdfName}: {row.Teams}; score={row.Score}; players={row.PlayersCount}; stats={row.StatsCount}; warnings={row.WarningCount}; logicalFailedRules={row.LogicalFailedRuleCount}; failedStatLinks={row.FailedStatLinksCount}; duplicateFieldIds={row.DuplicateFieldIdsCount}; status={row.Status}");

                Assert.Contains(result.OcrRuns, x => x.Engine == OcrStrategyNames.TesseractFullPage && x.Status == OcrRunStatus.Success);
                Assert.NotEqual(FileProcessingStatus.Failed, result.ProcessedFile.Status);
                Assert.NotEqual(FileProcessingStatus.SkippedDuplicate, result.ProcessedFile.Status);
                Assert.Equal(2, result.Teams.Count);
                Assert.True(result.Players.Count > 0, $"{row.PdfName}: expected at least one player.");
                Assert.True(result.Stats.Count > 0, $"{row.PdfName}: expected at least one stat.");
                Assert.Empty(duplicateFieldIds);
                Assert.All(result.Stats, stat => Assert.False(string.IsNullOrWhiteSpace(stat.StatKey), $"{row.PdfName}: empty statKey."));
                Assert.All(result.Stats, stat => Assert.False(string.IsNullOrWhiteSpace(stat.FieldId), $"{row.PdfName}: empty fieldId."));
                Assert.All(result.Stats, stat => Assert.True(StatKeyRegistry.Contains(stat.StatKey), $"{row.PdfName}: StatKey not registered: {stat.StatKey}"));
                Assert.True(result.Validation.Warnings.Count >= rawWarningCount, $"{row.PdfName}: raw warnings were not preserved in normalized validation warnings.");
                AssertFailedRulesAreLinked(result, row.PdfName);
                Assert.NotNull(row.WarningCategories);
                Assert.NotNull(row.FailedRuleCategories);
            }

            WriteReport(repoRoot, reportRows);
        }
        finally
        {
            DeleteDirectory(tempRoot);
        }
    }

    private static void AssertFailedRulesAreLinked(ProcessingResult result, string pdfName)
    {
        foreach (var stat in result.Stats.Where(x => x.FailedRules.Count > 0))
        {
            foreach (var failedRule in stat.FailedRules)
            {
                Assert.Contains(result.Validation.Warnings, warning =>
                    warning.RuleId == failedRule &&
                    warning.FieldIds.Contains(stat.FieldId, StringComparer.OrdinalIgnoreCase));
            }
        }

        foreach (var warning in result.Validation.Warnings.Where(x =>
                     x.RuleId.StartsWith("math.", StringComparison.OrdinalIgnoreCase) &&
                     x.FieldIds.Any(fieldId => result.Stats.Any(stat =>
                         stat.FieldId == fieldId &&
                         stat.FailedRules.Contains(x.RuleId, StringComparer.OrdinalIgnoreCase)))))
        {
            Assert.NotEmpty(warning.FieldIds);
            foreach (var fieldId in warning.FieldIds)
            {
                var stat = Assert.Single(result.Stats, x => x.FieldId == fieldId);
                Assert.Contains(warning.RuleId, stat.FailedRules);
            }
        }
    }

    private static int ReadRawWarningCount(string rawOutputFolder, string samplePdf)
    {
        if (!Directory.Exists(rawOutputFolder))
        {
            return 0;
        }

        var stem = Path.GetFileNameWithoutExtension(samplePdf);
        var rawPath = Directory.EnumerateFiles(rawOutputFolder, $"{stem}.*.tesseract-full-page.raw.json", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (rawPath is null)
        {
            return 0;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(rawPath));
        return TryGet(document.RootElement, out var warnings, "affidabilita", "warnings") && warnings.ValueKind == JsonValueKind.Array
            ? warnings.GetArrayLength()
            : 0;
    }

    private static void WriteReport(string repoRoot, IReadOnlyCollection<RegressionReportRow> rows)
    {
        var reportPath = Path.Combine(repoRoot, "TestResults", "TesseractPythonRegression", "sample-pdf-report.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(rows, JsonDefaults.Options));
    }

    private static IReadOnlyDictionary<string, int> CountBy(IEnumerable<string> values)
    {
        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<LogicalFailedRuleRow> GetLogicalFailedRules(ProcessingResult result)
    {
        return result.Validation.Warnings
            .Where(x => x.RuleId.StartsWith("math.", StringComparison.OrdinalIgnoreCase) &&
                        x.FieldIds.Any(fieldId => result.Stats.Any(stat =>
                            stat.FieldId == fieldId &&
                            stat.FailedRules.Contains(x.RuleId, StringComparer.OrdinalIgnoreCase))))
            .Select(warning =>
            {
                var primary = warning.FieldIds
                    .Select(fieldId => result.Stats.FirstOrDefault(stat => stat.FieldId == fieldId))
                    .FirstOrDefault(stat => stat is not null && stat.FailedRules.Contains(warning.RuleId, StringComparer.OrdinalIgnoreCase));
                return new LogicalFailedRuleRow(
                    warning.RuleId,
                    primary?.Scope.ToString() ?? string.Empty,
                    primary?.EntityId ?? string.Empty,
                    primary?.Side,
                    primary?.FieldId ?? warning.FieldId ?? string.Empty,
                    warning.FieldIds);
            })
            .GroupBy(x => $"{x.RuleId}|{x.Scope}|{x.EntityId}|{x.Side}|{x.TargetFieldId}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.RuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.EntityId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.TargetFieldId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Availability CheckAvailability(string repoRoot)
    {
        var pythonPath = Path.Combine(repoRoot, "fiba_pdf_to_json_programma_v2", "fiba_pdf_to_json", ".venv", "Scripts", "python.exe");
        if (!File.Exists(pythonPath))
        {
            return Availability.Unavailable($"Python venv not found: {pythonPath}");
        }

        var workerPath = Path.Combine(repoRoot, "fiba_pdf_to_json_programma_v2", "fiba_pdf_to_json", "fiba_pdf_to_json", "worker.py");
        if (!File.Exists(workerPath))
        {
            return Availability.Unavailable($"Python worker not found: {workerPath}");
        }

        if (!CommandSucceeds("tesseract", "--version"))
        {
            return Availability.Unavailable("tesseract executable not available on PATH.");
        }

        return Availability.Success(pythonPath);
    }

    private static bool CommandSucceeds(string fileName, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            if (process is null)
            {
                return false;
            }

            return process.WaitForExit(10_000) && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "BasketPdfStats.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate BasketPdfStats.sln from test output directory.");
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static bool TryGet(JsonElement element, out JsonElement value, params string[] path)
    {
        value = element;
        foreach (var part in path)
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(part, out value))
            {
                value = default;
                return false;
            }
        }

        return true;
    }

    private sealed record Availability(bool Available, string? PythonPath, string? Reason)
    {
        public static Availability Success(string pythonPath) => new(true, pythonPath, null);
        public static Availability Unavailable(string reason) => new(false, null, reason);
    }

    private sealed record RegressionReportRow(
        string PdfName,
        string Teams,
        string Score,
        int PlayersCount,
        int StatsCount,
        int WarningCount,
        int LogicalFailedRuleCount,
        int DistinctFailedRuleTypeCount,
        int FailedStatLinksCount,
        int DuplicateFieldIdsCount,
        IReadOnlyDictionary<string, int> WarningCategories,
        IReadOnlyDictionary<string, int> FailedRuleCategories,
        IReadOnlyList<LogicalFailedRuleRow> LogicalFailedRules,
        IReadOnlyList<string> FailedRuleFieldIds,
        string Status);

    private sealed record LogicalFailedRuleRow(
        string RuleId,
        string Scope,
        string EntityId,
        string? Side,
        string TargetFieldId,
        IReadOnlyList<string> FieldIds);
}
