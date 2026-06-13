namespace BasketPdfStats.Core.Models;

public sealed class GameStats
{
    public string GameId { get; set; } = string.Empty;
    public string? Competition { get; set; }
    public string? Venue { get; set; }
    public string? Date { get; set; }
    public string? Time { get; set; }
    public string? Number { get; set; }
    public int? Spectators { get; set; }
    public string? Duration { get; set; }
    public string? ReportCreated { get; set; }
    public List<string> Referees { get; set; } = [];
    public string? HomeTeamId { get; set; }
    public string? AwayTeamId { get; set; }
    public string? FinalScore { get; set; }
    public List<PeriodScore> Periods { get; set; } = [];
}

public sealed class PeriodScore
{
    public string Period { get; set; } = string.Empty;
    public int? Home { get; set; }
    public int? Away { get; set; }
}
