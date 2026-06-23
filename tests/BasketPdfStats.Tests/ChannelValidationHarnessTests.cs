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

        var warningsBySeverity = result.Validation.Warnings
            .GroupBy(w => string.IsNullOrWhiteSpace(w.Severity) ? "(none)" : w.Severity, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var warningsByCategory = result.Validation.Warnings
            .GroupBy(w => RuleCategory(w.RuleId), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var mathWarnings = BuildMathWarningDetails(result);

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
            providers,
            warningsBySeverity,
            warningsByCategory,
            mathWarnings);
    }

    /// <summary>
    /// Per ogni warning di categoria <c>math.*</c> collega la voce alle statistiche coinvolte
    /// (via FieldIds → Stats[].FieldId) e ne estrae entità, valore finale, valori per provider e
    /// FailedRules. Restituisce il dettaglio diagnostico richiesto dalla Fase 10D.
    /// </summary>
    private static IReadOnlyList<MathWarningDetail> BuildMathWarningDetails(ProcessingResult result)
    {
        // Indice Stats[] per FieldId (un FieldId può mappare più di una stat).
        var statsByFieldId = new Dictionary<string, List<StatValue>>(StringComparer.OrdinalIgnoreCase);
        foreach (var stat in result.Stats)
        {
            if (string.IsNullOrWhiteSpace(stat.FieldId))
            {
                continue;
            }

            if (!statsByFieldId.TryGetValue(stat.FieldId, out var list))
            {
                list = [];
                statsByFieldId[stat.FieldId] = list;
            }

            list.Add(stat);
        }

        var details = new List<MathWarningDetail>();
        foreach (var warning in result.Validation.Warnings)
        {
            if (!string.Equals(RuleCategory(warning.RuleId), "math", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fieldIds = warning.FieldIds is { Count: > 0 }
                ? warning.FieldIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                : (string.IsNullOrWhiteSpace(warning.FieldId) ? [] : new List<string> { warning.FieldId! });

            var involved = new List<StatValue>();
            var seen = new HashSet<StatValue>();
            foreach (var fieldId in fieldIds)
            {
                if (statsByFieldId.TryGetValue(fieldId, out var list))
                {
                    foreach (var stat in list)
                    {
                        if (seen.Add(stat))
                        {
                            involved.Add(stat);
                        }
                    }
                }
            }

            var involvedStats = involved.Select(stat => new MathWarningStat(
                    stat.EntityId,
                    ResolveEntityLabel(result, stat.EntityId, stat.Side),
                    stat.Side,
                    stat.StatKey,
                    stat.FieldId,
                    NormalizeValue(stat.Value) ?? "(null)",
                    string.IsNullOrWhiteSpace(stat.Reconciliation?.SelectedProvider) ? "-" : stat.Reconciliation!.SelectedProvider,
                    stat.Reconciliation?.AgreedProviders ?? [],
                    (stat.Reconciliation?.ProviderValues ?? new Dictionary<string, object?>())
                        .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(kv => kv.Key, kv => NormalizeValue(kv.Value), StringComparer.OrdinalIgnoreCase),
                    stat.FailedRules))
                .ToList();

            // Entità coinvolta (le stat di un warning condividono entityId+side): primo non vuoto.
            var entity = involvedStats.Select(s => s.EntityLabel).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))
                         ?? "(entità sconosciuta)";

            // Collegamento a FailedRules: la regola compare in Validation.FailedRules e/o sulle singole stat.
            var linkedFailedRules = new List<string>();
            if (result.Validation.FailedRules.Contains(warning.RuleId, StringComparer.OrdinalIgnoreCase))
            {
                linkedFailedRules.Add($"validation.failedRules: {warning.RuleId}");
            }

            if (involvedStats.Any(s => s.FailedRules.Contains(warning.RuleId, StringComparer.OrdinalIgnoreCase)))
            {
                linkedFailedRules.Add($"stats[].failedRules: {warning.RuleId}");
            }

            details.Add(new MathWarningDetail(
                warning.RuleId,
                string.IsNullOrWhiteSpace(warning.Severity) ? "(none)" : warning.Severity,
                RuleCategory(warning.RuleId),
                string.Equals(warning.Severity, "Error", StringComparison.OrdinalIgnoreCase),
                entity,
                warning.Message,
                fieldIds,
                linkedFailedRules,
                involvedStats));
        }

        return details;
    }

    /// <summary>Etichetta leggibile per un entityId (giocatore o squadra) usando Players/Teams del risultato.</summary>
    private static string ResolveEntityLabel(ProcessingResult result, string entityId, string? side)
    {
        var sideSuffix = string.IsNullOrWhiteSpace(side) ? string.Empty : $" [{side}]";

        var player = result.Players.FirstOrDefault(p => string.Equals(p.EntityId, entityId, StringComparison.OrdinalIgnoreCase));
        if (player is not null)
        {
            var number = string.IsNullOrWhiteSpace(player.Number) ? string.Empty : $"#{player.Number} ";
            var name = player.FullName ?? $"{player.FirstName} {player.LastName}".Trim();
            return $"Giocatore {number}{(string.IsNullOrWhiteSpace(name) ? entityId : name)}{sideSuffix}";
        }

        var team = result.Teams.FirstOrDefault(t => string.Equals(t.TeamId, entityId, StringComparison.OrdinalIgnoreCase));
        if (team is not null)
        {
            return $"Squadra {team.Name ?? team.TeamId}{sideSuffix}";
        }

        return $"{entityId}{sideSuffix}";
    }

    private static string RuleCategory(string? ruleId)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            return "(none)";
        }

        var dot = ruleId.IndexOf('.');
        return dot > 0 ? ruleId[..dot] : ruleId;
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
        _output.WriteLine($"  [warnings] {report.WarningCount} totali — severità: {Format(report.WarningsBySeverity)}");
        _output.WriteLine($"  [warnings] categorie: {Format(report.WarningsByCategory)}");
        _output.WriteLine($"  [math] warning matematici: {report.MathWarnings.Count}");
        foreach (var m in report.MathWarnings)
        {
            _output.WriteLine($"    - {m.RuleId} ({m.Severity}, {(m.Blocking ? "bloccante" : "diagnostico")}) — {m.Entity}");
            _output.WriteLine($"      {m.Message}");
            foreach (var s in m.InvolvedStats)
            {
                _output.WriteLine($"      · {s.StatKey}=fin:{s.FinalValue} [scelto:{s.SelectedProvider}] provider: {FormatProviderValues(s.ProviderValues)}");
            }
        }
    }

    private static string FormatProviderValues(IReadOnlyDictionary<string, string?> values) =>
        values.Count == 0 ? "-" : string.Join(", ", values.Select(kv => $"{kv.Key}={kv.Value ?? "null"}"));

    private static string Format(IReadOnlyDictionary<string, int> map) =>
        map.Count == 0 ? "-" : string.Join(", ", map.Select(kv => $"{kv.Key}={kv.Value}"));

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
            md.AppendLine($"- Warnings ({r.WarningCount}) per severità: {Format(r.WarningsBySeverity)}");
            md.AppendLine($"- Warnings per categoria: {Format(r.WarningsByCategory)}");
            md.AppendLine();

            AppendMathWarnings(md, $"### Dettaglio warning matematici — {r.PdfName}", r.MathWarnings);
        }

        AppendAggregate(md, reports);
        AppendMathWarningsAggregate(md, reports);

        var mdPath = Path.Combine(dir, "channel-validation-report.md");
        File.WriteAllText(mdPath, md.ToString());
        return mdPath;
    }

    private static void AppendAggregate(StringBuilder md, IReadOnlyList<PdfChannelReport> reports)
    {
        md.AppendLine($"## Aggregato ({reports.Count} PDF)");
        md.AppendLine();

        // Per-provider: copertura/accordi/conflitti totali + accordo aggregato.
        var providerKeys = reports.SelectMany(r => r.Providers.Select(p => p.Provider))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
        md.AppendLine("| Provider | Copertura tot | Accordi tot | Conflitti tot | Accordo % |");
        md.AppendLine("|---|---|---|---|---|");
        foreach (var key in providerKeys)
        {
            var cov = reports.Sum(r => r.Providers.Where(p => p.Provider == key).Sum(p => p.Coverage));
            var agr = reports.Sum(r => r.Providers.Where(p => p.Provider == key).Sum(p => p.Agreed));
            var con = reports.Sum(r => r.Providers.Where(p => p.Provider == key).Sum(p => p.Conflict));
            var pct = cov > 0 ? (100.0 * agr / cov).ToString("F1", CultureInfo.InvariantCulture) : "-";
            md.AppendLine($"| {key} | {cov} | {agr} | {con} | {pct} |");
        }
        md.AppendLine();

        // Tempi medi per canale (run).
        var runKeys = reports.SelectMany(r => r.ChannelRuns.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
        md.AppendLine("| Canale (run) | Tempo medio (ms) | PDF |");
        md.AppendLine("|---|---|---|");
        foreach (var key in runKeys)
        {
            var times = reports.Where(r => r.ChannelRuns.ContainsKey(key)).Select(r => r.ChannelRuns[key].DurationMs).ToArray();
            var avg = times.Length > 0 ? ((double)times.Sum() / times.Length).ToString("F0", CultureInfo.InvariantCulture) : "-";
            md.AppendLine($"| {key} | {avg} | {times.Length} |");
        }
        md.AppendLine();

        // Warnings aggregati.
        var totalWarnings = reports.Sum(r => r.WarningCount);
        var bySeverity = Merge(reports.Select(r => r.WarningsBySeverity));
        var byCategory = Merge(reports.Select(r => r.WarningsByCategory));
        md.AppendLine($"- Warnings totali: {totalWarnings}");
        md.AppendLine($"- Per severità: {Format(bySeverity)}");
        md.AppendLine($"- Per categoria: {Format(byCategory)}");
        md.AppendLine($"- Campi riconciliati totali: {reports.Sum(r => r.ReconciledFieldCount)}");
        md.AppendLine();
        md.AppendLine("> Nota: l'accordo è rispetto al valore selezionato dal reconciler, non a una ground truth. " +
                      "I conflitti e i warning `math.*` (mismatch di formula) sono i segnali di correttezza da ispezionare prima di tarare i pesi.");
    }

    /// <summary>
    /// Scrive il dettaglio completo dei warning matematici (Fase 10D): per ognuno entità, regola,
    /// messaggio, severità, bloccante/diagnostico, FieldIds, collegamento a FailedRules e — per ogni
    /// statistica coinvolta — valore finale scelto e valori disponibili per provider.
    /// </summary>
    private static void AppendMathWarnings(StringBuilder md, string heading, IReadOnlyList<MathWarningDetail> mathWarnings)
    {
        md.AppendLine(heading);
        md.AppendLine();
        if (mathWarnings.Count == 0)
        {
            md.AppendLine("Nessun warning matematico (`math.*`) per questo elemento.");
            md.AppendLine();
            return;
        }

        var index = 0;
        foreach (var w in mathWarnings)
        {
            index++;
            md.AppendLine($"#### {index}. `{w.RuleId}` — {w.Entity}");
            md.AppendLine($"- Regola fallita: `{w.RuleId}`");
            md.AppendLine($"- Categoria: `{w.Category}` — Severità: **{w.Severity}** — {(w.Blocking ? "**bloccante**" : "diagnostico (non bloccante per l'import)")}");
            md.AppendLine($"- Messaggio: {w.Message}");
            md.AppendLine($"- FieldIds coinvolti: {(w.FieldIds.Count == 0 ? "-" : string.Join(", ", w.FieldIds.Select(f => $"`{f}`")))}");
            md.AppendLine($"- Collegamento FailedRules: {(w.LinkedFailedRules.Count == 0 ? "-" : string.Join("; ", w.LinkedFailedRules.Select(f => $"`{f}`")))}");
            md.AppendLine();
            if (w.InvolvedStats.Count == 0)
            {
                md.AppendLine("_Nessuna statistica collegata via FieldId (warning senza FieldIds risolvibili)._");
                md.AppendLine();
                continue;
            }

            md.AppendLine("| Statistica | Valore finale | Provider scelto | Provider concordi | Valori per provider |");
            md.AppendLine("|---|---|---|---|---|");
            foreach (var s in w.InvolvedStats)
            {
                var agreed = s.AgreedProviders.Count == 0 ? "-" : string.Join(", ", s.AgreedProviders);
                md.AppendLine($"| `{s.StatKey}` | {s.FinalValue} | {s.SelectedProvider} | {agreed} | {FormatProviderValues(s.ProviderValues)} |");
            }

            md.AppendLine();
        }
    }

    /// <summary>Sezione aggregata: tutti i warning matematici su tutti i PDF, con la colonna PDF.</summary>
    private static void AppendMathWarningsAggregate(StringBuilder md, IReadOnlyList<PdfChannelReport> reports)
    {
        var totalMath = reports.Sum(r => r.MathWarnings.Count);
        md.AppendLine("## Dettaglio warning matematici (aggregato)");
        md.AppendLine();
        md.AppendLine($"Warning `math.*` totali sui {reports.Count} PDF: **{totalMath}** (su {reports.Sum(r => r.ReconciledFieldCount)} campi riconciliati).");
        md.AppendLine();
        if (totalMath == 0)
        {
            md.AppendLine("Nessun warning matematico rilevato.");
            md.AppendLine();
            return;
        }

        md.AppendLine("| # | PDF | Regola | Entità | Severità | Bloccante | Messaggio |");
        md.AppendLine("|---|---|---|---|---|---|---|");
        var index = 0;
        foreach (var r in reports)
        {
            foreach (var w in r.MathWarnings)
            {
                index++;
                md.AppendLine($"| {index} | {r.PdfName} | `{w.RuleId}` | {w.Entity} | {w.Severity} | {(w.Blocking ? "sì" : "no")} | {w.Message.Replace("|", "\\|")} |");
            }
        }

        md.AppendLine();
    }

    private static IReadOnlyDictionary<string, int> Merge(IEnumerable<IReadOnlyDictionary<string, int>> maps)
    {
        var merged = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var map in maps)
        {
            foreach (var kv in map)
            {
                merged[kv.Key] = (merged.TryGetValue(kv.Key, out var current) ? current : 0) + kv.Value;
            }
        }

        return merged.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
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

    /// <summary>Singola statistica coinvolta in un warning matematico (con i valori per provider).</summary>
    private sealed record MathWarningStat(
        string EntityId,
        string EntityLabel,
        string? Side,
        string StatKey,
        string FieldId,
        string FinalValue,
        string SelectedProvider,
        IReadOnlyList<string> AgreedProviders,
        IReadOnlyDictionary<string, string?> ProviderValues,
        IReadOnlyList<string> FailedRules);

    /// <summary>Dettaglio completo di un warning matematico (Fase 10D).</summary>
    private sealed record MathWarningDetail(
        string RuleId,
        string Severity,
        string Category,
        bool Blocking,
        string Entity,
        string Message,
        IReadOnlyList<string> FieldIds,
        IReadOnlyList<string> LinkedFailedRules,
        IReadOnlyList<MathWarningStat> InvolvedStats);

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
        IReadOnlyList<ProviderMetric> Providers,
        IReadOnlyDictionary<string, int> WarningsBySeverity,
        IReadOnlyDictionary<string, int> WarningsByCategory,
        IReadOnlyList<MathWarningDetail> MathWarnings);
}
