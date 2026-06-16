-- Piazzetta Madness - query di sola lettura per il sito web
-- Schema: docs/online-mysql-schema.sql
-- Per limitare i risultati a una singola edizione, aggiungere:
--   AND m.edition_id = :edition_id


-- 1. PARTITA ATTUALMENTE IN CORSO
-- Restituisce al massimo una partita. Live ha precedenza su Paused.
SELECT
    m.id AS match_id,
    m.name AS match_name,
    m.phase,
    m.round,
    m.status,
    m.scheduled_start_at,
    m.actual_start_at,
    t.name AS tournament_name,
    e.id AS edition_id,
    e.name AS edition_name,
    c.name AS court_name,
    c.location AS court_location,
    home_team.id AS home_team_id,
    home_team.name AS home_team_name,
    home_team.short_name AS home_team_short_name,
    home_team.primary_color AS home_team_color,
    home_team.logo_path AS home_team_logo,
    COALESCE(ss.home_score, home_side.score, 0) AS home_score,
    away_team.id AS away_team_id,
    away_team.name AS away_team_name,
    away_team.short_name AS away_team_short_name,
    away_team.primary_color AS away_team_color,
    away_team.logo_path AS away_team_logo,
    COALESCE(ss.away_score, away_side.score, 0) AS away_score,
    COALESCE(ss.current_period, 1) AS current_period,
    COALESCE(ss.current_period_type, 'Regular') AS current_period_type,
    COALESCE(ss.game_clock_ms_remaining, m.period_duration_ms) AS game_clock_ms_remaining,
    COALESCE(ss.shot_clock_ms_remaining, m.shot_clock_ms) AS shot_clock_ms_remaining,
    COALESCE(ss.home_fouls_current_period, home_side.fouls_current_period, 0) AS home_fouls,
    COALESCE(ss.away_fouls_current_period, away_side.fouls_current_period, 0) AS away_fouls,
    COALESCE(ss.is_game_clock_running, 0) AS is_game_clock_running,
    COALESCE(ss.is_shot_clock_running, 0) AS is_shot_clock_running,
    ss.last_updated_at
FROM matches m
INNER JOIN editions e ON e.id = m.edition_id
INNER JOIN tournaments t ON t.id = e.tournament_id
LEFT JOIN courts c ON c.id = m.court_id
INNER JOIN match_teams home_side ON home_side.match_id = m.id AND home_side.side = 'Home'
INNER JOIN teams home_team ON home_team.id = home_side.team_id
INNER JOIN match_teams away_side ON away_side.match_id = m.id AND away_side.side = 'Away'
INNER JOIN teams away_team ON away_team.id = away_side.team_id
LEFT JOIN scoreboard_states ss ON ss.match_id = m.id
WHERE m.status IN ('Live', 'Paused')
ORDER BY
    CASE m.status WHEN 'Live' THEN 0 ELSE 1 END,
    COALESCE(m.actual_start_at, m.scheduled_start_at) DESC,
    m.id DESC
LIMIT 1;


-- 2. PARTITE CONCLUSE
-- Il parametro :limit va sostituito con un intero o gestito tramite prepared statement.
SELECT
    m.id AS match_id,
    m.name AS match_name,
    m.phase,
    m.round,
    m.status,
    m.scheduled_start_at,
    m.actual_start_at,
    m.actual_end_at,
    m.win_reason,
    t.name AS tournament_name,
    e.id AS edition_id,
    e.name AS edition_name,
    c.name AS court_name,
    home_team.id AS home_team_id,
    home_team.name AS home_team_name,
    home_team.short_name AS home_team_short_name,
    home_team.primary_color AS home_team_color,
    home_team.logo_path AS home_team_logo,
    home_side.score AS home_score,
    away_team.id AS away_team_id,
    away_team.name AS away_team_name,
    away_team.short_name AS away_team_short_name,
    away_team.primary_color AS away_team_color,
    away_team.logo_path AS away_team_logo,
    away_side.score AS away_score,
    winner.id AS winner_team_id,
    winner.name AS winner_team_name
FROM matches m
INNER JOIN editions e ON e.id = m.edition_id
INNER JOIN tournaments t ON t.id = e.tournament_id
LEFT JOIN courts c ON c.id = m.court_id
INNER JOIN match_teams home_side ON home_side.match_id = m.id AND home_side.side = 'Home'
INNER JOIN teams home_team ON home_team.id = home_side.team_id
INNER JOIN match_teams away_side ON away_side.match_id = m.id AND away_side.side = 'Away'
INNER JOIN teams away_team ON away_team.id = away_side.team_id
LEFT JOIN teams winner ON winner.id = m.winner_team_id
WHERE m.status = 'Finished'
ORDER BY COALESCE(m.actual_end_at, m.scheduled_end_at, m.scheduled_start_at) DESC, m.id DESC
LIMIT :limit;


-- 3. PARTITE FUTURE O ANCORA DA GIOCARE
-- Lo stato e il criterio principale: include anche una partita in ritardo ma non ancora iniziata.
SELECT
    m.id AS match_id,
    m.name AS match_name,
    m.phase,
    m.round,
    m.status,
    m.scheduled_start_at,
    m.scheduled_end_at,
    m.period_count,
    m.period_duration_ms,
    t.name AS tournament_name,
    e.id AS edition_id,
    e.name AS edition_name,
    c.name AS court_name,
    c.location AS court_location,
    home_team.id AS home_team_id,
    home_team.name AS home_team_name,
    home_team.short_name AS home_team_short_name,
    home_team.primary_color AS home_team_color,
    home_team.logo_path AS home_team_logo,
    away_team.id AS away_team_id,
    away_team.name AS away_team_name,
    away_team.short_name AS away_team_short_name,
    away_team.primary_color AS away_team_color,
    away_team.logo_path AS away_team_logo
FROM matches m
INNER JOIN editions e ON e.id = m.edition_id
INNER JOIN tournaments t ON t.id = e.tournament_id
LEFT JOIN courts c ON c.id = m.court_id
INNER JOIN match_teams home_side ON home_side.match_id = m.id AND home_side.side = 'Home'
INNER JOIN teams home_team ON home_team.id = home_side.team_id
INNER JOIN match_teams away_side ON away_side.match_id = m.id AND away_side.side = 'Away'
INNER JOIN teams away_team ON away_team.id = away_side.team_id
WHERE m.status IN ('Scheduled', 'Ready')
ORDER BY
    CASE WHEN m.scheduled_start_at IS NULL THEN 1 ELSE 0 END,
    m.scheduled_start_at ASC,
    m.id ASC;


-- 4. STATISTICHE GIOCATORI DI UNA PARTITA
-- Query accessoria per la pagina di dettaglio. Impostare :match_id.
SELECT
    mp.match_id,
    mp.team_id,
    tm.name AS team_name,
    tm.short_name AS team_short_name,
    tm.primary_color AS team_color,
    mp.player_id,
    mp.jersey_number,
    p.first_name,
    p.last_name,
    p.nickname,
    mp.points,
    mp.personal_fouls,
    mp.is_fouled_out,
    mp.is_ejected
FROM match_players mp
INNER JOIN players p ON p.id = mp.player_id
INNER JOIN teams tm ON tm.id = mp.team_id
WHERE mp.match_id = :match_id
ORDER BY tm.name, mp.jersey_number, p.last_name, p.first_name;
