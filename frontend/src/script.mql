-- ============================================================================
-- Live Basket DB Schema - MySQL / MariaDB
-- Versione: 2026-06-03
-- Target: Hosting condiviso Aruba Linux, PHP + MySQL
--
-- Scopo:
--   Schema E/R per sistema Live Basket:
--   - tabellone desktop C# come fonte live
--   - OCR/PDF come fonte statistiche complete
--   - pubblicazione live-state.json e stats-state.json via CDN
--
-- Note:
--   - Tutte le tabelle usano InnoDB, FK e transazioni.
--   - Charset utf8mb4 per nomi con accenti.
--   - Timestamp in UTC: conversione fuso lato applicazione/sito.
--   - Il sito pubblico NON legge direttamente queste tabelle: legge i JSON.
--
-- Uso:
--   1) Crea/seleziona il database su Aruba.
--   2) Esegui questo script.
--   3) Mantieni le credenziali DB solo lato backend PHP.
-- ============================================================================

SET NAMES utf8mb4;
SET time_zone = '+00:00';

-- ============================================================================
-- 1. ANAGRAFICA TORNEO
-- ============================================================================

CREATE TABLE IF NOT EXISTS tournaments (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  name VARCHAR(160) NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_tournaments_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


CREATE TABLE IF NOT EXISTS editions (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  tournament_id INT UNSIGNED NOT NULL,
  name VARCHAR(160) NOT NULL,
  year SMALLINT UNSIGNED NOT NULL,
  status ENUM('draft','active','completed','cancelled') NOT NULL DEFAULT 'draft',
  start_date DATE NULL,
  end_date DATE NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_editions_tournament_year (tournament_id, year),
  KEY idx_editions_status (status),
  CONSTRAINT fk_editions_tournament
    FOREIGN KEY (tournament_id) REFERENCES tournaments(id)
    ON UPDATE RESTRICT ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


CREATE TABLE IF NOT EXISTS tournament_groups (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  edition_id INT UNSIGNED NOT NULL,
  name VARCHAR(120) NOT NULL,
  code VARCHAR(40) NULL,
  sort_order INT UNSIGNED NOT NULL DEFAULT 0,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_tournament_groups_code (edition_id, code),
  KEY idx_tournament_groups_edition (edition_id),
  CONSTRAINT fk_tournament_groups_edition
    FOREIGN KEY (edition_id) REFERENCES editions(id)
    ON UPDATE RESTRICT ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


CREATE TABLE IF NOT EXISTS teams (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  edition_id INT UNSIGNED NOT NULL,
  name VARCHAR(160) NOT NULL,
  short_name VARCHAR(30) NULL,
  primary_color VARCHAR(20) NULL,
  secondary_color VARCHAR(20) NULL,
  logo_path VARCHAR(500) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_teams_edition_name (edition_id, name),
  KEY idx_teams_edition (edition_id),
  CONSTRAINT fk_teams_edition
    FOREIGN KEY (edition_id) REFERENCES editions(id)
    ON UPDATE RESTRICT ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


CREATE TABLE IF NOT EXISTS players (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  first_name VARCHAR(120) NOT NULL,
  last_name VARCHAR(120) NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_players_name (last_name, first_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


CREATE TABLE IF NOT EXISTS team_rosters (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  team_id INT UNSIGNED NOT NULL,
  player_id INT UNSIGNED NOT NULL,
  jersey_number VARCHAR(10) NULL,
  role VARCHAR(60) NULL,
  is_captain TINYINT(1) NOT NULL DEFAULT 0,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_team_rosters_team_player (team_id, player_id),
  UNIQUE KEY uq_team_rosters_team_jersey (team_id, jersey_number),
  KEY idx_team_rosters_player (player_id),
  CONSTRAINT fk_team_rosters_team
    FOREIGN KEY (team_id) REFERENCES teams(id)
    ON UPDATE RESTRICT ON DELETE CASCADE,
  CONSTRAINT fk_team_rosters_player
    FOREIGN KEY (player_id) REFERENCES players(id)
    ON UPDATE RESTRICT ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


CREATE TABLE IF NOT EXISTS group_teams (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  group_id INT UNSIGNED NOT NULL,
  team_id INT UNSIGNED NOT NULL,
  seed_label VARCHAR(40) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_group_teams_group_team (group_id, team_id),
  KEY idx_group_teams_team (team_id),
  CONSTRAINT fk_group_teams_group
    FOREIGN KEY (group_id) REFERENCES tournament_groups(id)
    ON UPDATE RESTRICT ON DELETE CASCADE,
  CONSTRAINT fk_group_teams_team
    FOREIGN KEY (team_id) REFERENCES teams(id)
    ON UPDATE RESTRICT ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ============================================================================
-- 2. PARTITE
-- ============================================================================

CREATE TABLE IF NOT EXISTS matches (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  edition_id INT UNSIGNED NOT NULL,
  group_id INT UNSIGNED NULL,

  home_team_id INT UNSIGNED NOT NULL,
  away_team_id INT UNSIGNED NOT NULL,

  phase ENUM('group_stage','semifinal','third_place_final','final','friendly','other') NOT NULL DEFAULT 'group_stage',
  round_label VARCHAR(80) NULL,
  scheduled_at DATETIME NULL,

  status ENUM('scheduled','ready','live','paused','finished','cancelled') NOT NULL DEFAULT 'scheduled',

  -- Regole partita: default coerenti con tornei 2 periodi + eventuale overtime.
  period_count TINYINT UNSIGNED NOT NULL DEFAULT 2,
  period_duration_seconds SMALLINT UNSIGNED NOT NULL DEFAULT 720,
  overtime_duration_seconds SMALLINT UNSIGNED NOT NULL DEFAULT 120,
  timeouts_per_team TINYINT UNSIGNED NOT NULL DEFAULT 2,
  personal_foul_limit TINYINT UNSIGNED NOT NULL DEFAULT 5,
  team_foul_bonus_threshold TINYINT UNSIGNED NOT NULL DEFAULT 5,

  -- Risultato consolidato a fine partita.
  final_home_score SMALLINT UNSIGNED NULL,
  final_away_score SMALLINT UNSIGNED NULL,
  winner_team_id INT UNSIGNED NULL,
  win_reason ENUM('regular','overtime','forfeit','disqualification','abandoned','other') NULL,

  notes TEXT NULL,
  started_at DATETIME NULL,
  finished_at DATETIME NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (id),
  KEY idx_matches_edition_status (edition_id, status),
  KEY idx_matches_group (group_id),
  KEY idx_matches_home_team (home_team_id),
  KEY idx_matches_away_team (away_team_id),
  KEY idx_matches_winner (winner_team_id),
  CONSTRAINT fk_matches_edition
    FOREIGN KEY (edition_id) REFERENCES editions(id)
    ON UPDATE RESTRICT ON DELETE CASCADE,
  CONSTRAINT fk_matches_group
    FOREIGN KEY (group_id) REFERENCES tournament_groups(id)
    ON UPDATE RESTRICT ON DELETE SET NULL,
  CONSTRAINT fk_matches_home_team
    FOREIGN KEY (home_team_id) REFERENCES teams(id)
    ON UPDATE RESTRICT ON DELETE RESTRICT,
  CONSTRAINT fk_matches_away_team
    FOREIGN KEY (away_team_id) REFERENCES teams(id)
    ON UPDATE RESTRICT ON DELETE RESTRICT,
  CONSTRAINT fk_matches_winner_team
    FOREIGN KEY (winner_team_id) REFERENCES teams(id)
    ON UPDATE RESTRICT ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


CREATE TABLE IF NOT EXISTS match_periods (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  match_id INT UNSIGNED NOT NULL,
  period_number TINYINT UNSIGNED NOT NULL,
  period_type ENUM('regular','overtime') NOT NULL DEFAULT 'regular',

  -- Parziale del periodo, non punteggio cumulativo.
  home_score SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  away_score SMALLINT UNSIGNED NOT NULL DEFAULT 0,

  started_at DATETIME NULL,
  ended_at DATETIME NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (id),
  UNIQUE KEY uq_match_periods_match_period (match_id, period_number),
  CONSTRAINT fk_match_periods_match
    FOREIGN KEY (match_id) REFERENCES matches(id)
    ON UPDATE RESTRICT ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ============================================================================
-- 3. SICUREZZA API / DISPOSITIVI
-- ============================================================================

CREATE TABLE IF NOT EXISTS api_users (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  name VARCHAR(160) NOT NULL,
  role ENUM('scoreboard_device','ocr','admin') NOT NULL,
  token_hash VARCHAR(255) NOT NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  last_used_at DATETIME NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_api_users_name (name),
  KEY idx_api_users_role_active (role, is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ============================================================================
-- 4. STATO LIVE CORRENTE
-- ============================================================================

CREATE TABLE IF NOT EXISTS live_match_state (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  match_id INT UNSIGNED NOT NULL,

  has_live TINYINT(1) NOT NULL DEFAULT 0,
  status ENUM('none','pre','live','paused','finished') NOT NULL DEFAULT 'none',

  -- Versione live: incrementa solo quando cambia lo stato mostrato al pubblico.
  version INT UNSIGNED NOT NULL DEFAULT 0,

  period TINYINT UNSIGNED NOT NULL DEFAULT 1,
  clock_seconds SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  clock_tenths TINYINT UNSIGNED NOT NULL DEFAULT 0,
  clock_running TINYINT(1) NOT NULL DEFAULT 0,
  clock_snapshot_at DATETIME NULL,

  home_score SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  away_score SMALLINT UNSIGNED NOT NULL DEFAULT 0,

  home_timeouts_used TINYINT UNSIGNED NOT NULL DEFAULT 0,
  away_timeouts_used TINYINT UNSIGNED NOT NULL DEFAULT 0,

  last_event_type ENUM('match_start','score','foul','timeout','clock_start','clock_stop','period_change','correction','heartbeat','finish') NULL,

  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (id),
  UNIQUE KEY uq_live_match_state_match (match_id),
  KEY idx_live_match_state_has_live (has_live, status),
  CONSTRAINT fk_live_match_state_match
    FOREIGN KEY (match_id) REFERENCES matches(id)
    ON UPDATE RESTRICT ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


CREATE TABLE IF NOT EXISTS live_player_state (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  match_id INT UNSIGNED NOT NULL,
  team_id INT UNSIGNED NOT NULL,
  player_id INT UNSIGNED NOT NULL,

  points SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  fouls TINYINT UNSIGNED NOT NULL DEFAULT 0,

  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (id),
  UNIQUE KEY uq_live_player_state_match_player (match_id, player_id),
  KEY idx_live_player_state_match_team (match_id, team_id),
  KEY idx_live_player_state_player (player_id),
  CONSTRAINT fk_live_player_state_match
    FOREIGN KEY (match_id) REFERENCES matches(id)
    ON UPDATE RESTRICT ON DELETE CASCADE,
  CONSTRAINT fk_live_player_state_team
    FOREIGN KEY (team_id) REFERENCES teams(id)
    ON UPDATE RESTRICT ON DELETE RESTRICT,
  CONSTRAINT fk_live_player_state_player
    FOREIGN KEY (player_id) REFERENCES players(id)
    ON UPDATE RESTRICT ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ============================================================================
-- 5. EVENTI TABELLONE
-- ============================================================================

CREATE TABLE IF NOT EXISTS match_events (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,

  match_id INT UNSIGNED NOT NULL,
  device_id INT UNSIGNED NOT NULL,

  -- Idempotenza offline/retry app C#.
  local_event_id VARCHAR(80) NOT NULL,
  client_sequence INT UNSIGNED NOT NULL,

  event_type ENUM(
    'match_start',
    'score',
    'foul',
    'timeout',
    'clock_start',
    'clock_stop',
    'period_change',
    'correction',
    'heartbeat',
    'finish'
  ) NOT NULL,

  team_id INT UNSIGNED NULL,
  player_id INT UNSIGNED NULL,

  period TINYINT UNSIGNED NULL,
  clock_seconds SMALLINT UNSIGNED NULL,
  clock_tenths TINYINT UNSIGNED NULL,

  -- Campi diretti per query/debug senza parsare payload_json.
  points_delta TINYINT NULL,
  score_type ENUM('free_throw','two_points','three_points') NULL,
  foul_type ENUM('personal','technical','unsportsmanlike','disqualifying','other') NULL,
  correction_target ENUM('team_score','player_points','player_foul','timeout','clock','period','undo_event') NULL,

  payload_json JSON NULL,

  client_created_at DATETIME NOT NULL,
  server_received_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

  is_correction TINYINT(1) NOT NULL DEFAULT 0,
  reverts_event_id INT UNSIGNED NULL,

  PRIMARY KEY (id),
  UNIQUE KEY uq_match_events_idempotency (device_id, local_event_id),
  KEY idx_match_events_match_sequence (match_id, client_sequence),
  KEY idx_match_events_match_type (match_id, event_type),
  KEY idx_match_events_match_period (match_id, period),
  KEY idx_match_events_team (team_id),
  KEY idx_match_events_player (player_id),
  KEY idx_match_events_reverts (reverts_event_id),
  CONSTRAINT fk_match_events_match
    FOREIGN KEY (match_id) REFERENCES matches(id)
    ON UPDATE RESTRICT ON DELETE CASCADE,
  CONSTRAINT fk_match_events_device
    FOREIGN KEY (device_id) REFERENCES api_users(id)
    ON UPDATE RESTRICT ON DELETE RESTRICT,
  CONSTRAINT fk_match_events_team
    FOREIGN KEY (team_id) REFERENCES teams(id)
    ON UPDATE RESTRICT ON DELETE SET NULL,
  CONSTRAINT fk_match_events_player
    FOREIGN KEY (player_id) REFERENCES players(id)
    ON UPDATE RESTRICT ON DELETE SET NULL,
  CONSTRAINT fk_match_events_reverts
    FOREIGN KEY (reverts_event_id) REFERENCES match_events(id)
    ON UPDATE RESTRICT ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ============================================================================
-- 6. IMPORT OCR / STATISTICHE COMPLETE
-- ============================================================================

CREATE TABLE IF NOT EXISTS ocr_import_log (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  match_id INT UNSIGNED NOT NULL,

  source_pdf VARCHAR(500) NULL,
  document_hash CHAR(64) NULL,

  stats_version INT UNSIGNED NOT NULL,
  imported_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

  status ENUM('draft','published','failed','rejected') NOT NULL DEFAULT 'draft',

  warnings_json JSON NULL,
  raw_json_path VARCHAR(500) NULL,
  error_message TEXT NULL,

  PRIMARY KEY (id),
  UNIQUE KEY uq_ocr_import_log_match_version (match_id, stats_version),
  KEY idx_ocr_import_log_hash (document_hash),
  KEY idx_ocr_import_log_status (status),
  CONSTRAINT fk_ocr_import_log_match
    FOREIGN KEY (match_id) REFERENCES matches(id)
    ON UPDATE RESTRICT ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


CREATE TABLE IF NOT EXISTS match_player_stats (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  match_id INT UNSIGNED NOT NULL,
  team_id INT UNSIGNED NOT NULL,
  player_id INT UNSIGNED NOT NULL,

  jersey_number VARCHAR(10) NULL,
  is_starter TINYINT(1) NOT NULL DEFAULT 0,
  did_not_play TINYINT(1) NOT NULL DEFAULT 0,

  -- Valori grezzi OCR: nullable per distinguere N.E. da 0 reale.
  minutes_seconds INT UNSIGNED NULL,

  fg_made SMALLINT UNSIGNED NULL,
  fg_att SMALLINT UNSIGNED NULL,
  two_made SMALLINT UNSIGNED NULL,
  two_att SMALLINT UNSIGNED NULL,
  three_made SMALLINT UNSIGNED NULL,
  three_att SMALLINT UNSIGNED NULL,
  ft_made SMALLINT UNSIGNED NULL,
  ft_att SMALLINT UNSIGNED NULL,

  reb_off SMALLINT UNSIGNED NULL,
  reb_def SMALLINT UNSIGNED NULL,

  assists SMALLINT UNSIGNED NULL,
  turnovers SMALLINT UNSIGNED NULL,
  steals SMALLINT UNSIGNED NULL,
  blocks SMALLINT UNSIGNED NULL,

  fouls_committed SMALLINT UNSIGNED NULL,
  fouls_drawn SMALLINT UNSIGNED NULL,

  plus_minus SMALLINT NULL,
  evaluation SMALLINT NULL,
  points SMALLINT UNSIGNED NULL,

  source ENUM('ocr') NOT NULL DEFAULT 'ocr',
  stats_version INT UNSIGNED NOT NULL,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (id),
  UNIQUE KEY uq_match_player_stats_match_player (match_id, player_id),
  KEY idx_match_player_stats_match_team (match_id, team_id),
  KEY idx_match_player_stats_player (player_id),
  KEY idx_match_player_stats_version (match_id, stats_version),
  CONSTRAINT fk_match_player_stats_match
    FOREIGN KEY (match_id) REFERENCES matches(id)
    ON UPDATE RESTRICT ON DELETE CASCADE,
  CONSTRAINT fk_match_player_stats_team
    FOREIGN KEY (team_id) REFERENCES teams(id)
    ON UPDATE RESTRICT ON DELETE RESTRICT,
  CONSTRAINT fk_match_player_stats_player
    FOREIGN KEY (player_id) REFERENCES players(id)
    ON UPDATE RESTRICT ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


CREATE TABLE IF NOT EXISTS match_team_stats (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  match_id INT UNSIGNED NOT NULL,
  team_id INT UNSIGNED NOT NULL,

  -- Totali grezzi squadra.
  minutes_seconds INT UNSIGNED NULL,

  fg_made SMALLINT UNSIGNED NULL,
  fg_att SMALLINT UNSIGNED NULL,
  two_made SMALLINT UNSIGNED NULL,
  two_att SMALLINT UNSIGNED NULL,
  three_made SMALLINT UNSIGNED NULL,
  three_att SMALLINT UNSIGNED NULL,
  ft_made SMALLINT UNSIGNED NULL,
  ft_att SMALLINT UNSIGNED NULL,

  reb_off SMALLINT UNSIGNED NULL,
  reb_def SMALLINT UNSIGNED NULL,

  assists SMALLINT UNSIGNED NULL,
  turnovers SMALLINT UNSIGNED NULL,
  steals SMALLINT UNSIGNED NULL,
  blocks SMALLINT UNSIGNED NULL,

  fouls_committed SMALLINT UNSIGNED NULL,
  fouls_drawn SMALLINT UNSIGNED NULL,

  plus_minus SMALLINT NULL,
  evaluation SMALLINT NULL,
  points SMALLINT UNSIGNED NULL,

  -- Comparative FIBA.
  points_in_paint SMALLINT UNSIGNED NULL,
  fast_break_points SMALLINT UNSIGNED NULL,
  second_chance_points SMALLINT UNSIGNED NULL,
  points_off_turnovers SMALLINT UNSIGNED NULL,
  bench_points SMALLINT UNSIGNED NULL,
  biggest_lead SMALLINT UNSIGNED NULL,
  biggest_run VARCHAR(20) NULL,
  lead_changes SMALLINT UNSIGNED NULL,
  times_tied SMALLINT UNSIGNED NULL,
  time_in_lead_seconds INT UNSIGNED NULL,
  points_per_possession DECIMAL(8,3) NULL,

  source ENUM('ocr') NOT NULL DEFAULT 'ocr',
  stats_version INT UNSIGNED NOT NULL,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (id),
  UNIQUE KEY uq_match_team_stats_match_team (match_id, team_id),
  KEY idx_match_team_stats_team (team_id),
  KEY idx_match_team_stats_version (match_id, stats_version),
  CONSTRAINT fk_match_team_stats_match
    FOREIGN KEY (match_id) REFERENCES matches(id)
    ON UPDATE RESTRICT ON DELETE CASCADE,
  CONSTRAINT fk_match_team_stats_team
    FOREIGN KEY (team_id) REFERENCES teams(id)
    ON UPDATE RESTRICT ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ============================================================================
-- 7. PUBBLICAZIONE JSON
-- ============================================================================

CREATE TABLE IF NOT EXISTS snapshot_state (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  match_id INT UNSIGNED NOT NULL,
  kind ENUM('live','stats') NOT NULL,
  version INT UNSIGNED NOT NULL DEFAULT 0,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  json_path VARCHAR(500) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_snapshot_state_match_kind (match_id, kind),
  KEY idx_snapshot_state_kind (kind),
  CONSTRAINT fk_snapshot_state_match
    FOREIGN KEY (match_id) REFERENCES matches(id)
    ON UPDATE RESTRICT ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ============================================================================
-- 8. VISTE: DERIVATE CALCOLATE, NON MEMORIZZATE
-- ============================================================================

CREATE OR REPLACE VIEW v_match_player_stats_public AS
SELECT
  mps.id,
  mps.match_id,
  mps.team_id,
  t.name AS team_name,
  t.short_name AS team_short_name,
  mps.player_id,
  p.first_name,
  p.last_name,
  mps.jersey_number,
  mps.is_starter,
  mps.did_not_play,
  mps.minutes_seconds,

  mps.fg_made,
  mps.fg_att,
  CASE WHEN mps.fg_att IS NULL OR mps.fg_att = 0 THEN NULL
       ELSE ROUND(mps.fg_made / mps.fg_att * 100, 1) END AS fg_pct,

  mps.two_made,
  mps.two_att,
  CASE WHEN mps.two_att IS NULL OR mps.two_att = 0 THEN NULL
       ELSE ROUND(mps.two_made / mps.two_att * 100, 1) END AS two_pct,

  mps.three_made,
  mps.three_att,
  CASE WHEN mps.three_att IS NULL OR mps.three_att = 0 THEN NULL
       ELSE ROUND(mps.three_made / mps.three_att * 100, 1) END AS three_pct,

  mps.ft_made,
  mps.ft_att,
  CASE WHEN mps.ft_att IS NULL OR mps.ft_att = 0 THEN NULL
       ELSE ROUND(mps.ft_made / mps.ft_att * 100, 1) END AS ft_pct,

  mps.reb_off,
  mps.reb_def,
  CASE WHEN mps.did_not_play = 1 THEN NULL
       ELSE COALESCE(mps.reb_off, 0) + COALESCE(mps.reb_def, 0) END AS reb_tot,

  mps.assists,
  mps.turnovers,
  mps.steals,
  mps.blocks,
  mps.fouls_committed,
  mps.fouls_drawn,
  mps.plus_minus,
  mps.evaluation,
  mps.points,
  mps.stats_version,
  mps.updated_at
FROM match_player_stats mps
JOIN players p ON p.id = mps.player_id
JOIN teams t ON t.id = mps.team_id;


CREATE OR REPLACE VIEW v_match_team_stats_public AS
SELECT
  mts.id,
  mts.match_id,
  mts.team_id,
  t.name AS team_name,
  t.short_name AS team_short_name,
  mts.minutes_seconds,

  mts.fg_made,
  mts.fg_att,
  CASE WHEN mts.fg_att IS NULL OR mts.fg_att = 0 THEN NULL
       ELSE ROUND(mts.fg_made / mts.fg_att * 100, 1) END AS fg_pct,

  mts.two_made,
  mts.two_att,
  CASE WHEN mts.two_att IS NULL OR mts.two_att = 0 THEN NULL
       ELSE ROUND(mts.two_made / mts.two_att * 100, 1) END AS two_pct,

  mts.three_made,
  mts.three_att,
  CASE WHEN mts.three_att IS NULL OR mts.three_att = 0 THEN NULL
       ELSE ROUND(mts.three_made / mts.three_att * 100, 1) END AS three_pct,

  mts.ft_made,
  mts.ft_att,
  CASE WHEN mts.ft_att IS NULL OR mts.ft_att = 0 THEN NULL
       ELSE ROUND(mts.ft_made / mts.ft_att * 100, 1) END AS ft_pct,

  mts.reb_off,
  mts.reb_def,
  COALESCE(mts.reb_off, 0) + COALESCE(mts.reb_def, 0) AS reb_tot,

  mts.assists,
  mts.turnovers,
  mts.steals,
  mts.blocks,
  mts.fouls_committed,
  mts.fouls_drawn,
  mts.plus_minus,
  mts.evaluation,
  mts.points,

  mts.points_in_paint,
  mts.fast_break_points,
  mts.second_chance_points,
  mts.points_off_turnovers,
  mts.bench_points,
  mts.biggest_lead,
  mts.biggest_run,
  mts.lead_changes,
  mts.times_tied,
  mts.time_in_lead_seconds,
  mts.points_per_possession,

  mts.stats_version,
  mts.updated_at
FROM match_team_stats mts
JOIN teams t ON t.id = mts.team_id;


CREATE OR REPLACE VIEW v_standings AS
SELECT
  x.edition_id,
  x.team_id,
  SUM(x.games_played) AS games_played,
  SUM(x.wins) AS wins,
  SUM(x.losses) AS losses,
  SUM(x.points_for) AS points_for,
  SUM(x.points_against) AS points_against,
  SUM(x.points_for) - SUM(x.points_against) AS point_diff
FROM (
  SELECT
    m.edition_id,
    m.home_team_id AS team_id,
    1 AS games_played,
    CASE WHEN m.final_home_score > m.final_away_score THEN 1 ELSE 0 END AS wins,
    CASE WHEN m.final_home_score < m.final_away_score THEN 1 ELSE 0 END AS losses,
    m.final_home_score AS points_for,
    m.final_away_score AS points_against
  FROM matches m
  WHERE m.status = 'finished'
    AND m.final_home_score IS NOT NULL
    AND m.final_away_score IS NOT NULL

  UNION ALL

  SELECT
    m.edition_id,
    m.away_team_id AS team_id,
    1 AS games_played,
    CASE WHEN m.final_away_score > m.final_home_score THEN 1 ELSE 0 END AS wins,
    CASE WHEN m.final_away_score < m.final_home_score THEN 1 ELSE 0 END AS losses,
    m.final_away_score AS points_for,
    m.final_home_score AS points_against
  FROM matches m
  WHERE m.status = 'finished'
    AND m.final_home_score IS NOT NULL
    AND m.final_away_score IS NOT NULL
) x
GROUP BY x.edition_id, x.team_id;


CREATE OR REPLACE VIEW v_player_edition_stats AS
SELECT
  m.edition_id,
  mps.team_id,
  mps.player_id,
  COUNT(*) AS games_with_stats,
  SUM(CASE WHEN mps.did_not_play = 0 THEN 1 ELSE 0 END) AS games_played,
  SUM(COALESCE(mps.minutes_seconds, 0)) AS minutes_seconds,
  SUM(COALESCE(mps.points, 0)) AS points,
  SUM(COALESCE(mps.reb_off, 0)) AS reb_off,
  SUM(COALESCE(mps.reb_def, 0)) AS reb_def,
  SUM(COALESCE(mps.reb_off, 0) + COALESCE(mps.reb_def, 0)) AS reb_tot,
  SUM(COALESCE(mps.assists, 0)) AS assists,
  SUM(COALESCE(mps.turnovers, 0)) AS turnovers,
  SUM(COALESCE(mps.steals, 0)) AS steals,
  SUM(COALESCE(mps.blocks, 0)) AS blocks,
  SUM(COALESCE(mps.evaluation, 0)) AS evaluation
FROM match_player_stats mps
JOIN matches m ON m.id = mps.match_id
GROUP BY m.edition_id, mps.team_id, mps.player_id;


CREATE OR REPLACE VIEW v_team_edition_stats AS
SELECT
  m.edition_id,
  mts.team_id,
  COUNT(*) AS games_with_stats,
  SUM(COALESCE(mts.points, 0)) AS points,
  SUM(COALESCE(mts.reb_off, 0)) AS reb_off,
  SUM(COALESCE(mts.reb_def, 0)) AS reb_def,
  SUM(COALESCE(mts.reb_off, 0) + COALESCE(mts.reb_def, 0)) AS reb_tot,
  SUM(COALESCE(mts.assists, 0)) AS assists,
  SUM(COALESCE(mts.turnovers, 0)) AS turnovers,
  SUM(COALESCE(mts.steals, 0)) AS steals,
  SUM(COALESCE(mts.blocks, 0)) AS blocks,
  SUM(COALESCE(mts.points_in_paint, 0)) AS points_in_paint,
  SUM(COALESCE(mts.fast_break_points, 0)) AS fast_break_points,
  SUM(COALESCE(mts.second_chance_points, 0)) AS second_chance_points,
  SUM(COALESCE(mts.points_off_turnovers, 0)) AS points_off_turnovers,
  SUM(COALESCE(mts.bench_points, 0)) AS bench_points
FROM match_team_stats mts
JOIN matches m ON m.id = mts.match_id
GROUP BY m.edition_id, mts.team_id;

-- ============================================================================
-- Fine script
-- ============================================================================
