using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using BasketPdfStats.App;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Infrastructure.Configuration;
using BasketPdfStats.Infrastructure.Serialization;
using Xunit.Abstractions;

namespace BasketPdfStats.Tests;

/// <summary>
/// Harness di validazione end-to-end CH1–CH4 (Fase 10). NON è un test di regressione: esegue la
/// pipeline reale (tutti i canali da appsettings.json) su ogni PDF in samples/pdf/ e produce un
/// report per-canale (copertura, accordi, conflitti, tempi) per tarare reconciliation.engineWeights.
/// Richiede i venv OCR + Tesseract: se assenti, il test esce con un messaggio (non fallisce).
/// Eseguire in locale:  dotnet test --filter "FullyQualifiedName~ChannelValidationHarnessTests"
/// </summary>
public sealed class ChannelValidationHarnessTests
{
    private readonly ITestOutputHelper _output;

    public ChannelValidationHarnessTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Channel_validation_report_for_all_sample_pdfs()
    {
        var repoRoot = FindRepositoryRoot();
        var settingsPath = Path.Combine(repoRoot, "Config", "appsettings.json");
        if (!File.Exists(settingsPath))
        {
            _output.WriteLine($"SKIP: Config/appsettings.json non trovato ({settingsPath}). Necessario per i percorsi dei venv.");
            return;
        }

        var samplesPath = Path.Combine(repoRoot, "samples", "pdf");
        var samplePdfs = Directory.Exists(samplesPath)
            ? Directory.EnumerateFiles(samplesPath, "*.pdf", SearchOption.TopDirectoryOnly)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()
            : [];
        if (samplePdfs.Length == 0)
        {
            _output.WriteLine($"SKIP: nessun PDF in {samplesPath}.");
            return;
        }

        if (!CommandSucceeds("tesseract", "--version"))
        {
            _output.WriteLine("SKIP: tesseract non disponibile nel PATH (CH1 obbligatorio).");
            return;
        }

        var settings = AppSettingsLoader.Load(settingsPath);
        var composition = AppComposition.Build(settings, repoRoot);

        var reports = new List<PdfChannelReport>();
        foreach (var pdf in samplePdfs)
        {
            _output.WriteLine($"--- Elaboro: {Path.GetFileName(pdf)} ---");
            var result = await composition.Pipeline.ProcessPdfAsync(
                pdf,
                new OcrRunSelection { UseTesseract = true, UsePaddle = true });

            var report = BuildReport(Path.GetFileName(pdf), result);
            reports.Add(report);
            WriteConsole(report);
        }

        var reportPath = WriteReport(repoRoot, reports);
        _output.WriteLine($"\nReport scritto in: {reportPath}");

        // Il test non impone soglie: serve a raccogliere metriche per il tuning manuale dei pesi.
        Assert.NotEmpty(reports);
    }

    private static PdfChannelReport BuildReport(string pdfName, ProcessingResult result)
    {
        // Tempi/stato per canale (chiave = nome engine, es. TesseractFullPage).
        var channelRuns = result.OcrRuns
            .GroupBy(r => r.Engine, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new ChannelRun(g.First().Status.ToString(), g.Sum(r => r.DurationMs), g.First().Error),
                StringComparer.OrdinalIgnoreCase);

        // Copertura/accordi/conflitti per provider (chiave = sourceId, es. ocr.tesseract.fullpage).
        var coverage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var agreed = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var conflict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var reconciledFields = 0;

        foreach (var stat in result.Stats)
        {
            var providerValues = stat.Reconciliation?.ProviderValues;
            if (providerValues is null || providerValues.Count == 0)
            {
                continue;
            }

            reconciledFields++;
            var selected = NormalizeValue(stat.Value);
            foreach (var kv in providerValues)
            {
                var value = NormalizeValue(kv.Value);
                if (value is null)
                {
                    continue;
                }

                Increment(coverage, kv.Key);
                if (string.Equals(value, selected, StringComparison.OrdinalIgnoreCase))
                {
                    Increment(agreed, kv.Key);
                }
                else
                {
                    Increment(conflict, kv.Key);
                }
            }
        }

        var providers = coverage.Keys
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(p => new ProviderMetric(
                p,
                coverage.GetValueOrDefault(p),
                agreed.GetValueOrDefault(p),
                conflict.GetValueOrDefault(p)))
            .ToArray();

        return new PdfChannelReport(
            pdfName,
            result.ProcessedFile.Status.ToString(),
            string.Join(" vs ", result.Teams.Select(t => t.Name ?? t.TeamId)),
            result.Game.FinalScore ?? string.Empty,
            result.Players.Count,
            result.Stats.Count,
            reconciledFields,
            result.Validation.Warnings.Count,
            channelRuns,
            providers);
    }

    private void WriteConsole(PdfChannelReport report)
    {
        _output.WriteLine($"  status={report.Status}; teams={report.Teams}; score={report.Score}; players={report.PlayersCount}; stats={report.StatsCount}; reconciledFields={report.ReconciledFieldCount}; warnings={report.WarningCount}");
        foreach (var run in report.ChannelRuns)
        {
            _output.WriteLine($"  [run] {run.Key}: {run.Value.Status} ({run.Value.DurationMs} ms)");
        }
        foreach (var p in report.Providers)
        {
            var agreementPct = p.Coverage > 0 ? (100.0 * p.Agreed / p.Coverage).ToString("F1", CultureInfo.InvariantCulture) : "-";
            _output.WriteLine($"  [provider] {p.Provider}: coverage={p.Coverage}, agreed={p.Agreed}, conflict={p.Conflict} (accordo {agreementPct}%)");
        }
    }

    private static string WriteReport(string repoRoot, IReadOnlyList<PdfChannelReport> reports)
    {
        var dir = Path.Combine(repoRoot, "TestResults", "ChannelValidation");
        Directory.CreateDirectory(dir);
        var jsonPath = Path.Combine(dir, "channel-validation-report.json");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(reports, JsonDefaults.Options));

        var md = new StringBuilder();
        md.AppendLine("# Report validazione canali (CH1–CH4)");
        md.AppendLine();
        md.AppendLine($"Generato: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        md.AppendLine();
        foreach (var r in reports)
        {
            md.AppendLine($"## {r.PdfName}");
            md.AppendLine($"- Stato: {r.Status} — {r.Teams} — {r.Score}");
            md.AppendLine($"- players={r.PlayersCount}, stats={r.StatsCount}, campi riconciliati={r.ReconciledFieldCount}, warnings={r.WarningCount}");
            md.AppendLine();
            md.AppendLine("| Canale (run) | Stato | Tempo (ms) |");
            md.AppendLine("|---|---|---|");
            foreach (var run in r.ChannelRuns)
            {
                md.AppendLine($"| {run.Key} | {run.Value.Status} | {run.Value.DurationMs} |");
            }
            md.AppendLine();
            md.AppendLine("| Provider | Copertura | Accordi | Conflitti | Accordo % |");
            md.AppendLine("|---|---|---|---|---|");
            foreach (var p in r.Providers)
            {
                var pct = p.Coverage > 0 ? (100.0 * p.Agreed / p.Coverage).ToString("F1", CultureInfo.InvariantCulture) : "-";
                md.AppendLine($"| {p.Provider} | {p.Coverage} | {p.Agreed} | {p.Conflict} | {pct} |");
            }
            md.AppendLine();
        }

        var mdPath = Path.Combine(dir, "channel-validation-report.md");
        File.WriteAllText(mdPath, md.ToString());
        return mdPath;
    }

    private static string? NormalizeValue(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case JsonElement json:
                return json.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.Number => json.GetRawText(),
                    JsonValueKind.String => json.GetString(),
                    _ => json.GetRawText()
                };
            case IFormattable formattable:
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            default:
                var text = value.ToString();
                return string.IsNullOrWhiteSpace(text) ? null : text;
        }
    }

    private static void Increment(Dictionary<string, int> map, string key) =>
        map[key] = (map.TryGetValue(key, out var current) ? current : 0) + 1;

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
            return process is not null && process.WaitForExit(10_000) && process.ExitCode == 0;
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

        throw new DirectoryNotFoundException("BasketPdfStats.sln non trovato dalla directory di output dei test.");
    }

    private sealed record ChannelRun(string Status, long DurationMs, string? Error);

    private sealed record ProviderMetric(string Provider, int Coverage, int Agreed, int Conflict);

    private sealed record PdfChannelReport(
        string PdfName,
        string Status,
        string Teams,
        string Score,
        int PlayersCount,
        int StatsCount,
        int ReconciledFieldCount,
        int WarningCount,
        IReadOnlyDictionary<string, ChannelRun> ChannelRuns,
        IReadOnlyList<ProviderMetric> Providers);
}
