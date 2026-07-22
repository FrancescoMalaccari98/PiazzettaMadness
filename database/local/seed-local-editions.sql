-- Fixture locale: mantiene la copia Aruba e aggiunge uno storico 2025 completo.
-- Origine dati: edizione reale 2026 con id=5.

SET FOREIGN_KEY_CHECKS = 0;

DELETE s
FROM `three_point_contest_shots` s
INNER JOIN `three_point_contest_rounds` r ON r.`id` = s.`round_id`
INNER JOIN `three_point_contest_entries` e ON e.`id` = r.`entry_id`
INNER JOIN `competition_events` ce ON ce.`id` = e.`competition_event_id`
WHERE ce.`edition_id` = 105;

DELETE r
FROM `three_point_contest_rounds` r
INNER JOIN `three_point_contest_entries` e ON e.`id` = r.`entry_id`
INNER JOIN `competition_events` ce ON ce.`id` = e.`competition_event_id`
WHERE ce.`edition_id` = 105;

DELETE e
FROM `three_point_contest_entries` e
INNER JOIN `competition_events` ce ON ce.`id` = e.`competition_event_id`
WHERE ce.`edition_id` = 105;

DELETE me
FROM `match_events` me
INNER JOIN `matches` m ON m.`id` = me.`match_id`
WHERE m.`edition_id` = 105;

DELETE ss
FROM `scoreboard_states` ss
INNER JOIN `matches` m ON m.`id` = ss.`match_id`
WHERE m.`edition_id` = 105;

DELETE mp
FROM `match_players` mp
INNER JOIN `matches` m ON m.`id` = mp.`match_id`
WHERE m.`edition_id` = 105;

DELETE mps
FROM `match_player_stats` mps
INNER JOIN `matches` m ON m.`id` = mps.`match_id`
WHERE m.`edition_id` = 105;

DELETE mts
FROM `match_team_stats` mts
INNER JOIN `matches` m ON m.`id` = mts.`match_id`
WHERE m.`edition_id` = 105;

DELETE mt
FROM `match_teams` mt
INNER JOIN `matches` m ON m.`id` = mt.`match_id`
WHERE m.`edition_id` = 105;

DELETE FROM `matches` WHERE `edition_id` = 105;
DELETE FROM `standings` WHERE `team_id` IN (SELECT `id` FROM `teams` WHERE `edition_id` = 105);
DELETE FROM `group_teams` WHERE `team_id` IN (SELECT `id` FROM `teams` WHERE `edition_id` = 105);
DELETE FROM `team_rosters` WHERE `team_id` IN (SELECT `id` FROM `teams` WHERE `edition_id` = 105);
DELETE FROM `competition_events` WHERE `edition_id` = 105;
DELETE FROM `teams` WHERE `edition_id` = 105;
DELETE FROM `tournament_groups` WHERE `edition_id` = 105;
DELETE FROM `courts` WHERE `edition_id` = 105;
DELETE FROM `editions` WHERE `id` = 105;

-- La 2026 locale deve risultare conclusa, cosi' il sito puo' testare storico e albo d'oro.
UPDATE `editions`
SET
  `name` = 'Piazzetta Madness 2026',
  `status` = 'Completed',
  `is_console_active` = 0,
  `end_date` = '2026-07-11',
  `updated_at` = NOW()
WHERE `id` = 5;

-- Edizione 2025: copia completa della 2026 reale.
INSERT INTO `editions` (`id`, `tournament_id`, `name`, `year`, `start_date`, `end_date`, `status`, `is_console_active`, `created_at`, `updated_at`)
SELECT 105, `tournament_id`, 'Piazzetta Madness 2025', 2025, DATE_SUB(`start_date`, INTERVAL 1 YEAR), DATE_SUB('2026-07-11', INTERVAL 1 YEAR), 'Completed', 0, NOW(), NOW()
FROM `editions`
WHERE `id` = 5;

INSERT INTO `courts` (`id`, `edition_id`, `name`, `location`)
SELECT `id` + 1000, 105, `name`, `location`
FROM `courts`
WHERE `edition_id` = 5;

INSERT INTO `tournament_groups` (`id`, `edition_id`, `name`, `code`, `sort_order`)
SELECT `id` + 1000, 105, `name`, `code`, `sort_order`
FROM `tournament_groups`
WHERE `edition_id` = 5;

INSERT INTO `teams` (`id`, `edition_id`, `name`, `short_name`, `primary_color`, `secondary_color`, `logo_path`, `created_at`, `updated_at`)
SELECT `id` + 1000, 105, `name`, `short_name`, `primary_color`, `secondary_color`, `logo_path`, NOW(), NOW()
FROM `teams`
WHERE `edition_id` = 5;

INSERT INTO `group_teams` (`id`, `group_id`, `team_id`, `seed_label`)
SELECT `id` + 1000, `group_id` + 1000, `team_id` + 1000, `seed_label`
FROM `group_teams`
WHERE `team_id` IN (SELECT `id` FROM `teams` WHERE `edition_id` = 5);

INSERT INTO `team_rosters` (`id`, `team_id`, `player_id`, `jersey_number`, `role`, `is_captain`, `is_active`, `created_at`, `updated_at`)
SELECT `id` + 1000, `team_id` + 1000, `player_id`, `jersey_number`, `role`, `is_captain`, `is_active`, NOW(), NOW()
FROM `team_rosters`
WHERE `team_id` IN (SELECT `id` FROM `teams` WHERE `edition_id` = 5);

INSERT INTO `competition_events` (`id`, `edition_id`, `event_type`, `name`, `scheduled_start_at`, `scheduled_end_at`, `status`)
SELECT `id` + 1000, 105, `event_type`, `name`, DATE_SUB(`scheduled_start_at`, INTERVAL 1 YEAR), DATE_SUB(`scheduled_end_at`, INTERVAL 1 YEAR), 'Completed'
FROM `competition_events`
WHERE `edition_id` = 5;

INSERT INTO `matches` (`id`, `edition_id`, `group_id`, `court_id`, `name`, `phase`, `round`, `scheduled_start_at`, `scheduled_end_at`, `actual_start_at`, `actual_end_at`, `status`, `period_count`, `period_duration_ms`, `break_duration_ms`, `overtime_duration_ms`, `shot_clock_ms`, `timeout_duration_ms`, `timeouts_per_team`, `timeouts_per_period`, `personal_foul_limit`, `team_foul_bonus_threshold`, `max_score`, `max_score_enabled`, `stop_clock_on_free_throws`, `winner_team_id`, `win_reason`, `notes`, `created_at`, `updated_at`)
SELECT
  `id` + 1000,
  105,
  CASE WHEN `group_id` IS NULL THEN NULL ELSE `group_id` + 1000 END,
  CASE WHEN `court_id` IS NULL THEN NULL ELSE `court_id` + 1000 END,
  `name`,
  `phase`,
  `round`,
  DATE_SUB(`scheduled_start_at`, INTERVAL 1 YEAR),
  DATE_SUB(`scheduled_end_at`, INTERVAL 1 YEAR),
  DATE_SUB(`actual_start_at`, INTERVAL 1 YEAR),
  DATE_SUB(`actual_end_at`, INTERVAL 1 YEAR),
  'Finished',
  `period_count`,
  `period_duration_ms`,
  `break_duration_ms`,
  `overtime_duration_ms`,
  `shot_clock_ms`,
  `timeout_duration_ms`,
  `timeouts_per_team`,
  `timeouts_per_period`,
  `personal_foul_limit`,
  `team_foul_bonus_threshold`,
  `max_score`,
  `max_score_enabled`,
  `stop_clock_on_free_throws`,
  CASE WHEN `winner_team_id` IS NULL THEN NULL ELSE `winner_team_id` + 1000 END,
  COALESCE(`win_reason`, 'Regular'),
  `notes`,
  NOW(),
  NOW()
FROM `matches`
WHERE `edition_id` = 5;

INSERT INTO `match_teams` (`id`, `match_id`, `team_id`, `side`, `score`, `fouls_current_period`, `timeouts_used_total`, `timeouts_used_period`, `is_winner`, `forfeit_score`)
SELECT `id` + 1000, `match_id` + 1000, `team_id` + 1000, `side`, `score`, `fouls_current_period`, `timeouts_used_total`, `timeouts_used_period`, `is_winner`, `forfeit_score`
FROM `match_teams`
WHERE `match_id` IN (SELECT `id` FROM `matches` WHERE `edition_id` = 5);

INSERT INTO `standings` (`id`, `group_id`, `team_id`, `played`, `wins`, `losses`, `points_for`, `points_against`, `point_difference`, `ranking_points`, `position`, `tie_break_note`)
SELECT `id` + 1000, `group_id` + 1000, `team_id` + 1000, `played`, `wins`, `losses`, `points_for`, `points_against`, `point_difference`, `ranking_points`, `position`, `tie_break_note`
FROM `standings`
WHERE `team_id` IN (SELECT `id` FROM `teams` WHERE `edition_id` = 5);

INSERT INTO `match_player_stats` (`id`, `match_id`, `team_id`, `player_id`, `jersey_number`, `is_starter`, `did_not_play`, `minutes_seconds`, `fg_made`, `fg_att`, `two_made`, `two_att`, `three_made`, `three_att`, `ft_made`, `ft_att`, `reb_off`, `reb_def`, `assists`, `turnovers`, `steals`, `blocks`, `fouls_committed`, `fouls_drawn`, `plus_minus`, `evaluation`, `points`, `created_at`, `updated_at`)
SELECT `id` + 1000, `match_id` + 1000, `team_id` + 1000, `player_id`, `jersey_number`, `is_starter`, `did_not_play`, `minutes_seconds`, `fg_made`, `fg_att`, `two_made`, `two_att`, `three_made`, `three_att`, `ft_made`, `ft_att`, `reb_off`, `reb_def`, `assists`, `turnovers`, `steals`, `blocks`, `fouls_committed`, `fouls_drawn`, `plus_minus`, `evaluation`, `points`, NOW(), NOW()
FROM `match_player_stats`
WHERE `match_id` IN (SELECT `id` FROM `matches` WHERE `edition_id` = 5);

INSERT INTO `match_team_stats` (`id`, `match_id`, `team_id`, `fg_made`, `fg_att`, `two_made`, `two_att`, `three_made`, `three_att`, `ft_made`, `ft_att`, `reb_off`, `reb_def`, `assists`, `turnovers`, `steals`, `blocks`, `fouls_committed`, `fouls_drawn`, `points`, `points_in_paint`, `fast_break_points`, `fast_break_points_off_turnovers`, `second_chance_points`, `points_off_turnovers`, `bench_points`, `biggest_lead`, `biggest_run`, `lead_changes`, `times_tied`, `time_in_lead`, `points_per_possession`, `created_at`, `updated_at`)
SELECT `id` + 1000, `match_id` + 1000, `team_id` + 1000, `fg_made`, `fg_att`, `two_made`, `two_att`, `three_made`, `three_att`, `ft_made`, `ft_att`, `reb_off`, `reb_def`, `assists`, `turnovers`, `steals`, `blocks`, `fouls_committed`, `fouls_drawn`, `points`, `points_in_paint`, `fast_break_points`, `fast_break_points_off_turnovers`, `second_chance_points`, `points_off_turnovers`, `bench_points`, `biggest_lead`, `biggest_run`, `lead_changes`, `times_tied`, `time_in_lead`, `points_per_possession`, NOW(), NOW()
FROM `match_team_stats`
WHERE `match_id` IN (SELECT `id` FROM `matches` WHERE `edition_id` = 5);

INSERT INTO `match_players` (`id`, `match_id`, `team_id`, `player_id`, `jersey_number`, `is_starting_five`, `is_on_court`, `points`, `personal_fouls`, `is_fouled_out`, `is_ejected`)
SELECT `id` + 1000, `match_id` + 1000, `team_id` + 1000, `player_id`, `jersey_number`, `is_starting_five`, 0, `points`, `personal_fouls`, `is_fouled_out`, `is_ejected`
FROM `match_players`
WHERE `match_id` IN (SELECT `id` FROM `matches` WHERE `edition_id` = 5);

INSERT INTO `match_events` (`id`, `match_id`, `period`, `period_type`, `game_clock_ms_remaining`, `shot_clock_ms_remaining`, `team_id`, `player_id`, `event_type`, `points`, `is_correction`, `reverts_event_id`, `description`, `payload_json`, `created_at`, `synced_at`)
SELECT
  `id` + 10000,
  `match_id` + 1000,
  `period`,
  `period_type`,
  `game_clock_ms_remaining`,
  `shot_clock_ms_remaining`,
  CASE WHEN `team_id` IS NULL THEN NULL ELSE `team_id` + 1000 END,
  `player_id`,
  `event_type`,
  `points`,
  `is_correction`,
  CASE WHEN `reverts_event_id` IS NULL THEN NULL ELSE `reverts_event_id` + 10000 END,
  `description`,
  `payload_json`,
  DATE_SUB(`created_at`, INTERVAL 1 YEAR),
  DATE_SUB(`synced_at`, INTERVAL 1 YEAR)
FROM `match_events`
WHERE `match_id` IN (SELECT `id` FROM `matches` WHERE `edition_id` = 5);

INSERT INTO `scoreboard_states` (`id`, `match_id`, `current_period`, `current_period_type`, `game_clock_ms_remaining`, `shot_clock_ms_remaining`, `timeout_clock_ms_remaining`, `break_clock_ms_remaining`, `is_game_clock_running`, `is_shot_clock_running`, `is_timeout_running`, `is_break_running`, `possession_team_id`, `home_score`, `away_score`, `home_fouls_current_period`, `away_fouls_current_period`, `home_timeouts_used_total`, `away_timeouts_used_total`, `last_event_id`, `last_updated_at`)
SELECT `id` + 1000, `match_id` + 1000, `current_period`, `current_period_type`, `game_clock_ms_remaining`, `shot_clock_ms_remaining`, `timeout_clock_ms_remaining`, `break_clock_ms_remaining`, 0, 0, 0, 0, CASE WHEN `possession_team_id` IS NULL THEN NULL ELSE `possession_team_id` + 1000 END, `home_score`, `away_score`, `home_fouls_current_period`, `away_fouls_current_period`, `home_timeouts_used_total`, `away_timeouts_used_total`, CASE WHEN `last_event_id` IS NULL THEN NULL ELSE `last_event_id` + 10000 END, DATE_SUB(`last_updated_at`, INTERVAL 1 YEAR)
FROM `scoreboard_states`
WHERE `match_id` IN (SELECT `id` FROM `matches` WHERE `edition_id` = 5);

INSERT INTO `three_point_contest_entries` (`id`, `competition_event_id`, `team_id`, `player_id`, `seed_order`, `total_score`, `final_position`)
SELECT `id` + 1000, `competition_event_id` + 1000, `team_id` + 1000, `player_id`, `seed_order`, `total_score`, `final_position`
FROM `three_point_contest_entries`
WHERE `competition_event_id` IN (SELECT `id` FROM `competition_events` WHERE `edition_id` = 5);

INSERT INTO `three_point_contest_rounds` (`id`, `entry_id`, `round_number`, `round_type`, `station1_score`, `station2_score`, `station3_score`, `station4_score`, `station5_score`, `total_score`, `notes`)
SELECT `id` + 1000, `entry_id` + 1000, `round_number`, `round_type`, `station1_score`, `station2_score`, `station3_score`, `station4_score`, `station5_score`, `total_score`, `notes`
FROM `three_point_contest_rounds`
WHERE `entry_id` IN (
  SELECT `id`
  FROM `three_point_contest_entries`
  WHERE `competition_event_id` IN (SELECT `id` FROM `competition_events` WHERE `edition_id` = 5)
);

INSERT INTO `three_point_contest_shots` (`id`, `round_id`, `station_number`, `ball_number`, `point_value`, `result`)
SELECT `id` + 10000, `round_id` + 1000, `station_number`, `ball_number`, `point_value`, `result`
FROM `three_point_contest_shots`
WHERE `round_id` IN (
  SELECT `id`
  FROM `three_point_contest_rounds`
  WHERE `entry_id` IN (
    SELECT `id`
    FROM `three_point_contest_entries`
    WHERE `competition_event_id` IN (SELECT `id` FROM `competition_events` WHERE `edition_id` = 5)
  )
);

ALTER TABLE `competition_events` AUTO_INCREMENT = 2000;
ALTER TABLE `courts` AUTO_INCREMENT = 2000;
ALTER TABLE `editions` AUTO_INCREMENT = 200;
ALTER TABLE `group_teams` AUTO_INCREMENT = 2000;
ALTER TABLE `matches` AUTO_INCREMENT = 2000;
ALTER TABLE `match_events` AUTO_INCREMENT = 20000;
ALTER TABLE `match_players` AUTO_INCREMENT = 2000;
ALTER TABLE `match_player_stats` AUTO_INCREMENT = 2000;
ALTER TABLE `match_teams` AUTO_INCREMENT = 2000;
ALTER TABLE `match_team_stats` AUTO_INCREMENT = 2000;
ALTER TABLE `scoreboard_states` AUTO_INCREMENT = 2000;
ALTER TABLE `standings` AUTO_INCREMENT = 2000;
ALTER TABLE `teams` AUTO_INCREMENT = 2000;
ALTER TABLE `team_rosters` AUTO_INCREMENT = 2000;
ALTER TABLE `three_point_contest_entries` AUTO_INCREMENT = 2000;
ALTER TABLE `three_point_contest_rounds` AUTO_INCREMENT = 2000;
ALTER TABLE `three_point_contest_shots` AUTO_INCREMENT = 20000;
ALTER TABLE `tournament_groups` AUTO_INCREMENT = 2000;

SET FOREIGN_KEY_CHECKS = 1;
