PRAGMA foreign_keys = ON;

CREATE TABLE tournaments (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    description TEXT NULL,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE editions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    tournament_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    year INTEGER NOT NULL,
    start_date TEXT NULL,
    end_date TEXT NULL,
    status TEXT NOT NULL DEFAULT 'Draft'
        CHECK (status IN ('Draft', 'Active', 'Completed', 'Cancelled')),
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE
);

CREATE TABLE courts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    edition_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    location TEXT NULL,
    FOREIGN KEY (edition_id) REFERENCES editions(id) ON DELETE CASCADE
);

CREATE TABLE teams (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    edition_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    short_name TEXT NULL,
    primary_color TEXT NULL,
    secondary_color TEXT NULL,
    logo_path TEXT NULL,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (edition_id) REFERENCES editions(id) ON DELETE CASCADE,
    UNIQUE (edition_id, name)
);

CREATE TABLE sponsors (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    description TEXT NULL,
    image_path TEXT NULL,
    is_active INTEGER NOT NULL DEFAULT 1 CHECK (is_active IN (0, 1)),
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE players (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    nickname TEXT NULL,
    fiscal_code TEXT NULL,
    address TEXT NULL,
    phone_number TEXT NULL,
    email TEXT NULL,
    birth_date TEXT NULL,
    photo_path TEXT NULL,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE team_rosters (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    team_id INTEGER NOT NULL,
    player_id INTEGER NOT NULL,
    jersey_number INTEGER NULL,
    role TEXT NULL,
    is_captain INTEGER NOT NULL DEFAULT 0 CHECK (is_captain IN (0, 1)),
    is_active INTEGER NOT NULL DEFAULT 1 CHECK (is_active IN (0, 1)),
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (team_id) REFERENCES teams(id) ON DELETE CASCADE,
    FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE RESTRICT,
    UNIQUE (team_id, player_id),
    UNIQUE (team_id, jersey_number)
);

CREATE TABLE tournament_groups (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    edition_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    code TEXT NOT NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (edition_id) REFERENCES editions(id) ON DELETE CASCADE,
    UNIQUE (edition_id, code)
);

CREATE TABLE group_teams (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    group_id INTEGER NOT NULL,
    team_id INTEGER NOT NULL,
    seed_label TEXT NULL,
    FOREIGN KEY (group_id) REFERENCES tournament_groups(id) ON DELETE CASCADE,
    FOREIGN KEY (team_id) REFERENCES teams(id) ON DELETE CASCADE,
    UNIQUE (group_id, team_id),
    UNIQUE (group_id, seed_label)
);

CREATE TABLE standings (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    group_id INTEGER NOT NULL,
    team_id INTEGER NOT NULL,
    played INTEGER NOT NULL DEFAULT 0,
    wins INTEGER NOT NULL DEFAULT 0,
    losses INTEGER NOT NULL DEFAULT 0,
    points_for INTEGER NOT NULL DEFAULT 0,
    points_against INTEGER NOT NULL DEFAULT 0,
    point_difference INTEGER NOT NULL DEFAULT 0,
    ranking_points INTEGER NOT NULL DEFAULT 0,
    position INTEGER NULL,
    tie_break_note TEXT NULL,
    FOREIGN KEY (group_id) REFERENCES tournament_groups(id) ON DELETE CASCADE,
    FOREIGN KEY (team_id) REFERENCES teams(id) ON DELETE CASCADE,
    UNIQUE (group_id, team_id)
);

CREATE TABLE matches (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    edition_id INTEGER NOT NULL,
    group_id INTEGER NULL,
    court_id INTEGER NULL,
    name TEXT NULL,
    phase TEXT NOT NULL
        CHECK (phase IN ('GroupStage', 'SemiFinal', 'ThirdPlaceFinal', 'Final')),
    round TEXT NULL,
    scheduled_start_at TEXT NULL,
    scheduled_end_at TEXT NULL,
    actual_start_at TEXT NULL,
    actual_end_at TEXT NULL,
    status TEXT NOT NULL DEFAULT 'Scheduled'
        CHECK (status IN ('Scheduled', 'Ready', 'Live', 'Paused', 'Finished', 'Cancelled')),
    period_count INTEGER NOT NULL DEFAULT 2,
    period_duration_ms INTEGER NOT NULL DEFAULT 720000,
    break_duration_ms INTEGER NOT NULL DEFAULT 120000,
    overtime_duration_ms INTEGER NOT NULL DEFAULT 120000,
    shot_clock_ms INTEGER NOT NULL DEFAULT 24000,
    timeout_duration_ms INTEGER NOT NULL DEFAULT 30000,
    timeouts_per_team INTEGER NOT NULL DEFAULT 2,
    timeouts_per_period INTEGER NOT NULL DEFAULT 1,
    personal_foul_limit INTEGER NOT NULL DEFAULT 5,
    team_foul_bonus_threshold INTEGER NOT NULL DEFAULT 5,
    max_score INTEGER NULL,
    max_score_enabled INTEGER NOT NULL DEFAULT 0 CHECK (max_score_enabled IN (0, 1)),
    stop_clock_on_free_throws INTEGER NOT NULL DEFAULT 1 CHECK (stop_clock_on_free_throws IN (0, 1)),
    winner_team_id INTEGER NULL,
    win_reason TEXT NULL
        CHECK (win_reason IS NULL OR win_reason IN ('Regular', 'MaxScoreReached', 'Overtime', 'SuddenDeathFreeThrow', 'Forfeit', 'Disqualification', 'Abandoned')),
    notes TEXT NULL,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (edition_id) REFERENCES editions(id) ON DELETE CASCADE,
    FOREIGN KEY (group_id) REFERENCES tournament_groups(id) ON DELETE SET NULL,
    FOREIGN KEY (court_id) REFERENCES courts(id) ON DELETE SET NULL,
    FOREIGN KEY (winner_team_id) REFERENCES teams(id) ON DELETE SET NULL
);

CREATE TABLE match_teams (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    match_id INTEGER NOT NULL,
    team_id INTEGER NOT NULL,
    side TEXT NOT NULL CHECK (side IN ('Home', 'Away')),
    score INTEGER NOT NULL DEFAULT 0,
    fouls_current_period INTEGER NOT NULL DEFAULT 0,
    timeouts_used_total INTEGER NOT NULL DEFAULT 0,
    timeouts_used_period INTEGER NOT NULL DEFAULT 0,
    is_winner INTEGER NOT NULL DEFAULT 0 CHECK (is_winner IN (0, 1)),
    forfeit_score INTEGER NULL,
    FOREIGN KEY (match_id) REFERENCES matches(id) ON DELETE CASCADE,
    FOREIGN KEY (team_id) REFERENCES teams(id) ON DELETE RESTRICT,
    UNIQUE (match_id, team_id),
    UNIQUE (match_id, side)
);

CREATE TABLE match_players (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    match_id INTEGER NOT NULL,
    team_id INTEGER NOT NULL,
    player_id INTEGER NOT NULL,
    jersey_number INTEGER NULL,
    is_starting_five INTEGER NOT NULL DEFAULT 0 CHECK (is_starting_five IN (0, 1)),
    is_on_court INTEGER NOT NULL DEFAULT 0 CHECK (is_on_court IN (0, 1)),
    points INTEGER NOT NULL DEFAULT 0,
    personal_fouls INTEGER NOT NULL DEFAULT 0,
    is_fouled_out INTEGER NOT NULL DEFAULT 0 CHECK (is_fouled_out IN (0, 1)),
    is_ejected INTEGER NOT NULL DEFAULT 0 CHECK (is_ejected IN (0, 1)),
    FOREIGN KEY (match_id) REFERENCES matches(id) ON DELETE CASCADE,
    FOREIGN KEY (team_id) REFERENCES teams(id) ON DELETE RESTRICT,
    FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE RESTRICT,
    UNIQUE (match_id, player_id),
    UNIQUE (match_id, team_id, jersey_number)
);

CREATE TABLE scoreboard_states (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    match_id INTEGER NOT NULL UNIQUE,
    current_period INTEGER NOT NULL DEFAULT 1,
    current_period_type TEXT NOT NULL DEFAULT 'Regular'
        CHECK (current_period_type IN ('Regular', 'Overtime', 'SuddenDeath')),
    game_clock_ms_remaining INTEGER NOT NULL DEFAULT 720000,
    shot_clock_ms_remaining INTEGER NOT NULL DEFAULT 24000,
    timeout_clock_ms_remaining INTEGER NULL,
    break_clock_ms_remaining INTEGER NULL,
    is_game_clock_running INTEGER NOT NULL DEFAULT 0 CHECK (is_game_clock_running IN (0, 1)),
    is_shot_clock_running INTEGER NOT NULL DEFAULT 0 CHECK (is_shot_clock_running IN (0, 1)),
    is_timeout_running INTEGER NOT NULL DEFAULT 0 CHECK (is_timeout_running IN (0, 1)),
    is_break_running INTEGER NOT NULL DEFAULT 0 CHECK (is_break_running IN (0, 1)),
    possession_team_id INTEGER NULL,
    home_score INTEGER NOT NULL DEFAULT 0,
    away_score INTEGER NOT NULL DEFAULT 0,
    home_fouls_current_period INTEGER NOT NULL DEFAULT 0,
    away_fouls_current_period INTEGER NOT NULL DEFAULT 0,
    home_timeouts_used_total INTEGER NOT NULL DEFAULT 0,
    away_timeouts_used_total INTEGER NOT NULL DEFAULT 0,
    last_event_id INTEGER NULL,
    last_updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (match_id) REFERENCES matches(id) ON DELETE CASCADE,
    FOREIGN KEY (possession_team_id) REFERENCES teams(id) ON DELETE SET NULL
);

CREATE TABLE match_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    match_id INTEGER NOT NULL,
    period INTEGER NOT NULL,
    period_type TEXT NOT NULL CHECK (period_type IN ('Regular', 'Overtime', 'SuddenDeath')),
    game_clock_ms_remaining INTEGER NOT NULL,
    shot_clock_ms_remaining INTEGER NULL,
    team_id INTEGER NULL,
    player_id INTEGER NULL,
    event_type TEXT NOT NULL,
    points INTEGER NULL,
    is_correction INTEGER NOT NULL DEFAULT 0 CHECK (is_correction IN (0, 1)),
    reverts_event_id INTEGER NULL,
    description TEXT NULL,
    payload_json TEXT NULL,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    synced_at TEXT NULL,
    FOREIGN KEY (match_id) REFERENCES matches(id) ON DELETE CASCADE,
    FOREIGN KEY (team_id) REFERENCES teams(id) ON DELETE SET NULL,
    FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE SET NULL,
    FOREIGN KEY (reverts_event_id) REFERENCES match_events(id) ON DELETE SET NULL
);

CREATE TABLE forfeit_results (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    match_id INTEGER NOT NULL UNIQUE,
    winning_team_id INTEGER NULL,
    losing_team_id INTEGER NULL,
    home_assigned_score INTEGER NOT NULL DEFAULT 20,
    away_assigned_score INTEGER NOT NULL DEFAULT 0,
    reason TEXT NOT NULL
        CHECK (reason IN ('NotEnoughPlayers', 'Abandonment', 'Disqualification', 'WeatherDecision', 'OrganizerDecision')),
    notes TEXT NULL,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (match_id) REFERENCES matches(id) ON DELETE CASCADE,
    FOREIGN KEY (winning_team_id) REFERENCES teams(id) ON DELETE SET NULL,
    FOREIGN KEY (losing_team_id) REFERENCES teams(id) ON DELETE SET NULL
);

CREATE TABLE competition_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    edition_id INTEGER NOT NULL,
    event_type TEXT NOT NULL CHECK (event_type IN ('ThreePointContest', 'AwardCeremony', 'Other')),
    name TEXT NOT NULL,
    scheduled_start_at TEXT NULL,
    scheduled_end_at TEXT NULL,
    status TEXT NOT NULL DEFAULT 'Scheduled'
        CHECK (status IN ('Scheduled', 'Live', 'Completed', 'Cancelled')),
    FOREIGN KEY (edition_id) REFERENCES editions(id) ON DELETE CASCADE
);

CREATE TABLE three_point_contest_entries (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    competition_event_id INTEGER NOT NULL,
    team_id INTEGER NOT NULL,
    player_id INTEGER NOT NULL,
    seed_order INTEGER NULL,
    total_score INTEGER NOT NULL DEFAULT 0,
    final_position INTEGER NULL,
    FOREIGN KEY (competition_event_id) REFERENCES competition_events(id) ON DELETE CASCADE,
    FOREIGN KEY (team_id) REFERENCES teams(id) ON DELETE RESTRICT,
    FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE RESTRICT,
    UNIQUE (competition_event_id, team_id),
    UNIQUE (competition_event_id, player_id)
);

CREATE TABLE three_point_contest_rounds (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entry_id INTEGER NOT NULL,
    round_number INTEGER NOT NULL,
    round_type TEXT NOT NULL CHECK (round_type IN ('Qualification', 'Final', 'TieBreak')),
    station1_score INTEGER NOT NULL DEFAULT 0,
    station2_score INTEGER NOT NULL DEFAULT 0,
    station3_score INTEGER NOT NULL DEFAULT 0,
    station4_score INTEGER NOT NULL DEFAULT 0,
    station5_score INTEGER NOT NULL DEFAULT 0,
    total_score INTEGER NOT NULL DEFAULT 0,
    notes TEXT NULL,
    FOREIGN KEY (entry_id) REFERENCES three_point_contest_entries(id) ON DELETE CASCADE,
    UNIQUE (entry_id, round_number, round_type)
);

CREATE TABLE sync_queue (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    edition_id INTEGER NULL,
    entity_type TEXT NOT NULL,
    entity_id TEXT NOT NULL,
    operation TEXT NOT NULL CHECK (operation IN ('Create', 'Update', 'Delete', 'Snapshot')),
    payload_json TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'Pending'
        CHECK (status IN ('Pending', 'Sending', 'Sent', 'Failed')),
    retry_count INTEGER NOT NULL DEFAULT 0,
    last_error TEXT NULL,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sent_at TEXT NULL,
    FOREIGN KEY (edition_id) REFERENCES editions(id) ON DELETE SET NULL
);

CREATE INDEX ix_editions_tournament_id ON editions(tournament_id);
CREATE INDEX ix_teams_edition_id ON teams(edition_id);
CREATE INDEX ix_team_rosters_team_id ON team_rosters(team_id);
CREATE INDEX ix_team_rosters_player_id ON team_rosters(player_id);
CREATE INDEX ix_tournament_groups_edition_id ON tournament_groups(edition_id);
CREATE INDEX ix_group_teams_group_id ON group_teams(group_id);
CREATE INDEX ix_group_teams_team_id ON group_teams(team_id);
CREATE INDEX ix_matches_edition_id ON matches(edition_id);
CREATE INDEX ix_matches_group_id ON matches(group_id);
CREATE INDEX ix_matches_phase_status ON matches(phase, status);
CREATE INDEX ix_match_teams_match_id ON match_teams(match_id);
CREATE INDEX ix_match_players_match_id ON match_players(match_id);
CREATE INDEX ix_match_events_match_id_created_at ON match_events(match_id, created_at);
CREATE INDEX ix_match_events_synced_at ON match_events(synced_at);
CREATE INDEX ix_sync_queue_status_created_at ON sync_queue(status, created_at);
