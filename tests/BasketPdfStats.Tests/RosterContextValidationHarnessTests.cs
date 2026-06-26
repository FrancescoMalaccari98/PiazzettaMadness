using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BasketPdfStats.App;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Identity;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Infrastructure.Configuration;
using BasketPdfStats.Infrastructure.Serialization;
using Xunit.Abstractions;

namespace BasketPdfStats.Tests;

/// <summary>
/// Harness di validazione del ROSTER DINAMICO reale (Fase 10E). A differenza di
/// <see cref="ChannelValidationHarnessTests"/> (che gira senza MatchContext), questo harness carica un
/// <c>context.json</c> (fixture DB) e processa i PDF della stessa partita PASSANDO l'OcrMatchContext alla
/// pipeline, per verificare che CH1 usi il roster dinamico (non known_names.py come fonte primaria) e che
/// il matching identità avvenga sul roster della squadra corretta.
///
/// Dati richiesti (NON committati — cartella gitignored samples/private/):
///   samples/private/db-context/&lt;match&gt;/context.json   (copia compilata di context.template.json)
///   samples/private/db-context/&lt;match&gt;/pdfs/*.pdf      (i PDF reali della partita)
///
/// Se mancano context.json + PDF o l'ambiente OCR, l'harness esce con un messaggio (non fallisce).
/// Eseguire in locale:  dotnet test --filter "FullyQualifiedName~RosterContextValidationHarnessTests"
/// </summary>
public sealed class RosterContextValidationHarnessTests
{
    // Marker emessi da roster_context.py su stderr (catturati in OcrRun.Error di CH1).
    // Dopo Fase 9 non esiste più un fallback hardcoded (known_names.py rimosso): senza roster
    // attivo CH1 procede senza correzione nomi/numeri. Questi marker indicano "roster non attivo".
    private const string DynamicActiveMarker = "Roster dinamico attivo";
    private static readonly string[] RosterInactiveMarkers =
    [
        "Roster dinamico non attivo",
        "Impossibile caricare il roster JSON",
        "Roster JSON vuoto o incompleto"
    ];

    private readonly ITestOutputHelper _output;

    public RosterContextValidationHarnessTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Roster_context_validation_for_match_pdfs()
    {
        var repoRoot = FindRepositoryRoot();
        var settingsPath = Path.Combine(repoRoot, "Config", "appsettings.json");
        if (!File.Exists(settingsPath))
        {
            _output.WriteLine($"SKIP: Config/appsettings.json non trovato ({settingsPath}). Necessario per i percorsi dei venv.");
            return;
        }

        var matchFolders = FindMatchFolders(repoRoot);
        if (matchFolders.Count == 0)
        {
            _output.WriteLine(
                "SKIP: nessuna fixture trovata. Attesa: samples/private/db-context/<match>/context.json + pdfs/*.pdf. " +
                "Compila samples/private/db-context/aurora-lynx-nebula-bears-08-lug-2030/context.template.json con i dati DB reali, " +
                "rinominalo in context.json e metti i 5 PDF in pdfs/.");
            return;
        }

        if (!CommandSucceeds("tesseract", "--version"))
        {
            _output.WriteLine("SKIP: tesseract non disponibile nel PATH (CH1 obbligatorio).");
            return;
        }

        var settings = AppSettingsLoader.Load(settingsPath);
        var composition = AppComposition.Build(settings, repoRoot);

        var matchReports = new List<MatchRosterReport>();
        foreach (var folder in matchFolders)
        {
            var contextPath = Path.Combine(folder, "context.json");
            RosterContextFixture? fixture;
            try
            {
                fixture = LoadFixture(contextPath);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"SKIP {folder}: context.json non leggibile: {ex.Message}");
                continue;
            }

            if (fixture is null)
            {
                _output.WriteLine($"SKIP {folder}: context.json vuoto.");
                continue;
            }

            var matchContext = fixture.ToMatchContext();
            var pdfs = Directory.EnumerateFiles(Path.Combine(folder, "pdfs"), "*.pdf", SearchOption.TopDirectoryOnly)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            if (pdfs.Length == 0)
            {
                _output.WriteLine($"SKIP {folder}: nessun PDF in pdfs/.");
                continue;
            }

            _output.WriteLine($"=== Partita: {fixture.HomeTeam.Name} vs {fixture.AwayTeam.Name} (matchId={fixture.MatchId}, {fixture.MatchDate} {fixture.Time}, {fixture.Group}) ===");
            _output.WriteLine($"    Roster DB: Home {matchContext.HomeTeam.Players.Count} giocatori, Away {matchContext.AwayTeam.Players.Count} giocatori. PDF: {pdfs.Length}.");

            var pdfReports = new List<PdfRosterReport>();
            foreach (var pdf in pdfs)
            {
                _output.WriteLine($"--- Elaboro: {Path.GetFileName(pdf)} ---");
                var result = await composition.Pipeline.ProcessPdfAsync(
                    pdf,
                    new OcrRunSelection { UseTesseract = true, UsePaddle = true },
                    matchContext);

                var report = BuildPdfReport(Path.GetFileName(pdf), result, matchContext);
                pdfReports.Add(report);
                WriteConsole(report);
            }

            matchReports.Add(new MatchRosterReport(
                Path.GetFileName(folder),
                fixture.MatchId,
                fixture.MatchDate,
                fixture.Time,
                fixture.Group,
                matchContext.HomeTeam.Name,
                matchContext.HomeTeam.TeamId,
                matchContext.HomeTeam.Players.Count,
                matchContext.AwayTeam.Name,
                matchContext.AwayTeam.TeamId,
                matchContext.AwayTeam.Players.Count,
                pdfReports));
        }

        if (matchReports.Count == 0)
        {
            _output.WriteLine("SKIP: fixture presenti ma nessun PDF elaborabile.");
            return;
        }

        var reportPath = WriteReport(repoRoot, matchReports);
        _output.WriteLine($"\nReport scritto in: {reportPath}");
        Assert.NotEmpty(matchReports);
    }

    private static PdfRosterReport BuildPdfReport(string pdfName, ProcessingResult result, OcrMatchContext context)
    {
        // CH1 roster source dallo stderr catturato (OcrRun.Error del run TesseractFullPage).
        var ch1 = result.OcrRuns.FirstOrDefault(r => string.Equals(r.Engine, "TesseractFullPage", StringComparison.OrdinalIgnoreCase));
        var ch1Stderr = ch1?.Error ?? string.Empty;
        var ch1Success = ch1 is { Status: Core.Enums.OcrRunStatus.Success };

        string rosterSource;
        if (ch1 is null || !ch1Success)
        {
            rosterSource = "none";
        }
        else if (ch1Stderr.Contains(DynamicActiveMarker, StringComparison.OrdinalIgnoreCase))
        {
            rosterSource = "dynamic-context";
        }
        else if (RosterInactiveMarkers.Any(m => ch1Stderr.Contains(m, StringComparison.OrdinalIgnoreCase)))
        {
            rosterSource = "roster-not-active";
        }
        else
        {
            // Nessun marker: il worker potrebbe non aver loggato (es. roster mai consultato). Inconcludente.
            rosterSource = "unknown";
        }

        var rosterInactive = RosterInactiveMarkers.Any(m => ch1Stderr.Contains(m, StringComparison.OrdinalIgnoreCase));

        var home = MatchSide(result, "Home", context.HomeTeam);
        var away = MatchSide(result, "Away", context.AwayTeam);

        // Nessun playerId inventato da Python: i giocatori OCR devono avere entityId in formato OCR
        // ("player:...") e mai un playerId canonico DB (int). I playerId DB compaiono solo in IdentityReview
        // (risolti da C#), e devono appartenere al roster del DB.
        var ocrIdsAllOcrShaped = result.Players.All(p => (p.EntityId ?? string.Empty).StartsWith("player:", StringComparison.OrdinalIgnoreCase));
        var dbPlayerIds = context.HomeTeam.Players.Concat(context.AwayTeam.Players).Select(p => p.PlayerId).ToHashSet();
        var reviewIdsAllFromDb = result.IdentityReview
            .Where(r => r.CandidatePlayerId is not null)
            .All(r => dbPlayerIds.Contains(r.CandidatePlayerId!.Value));
        var inventedPlayerId = !ocrIdsAllOcrShaped || !reviewIdsAllFromDb;

        var reviewRequired = result.ProcessedFile.Status == Core.Enums.FileProcessingStatus.CompletedWithReviewRequired
                             || result.IdentityReview.Any(r => r.Reason is IdentityReviewReason.Conflict or IdentityReviewReason.NotInPdf);

        var ocrHome = result.Players.Count(p => string.Equals(p.Side, "Home", StringComparison.OrdinalIgnoreCase));
        var ocrAway = result.Players.Count(p => string.Equals(p.Side, "Away", StringComparison.OrdinalIgnoreCase));

        return new PdfRosterReport(
            pdfName,
            result.ProcessedFile.Status.ToString(),
            ocrHome,
            ocrAway,
            rosterSource,
            rosterInactive,
            reviewRequired,
            inventedPlayerId,
            home,
            away,
            Snippet(ch1Stderr));
    }

    /// <summary>
    /// Esegue PlayerIdentityMatcher sui giocatori OCR di un lato usando SOLO il roster di quella squadra
    /// (mai un roster combinato). Conta gli esiti e verifica che ogni candidato appartenga alla squadra giusta.
    /// </summary>
    private static SideMatchCounts MatchSide(ProcessingResult result, string side, OcrContextTeam team)
    {
        var matcher = new PlayerIdentityMatcher();
        var roster = team.Players;
        var ocrPlayers = result.Players.Where(p => string.Equals(p.Side, side, StringComparison.OrdinalIgnoreCase)).ToArray();

        int certain = 0, probable = 0, conflict = 0, numberNotFound = 0, unmatched = 0, crossTeamLeak = 0;
        var matchedIds = new HashSet<int>();

        foreach (var ocr in ocrPlayers)
        {
            var match = matcher.Match(ocr.Number, ocr.FullName, roster);
            switch (match.Kind)
            {
                case PlayerMatchKind.CertainMatch: certain++; break;
                case PlayerMatchKind.ProbableMatch: probable++; break;
                case PlayerMatchKind.Conflict: conflict++; break;
                case PlayerMatchKind.NumberNotFound: numberNotFound++; break;
                case PlayerMatchKind.Unmatched: unmatched++; break;
            }

            if (match.Player is not null)
            {
                matchedIds.Add(match.Player.PlayerId);
                // Il candidato deve appartenere alla squadra del lato corrente (roster per-squadra, non combinato).
                if (match.Player.TeamId != team.TeamId)
                {
                    crossTeamLeak++;
                }
            }
        }

        var notInPdf = roster.Count(p => !matchedIds.Contains(p.PlayerId));
        return new SideMatchCounts(roster.Count, ocrPlayers.Length, certain, probable, conflict, numberNotFound, unmatched, notInPdf, crossTeamLeak);
    }

    private void WriteConsole(PdfRosterReport r)
    {
        _output.WriteLine($"  status={r.Status}; ocrPlayers Home={r.OcrPlayersHome}/Away={r.OcrPlayersAway}");
        _output.WriteLine($"  CH1 roster source={r.Ch1RosterSource}; roster not active={(r.RosterInactive ? "yes" : "no")}; review required={(r.ReviewRequired ? "yes" : "no")}; invented playerId={(r.InventedPlayerId ? "YES (!)" : "no")}");
        _output.WriteLine($"  [Home] {Describe(r.Home)}");
        _output.WriteLine($"  [Away] {Describe(r.Away)}");
        if (!string.IsNullOrWhiteSpace(r.Ch1StderrSnippet))
        {
            _output.WriteLine($"  CH1 roster log: {r.Ch1StderrSnippet}");
        }
    }

    private static string Describe(SideMatchCounts c) =>
        $"roster={c.RosterCount}, ocr={c.OcrCount} → certain={c.Certain}, probable={c.Probable}, conflict={c.Conflict}, numberNotFound={c.NumberNotFound}, unmatched={c.Unmatched}, notInPdf={c.NotInPdf}, crossTeamLeak={c.CrossTeamLeak}";

    private static string WriteReport(string repoRoot, IReadOnlyList<MatchRosterReport> reports)
    {
        var dir = Path.Combine(repoRoot, "TestResults", "RosterContextValidation");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "roster-context-report.json"), JsonSerializer.Serialize(reports, JsonDefaults.Options));

        var md = new StringBuilder();
        md.AppendLine("# Report validazione roster dinamico (Fase 10E)");
        md.AppendLine();
        md.AppendLine($"Generato: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        md.AppendLine();
        md.AppendLine("Contesto fornito alla pipeline: **sì** (OcrMatchContext). Sorgente contesto: **JSON fixture** (`context.json`).");
        md.AppendLine();

        foreach (var m in reports)
        {
            md.AppendLine($"## {m.HomeTeam} vs {m.AwayTeam}");
            md.AppendLine($"- matchId: {m.MatchId} — data: {m.MatchDate} {m.Time} — girone: {m.Group}");
            md.AppendLine($"- Home: {m.HomeTeam} (teamId={m.HomeTeamId}, roster={m.HomeRoster}) — Away: {m.AwayTeam} (teamId={m.AwayTeamId}, roster={m.AwayRoster})");
            md.AppendLine($"- PDF processati: {m.Pdfs.Count}");
            md.AppendLine();
            md.AppendLine("| PDF | Stato | CH1 roster source | Roster non attivo | Review required | playerId inventato |");
            md.AppendLine("|---|---|---|---|---|---|");
            foreach (var p in m.Pdfs)
            {
                md.AppendLine($"| {p.PdfName} | {p.Status} | {p.Ch1RosterSource} | {(p.RosterInactive ? "yes" : "no")} | {(p.ReviewRequired ? "yes" : "no")} | {(p.InventedPlayerId ? "YES (!)" : "no")} |");
            }
            md.AppendLine();
            md.AppendLine("Matching identità per lato (roster della squadra corretta):");
            md.AppendLine();
            md.AppendLine("| PDF | Lato | Roster | OCR | Certain | Probable | Conflict | NumberNotFound | Unmatched | NotInPdf | CrossTeamLeak |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var p in m.Pdfs)
            {
                AppendSideRow(md, p.PdfName, "Home", p.Home);
                AppendSideRow(md, p.PdfName, "Away", p.Away);
            }
            md.AppendLine();
        }

        AppendVerdict(md, reports);

        var mdPath = Path.Combine(dir, "roster-context-report.md");
        File.WriteAllText(mdPath, md.ToString());
        return mdPath;
    }

    private static void AppendSideRow(StringBuilder md, string pdf, string side, SideMatchCounts c) =>
        md.AppendLine($"| {pdf} | {side} | {c.RosterCount} | {c.OcrCount} | {c.Certain} | {c.Probable} | {c.Conflict} | {c.NumberNotFound} | {c.Unmatched} | {c.NotInPdf} | {c.CrossTeamLeak} |");

    private static void AppendVerdict(StringBuilder md, IReadOnlyList<MatchRosterReport> reports)
    {
        var all = reports.SelectMany(m => m.Pdfs).ToArray();
        var dynamicAll = all.Length > 0 && all.All(p => p.Ch1RosterSource == "dynamic-context");
        var anyInactive = all.Any(p => p.RosterInactive);
        var anyInvented = all.Any(p => p.InventedPlayerId);
        var anyCrossLeak = all.Any(p => p.Home.CrossTeamLeak > 0 || p.Away.CrossTeamLeak > 0);

        md.AppendLine("## Verdetto");
        md.AppendLine();
        md.AppendLine($"- CH1 usa il roster dinamico su tutti i PDF: **{(dynamicAll ? "sì" : "no")}**");
        md.AppendLine($"- Roster non attivo in almeno un PDF (dopo Fase 9 non c'è fallback hardcoded): **{(anyInactive ? "sì" : "no")}**");
        md.AppendLine($"- playerId inventato da Python: **{(anyInvented ? "SÌ (anomalia)" : "no")}**");
        md.AppendLine($"- Cross-team leak nel matching (candidato di squadra sbagliata): **{(anyCrossLeak ? "SÌ (anomalia)" : "no")}** — il matching usa il roster per-squadra.");
        md.AppendLine();
        md.AppendLine("> Nota: la sorgente roster CH1 è dedotta dai log stderr del worker (`Roster dinamico attivo` vs " +
                      "marker di roster non attivo), catturati in `OcrRun.Error`. `dynamic-context` = roster DB attivo; " +
                      "`roster-not-active` = roster assente/invalido (nessun fallback hardcoded dopo Fase 9); " +
                      "`none` = CH1 non riuscito; `unknown` = nessun marker loggato.");
    }

    private static RosterContextFixture? LoadFixture(string contextPath)
    {
        if (!File.Exists(contextPath))
        {
            return null;
        }

        var json = File.ReadAllText(contextPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        return JsonSerializer.Deserialize<RosterContextFixture>(json, options);
    }

    private static List<string> FindMatchFolders(string repoRoot)
    {
        var baseDir = Path.Combine(repoRoot, "samples", "private", "db-context");
        if (!Directory.Exists(baseDir))
        {
            return [];
        }

        return Directory.EnumerateDirectories(baseDir)
            .Where(d => File.Exists(Path.Combine(d, "context.json")) && Directory.Exists(Path.Combine(d, "pdfs")))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Snippet(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var line = text.Split('\n')
            .FirstOrDefault(l => l.Contains("Roster dinamico", StringComparison.OrdinalIgnoreCase)
                                 || l.Contains("Roster JSON", StringComparison.OrdinalIgnoreCase));
        line ??= text;
        line = line.Trim();
        return line.Length > 200 ? line[..200] : line;
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

    // ---- Fixture (context.json) ----

    private sealed record RosterContextFixture
    {
        public int MatchId { get; init; }
        public string? MatchDate { get; init; }
        public string? Time { get; init; }
        public string? Group { get; init; }
        public FixtureTeam HomeTeam { get; init; } = new();
        public FixtureTeam AwayTeam { get; init; } = new();

        public OcrMatchContext ToMatchContext() => new()
        {
            MatchId = MatchId,
            HomeTeam = HomeTeam.ToContextTeam(),
            AwayTeam = AwayTeam.ToContextTeam()
        };
    }

    private sealed record FixtureTeam
    {
        public int TeamId { get; init; }
        public string Name { get; init; } = string.Empty;
        public List<FixturePlayer> Players { get; init; } = [];

        public OcrContextTeam ToContextTeam() => new()
        {
            TeamId = TeamId,
            Name = Name,
            Players = Players.Select(p => new OcrRosterPlayer
            {
                PlayerId = p.PlayerId,
                TeamId = p.TeamId == 0 ? TeamId : p.TeamId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                JerseyNumber = p.JerseyNumber
            }).ToList()
        };
    }

    private sealed record FixturePlayer
    {
        public int PlayerId { get; init; }
        public int TeamId { get; init; }
        public int JerseyNumber { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? DisplayName { get; init; }
    }

    // ---- Report ----

    private sealed record SideMatchCounts(
        int RosterCount,
        int OcrCount,
        int Certain,
        int Probable,
        int Conflict,
        int NumberNotFound,
        int Unmatched,
        int NotInPdf,
        int CrossTeamLeak);

    private sealed record PdfRosterReport(
        string PdfName,
        string Status,
        int OcrPlayersHome,
        int OcrPlayersAway,
        string Ch1RosterSource,
        bool RosterInactive,
        bool ReviewRequired,
        bool InventedPlayerId,
        SideMatchCounts Home,
        SideMatchCounts Away,
        string Ch1StderrSnippet);

    private sealed record MatchRosterReport(
        string Folder,
        int MatchId,
        string? MatchDate,
        string? Time,
        string? Group,
        string HomeTeam,
        int HomeTeamId,
        int HomeRoster,
        string AwayTeam,
        int AwayTeamId,
        int AwayRoster,
        IReadOnlyList<PdfRosterReport> Pdfs);
}
