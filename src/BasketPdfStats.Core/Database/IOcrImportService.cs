using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Core.Database;

public interface IOcrImportService
{
    Task<OcrMatchLookupResult> GetTodayMatchesAsync(CancellationToken cancellationToken = default);
    Task<OcrImportResult> ImportAsync(
        int matchId,
        ProcessingResult result,
        bool allowTeamMismatch = false,
        CancellationToken cancellationToken = default);
}

public sealed class OcrMatchLookupResult
{
    public bool Success { get; set; }
    public string? GameDay { get; set; }
    public string? WindowStart { get; set; }
    public string? WindowEnd { get; set; }
    public List<OcrMatchOption> Matches { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public sealed class OcrMatchOption
{
    public int MatchId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? MatchName { get; set; }
    public string? Phase { get; set; }
    public string? Round { get; set; }
    public string? ScheduledStartAt { get; set; }
    public string? ScheduledEndAt { get; set; }
    public string? Status { get; set; }
    public OcrMatchTeam HomeTeam { get; set; } = new();
    public OcrMatchTeam AwayTeam { get; set; } = new();
}

public sealed class OcrMatchTeam
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
}

public sealed class OcrImportResult
{
    public bool Success { get; set; }
    public int MatchId { get; set; }
    public int ImportedPlayers { get; set; }
    public List<string> Warnings { get; set; } = [];
    public string? ErrorMessage { get; set; }
}
