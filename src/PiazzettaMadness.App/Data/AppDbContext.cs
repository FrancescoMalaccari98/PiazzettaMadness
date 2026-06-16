using Microsoft.EntityFrameworkCore;

namespace PiazzettaMadness.App.Data;

public sealed class AppDbContext : DbContext
{
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Edition> Editions => Set<Edition>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<TournamentGroup> TournamentGroups => Set<TournamentGroup>();
    public DbSet<GroupTeam> GroupTeams => Set<GroupTeam>();
    public DbSet<CompetitionEvent> CompetitionEvents => Set<CompetitionEvent>();
    public DbSet<ThreePointContestEntry> ThreePointContestEntries => Set<ThreePointContestEntry>();
    public DbSet<ThreePointContestRound> ThreePointContestRounds => Set<ThreePointContestRound>();
    public DbSet<ForfeitResult> ForfeitResults => Set<ForfeitResult>();
    public DbSet<Standing> Standings => Set<Standing>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Sponsor> Sponsors => Set<Sponsor>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<TeamRoster> TeamRosters => Set<TeamRoster>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchTeam> MatchTeams => Set<MatchTeam>();
    public DbSet<MatchPlayer> MatchPlayers => Set<MatchPlayer>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();
    public DbSet<ScoreboardStateRecord> ScoreboardStates => Set<ScoreboardStateRecord>();
    public DbSet<SyncQueueItem> SyncQueue => Set<SyncQueueItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={AppPaths.DatabasePath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tournament>().ToTable("tournaments");
        modelBuilder.Entity<Edition>().ToTable("editions");
        modelBuilder.Entity<Court>().ToTable("courts");
        modelBuilder.Entity<TournamentGroup>().ToTable("tournament_groups");
        modelBuilder.Entity<GroupTeam>().ToTable("group_teams");
        modelBuilder.Entity<CompetitionEvent>().ToTable("competition_events");
        modelBuilder.Entity<ThreePointContestEntry>().ToTable("three_point_contest_entries");
        modelBuilder.Entity<ThreePointContestRound>().ToTable("three_point_contest_rounds");
        modelBuilder.Entity<ForfeitResult>().ToTable("forfeit_results");
        modelBuilder.Entity<Standing>().ToTable("standings");
        modelBuilder.Entity<Team>().ToTable("teams");
        modelBuilder.Entity<Sponsor>().ToTable("sponsors");
        modelBuilder.Entity<Player>().ToTable("players");
        modelBuilder.Entity<TeamRoster>().ToTable("team_rosters");
        modelBuilder.Entity<Match>().ToTable("matches");
        modelBuilder.Entity<MatchTeam>().ToTable("match_teams");
        modelBuilder.Entity<MatchPlayer>().ToTable("match_players");
        modelBuilder.Entity<MatchEvent>().ToTable("match_events");
        modelBuilder.Entity<ScoreboardStateRecord>().ToTable("scoreboard_states");
        modelBuilder.Entity<SyncQueueItem>().ToTable("sync_queue");

        ConfigureTournament(modelBuilder);
        ConfigureEdition(modelBuilder);
        ConfigureCourt(modelBuilder);
        ConfigureTournamentGroup(modelBuilder);
        ConfigureGroupTeam(modelBuilder);
        ConfigureCompetitionEvent(modelBuilder);
        ConfigureThreePointContestEntry(modelBuilder);
        ConfigureThreePointContestRound(modelBuilder);
        ConfigureForfeitResult(modelBuilder);
        ConfigureStanding(modelBuilder);
        ConfigureTeam(modelBuilder);
        ConfigureSponsor(modelBuilder);
        ConfigurePlayer(modelBuilder);
        ConfigureTeamRoster(modelBuilder);
        ConfigureMatch(modelBuilder);
        ConfigureMatchTeam(modelBuilder);
        ConfigureMatchPlayer(modelBuilder);
        ConfigureMatchEvent(modelBuilder);
        ConfigureScoreboardState(modelBuilder);
        ConfigureSyncQueue(modelBuilder);
    }

    private static void ConfigureTournament(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }

    private static void ConfigureEdition(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Edition>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TournamentId).HasColumnName("tournament_id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.Year).HasColumnName("year");
            entity.Property(x => x.StartDate).HasColumnName("start_date");
            entity.Property(x => x.EndDate).HasColumnName("end_date");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }

    private static void ConfigureCourt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Court>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EditionId).HasColumnName("edition_id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.Location).HasColumnName("location");
        });
    }

    private static void ConfigureTournamentGroup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TournamentGroup>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EditionId).HasColumnName("edition_id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.Code).HasColumnName("code");
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
        });
    }

    private static void ConfigureGroupTeam(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GroupTeam>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.GroupId).HasColumnName("group_id");
            entity.Property(x => x.TeamId).HasColumnName("team_id");
            entity.Property(x => x.SeedLabel).HasColumnName("seed_label");
        });
    }

    private static void ConfigureCompetitionEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompetitionEvent>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EditionId).HasColumnName("edition_id");
            entity.Property(x => x.EventType).HasColumnName("event_type");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.ScheduledStartAt).HasColumnName("scheduled_start_at");
            entity.Property(x => x.ScheduledEndAt).HasColumnName("scheduled_end_at");
            entity.Property(x => x.Status).HasColumnName("status");
        });
    }

    private static void ConfigureThreePointContestEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ThreePointContestEntry>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CompetitionEventId).HasColumnName("competition_event_id");
            entity.Property(x => x.TeamId).HasColumnName("team_id");
            entity.Property(x => x.PlayerId).HasColumnName("player_id");
            entity.Property(x => x.SeedOrder).HasColumnName("seed_order");
            entity.Property(x => x.TotalScore).HasColumnName("total_score");
            entity.Property(x => x.FinalPosition).HasColumnName("final_position");
        });
    }

    private static void ConfigureThreePointContestRound(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ThreePointContestRound>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EntryId).HasColumnName("entry_id");
            entity.Property(x => x.RoundNumber).HasColumnName("round_number");
            entity.Property(x => x.RoundType).HasColumnName("round_type");
            entity.Property(x => x.Station1Score).HasColumnName("station1_score");
            entity.Property(x => x.Station2Score).HasColumnName("station2_score");
            entity.Property(x => x.Station3Score).HasColumnName("station3_score");
            entity.Property(x => x.Station4Score).HasColumnName("station4_score");
            entity.Property(x => x.Station5Score).HasColumnName("station5_score");
            entity.Property(x => x.TotalScore).HasColumnName("total_score");
            entity.Property(x => x.Notes).HasColumnName("notes");
        });
    }

    private static void ConfigureForfeitResult(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ForfeitResult>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.MatchId).HasColumnName("match_id");
            entity.Property(x => x.WinningTeamId).HasColumnName("winning_team_id");
            entity.Property(x => x.LosingTeamId).HasColumnName("losing_team_id");
            entity.Property(x => x.HomeAssignedScore).HasColumnName("home_assigned_score");
            entity.Property(x => x.AwayAssignedScore).HasColumnName("away_assigned_score");
            entity.Property(x => x.Reason).HasColumnName("reason");
            entity.Property(x => x.Notes).HasColumnName("notes");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }

    private static void ConfigureStanding(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Standing>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.GroupId).HasColumnName("group_id");
            entity.Property(x => x.TeamId).HasColumnName("team_id");
            entity.Property(x => x.Played).HasColumnName("played");
            entity.Property(x => x.Wins).HasColumnName("wins");
            entity.Property(x => x.Losses).HasColumnName("losses");
            entity.Property(x => x.PointsFor).HasColumnName("points_for");
            entity.Property(x => x.PointsAgainst).HasColumnName("points_against");
            entity.Property(x => x.PointDifference).HasColumnName("point_difference");
            entity.Property(x => x.RankingPoints).HasColumnName("ranking_points");
            entity.Property(x => x.Position).HasColumnName("position");
            entity.Property(x => x.TieBreakNote).HasColumnName("tie_break_note");
        });
    }

    private static void ConfigureTeam(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EditionId).HasColumnName("edition_id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.ShortName).HasColumnName("short_name");
            entity.Property(x => x.PrimaryColor).HasColumnName("primary_color");
            entity.Property(x => x.SecondaryColor).HasColumnName("secondary_color");
            entity.Property(x => x.LogoPath).HasColumnName("logo_path");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }

    private static void ConfigureSponsor(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sponsor>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.ImagePath).HasColumnName("image_path");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }

    private static void ConfigurePlayer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.FirstName).HasColumnName("first_name");
            entity.Property(x => x.LastName).HasColumnName("last_name");
            entity.Property(x => x.Nickname).HasColumnName("nickname");
            entity.Property(x => x.FiscalCode).HasColumnName("fiscal_code");
            entity.Property(x => x.Address).HasColumnName("address");
            entity.Property(x => x.PhoneNumber).HasColumnName("phone_number");
            entity.Property(x => x.Email).HasColumnName("email");
            entity.Property(x => x.BirthDate).HasColumnName("birth_date");
            entity.Property(x => x.PhotoPath).HasColumnName("photo_path");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }

    private static void ConfigureTeamRoster(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TeamRoster>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TeamId).HasColumnName("team_id");
            entity.Property(x => x.PlayerId).HasColumnName("player_id");
            entity.Property(x => x.JerseyNumber).HasColumnName("jersey_number");
            entity.Property(x => x.Role).HasColumnName("role");
            entity.Property(x => x.IsCaptain).HasColumnName("is_captain");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }

    private static void ConfigureMatch(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Match>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EditionId).HasColumnName("edition_id");
            entity.Property(x => x.GroupId).HasColumnName("group_id");
            entity.Property(x => x.CourtId).HasColumnName("court_id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.Phase).HasColumnName("phase");
            entity.Property(x => x.Round).HasColumnName("round");
            entity.Property(x => x.ScheduledStartAt).HasColumnName("scheduled_start_at");
            entity.Property(x => x.ScheduledEndAt).HasColumnName("scheduled_end_at");
            entity.Property(x => x.ActualStartAt).HasColumnName("actual_start_at");
            entity.Property(x => x.ActualEndAt).HasColumnName("actual_end_at");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.PeriodDurationMs).HasColumnName("period_duration_ms");
            entity.Property(x => x.ShotClockMs).HasColumnName("shot_clock_ms");
            entity.Property(x => x.MaxScore).HasColumnName("max_score");
            entity.Property(x => x.MaxScoreEnabled).HasColumnName("max_score_enabled");
            entity.Property(x => x.WinnerTeamId).HasColumnName("winner_team_id");
            entity.Property(x => x.WinReason).HasColumnName("win_reason");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }

    private static void ConfigureMatchTeam(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchTeam>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.MatchId).HasColumnName("match_id");
            entity.Property(x => x.TeamId).HasColumnName("team_id");
            entity.Property(x => x.Side).HasColumnName("side");
            entity.Property(x => x.Score).HasColumnName("score");
            entity.Property(x => x.FoulsCurrentPeriod).HasColumnName("fouls_current_period");
            entity.Property(x => x.TimeoutsUsedTotal).HasColumnName("timeouts_used_total");
            entity.Property(x => x.TimeoutsUsedPeriod).HasColumnName("timeouts_used_period");
            entity.Property(x => x.IsWinner).HasColumnName("is_winner");
            entity.Property(x => x.ForfeitScore).HasColumnName("forfeit_score");
        });
    }

    private static void ConfigureMatchPlayer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchPlayer>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.MatchId).HasColumnName("match_id");
            entity.Property(x => x.TeamId).HasColumnName("team_id");
            entity.Property(x => x.PlayerId).HasColumnName("player_id");
            entity.Property(x => x.JerseyNumber).HasColumnName("jersey_number");
            entity.Property(x => x.IsStartingFive).HasColumnName("is_starting_five");
            entity.Property(x => x.IsOnCourt).HasColumnName("is_on_court");
            entity.Property(x => x.Points).HasColumnName("points");
            entity.Property(x => x.PersonalFouls).HasColumnName("personal_fouls");
            entity.Property(x => x.IsFouledOut).HasColumnName("is_fouled_out");
            entity.Property(x => x.IsEjected).HasColumnName("is_ejected");
        });
    }

    private static void ConfigureMatchEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchEvent>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.MatchId).HasColumnName("match_id");
            entity.Property(x => x.Period).HasColumnName("period");
            entity.Property(x => x.PeriodType).HasColumnName("period_type");
            entity.Property(x => x.GameClockMsRemaining).HasColumnName("game_clock_ms_remaining");
            entity.Property(x => x.ShotClockMsRemaining).HasColumnName("shot_clock_ms_remaining");
            entity.Property(x => x.TeamId).HasColumnName("team_id");
            entity.Property(x => x.PlayerId).HasColumnName("player_id");
            entity.Property(x => x.EventType).HasColumnName("event_type");
            entity.Property(x => x.Points).HasColumnName("points");
            entity.Property(x => x.IsCorrection).HasColumnName("is_correction");
            entity.Property(x => x.RevertsEventId).HasColumnName("reverts_event_id");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.PayloadJson).HasColumnName("payload_json");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.SyncedAt).HasColumnName("synced_at");
        });
    }

    private static void ConfigureScoreboardState(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScoreboardStateRecord>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.MatchId).HasColumnName("match_id");
            entity.Property(x => x.CurrentPeriod).HasColumnName("current_period");
            entity.Property(x => x.CurrentPeriodType).HasColumnName("current_period_type");
            entity.Property(x => x.GameClockMsRemaining).HasColumnName("game_clock_ms_remaining");
            entity.Property(x => x.ShotClockMsRemaining).HasColumnName("shot_clock_ms_remaining");
            entity.Property(x => x.IsGameClockRunning).HasColumnName("is_game_clock_running");
            entity.Property(x => x.IsShotClockRunning).HasColumnName("is_shot_clock_running");
            entity.Property(x => x.HomeScore).HasColumnName("home_score");
            entity.Property(x => x.AwayScore).HasColumnName("away_score");
            entity.Property(x => x.HomeFoulsCurrentPeriod).HasColumnName("home_fouls_current_period");
            entity.Property(x => x.AwayFoulsCurrentPeriod).HasColumnName("away_fouls_current_period");
            entity.Property(x => x.HomeTimeoutsUsedTotal).HasColumnName("home_timeouts_used_total");
            entity.Property(x => x.AwayTimeoutsUsedTotal).HasColumnName("away_timeouts_used_total");
            entity.Property(x => x.LastUpdatedAt).HasColumnName("last_updated_at");
        });
    }

    private static void ConfigureSyncQueue(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncQueueItem>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EditionId).HasColumnName("edition_id");
            entity.Property(x => x.EntityType).HasColumnName("entity_type");
            entity.Property(x => x.EntityId).HasColumnName("entity_id");
            entity.Property(x => x.Operation).HasColumnName("operation");
            entity.Property(x => x.PayloadJson).HasColumnName("payload_json");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.RetryCount).HasColumnName("retry_count");
            entity.Property(x => x.LastError).HasColumnName("last_error");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.SentAt).HasColumnName("sent_at");
        });
    }
}
