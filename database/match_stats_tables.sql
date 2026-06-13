-- ============================================================================
-- Tabelle statistiche partita per il DB reale `Sql1938817_1`
-- Versione: 2026-06-07
-- Target: Hosting condiviso Aruba, MySQL 8.0
--
-- Scopo:
--   Aggiungere le statistiche COMPLETE del tabellino (che l'attuale
--   `match_players` non contiene: ha solo points + personal_fouls), a partire
--   dal JSON di output dell'OCR (runtime/OutputJson/<file>.json).
--   Si tralasciano warning, confidenze, candidati e metadati di debug:
--   solo i numeri delle statistiche.
--
--   - match_player_stats : box score per giocatore
--   - match_team_stats   : totali squadra + comparative FIBA
--
--   I riferimenti (partita, squadra, giocatore) sono FK alle tabelle esistenti
--   `matches`, `teams`, `players` (vedi note di risoluzione in fondo).
--
-- Uso:
--   Eseguire questo script sul database Sql1938817_1.
-- ============================================================================

SET NAMES utf8mb4;

-- ----------------------------------------------------------------------------
-- 1) Box score per giocatore
-- ----------------------------------------------------------------------------
CREATE TABLE `match_player_stats` (
  `id` int UNSIGNED NOT NULL AUTO_INCREMENT,
  `match_id` int UNSIGNED NOT NULL,
  `team_id` int UNSIGNED NOT NULL,
  `player_id` int UNSIGNED NOT NULL,

  `jersey_number` int DEFAULT NULL,
  `is_starter` tinyint(1) NOT NULL DEFAULT 0,
  `did_not_play` tinyint(1) NOT NULL DEFAULT 0,

  -- Valori grezzi OCR: NULL distingue il N.E. (did_not_play=1) dallo 0 reale.
  `minutes_seconds` int DEFAULT NULL,           -- da "MM:SS" convertito in secondi

  `fg_made` int DEFAULT NULL,                   -- fieldGoals.made
  `fg_att` int DEFAULT NULL,                    -- fieldGoals.attempted
  `two_made` int DEFAULT NULL,                  -- twoPoints.made
  `two_att` int DEFAULT NULL,                   -- twoPoints.attempted
  `three_made` int DEFAULT NULL,                -- threePoints.made
  `three_att` int DEFAULT NULL,                 -- threePoints.attempted
  `ft_made` int DEFAULT NULL,                   -- freeThrows.made
  `ft_att` int DEFAULT NULL,                    -- freeThrows.attempted

  `reb_off` int DEFAULT NULL,                   -- rebounds.offensive
  `reb_def` int DEFAULT NULL,                   -- rebounds.defensive
                                                -- (rebounds.total derivato: off+def)
  `assists` int DEFAULT NULL,
  `turnovers` int DEFAULT NULL,
  `steals` int DEFAULT NULL,
  `blocks` int DEFAULT NULL,

  `fouls_committed` int DEFAULT NULL,           -- fouls.committed
  `fouls_drawn` int DEFAULT NULL,               -- fouls.drawn

  `plus_minus` int DEFAULT NULL,                -- firmato
  `evaluation` int DEFAULT NULL,                -- firmato
  `points` int DEFAULT NULL,

  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_match_player_stats_match_player` (`match_id`,`player_id`),
  KEY `ix_match_player_stats_team_id` (`team_id`),
  KEY `ix_match_player_stats_player_id` (`player_id`),
  CONSTRAINT `fk_match_player_stats_match`  FOREIGN KEY (`match_id`)  REFERENCES `matches` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_match_player_stats_team`   FOREIGN KEY (`team_id`)   REFERENCES `teams` (`id`)   ON DELETE RESTRICT,
  CONSTRAINT `fk_match_player_stats_player` FOREIGN KEY (`player_id`) REFERENCES `players` (`id`)  ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ----------------------------------------------------------------------------
-- 2) Totali squadra + comparative FIBA
-- ----------------------------------------------------------------------------
CREATE TABLE `match_team_stats` (
  `id` int UNSIGNED NOT NULL AUTO_INCREMENT,
  `match_id` int UNSIGNED NOT NULL,
  `team_id` int UNSIGNED NOT NULL,

  -- Totali squadra (scope Team del JSON).
  `fg_made` int DEFAULT NULL,
  `fg_att` int DEFAULT NULL,
  `two_made` int DEFAULT NULL,
  `two_att` int DEFAULT NULL,
  `three_made` int DEFAULT NULL,
  `three_att` int DEFAULT NULL,
  `ft_made` int DEFAULT NULL,
  `ft_att` int DEFAULT NULL,
  `reb_off` int DEFAULT NULL,
  `reb_def` int DEFAULT NULL,
  `assists` int DEFAULT NULL,
  `turnovers` int DEFAULT NULL,
  `steals` int DEFAULT NULL,
  `blocks` int DEFAULT NULL,
  `fouls_committed` int DEFAULT NULL,
  `fouls_drawn` int DEFAULT NULL,
  `points` int DEFAULT NULL,

  -- Comparative FIBA (scope Comparative del JSON).
  `points_in_paint` int DEFAULT NULL,                   -- comparative.pointsInThePaint
  `fast_break_points` int DEFAULT NULL,                 -- comparative.fastBreakPoints
  `fast_break_points_off_turnovers` int DEFAULT NULL,   -- comparative.fastBreakPointsFromTurnovers
  `second_chance_points` int DEFAULT NULL,              -- comparative.secondChancePoints
  `points_off_turnovers` int DEFAULT NULL,              -- comparative.pointsFromTurnovers
  `bench_points` int DEFAULT NULL,                      -- comparative.benchPoints
  `biggest_lead` int DEFAULT NULL,                      -- comparative.largestLead
  `biggest_run` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,   -- comparative.biggestRun (es. "12-0")
  `lead_changes` int DEFAULT NULL,                      -- comparative.leadChanges
  `times_tied` int DEFAULT NULL,                        -- comparative.timesTied
  `time_in_lead` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,  -- comparative.timeInLead
  `points_per_possession` decimal(8,3) DEFAULT NULL,    -- comparative.pointsPerPossession

  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_match_team_stats_match_team` (`match_id`,`team_id`),
  KEY `ix_match_team_stats_team_id` (`team_id`),
  CONSTRAINT `fk_match_team_stats_match` FOREIGN KEY (`match_id`) REFERENCES `matches` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_match_team_stats_team`  FOREIGN KEY (`team_id`)  REFERENCES `teams` (`id`)   ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ============================================================================
-- Risoluzione dei riferimenti dalle tabelle esistenti (in fase di import)
-- ----------------------------------------------------------------------------
-- Il JSON di output non contiene gli id del DB: si risolvono cosi'.
--
--   Partita  (match_id): id del match gia' a calendario (fornito dall'import).
--
--   Squadra  (team_id): dal lato Home/Away del JSON
--     SELECT team_id FROM match_teams
--      WHERE match_id = :match_id AND side = :side;   -- side = 'Home' | 'Away'
--
--   Giocatore (player_id): dal numero di maglia del JSON
--     SELECT player_id FROM team_rosters
--      WHERE team_id = :team_id AND jersey_number = :jersey;
--     (rimuovere l'eventuale '*' di starter dal numero: va in is_starter)
--
-- Upsert idempotente (re-import della stessa partita):
--   INSERT INTO match_player_stats (...) VALUES (...)
--   ON DUPLICATE KEY UPDATE fg_made=VALUES(fg_made), ... , points=VALUES(points);
-- ============================================================================
