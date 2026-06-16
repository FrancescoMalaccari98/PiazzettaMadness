namespace PiazzettaMadness.App.Live;

public sealed class ScoreboardState
{
    public string HomeName { get; set; } = "A2";
    public string AwayName { get; set; } = "A4";
    public string HomeColor { get; set; } = "#f77f00";
    public string AwayColor { get; set; } = "#457b9d";
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public int HomeFouls { get; set; }
    public int AwayFouls { get; set; }
    public int HomeTimeouts { get; set; }
    public int AwayTimeouts { get; set; }
    public int Period { get; set; } = 1;
    public int GameClockMs { get; set; } = 720000;
    public int ShotClockMs { get; set; } = 24000;
    public bool IsGameClockRunning { get; set; }
    public bool IsShotClockRunning { get; set; }
    public bool HasActiveMatch { get; set; }
    public List<PlayerStat> HomePlayers { get; set; } = [];
    public List<PlayerStat> AwayPlayers { get; set; } = [];
}

public sealed record PlayerStat(int? JerseyNumber, string Name, int Points, int Fouls);

public sealed class ThreePointContestDisplayState
{
    public bool IsActive { get; set; }
    public string EventName { get; set; } = "3 Point Contest";
    public string PlayerName { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string TeamColor { get; set; } = "#ea6324";
    public int Score { get; set; }
    public int CurrentStation { get; set; } = 1;
    public int[] StationScores { get; set; } = [0, 0, 0, 0, 0];
    public int ClockMs { get; set; } = 60000;
    public string Status { get; set; } = "Ready";
}
