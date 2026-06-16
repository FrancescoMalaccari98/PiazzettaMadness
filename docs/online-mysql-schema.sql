-- Piazzetta Madness - schema database online MySQL/MariaDB
-- Uso previsto: incollare in phpMyAdmin dentro il database gia creato.
-- Nota: il sito legge soltanto; l'app desktop sara l'unico writer.

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS tournaments (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    name VARCHAR(160) NOT NULL,
    description TEXT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS editions (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    tournament_id INT UNSIGNED NOT NULL,
    name VARCHAR(160) NOT NULL,
    year INT NOT NULL,
    start_date DATE NULL,
    end_date DATE NULL,
    status ENUM('Draft', 'Active', 'Completed', 'Cancelled') NOT NULL DEFAULT 'Draft',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX ix_editions_tournament_id (tournament_id),
    CONSTRAINT fk_editions_tournament
        FOREIGN KEY (tournament_id) REFERENCES tournaments(id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS courts (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    edition_id INT UNSIGNED NOT NULL,
    name VARCHAR(160) NOT NULL,
    location VARCHAR(255) NULL,
    PRIMARY KEY (id),
    INDEX ix_courts_edition_id (edition_id),
    CONSTRAINT fk_courts_edition
        FOREIGN KEY (edition_id) REFERENCES editions(id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS teams (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    edition_id INT UNSIGNED NOT NULL,
    name VARCHAR(160) NOT NULL,
    short_name VARCHAR(40) NULL,
    primary_color VARCHAR(20) NULL,
    secondary_color VARCHAR(20) NULL,
    logo_path VARCHAR(500) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_teams_edition_name (edition_id, name),
    INDEX ix_teams_edition_id (edition_id),
    CONSTRAINT fk_teams_edition
        FOREIGN KEY (edition_id) REFERENCES editions(id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS sponsors (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    name VARCHAR(160) NOT NULL,
    description TEXT NULL,
    image_path VARCHAR(500) NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    sort_order INT NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX ix_sponsors_active_sort (is_active, sort_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS players (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    first_name VARCHAR(120) NOT NULL,
    last_name VARCHAR(120) NOT NULL,
    nickname VARCHAR(120) NULL,
    fiscal_code VARCHAR(32) NULL,
    address VARCHAR(255) NULL,
    phone_number VARCHAR(40) NULL,
    email VARCHAR(160) NULL,
    birth_date DATE NULL,
    photo_path VARCHAR(500) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX ix_players_name (last_name, first_name),
    INDEX ix_players_email (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS team_rosters (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    team_id INT UNSIGNED NOT NULL,
    player_id INT UNSIGNED NOT NULL,
    jersey_number INT NULL,
    role VARCHAR(80) NULL,
    is_captain TINYINT(1) NOT NULL DEFAULT 0,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_team_rosters_team_player (team_id, player_id),
    UNIQUE KEY ux_team_rosters_team_number (team_id, jersey_number),
    INDEX ix_team_rosters_player_id (player_id),
    CONSTRAINT fk_team_rosters_team
        FOREIGN KEY (team_id) REFERENCES teams(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_team_rosters_player
        FOREIGN KEY (player_id) REFERENCES players(id)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS tournament_groups (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    edition_id INT UNSIGNED NOT NULL,
    name VARCHAR(160) NOT NULL,
    code VARCHAR(20) NOT NULL,
    sort_order INT NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY ux_tournament_groups_edition_code (edition_id, code),
    INDEX ix_tournament_groups_edition_id (edition_id),
    CONSTRAINT fk_tournament_groups_edition
        FOREIGN KEY (edition_id) REFERENCES editions(id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS group_teams (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    group_id INT UNSIGNED NOT NULL,
    team_id INT UNSIGNED NOT NULL,
    seed_label VARCHAR(20) NULL,
    PRIMARY KEY (id),
    UNIQUE KEY ux_group_teams_group_team (group_id, team_id),
    UNIQUE KEY ux_group_teams_group_seed (group_id, seed_label),
    INDEX ix_group_teams_team_id (team_id),
    CONSTRAINT fk_group_teams_group
        FOREIGN KEY (group_id) REFERENCES tournament_groups(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_group_teams_team
        FOREIGN KEY (team_id) REFERENCES teams(id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS standings (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    group_id INT UNSIGNED NOT NULL,
    team_id INT UNSIGNED NOT NULL,
    played INT NOT NULL DEFAULT 0,
    wins INT NOT NULL DEFAULT 0,
    losses INT NOT NULL DEFAULT 0,
    points_for INT NOT NULL DEFAULT 0,
    points_against INT NOT NULL DEFAULT 0,
    point_difference INT NOT NULL DEFAULT 0,
    ranking_points INT NOT NULL DEFAULT 0,
    position INT NULL,
    tie_break_note TEXT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY ux_standings_group_team (group_id, team_id),
    INDEX ix_standings_team_id (team_id),
    CONSTRAINT fk_standings_group
        FOREIGN KEY (group_id) REFERENCES tournament_groups(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_standings_team
        FOREIGN KEY (team_id) REFERENCES teams(id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS matches (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    edition_id INT UNSIGNED NOT NULL,
    group_id INT UNSIGNED NULL,
    court_id INT UNSIGNED NULL,
    name VARCHAR(160) NULL,
    phase ENUM('GroupStage', 'SemiFinal', 'ThirdPlaceFinal', 'Final') NOT NULL,
    round VARCHAR(80) NULL,
    scheduled_start_at DATETIME NULL,
    scheduled_end_at DATETIME NULL,
    actual_start_at DATETIME NULL,
    actual_end_at DATETIME NULL,
    status ENUM('Scheduled', 'Ready', 'Live', 'Paused', 'Finished', 'Cancelled') NOT NULL DEFAULT 'Scheduled',
    period_count INT NOT NULL DEFAULT 2,
    period_duration_ms INT NOT NULL DEFAULT 720000,
    break_duration_ms INT NOT NULL DEFAULT 120000,
    overtime_duration_ms INT NOT NULL DEFAULT 120000,
    shot_clock_ms INT NOT NULL DEFAULT 24000,
    timeout_duration_ms INT NOT NULL DEFAULT 30000,
    timeouts_per_team INT NOT NULL DEFAULT 2,
    timeouts_per_period INT NOT NULL DEFAULT 1,
    personal_foul_limit INT NOT NULL DEFAULT 5,
    team_foul_bonus_threshold INT NOT NULL DEFAULT 5,
    max_score INT NULL,
    max_score_enabled TINYINT(1) NOT NULL DEFAULT 0,
    stop_clock_on_free_throws TINYINT(1) NOT NULL DEFAULT 1,
    winner_team_id INT UNSIGNED NULL,
    win_reason ENUM('Regular', 'MaxScoreReached', 'Overtime', 'SuddenDeathFreeThrow', 'Forfeit', 'Disqualification', 'Abandoned') NULL,
    notes TEXT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX ix_matches_edition_id (edition_id),
    INDEX ix_matches_group_id (group_id),
    INDEX ix_matches_phase_status (phase, status),
    INDEX ix_matches_court_id (court_id),
    INDEX ix_matches_winner_team_id (winner_team_id),
    CONSTRAINT fk_matches_edition
        FOREIGN KEY (edition_id) REFERENCES editions(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_matches_group
        FOREIGN KEY (group_id) REFERENCES tournament_groups(id)
        ON DELETE SET NULL,
    CONSTRAINT fk_matches_court
        FOREIGN KEY (court_id) REFERENCES courts(id)
        ON DELETE SET NULL,
    CONSTRAINT fk_matches_winner_team
        FOREIGN KEY (winner_team_id) REFERENCES teams(id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS match_teams (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    match_id INT UNSIGNED NOT NULL,
    team_id INT UNSIGNED NOT NULL,
    side ENUM('Home', 'Away') NOT NULL,
    score INT NOT NULL DEFAULT 0,
    fouls_current_period INT NOT NULL DEFAULT 0,
    timeouts_used_total INT NOT NULL DEFAULT 0,
    timeouts_used_period INT NOT NULL DEFAULT 0,
    is_winner TINYINT(1) NOT NULL DEFAULT 0,
    forfeit_score INT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY ux_match_teams_match_team (match_id, team_id),
    UNIQUE KEY ux_match_teams_match_side (match_id, side),
    INDEX ix_match_teams_team_id (team_id),
    CONSTRAINT fk_match_teams_match
        FOREIGN KEY (match_id) REFERENCES matches(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_match_teams_team
        FOREIGN KEY (team_id) REFERENCES teams(id)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS match_players (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    match_id INT UNSIGNED NOT NULL,
    team_id INT UNSIGNED NOT NULL,
    player_id INT UNSIGNED NOT NULL,
    jersey_number INT NULL,
    is_starting_five TINYINT(1) NOT NULL DEFAULT 0,
    is_on_court TINYINT(1) NOT NULL DEFAULT 0,
    points INT NOT NULL DEFAULT 0,
    personal_fouls INT NOT NULL DEFAULT 0,
    is_fouled_out TINYINT(1) NOT NULL DEFAULT 0,
    is_ejected TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY ux_match_players_match_player (match_id, player_id),
    UNIQUE KEY ux_match_players_match_team_number (match_id, team_id, jersey_number),
    INDEX ix_match_players_team_id (team_id),
    INDEX ix_match_players_player_id (player_id),
    CONSTRAINT fk_match_players_match
        FOREIGN KEY (match_id) REFERENCES matches(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_match_players_team
        FOREIGN KEY (team_id) REFERENCES teams(id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_match_players_player
        FOREIGN KEY (player_id) REFERENCES players(id)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS scoreboard_states (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    match_id INT UNSIGNED NOT NULL,
    current_period INT NOT NULL DEFAULT 1,
    current_period_type ENUM('Regular', 'Overtime', 'SuddenDeath') NOT NULL DEFAULT 'Regular',
    game_clock_ms_remaining INT NOT NULL DEFAULT 720000,
    shot_clock_ms_remaining INT NOT NULL DEFAULT 24000,
    timeout_clock_ms_remaining INT NULL,
    break_clock_ms_remaining INT NULL,
    is_game_clock_running TINYINT(1) NOT NULL DEFAULT 0,
    is_shot_clock_running TINYINT(1) NOT NULL DEFAULT 0,
    is_timeout_running TINYINT(1) NOT NULL DEFAULT 0,
    is_break_running TINYINT(1) NOT NULL DEFAULT 0,
    possession_team_id INT UNSIGNED NULL,
    home_score INT NOT NULL DEFAULT 0,
    away_score INT NOT NULL DEFAULT 0,
    home_fouls_current_period INT NOT NULL DEFAULT 0,
    away_fouls_current_period INT NOT NULL DEFAULT 0,
    home_timeouts_used_total INT NOT NULL DEFAULT 0,
    away_timeouts_used_total INT NOT NULL DEFAULT 0,
    last_event_id INT UNSIGNED NULL,
    last_updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_scoreboard_states_match (match_id),
    INDEX ix_scoreboard_states_last_updated (last_updated_at),
    INDEX ix_scoreboard_states_possession_team_id (possession_team_id),
    CONSTRAINT fk_scoreboard_states_match
        FOREIGN KEY (match_id) REFERENCES matches(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_scoreboard_states_possession_team
        FOREIGN KEY (possession_team_id) REFERENCES teams(id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS match_events (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    match_id INT UNSIGNED NOT NULL,
    period INT NOT NULL,
    period_type ENUM('Regular', 'Overtime', 'SuddenDeath') NOT NULL,
    game_clock_ms_remaining INT NOT NULL,
    shot_clock_ms_remaining INT NULL,
    team_id INT UNSIGNED NULL,
    player_id INT UNSIGNED NULL,
    event_type VARCHAR(80) NOT NULL,
    points INT NULL,
    is_correction TINYINT(1) NOT NULL DEFAULT 0,
    reverts_event_id INT UNSIGNED NULL,
    description TEXT NULL,
    payload_json JSON NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    synced_at DATETIME NULL,
    PRIMARY KEY (id),
    INDEX ix_match_events_match_created (match_id, created_at),
    INDEX ix_match_events_team_id (team_id),
    INDEX ix_match_events_player_id (player_id),
    INDEX ix_match_events_type (event_type),
    CONSTRAINT fk_match_events_match
        FOREIGN KEY (match_id) REFERENCES matches(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_match_events_team
        FOREIGN KEY (team_id) REFERENCES teams(id)
        ON DELETE SET NULL,
    CONSTRAINT fk_match_events_player
        FOREIGN KEY (player_id) REFERENCES players(id)
        ON DELETE SET NULL,
    CONSTRAINT fk_match_events_reverts_event
        FOREIGN KEY (reverts_event_id) REFERENCES match_events(id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS forfeit_results (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    match_id INT UNSIGNED NOT NULL,
    winning_team_id INT UNSIGNED NULL,
    losing_team_id INT UNSIGNED NULL,
    home_assigned_score INT NOT NULL DEFAULT 20,
    away_assigned_score INT NOT NULL DEFAULT 0,
    reason ENUM('NotEnoughPlayers', 'Abandonment', 'Disqualification', 'WeatherDecision', 'OrganizerDecision') NOT NULL,
    notes TEXT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_forfeit_results_match (match_id),
    INDEX ix_forfeit_results_winning_team_id (winning_team_id),
    INDEX ix_forfeit_results_losing_team_id (losing_team_id),
    CONSTRAINT fk_forfeit_results_match
        FOREIGN KEY (match_id) REFERENCES matches(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_forfeit_results_winning_team
        FOREIGN KEY (winning_team_id) REFERENCES teams(id)
        ON DELETE SET NULL,
    CONSTRAINT fk_forfeit_results_losing_team
        FOREIGN KEY (losing_team_id) REFERENCES teams(id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS competition_events (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    edition_id INT UNSIGNED NOT NULL,
    event_type ENUM('ThreePointContest', 'AwardCeremony', 'Other') NOT NULL,
    name VARCHAR(160) NOT NULL,
    scheduled_start_at DATETIME NULL,
    scheduled_end_at DATETIME NULL,
    status ENUM('Scheduled', 'Live', 'Completed', 'Cancelled') NOT NULL DEFAULT 'Scheduled',
    PRIMARY KEY (id),
    INDEX ix_competition_events_edition_id (edition_id),
    CONSTRAINT fk_competition_events_edition
        FOREIGN KEY (edition_id) REFERENCES editions(id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS three_point_contest_entries (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    competition_event_id INT UNSIGNED NOT NULL,
    team_id INT UNSIGNED NOT NULL,
    player_id INT UNSIGNED NOT NULL,
    seed_order INT NULL,
    total_score INT NOT NULL DEFAULT 0,
    final_position INT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY ux_three_point_entries_event_team (competition_event_id, team_id),
    UNIQUE KEY ux_three_point_entries_event_player (competition_event_id, player_id),
    INDEX ix_three_point_entries_team_id (team_id),
    INDEX ix_three_point_entries_player_id (player_id),
    CONSTRAINT fk_three_point_entries_event
        FOREIGN KEY (competition_event_id) REFERENCES competition_events(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_three_point_entries_team
        FOREIGN KEY (team_id) REFERENCES teams(id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_three_point_entries_player
        FOREIGN KEY (player_id) REFERENCES players(id)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS three_point_contest_rounds (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    entry_id INT UNSIGNED NOT NULL,
    round_number INT NOT NULL,
    round_type ENUM('Qualification', 'Final', 'TieBreak') NOT NULL,
    station1_score INT NOT NULL DEFAULT 0,
    station2_score INT NOT NULL DEFAULT 0,
    station3_score INT NOT NULL DEFAULT 0,
    station4_score INT NOT NULL DEFAULT 0,
    station5_score INT NOT NULL DEFAULT 0,
    total_score INT NOT NULL DEFAULT 0,
    notes TEXT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY ux_three_point_rounds_entry_round_type (entry_id, round_number, round_type),
    CONSTRAINT fk_three_point_rounds_entry
        FOREIGN KEY (entry_id) REFERENCES three_point_contest_entries(id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabella tecnica utile quando l'app inviera dati online.
-- Non serve al sito in lettura, ma aiuta a tracciare invii e retry.
SET FOREIGN_KEY_CHECKS = 1;
