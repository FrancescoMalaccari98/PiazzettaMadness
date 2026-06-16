-- ATTENZIONE: elimina definitivamente tutti i dati applicativi.
-- Mantiene struttura, indici, vincoli e tabelle statistiche.
-- Eseguire solo dopo avere esportato un backup completo da phpMyAdmin.

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS free_throw_sequences;
DROP TABLE IF EXISTS match_fouls;
DROP TABLE IF EXISTS match_timeouts;
DROP TABLE IF EXISTS match_periods;
DROP TABLE IF EXISTS online_sync_log;

DELETE FROM three_point_contest_rounds;
DELETE FROM three_point_contest_entries;
DELETE FROM match_player_stats;
DELETE FROM match_team_stats;
DELETE FROM match_events;
DELETE FROM scoreboard_states;
DELETE FROM match_players;
DELETE FROM match_teams;
DELETE FROM forfeit_results;
DELETE FROM standings;
DELETE FROM group_teams;
DELETE FROM team_rosters;
DELETE FROM competition_events;
DELETE FROM matches;
DELETE FROM tournament_groups;
DELETE FROM courts;
DELETE FROM teams;
DELETE FROM players;
DELETE FROM sponsors;
DELETE FROM editions;
DELETE FROM tournaments;

ALTER TABLE three_point_contest_rounds AUTO_INCREMENT = 1;
ALTER TABLE three_point_contest_entries AUTO_INCREMENT = 1;
ALTER TABLE match_player_stats AUTO_INCREMENT = 1;
ALTER TABLE match_team_stats AUTO_INCREMENT = 1;
ALTER TABLE match_events AUTO_INCREMENT = 1;
ALTER TABLE scoreboard_states AUTO_INCREMENT = 1;
ALTER TABLE match_players AUTO_INCREMENT = 1;
ALTER TABLE match_teams AUTO_INCREMENT = 1;
ALTER TABLE forfeit_results AUTO_INCREMENT = 1;
ALTER TABLE standings AUTO_INCREMENT = 1;
ALTER TABLE group_teams AUTO_INCREMENT = 1;
ALTER TABLE team_rosters AUTO_INCREMENT = 1;
ALTER TABLE competition_events AUTO_INCREMENT = 1;
ALTER TABLE matches AUTO_INCREMENT = 1;
ALTER TABLE tournament_groups AUTO_INCREMENT = 1;
ALTER TABLE courts AUTO_INCREMENT = 1;
ALTER TABLE teams AUTO_INCREMENT = 1;
ALTER TABLE players AUTO_INCREMENT = 1;
ALTER TABLE sponsors AUTO_INCREMENT = 1;
ALTER TABLE editions AUTO_INCREMENT = 1;
ALTER TABLE tournaments AUTO_INCREMENT = 1;

SET FOREIGN_KEY_CHECKS = 1;
