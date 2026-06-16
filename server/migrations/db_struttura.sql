-- phpMyAdmin SQL Dump
-- version 5.2.3
-- https://www.phpmyadmin.net/
--
-- Host: 31.11.39.155
-- Creato il: Giu 16, 2026 alle 14:22
-- Versione del server: 8.0.44-35
-- Versione PHP: 8.0.7

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `Sql1938817_1`
--

-- --------------------------------------------------------

--
-- Struttura della tabella `competition_events`
--

CREATE TABLE `competition_events` (
  `id` int UNSIGNED NOT NULL,
  `edition_id` int UNSIGNED NOT NULL,
  `event_type` enum('ThreePointContest','AwardCeremony','Other') COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(160) COLLATE utf8mb4_unicode_ci NOT NULL,
  `scheduled_start_at` datetime DEFAULT NULL,
  `scheduled_end_at` datetime DEFAULT NULL,
  `status` enum('Scheduled','Live','Completed','Cancelled') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Scheduled'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `courts`
--

CREATE TABLE `courts` (
  `id` int UNSIGNED NOT NULL,
  `edition_id` int UNSIGNED NOT NULL,
  `name` varchar(160) COLLATE utf8mb4_unicode_ci NOT NULL,
  `location` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `editions`
--

CREATE TABLE `editions` (
  `id` int UNSIGNED NOT NULL,
  `tournament_id` int UNSIGNED NOT NULL,
  `name` varchar(160) COLLATE utf8mb4_unicode_ci NOT NULL,
  `year` int NOT NULL,
  `start_date` date DEFAULT NULL,
  `end_date` date DEFAULT NULL,
  `status` enum('Draft','Active','Completed','Cancelled') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Draft',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `forfeit_results`
--

CREATE TABLE `forfeit_results` (
  `id` int UNSIGNED NOT NULL,
  `match_id` int UNSIGNED NOT NULL,
  `winning_team_id` int UNSIGNED DEFAULT NULL,
  `losing_team_id` int UNSIGNED DEFAULT NULL,
  `home_assigned_score` int NOT NULL DEFAULT '20',
  `away_assigned_score` int NOT NULL DEFAULT '0',
  `reason` enum('NotEnoughPlayers','Abandonment','Disqualification','WeatherDecision','OrganizerDecision') COLLATE utf8mb4_unicode_ci NOT NULL,
  `notes` text COLLATE utf8mb4_unicode_ci,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `group_teams`
--

CREATE TABLE `group_teams` (
  `id` int UNSIGNED NOT NULL,
  `group_id` int UNSIGNED NOT NULL,
  `team_id` int UNSIGNED NOT NULL,
  `seed_label` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `matches`
--

CREATE TABLE `matches` (
  `id` int UNSIGNED NOT NULL,
  `edition_id` int UNSIGNED NOT NULL,
  `group_id` int UNSIGNED DEFAULT NULL,
  `court_id` int UNSIGNED DEFAULT NULL,
  `name` varchar(160) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `phase` enum('GroupStage','SemiFinal','ThirdPlaceFinal','Final') COLLATE utf8mb4_unicode_ci NOT NULL,
  `round` varchar(80) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `scheduled_start_at` datetime DEFAULT NULL,
  `scheduled_end_at` datetime DEFAULT NULL,
  `actual_start_at` datetime DEFAULT NULL,
  `actual_end_at` datetime DEFAULT NULL,
  `status` enum('Scheduled','Ready','Live','Paused','Finished','Cancelled') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Scheduled',
  `period_count` int NOT NULL DEFAULT '2',
  `period_duration_ms` int NOT NULL DEFAULT '720000',
  `break_duration_ms` int NOT NULL DEFAULT '120000',
  `overtime_duration_ms` int NOT NULL DEFAULT '120000',
  `shot_clock_ms` int NOT NULL DEFAULT '24000',
  `timeout_duration_ms` int NOT NULL DEFAULT '30000',
  `timeouts_per_team` int NOT NULL DEFAULT '2',
  `timeouts_per_period` int NOT NULL DEFAULT '1',
  `personal_foul_limit` int NOT NULL DEFAULT '5',
  `team_foul_bonus_threshold` int NOT NULL DEFAULT '5',
  `max_score` int DEFAULT NULL,
  `max_score_enabled` tinyint(1) NOT NULL DEFAULT '0',
  `stop_clock_on_free_throws` tinyint(1) NOT NULL DEFAULT '1',
  `winner_team_id` int UNSIGNED DEFAULT NULL,
  `win_reason` enum('Regular','MaxScoreReached','Overtime','SuddenDeathFreeThrow','Forfeit','Disqualification','Abandoned') COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `notes` text COLLATE utf8mb4_unicode_ci,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `match_events`
--

CREATE TABLE `match_events` (
  `id` int UNSIGNED NOT NULL,
  `match_id` int UNSIGNED NOT NULL,
  `period` int NOT NULL,
  `period_type` enum('Regular','Overtime','SuddenDeath') COLLATE utf8mb4_unicode_ci NOT NULL,
  `game_clock_ms_remaining` int NOT NULL,
  `shot_clock_ms_remaining` int DEFAULT NULL,
  `team_id` int UNSIGNED DEFAULT NULL,
  `player_id` int UNSIGNED DEFAULT NULL,
  `event_type` varchar(80) COLLATE utf8mb4_unicode_ci NOT NULL,
  `points` int DEFAULT NULL,
  `is_correction` tinyint(1) NOT NULL DEFAULT '0',
  `reverts_event_id` int UNSIGNED DEFAULT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `payload_json` json DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `synced_at` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `match_players`
--

CREATE TABLE `match_players` (
  `id` int UNSIGNED NOT NULL,
  `match_id` int UNSIGNED NOT NULL,
  `team_id` int UNSIGNED NOT NULL,
  `player_id` int UNSIGNED NOT NULL,
  `jersey_number` int DEFAULT NULL,
  `is_starting_five` tinyint(1) NOT NULL DEFAULT '0',
  `is_on_court` tinyint(1) NOT NULL DEFAULT '0',
  `points` int NOT NULL DEFAULT '0',
  `personal_fouls` int NOT NULL DEFAULT '0',
  `is_fouled_out` tinyint(1) NOT NULL DEFAULT '0',
  `is_ejected` tinyint(1) NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `match_player_stats`
--

CREATE TABLE `match_player_stats` (
  `id` int UNSIGNED NOT NULL,
  `match_id` int UNSIGNED NOT NULL,
  `team_id` int UNSIGNED NOT NULL,
  `player_id` int UNSIGNED NOT NULL,
  `jersey_number` int DEFAULT NULL,
  `is_starter` tinyint(1) NOT NULL DEFAULT '0',
  `did_not_play` tinyint(1) NOT NULL DEFAULT '0',
  `minutes_seconds` int DEFAULT NULL,
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
  `plus_minus` int DEFAULT NULL,
  `evaluation` int DEFAULT NULL,
  `points` int DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `match_teams`
--

CREATE TABLE `match_teams` (
  `id` int UNSIGNED NOT NULL,
  `match_id` int UNSIGNED NOT NULL,
  `team_id` int UNSIGNED NOT NULL,
  `side` enum('Home','Away') COLLATE utf8mb4_unicode_ci NOT NULL,
  `score` int NOT NULL DEFAULT '0',
  `fouls_current_period` int NOT NULL DEFAULT '0',
  `timeouts_used_total` int NOT NULL DEFAULT '0',
  `timeouts_used_period` int NOT NULL DEFAULT '0',
  `is_winner` tinyint(1) NOT NULL DEFAULT '0',
  `forfeit_score` int DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `match_team_stats`
--

CREATE TABLE `match_team_stats` (
  `id` int UNSIGNED NOT NULL,
  `match_id` int UNSIGNED NOT NULL,
  `team_id` int UNSIGNED NOT NULL,
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
  `points_in_paint` int DEFAULT NULL,
  `fast_break_points` int DEFAULT NULL,
  `fast_break_points_off_turnovers` int DEFAULT NULL,
  `second_chance_points` int DEFAULT NULL,
  `points_off_turnovers` int DEFAULT NULL,
  `bench_points` int DEFAULT NULL,
  `biggest_lead` int DEFAULT NULL,
  `biggest_run` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `lead_changes` int DEFAULT NULL,
  `times_tied` int DEFAULT NULL,
  `time_in_lead` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `points_per_possession` decimal(8,3) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `players`
--

CREATE TABLE `players` (
  `id` int UNSIGNED NOT NULL,
  `first_name` varchar(120) COLLATE utf8mb4_unicode_ci NOT NULL,
  `last_name` varchar(120) COLLATE utf8mb4_unicode_ci NOT NULL,
  `nickname` varchar(120) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `fiscal_code` varchar(32) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `address` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `phone_number` varchar(40) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `email` varchar(160) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `birth_date` date DEFAULT NULL,
  `photo_path` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `scoreboard_states`
--

CREATE TABLE `scoreboard_states` (
  `id` int UNSIGNED NOT NULL,
  `match_id` int UNSIGNED NOT NULL,
  `current_period` int NOT NULL DEFAULT '1',
  `current_period_type` enum('Regular','Overtime','SuddenDeath') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Regular',
  `game_clock_ms_remaining` int NOT NULL DEFAULT '720000',
  `shot_clock_ms_remaining` int NOT NULL DEFAULT '24000',
  `timeout_clock_ms_remaining` int DEFAULT NULL,
  `break_clock_ms_remaining` int DEFAULT NULL,
  `is_game_clock_running` tinyint(1) NOT NULL DEFAULT '0',
  `is_shot_clock_running` tinyint(1) NOT NULL DEFAULT '0',
  `is_timeout_running` tinyint(1) NOT NULL DEFAULT '0',
  `is_break_running` tinyint(1) NOT NULL DEFAULT '0',
  `possession_team_id` int UNSIGNED DEFAULT NULL,
  `home_score` int NOT NULL DEFAULT '0',
  `away_score` int NOT NULL DEFAULT '0',
  `home_fouls_current_period` int NOT NULL DEFAULT '0',
  `away_fouls_current_period` int NOT NULL DEFAULT '0',
  `home_timeouts_used_total` int NOT NULL DEFAULT '0',
  `away_timeouts_used_total` int NOT NULL DEFAULT '0',
  `last_event_id` int UNSIGNED DEFAULT NULL,
  `last_updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `sponsors`
--

CREATE TABLE `sponsors` (
  `id` int UNSIGNED NOT NULL,
  `name` varchar(160) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `image_path` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `sort_order` int NOT NULL DEFAULT '0',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `staff`
--

CREATE TABLE `staff` (
  `id` int NOT NULL,
  `nome` varchar(100) NOT NULL,
  `ruolo` varchar(100) NOT NULL,
  `categoria` varchar(50) NOT NULL DEFAULT 'collaboratori',
  `bio` varchar(500) NOT NULL DEFAULT '',
  `foto` varchar(500) NOT NULL DEFAULT '',
  `ig` varchar(100) NOT NULL DEFAULT ''
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `standings`
--

CREATE TABLE `standings` (
  `id` int UNSIGNED NOT NULL,
  `group_id` int UNSIGNED NOT NULL,
  `team_id` int UNSIGNED NOT NULL,
  `played` int NOT NULL DEFAULT '0',
  `wins` int NOT NULL DEFAULT '0',
  `losses` int NOT NULL DEFAULT '0',
  `points_for` int NOT NULL DEFAULT '0',
  `points_against` int NOT NULL DEFAULT '0',
  `point_difference` int NOT NULL DEFAULT '0',
  `ranking_points` int NOT NULL DEFAULT '0',
  `position` int DEFAULT NULL,
  `tie_break_note` text COLLATE utf8mb4_unicode_ci
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `teams`
--

CREATE TABLE `teams` (
  `id` int UNSIGNED NOT NULL,
  `edition_id` int UNSIGNED NOT NULL,
  `name` varchar(160) COLLATE utf8mb4_unicode_ci NOT NULL,
  `short_name` varchar(40) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `primary_color` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `secondary_color` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `logo_path` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `team_rosters`
--

CREATE TABLE `team_rosters` (
  `id` int UNSIGNED NOT NULL,
  `team_id` int UNSIGNED NOT NULL,
  `player_id` int UNSIGNED NOT NULL,
  `jersey_number` int DEFAULT NULL,
  `role` varchar(80) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `is_captain` tinyint(1) NOT NULL DEFAULT '0',
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `three_point_contest_entries`
--

CREATE TABLE `three_point_contest_entries` (
  `id` int UNSIGNED NOT NULL,
  `competition_event_id` int UNSIGNED NOT NULL,
  `team_id` int UNSIGNED NOT NULL,
  `player_id` int UNSIGNED NOT NULL,
  `seed_order` int DEFAULT NULL,
  `total_score` int NOT NULL DEFAULT '0',
  `final_position` int DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `three_point_contest_rounds`
--

CREATE TABLE `three_point_contest_rounds` (
  `id` int UNSIGNED NOT NULL,
  `entry_id` int UNSIGNED NOT NULL,
  `round_number` int NOT NULL,
  `round_type` enum('Qualification','Final','TieBreak') COLLATE utf8mb4_unicode_ci NOT NULL,
  `station1_score` int NOT NULL DEFAULT '0',
  `station2_score` int NOT NULL DEFAULT '0',
  `station3_score` int NOT NULL DEFAULT '0',
  `station4_score` int NOT NULL DEFAULT '0',
  `station5_score` int NOT NULL DEFAULT '0',
  `total_score` int NOT NULL DEFAULT '0',
  `notes` text COLLATE utf8mb4_unicode_ci
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `tournaments`
--

CREATE TABLE `tournaments` (
  `id` int UNSIGNED NOT NULL,
  `name` varchar(160) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Struttura della tabella `tournament_groups`
--

CREATE TABLE `tournament_groups` (
  `id` int UNSIGNED NOT NULL,
  `edition_id` int UNSIGNED NOT NULL,
  `name` varchar(160) COLLATE utf8mb4_unicode_ci NOT NULL,
  `code` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `sort_order` int NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Indici per le tabelle scaricate
--

--
-- Indici per le tabelle `competition_events`
--
ALTER TABLE `competition_events`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_competition_events_edition_id` (`edition_id`);

--
-- Indici per le tabelle `courts`
--
ALTER TABLE `courts`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_courts_edition_id` (`edition_id`);

--
-- Indici per le tabelle `editions`
--
ALTER TABLE `editions`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_editions_tournament_id` (`tournament_id`);

--
-- Indici per le tabelle `forfeit_results`
--
ALTER TABLE `forfeit_results`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_forfeit_results_match` (`match_id`),
  ADD KEY `ix_forfeit_results_winning_team_id` (`winning_team_id`),
  ADD KEY `ix_forfeit_results_losing_team_id` (`losing_team_id`);

--
-- Indici per le tabelle `group_teams`
--
ALTER TABLE `group_teams`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_group_teams_group_team` (`group_id`,`team_id`),
  ADD UNIQUE KEY `ux_group_teams_group_seed` (`group_id`,`seed_label`),
  ADD KEY `ix_group_teams_team_id` (`team_id`);

--
-- Indici per le tabelle `matches`
--
ALTER TABLE `matches`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_matches_edition_id` (`edition_id`),
  ADD KEY `ix_matches_group_id` (`group_id`),
  ADD KEY `ix_matches_phase_status` (`phase`,`status`),
  ADD KEY `ix_matches_court_id` (`court_id`),
  ADD KEY `ix_matches_winner_team_id` (`winner_team_id`);

--
-- Indici per le tabelle `match_events`
--
ALTER TABLE `match_events`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_match_events_match_created` (`match_id`,`created_at`),
  ADD KEY `ix_match_events_team_id` (`team_id`),
  ADD KEY `ix_match_events_player_id` (`player_id`),
  ADD KEY `ix_match_events_type` (`event_type`),
  ADD KEY `fk_match_events_reverts_event` (`reverts_event_id`);

--
-- Indici per le tabelle `match_players`
--
ALTER TABLE `match_players`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_match_players_match_player` (`match_id`,`player_id`),
  ADD UNIQUE KEY `ux_match_players_match_team_number` (`match_id`,`team_id`,`jersey_number`),
  ADD KEY `ix_match_players_team_id` (`team_id`),
  ADD KEY `ix_match_players_player_id` (`player_id`);

--
-- Indici per le tabelle `match_player_stats`
--
ALTER TABLE `match_player_stats`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_match_player_stats_match_player` (`match_id`,`player_id`),
  ADD KEY `ix_match_player_stats_team_id` (`team_id`),
  ADD KEY `ix_match_player_stats_player_id` (`player_id`);

--
-- Indici per le tabelle `match_teams`
--
ALTER TABLE `match_teams`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_match_teams_match_team` (`match_id`,`team_id`),
  ADD UNIQUE KEY `ux_match_teams_match_side` (`match_id`,`side`),
  ADD KEY `ix_match_teams_team_id` (`team_id`);

--
-- Indici per le tabelle `match_team_stats`
--
ALTER TABLE `match_team_stats`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_match_team_stats_match_team` (`match_id`,`team_id`),
  ADD KEY `ix_match_team_stats_team_id` (`team_id`);

--
-- Indici per le tabelle `players`
--
ALTER TABLE `players`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_players_name` (`last_name`,`first_name`),
  ADD KEY `ix_players_email` (`email`);

--
-- Indici per le tabelle `scoreboard_states`
--
ALTER TABLE `scoreboard_states`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_scoreboard_states_match` (`match_id`),
  ADD KEY `ix_scoreboard_states_last_updated` (`last_updated_at`),
  ADD KEY `ix_scoreboard_states_possession_team_id` (`possession_team_id`);

--
-- Indici per le tabelle `sponsors`
--
ALTER TABLE `sponsors`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_sponsors_active_sort` (`is_active`,`sort_order`);

--
-- Indici per le tabelle `staff`
--
ALTER TABLE `staff`
  ADD PRIMARY KEY (`id`);

--
-- Indici per le tabelle `standings`
--
ALTER TABLE `standings`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_standings_group_team` (`group_id`,`team_id`),
  ADD KEY `ix_standings_team_id` (`team_id`);

--
-- Indici per le tabelle `teams`
--
ALTER TABLE `teams`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_teams_edition_name` (`edition_id`,`name`),
  ADD KEY `ix_teams_edition_id` (`edition_id`);

--
-- Indici per le tabelle `team_rosters`
--
ALTER TABLE `team_rosters`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_team_rosters_team_player` (`team_id`,`player_id`),
  ADD UNIQUE KEY `ux_team_rosters_team_number` (`team_id`,`jersey_number`),
  ADD KEY `ix_team_rosters_player_id` (`player_id`);

--
-- Indici per le tabelle `three_point_contest_entries`
--
ALTER TABLE `three_point_contest_entries`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_three_point_entries_event_team` (`competition_event_id`,`team_id`),
  ADD UNIQUE KEY `ux_three_point_entries_event_player` (`competition_event_id`,`player_id`),
  ADD KEY `ix_three_point_entries_team_id` (`team_id`),
  ADD KEY `ix_three_point_entries_player_id` (`player_id`);

--
-- Indici per le tabelle `three_point_contest_rounds`
--
ALTER TABLE `three_point_contest_rounds`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_three_point_rounds_entry_round_type` (`entry_id`,`round_number`,`round_type`);

--
-- Indici per le tabelle `tournaments`
--
ALTER TABLE `tournaments`
  ADD PRIMARY KEY (`id`);

--
-- Indici per le tabelle `tournament_groups`
--
ALTER TABLE `tournament_groups`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_tournament_groups_edition_code` (`edition_id`,`code`),
  ADD KEY `ix_tournament_groups_edition_id` (`edition_id`);

--
-- AUTO_INCREMENT per le tabelle scaricate
--

--
-- AUTO_INCREMENT per la tabella `competition_events`
--
ALTER TABLE `competition_events`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `courts`
--
ALTER TABLE `courts`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `editions`
--
ALTER TABLE `editions`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `forfeit_results`
--
ALTER TABLE `forfeit_results`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `group_teams`
--
ALTER TABLE `group_teams`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `matches`
--
ALTER TABLE `matches`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `match_events`
--
ALTER TABLE `match_events`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `match_players`
--
ALTER TABLE `match_players`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `match_player_stats`
--
ALTER TABLE `match_player_stats`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `match_teams`
--
ALTER TABLE `match_teams`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `match_team_stats`
--
ALTER TABLE `match_team_stats`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `players`
--
ALTER TABLE `players`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `scoreboard_states`
--
ALTER TABLE `scoreboard_states`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `sponsors`
--
ALTER TABLE `sponsors`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `staff`
--
ALTER TABLE `staff`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `standings`
--
ALTER TABLE `standings`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `teams`
--
ALTER TABLE `teams`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `team_rosters`
--
ALTER TABLE `team_rosters`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `three_point_contest_entries`
--
ALTER TABLE `three_point_contest_entries`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `three_point_contest_rounds`
--
ALTER TABLE `three_point_contest_rounds`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `tournaments`
--
ALTER TABLE `tournaments`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `tournament_groups`
--
ALTER TABLE `tournament_groups`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- Limiti per le tabelle scaricate
--

--
-- Limiti per la tabella `competition_events`
--
ALTER TABLE `competition_events`
  ADD CONSTRAINT `fk_competition_events_edition` FOREIGN KEY (`edition_id`) REFERENCES `editions` (`id`) ON DELETE CASCADE;

--
-- Limiti per la tabella `courts`
--
ALTER TABLE `courts`
  ADD CONSTRAINT `fk_courts_edition` FOREIGN KEY (`edition_id`) REFERENCES `editions` (`id`) ON DELETE CASCADE;

--
-- Limiti per la tabella `editions`
--
ALTER TABLE `editions`
  ADD CONSTRAINT `fk_editions_tournament` FOREIGN KEY (`tournament_id`) REFERENCES `tournaments` (`id`) ON DELETE CASCADE;

--
-- Limiti per la tabella `forfeit_results`
--
ALTER TABLE `forfeit_results`
  ADD CONSTRAINT `fk_forfeit_results_losing_team` FOREIGN KEY (`losing_team_id`) REFERENCES `teams` (`id`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_forfeit_results_match` FOREIGN KEY (`match_id`) REFERENCES `matches` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_forfeit_results_winning_team` FOREIGN KEY (`winning_team_id`) REFERENCES `teams` (`id`) ON DELETE SET NULL;

--
-- Limiti per la tabella `group_teams`
--
ALTER TABLE `group_teams`
  ADD CONSTRAINT `fk_group_teams_group` FOREIGN KEY (`group_id`) REFERENCES `tournament_groups` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_group_teams_team` FOREIGN KEY (`team_id`) REFERENCES `teams` (`id`) ON DELETE CASCADE;

--
-- Limiti per la tabella `matches`
--
ALTER TABLE `matches`
  ADD CONSTRAINT `fk_matches_court` FOREIGN KEY (`court_id`) REFERENCES `courts` (`id`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_matches_edition` FOREIGN KEY (`edition_id`) REFERENCES `editions` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_matches_group` FOREIGN KEY (`group_id`) REFERENCES `tournament_groups` (`id`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_matches_winner_team` FOREIGN KEY (`winner_team_id`) REFERENCES `teams` (`id`) ON DELETE SET NULL;

--
-- Limiti per la tabella `match_events`
--
ALTER TABLE `match_events`
  ADD CONSTRAINT `fk_match_events_match` FOREIGN KEY (`match_id`) REFERENCES `matches` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_match_events_player` FOREIGN KEY (`player_id`) REFERENCES `players` (`id`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_match_events_reverts_event` FOREIGN KEY (`reverts_event_id`) REFERENCES `match_events` (`id`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_match_events_team` FOREIGN KEY (`team_id`) REFERENCES `teams` (`id`) ON DELETE SET NULL;

--
-- Limiti per la tabella `match_players`
--
ALTER TABLE `match_players`
  ADD CONSTRAINT `fk_match_players_match` FOREIGN KEY (`match_id`) REFERENCES `matches` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_match_players_player` FOREIGN KEY (`player_id`) REFERENCES `players` (`id`) ON DELETE RESTRICT,
  ADD CONSTRAINT `fk_match_players_team` FOREIGN KEY (`team_id`) REFERENCES `teams` (`id`) ON DELETE RESTRICT;

--
-- Limiti per la tabella `match_player_stats`
--
ALTER TABLE `match_player_stats`
  ADD CONSTRAINT `fk_match_player_stats_match` FOREIGN KEY (`match_id`) REFERENCES `matches` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_match_player_stats_player` FOREIGN KEY (`player_id`) REFERENCES `players` (`id`) ON DELETE RESTRICT,
  ADD CONSTRAINT `fk_match_player_stats_team` FOREIGN KEY (`team_id`) REFERENCES `teams` (`id`) ON DELETE RESTRICT;

--
-- Limiti per la tabella `match_teams`
--
ALTER TABLE `match_teams`
  ADD CONSTRAINT `fk_match_teams_match` FOREIGN KEY (`match_id`) REFERENCES `matches` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_match_teams_team` FOREIGN KEY (`team_id`) REFERENCES `teams` (`id`) ON DELETE RESTRICT;

--
-- Limiti per la tabella `match_team_stats`
--
ALTER TABLE `match_team_stats`
  ADD CONSTRAINT `fk_match_team_stats_match` FOREIGN KEY (`match_id`) REFERENCES `matches` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_match_team_stats_team` FOREIGN KEY (`team_id`) REFERENCES `teams` (`id`) ON DELETE RESTRICT;

--
-- Limiti per la tabella `scoreboard_states`
--
ALTER TABLE `scoreboard_states`
  ADD CONSTRAINT `fk_scoreboard_states_match` FOREIGN KEY (`match_id`) REFERENCES `matches` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_scoreboard_states_possession_team` FOREIGN KEY (`possession_team_id`) REFERENCES `teams` (`id`) ON DELETE SET NULL;

--
-- Limiti per la tabella `standings`
--
ALTER TABLE `standings`
  ADD CONSTRAINT `fk_standings_group` FOREIGN KEY (`group_id`) REFERENCES `tournament_groups` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_standings_team` FOREIGN KEY (`team_id`) REFERENCES `teams` (`id`) ON DELETE CASCADE;

--
-- Limiti per la tabella `teams`
--
ALTER TABLE `teams`
  ADD CONSTRAINT `fk_teams_edition` FOREIGN KEY (`edition_id`) REFERENCES `editions` (`id`) ON DELETE CASCADE;

--
-- Limiti per la tabella `team_rosters`
--
ALTER TABLE `team_rosters`
  ADD CONSTRAINT `fk_team_rosters_player` FOREIGN KEY (`player_id`) REFERENCES `players` (`id`) ON DELETE RESTRICT,
  ADD CONSTRAINT `fk_team_rosters_team` FOREIGN KEY (`team_id`) REFERENCES `teams` (`id`) ON DELETE CASCADE;

--
-- Limiti per la tabella `three_point_contest_entries`
--
ALTER TABLE `three_point_contest_entries`
  ADD CONSTRAINT `fk_three_point_entries_event` FOREIGN KEY (`competition_event_id`) REFERENCES `competition_events` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_three_point_entries_player` FOREIGN KEY (`player_id`) REFERENCES `players` (`id`) ON DELETE RESTRICT,
  ADD CONSTRAINT `fk_three_point_entries_team` FOREIGN KEY (`team_id`) REFERENCES `teams` (`id`) ON DELETE RESTRICT;

--
-- Limiti per la tabella `three_point_contest_rounds`
--
ALTER TABLE `three_point_contest_rounds`
  ADD CONSTRAINT `fk_three_point_rounds_entry` FOREIGN KEY (`entry_id`) REFERENCES `three_point_contest_entries` (`id`) ON DELETE CASCADE;

--
-- Limiti per la tabella `tournament_groups`
--
ALTER TABLE `tournament_groups`
  ADD CONSTRAINT `fk_tournament_groups_edition` FOREIGN KEY (`edition_id`) REFERENCES `editions` (`id`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
