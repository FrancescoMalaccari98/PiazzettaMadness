namespace PiazzettaMadness.App.Data;

public sealed class Tournament
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}

public sealed class Edition
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public string Name { get; set; } = "";
    public int Year { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string Status { get; set; } = "Draft";
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}

public sealed class Team
{
    public int Id { get; set; }
    public int EditionId { get; set; }
    public string Name { get; set; } = "";
    public string? ShortName { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? LogoPath { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}

public sealed class Sponsor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}

public sealed class Court
{
    public int Id { get; set; }
    public int EditionId { get; set; }
    public string Name { get; set; } = "";
    public string? Location { get; set; }
}

public sealed class TournamentGroup
{
    public int Id { get; set; }
    public int EditionId { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class GroupTeam
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int TeamId { get; set; }
    public string? SeedLabel { get; set; }
}

public sealed class CompetitionEvent
{
    public int Id { get; set; }
    public int EditionId { get; set; }
    public string EventType { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ScheduledStartAt { get; set; }
    public string? ScheduledEndAt { get; set; }
    public string Status { get; set; } = "Scheduled";
}

public sealed class ThreePointContestEntry
{
    public int Id { get; set; }
    public int CompetitionEventId { get; set; }
    public int TeamId { get; set; }
    public int PlayerId { get; set; }
    public int? SeedOrder { get; set; }
    public int TotalScore { get; set; }
    public int? FinalPosition { get; set; }
}

public sealed class ThreePointContestRound
{
    public int Id { get; set; }
    public int EntryId { get; set; }
    public int RoundNumber { get; set; }
    public string RoundType { get; set; } = "Qualification";
    public int Station1Score { get; set; }
    public int Station2Score { get; set; }
    public int Station3Score { get; set; }
    public int Station4Score { get; set; }
    public int Station5Score { get; set; }
    public int TotalScore { get; set; }
    public string? Notes { get; set; }
}

public sealed class ForfeitResult
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int? WinningTeamId { get; set; }
    public int? LosingTeamId { get; set; }
    public int HomeAssignedScore { get; set; } = 20;
    public int AwayAssignedScore { get; set; }
    public string Reason { get; set; } = "OrganizerDecision";
    public string? Notes { get; set; }
    public string CreatedAt { get; set; } = "";
}

public sealed class Standing
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int TeamId { get; set; }
    public int Played { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int PointsFor { get; set; }
    public int PointsAgainst { get; set; }
    public int PointDifference { get; set; }
    public int RankingPoints { get; set; }
    public int? Position { get; set; }
    public string? TieBreakNote { get; set; }
}

public sealed class Player
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Nickname { get; set; }
    public string? FiscalCode { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? BirthDate { get; set; }
    public string? PhotoPath { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}

public sealed class TeamRoster
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public int PlayerId { get; set; }
    public int? JerseyNumber { get; set; }
    public string? Role { get; set; }
    public bool IsCaptain { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}

public sealed class Match
{
    public int Id { get; set; }
    public int EditionId { get; set; }
    public int? GroupId { get; set; }
    public int? CourtId { get; set; }
    public string? Name { get; set; }
    public string Phase { get; set; } = "GroupStage";
    public string? Round { get; set; }
    public string? ScheduledStartAt { get; set; }
    public string? ScheduledEndAt { get; set; }
    public string? ActualStartAt { get; set; }
    public string? ActualEndAt { get; set; }
    public string Status { get; set; } = "Scheduled";
    public int PeriodDurationMs { get; set; } = 720000;
    public int ShotClockMs { get; set; } = 24000;
    public int? MaxScore { get; set; } = 45;
    public bool MaxScoreEnabled { get; set; }
    public int? WinnerTeamId { get; set; }
    public string? WinReason { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}

public sealed class MatchTeam
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int TeamId { get; set; }
    public string Side { get; set; } = "";
    public int Score { get; set; }
    public int FoulsCurrentPeriod { get; set; }
    public int TimeoutsUsedTotal { get; set; }
    public int TimeoutsUsedPeriod { get; set; }
    public bool IsWinner { get; set; }
    public int? ForfeitScore { get; set; }
}

public sealed class MatchPlayer
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int TeamId { get; set; }
    public int PlayerId { get; set; }
    public int? JerseyNumber { get; set; }
    public bool IsStartingFive { get; set; }
    public bool IsOnCourt { get; set; }
    public int Points { get; set; }
    public int PersonalFouls { get; set; }
    public bool IsFouledOut { get; set; }
    public bool IsEjected { get; set; }
}

public sealed class MatchEvent
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int Period { get; set; } = 1;
    public string PeriodType { get; set; } = "Regular";
    public int GameClockMsRemaining { get; set; }
    public int? ShotClockMsRemaining { get; set; }
    public int? TeamId { get; set; }
    public int? PlayerId { get; set; }
    public string EventType { get; set; } = "";
    public int? Points { get; set; }
    public bool IsCorrection { get; set; }
    public int? RevertsEventId { get; set; }
    public string? Description { get; set; }
    public string? PayloadJson { get; set; }
    public string CreatedAt { get; set; } = "";
    public string? SyncedAt { get; set; }
}

public sealed class ScoreboardStateRecord
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int CurrentPeriod { get; set; } = 1;
    public string CurrentPeriodType { get; set; } = "Regular";
    public int GameClockMsRemaining { get; set; } = 720000;
    public int ShotClockMsRemaining { get; set; } = 24000;
    public bool IsGameClockRunning { get; set; }
    public bool IsShotClockRunning { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public int HomeFoulsCurrentPeriod { get; set; }
    public int AwayFoulsCurrentPeriod { get; set; }
    public int HomeTimeoutsUsedTotal { get; set; }
    public int AwayTimeoutsUsedTotal { get; set; }
    public string LastUpdatedAt { get; set; } = "";
}

public sealed class SyncQueueItem
{
    public int Id { get; set; }
    public int? EditionId { get; set; }
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Operation { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public string CreatedAt { get; set; } = "";
    public string? SentAt { get; set; }
}
