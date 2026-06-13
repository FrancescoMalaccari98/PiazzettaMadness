-- phpMyAdmin SQL Dump
-- version 5.2.3
-- https://www.phpmyadmin.net/
--
-- Host: 31.11.39.155
-- Creato il: Giu 08, 2026 alle 10:07
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

--
-- Dump dei dati per la tabella `competition_events`
--

INSERT INTO `competition_events` (`id`, `edition_id`, `event_type`, `name`, `scheduled_start_at`, `scheduled_end_at`, `status`) VALUES
(2, 2, 'ThreePointContest', '3 Point Contest', NULL, NULL, 'Scheduled'),
(3, 5, 'ThreePointContest', '3 Point Contest', NULL, NULL, 'Scheduled'),
(4, 2, 'ThreePointContest', 'Lorenzo Test', '2026-06-03 19:30:00', '2026-06-03 20:00:00', 'Scheduled'),
(5, 6, 'ThreePointContest', 'Gara da Tre - Qualificazioni', '2026-07-04 17:15:00', '2026-07-04 18:05:00', 'Completed'),
(6, 6, 'AwardCeremony', 'Premiazione MVP', '2026-07-04 21:30:00', '2026-07-04 22:00:00', 'Completed'),
(7, 6, 'Other', 'Skills Challenge Under 18', '2026-07-05 16:45:00', '2026-07-05 17:35:00', 'Scheduled'),
(8, 6, 'Other', 'Clinic Minibasket', '2026-07-05 18:20:00', '2026-07-05 19:10:00', 'Scheduled'),
(9, 6, 'AwardCeremony', 'Cerimonia Fair Play', '2026-07-06 20:10:00', '2026-07-06 20:35:00', 'Scheduled'),
(10, 6, 'Other', 'Closing Party', '2026-07-06 21:15:00', '2026-07-06 22:30:00', 'Scheduled'),
(19, 14, 'ThreePointContest', 'Gara del tiro da tre punti', '2026-07-13 17:00:00', '2026-07-13 17:45:00', 'Completed'),
(20, 14, 'Other', 'Clinic minibasket con gli istruttori', '2026-07-10 16:30:00', '2026-07-10 17:15:00', 'Completed'),
(21, 14, 'Other', 'Presentazione squadre', '2026-07-10 17:20:00', '2026-07-10 17:45:00', 'Completed'),
(22, 14, 'Other', 'Sfida abilita per capitani', '2026-07-12 17:15:00', '2026-07-12 17:45:00', 'Completed'),
(23, 14, 'AwardCeremony', 'Premiazione fair play', '2026-07-14 18:55:00', '2026-07-14 19:05:00', 'Completed'),
(24, 14, 'AwardCeremony', 'Premiazione finale', '2026-07-14 20:15:00', '2026-07-14 20:45:00', 'Completed');

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

--
-- Dump dei dati per la tabella `courts`
--

INSERT INTO `courts` (`id`, `edition_id`, `name`, `location`) VALUES
(2, 2, 'Piazzetta Verde', 'Porto Potenza Picena'),
(3, 2, 'Campo Centrale', 'Piazzetta'),
(4, 2, 'test', 'test'),
(5, 2, 'TEST', 'TEST'),
(6, 5, 'Campo Centrale', 'Piazzetta'),
(7, 6, 'Campo Centrale', 'Piazza XX Settembre, Porto Recanati'),
(8, 6, 'Campo Mare', 'Lungomare Lepanto, Civitanova Marche'),
(9, 6, 'Pala Verde', 'Via San Giorgio 18, Macerata'),
(10, 6, 'Playground Nord', 'Parco della Resistenza, Ancona'),
(11, 6, 'Arena Porto', 'Molo Sud, Porto Potenza Picena'),
(12, 6, 'Campo Scuola Basket', 'Via dello Sport 7, Osimo'),
(21, 14, 'Campo Centrale', 'Piazzetta Verde, Porto Potenza Picena'),
(22, 14, 'Campo Mare', 'Lungomare Sud, Porto Potenza Picena'),
(23, 14, 'Campo Pineta', 'Area Pineta, Porto Potenza Picena'),
(24, 14, 'Campo Arena', 'Parco Europa, Porto Potenza Picena'),
(25, 14, 'Campo Scuole', 'Palestra Leopardi, Porto Potenza Picena'),
(26, 14, 'Campo Porto', 'Area sportiva porto turistico');

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

--
-- Dump dei dati per la tabella `editions`
--

INSERT INTO `editions` (`id`, `tournament_id`, `name`, `year`, `start_date`, `end_date`, `status`, `created_at`, `updated_at`) VALUES
(2, 2, 'Piazzetta Madness 2026', 2026, NULL, NULL, 'Draft', '2026-05-27 10:19:14', '2026-06-08 08:54:08'),
(3, 3, 'test', 2026, NULL, NULL, 'Draft', '2026-06-03 17:12:43', '2026-06-03 17:12:43'),
(4, 3, 'test', 2026, NULL, NULL, 'Draft', '2026-06-03 17:12:50', '2026-06-03 17:12:50'),
(5, 3, 'TEST2', 2026, NULL, NULL, 'Draft', '2026-06-03 17:17:19', '2026-06-03 17:17:19'),
(6, 4, 'Edizione Riviera Marche 2026', 2026, '2026-07-04', '2026-07-06', 'Draft', '2026-06-08 00:26:32', '2026-06-08 01:55:42'),
(7, 5, 'Edizione Ancona Centro 2026', 2026, '2026-07-11', '2026-07-13', 'Draft', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(8, 6, 'Edizione Summer Challenge 2026', 2026, '2026-07-18', '2026-07-20', 'Draft', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(9, 7, 'Edizione Memorial 2026', 2026, '2026-07-25', '2026-07-27', 'Draft', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(10, 8, 'Edizione Adriatic Cup 2026', 2026, '2026-08-01', '2026-08-03', 'Draft', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(11, 9, 'Edizione Notte Campioni 2026', 2026, '2026-08-08', '2026-08-10', 'Draft', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(14, 12, 'Memorial Carlo Venturi 2026', 2026, '2026-07-10', '2026-07-14', 'Active', '2026-06-08 01:55:15', '2026-06-08 08:54:15');

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

--
-- Dump dei dati per la tabella `forfeit_results`
--

INSERT INTO `forfeit_results` (`id`, `match_id`, `winning_team_id`, `losing_team_id`, `home_assigned_score`, `away_assigned_score`, `reason`, `notes`, `created_at`) VALUES
(1, 15, 39, 40, 58, 54, 'OrganizerDecision', 'Rettifica registrata dal tavolo dopo controllo del referto.', '2026-06-08 00:26:32'),
(2, 16, 41, 40, 41, 49, 'Disqualification', 'Esito confermato dopo verifica disciplinare.', '2026-06-08 00:26:32'),
(3, 17, 41, 42, 63, 61, 'WeatherDecision', 'Risultato convalidato dalla direzione di gara.', '2026-06-08 00:26:32'),
(4, 18, 43, 42, 47, 52, 'OrganizerDecision', 'Rettifica organizzativa registrata a fine incontro.', '2026-06-08 00:26:32'),
(5, 19, 43, 44, 55, 45, 'Abandonment', 'Esito archiviato dopo abbandono dell’avversaria.', '2026-06-08 00:26:32'),
(6, 20, 39, 44, 60, 64, 'NotEnoughPlayers', 'Esito assegnato per numero giocatori insufficiente.', '2026-06-08 00:26:32'),
(9, 44, 54, 58, 0, 20, 'NotEnoughPlayers', 'Monte Bianco non raggiunge il numero minimo di giocatori disponibili entro il termine stabilito.', '2026-07-13 19:25:00');

-- --------------------------------------------------------

--
-- Struttura della tabella `free_throw_sequences`
--

CREATE TABLE `free_throw_sequences` (
  `id` int UNSIGNED NOT NULL,
  `match_id` int UNSIGNED NOT NULL,
  `team_id` int UNSIGNED NOT NULL,
  `player_id` int UNSIGNED DEFAULT NULL,
  `period` int NOT NULL,
  `awarded_shots` int NOT NULL,
  `made_shots` int NOT NULL DEFAULT '0',
  `reason` enum('Foul','Bonus','Technical','SuddenDeath') COLLATE utf8mb4_unicode_ci NOT NULL,
  `started_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ended_at` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dump dei dati per la tabella `free_throw_sequences`
--

INSERT INTO `free_throw_sequences` (`id`, `match_id`, `team_id`, `player_id`, `period`, `awarded_shots`, `made_shots`, `reason`, `started_at`, `ended_at`) VALUES
(1, 15, 39, 64, 1, 2, 2, 'Foul', '2026-07-04 18:08:14', '2026-07-04 18:08:48'),
(2, 16, 40, 65, 1, 3, 1, 'Bonus', '2026-07-04 19:01:33', '2026-07-04 19:02:05'),
(3, 17, 41, 66, 2, 1, 1, 'Technical', '2026-07-04 20:14:02', '2026-07-04 20:14:24'),
(4, 18, 42, 67, 2, 2, 0, 'Foul', '2026-07-05 18:30:45', '2026-07-05 18:31:12'),
(5, 19, 43, 68, 1, 2, 2, 'SuddenDeath', '2026-07-05 19:11:20', '2026-07-05 19:11:49'),
(6, 20, 44, 69, 2, 1, 1, 'Technical', '2026-07-05 20:18:11', '2026-07-05 20:18:31'),
(23, 37, 53, 134, 2, 2, 2, 'Bonus', '2026-07-10 18:00:00', '2026-07-10 18:48:00'),
(24, 38, 55, 146, 2, 2, 1, 'Bonus', '2026-07-10 19:05:00', '2026-07-10 19:53:00'),
(25, 39, 54, 140, 2, 2, 2, 'Foul', '2026-07-11 18:00:00', '2026-07-11 18:50:00'),
(26, 40, 56, 152, 2, 2, 1, 'Bonus', '2026-07-11 19:05:00', '2026-07-11 19:54:00'),
(27, 41, 58, 164, 2, 2, 2, 'Bonus', '2026-07-12 18:00:00', '2026-07-12 18:47:00'),
(28, 42, 58, 164, 2, 2, 1, 'Foul', '2026-07-12 19:05:00', '2026-07-12 19:56:00'),
(29, 43, 55, 146, 2, 2, 2, 'Bonus', '2026-07-13 18:00:00', '2026-07-13 18:52:00'),
(30, 44, 54, 140, 2, 2, 1, 'Bonus', '2026-07-13 19:10:00', '2026-07-13 19:25:00'),
(31, 45, 56, 152, 2, 2, 2, 'Foul', '2026-07-14 18:00:00', '2026-07-14 18:45:00'),
(32, 46, 55, 146, 2, 2, 1, 'Bonus', '2026-07-14 19:15:00', '2026-07-14 20:10:00');

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

--
-- Dump dei dati per la tabella `group_teams`
--

INSERT INTO `group_teams` (`id`, `group_id`, `team_id`, `seed_label`) VALUES
(9, 3, 12, 'A1'),
(10, 3, 13, 'A2'),
(11, 3, 14, 'A3'),
(12, 3, 15, 'A4'),
(13, 4, 16, 'B1'),
(14, 4, 17, 'B2'),
(15, 4, 18, 'B3'),
(16, 4, 19, 'B4'),
(17, 3, 20, 'HAW'),
(18, 3, 21, 'BUL'),
(19, 3, 22, 'LAK'),
(20, 3, 23, 'SHA'),
(21, 4, 24, 'RAP'),
(22, 4, 25, 'CEL'),
(23, 4, 26, 'SUN'),
(24, 4, 27, 'KIN'),
(25, 5, 31, 'HAW'),
(26, 5, 32, 'BUL'),
(27, 7, 12, NULL),
(28, 5, 34, 'SHA'),
(29, 6, 35, 'RAP'),
(30, 6, 36, 'CEL'),
(31, 6, 37, 'SUN'),
(32, 6, 38, 'KIN'),
(33, 7, 31, 'HAW'),
(34, 7, 32, 'BUL'),
(35, 7, 33, 'LAK'),
(36, 7, 34, 'SHA'),
(37, 8, 31, 'HAW'),
(38, 8, 32, 'BUL'),
(39, 8, 33, 'LAK'),
(40, 8, 34, 'SHA'),
(41, 9, 39, 'N1'),
(48, 9, 40, 'VIP'),
(49, 9, 41, 'SHK'),
(50, 9, 42, 'TIT'),
(51, 9, 43, 'WAV'),
(52, 9, 44, 'EAG'),
(61, 19, 53, 'A1'),
(62, 19, 54, 'A2'),
(63, 19, 55, 'A3'),
(64, 20, 56, 'B1'),
(65, 20, 57, 'B2'),
(66, 20, 58, 'B3');

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

--
-- Dump dei dati per la tabella `matches`
--

INSERT INTO `matches` (`id`, `edition_id`, `group_id`, `court_id`, `name`, `phase`, `round`, `scheduled_start_at`, `scheduled_end_at`, `actual_start_at`, `actual_end_at`, `status`, `period_count`, `period_duration_ms`, `break_duration_ms`, `overtime_duration_ms`, `shot_clock_ms`, `timeout_duration_ms`, `timeouts_per_team`, `timeouts_per_period`, `personal_foul_limit`, `team_foul_bonus_threshold`, `max_score`, `max_score_enabled`, `stop_clock_on_free_throws`, `winner_team_id`, `win_reason`, `notes`, `created_at`, `updated_at`) VALUES
(2, 2, 3, 2, 'A2 vs A4', 'GroupStage', 'Giornata 1', NULL, NULL, NULL, NULL, 'Finished', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, 45, 1, 1, 15, 'Regular', NULL, '2026-05-27 10:19:14', '2026-05-28 08:29:30'),
(3, 2, 3, 3, 'Piazzetta Hawks vs Porto Bulls', 'GroupStage', 'Giornata 1', NULL, NULL, NULL, NULL, 'Finished', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, 45, 1, 1, 20, 'Regular', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(4, 2, 3, 3, 'Monte Lakers vs Adriatica Sharks', 'GroupStage', 'Giornata 1', NULL, NULL, NULL, NULL, 'Ready', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, 45, 1, 1, NULL, NULL, NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(5, 2, 4, 3, 'Centro Raptors vs Piazza Celtics', 'GroupStage', 'Giornata 1', NULL, NULL, NULL, NULL, 'Finished', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, 45, 1, 1, 25, 'Regular', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(6, 2, 4, 3, 'Marina Suns vs Collina Kings', 'GroupStage', 'Giornata 1', NULL, NULL, NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, 45, 1, 1, NULL, NULL, NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(7, 2, NULL, 3, 'Semifinale 1', 'SemiFinal', 'Final Four', NULL, NULL, NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, 45, 1, 1, NULL, NULL, NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(8, 2, NULL, 3, 'Finale Piazzetta Madness', 'Final', 'Final Four', NULL, NULL, NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, 45, 1, 1, NULL, NULL, NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(9, 2, 3, 3, 'test', 'GroupStage', NULL, '2026-06-04 19:30:00', '2026-06-04 20:30:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, 45, 1, 1, NULL, NULL, NULL, '2026-06-03 17:53:27', '2026-06-03 17:53:27'),
(10, 5, 8, 6, 'Monte Lakers vs Adriatica Sharks', 'GroupStage', 'Giornata 1', NULL, NULL, NULL, NULL, 'Ready', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, 45, 1, 1, NULL, NULL, NULL, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(11, 5, 6, 6, 'Centro Raptors vs Piazza Celtics', 'GroupStage', 'Giornata 1', NULL, NULL, NULL, NULL, 'Finished', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, 45, 1, 1, 36, 'Regular', NULL, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(12, 5, 6, 6, 'Marina Suns vs Collina Kings', 'GroupStage', 'Giornata 1', NULL, NULL, NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, 45, 1, 1, NULL, NULL, NULL, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(13, 5, NULL, 6, 'Semifinale 1', 'SemiFinal', 'Final Four', NULL, NULL, NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, 45, 1, 1, NULL, NULL, NULL, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(14, 5, NULL, 6, 'Finale Piazzetta Madness', 'Final', 'Final Four', NULL, NULL, NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, 45, 1, 1, NULL, NULL, NULL, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(15, 6, 9, 7, 'Ancona Albatros vs Porto Recanati Vipers', 'GroupStage', 'Girone A', '2026-07-04 18:00:00', '2026-07-04 18:45:00', '2026-07-04 18:03:00', '2026-07-04 18:47:00', 'Finished', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, 39, 'Regular', 'Partita equilibrata decisa negli ultimi possessi.', '2026-06-08 00:26:32', '2026-06-08 01:31:08'),
(16, 6, 9, 8, 'Porto Recanati Vipers vs Civitanova Sharks', 'GroupStage', 'Girone A', '2026-07-04 18:50:00', '2026-07-04 19:35:00', '2026-07-04 18:52:00', '2026-07-04 19:38:00', 'Finished', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, 41, 'Regular', 'Break decisivo degli Sharks nel secondo periodo.', '2026-06-08 00:26:32', '2026-06-08 01:36:55'),
(17, 6, 9, 9, 'Civitanova Sharks vs Macerata Titans', 'GroupStage', 'Girone A', '2026-07-04 19:40:00', '2026-07-04 20:25:00', '2026-07-04 19:42:00', '2026-07-04 20:30:00', 'Finished', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, 41, 'Overtime', 'Vittoria al supplementare dopo parità nei tempi regolamentari.', '2026-06-08 00:26:32', '2026-06-08 01:32:35'),
(18, 6, 9, 10, 'Macerata Titans vs Loreto Waves', 'GroupStage', 'Girone A', '2026-07-05 18:00:00', '2026-07-05 18:45:00', '2026-07-05 18:01:00', '2026-07-05 18:48:00', 'Finished', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, 43, 'Regular', 'Loreto ribalta il risultato con alta percentuale da tre.', '2026-06-08 00:26:32', '2026-06-08 01:36:55'),
(19, 6, 9, 11, 'Loreto Waves vs Osimo Eagles', 'GroupStage', 'Girone A', '2026-07-05 18:50:00', '2026-07-05 19:35:00', '2026-07-05 18:53:00', '2026-07-05 19:39:00', 'Finished', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, 43, 'MaxScoreReached', 'Waves chiudono con massimo punteggio raggiunto.', '2026-06-08 00:26:32', '2026-06-08 01:32:35'),
(20, 6, 9, 12, 'Osimo Eagles vs Ancona Albatros', 'GroupStage', 'Girone A', '2026-07-05 19:40:00', '2026-07-05 20:25:00', '2026-07-05 19:43:00', '2026-07-05 20:31:00', 'Finished', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, 39, 'Regular', 'Ancona vince in rimonta con parziale finale di 12-3.', '2026-06-08 00:26:32', '2026-06-08 01:36:55'),
(37, 14, 19, 21, 'Adriatica Blu vs Macerata Reds', 'GroupStage', 'Girone A - giornata 1', '2026-07-10 18:00:00', '2026-07-10 18:48:00', '2026-07-10 18:00:00', '2026-07-10 18:48:00', 'Finished', 2, 720000, 120000, 180000, 24000, 30000, 2, 1, 5, 5, 70, 1, 1, 53, 'Regular', 'Risultato omologato dalla direzione gara.', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(38, 14, 19, 22, 'Adriatica Blu vs Porto Verde', 'GroupStage', 'Girone A - giornata 2', '2026-07-10 19:05:00', '2026-07-10 19:53:00', '2026-07-10 19:05:00', '2026-07-10 19:53:00', 'Finished', 2, 720000, 120000, 180000, 24000, 30000, 2, 1, 5, 5, 70, 1, 1, 55, 'Regular', 'Risultato omologato dalla direzione gara.', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(39, 14, 19, 21, 'Macerata Reds vs Porto Verde', 'GroupStage', 'Girone A - giornata 3', '2026-07-11 18:00:00', '2026-07-11 18:50:00', '2026-07-11 18:00:00', '2026-07-11 18:50:00', 'Finished', 2, 720000, 120000, 180000, 24000, 30000, 2, 1, 5, 5, 70, 1, 1, 54, 'Regular', 'Risultato omologato dalla direzione gara.', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(40, 14, 20, 22, 'Collina Nera vs Riviera Gold', 'GroupStage', 'Girone B - giornata 1', '2026-07-11 19:05:00', '2026-07-11 19:54:00', '2026-07-11 19:05:00', '2026-07-11 19:54:00', 'Finished', 2, 720000, 120000, 180000, 24000, 30000, 2, 1, 5, 5, 70, 1, 1, 56, 'Regular', 'Risultato omologato dalla direzione gara.', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(41, 14, 20, 21, 'Collina Nera vs Monte Bianco', 'GroupStage', 'Girone B - giornata 2', '2026-07-12 18:00:00', '2026-07-12 18:47:00', '2026-07-12 18:00:00', '2026-07-12 18:47:00', 'Finished', 2, 720000, 120000, 180000, 24000, 30000, 2, 1, 5, 5, 70, 1, 1, 58, 'Regular', 'Risultato omologato dalla direzione gara.', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(42, 14, 20, 22, 'Riviera Gold vs Monte Bianco', 'GroupStage', 'Girone B - giornata 3', '2026-07-12 19:05:00', '2026-07-12 19:56:00', '2026-07-12 19:05:00', '2026-07-12 19:56:00', 'Finished', 3, 720000, 120000, 180000, 24000, 30000, 2, 1, 5, 5, 70, 1, 1, 58, 'Overtime', 'Risultato omologato dalla direzione gara.', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(43, 14, NULL, 21, 'Semifinale 1 - Porto Verde vs Collina Nera', 'SemiFinal', 'Final Four', '2026-07-13 18:00:00', '2026-07-13 18:52:00', '2026-07-13 18:00:00', '2026-07-13 18:52:00', 'Finished', 2, 720000, 120000, 180000, 24000, 30000, 2, 1, 5, 5, 70, 1, 1, 55, 'Regular', 'Risultato omologato dalla direzione gara.', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(44, 14, NULL, 22, 'Semifinale 2 - Monte Bianco vs Macerata Reds', 'SemiFinal', 'Final Four', '2026-07-13 19:10:00', '2026-07-13 19:25:00', '2026-07-13 19:10:00', '2026-07-13 19:25:00', 'Finished', 2, 720000, 120000, 180000, 24000, 30000, 2, 1, 5, 5, 70, 1, 1, 54, 'Forfeit', 'Gara assegnata a tavolino per squadra non presentata con numero minimo di giocatori.', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(45, 14, NULL, 22, 'Finale terzo posto - Collina Nera vs Monte Bianco', 'ThirdPlaceFinal', 'Final Four', '2026-07-14 18:00:00', '2026-07-14 18:45:00', '2026-07-14 18:00:00', '2026-07-14 18:45:00', 'Finished', 2, 720000, 120000, 180000, 24000, 30000, 2, 1, 5, 5, 70, 1, 1, 56, 'Regular', 'Risultato omologato dalla direzione gara.', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(46, 14, NULL, 21, 'Finale - Porto Verde vs Macerata Reds', 'Final', 'Final Four', '2026-07-14 19:15:00', '2026-07-14 20:10:00', '2026-07-14 19:15:00', '2026-07-14 20:10:00', 'Finished', 3, 720000, 120000, 180000, 24000, 30000, 2, 1, 5, 5, 70, 1, 1, 55, 'Overtime', 'Risultato omologato dalla direzione gara.', '2026-06-08 01:55:15', '2026-06-08 01:55:15');

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

--
-- Dump dei dati per la tabella `match_events`
--

INSERT INTO `match_events` (`id`, `match_id`, `period`, `period_type`, `game_clock_ms_remaining`, `shot_clock_ms_remaining`, `team_id`, `player_id`, `event_type`, `points`, `is_correction`, `reverts_event_id`, `description`, `payload_json`, `created_at`, `synced_at`) VALUES
(1, 3, 1, 'Regular', 480000, 18000, 20, 5, 'Score', 3, 0, NULL, 'Tripla in transizione', NULL, '2026-05-27 12:37:26', NULL),
(2, 3, 1, 'Regular', 480000, 18000, 21, 13, 'Score', 2, 0, NULL, 'Canestro dal pitturato', NULL, '2026-05-27 12:37:26', NULL),
(3, 5, 1, 'Regular', 480000, 18000, 25, 40, 'Score', 3, 0, NULL, 'Tripla decisiva', NULL, '2026-05-27 12:37:26', NULL),
(4, 2, 1, 'Regular', 716957, 20957, 13, NULL, 'Score', 2, 0, NULL, 'A2 +2', NULL, '2026-05-27 15:44:20', NULL),
(5, 2, 1, 'Regular', 709864, 13864, 13, NULL, 'Score', 2, 0, NULL, 'A2 +2', NULL, '2026-05-27 15:44:27', NULL),
(6, 2, 1, 'Regular', 703103, 20951, 15, NULL, 'Score', 3, 0, NULL, 'A4 +3', NULL, '2026-05-27 15:44:34', NULL),
(7, 2, 1, 'Regular', 701315, 19163, 15, NULL, 'Score', 3, 0, NULL, 'A4 +3', NULL, '2026-05-27 15:44:36', NULL),
(8, 3, 1, 'Regular', 720000, 24000, 20, 7, 'Score', 2, 0, NULL, '#6 Marchetti Andrea +2', NULL, '2026-05-27 16:05:06', NULL),
(9, 3, 1, 'Regular', 720000, 24000, 20, 9, 'Score', 1, 0, NULL, '#8 Conti Davide +1', NULL, '2026-05-27 16:33:14', NULL),
(10, 3, 1, 'Regular', 720000, 24000, 20, 11, 'Score', 1, 0, NULL, '#10 Serafini Alessandro +1', NULL, '2026-05-27 16:33:16', NULL),
(11, 3, 1, 'Regular', 720000, 24000, 20, 11, 'Score', 3, 0, NULL, '#10 Serafini Alessandro +3', NULL, '2026-05-27 16:33:31', NULL),
(12, 3, 1, 'Regular', 720000, 24000, 20, 11, 'Score', 3, 0, NULL, '#10 Serafini Alessandro +3', NULL, '2026-05-27 16:33:33', NULL),
(13, 3, 1, 'Regular', 720000, 24000, 20, 11, 'Score', -1, 0, NULL, '#10 Serafini Alessandro +-1', NULL, '2026-05-27 16:33:34', NULL),
(14, 3, 1, 'Regular', 720000, 24000, 20, 11, 'Score', -1, 0, NULL, '#10 Serafini Alessandro +-1', NULL, '2026-05-27 16:33:35', NULL),
(15, 3, 1, 'Regular', 720000, 24000, 20, 11, 'Score', -1, 0, NULL, '#10 Serafini Alessandro +-1', NULL, '2026-05-27 16:33:35', NULL),
(16, 4, 1, 'Regular', 720000, 24000, 22, NULL, 'Foul', NULL, 0, NULL, 'LAK falli +1', NULL, '2026-05-27 16:44:20', NULL),
(17, 4, 1, 'Regular', 720000, 24000, 22, NULL, 'Foul', NULL, 0, NULL, 'LAK falli +1', NULL, '2026-05-27 16:44:22', NULL),
(18, 4, 1, 'Regular', 720000, 24000, 22, NULL, 'FoulCorrection', NULL, 1, NULL, 'LAK falli -1', NULL, '2026-05-27 16:44:22', NULL),
(19, 4, 1, 'Regular', 720000, 24000, 22, NULL, 'FoulCorrection', NULL, 1, NULL, 'LAK falli -1', NULL, '2026-05-27 16:44:22', NULL),
(20, 4, 1, 'Regular', 720000, 24000, 22, NULL, 'Foul', NULL, 0, NULL, 'LAK falli +1', NULL, '2026-05-27 16:44:23', NULL),
(21, 4, 1, 'Regular', 720000, 24000, 22, NULL, 'Foul', NULL, 0, NULL, 'LAK falli +1', NULL, '2026-05-27 16:44:23', NULL),
(22, 4, 1, 'Regular', 720000, 24000, 22, NULL, 'FoulCorrection', NULL, 1, NULL, 'LAK falli -1', NULL, '2026-05-27 16:44:24', NULL),
(23, 4, 1, 'Regular', 720000, 24000, 22, NULL, 'FoulCorrection', NULL, 1, NULL, 'LAK falli -1', NULL, '2026-05-27 16:44:24', NULL),
(24, 4, 4, 'Regular', 711451, 15451, 22, 21, 'Score', 2, 0, NULL, '#26 Gentili Alessandro +2', NULL, '2026-05-27 16:46:08', NULL),
(25, 4, 4, 'Regular', 709619, 13619, 22, 21, 'Score', 2, 0, NULL, '#26 Gentili Alessandro +2', NULL, '2026-05-27 16:46:10', NULL),
(26, 4, 4, 'Regular', 705757, 9757, 22, 21, 'Score', -1, 0, NULL, '#26 Gentili Alessandro +-1', NULL, '2026-05-27 16:46:14', NULL),
(27, 4, 4, 'Regular', 701573, 5573, 22, 21, 'Score', -1, 0, NULL, '#26 Gentili Alessandro +-1', NULL, '2026-05-27 16:46:18', NULL),
(28, 4, 4, 'Regular', 701105, 5105, 22, 21, 'Score', -1, 0, NULL, '#26 Gentili Alessandro +-1', NULL, '2026-05-27 16:46:19', NULL),
(29, 4, 4, 'Regular', 700715, 4715, 22, 21, 'Score', -1, 0, NULL, '#26 Gentili Alessandro +-1', NULL, '2026-05-27 16:46:19', NULL),
(30, 2, 1, 'Regular', 708306, 12306, 13, NULL, 'Score', 1, 0, NULL, 'A2 +1', NULL, '2026-05-27 19:38:47', NULL),
(31, 2, 1, 'Regular', 707887, 11887, 13, NULL, 'Score', 1, 0, NULL, 'A2 +1', NULL, '2026-05-27 19:38:48', NULL),
(32, 2, 1, 'Regular', 706321, 10321, 13, NULL, 'Score', -1, 0, NULL, 'A2 +-1', NULL, '2026-05-27 19:38:49', NULL),
(33, 2, 1, 'Regular', 706168, 10168, 13, NULL, 'Score', -1, 0, NULL, 'A2 +-1', NULL, '2026-05-27 19:38:49', NULL),
(34, 3, 1, 'Regular', 714749, 13943, 20, 5, 'Score', 2, 0, NULL, '#4 Rossi Luca +2', NULL, '2026-05-27 19:38:54', NULL),
(35, 3, 1, 'Regular', 714749, 13943, 20, 5, 'Score', 2, 0, NULL, '#4 Rossi Luca +2', NULL, '2026-05-27 19:38:57', NULL),
(36, 3, 1, 'Regular', 714749, 13943, 20, 5, 'Score', 2, 0, NULL, '#4 Rossi Luca +2', NULL, '2026-05-27 19:38:58', NULL),
(37, 3, 1, 'Regular', 714749, 13943, 20, 8, 'Score', 2, 0, NULL, '#7 Ferri Matteo +2', NULL, '2026-05-27 19:39:00', NULL),
(38, 3, 1, 'Regular', 709015, 8209, 20, 6, 'Score', 2, 0, NULL, '#5 Bianchi Marco +2', NULL, '2026-05-27 19:39:21', NULL),
(39, 3, 1, 'Regular', 707072, 6266, 20, 6, 'Score', 2, 0, NULL, '#5 Bianchi Marco +2', NULL, '2026-05-27 19:39:23', NULL),
(40, 3, 1, 'Regular', 703122, 2316, 20, 8, 'Foul', NULL, 0, NULL, '#7 Ferri Matteo fallo +1', NULL, '2026-05-27 19:39:27', NULL),
(41, 2, 1, 'Regular', 718904, 24000, 15, NULL, 'Score', 2, 0, NULL, 'A4 +2', NULL, '2026-05-28 08:29:26', NULL),
(42, 2, 1, 'Regular', 718664, 24000, 15, NULL, 'Score', 2, 0, NULL, 'A4 +2', NULL, '2026-05-28 08:29:26', NULL),
(43, 4, 4, 'Regular', 716534, 24000, 22, 21, 'Score', 2, 0, NULL, '#26 Gentili Alessandro +2', NULL, '2026-05-28 16:53:43', NULL),
(44, 4, 4, 'Regular', 715767, 24000, 22, 21, 'Score', 2, 0, NULL, '#26 Gentili Alessandro +2', NULL, '2026-05-28 16:53:44', NULL),
(45, 4, 4, 'Regular', 715019, 24000, 22, 21, 'Score', 2, 0, NULL, '#26 Gentili Alessandro +2', NULL, '2026-05-28 16:53:45', NULL),
(46, 9, 1, 'Regular', 480000, 18000, 31, 5, 'Score', 3, 0, NULL, 'Tripla in transizione', NULL, '2026-06-03 15:23:47', NULL),
(47, 9, 1, 'Regular', 480000, 18000, 32, 13, 'Score', 2, 0, NULL, 'Canestro dal pitturato', NULL, '2026-06-03 15:23:47', NULL),
(48, 11, 1, 'Regular', 480000, 18000, 36, 40, 'Score', 3, 0, NULL, 'Tripla decisiva', NULL, '2026-06-03 15:23:47', NULL),
(49, 15, 2, 'Regular', 83000, 7000, 39, 64, 'Score', 3, 0, NULL, 'Tripla di Luca Verdi dall\'angolo sinistro.', '{\"action\": \"three_points\", \"source\": \"tavolo\", \"clock_ms\": 83000}', '2026-06-08 00:26:32', NULL),
(50, 16, 2, 'Regular', 147000, 12000, 41, 66, 'Steal', NULL, 0, NULL, 'Recupero difensivo di Andrea Ricci a metà campo.', '{\"action\": \"steal\", \"source\": \"tavolo\", \"clock_ms\": 147000}', '2026-06-08 00:26:32', NULL),
(51, 17, 3, 'Overtime', 26000, 4000, 41, 66, 'Score', 2, 0, NULL, 'Appoggio di Andrea Ricci nel supplementare.', '{\"action\": \"two_points\", \"source\": \"tavolo\", \"clock_ms\": 26000}', '2026-06-08 00:26:32', NULL),
(52, 18, 2, 'Regular', 91000, 9000, 43, 68, 'Timeout', NULL, 0, NULL, 'Timeout chiamato dalla panchina di Loreto.', '{\"action\": \"timeout\", \"source\": \"tavolo\", \"clock_ms\": 91000}', '2026-06-08 00:26:32', NULL),
(53, 19, 2, 'Regular', 112000, 16000, 43, 68, 'Score', 3, 0, NULL, 'Tripla di Davide Galli dopo scarico centrale.', '{\"action\": \"three_points\", \"source\": \"tavolo\", \"clock_ms\": 112000}', '2026-06-08 00:26:32', NULL),
(54, 20, 2, 'Regular', 39000, 3000, 39, 64, 'Score', 2, 0, NULL, 'Canestro decisivo di Luca Verdi in penetrazione.', '{\"action\": \"two_points\", \"source\": \"tavolo\", \"clock_ms\": 39000}', '2026-06-08 00:26:32', NULL),
(119, 37, 1, 'Regular', 689887, 18000, 53, 134, 'Score', 3, 0, NULL, 'Adriatica Blu vs Macerata Reds - Tripla in apertura', '{\"gara\": \"m1\", \"tipo\": \"Score\", \"progressivo\": 1}', '2026-07-10 18:00:00', '2026-07-10 18:48:00'),
(120, 37, 1, 'Regular', 640887, 12000, 54, 141, 'Score', 2, 0, NULL, 'Adriatica Blu vs Macerata Reds - Arresto e tiro dalla media', '{\"gara\": \"m1\", \"tipo\": \"Score\", \"progressivo\": 2}', '2026-07-10 18:00:00', '2026-07-10 18:48:00'),
(121, 37, 1, 'Regular', 597887, 9000, 53, 136, 'Foul', NULL, 0, NULL, 'Adriatica Blu vs Macerata Reds - Fallo personale su penetrazione', '{\"gara\": \"m1\", \"tipo\": \"Foul\", \"progressivo\": 3}', '2026-07-10 18:00:00', '2026-07-10 18:48:00'),
(122, 37, 2, 'Regular', 511887, 17000, 54, 140, 'Score', 3, 0, NULL, 'Adriatica Blu vs Macerata Reds - Tripla dall\'angolo', '{\"gara\": \"m1\", \"tipo\": \"Score\", \"progressivo\": 4}', '2026-07-10 18:00:00', '2026-07-10 18:48:00'),
(123, 37, 2, 'Regular', 283887, 22000, 53, 138, 'Score', 2, 0, NULL, 'Adriatica Blu vs Macerata Reds - Rimbalzo offensivo e appoggio', '{\"gara\": \"m1\", \"tipo\": \"Score\", \"progressivo\": 5}', '2026-07-10 18:00:00', '2026-07-10 18:48:00'),
(124, 37, 2, 'Regular', 95887, 8000, 54, 143, 'Timeout', NULL, 0, NULL, 'Adriatica Blu vs Macerata Reds - Timeout chiamato dalla panchina', '{\"gara\": \"m1\", \"tipo\": \"Timeout\", \"progressivo\": 6}', '2026-07-10 18:00:00', '2026-07-10 18:48:00'),
(125, 38, 1, 'Regular', 689774, 18000, 53, 134, 'Score', 3, 0, NULL, 'Adriatica Blu vs Porto Verde - Tripla in apertura', '{\"gara\": \"m2\", \"tipo\": \"Score\", \"progressivo\": 1}', '2026-07-10 19:05:00', '2026-07-10 19:53:00'),
(126, 38, 1, 'Regular', 640774, 12000, 55, 147, 'Score', 2, 0, NULL, 'Adriatica Blu vs Porto Verde - Arresto e tiro dalla media', '{\"gara\": \"m2\", \"tipo\": \"Score\", \"progressivo\": 2}', '2026-07-10 19:05:00', '2026-07-10 19:53:00'),
(127, 38, 1, 'Regular', 597774, 9000, 53, 136, 'Foul', NULL, 0, NULL, 'Adriatica Blu vs Porto Verde - Fallo personale su penetrazione', '{\"gara\": \"m2\", \"tipo\": \"Foul\", \"progressivo\": 3}', '2026-07-10 19:05:00', '2026-07-10 19:53:00'),
(128, 38, 2, 'Regular', 511774, 17000, 55, 146, 'Score', 3, 0, NULL, 'Adriatica Blu vs Porto Verde - Tripla dall\'angolo', '{\"gara\": \"m2\", \"tipo\": \"Score\", \"progressivo\": 4}', '2026-07-10 19:05:00', '2026-07-10 19:53:00'),
(129, 38, 2, 'Regular', 283774, 22000, 53, 138, 'Score', 2, 0, NULL, 'Adriatica Blu vs Porto Verde - Rimbalzo offensivo e appoggio', '{\"gara\": \"m2\", \"tipo\": \"Score\", \"progressivo\": 5}', '2026-07-10 19:05:00', '2026-07-10 19:53:00'),
(130, 38, 2, 'Regular', 95774, 8000, 55, 149, 'Timeout', NULL, 0, NULL, 'Adriatica Blu vs Porto Verde - Timeout chiamato dalla panchina', '{\"gara\": \"m2\", \"tipo\": \"Timeout\", \"progressivo\": 6}', '2026-07-10 19:05:00', '2026-07-10 19:53:00'),
(131, 39, 1, 'Regular', 689661, 18000, 54, 140, 'Score', 3, 0, NULL, 'Macerata Reds vs Porto Verde - Tripla in apertura', '{\"gara\": \"m3\", \"tipo\": \"Score\", \"progressivo\": 1}', '2026-07-11 18:00:00', '2026-07-11 18:50:00'),
(132, 39, 1, 'Regular', 640661, 12000, 55, 147, 'Score', 2, 0, NULL, 'Macerata Reds vs Porto Verde - Arresto e tiro dalla media', '{\"gara\": \"m3\", \"tipo\": \"Score\", \"progressivo\": 2}', '2026-07-11 18:00:00', '2026-07-11 18:50:00'),
(133, 39, 1, 'Regular', 597661, 9000, 54, 142, 'Foul', NULL, 0, NULL, 'Macerata Reds vs Porto Verde - Fallo personale su penetrazione', '{\"gara\": \"m3\", \"tipo\": \"Foul\", \"progressivo\": 3}', '2026-07-11 18:00:00', '2026-07-11 18:50:00'),
(134, 39, 2, 'Regular', 511661, 17000, 55, 146, 'Score', 3, 0, NULL, 'Macerata Reds vs Porto Verde - Tripla dall\'angolo', '{\"gara\": \"m3\", \"tipo\": \"Score\", \"progressivo\": 4}', '2026-07-11 18:00:00', '2026-07-11 18:50:00'),
(135, 39, 2, 'Regular', 283661, 22000, 54, 144, 'Score', 2, 0, NULL, 'Macerata Reds vs Porto Verde - Rimbalzo offensivo e appoggio', '{\"gara\": \"m3\", \"tipo\": \"Score\", \"progressivo\": 5}', '2026-07-11 18:00:00', '2026-07-11 18:50:00'),
(136, 39, 2, 'Regular', 95661, 8000, 55, 149, 'Timeout', NULL, 0, NULL, 'Macerata Reds vs Porto Verde - Timeout chiamato dalla panchina', '{\"gara\": \"m3\", \"tipo\": \"Timeout\", \"progressivo\": 6}', '2026-07-11 18:00:00', '2026-07-11 18:50:00'),
(137, 40, 1, 'Regular', 689548, 18000, 56, 152, 'Score', 3, 0, NULL, 'Collina Nera vs Riviera Gold - Tripla in apertura', '{\"gara\": \"m4\", \"tipo\": \"Score\", \"progressivo\": 1}', '2026-07-11 19:05:00', '2026-07-11 19:54:00'),
(138, 40, 1, 'Regular', 640548, 12000, 57, 159, 'Score', 2, 0, NULL, 'Collina Nera vs Riviera Gold - Arresto e tiro dalla media', '{\"gara\": \"m4\", \"tipo\": \"Score\", \"progressivo\": 2}', '2026-07-11 19:05:00', '2026-07-11 19:54:00'),
(139, 40, 1, 'Regular', 597548, 9000, 56, 154, 'Foul', NULL, 0, NULL, 'Collina Nera vs Riviera Gold - Fallo personale su penetrazione', '{\"gara\": \"m4\", \"tipo\": \"Foul\", \"progressivo\": 3}', '2026-07-11 19:05:00', '2026-07-11 19:54:00'),
(140, 40, 2, 'Regular', 511548, 17000, 57, 158, 'Score', 3, 0, NULL, 'Collina Nera vs Riviera Gold - Tripla dall\'angolo', '{\"gara\": \"m4\", \"tipo\": \"Score\", \"progressivo\": 4}', '2026-07-11 19:05:00', '2026-07-11 19:54:00'),
(141, 40, 2, 'Regular', 283548, 22000, 56, 156, 'Score', 2, 0, NULL, 'Collina Nera vs Riviera Gold - Rimbalzo offensivo e appoggio', '{\"gara\": \"m4\", \"tipo\": \"Score\", \"progressivo\": 5}', '2026-07-11 19:05:00', '2026-07-11 19:54:00'),
(142, 40, 2, 'Regular', 95548, 8000, 57, 161, 'Timeout', NULL, 0, NULL, 'Collina Nera vs Riviera Gold - Timeout chiamato dalla panchina', '{\"gara\": \"m4\", \"tipo\": \"Timeout\", \"progressivo\": 6}', '2026-07-11 19:05:00', '2026-07-11 19:54:00'),
(143, 41, 1, 'Regular', 689435, 18000, 56, 152, 'Score', 3, 0, NULL, 'Collina Nera vs Monte Bianco - Tripla in apertura', '{\"gara\": \"m5\", \"tipo\": \"Score\", \"progressivo\": 1}', '2026-07-12 18:00:00', '2026-07-12 18:47:00'),
(144, 41, 1, 'Regular', 640435, 12000, 58, 165, 'Score', 2, 0, NULL, 'Collina Nera vs Monte Bianco - Arresto e tiro dalla media', '{\"gara\": \"m5\", \"tipo\": \"Score\", \"progressivo\": 2}', '2026-07-12 18:00:00', '2026-07-12 18:47:00'),
(145, 41, 1, 'Regular', 597435, 9000, 56, 154, 'Foul', NULL, 0, NULL, 'Collina Nera vs Monte Bianco - Fallo personale su penetrazione', '{\"gara\": \"m5\", \"tipo\": \"Foul\", \"progressivo\": 3}', '2026-07-12 18:00:00', '2026-07-12 18:47:00'),
(146, 41, 2, 'Regular', 511435, 17000, 58, 164, 'Score', 3, 0, NULL, 'Collina Nera vs Monte Bianco - Tripla dall\'angolo', '{\"gara\": \"m5\", \"tipo\": \"Score\", \"progressivo\": 4}', '2026-07-12 18:00:00', '2026-07-12 18:47:00'),
(147, 41, 2, 'Regular', 283435, 22000, 56, 156, 'Score', 2, 0, NULL, 'Collina Nera vs Monte Bianco - Rimbalzo offensivo e appoggio', '{\"gara\": \"m5\", \"tipo\": \"Score\", \"progressivo\": 5}', '2026-07-12 18:00:00', '2026-07-12 18:47:00'),
(148, 41, 2, 'Regular', 95435, 8000, 58, 167, 'Timeout', NULL, 0, NULL, 'Collina Nera vs Monte Bianco - Timeout chiamato dalla panchina', '{\"gara\": \"m5\", \"tipo\": \"Timeout\", \"progressivo\": 6}', '2026-07-12 18:00:00', '2026-07-12 18:47:00'),
(149, 42, 1, 'Regular', 689322, 18000, 57, 158, 'Score', 3, 0, NULL, 'Riviera Gold vs Monte Bianco - Tripla in apertura', '{\"gara\": \"m6\", \"tipo\": \"Score\", \"progressivo\": 1}', '2026-07-12 19:05:00', '2026-07-12 19:56:00'),
(150, 42, 1, 'Regular', 640322, 12000, 58, 165, 'Score', 2, 0, NULL, 'Riviera Gold vs Monte Bianco - Arresto e tiro dalla media', '{\"gara\": \"m6\", \"tipo\": \"Score\", \"progressivo\": 2}', '2026-07-12 19:05:00', '2026-07-12 19:56:00'),
(151, 42, 1, 'Regular', 597322, 9000, 57, 160, 'Foul', NULL, 0, NULL, 'Riviera Gold vs Monte Bianco - Fallo personale su penetrazione', '{\"gara\": \"m6\", \"tipo\": \"Foul\", \"progressivo\": 3}', '2026-07-12 19:05:00', '2026-07-12 19:56:00'),
(152, 42, 2, 'Regular', 511322, 17000, 58, 164, 'Score', 3, 0, NULL, 'Riviera Gold vs Monte Bianco - Tripla dall\'angolo', '{\"gara\": \"m6\", \"tipo\": \"Score\", \"progressivo\": 4}', '2026-07-12 19:05:00', '2026-07-12 19:56:00'),
(153, 42, 2, 'Regular', 283322, 22000, 57, 162, 'Score', 2, 0, NULL, 'Riviera Gold vs Monte Bianco - Rimbalzo offensivo e appoggio', '{\"gara\": \"m6\", \"tipo\": \"Score\", \"progressivo\": 5}', '2026-07-12 19:05:00', '2026-07-12 19:56:00'),
(154, 42, 2, 'Regular', 95322, 8000, 58, 167, 'Timeout', NULL, 0, NULL, 'Riviera Gold vs Monte Bianco - Timeout chiamato dalla panchina', '{\"gara\": \"m6\", \"tipo\": \"Timeout\", \"progressivo\": 6}', '2026-07-12 19:05:00', '2026-07-12 19:56:00'),
(155, 43, 1, 'Regular', 689209, 18000, 55, 146, 'Score', 3, 0, NULL, 'Semifinale 1 - Porto Verde vs Collina Nera - Tripla in apertura', '{\"gara\": \"m7\", \"tipo\": \"Score\", \"progressivo\": 1}', '2026-07-13 18:00:00', '2026-07-13 18:52:00'),
(156, 43, 1, 'Regular', 640209, 12000, 56, 153, 'Score', 2, 0, NULL, 'Semifinale 1 - Porto Verde vs Collina Nera - Arresto e tiro dalla media', '{\"gara\": \"m7\", \"tipo\": \"Score\", \"progressivo\": 2}', '2026-07-13 18:00:00', '2026-07-13 18:52:00'),
(157, 43, 1, 'Regular', 597209, 9000, 55, 148, 'Foul', NULL, 0, NULL, 'Semifinale 1 - Porto Verde vs Collina Nera - Fallo personale su penetrazione', '{\"gara\": \"m7\", \"tipo\": \"Foul\", \"progressivo\": 3}', '2026-07-13 18:00:00', '2026-07-13 18:52:00'),
(158, 43, 2, 'Regular', 511209, 17000, 56, 152, 'Score', 3, 0, NULL, 'Semifinale 1 - Porto Verde vs Collina Nera - Tripla dall\'angolo', '{\"gara\": \"m7\", \"tipo\": \"Score\", \"progressivo\": 4}', '2026-07-13 18:00:00', '2026-07-13 18:52:00'),
(159, 43, 2, 'Regular', 283209, 22000, 55, 150, 'Score', 2, 0, NULL, 'Semifinale 1 - Porto Verde vs Collina Nera - Rimbalzo offensivo e appoggio', '{\"gara\": \"m7\", \"tipo\": \"Score\", \"progressivo\": 5}', '2026-07-13 18:00:00', '2026-07-13 18:52:00'),
(160, 43, 2, 'Regular', 95209, 8000, 56, 155, 'Timeout', NULL, 0, NULL, 'Semifinale 1 - Porto Verde vs Collina Nera - Timeout chiamato dalla panchina', '{\"gara\": \"m7\", \"tipo\": \"Timeout\", \"progressivo\": 6}', '2026-07-13 18:00:00', '2026-07-13 18:52:00'),
(161, 44, 1, 'Regular', 689096, 18000, 58, 164, 'Score', 3, 0, NULL, 'Semifinale 2 - Monte Bianco vs Macerata Reds - Tripla in apertura', '{\"gara\": \"m8\", \"tipo\": \"Score\", \"progressivo\": 1}', '2026-07-13 19:10:00', '2026-07-13 19:25:00'),
(162, 44, 1, 'Regular', 640096, 12000, 54, 141, 'Score', 2, 0, NULL, 'Semifinale 2 - Monte Bianco vs Macerata Reds - Arresto e tiro dalla media', '{\"gara\": \"m8\", \"tipo\": \"Score\", \"progressivo\": 2}', '2026-07-13 19:10:00', '2026-07-13 19:25:00'),
(163, 44, 1, 'Regular', 597096, 9000, 58, 166, 'Foul', NULL, 0, NULL, 'Semifinale 2 - Monte Bianco vs Macerata Reds - Fallo personale su penetrazione', '{\"gara\": \"m8\", \"tipo\": \"Foul\", \"progressivo\": 3}', '2026-07-13 19:10:00', '2026-07-13 19:25:00'),
(164, 44, 2, 'Regular', 511096, 17000, 54, 140, 'Score', 3, 0, NULL, 'Semifinale 2 - Monte Bianco vs Macerata Reds - Tripla dall\'angolo', '{\"gara\": \"m8\", \"tipo\": \"Score\", \"progressivo\": 4}', '2026-07-13 19:10:00', '2026-07-13 19:25:00'),
(165, 44, 2, 'Regular', 283096, 22000, 58, 168, 'Score', 2, 0, NULL, 'Semifinale 2 - Monte Bianco vs Macerata Reds - Rimbalzo offensivo e appoggio', '{\"gara\": \"m8\", \"tipo\": \"Score\", \"progressivo\": 5}', '2026-07-13 19:10:00', '2026-07-13 19:25:00'),
(166, 44, 2, 'Regular', 95096, 8000, 54, 143, 'Timeout', NULL, 0, NULL, 'Semifinale 2 - Monte Bianco vs Macerata Reds - Timeout chiamato dalla panchina', '{\"gara\": \"m8\", \"tipo\": \"Timeout\", \"progressivo\": 6}', '2026-07-13 19:10:00', '2026-07-13 19:25:00'),
(167, 45, 1, 'Regular', 688983, 18000, 56, 152, 'Score', 3, 0, NULL, 'Finale terzo posto - Collina Nera vs Monte Bianco - Tripla in apertura', '{\"gara\": \"m9\", \"tipo\": \"Score\", \"progressivo\": 1}', '2026-07-14 18:00:00', '2026-07-14 18:45:00'),
(168, 45, 1, 'Regular', 639983, 12000, 58, 165, 'Score', 2, 0, NULL, 'Finale terzo posto - Collina Nera vs Monte Bianco - Arresto e tiro dalla media', '{\"gara\": \"m9\", \"tipo\": \"Score\", \"progressivo\": 2}', '2026-07-14 18:00:00', '2026-07-14 18:45:00'),
(169, 45, 1, 'Regular', 596983, 9000, 56, 154, 'Foul', NULL, 0, NULL, 'Finale terzo posto - Collina Nera vs Monte Bianco - Fallo personale su penetrazione', '{\"gara\": \"m9\", \"tipo\": \"Foul\", \"progressivo\": 3}', '2026-07-14 18:00:00', '2026-07-14 18:45:00'),
(170, 45, 2, 'Regular', 510983, 17000, 58, 164, 'Score', 3, 0, NULL, 'Finale terzo posto - Collina Nera vs Monte Bianco - Tripla dall\'angolo', '{\"gara\": \"m9\", \"tipo\": \"Score\", \"progressivo\": 4}', '2026-07-14 18:00:00', '2026-07-14 18:45:00'),
(171, 45, 2, 'Regular', 282983, 22000, 56, 156, 'Score', 2, 0, NULL, 'Finale terzo posto - Collina Nera vs Monte Bianco - Rimbalzo offensivo e appoggio', '{\"gara\": \"m9\", \"tipo\": \"Score\", \"progressivo\": 5}', '2026-07-14 18:00:00', '2026-07-14 18:45:00'),
(172, 45, 2, 'Regular', 94983, 8000, 58, 167, 'Timeout', NULL, 0, NULL, 'Finale terzo posto - Collina Nera vs Monte Bianco - Timeout chiamato dalla panchina', '{\"gara\": \"m9\", \"tipo\": \"Timeout\", \"progressivo\": 6}', '2026-07-14 18:00:00', '2026-07-14 18:45:00'),
(173, 46, 1, 'Regular', 688870, 18000, 55, 146, 'Score', 3, 0, NULL, 'Finale - Porto Verde vs Macerata Reds - Tripla in apertura', '{\"gara\": \"m10\", \"tipo\": \"Score\", \"progressivo\": 1}', '2026-07-14 19:15:00', '2026-07-14 20:10:00'),
(174, 46, 1, 'Regular', 639870, 12000, 54, 141, 'Score', 2, 0, NULL, 'Finale - Porto Verde vs Macerata Reds - Arresto e tiro dalla media', '{\"gara\": \"m10\", \"tipo\": \"Score\", \"progressivo\": 2}', '2026-07-14 19:15:00', '2026-07-14 20:10:00'),
(175, 46, 1, 'Regular', 596870, 9000, 55, 148, 'Foul', NULL, 0, NULL, 'Finale - Porto Verde vs Macerata Reds - Fallo personale su penetrazione', '{\"gara\": \"m10\", \"tipo\": \"Foul\", \"progressivo\": 3}', '2026-07-14 19:15:00', '2026-07-14 20:10:00'),
(176, 46, 2, 'Regular', 510870, 17000, 54, 140, 'Score', 3, 0, NULL, 'Finale - Porto Verde vs Macerata Reds - Tripla dall\'angolo', '{\"gara\": \"m10\", \"tipo\": \"Score\", \"progressivo\": 4}', '2026-07-14 19:15:00', '2026-07-14 20:10:00'),
(177, 46, 2, 'Regular', 282870, 22000, 55, 150, 'Score', 2, 0, NULL, 'Finale - Porto Verde vs Macerata Reds - Rimbalzo offensivo e appoggio', '{\"gara\": \"m10\", \"tipo\": \"Score\", \"progressivo\": 5}', '2026-07-14 19:15:00', '2026-07-14 20:10:00'),
(178, 46, 2, 'Regular', 94870, 8000, 54, 143, 'Timeout', NULL, 0, NULL, 'Finale - Porto Verde vs Macerata Reds - Timeout chiamato dalla panchina', '{\"gara\": \"m10\", \"tipo\": \"Timeout\", \"progressivo\": 6}', '2026-07-14 19:15:00', '2026-07-14 20:10:00');

-- --------------------------------------------------------

--
-- Struttura della tabella `match_fouls`
--

CREATE TABLE `match_fouls` (
  `id` int UNSIGNED NOT NULL,
  `match_id` int UNSIGNED NOT NULL,
  `team_id` int UNSIGNED NOT NULL,
  `player_id` int UNSIGNED DEFAULT NULL,
  `period` int NOT NULL,
  `game_clock_ms_remaining` int NOT NULL,
  `foul_type` enum('Personal','Technical','Unsportsmanlike','Disqualifying') COLLATE utf8mb4_unicode_ci NOT NULL,
  `counts_as_team_foul` tinyint(1) NOT NULL DEFAULT '1',
  `free_throws_awarded` int NOT NULL DEFAULT '0',
  `created_event_id` int UNSIGNED DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dump dei dati per la tabella `match_fouls`
--

INSERT INTO `match_fouls` (`id`, `match_id`, `team_id`, `player_id`, `period`, `game_clock_ms_remaining`, `foul_type`, `counts_as_team_foul`, `free_throws_awarded`, `created_event_id`) VALUES
(1, 15, 40, 65, 1, 612000, 'Personal', 1, 2, NULL),
(2, 16, 41, 66, 1, 505000, 'Technical', 1, 1, NULL),
(3, 17, 42, 67, 2, 438000, 'Unsportsmanlike', 1, 2, NULL),
(4, 18, 43, 68, 2, 322000, 'Personal', 1, 0, NULL),
(5, 19, 44, 69, 1, 288000, 'Disqualifying', 1, 3, NULL),
(6, 20, 39, 64, 2, 141000, 'Personal', 1, 2, NULL),
(39, 37, 53, 136, 1, 519549, 'Personal', 1, 0, NULL),
(40, 37, 54, 143, 2, 519549, 'Personal', 1, 2, NULL),
(41, 38, 53, 136, 1, 519098, 'Personal', 1, 0, NULL),
(42, 38, 55, 149, 2, 519098, 'Personal', 1, 2, NULL),
(43, 39, 54, 142, 1, 518647, 'Personal', 1, 0, NULL),
(44, 39, 55, 149, 2, 518647, 'Personal', 1, 2, NULL),
(45, 40, 56, 154, 1, 518196, 'Personal', 1, 0, NULL),
(46, 40, 57, 161, 2, 518196, 'Technical', 1, 2, NULL),
(47, 41, 56, 154, 1, 517745, 'Personal', 1, 0, NULL),
(48, 41, 58, 167, 2, 517745, 'Personal', 1, 2, NULL),
(49, 42, 57, 160, 1, 517294, 'Personal', 1, 0, NULL),
(50, 42, 58, 167, 2, 517294, 'Personal', 1, 2, NULL),
(51, 43, 55, 148, 1, 516843, 'Personal', 1, 0, NULL),
(52, 43, 56, 155, 2, 516843, 'Personal', 1, 2, NULL),
(53, 44, 58, 166, 1, 516392, 'Personal', 1, 0, NULL),
(54, 44, 54, 143, 2, 516392, 'Technical', 1, 2, NULL),
(55, 45, 56, 154, 1, 515941, 'Personal', 1, 0, NULL),
(56, 45, 58, 167, 2, 515941, 'Personal', 1, 2, NULL),
(57, 46, 55, 148, 1, 515490, 'Personal', 1, 0, NULL),
(58, 46, 54, 143, 2, 515490, 'Personal', 1, 2, NULL);

-- --------------------------------------------------------

--
-- Struttura della tabella `match_periods`
--

CREATE TABLE `match_periods` (
  `id` int UNSIGNED NOT NULL,
  `match_id` int UNSIGNED NOT NULL,
  `period_number` int NOT NULL,
  `period_type` enum('Regular','Overtime','SuddenDeath') COLLATE utf8mb4_unicode_ci NOT NULL,
  `duration_ms` int NOT NULL,
  `started_at` datetime DEFAULT NULL,
  `ended_at` datetime DEFAULT NULL,
  `home_score_start` int NOT NULL DEFAULT '0',
  `away_score_start` int NOT NULL DEFAULT '0',
  `home_score_end` int DEFAULT NULL,
  `away_score_end` int DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dump dei dati per la tabella `match_periods`
--

INSERT INTO `match_periods` (`id`, `match_id`, `period_number`, `period_type`, `duration_ms`, `started_at`, `ended_at`, `home_score_start`, `away_score_start`, `home_score_end`, `away_score_end`) VALUES
(1, 15, 1, 'Regular', 720000, '2026-07-04 18:03:00', '2026-07-04 18:24:00', 0, 0, 28, 26),
(2, 16, 1, 'Regular', 720000, '2026-07-04 18:52:00', '2026-07-04 19:13:00', 0, 0, 18, 22),
(3, 17, 1, 'Regular', 720000, '2026-07-04 19:42:00', '2026-07-04 20:03:00', 0, 0, 31, 28),
(4, 18, 1, 'Regular', 720000, '2026-07-05 18:01:00', '2026-07-05 18:22:00', 0, 0, 20, 25),
(5, 19, 1, 'Regular', 720000, '2026-07-05 18:53:00', '2026-07-05 19:14:00', 0, 0, 29, 21),
(6, 20, 1, 'Regular', 720000, '2026-07-05 19:43:00', '2026-07-05 20:04:00', 0, 0, 27, 32),
(39, 37, 1, 'Regular', 720000, '2026-07-10 18:00:00', '2026-07-10 18:12:00', 0, 0, 27, 24),
(40, 37, 2, 'Regular', 720000, '2026-07-10 18:14:00', '2026-07-10 18:48:00', 27, 24, 58, 52),
(41, 38, 1, 'Regular', 720000, '2026-07-10 19:05:00', '2026-07-10 19:17:00', 0, 0, 23, 28),
(42, 38, 2, 'Regular', 720000, '2026-07-10 19:19:00', '2026-07-10 19:53:00', 23, 28, 49, 61),
(43, 39, 1, 'Regular', 720000, '2026-07-11 18:00:00', '2026-07-11 18:12:00', 0, 0, 26, 22),
(44, 39, 2, 'Regular', 720000, '2026-07-11 18:14:00', '2026-07-11 18:50:00', 26, 22, 55, 47),
(45, 40, 1, 'Regular', 720000, '2026-07-11 19:05:00', '2026-07-11 19:17:00', 0, 0, 30, 26),
(46, 40, 2, 'Regular', 720000, '2026-07-11 19:19:00', '2026-07-11 19:54:00', 30, 26, 63, 57),
(47, 41, 1, 'Regular', 720000, '2026-07-12 18:00:00', '2026-07-12 18:12:00', 0, 0, 25, 27),
(48, 41, 2, 'Regular', 720000, '2026-07-12 18:14:00', '2026-07-12 18:47:00', 25, 27, 54, 59),
(49, 42, 1, 'Regular', 720000, '2026-07-12 19:05:00', '2026-07-12 19:17:00', 0, 0, 28, 27),
(50, 42, 2, 'Regular', 720000, '2026-07-12 19:19:00', '2026-07-12 19:33:00', 28, 27, 58, 58),
(51, 42, 3, 'Overtime', 180000, '2026-07-12 19:36:00', '2026-07-12 19:56:00', 58, 58, 62, 64),
(52, 43, 1, 'Regular', 720000, '2026-07-13 18:00:00', '2026-07-13 18:12:00', 0, 0, 31, 28),
(53, 43, 2, 'Regular', 720000, '2026-07-13 18:14:00', '2026-07-13 18:52:00', 31, 28, 66, 60),
(54, 44, 1, 'Regular', 720000, '2026-07-13 19:10:00', '2026-07-13 19:22:00', 0, 0, 0, 9),
(55, 44, 2, 'Regular', 720000, '2026-07-13 19:24:00', '2026-07-13 19:25:00', 0, 9, 0, 20),
(56, 45, 1, 'Regular', 720000, '2026-07-14 18:00:00', '2026-07-14 18:12:00', 0, 0, 24, 20),
(57, 45, 2, 'Regular', 720000, '2026-07-14 18:14:00', '2026-07-14 18:45:00', 24, 20, 50, 44),
(58, 46, 1, 'Regular', 720000, '2026-07-14 19:15:00', '2026-07-14 19:27:00', 0, 0, 29, 29),
(59, 46, 2, 'Regular', 720000, '2026-07-14 19:29:00', '2026-07-14 19:43:00', 29, 29, 61, 61),
(60, 46, 3, 'Overtime', 180000, '2026-07-14 19:46:00', '2026-07-14 20:10:00', 61, 61, 68, 65);

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

--
-- Dump dei dati per la tabella `match_players`
--

INSERT INTO `match_players` (`id`, `match_id`, `team_id`, `player_id`, `jersey_number`, `is_starting_five`, `is_on_court`, `points`, `personal_fouls`, `is_fouled_out`, `is_ejected`) VALUES
(1, 3, 20, 5, 4, 1, 1, 10, 0, 0, 0),
(2, 3, 20, 6, 5, 1, 1, 9, 1, 0, 0),
(3, 3, 20, 7, 6, 1, 1, 8, 2, 0, 0),
(4, 3, 20, 8, 7, 1, 1, 9, 1, 0, 0),
(5, 3, 20, 9, 8, 1, 1, 9, 1, 0, 0),
(6, 3, 20, 10, 9, 0, 0, 5, 2, 0, 0),
(7, 3, 20, 11, 10, 0, 0, 10, 0, 0, 0),
(8, 3, 21, 12, 14, 1, 1, 4, 0, 0, 0),
(9, 3, 21, 13, 15, 1, 1, 5, 1, 0, 0),
(10, 3, 21, 14, 16, 1, 1, 6, 2, 0, 0),
(11, 3, 21, 15, 17, 1, 1, 7, 0, 0, 0),
(12, 3, 21, 16, 18, 1, 1, 8, 1, 0, 0),
(13, 3, 21, 17, 19, 0, 0, 5, 2, 0, 0),
(14, 3, 21, 18, 20, 0, 0, 6, 0, 0, 0),
(15, 4, 22, 19, 24, 1, 1, 4, 0, 0, 0),
(16, 4, 22, 20, 25, 1, 1, 5, 1, 0, 0),
(17, 4, 22, 21, 26, 1, 1, 12, 2, 0, 0),
(18, 4, 22, 22, 27, 1, 1, 7, 0, 0, 0),
(19, 4, 22, 23, 28, 1, 1, 8, 1, 0, 0),
(20, 4, 22, 24, 29, 0, 0, 5, 2, 0, 0),
(21, 4, 22, 25, 30, 0, 0, 6, 0, 0, 0),
(22, 4, 23, 26, 4, 1, 1, 4, 0, 0, 0),
(23, 4, 23, 27, 5, 1, 1, 5, 1, 0, 0),
(24, 4, 23, 28, 6, 1, 1, 6, 2, 0, 0),
(25, 4, 23, 29, 7, 1, 1, 7, 0, 0, 0),
(26, 4, 23, 30, 8, 1, 1, 8, 1, 0, 0),
(27, 4, 23, 31, 9, 0, 0, 5, 2, 0, 0),
(28, 4, 23, 32, 10, 0, 0, 6, 0, 0, 0),
(29, 5, 24, 33, 14, 1, 1, 4, 0, 0, 0),
(30, 5, 24, 34, 15, 1, 1, 5, 1, 0, 0),
(31, 5, 24, 35, 16, 1, 1, 6, 2, 0, 0),
(32, 5, 24, 36, 17, 1, 1, 7, 0, 0, 0),
(33, 5, 24, 37, 18, 1, 1, 8, 1, 0, 0),
(34, 5, 24, 38, 19, 1, 1, 5, 2, 0, 0),
(35, 5, 24, 39, 20, 1, 1, 6, 0, 0, 0),
(36, 5, 25, 40, 24, 1, 1, 4, 0, 0, 0),
(37, 5, 25, 41, 25, 1, 1, 5, 1, 0, 0),
(38, 5, 25, 42, 26, 1, 1, 6, 2, 0, 0),
(39, 5, 25, 43, 27, 1, 1, 7, 0, 0, 0),
(40, 5, 25, 44, 28, 1, 1, 8, 1, 0, 0),
(41, 5, 25, 45, 29, 0, 0, 5, 2, 0, 0),
(42, 5, 25, 46, 30, 0, 0, 6, 0, 0, 0),
(43, 6, 26, 47, 4, 1, 1, 4, 0, 0, 0),
(44, 6, 26, 48, 5, 1, 1, 5, 1, 0, 0),
(45, 6, 26, 49, 6, 1, 1, 6, 2, 0, 0),
(46, 6, 26, 50, 7, 1, 1, 7, 0, 0, 0),
(47, 6, 26, 51, 8, 1, 1, 8, 1, 0, 0),
(48, 6, 26, 52, 9, 0, 0, 5, 2, 0, 0),
(49, 6, 26, 53, 10, 0, 0, 6, 0, 0, 0),
(50, 6, 27, 54, 14, 1, 1, 4, 0, 0, 0),
(51, 6, 27, 55, 15, 1, 1, 5, 1, 0, 0),
(52, 6, 27, 56, 16, 1, 1, 6, 2, 0, 0),
(53, 6, 27, 57, 17, 1, 1, 7, 0, 0, 0),
(54, 6, 27, 58, 18, 1, 1, 8, 1, 0, 0),
(55, 6, 27, 59, 19, 0, 0, 5, 2, 0, 0),
(56, 6, 27, 60, 20, 0, 0, 6, 0, 0, 0),
(57, 7, 20, 5, 4, 0, 0, 0, 0, 0, 0),
(58, 9, 31, 5, 4, 1, 1, 4, 0, 0, 0),
(59, 9, 31, 6, 5, 1, 1, 5, 1, 0, 0),
(60, 9, 31, 7, 6, 1, 1, 6, 2, 0, 0),
(61, 9, 31, 8, 7, 1, 1, 7, 0, 0, 0),
(62, 9, 31, 9, 8, 1, 1, 8, 1, 0, 0),
(63, 9, 31, 10, 9, 0, 0, 5, 2, 0, 0),
(64, 9, 31, 11, 10, 0, 0, 6, 0, 0, 0),
(65, 9, 32, 12, 14, 1, 1, 4, 0, 0, 0),
(66, 9, 32, 13, 15, 1, 1, 5, 1, 0, 0),
(67, 9, 32, 14, 16, 1, 1, 6, 2, 0, 0),
(68, 9, 32, 15, 17, 1, 1, 7, 0, 0, 0),
(69, 9, 32, 16, 18, 1, 1, 8, 1, 0, 0),
(70, 9, 32, 17, 19, 0, 0, 5, 2, 0, 0),
(71, 9, 32, 18, 20, 0, 0, 6, 0, 0, 0),
(72, 10, 33, 19, 24, 1, 1, 4, 0, 0, 0),
(73, 10, 33, 20, 25, 1, 1, 5, 1, 0, 0),
(74, 10, 33, 21, 26, 1, 1, 6, 2, 0, 0),
(75, 10, 33, 22, 27, 1, 1, 7, 0, 0, 0),
(76, 10, 33, 23, 28, 1, 1, 8, 1, 0, 0),
(77, 10, 33, 24, 29, 0, 0, 5, 2, 0, 0),
(78, 10, 33, 25, 30, 0, 0, 6, 0, 0, 0),
(79, 10, 34, 26, 4, 1, 1, 4, 0, 0, 0),
(80, 10, 34, 27, 5, 1, 1, 5, 1, 0, 0),
(81, 10, 34, 28, 6, 1, 1, 6, 2, 0, 0),
(82, 10, 34, 29, 7, 1, 1, 7, 0, 0, 0),
(83, 10, 34, 30, 8, 1, 1, 8, 1, 0, 0),
(84, 10, 34, 31, 9, 0, 0, 5, 2, 0, 0),
(85, 10, 34, 32, 10, 0, 0, 6, 0, 0, 0),
(86, 11, 35, 33, 14, 1, 1, 4, 0, 0, 0),
(87, 11, 35, 34, 15, 1, 1, 5, 1, 0, 0),
(88, 11, 35, 35, 16, 1, 1, 6, 2, 0, 0),
(89, 11, 35, 36, 17, 1, 1, 7, 0, 0, 0),
(90, 11, 35, 37, 18, 1, 1, 8, 1, 0, 0),
(91, 11, 35, 38, 19, 0, 0, 5, 2, 0, 0),
(92, 11, 35, 39, 20, 0, 0, 6, 0, 0, 0),
(93, 11, 36, 40, 24, 1, 1, 4, 0, 0, 0),
(94, 11, 36, 41, 25, 1, 1, 5, 1, 0, 0),
(95, 11, 36, 42, 26, 1, 1, 6, 2, 0, 0),
(96, 11, 36, 43, 27, 1, 1, 7, 0, 0, 0),
(97, 11, 36, 44, 28, 1, 1, 8, 1, 0, 0),
(98, 11, 36, 45, 29, 0, 0, 5, 2, 0, 0),
(99, 11, 36, 46, 30, 0, 0, 6, 0, 0, 0),
(100, 12, 37, 47, 4, 1, 1, 4, 0, 0, 0),
(101, 12, 37, 48, 5, 1, 1, 5, 1, 0, 0),
(102, 12, 37, 49, 6, 1, 1, 6, 2, 0, 0),
(103, 12, 37, 50, 7, 1, 1, 7, 0, 0, 0),
(104, 12, 37, 51, 8, 1, 1, 8, 1, 0, 0),
(105, 12, 37, 52, 9, 0, 0, 5, 2, 0, 0),
(106, 12, 37, 53, 10, 0, 0, 6, 0, 0, 0),
(107, 12, 38, 54, 14, 1, 1, 4, 0, 0, 0),
(108, 12, 38, 55, 15, 1, 1, 5, 1, 0, 0),
(109, 12, 38, 56, 16, 1, 1, 6, 2, 0, 0),
(110, 12, 38, 57, 17, 1, 1, 7, 0, 0, 0),
(111, 12, 38, 58, 18, 1, 1, 8, 1, 0, 0),
(112, 12, 38, 59, 19, 0, 0, 5, 2, 0, 0),
(113, 12, 38, 60, 20, 0, 0, 6, 0, 0, 0),
(114, 15, 39, 64, 7, 1, 0, 19, 2, 0, 0),
(115, 16, 40, 65, 12, 1, 0, 14, 3, 0, 0),
(116, 17, 41, 66, 23, 1, 0, 22, 1, 0, 0),
(117, 18, 42, 67, 34, 1, 0, 17, 4, 0, 0),
(118, 19, 43, 68, 45, 1, 0, 25, 2, 0, 0),
(119, 20, 44, 69, 6, 0, 0, 11, 5, 1, 0),
(248, 37, 53, 134, 4, 1, 1, 14, 4, 0, 0),
(249, 37, 53, 135, 8, 1, 1, 9, 4, 0, 0),
(250, 37, 53, 136, 11, 1, 1, 11, 0, 0, 0),
(251, 37, 53, 137, 15, 1, 1, 8, 0, 0, 0),
(252, 37, 53, 138, 21, 1, 1, 10, 2, 0, 0),
(253, 37, 53, 139, 30, 0, 0, 6, 2, 0, 0),
(254, 37, 54, 140, 5, 1, 1, 13, 4, 0, 0),
(255, 37, 54, 141, 9, 1, 1, 9, 4, 0, 0),
(256, 37, 54, 142, 13, 1, 1, 11, 0, 0, 0),
(257, 37, 54, 143, 18, 1, 1, 8, 0, 0, 0),
(258, 37, 54, 144, 24, 1, 1, 5, 0, 0, 0),
(259, 37, 54, 145, 33, 0, 0, 6, 2, 0, 0),
(260, 38, 53, 134, 4, 1, 1, 14, 4, 0, 0),
(261, 38, 53, 135, 8, 1, 1, 9, 4, 0, 0),
(262, 38, 53, 136, 11, 1, 1, 6, 4, 0, 0),
(263, 38, 53, 137, 15, 1, 1, 8, 0, 0, 0),
(264, 38, 53, 138, 21, 1, 1, 6, 1, 0, 0),
(265, 38, 53, 139, 30, 0, 0, 6, 2, 0, 0),
(266, 38, 55, 146, 6, 1, 1, 12, 4, 0, 0),
(267, 38, 55, 147, 10, 1, 1, 14, 0, 0, 0),
(268, 38, 55, 148, 14, 1, 1, 10, 0, 0, 0),
(269, 38, 55, 149, 19, 1, 1, 12, 2, 0, 0),
(270, 38, 55, 150, 27, 1, 1, 9, 2, 0, 0),
(271, 38, 55, 151, 35, 0, 0, 4, 1, 0, 0),
(272, 39, 54, 140, 5, 1, 1, 12, 4, 0, 0),
(273, 39, 54, 141, 9, 1, 1, 13, 0, 0, 0),
(274, 39, 54, 142, 13, 1, 1, 10, 0, 0, 0),
(275, 39, 54, 143, 18, 1, 1, 7, 0, 0, 0),
(276, 39, 54, 144, 24, 1, 1, 9, 2, 0, 0),
(277, 39, 54, 145, 33, 0, 0, 4, 1, 0, 0),
(278, 39, 55, 146, 6, 1, 1, 12, 4, 0, 0),
(279, 39, 55, 147, 10, 1, 1, 7, 3, 0, 0),
(280, 39, 55, 148, 14, 1, 1, 9, 0, 0, 0),
(281, 39, 55, 149, 19, 1, 1, 6, 0, 0, 0),
(282, 39, 55, 150, 27, 1, 1, 8, 1, 0, 0),
(283, 39, 55, 151, 35, 0, 0, 5, 1, 0, 0),
(284, 40, 56, 152, 7, 1, 1, 16, 0, 0, 0),
(285, 40, 56, 153, 12, 1, 1, 11, 4, 0, 0),
(286, 40, 56, 154, 16, 1, 1, 13, 1, 0, 0),
(287, 40, 56, 155, 22, 1, 1, 10, 1, 0, 0),
(288, 40, 56, 156, 28, 1, 1, 6, 1, 0, 0),
(289, 40, 56, 157, 41, 0, 0, 7, 2, 0, 0),
(290, 40, 57, 158, 3, 1, 1, 14, 4, 0, 0),
(291, 40, 57, 159, 17, 1, 1, 11, 4, 0, 0),
(292, 40, 57, 160, 23, 1, 1, 8, 4, 0, 0),
(293, 40, 57, 161, 31, 1, 1, 10, 1, 0, 0),
(294, 40, 57, 162, 44, 1, 1, 6, 1, 0, 0),
(295, 40, 57, 163, 55, 0, 0, 8, 2, 0, 0),
(296, 41, 56, 152, 7, 1, 1, 11, 3, 0, 0),
(297, 41, 56, 153, 12, 1, 1, 12, 0, 0, 0),
(298, 41, 56, 154, 16, 1, 1, 9, 0, 0, 0),
(299, 41, 56, 155, 22, 1, 1, 11, 1, 0, 0),
(300, 41, 56, 156, 28, 1, 1, 8, 1, 0, 0),
(301, 41, 56, 157, 41, 0, 0, 3, 1, 0, 0),
(302, 41, 58, 164, 2, 1, 1, 13, 4, 0, 0),
(303, 41, 58, 165, 20, 1, 1, 14, 0, 0, 0),
(304, 41, 58, 166, 25, 1, 1, 11, 0, 0, 0),
(305, 41, 58, 167, 32, 1, 1, 7, 0, 0, 0),
(306, 41, 58, 168, 45, 1, 1, 9, 2, 0, 0),
(307, 41, 58, 169, 51, 0, 0, 5, 1, 0, 0),
(308, 42, 57, 158, 3, 1, 1, 14, 4, 0, 0),
(309, 42, 57, 159, 17, 1, 1, 10, 4, 0, 0),
(310, 42, 57, 160, 23, 1, 1, 12, 1, 0, 0),
(311, 42, 57, 161, 31, 1, 1, 9, 1, 0, 0),
(312, 42, 57, 162, 44, 1, 1, 11, 2, 0, 0),
(313, 42, 57, 163, 55, 0, 0, 6, 2, 0, 0),
(314, 42, 58, 164, 2, 1, 1, 16, 0, 0, 0),
(315, 42, 58, 165, 20, 1, 1, 11, 4, 0, 0),
(316, 42, 58, 166, 25, 1, 1, 14, 1, 0, 0),
(317, 42, 58, 167, 32, 1, 1, 10, 1, 0, 0),
(318, 42, 58, 168, 45, 1, 1, 6, 1, 0, 0),
(319, 42, 58, 169, 51, 0, 0, 7, 2, 0, 0),
(320, 43, 55, 146, 6, 1, 1, 18, 1, 0, 0),
(321, 43, 55, 147, 10, 1, 1, 12, 0, 0, 0),
(322, 43, 55, 148, 14, 1, 1, 9, 0, 0, 0),
(323, 43, 55, 149, 19, 1, 1, 11, 1, 0, 0),
(324, 43, 55, 150, 27, 1, 1, 7, 1, 0, 0),
(325, 43, 55, 151, 35, 0, 0, 9, 3, 0, 0),
(326, 43, 56, 152, 7, 1, 1, 12, 4, 0, 0),
(327, 43, 56, 153, 12, 1, 1, 13, 0, 0, 0),
(328, 43, 56, 154, 16, 1, 1, 10, 0, 0, 0),
(329, 43, 56, 155, 22, 1, 1, 12, 2, 0, 0),
(330, 43, 56, 156, 28, 1, 1, 9, 2, 0, 0),
(331, 43, 56, 157, 41, 0, 0, 4, 1, 0, 0),
(332, 44, 58, 164, 2, 1, 1, 0, 0, 0, 0),
(333, 44, 58, 165, 20, 1, 1, 0, 1, 0, 0),
(334, 44, 58, 166, 25, 1, 1, 0, 2, 0, 0),
(335, 44, 58, 167, 32, 1, 1, 0, 3, 0, 0),
(336, 44, 58, 168, 45, 1, 1, 0, 4, 0, 0),
(337, 44, 58, 169, 51, 0, 0, 0, 0, 0, 0),
(338, 44, 54, 140, 5, 1, 1, 4, 1, 0, 0),
(339, 44, 54, 141, 9, 1, 1, 3, 2, 0, 0),
(340, 44, 54, 142, 13, 1, 1, 4, 3, 0, 0),
(341, 44, 54, 143, 18, 1, 1, 3, 4, 0, 0),
(342, 44, 54, 144, 24, 1, 1, 4, 0, 0, 0),
(343, 44, 54, 145, 33, 0, 0, 2, 0, 0, 0),
(344, 45, 56, 152, 7, 1, 1, 11, 3, 0, 0),
(345, 45, 56, 153, 12, 1, 1, 9, 4, 0, 0),
(346, 45, 56, 154, 16, 1, 1, 11, 0, 0, 0),
(347, 45, 56, 155, 22, 1, 1, 8, 0, 0, 0),
(348, 45, 56, 156, 28, 1, 1, 5, 0, 0, 0),
(349, 45, 56, 157, 41, 0, 0, 6, 2, 0, 0),
(350, 45, 58, 164, 2, 1, 1, 12, 4, 0, 0),
(351, 45, 58, 165, 20, 1, 1, 8, 3, 0, 0),
(352, 45, 58, 166, 25, 1, 1, 6, 4, 0, 0),
(353, 45, 58, 167, 32, 1, 1, 7, 0, 0, 0),
(354, 45, 58, 168, 45, 1, 1, 5, 0, 0, 0),
(355, 45, 58, 169, 51, 0, 0, 6, 2, 0, 0),
(356, 46, 55, 146, 6, 1, 1, 14, 4, 0, 0),
(357, 46, 55, 147, 10, 1, 1, 15, 1, 0, 0),
(358, 46, 55, 148, 14, 1, 1, 11, 0, 0, 0),
(359, 46, 55, 149, 19, 1, 1, 14, 2, 0, 0),
(360, 46, 55, 150, 27, 1, 1, 10, 2, 0, 0),
(361, 46, 55, 151, 35, 0, 0, 4, 1, 0, 0),
(362, 46, 54, 140, 5, 1, 1, 14, 4, 0, 0),
(363, 46, 54, 141, 9, 1, 1, 16, 1, 0, 0),
(364, 46, 54, 142, 13, 1, 1, 12, 1, 0, 0),
(365, 46, 54, 143, 18, 1, 1, 8, 0, 0, 0),
(366, 46, 54, 144, 24, 1, 1, 10, 2, 0, 0),
(367, 46, 54, 145, 33, 0, 0, 5, 1, 0, 0);

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

--
-- Dump dei dati per la tabella `match_player_stats`
--

INSERT INTO `match_player_stats` (`id`, `match_id`, `team_id`, `player_id`, `jersey_number`, `is_starter`, `did_not_play`, `minutes_seconds`, `fg_made`, `fg_att`, `two_made`, `two_att`, `three_made`, `three_att`, `ft_made`, `ft_att`, `reb_off`, `reb_def`, `assists`, `turnovers`, `steals`, `blocks`, `fouls_committed`, `fouls_drawn`, `plus_minus`, `evaluation`, `points`, `created_at`, `updated_at`) VALUES
(1, 15, 39, 64, 7, 1, 0, 1245, 7, 13, 4, 7, 3, 6, 2, 3, 1, 4, 5, 2, 3, 0, 2, 4, 8, 21, 19, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(2, 16, 40, 65, 12, 1, 0, 1188, 5, 11, 3, 6, 2, 5, 2, 4, 0, 3, 4, 3, 1, 0, 3, 2, -3, 13, 14, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(3, 17, 41, 66, 23, 1, 0, 1322, 8, 15, 5, 8, 3, 7, 3, 4, 2, 5, 6, 1, 2, 1, 1, 5, 12, 28, 22, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(4, 18, 42, 67, 34, 1, 0, 1015, 6, 14, 4, 9, 2, 5, 3, 5, 1, 6, 2, 4, 1, 2, 4, 3, -5, 16, 17, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(5, 19, 43, 68, 45, 1, 0, 1401, 9, 16, 6, 10, 3, 6, 4, 5, 3, 7, 3, 2, 2, 1, 2, 6, 14, 31, 25, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(6, 20, 44, 69, 6, 0, 0, 845, 4, 9, 3, 5, 1, 4, 2, 2, 0, 2, 5, 2, 1, 0, 5, 1, 4, 10, 11, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(135, 37, 53, 134, 4, 1, 0, 1574, 3, 5, 0, 1, 3, 4, 5, 5, 2, 3, 3, 2, 2, 0, 4, 4, 4, 20, 14, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(136, 37, 53, 135, 8, 1, 0, 1406, 1, 5, 1, 3, 0, 2, 7, 8, 2, 6, 3, 2, 2, 0, 4, 0, 5, 11, 9, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(137, 37, 53, 136, 11, 1, 0, 1665, 3, 4, 3, 4, 0, 0, 5, 5, 1, 4, 4, 3, 1, 1, 0, 2, 6, 20, 11, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(138, 37, 53, 137, 15, 1, 0, 1619, 4, 7, 4, 6, 0, 1, 0, 0, 3, 3, 5, 0, 1, 0, 0, 3, 7, 20, 8, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(139, 37, 53, 138, 21, 1, 0, 1278, 1, 4, 0, 1, 1, 3, 7, 7, 2, 1, 0, 1, 0, 1, 2, 1, 8, 9, 10, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(140, 37, 53, 139, 30, 0, 0, 1711, 2, 4, 2, 4, 0, 0, 2, 3, 3, 5, 0, 1, 0, 0, 2, 2, 9, 10, 6, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(141, 37, 54, 140, 5, 1, 0, 1513, 3, 5, 0, 1, 3, 4, 4, 4, 1, 2, 3, 1, 2, 0, 4, 4, -8, 18, 13, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(142, 37, 54, 141, 9, 1, 0, 1406, 1, 5, 1, 3, 0, 2, 7, 8, 2, 6, 3, 2, 2, 0, 4, 0, -7, 11, 9, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(143, 37, 54, 142, 13, 1, 0, 1665, 3, 4, 3, 4, 0, 0, 5, 5, 1, 4, 4, 3, 1, 1, 0, 2, -6, 20, 11, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(144, 37, 54, 143, 18, 1, 0, 1619, 4, 7, 4, 6, 0, 1, 0, 0, 3, 3, 5, 0, 1, 0, 0, 3, -5, 20, 8, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(145, 37, 54, 144, 24, 1, 0, 1513, 1, 4, 0, 1, 1, 3, 2, 2, 1, 2, 5, 0, 2, 0, 0, 4, -4, 16, 5, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(146, 37, 54, 145, 33, 0, 0, 1711, 2, 4, 2, 4, 0, 0, 2, 3, 3, 5, 0, 1, 0, 0, 2, 2, -3, 10, 6, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(147, 38, 53, 134, 4, 1, 0, 1574, 3, 5, 0, 1, 3, 4, 5, 5, 2, 3, 3, 2, 2, 0, 4, 4, -14, 20, 14, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(148, 38, 53, 135, 8, 1, 0, 1406, 1, 5, 1, 3, 0, 2, 7, 8, 2, 6, 3, 2, 2, 0, 4, 0, -13, 11, 9, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(149, 38, 53, 136, 11, 1, 0, 1360, 3, 4, 3, 4, 0, 0, 0, 0, 0, 5, 3, 2, 0, 1, 4, 1, -12, 9, 6, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(150, 38, 53, 137, 15, 1, 0, 1619, 4, 7, 4, 6, 0, 1, 0, 0, 3, 3, 5, 0, 1, 0, 0, 3, -11, 20, 8, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(151, 38, 53, 138, 21, 1, 0, 1574, 0, 3, 0, 1, 0, 2, 6, 6, 2, 3, 5, 0, 2, 1, 1, 0, -10, 15, 6, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(152, 38, 53, 139, 30, 0, 0, 1711, 2, 4, 2, 4, 0, 0, 2, 3, 3, 5, 0, 1, 0, 0, 2, 2, -9, 10, 6, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(153, 38, 55, 146, 6, 1, 0, 1452, 5, 7, 3, 4, 2, 3, 0, 0, 0, 1, 3, 1, 2, 0, 4, 4, 10, 15, 12, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(154, 38, 55, 147, 10, 1, 0, 1711, 5, 9, 2, 4, 3, 5, 1, 2, 3, 5, 4, 3, 0, 1, 0, 1, 11, 20, 14, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(155, 38, 55, 148, 14, 1, 0, 1604, 2, 3, 2, 3, 0, 0, 6, 6, 0, 3, 4, 3, 1, 1, 0, 2, 12, 17, 10, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(156, 38, 55, 149, 19, 1, 0, 1263, 4, 7, 4, 6, 0, 1, 4, 5, 3, 1, 0, 0, 2, 1, 2, 0, 13, 13, 12, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(157, 38, 55, 150, 27, 1, 0, 1757, 3, 6, 2, 3, 1, 3, 2, 2, 1, 6, 0, 1, 2, 1, 2, 1, 14, 14, 9, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(158, 38, 55, 151, 35, 0, 0, 1589, 2, 4, 2, 4, 0, 0, 0, 0, 1, 3, 0, 1, 2, 1, 1, 1, 15, 8, 4, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(159, 39, 54, 140, 5, 1, 0, 1452, 5, 7, 3, 4, 2, 3, 0, 0, 0, 1, 3, 1, 2, 0, 4, 4, 6, 15, 12, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(160, 39, 54, 141, 9, 1, 0, 1650, 5, 9, 2, 4, 3, 5, 0, 0, 2, 4, 4, 2, 0, 1, 0, 1, 7, 19, 13, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(161, 39, 54, 142, 13, 1, 0, 1604, 2, 3, 2, 3, 0, 0, 6, 6, 0, 3, 4, 3, 1, 1, 0, 2, 8, 17, 10, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(162, 39, 54, 143, 18, 1, 0, 1558, 2, 5, 1, 3, 1, 2, 2, 3, 2, 2, 4, 0, 1, 0, 0, 3, 9, 15, 7, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(163, 39, 54, 144, 24, 1, 0, 1757, 3, 6, 2, 3, 1, 3, 2, 2, 1, 6, 0, 1, 2, 1, 2, 1, 10, 14, 9, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(164, 39, 54, 145, 33, 0, 0, 1589, 2, 4, 2, 4, 0, 0, 0, 0, 1, 3, 0, 1, 2, 1, 1, 1, 11, 8, 4, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(165, 39, 55, 146, 6, 1, 0, 1452, 5, 7, 3, 4, 2, 3, 0, 0, 0, 1, 3, 1, 2, 0, 4, 4, -10, 15, 12, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(166, 39, 55, 147, 10, 1, 0, 1284, 1, 5, 1, 3, 0, 2, 5, 6, 0, 4, 2, 2, 2, 0, 3, 4, -9, 9, 7, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(167, 39, 55, 148, 14, 1, 0, 1543, 3, 4, 3, 4, 0, 0, 3, 3, 3, 2, 4, 3, 0, 1, 0, 2, -8, 17, 9, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(168, 39, 55, 149, 19, 1, 0, 1497, 2, 5, 1, 3, 1, 2, 1, 2, 1, 1, 4, 3, 1, 0, 0, 3, -7, 9, 6, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(169, 39, 55, 150, 27, 1, 0, 1696, 2, 5, 1, 2, 1, 3, 3, 3, 0, 5, 0, 1, 2, 1, 1, 0, -6, 11, 8, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(170, 39, 55, 151, 35, 0, 0, 1650, 0, 2, 0, 2, 0, 0, 5, 6, 2, 4, 0, 1, 0, 1, 1, 1, -5, 8, 5, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(171, 40, 56, 152, 7, 1, 0, 1696, 5, 7, 2, 3, 3, 4, 3, 3, 0, 5, 4, 2, 0, 0, 0, 0, 4, 21, 16, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(172, 40, 56, 153, 12, 1, 0, 1528, 4, 8, 1, 3, 3, 5, 0, 0, 0, 2, 3, 2, 0, 0, 4, 0, 5, 6, 11, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(173, 40, 56, 154, 16, 1, 0, 1787, 3, 4, 3, 4, 0, 0, 7, 7, 3, 6, 5, 3, 1, 0, 1, 3, 6, 26, 13, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(174, 40, 56, 155, 22, 1, 0, 1681, 4, 7, 4, 6, 0, 1, 2, 3, 1, 5, 5, 0, 2, 0, 1, 4, 7, 22, 10, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(175, 40, 56, 156, 28, 1, 0, 1574, 0, 3, 0, 1, 0, 2, 6, 6, 2, 3, 5, 0, 2, 1, 1, 0, 8, 15, 6, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(176, 40, 56, 157, 41, 0, 0, 1772, 1, 3, 1, 3, 0, 0, 5, 6, 0, 6, 0, 2, 0, 0, 2, 2, 9, 8, 7, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(177, 40, 57, 158, 3, 1, 0, 1574, 3, 5, 0, 1, 3, 4, 5, 5, 2, 3, 3, 2, 2, 0, 4, 4, -8, 20, 14, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(178, 40, 57, 159, 17, 1, 0, 1528, 4, 8, 1, 3, 3, 5, 0, 0, 0, 2, 3, 2, 0, 0, 4, 0, -7, 6, 11, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(179, 40, 57, 160, 23, 1, 0, 1482, 2, 3, 2, 3, 0, 0, 4, 4, 2, 1, 4, 3, 0, 1, 4, 1, -6, 9, 8, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(180, 40, 57, 161, 31, 1, 0, 1681, 4, 7, 4, 6, 0, 1, 2, 3, 1, 5, 5, 0, 2, 0, 1, 4, -5, 22, 10, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(181, 40, 57, 162, 44, 1, 0, 1574, 0, 3, 0, 1, 0, 2, 6, 6, 2, 3, 5, 0, 2, 1, 1, 0, -4, 15, 6, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(182, 40, 57, 163, 55, 0, 0, 1293, 2, 4, 0, 2, 2, 2, 2, 3, 1, 1, 1, 2, 0, 0, 2, 2, -3, 6, 8, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(183, 41, 56, 152, 7, 1, 0, 1391, 4, 6, 2, 3, 2, 3, 1, 1, 3, 6, 2, 1, 2, 1, 3, 3, -7, 22, 11, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(184, 41, 56, 153, 12, 1, 0, 1589, 4, 8, 1, 3, 3, 5, 1, 2, 1, 3, 4, 2, 0, 1, 0, 1, -6, 15, 12, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(185, 41, 56, 154, 16, 1, 0, 1543, 3, 4, 3, 4, 0, 0, 3, 3, 3, 2, 4, 3, 0, 1, 0, 2, -5, 17, 9, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(186, 41, 56, 155, 22, 1, 0, 1742, 5, 8, 5, 7, 0, 1, 1, 2, 2, 6, 5, 0, 2, 0, 1, 4, -4, 25, 11, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(187, 41, 56, 156, 28, 1, 0, 1696, 2, 5, 1, 2, 1, 3, 3, 3, 0, 5, 0, 1, 2, 1, 1, 0, -3, 11, 8, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(188, 41, 56, 157, 41, 0, 0, 1528, 1, 3, 1, 3, 0, 0, 1, 2, 0, 2, 5, 1, 2, 1, 1, 1, -2, 9, 3, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(189, 41, 58, 164, 2, 1, 0, 1513, 3, 5, 0, 1, 3, 4, 4, 4, 1, 2, 3, 1, 2, 0, 4, 4, 3, 18, 13, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(190, 41, 58, 165, 20, 1, 0, 1711, 5, 9, 2, 4, 3, 5, 1, 2, 3, 5, 4, 3, 0, 1, 0, 1, 4, 20, 14, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(191, 41, 58, 166, 25, 1, 0, 1665, 3, 4, 3, 4, 0, 0, 5, 5, 1, 4, 4, 3, 1, 1, 0, 2, 5, 20, 11, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(192, 41, 58, 167, 32, 1, 0, 1558, 2, 5, 1, 3, 1, 2, 2, 3, 2, 2, 4, 0, 1, 0, 0, 3, 6, 15, 7, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(193, 41, 58, 168, 45, 1, 0, 1757, 3, 6, 2, 3, 1, 3, 2, 2, 1, 6, 0, 1, 2, 1, 2, 1, 7, 14, 9, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(194, 41, 58, 169, 51, 0, 0, 1650, 0, 2, 0, 2, 0, 0, 5, 6, 2, 4, 0, 1, 0, 1, 1, 1, 8, 8, 5, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(195, 42, 57, 158, 3, 1, 0, 1574, 3, 5, 0, 1, 3, 4, 5, 5, 2, 3, 3, 2, 2, 0, 4, 4, -4, 20, 14, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(196, 42, 57, 159, 17, 1, 0, 1467, 3, 7, 0, 2, 3, 5, 1, 2, 3, 1, 3, 2, 0, 0, 4, 0, -3, 6, 10, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(197, 42, 57, 160, 23, 1, 0, 1726, 2, 3, 2, 3, 0, 0, 8, 8, 2, 5, 5, 3, 1, 0, 1, 3, -2, 23, 12, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(198, 42, 57, 161, 31, 1, 0, 1620, 1, 4, 0, 2, 1, 2, 6, 7, 0, 4, 5, 0, 1, 0, 1, 4, -1, 18, 9, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(199, 42, 57, 162, 44, 1, 0, 1339, 2, 5, 1, 2, 1, 3, 6, 6, 3, 2, 0, 1, 0, 1, 2, 1, 0, 12, 11, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(200, 42, 57, 163, 55, 0, 0, 1711, 2, 4, 2, 4, 0, 0, 2, 3, 3, 5, 0, 1, 0, 0, 2, 2, 1, 10, 6, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(201, 42, 58, 164, 2, 1, 0, 1696, 5, 7, 2, 3, 3, 4, 3, 3, 0, 5, 4, 2, 0, 0, 0, 0, 0, 21, 16, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(202, 42, 58, 165, 20, 1, 0, 1528, 4, 8, 1, 3, 3, 5, 0, 0, 0, 2, 3, 2, 0, 0, 4, 0, 1, 6, 11, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(203, 42, 58, 166, 25, 1, 0, 1308, 5, 6, 1, 2, 4, 4, 0, 0, 0, 1, 5, 0, 1, 0, 1, 3, 2, 22, 14, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(204, 42, 58, 167, 32, 1, 0, 1681, 4, 7, 4, 6, 0, 1, 2, 3, 1, 5, 5, 0, 2, 0, 1, 4, 3, 22, 10, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(205, 42, 58, 168, 45, 1, 0, 1574, 0, 3, 0, 1, 0, 2, 6, 6, 2, 3, 5, 0, 2, 1, 1, 0, 4, 15, 6, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(206, 42, 58, 169, 51, 0, 0, 1772, 1, 3, 1, 3, 0, 0, 5, 6, 0, 6, 0, 2, 0, 0, 2, 2, 5, 8, 7, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(207, 43, 55, 146, 6, 1, 0, 1278, 4, 6, 0, 1, 4, 5, 6, 6, 2, 1, 4, 2, 0, 1, 1, 1, 4, 22, 18, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(208, 43, 55, 147, 10, 1, 0, 1589, 4, 8, 1, 3, 3, 5, 1, 2, 1, 3, 4, 2, 0, 1, 0, 1, 5, 15, 12, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(209, 43, 55, 148, 14, 1, 0, 1543, 3, 4, 3, 4, 0, 0, 3, 3, 3, 2, 4, 3, 0, 1, 0, 2, 6, 17, 9, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(210, 43, 55, 149, 19, 1, 0, 1742, 5, 8, 5, 7, 0, 1, 1, 2, 2, 6, 5, 0, 2, 0, 1, 4, 7, 25, 11, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(211, 43, 55, 150, 27, 1, 0, 1635, 2, 5, 0, 1, 2, 4, 1, 1, 3, 4, 5, 1, 2, 1, 1, 0, 8, 17, 7, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(212, 43, 55, 151, 35, 0, 0, 1354, 2, 4, 0, 2, 2, 2, 3, 4, 2, 2, 1, 2, 0, 0, 3, 3, 9, 9, 9, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(213, 43, 56, 152, 7, 1, 0, 1452, 5, 7, 3, 4, 2, 3, 0, 0, 0, 1, 3, 1, 2, 0, 4, 4, -8, 15, 12, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(214, 43, 56, 153, 12, 1, 0, 1650, 5, 9, 2, 4, 3, 5, 0, 0, 2, 4, 4, 2, 0, 1, 0, 1, -7, 19, 13, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(215, 43, 56, 154, 16, 1, 0, 1604, 2, 3, 2, 3, 0, 0, 6, 6, 0, 3, 4, 3, 1, 1, 0, 2, -6, 17, 10, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(216, 43, 56, 155, 22, 1, 0, 1263, 4, 7, 4, 6, 0, 1, 4, 5, 3, 1, 0, 0, 2, 1, 2, 0, -5, 13, 12, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(217, 43, 56, 156, 28, 1, 0, 1757, 3, 6, 2, 3, 1, 3, 2, 2, 1, 6, 0, 1, 2, 1, 2, 1, -4, 14, 9, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(218, 43, 56, 157, 41, 0, 0, 1589, 2, 4, 2, 4, 0, 0, 0, 0, 1, 3, 0, 1, 2, 1, 1, 1, -3, 8, 4, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(219, 44, 58, 164, 2, 1, 0, 1260, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, -22, 0, 0, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(220, 44, 58, 165, 20, 1, 0, 1397, 0, 1, 0, 1, 0, 0, 0, 0, 1, 3, 1, 1, 1, 1, 1, 2, -21, 6, 0, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(221, 44, 58, 166, 25, 1, 0, 1534, 0, 1, 0, 1, 0, 0, 0, 0, 2, 5, 2, 2, 2, 0, 2, 4, -20, 10, 0, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(222, 44, 58, 167, 32, 1, 0, 1671, 0, 1, 0, 1, 0, 0, 0, 0, 3, 1, 3, 3, 0, 1, 3, 1, -19, 2, 0, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(223, 44, 58, 168, 45, 1, 0, 1748, 0, 1, 0, 1, 0, 0, 0, 0, 0, 3, 4, 0, 1, 0, 4, 3, -18, 6, 0, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(224, 44, 58, 169, 51, 0, 0, 1345, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 5, 1, 2, 1, 0, 0, -17, 8, 0, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(225, 44, 54, 140, 5, 1, 0, 1504, 0, 2, 0, 1, 0, 1, 4, 4, 0, 5, 1, 0, 0, 0, 1, 1, 18, 8, 4, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(226, 44, 54, 141, 9, 1, 0, 1580, 1, 5, 0, 2, 1, 3, 0, 0, 0, 6, 1, 1, 1, 1, 2, 3, 19, 8, 3, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(227, 44, 54, 142, 13, 1, 0, 1778, 0, 1, 0, 1, 0, 0, 4, 4, 2, 3, 3, 2, 2, 0, 3, 0, 20, 8, 4, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(228, 44, 54, 143, 18, 1, 0, 1314, 0, 3, 0, 2, 0, 1, 3, 4, 2, 4, 3, 3, 0, 1, 4, 2, 21, 4, 3, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(229, 44, 54, 144, 24, 1, 0, 1452, 0, 3, 0, 1, 0, 2, 4, 4, 0, 1, 5, 0, 1, 0, 0, 4, 22, 12, 4, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(230, 44, 54, 145, 33, 0, 0, 1467, 0, 2, 0, 2, 0, 0, 2, 3, 3, 1, 5, 1, 2, 1, 0, 0, 23, 10, 2, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(231, 45, 56, 152, 7, 1, 0, 1391, 4, 6, 2, 3, 2, 3, 1, 1, 3, 6, 2, 1, 2, 1, 3, 3, 4, 22, 11, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(232, 45, 56, 153, 12, 1, 0, 1406, 1, 5, 1, 3, 0, 2, 7, 8, 2, 6, 3, 2, 2, 0, 4, 0, 5, 11, 9, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(233, 45, 56, 154, 16, 1, 0, 1665, 3, 4, 3, 4, 0, 0, 5, 5, 1, 4, 4, 3, 1, 1, 0, 2, 6, 20, 11, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(234, 45, 56, 155, 22, 1, 0, 1619, 4, 7, 4, 6, 0, 1, 0, 0, 3, 3, 5, 0, 1, 0, 0, 3, 7, 20, 8, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(235, 45, 56, 156, 28, 1, 0, 1513, 1, 4, 0, 1, 1, 3, 2, 2, 1, 2, 5, 0, 2, 0, 0, 4, 8, 16, 5, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(236, 45, 56, 157, 41, 0, 0, 1711, 2, 4, 2, 4, 0, 0, 2, 3, 3, 5, 0, 1, 0, 0, 2, 2, 9, 10, 6, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(237, 45, 58, 164, 2, 1, 0, 1452, 5, 7, 3, 4, 2, 3, 0, 0, 0, 1, 3, 1, 2, 0, 4, 4, -8, 15, 12, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(238, 45, 58, 165, 20, 1, 0, 1345, 0, 4, 0, 2, 0, 2, 8, 9, 1, 5, 3, 2, 2, 0, 3, 4, -7, 13, 8, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(239, 45, 58, 166, 25, 1, 0, 1360, 3, 4, 3, 4, 0, 0, 0, 0, 0, 5, 3, 2, 0, 1, 4, 1, -6, 9, 6, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(240, 45, 58, 167, 32, 1, 0, 1558, 2, 5, 1, 3, 1, 2, 2, 3, 2, 2, 4, 0, 1, 0, 0, 3, -5, 15, 7, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(241, 45, 58, 168, 45, 1, 0, 1513, 1, 4, 0, 1, 1, 3, 2, 2, 1, 2, 5, 0, 2, 0, 0, 4, -4, 16, 5, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(242, 45, 58, 169, 51, 0, 0, 1711, 2, 4, 2, 4, 0, 0, 2, 3, 3, 5, 0, 1, 0, 0, 2, 2, -3, 10, 6, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(243, 46, 55, 146, 6, 1, 0, 1574, 3, 5, 0, 1, 3, 4, 5, 5, 2, 3, 3, 2, 2, 0, 4, 4, 1, 20, 14, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(244, 46, 55, 147, 10, 1, 0, 1772, 4, 8, 0, 2, 4, 6, 3, 4, 0, 6, 4, 3, 1, 1, 1, 2, 2, 20, 15, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(245, 46, 55, 148, 14, 1, 0, 1665, 3, 4, 3, 4, 0, 0, 5, 5, 1, 4, 4, 3, 1, 1, 0, 2, 3, 20, 11, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(246, 46, 55, 149, 19, 1, 0, 1385, 4, 7, 4, 6, 0, 1, 6, 7, 1, 3, 0, 1, 2, 1, 2, 0, 4, 14, 14, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(247, 46, 55, 150, 27, 1, 0, 1278, 1, 4, 0, 1, 1, 3, 7, 7, 2, 1, 0, 1, 0, 1, 2, 1, 5, 9, 10, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(248, 46, 55, 151, 35, 0, 0, 1589, 2, 4, 2, 4, 0, 0, 0, 0, 1, 3, 0, 1, 2, 1, 1, 1, 6, 8, 4, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(249, 46, 54, 140, 5, 1, 0, 1574, 3, 5, 0, 1, 3, 4, 5, 5, 2, 3, 3, 2, 2, 0, 4, 4, -5, 20, 14, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(250, 46, 54, 141, 9, 1, 0, 1293, 4, 8, 0, 2, 4, 6, 4, 5, 1, 1, 5, 3, 1, 1, 1, 2, -4, 18, 16, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(251, 46, 54, 142, 13, 1, 0, 1726, 2, 3, 2, 3, 0, 0, 8, 8, 2, 5, 5, 3, 1, 0, 1, 3, -3, 23, 12, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(252, 46, 54, 143, 18, 1, 0, 1619, 4, 7, 4, 6, 0, 1, 0, 0, 3, 3, 5, 0, 1, 0, 0, 3, -2, 20, 8, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(253, 46, 54, 144, 24, 1, 0, 1278, 1, 4, 0, 1, 1, 3, 7, 7, 2, 1, 0, 1, 0, 1, 2, 1, -1, 9, 10, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(254, 46, 54, 145, 33, 0, 0, 1650, 0, 2, 0, 2, 0, 0, 5, 6, 2, 4, 0, 1, 0, 1, 1, 1, 0, 8, 5, '2026-06-08 01:55:15', '2026-06-08 01:55:15');

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

--
-- Dump dei dati per la tabella `match_teams`
--

INSERT INTO `match_teams` (`id`, `match_id`, `team_id`, `side`, `score`, `fouls_current_period`, `timeouts_used_total`, `timeouts_used_period`, `is_winner`, `forfeit_score`) VALUES
(3, 2, 13, 'Home', 0, 0, 0, 0, 0, NULL),
(4, 2, 15, 'Away', 4, 0, 0, 0, 1, NULL),
(5, 3, 20, 'Home', 45, 1, 0, 0, 1, NULL),
(6, 3, 21, 'Away', 39, 0, 0, 0, 0, NULL),
(7, 4, 22, 'Home', 0, 0, 0, 0, 0, NULL),
(8, 4, 23, 'Away', 0, 0, 0, 0, 0, NULL),
(9, 5, 24, 'Home', 37, 0, 0, 0, 0, NULL),
(10, 5, 25, 'Away', 41, 0, 0, 0, 1, NULL),
(11, 6, 26, 'Home', 0, 0, 0, 0, 0, NULL),
(12, 6, 27, 'Away', 0, 0, 0, 0, 0, NULL),
(13, 7, 20, 'Home', 0, 0, 0, 0, 0, NULL),
(14, 7, 25, 'Away', 0, 0, 0, 0, 0, NULL),
(15, 8, 21, 'Home', 0, 0, 0, 0, 0, NULL),
(16, 8, 24, 'Away', 0, 0, 0, 0, 0, NULL),
(17, 9, 23, 'Home', 0, 0, 0, 0, 0, NULL),
(18, 9, 27, 'Away', 0, 0, 0, 0, 0, NULL),
(19, 10, 33, 'Home', 0, 0, 0, 0, 0, NULL),
(20, 10, 34, 'Away', 0, 0, 0, 0, 0, NULL),
(21, 11, 35, 'Home', 37, 0, 0, 0, 0, NULL),
(22, 11, 36, 'Away', 41, 0, 0, 0, 1, NULL),
(23, 12, 37, 'Home', 0, 0, 0, 0, 0, NULL),
(24, 12, 38, 'Away', 0, 0, 0, 0, 0, NULL),
(25, 13, 31, 'Home', 0, 0, 0, 0, 0, NULL),
(26, 13, 36, 'Away', 0, 0, 0, 0, 0, NULL),
(27, 14, 32, 'Home', 0, 0, 0, 0, 0, NULL),
(28, 14, 35, 'Away', 0, 0, 0, 0, 0, NULL),
(29, 15, 39, 'Home', 58, 4, 1, 1, 1, NULL),
(30, 15, 40, 'Away', 54, 5, 2, 1, 0, NULL),
(31, 16, 40, 'Home', 41, 3, 1, 0, 0, NULL),
(32, 16, 41, 'Away', 49, 6, 1, 1, 1, NULL),
(33, 17, 41, 'Home', 63, 5, 2, 1, 1, NULL),
(34, 17, 42, 'Away', 61, 5, 2, 1, 0, NULL),
(35, 18, 42, 'Home', 47, 7, 1, 1, 0, NULL),
(36, 18, 43, 'Away', 52, 4, 2, 0, 1, NULL),
(37, 19, 43, 'Home', 55, 2, 0, 0, 1, NULL),
(38, 19, 44, 'Away', 45, 8, 2, 1, 0, NULL),
(39, 20, 44, 'Home', 60, 5, 2, 1, 0, NULL),
(40, 20, 39, 'Away', 64, 3, 1, 0, 1, NULL),
(75, 37, 53, 'Home', 58, 1, 1, 1, 1, NULL),
(76, 37, 54, 'Away', 52, 3, 2, 0, 0, NULL),
(77, 38, 53, 'Home', 49, 2, 2, 0, 0, NULL),
(78, 38, 55, 'Away', 61, 4, 0, 1, 1, NULL),
(79, 39, 54, 'Home', 55, 3, 0, 1, 1, NULL),
(80, 39, 55, 'Away', 47, 5, 1, 0, 0, NULL),
(81, 40, 56, 'Home', 63, 4, 1, 0, 1, NULL),
(82, 40, 57, 'Away', 57, 0, 2, 1, 0, NULL),
(83, 41, 56, 'Home', 54, 5, 2, 1, 0, NULL),
(84, 41, 58, 'Away', 59, 1, 0, 0, 1, NULL),
(85, 42, 57, 'Home', 62, 0, 0, 0, 0, NULL),
(86, 42, 58, 'Away', 64, 2, 1, 1, 1, NULL),
(87, 43, 55, 'Home', 66, 1, 1, 1, 1, NULL),
(88, 43, 56, 'Away', 60, 3, 2, 0, 0, NULL),
(89, 44, 58, 'Home', 0, 2, 2, 0, 0, 0),
(90, 44, 54, 'Away', 20, 4, 0, 1, 1, 20),
(91, 45, 56, 'Home', 50, 3, 0, 1, 1, NULL),
(92, 45, 58, 'Away', 44, 5, 1, 0, 0, NULL),
(93, 46, 55, 'Home', 68, 4, 1, 0, 1, NULL),
(94, 46, 54, 'Away', 65, 0, 2, 1, 0, NULL);

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

--
-- Dump dei dati per la tabella `match_team_stats`
--

INSERT INTO `match_team_stats` (`id`, `match_id`, `team_id`, `fg_made`, `fg_att`, `two_made`, `two_att`, `three_made`, `three_att`, `ft_made`, `ft_att`, `reb_off`, `reb_def`, `assists`, `turnovers`, `steals`, `blocks`, `fouls_committed`, `fouls_drawn`, `points`, `points_in_paint`, `fast_break_points`, `fast_break_points_off_turnovers`, `second_chance_points`, `points_off_turnovers`, `bench_points`, `biggest_lead`, `biggest_run`, `lead_changes`, `times_tied`, `time_in_lead`, `points_per_possession`, `created_at`, `updated_at`) VALUES
(1, 15, 39, 23, 48, 15, 27, 8, 21, 4, 6, 7, 24, 14, 10, 8, 3, 17, 19, 58, 30, 10, 6, 8, 13, 12, 9, '11-0', 5, 4, '16:42', 1.074, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(2, 15, 40, 21, 52, 16, 31, 5, 21, 7, 10, 9, 21, 11, 12, 6, 2, 19, 17, 54, 32, 8, 3, 6, 11, 14, 5, '8-0', 5, 4, '12:18', 0.981, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(3, 16, 40, 16, 43, 13, 24, 3, 19, 6, 8, 6, 18, 9, 14, 5, 1, 14, 16, 41, 26, 6, 4, 5, 9, 9, 3, '7-0', 3, 6, '07:35', 0.872, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(4, 16, 41, 19, 45, 14, 26, 5, 19, 6, 9, 8, 23, 15, 9, 9, 4, 16, 14, 49, 28, 12, 8, 7, 15, 16, 10, '12-2', 3, 6, '22:25', 1.043, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(5, 17, 41, 25, 55, 17, 33, 8, 22, 5, 8, 10, 26, 17, 8, 10, 5, 18, 20, 63, 34, 14, 9, 10, 16, 18, 12, '14-3', 7, 5, '24:11', 1.167, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(6, 17, 42, 24, 57, 18, 35, 6, 22, 7, 9, 11, 25, 13, 11, 7, 3, 20, 18, 61, 36, 9, 5, 9, 12, 11, 6, '9-0', 7, 5, '13:49', 1.109, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(7, 18, 42, 18, 47, 12, 29, 6, 18, 5, 7, 5, 20, 10, 13, 4, 2, 21, 15, 47, 24, 7, 3, 4, 8, 10, 4, '6-0', 4, 3, '09:04', 0.922, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(8, 18, 43, 20, 49, 13, 28, 7, 21, 5, 8, 8, 24, 16, 7, 8, 4, 15, 21, 52, 26, 11, 7, 8, 14, 15, 11, '13-4', 4, 3, '20:56', 1.061, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(9, 19, 43, 22, 46, 15, 27, 7, 19, 4, 6, 7, 22, 12, 9, 6, 3, 12, 22, 55, 30, 8, 4, 6, 10, 13, 14, '10-0', 2, 2, '25:30', 1.196, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(10, 19, 44, 17, 44, 13, 25, 4, 19, 7, 9, 6, 19, 8, 15, 5, 1, 22, 12, 45, 26, 5, 2, 5, 7, 8, 2, '5-0', 2, 2, '04:30', 0.865, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(11, 20, 44, 23, 54, 16, 32, 7, 22, 7, 10, 9, 23, 15, 10, 7, 2, 18, 14, 60, 32, 13, 8, 9, 15, 17, 5, '9-2', 6, 7, '11:15', 1.132, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(12, 20, 39, 26, 58, 18, 34, 8, 24, 4, 7, 10, 27, 18, 8, 9, 5, 14, 18, 64, 36, 15, 10, 11, 17, 20, 13, '12-0', 6, 7, '18:45', 1.231, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(47, 37, 53, 14, 29, 10, 19, 4, 10, 26, 28, 13, 22, 15, 9, 6, 2, 12, 12, 58, 22, 12, 7, 26, 19, 6, 11, '8-0', 3, 2, '19:07', 1.184, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(48, 37, 54, 14, 29, 10, 19, 4, 10, 20, 22, 11, 22, 20, 7, 8, 1, 10, 15, 52, 22, 11, 6, 22, 25, 6, 11, '8-0', 3, 2, '7:05', 1.000, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(49, 38, 53, 13, 28, 10, 19, 3, 9, 20, 22, 12, 25, 19, 7, 7, 2, 15, 10, 49, 24, 11, 7, 24, 23, 6, 18, '9-0', 4, 3, '8:10', 0.980, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(50, 38, 55, 21, 36, 15, 24, 6, 12, 13, 15, 8, 19, 11, 9, 9, 5, 9, 9, 61, 34, 14, 8, 16, 29, 4, 18, '9-0', 4, 3, '20:14', 1.151, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(51, 39, 54, 19, 34, 12, 21, 7, 13, 10, 11, 6, 19, 15, 8, 8, 4, 7, 12, 55, 24, 14, 6, 12, 27, 4, 15, '10-0', 5, 4, '21:21', 1.078, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(52, 39, 55, 13, 28, 9, 18, 4, 10, 17, 20, 6, 17, 13, 11, 7, 3, 9, 14, 47, 18, 12, 5, 12, 24, 5, 15, '10-0', 5, 4, '9:15', 0.870, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(53, 40, 56, 17, 32, 11, 20, 6, 12, 23, 25, 6, 27, 22, 9, 5, 1, 9, 9, 63, 24, 12, 8, 12, 19, 7, 14, '11-0', 6, 5, '22:28', 1.212, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(54, 40, 57, 15, 30, 7, 16, 8, 14, 19, 21, 8, 15, 21, 9, 6, 2, 16, 11, 57, 16, 11, 7, 16, 22, 8, 14, '11-0', 6, 5, '10:20', 1.036, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(55, 41, 56, 19, 34, 13, 22, 6, 12, 10, 13, 9, 24, 20, 8, 8, 5, 6, 11, 54, 30, 11, 8, 18, 29, 3, 14, '7-0', 7, 1, '11:25', 1.019, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(56, 41, 58, 16, 31, 8, 17, 8, 14, 19, 22, 10, 23, 15, 9, 6, 4, 7, 12, 59, 20, 12, 8, 20, 23, 5, 14, '7-0', 7, 1, '23:35', 1.229, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(57, 42, 57, 13, 28, 5, 14, 8, 14, 28, 31, 13, 20, 16, 9, 4, 1, 14, 14, 62, 10, 14, 6, 26, 18, 6, 6, '8-0', 8, 2, '12:30', 1.148, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(58, 42, 58, 19, 34, 9, 18, 10, 16, 16, 18, 3, 22, 22, 6, 5, 1, 9, 9, 64, 18, 14, 7, 6, 21, 7, 6, '8-0', 8, 2, '24:42', 1.306, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(59, 43, 55, 20, 35, 9, 18, 11, 17, 15, 18, 13, 18, 23, 10, 4, 4, 6, 11, 66, 20, 16, 8, 26, 19, 9, 11, '9-0', 2, 3, '25:49', 1.200, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(60, 43, 56, 21, 36, 15, 24, 6, 12, 12, 13, 7, 18, 11, 8, 9, 5, 9, 9, 60, 32, 15, 7, 14, 34, 4, 11, '9-0', 2, 3, '13:35', 1.200, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(61, 44, 58, 0, 6, 0, 6, 0, 0, 0, 0, 7, 14, 15, 7, 6, 3, 10, 10, 0, 0, 0, 2, 0, 0, 0, 26, '10-0', 3, 4, '14:40', 0.000, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(62, 44, 54, 1, 16, 0, 9, 1, 7, 17, 19, 7, 20, 18, 7, 6, 3, 10, 10, 20, 4, 4, 4, 14, 5, 2, 26, '10-0', 3, 4, '26:56', 0.392, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(63, 45, 56, 15, 30, 12, 21, 3, 9, 17, 19, 13, 26, 19, 7, 8, 2, 9, 14, 50, 24, 11, 5, 26, 33, 6, 13, '11-0', 4, 5, '27:03', 1.020, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(64, 45, 58, 13, 28, 9, 18, 4, 10, 14, 17, 7, 20, 18, 6, 7, 1, 13, 18, 44, 18, 9, 4, 14, 30, 6, 13, '11-0', 4, 5, '15:45', 0.846, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(65, 46, 55, 17, 32, 9, 18, 8, 14, 26, 28, 7, 20, 11, 11, 8, 5, 10, 10, 68, 20, 15, 8, 14, 34, 4, 11, '7-0', 5, 1, '28:10', 1.360, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(66, 46, 54, 14, 29, 6, 15, 8, 14, 29, 31, 12, 17, 18, 10, 5, 3, 9, 14, 65, 14, 15, 8, 24, 25, 5, 11, '7-0', 5, 1, '16:50', 1.226, '2026-06-08 01:55:15', '2026-06-08 01:55:15');

-- --------------------------------------------------------

--
-- Struttura della tabella `match_timeouts`
--

CREATE TABLE `match_timeouts` (
  `id` int UNSIGNED NOT NULL,
  `match_id` int UNSIGNED NOT NULL,
  `team_id` int UNSIGNED NOT NULL,
  `period` int NOT NULL,
  `requested_by_player_id` int UNSIGNED DEFAULT NULL,
  `requested_by_coach_name` varchar(160) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `duration_ms` int NOT NULL DEFAULT '30000',
  `started_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ended_at` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dump dei dati per la tabella `match_timeouts`
--

INSERT INTO `match_timeouts` (`id`, `match_id`, `team_id`, `period`, `requested_by_player_id`, `requested_by_coach_name`, `duration_ms`, `started_at`, `ended_at`) VALUES
(1, 15, 39, 2, 64, 'Enrico Bassi', 30000, '2026-07-04 18:39:10', '2026-07-04 18:39:40'),
(2, 16, 40, 1, 65, 'Paolo Marinelli', 45000, '2026-07-04 19:04:55', '2026-07-04 19:05:40'),
(3, 17, 41, 3, 66, 'Roberto De Luca', 30000, '2026-07-04 20:24:20', '2026-07-04 20:24:50'),
(4, 18, 42, 2, 67, 'Stefano Vitali', 60000, '2026-07-05 18:36:15', '2026-07-05 18:37:15'),
(5, 19, 43, 1, 68, 'Claudio Neri', 30000, '2026-07-05 19:05:40', '2026-07-05 19:06:10'),
(6, 20, 44, 2, 69, 'Gianni Ferri', 45000, '2026-07-05 20:17:05', '2026-07-05 20:17:50'),
(23, 37, 53, 2, 134, 'Coach BLU', 35000, '2026-07-10 18:00:00', '2026-07-10 18:48:00'),
(24, 38, 53, 2, 134, 'Coach BLU', 40000, '2026-07-10 19:05:00', '2026-07-10 19:53:00'),
(25, 39, 54, 2, 140, 'Coach RED', 30000, '2026-07-11 18:00:00', '2026-07-11 18:50:00'),
(26, 40, 56, 2, 152, 'Coach NER', 35000, '2026-07-11 19:05:00', '2026-07-11 19:54:00'),
(27, 41, 56, 2, 152, 'Coach NER', 40000, '2026-07-12 18:00:00', '2026-07-12 18:47:00'),
(28, 42, 57, 2, 158, 'Coach GLD', 30000, '2026-07-12 19:05:00', '2026-07-12 19:56:00'),
(29, 43, 55, 2, 146, 'Coach VER', 35000, '2026-07-13 18:00:00', '2026-07-13 18:52:00'),
(30, 44, 58, 2, 164, 'Coach WHT', 40000, '2026-07-13 19:10:00', '2026-07-13 19:25:00'),
(31, 45, 56, 2, 152, 'Coach NER', 30000, '2026-07-14 18:00:00', '2026-07-14 18:45:00'),
(32, 46, 55, 2, 146, 'Coach VER', 35000, '2026-07-14 19:15:00', '2026-07-14 20:10:00');

-- --------------------------------------------------------

--
-- Struttura della tabella `online_sync_log`
--

CREATE TABLE `online_sync_log` (
  `id` bigint UNSIGNED NOT NULL,
  `source_device` varchar(120) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `entity_type` varchar(80) COLLATE utf8mb4_unicode_ci NOT NULL,
  `entity_id` varchar(80) COLLATE utf8mb4_unicode_ci NOT NULL,
  `operation` enum('Create','Update','Delete','Snapshot') COLLATE utf8mb4_unicode_ci NOT NULL,
  `payload_json` json DEFAULT NULL,
  `status` enum('Received','Applied','Rejected') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Received',
  `error_message` text COLLATE utf8mb4_unicode_ci,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dump dei dati per la tabella `online_sync_log`
--

INSERT INTO `online_sync_log` (`id`, `source_device`, `entity_type`, `entity_id`, `operation`, `payload_json`, `status`, `error_message`, `created_at`) VALUES
(1, 'Tablet Tavolo 01', 'MatchReport', 'MATCH-ANV-20260704-01', 'Update', '{\"away\": 54, \"home\": 58, \"events\": 31, \"match_id\": 15}', 'Applied', NULL, '2026-06-08 00:26:32'),
(2, 'Tablet Tavolo 02', 'PlayerStats', 'PSTAT-RIC-20260704-03', 'Update', '{\"points\": 22, \"player_id\": 66, \"evaluation\": 28, \"minutes_seconds\": 1322}', 'Applied', NULL, '2026-06-08 00:26:32'),
(3, 'Console Segnapunti', 'TeamStats', 'TSTAT-WAV-20260705-05', 'Update', '{\"points\": 55, \"assists\": 12, \"team_id\": 43, \"rebounds\": 29}', 'Applied', NULL, '2026-06-08 00:26:32'),
(4, 'Laptop Direzione', 'ContestRound', '3PT-ROUND-20260704-01', 'Create', '{\"event_id\": 5, \"best_score\": 27, \"participants\": 6}', 'Received', NULL, '2026-06-08 00:26:32'),
(5, 'Tablet Arbitro 01', 'StandingUpdate', 'RANK-GIRONE-20260706', 'Snapshot', '{\"group_count\": 6, \"updated_rows\": 6, \"max_points_for\": 286}', 'Applied', NULL, '2026-06-08 00:26:32'),
(6, 'Server Referti', 'SyncAudit', 'AUDIT-20260706-22', 'Snapshot', '{\"warnings\": 1, \"rows_checked\": 168, \"tables_checked\": 26}', 'Rejected', 'Controllo manuale richiesto per differenza tra referto cartaceo e digitale.', '2026-06-08 00:26:32'),
(8, 'tablet-arbitro-2', 'Match', 'match:37', 'Snapshot', '{\"gara\": \"m1\", \"stato\": \"sincronizzato\"}', 'Applied', NULL, '2026-07-10 18:48:00'),
(9, 'tablet-arbitro-3', 'Match', 'match:38', 'Snapshot', '{\"gara\": \"m2\", \"stato\": \"sincronizzato\"}', 'Applied', NULL, '2026-07-10 19:53:00'),
(10, 'tablet-arbitro-1', 'Match', 'match:39', 'Snapshot', '{\"gara\": \"m3\", \"stato\": \"sincronizzato\"}', 'Applied', NULL, '2026-07-11 18:50:00'),
(11, 'tablet-arbitro-2', 'Match', 'match:40', 'Snapshot', '{\"gara\": \"m4\", \"stato\": \"sincronizzato\"}', 'Applied', NULL, '2026-07-11 19:54:00'),
(12, 'tablet-arbitro-3', 'Match', 'match:41', 'Snapshot', '{\"gara\": \"m5\", \"stato\": \"sincronizzato\"}', 'Applied', NULL, '2026-07-12 18:47:00'),
(13, 'tablet-arbitro-1', 'Match', 'match:42', 'Snapshot', '{\"gara\": \"m6\", \"stato\": \"sincronizzato\"}', 'Applied', NULL, '2026-07-12 19:56:00'),
(14, 'console-direzione', 'Edition', 'edition:14', 'Create', '{\"edizione\": \"Memorial Carlo Venturi 2026\"}', 'Applied', NULL, '2026-07-10 15:30:00'),
(15, 'totem-pubblico', 'Scoreboard', 'finale:46', 'Snapshot', '{\"away\": 65, \"home\": 68}', 'Applied', NULL, '2026-07-14 20:10:30');

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

--
-- Dump dei dati per la tabella `players`
--

INSERT INTO `players` (`id`, `first_name`, `last_name`, `nickname`, `fiscal_code`, `address`, `phone_number`, `email`, `birth_date`, `photo_path`, `created_at`, `updated_at`) VALUES
(1, 'Nuovo', 'Giocatore', NULL, NULL, NULL, NULL, NULL, NULL, NULL, '2026-05-27 10:14:28', '2026-05-27 10:14:33'),
(3, 'asd', 'asd', NULL, NULL, NULL, NULL, NULL, NULL, 'asdasd', '2026-05-27 11:45:25', '2026-05-27 11:45:25'),
(4, 'asd', 'asd', NULL, NULL, NULL, NULL, NULL, NULL, NULL, '2026-05-27 11:52:19', '2026-05-27 11:52:19'),
(5, 'Luca', 'Rossi', 'HAW Captain', 'DMO0101PMADNESS', 'Via del Basket 1, 4', '+39 333 100000', 'haw.4@demo.piazzetta.local', '1990-01-10', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(6, 'Marco', 'Bianchi', NULL, 'DMO0102PMADNESS', 'Via del Basket 1, 5', '+39 333 100001', 'haw.5@demo.piazzetta.local', '1991-02-11', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(7, 'Andrea', 'Marchetti', NULL, 'DMO0103PMADNESS', 'Via del Basket 1, 6', '+39 333 100002', 'haw.6@demo.piazzetta.local', '1992-03-12', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(8, 'Matteo', 'Ferri', NULL, 'DMO0104PMADNESS', 'Via del Basket 1, 7', '+39 333 100003', 'haw.7@demo.piazzetta.local', '1993-04-13', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(9, 'Davide', 'Conti', NULL, 'DMO0105PMADNESS', 'Via del Basket 1, 8', '+39 333 100004', 'haw.8@demo.piazzetta.local', '1994-05-14', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(10, 'Simone', 'Romagnoli', NULL, 'DMO0106PMADNESS', 'Via del Basket 1, 9', '+39 333 100005', 'haw.9@demo.piazzetta.local', '1995-06-15', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(11, 'Alessandro', 'Serafini', NULL, 'DMO0107PMADNESS', 'Via del Basket 1, 10', '+39 333 100006', 'haw.10@demo.piazzetta.local', '1996-07-16', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(12, 'Andrea', 'Ferri', 'BUL Captain', 'DMO0201PMADNESS', 'Via del Basket 2, 4', '+39 333 100100', 'bul.14@demo.piazzetta.local', '1990-01-11', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(13, 'Matteo', 'Conti', NULL, 'DMO0202PMADNESS', 'Via del Basket 2, 5', '+39 333 100101', 'bul.15@demo.piazzetta.local', '1991-02-12', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(14, 'Davide', 'Romagnoli', NULL, 'DMO0203PMADNESS', 'Via del Basket 2, 6', '+39 333 100102', 'bul.16@demo.piazzetta.local', '1992-03-13', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(15, 'Simone', 'Serafini', NULL, 'DMO0204PMADNESS', 'Via del Basket 2, 7', '+39 333 100103', 'bul.17@demo.piazzetta.local', '1993-04-14', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(16, 'Alessandro', 'Moretti', NULL, 'DMO0205PMADNESS', 'Via del Basket 2, 8', '+39 333 100104', 'bul.18@demo.piazzetta.local', '1994-05-15', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(17, 'Francesco', 'Gentili', NULL, 'DMO0206PMADNESS', 'Via del Basket 2, 9', '+39 333 100105', 'bul.19@demo.piazzetta.local', '1995-06-16', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(18, 'Gabriele', 'Mancini', NULL, 'DMO0207PMADNESS', 'Via del Basket 2, 10', '+39 333 100106', 'bul.20@demo.piazzetta.local', '1996-07-17', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(19, 'Davide', 'Serafini', 'LAK Captain', 'DMO0301PMADNESS', 'Via del Basket 3, 4', '+39 333 100200', 'lak.24@demo.piazzetta.local', '1990-01-12', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(20, 'Simone', 'Moretti', NULL, 'DMO0302PMADNESS', 'Via del Basket 3, 5', '+39 333 100201', 'lak.25@demo.piazzetta.local', '1991-02-13', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(21, 'Alessandro', 'Gentili', NULL, 'DMO0303PMADNESS', 'Via del Basket 3, 6', '+39 333 100202', 'lak.26@demo.piazzetta.local', '1992-03-14', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(22, 'Francesco', 'Mancini', NULL, 'DMO0304PMADNESS', 'Via del Basket 3, 7', '+39 333 100203', 'lak.27@demo.piazzetta.local', '1993-04-15', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(23, 'Gabriele', 'De Santis', NULL, 'DMO0305PMADNESS', 'Via del Basket 3, 8', '+39 333 100204', 'lak.28@demo.piazzetta.local', '1994-05-16', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(24, 'Filippo', 'Giuliani', NULL, 'DMO0306PMADNESS', 'Via del Basket 3, 9', '+39 333 100205', 'lak.29@demo.piazzetta.local', '1995-06-17', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(25, 'Riccardo', 'Silvestri', NULL, 'DMO0307PMADNESS', 'Via del Basket 3, 10', '+39 333 100206', 'lak.30@demo.piazzetta.local', '1996-07-18', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(26, 'Alessandro', 'Mancini', 'SHA Captain', 'DMO0401PMADNESS', 'Via del Basket 4, 4', '+39 333 100300', 'sha.4@demo.piazzetta.local', '1990-01-13', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(27, 'Francesco', 'De Santis', NULL, 'DMO0402PMADNESS', 'Via del Basket 4, 5', '+39 333 100301', 'sha.5@demo.piazzetta.local', '1991-02-14', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(28, 'Gabriele', 'Giuliani', NULL, 'DMO0403PMADNESS', 'Via del Basket 4, 6', '+39 333 100302', 'sha.6@demo.piazzetta.local', '1992-03-15', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(29, 'Filippo', 'Silvestri', NULL, 'DMO0404PMADNESS', 'Via del Basket 4, 7', '+39 333 100303', 'sha.7@demo.piazzetta.local', '1993-04-16', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(30, 'Riccardo', 'Carletti', NULL, 'DMO0405PMADNESS', 'Via del Basket 4, 8', '+39 333 100304', 'sha.8@demo.piazzetta.local', '1994-05-17', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(31, 'Tommaso', 'Monti', NULL, 'DMO0406PMADNESS', 'Via del Basket 4, 9', '+39 333 100305', 'sha.9@demo.piazzetta.local', '1995-06-18', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(32, 'Nicolò', 'Lombardi', NULL, 'DMO0407PMADNESS', 'Via del Basket 4, 10', '+39 333 100306', 'sha.10@demo.piazzetta.local', '1996-07-19', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(33, 'Gabriele', 'Silvestri', 'RAP Captain', 'DMO0501PMADNESS', 'Via del Basket 5, 4', '+39 333 100400', 'rap.14@demo.piazzetta.local', '1990-01-14', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(34, 'Filippo', 'Carletti', NULL, 'DMO0502PMADNESS', 'Via del Basket 5, 5', '+39 333 100401', 'rap.15@demo.piazzetta.local', '1991-02-15', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(35, 'Riccardo', 'Monti', NULL, 'DMO0503PMADNESS', 'Via del Basket 5, 6', '+39 333 100402', 'rap.16@demo.piazzetta.local', '1992-03-16', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(36, 'Tommaso', 'Lombardi', NULL, 'DMO0504PMADNESS', 'Via del Basket 5, 7', '+39 333 100403', 'rap.17@demo.piazzetta.local', '1993-04-17', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(37, 'Nicolò', 'Rossi', NULL, 'DMO0505PMADNESS', 'Via del Basket 5, 8', '+39 333 100404', 'rap.18@demo.piazzetta.local', '1994-05-18', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(38, 'Edoardo', 'Bianchi', NULL, 'DMO0506PMADNESS', 'Via del Basket 5, 9', '+39 333 100405', 'rap.19@demo.piazzetta.local', '1995-06-19', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(39, 'Michele', 'Marchetti', NULL, 'DMO0507PMADNESS', 'Via del Basket 5, 10', '+39 333 100406', 'rap.20@demo.piazzetta.local', '1996-07-20', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(40, 'Riccardo', 'Lombardi', 'CEL Captain', 'DMO0601PMADNESS', 'Via del Basket 6, 4', '+39 333 100500', 'cel.24@demo.piazzetta.local', '1990-01-15', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(41, 'Tommaso', 'Rossi', NULL, 'DMO0602PMADNESS', 'Via del Basket 6, 5', '+39 333 100501', 'cel.25@demo.piazzetta.local', '1991-02-16', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(42, 'Nicolò', 'Bianchi', NULL, 'DMO0603PMADNESS', 'Via del Basket 6, 6', '+39 333 100502', 'cel.26@demo.piazzetta.local', '1992-03-17', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(43, 'Edoardo', 'Marchetti', NULL, 'DMO0604PMADNESS', 'Via del Basket 6, 7', '+39 333 100503', 'cel.27@demo.piazzetta.local', '1993-04-18', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(44, 'Michele', 'Ferri', NULL, 'DMO0605PMADNESS', 'Via del Basket 6, 8', '+39 333 100504', 'cel.28@demo.piazzetta.local', '1994-05-19', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(45, 'Stefano', 'Conti', NULL, 'DMO0606PMADNESS', 'Via del Basket 6, 9', '+39 333 100505', 'cel.29@demo.piazzetta.local', '1995-06-20', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(46, 'Luca', 'Romagnoli', NULL, 'DMO0607PMADNESS', 'Via del Basket 6, 10', '+39 333 100506', 'cel.30@demo.piazzetta.local', '1996-07-21', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(47, 'Nicolò', 'Marchetti', 'SUN Captain', 'DMO0701PMADNESS', 'Via del Basket 7, 4', '+39 333 100600', 'sun.4@demo.piazzetta.local', '1990-01-16', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(48, 'Edoardo', 'Ferri', NULL, 'DMO0702PMADNESS', 'Via del Basket 7, 5', '+39 333 100601', 'sun.5@demo.piazzetta.local', '1991-02-17', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(49, 'Michele', 'Conti', NULL, 'DMO0703PMADNESS', 'Via del Basket 7, 6', '+39 333 100602', 'sun.6@demo.piazzetta.local', '1992-03-18', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(50, 'Stefano', 'Romagnoli', NULL, 'DMO0704PMADNESS', 'Via del Basket 7, 7', '+39 333 100603', 'sun.7@demo.piazzetta.local', '1993-04-19', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(51, 'Luca', 'Serafini', NULL, 'DMO0705PMADNESS', 'Via del Basket 7, 8', '+39 333 100604', 'sun.8@demo.piazzetta.local', '1994-05-20', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(52, 'Marco', 'Moretti', NULL, 'DMO0706PMADNESS', 'Via del Basket 7, 9', '+39 333 100605', 'sun.9@demo.piazzetta.local', '1995-06-21', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(53, 'Andrea', 'Gentili', NULL, 'DMO0707PMADNESS', 'Via del Basket 7, 10', '+39 333 100606', 'sun.10@demo.piazzetta.local', '1996-07-22', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(54, 'Michele', 'Romagnoli', 'KIN Captain', 'DMO0801PMADNESS', 'Via del Basket 8, 4', '+39 333 100700', 'kin.14@demo.piazzetta.local', '1990-01-17', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(55, 'Stefano', 'Serafini', NULL, 'DMO0802PMADNESS', 'Via del Basket 8, 5', '+39 333 100701', 'kin.15@demo.piazzetta.local', '1991-02-18', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(56, 'Luca', 'Moretti', NULL, 'DMO0803PMADNESS', 'Via del Basket 8, 6', '+39 333 100702', 'kin.16@demo.piazzetta.local', '1992-03-19', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(57, 'Marco', 'Gentili', NULL, 'DMO0804PMADNESS', 'Via del Basket 8, 7', '+39 333 100703', 'kin.17@demo.piazzetta.local', '1993-04-20', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(58, 'Andrea', 'Mancini', NULL, 'DMO0805PMADNESS', 'Via del Basket 8, 8', '+39 333 100704', 'kin.18@demo.piazzetta.local', '1994-05-21', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(59, 'Matteo', 'De Santis', NULL, 'DMO0806PMADNESS', 'Via del Basket 8, 9', '+39 333 100705', 'kin.19@demo.piazzetta.local', '1995-06-22', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(60, 'Davide', 'Giuliani', NULL, 'DMO0807PMADNESS', 'Via del Basket 8, 10', '+39 333 100706', 'kin.20@demo.piazzetta.local', '1996-07-23', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(61, 'test', 'test', 'test', 'TEST', 'test', 'test', 'test@test.om', '2026-06-03', 'C:\\Users\\rebic\\Pictures\\Screenshots\\Screenshot 2025-07-17 220640.png', '2026-06-03 15:50:29', '2026-06-03 15:50:29'),
(63, 'Lorenzo', 'Rebichini test', NULL, NULL, NULL, NULL, NULL, NULL, NULL, '2026-06-03 16:31:41', '2026-06-03 16:32:08'),
(64, 'Luca', 'Verdi', 'Luca Verdi', 'VRDLCU98A11A271Q', 'Via Garibaldi 12, Ancona', '+39 333 2145801', 'luca.verdi@piazzetta.example', '1998-01-11', '/assets/players/luca-verdi.jpg', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(65, 'Marco', 'Fontana', 'Marco Fontana', 'FNTMRC97B12H501R', 'Corso Matteotti 45, Porto Recanati', '+39 333 2145802', 'marco.fontana@piazzetta.example', '1997-02-12', '/assets/players/marco-fontana.jpg', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(66, 'Andrea', 'Ricci', 'Andrea Ricci', 'RCCNDR99C13C770T', 'Via Roma 8, Civitanova Marche', '+39 333 2145803', 'andrea.ricci@piazzetta.example', '1999-03-13', '/assets/players/andrea-ricci.jpg', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(67, 'Matteo', 'Romano', 'Matteo Romano', 'RMNMTT96D14E783K', 'Via Dante 21, Macerata', '+39 333 2145804', 'matteo.romano@piazzetta.example', '1996-04-14', '/assets/players/matteo-romano.jpg', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(68, 'Davide', 'Galli', 'Davide Galli', 'GLLDVD95E15F205M', 'Via della Repubblica 6, Loreto', '+39 333 2145805', 'davide.galli@piazzetta.example', '1995-05-15', '/assets/players/davide-galli.jpg', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(69, 'Simone', 'Ferraro', 'Simone Ferraro', 'FRRSMN94F16G157P', 'Via Trento 33, Osimo', '+39 333 2145806', 'simone.ferraro@piazzetta.example', '1994-06-16', '/assets/players/simone-ferraro.jpg', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(134, 'Luca', 'Verdi', 'Capitano BLU', 'VERLUC81A01F205X', 'Via delle Palme 1, Porto Potenza Picena', '+39 333 800001', 'luca.verdi@costa-verde.local', '1989-01-09', '/assets/players/luca.verdi.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(135, 'Marco', 'Rinaldi', NULL, 'RINMAR82A01F205X', 'Via delle Palme 2, Porto Potenza Picena', '+39 333 800002', 'marco.rinaldi@costa-verde.local', '1990-02-10', '/assets/players/marco.rinaldi.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(136, 'Andrea', 'Fabbri', NULL, 'FABAND83A01F205X', 'Via delle Palme 3, Porto Potenza Picena', '+39 333 800003', 'andrea.fabbri@costa-verde.local', '1991-03-11', '/assets/players/andrea.fabbri.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(137, 'Davide', 'Marini', NULL, 'MARDAV84A01F205X', 'Via delle Palme 4, Porto Potenza Picena', '+39 333 800004', 'davide.marini@costa-verde.local', '1992-04-12', '/assets/players/davide.marini.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(138, 'Simone', 'Ricci', NULL, 'RICSIM85A01F205X', 'Via delle Palme 5, Porto Potenza Picena', '+39 333 800005', 'simone.ricci@costa-verde.local', '1993-05-13', '/assets/players/simone.ricci.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(139, 'Tommaso', 'Galli', NULL, 'GALTOM86A01F205X', 'Via delle Palme 6, Porto Potenza Picena', '+39 333 800006', 'tommaso.galli@costa-verde.local', '1994-06-14', '/assets/players/tommaso.galli.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(140, 'Matteo', 'Conti', 'Capitano RED', 'CONMAT87A01F205X', 'Via delle Palme 7, Porto Potenza Picena', '+39 333 800007', 'matteo.conti@costa-verde.local', '1995-07-15', '/assets/players/matteo.conti.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(141, 'Alessandro', 'Moretti', NULL, 'MORALE88A01F205X', 'Via delle Palme 8, Porto Potenza Picena', '+39 333 800008', 'alessandro.moretti@costa-verde.local', '1996-08-16', '/assets/players/alessandro.moretti.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(142, 'Francesco', 'Gentili', NULL, 'GENFRA89A01F205X', 'Via delle Palme 9, Porto Potenza Picena', '+39 333 800009', 'francesco.gentili@costa-verde.local', '1997-09-17', '/assets/players/francesco.gentili.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(143, 'Gabriele', 'Mancini', NULL, 'MANGAB90A01F205X', 'Via delle Palme 10, Porto Potenza Picena', '+39 333 800010', 'gabriele.mancini@costa-verde.local', '1998-10-18', '/assets/players/gabriele.mancini.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(144, 'Nicola', 'Lombardi', NULL, 'LOMNIC91A01F205X', 'Via delle Palme 11, Porto Potenza Picena', '+39 333 800011', 'nicola.lombardi@costa-verde.local', '1999-11-19', '/assets/players/nicola.lombardi.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(145, 'Edoardo', 'Ferrari', NULL, 'FEREDO92A01F205X', 'Via delle Palme 12, Porto Potenza Picena', '+39 333 800012', 'edoardo.ferrari@costa-verde.local', '1988-12-20', '/assets/players/edoardo.ferrari.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(146, 'Riccardo', 'Serafini', 'Capitano VER', 'SERRIC93A01F205X', 'Via delle Palme 13, Porto Potenza Picena', '+39 333 800013', 'riccardo.serafini@costa-verde.local', '1989-01-21', '/assets/players/riccardo.serafini.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(147, 'Michele', 'Bassi', NULL, 'BASMIC94A01F205X', 'Via delle Palme 14, Porto Potenza Picena', '+39 333 800014', 'michele.bassi@costa-verde.local', '1990-02-22', '/assets/players/michele.bassi.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(148, 'Pietro', 'Leoni', NULL, 'LEOPIE95A01F205X', 'Via delle Palme 15, Porto Potenza Picena', '+39 333 800015', 'pietro.leoni@costa-verde.local', '1991-03-23', '/assets/players/pietro.leoni.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(149, 'Samuele', 'Barbieri', NULL, 'BARSAM96A01F205X', 'Via delle Palme 16, Porto Potenza Picena', '+39 333 800016', 'samuele.barbieri@costa-verde.local', '1992-04-24', '/assets/players/samuele.barbieri.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(150, 'Filippo', 'Orsini', NULL, 'ORSFIL97A01F205X', 'Via delle Palme 17, Porto Potenza Picena', '+39 333 800017', 'filippo.orsini@costa-verde.local', '1993-05-25', '/assets/players/filippo.orsini.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(151, 'Giacomo', 'Rossi', NULL, 'ROSGIA98A01F205X', 'Via delle Palme 18, Porto Potenza Picena', '+39 333 800018', 'giacomo.rossi@costa-verde.local', '1994-06-26', '/assets/players/giacomo.rossi.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(152, 'Lorenzo', 'De Santis', 'Capitano NER', 'DEXLOR99A01F205X', 'Via delle Palme 19, Porto Potenza Picena', '+39 333 800019', 'lorenzo.desantis@costa-verde.local', '1995-07-01', '/assets/players/lorenzo.desantis.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(153, 'Federico', 'Pellegrini', NULL, 'PELFED100A01F205', 'Via delle Palme 20, Porto Potenza Picena', '+39 333 800020', 'federico.pellegrini@costa-verde.local', '1996-08-02', '/assets/players/federico.pellegrini.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(154, 'Daniele', 'Villa', NULL, 'VILDAN101A01F205', 'Via delle Palme 21, Porto Potenza Picena', '+39 333 800021', 'daniele.villa@costa-verde.local', '1997-09-03', '/assets/players/daniele.villa.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(155, 'Cristian', 'Monti', NULL, 'MONCRI102A01F205', 'Via delle Palme 22, Porto Potenza Picena', '+39 333 800022', 'cristian.monti@costa-verde.local', '1998-10-04', '/assets/players/cristian.monti.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(156, 'Manuel', 'Costa', NULL, 'COSMAN103A01F205', 'Via delle Palme 23, Porto Potenza Picena', '+39 333 800023', 'manuel.costa@costa-verde.local', '1999-11-05', '/assets/players/manuel.costa.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(157, 'Enrico', 'Romano', NULL, 'ROMENR104A01F205', 'Via delle Palme 24, Porto Potenza Picena', '+39 333 800024', 'enrico.romano@costa-verde.local', '1988-12-06', '/assets/players/enrico.romano.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(158, 'Alberto', 'Neri', 'Capitano GLD', 'NERALB105A01F205', 'Via delle Palme 25, Porto Potenza Picena', '+39 333 800025', 'alberto.neri@costa-verde.local', '1989-01-07', '/assets/players/alberto.neri.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(159, 'Stefano', 'Benedetti', NULL, 'BENSTE106A01F205', 'Via delle Palme 26, Porto Potenza Picena', '+39 333 800026', 'stefano.benedetti@costa-verde.local', '1990-02-08', '/assets/players/stefano.benedetti.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(160, 'Vittorio', 'Riva', NULL, 'RIVVIT107A01F205', 'Via delle Palme 27, Porto Potenza Picena', '+39 333 800027', 'vittorio.riva@costa-verde.local', '1991-03-09', '/assets/players/vittorio.riva.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(161, 'Giorgio', 'Marchetti', NULL, 'MARGIO108A01F205', 'Via delle Palme 28, Porto Potenza Picena', '+39 333 800028', 'giorgio.marchetti@costa-verde.local', '1992-04-10', '/assets/players/giorgio.marchetti.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(162, 'Leonardo', 'Sarti', NULL, 'SARLEO109A01F205', 'Via delle Palme 29, Porto Potenza Picena', '+39 333 800029', 'leonardo.sarti@costa-verde.local', '1993-05-11', '/assets/players/leonardo.sarti.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(163, 'Claudio', 'Ferri', NULL, 'FERCLA110A01F205', 'Via delle Palme 30, Porto Potenza Picena', '+39 333 800030', 'claudio.ferri@costa-verde.local', '1994-06-12', '/assets/players/claudio.ferri.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(164, 'Paolo', 'Greco', 'Capitano WHT', 'GREPAO111A01F205', 'Via delle Palme 31, Porto Potenza Picena', '+39 333 800031', 'paolo.greco@costa-verde.local', '1995-07-13', '/assets/players/paolo.greco.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(165, 'Antonio', 'Sala', NULL, 'SALANT112A01F205', 'Via delle Palme 32, Porto Potenza Picena', '+39 333 800032', 'antonio.sala@costa-verde.local', '1996-08-14', '/assets/players/antonio.sala.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(166, 'Roberto', 'D Amico', NULL, 'DXAROB113A01F205', 'Via delle Palme 33, Porto Potenza Picena', '+39 333 800033', 'roberto.damico@costa-verde.local', '1997-09-15', '/assets/players/roberto.damico.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(167, 'Elia', 'Fontana', NULL, 'FONELI114A01F205', 'Via delle Palme 34, Porto Potenza Picena', '+39 333 800034', 'elia.fontana@costa-verde.local', '1998-10-16', '/assets/players/elia.fontana.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(168, 'Massimo', 'Moro', NULL, 'MORMAS115A01F205', 'Via delle Palme 35, Porto Potenza Picena', '+39 333 800035', 'massimo.moro@costa-verde.local', '1999-11-17', '/assets/players/massimo.moro.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(169, 'Emanuele', 'Biagi', NULL, 'BIAEMA116A01F205', 'Via delle Palme 36, Porto Potenza Picena', '+39 333 800036', 'emanuele.biagi@costa-verde.local', '1988-12-18', '/assets/players/emanuele.biagi.jpg', '2026-06-08 01:55:15', '2026-06-08 01:55:15');

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

--
-- Dump dei dati per la tabella `scoreboard_states`
--

INSERT INTO `scoreboard_states` (`id`, `match_id`, `current_period`, `current_period_type`, `game_clock_ms_remaining`, `shot_clock_ms_remaining`, `timeout_clock_ms_remaining`, `break_clock_ms_remaining`, `is_game_clock_running`, `is_shot_clock_running`, `is_timeout_running`, `is_break_running`, `possession_team_id`, `home_score`, `away_score`, `home_fouls_current_period`, `away_fouls_current_period`, `home_timeouts_used_total`, `away_timeouts_used_total`, `last_event_id`, `last_updated_at`) VALUES
(2, 2, 1, 'Regular', 717929, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 4, 0, 0, 0, 0, NULL, '2026-05-28 08:29:30'),
(3, 4, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-03 15:17:09'),
(4, 3, 1, 'Regular', 668316, 0, NULL, NULL, 0, 0, 0, 0, NULL, 12, 0, 0, 0, 0, 0, NULL, '2026-05-27 19:40:02'),
(5, 7, 1, 'Regular', 720000, 17124, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-05-28 08:07:48'),
(6, 8, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-05-27 15:44:12'),
(7, 6, 1, 'Regular', 720000, 24000, NULL, NULL, 1, 1, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-05-27 16:47:39'),
(8, 5, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 37, 41, 0, 0, 0, 0, NULL, '2026-05-28 08:03:24'),
(9, 10, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-03 15:52:32'),
(10, 15, 2, 'Regular', 0, 0, NULL, NULL, 0, 0, 0, 0, 39, 58, 54, 4, 5, 1, 2, NULL, '2026-07-04 18:47:05'),
(11, 16, 2, 'Regular', 0, 0, NULL, NULL, 0, 0, 0, 0, 41, 41, 49, 3, 6, 1, 1, NULL, '2026-07-04 19:38:02'),
(12, 17, 3, 'Overtime', 0, 0, NULL, NULL, 0, 0, 0, 0, 41, 63, 61, 5, 5, 2, 2, NULL, '2026-07-04 20:30:11'),
(13, 18, 2, 'Regular', 0, 0, NULL, NULL, 0, 0, 0, 0, 43, 47, 52, 7, 4, 1, 2, NULL, '2026-07-05 18:48:08'),
(14, 19, 2, 'Regular', 0, 0, NULL, NULL, 0, 0, 0, 0, 43, 55, 45, 2, 8, 0, 2, NULL, '2026-07-05 19:39:20'),
(15, 20, 2, 'Regular', 0, 0, NULL, NULL, 0, 0, 0, 0, 39, 60, 64, 5, 3, 2, 1, NULL, '2026-07-05 20:31:04'),
(32, 37, 2, 'Regular', 0, 0, NULL, NULL, 0, 0, 0, 0, 53, 58, 52, 1, 3, 1, 2, NULL, '2026-07-10 18:48:00'),
(33, 38, 2, 'Regular', 0, 0, NULL, NULL, 0, 0, 0, 0, 55, 49, 61, 2, 4, 2, 0, NULL, '2026-07-10 19:53:00'),
(34, 39, 2, 'Regular', 0, 0, NULL, NULL, 0, 0, 0, 0, 54, 55, 47, 3, 5, 0, 1, NULL, '2026-07-11 18:50:00'),
(35, 40, 2, 'Regular', 0, 0, NULL, NULL, 0, 0, 0, 0, 56, 63, 57, 4, 0, 1, 2, NULL, '2026-07-11 19:54:00'),
(36, 41, 2, 'Regular', 0, 0, NULL, NULL, 0, 0, 0, 0, 58, 54, 59, 5, 1, 2, 0, NULL, '2026-07-12 18:47:00'),
(37, 42, 3, 'Overtime', 0, 0, NULL, NULL, 0, 0, 0, 0, 58, 62, 64, 0, 2, 0, 1, NULL, '2026-07-12 19:56:00'),
(38, 43, 2, 'Regular', 0, 0, NULL, NULL, 0, 0, 0, 0, 55, 66, 60, 1, 3, 1, 2, NULL, '2026-07-13 18:52:00'),
(39, 44, 2, 'Regular', 0, 0, NULL, NULL, 0, 0, 0, 0, 54, 0, 20, 2, 4, 2, 0, NULL, '2026-07-13 19:25:00'),
(40, 45, 2, 'Regular', 0, 0, NULL, NULL, 0, 0, 0, 0, 56, 50, 44, 3, 5, 0, 1, NULL, '2026-07-14 18:45:00'),
(41, 46, 3, 'Overtime', 0, 0, NULL, NULL, 0, 0, 0, 0, 55, 68, 65, 4, 0, 1, 2, NULL, '2026-07-14 20:10:00');

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

--
-- Dump dei dati per la tabella `sponsors`
--

INSERT INTO `sponsors` (`id`, `name`, `description`, `image_path`, `is_active`, `sort_order`, `created_at`, `updated_at`) VALUES
(1, 'Test', 'test', 'C:\\Users\\rebic\\Pictures\\Screenshots\\Screenshot 2025-07-17 235931.png', 1, 1, '2026-05-28 08:47:32', '2026-05-28 08:47:32'),
(2, 'test 2', 'test 2', 'C:\\Users\\rebic\\Pictures\\Screenshots\\Screenshot 2025-09-03 133300.png', 1, 2, '2026-05-28 08:47:48', '2026-05-28 08:47:48'),
(4, 'test', 'test', NULL, 1, 3, '2026-06-03 16:15:32', '2026-06-03 16:15:32'),
(5, 'test6', 'test6', NULL, 1, 4, '2026-06-03 16:31:06', '2026-06-03 16:31:12'),
(7, 'Caffè Aurora', 'Torrefazione locale e punto ristoro ufficiale.', '/assets/sponsor/caffe-aurora.png', 1, 10, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(8, 'Pizzeria Marina', 'Pizzeria artigianale partner delle serate finali.', '/assets/sponsor/pizzeria-marina.png', 1, 20, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(9, 'Studio Fisio San Giorgio', 'Centro fisioterapico per assistenza atleti.', '/assets/sponsor/fisio-san-giorgio.png', 1, 30, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(10, 'Banca del Porto', 'Partner bancario per premi e iscrizioni.', '/assets/sponsor/banca-porto.png', 1, 40, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(11, 'Gelateria Centrale', 'Gelateria storica sponsor del premio fair play.', '/assets/sponsor/gelateria-centrale.png', 1, 50, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(12, 'TechLab Marche', 'Laboratorio informatico per supporto referti e streaming.', '/assets/sponsor/techlab-marche.png', 1, 60, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(21, 'Gelateria Aurora', 'Partner food area e premio miglior giovane.', '/assets/sponsors/gelateria-aurora.png', 1, 20, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(22, 'Officina Mancini', 'Sponsor tecnico per attrezzature e manutenzione campi.', '/assets/sponsors/officina-mancini.png', 1, 30, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(23, 'Studio Fisiomed', 'Assistenza fisioterapica durante le gare.', '/assets/sponsors/studio-fisiomed.png', 1, 40, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(24, 'Libreria La Vela', 'Partner culturale e premio fair play.', '/assets/sponsors/libreria-la-vela.png', 1, 50, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(25, 'Hotel Belvedere', 'Ospitalita per arbitri e staff organizzativo.', '/assets/sponsors/hotel-belvedere.png', 1, 60, '2026-06-08 01:55:15', '2026-06-08 01:55:15');

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

--
-- Dump dei dati per la tabella `standings`
--

INSERT INTO `standings` (`id`, `group_id`, `team_id`, `played`, `wins`, `losses`, `points_for`, `points_against`, `point_difference`, `ranking_points`, `position`, `tie_break_note`) VALUES
(33, 3, 20, 1, 1, 0, 45, 39, 6, 2, 1, NULL),
(34, 3, 15, 1, 1, 0, 4, 0, 4, 2, 2, NULL),
(35, 3, 12, 0, 0, 0, 0, 0, 0, 0, 3, NULL),
(36, 3, 14, 0, 0, 0, 0, 0, 0, 0, 4, NULL),
(37, 3, 22, 0, 0, 0, 0, 0, 0, 0, 5, NULL),
(38, 3, 23, 0, 0, 0, 0, 0, 0, 0, 6, NULL),
(39, 3, 13, 1, 0, 1, 0, 4, -4, 0, 7, NULL),
(40, 3, 21, 1, 0, 1, 39, 45, -6, 0, 8, NULL),
(41, 4, 25, 1, 1, 0, 41, 37, 4, 2, 1, NULL),
(42, 4, 16, 0, 0, 0, 0, 0, 0, 0, 2, NULL),
(43, 4, 17, 0, 0, 0, 0, 0, 0, 0, 3, NULL),
(44, 4, 18, 0, 0, 0, 0, 0, 0, 0, 4, NULL),
(45, 4, 19, 0, 0, 0, 0, 0, 0, 0, 5, NULL),
(46, 4, 26, 0, 0, 0, 0, 0, 0, 0, 6, NULL),
(47, 4, 27, 0, 0, 0, 0, 0, 0, 0, 7, NULL),
(48, 4, 24, 1, 0, 1, 37, 41, -4, 0, 8, NULL),
(49, 5, 31, 0, 0, 0, 0, 0, 0, 0, 1, NULL),
(50, 5, 32, 0, 0, 0, 0, 0, 0, 0, 2, NULL),
(51, 5, 34, 0, 0, 0, 0, 0, 0, 0, 3, NULL),
(52, 6, 36, 1, 1, 0, 41, 37, 4, 2, 1, NULL),
(53, 6, 37, 0, 0, 0, 0, 0, 0, 0, 2, NULL),
(54, 6, 38, 0, 0, 0, 0, 0, 0, 0, 3, NULL),
(55, 6, 35, 1, 0, 1, 37, 41, -4, 0, 4, NULL),
(56, 7, 12, 0, 0, 0, 0, 0, 0, 0, 1, NULL),
(57, 7, 31, 0, 0, 0, 0, 0, 0, 0, 2, NULL),
(58, 7, 32, 0, 0, 0, 0, 0, 0, 0, 3, NULL),
(59, 7, 33, 0, 0, 0, 0, 0, 0, 0, 4, NULL),
(60, 7, 34, 0, 0, 0, 0, 0, 0, 0, 5, NULL),
(61, 8, 31, 0, 0, 0, 0, 0, 0, 0, 1, NULL),
(62, 8, 32, 0, 0, 0, 0, 0, 0, 0, 2, NULL),
(63, 8, 33, 0, 0, 0, 0, 0, 0, 0, 3, NULL),
(64, 8, 34, 0, 0, 0, 0, 0, 0, 0, 4, NULL),
(65, 9, 39, 4, 3, 1, 247, 221, 26, 6, 2, 'Differenza canestri positiva negli scontri diretti'),
(66, 9, 40, 5, 2, 3, 286, 301, -15, 4, 4, 'Peggior quoziente canestri nel girone'),
(67, 9, 41, 4, 4, 0, 271, 244, 27, 8, 1, 'Prima per vittorie totali e miglior difesa'),
(68, 9, 42, 5, 1, 4, 259, 276, -17, 2, 6, 'Ultima dopo classifica avulsa'),
(69, 9, 43, 3, 2, 1, 188, 174, 14, 4, 3, 'Terza per minor numero di falli'),
(70, 9, 44, 4, 1, 3, 214, 239, -25, 2, 5, 'Quinta per scontro diretto perso'),
(79, 19, 53, 2, 1, 1, 107, 113, -6, 2, 3, 'Classifica calcolata su punti, differenza canestri e punti realizzati.'),
(80, 19, 54, 2, 1, 1, 107, 105, 2, 2, 2, 'Classifica calcolata su punti, differenza canestri e punti realizzati.'),
(81, 19, 55, 2, 1, 1, 108, 104, 4, 2, 1, 'Classifica calcolata su punti, differenza canestri e punti realizzati.'),
(82, 20, 56, 2, 1, 1, 117, 116, 1, 2, 2, 'Classifica calcolata su punti, differenza canestri e punti realizzati.'),
(83, 20, 57, 2, 0, 2, 119, 127, -8, 0, 3, 'Classifica calcolata su punti, differenza canestri e punti realizzati.'),
(84, 20, 58, 2, 2, 0, 123, 116, 7, 4, 1, 'Classifica calcolata su punti, differenza canestri e punti realizzati.');

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

--
-- Dump dei dati per la tabella `teams`
--

INSERT INTO `teams` (`id`, `edition_id`, `name`, `short_name`, `primary_color`, `secondary_color`, `logo_path`, `created_at`, `updated_at`) VALUES
(12, 2, 'Team A1', 'A1', '#e63946', '#111827', NULL, '2026-05-27 10:19:14', '2026-05-27 10:19:14'),
(13, 2, 'Team A2', 'A2', '#f77f00', '#111827', NULL, '2026-05-27 10:19:14', '2026-05-27 10:19:14'),
(14, 2, 'Team A3', 'A3', '#2a9d8f', '#111827', NULL, '2026-05-27 10:19:14', '2026-05-27 10:19:14'),
(15, 2, 'Team A4', 'A4', '#457b9d', '#111827', NULL, '2026-05-27 10:19:14', '2026-05-27 10:19:14'),
(16, 2, 'Team B1', 'B1', '#7b2cbf', '#111827', NULL, '2026-05-27 10:19:14', '2026-05-27 10:19:14'),
(17, 2, 'Team B2', 'B2', '#06d6a0', '#111827', NULL, '2026-05-27 10:19:14', '2026-05-27 10:19:14'),
(18, 2, 'Team B3', 'B3', '#ef476f', '#111827', NULL, '2026-05-27 10:19:14', '2026-05-27 10:19:14'),
(19, 2, 'Team B4', 'B4', '#118ab2', '#111827', NULL, '2026-05-27 10:19:14', '2026-05-27 10:19:14'),
(20, 2, 'Piazzetta Hawks', 'HAW', '#e63946', '#111827', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(21, 2, 'Porto Bulls', 'BUL', '#f77f00', '#111827', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(22, 2, 'Monte Lakers', 'LAK', '#6d28d9', '#fbbf24', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(23, 2, 'Adriatica Sharks', 'SHA', '#0284c7', '#e0f2fe', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(24, 2, 'Centro Raptors', 'RAP', '#16a34a', '#052e16', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(25, 2, 'Piazza Celtics', 'CEL', '#059669', '#f8fafc', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(26, 2, 'Marina Suns', 'SUN', '#facc15', '#7c2d12', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(27, 2, 'Collina Kings', 'KIN', '#7c3aed', '#f8fafc', NULL, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(28, 2, 'TEST', 'TEST', '#2563eb', '#111827', NULL, '2026-06-03 17:13:11', '2026-06-03 17:13:11'),
(30, 2, 'TEST 2', 'TEST 23', '#2563eb', '#111827', NULL, '2026-06-03 17:18:06', '2026-06-03 17:18:06'),
(31, 5, 'Piazzetta Hawks', 'HAW', '#e63946', '#111827', NULL, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(32, 5, 'Porto Bulls', 'BUL', '#f77f00', '#111827', NULL, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(33, 5, 'Monte Lakers', 'LAK', '#6d28d9', '#fbbf24', NULL, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(34, 5, 'Adriatica Sharks', 'SHA', '#0284c7', '#e0f2fe', NULL, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(35, 5, 'Centro Raptors', 'RAP', '#16a34a', '#052e16', NULL, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(36, 5, 'Piazza Celtics', 'CEL', '#059669', '#f8fafc', NULL, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(37, 5, 'Marina Suns', 'SUN', '#facc15', '#7c2d12', NULL, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(38, 5, 'Collina Kings', 'KIN', '#7c3aed', '#f8fafc', NULL, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(39, 6, 'Ancona Albatros', 'ALB', '#0f766e', '#f0fdfa', '/assets/teams/ancona-albatros.png', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(40, 6, 'Porto Recanati Vipers', 'VIP', '#7c2d12', '#fff7ed', '/assets/teams/porto-recanati-vipers.png', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(41, 6, 'Civitanova Sharks', 'SHK', '#0369a1', '#e0f2fe', '/assets/teams/civitanova-sharks.png', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(42, 6, 'Macerata Titans', 'TIT', '#991b1b', '#fee2e2', '/assets/teams/macerata-titans.png', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(43, 6, 'Loreto Waves', 'WAV', '#1d4ed8', '#dbeafe', '/assets/teams/loreto-waves.png', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(44, 6, 'Osimo Eagles', 'EAG', '#4d7c0f', '#ecfccb', '/assets/teams/osimo-eagles.png', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(53, 14, 'Adriatica Blu', 'BLU', '#1d4ed8', '#f8fafc', '/assets/teams/blu.png', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(54, 14, 'Macerata Reds', 'RED', '#dc2626', '#fff7ed', '/assets/teams/red.png', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(55, 14, 'Porto Verde', 'VER', '#059669', '#ecfdf5', '/assets/teams/ver.png', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(56, 14, 'Collina Nera', 'NER', '#111827', '#e5e7eb', '/assets/teams/ner.png', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(57, 14, 'Riviera Gold', 'GLD', '#d97706', '#1f2937', '/assets/teams/gld.png', '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(58, 14, 'Monte Bianco', 'WHT', '#64748b', '#f8fafc', '/assets/teams/wht.png', '2026-06-08 01:55:15', '2026-06-08 01:55:15');

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

--
-- Dump dei dati per la tabella `team_rosters`
--

INSERT INTO `team_rosters` (`id`, `team_id`, `player_id`, `jersey_number`, `role`, `is_captain`, `is_active`, `created_at`, `updated_at`) VALUES
(5, 20, 5, 4, 'Playmaker', 1, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(6, 20, 6, 5, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(7, 20, 7, 6, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(8, 20, 8, 7, 'Ala forte', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(9, 20, 9, 8, 'Centro', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(10, 20, 10, 9, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(11, 20, 11, 10, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(12, 21, 12, 14, 'Playmaker', 1, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(13, 21, 13, 15, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(14, 21, 14, 16, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(15, 21, 15, 17, 'Ala forte', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(16, 21, 16, 18, 'Centro', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(17, 21, 17, 19, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(18, 21, 18, 20, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(19, 22, 19, 24, 'Playmaker', 1, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(20, 22, 20, 25, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(21, 22, 21, 26, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(22, 22, 22, 27, 'Ala forte', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(23, 22, 23, 28, 'Centro', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(24, 22, 24, 29, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(25, 22, 25, 30, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(26, 23, 26, 4, 'Playmaker', 1, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(27, 23, 27, 5, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(28, 23, 28, 6, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(29, 23, 29, 7, 'Ala forte', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(30, 23, 30, 8, 'Centro', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(31, 23, 31, 9, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(32, 23, 32, 10, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(33, 24, 33, 14, 'Playmaker', 1, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(34, 24, 34, 15, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(35, 24, 35, 16, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(36, 24, 36, 17, 'Ala forte', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(37, 24, 37, 18, 'Centro', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(38, 24, 38, 19, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(39, 24, 39, 20, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(40, 25, 40, 24, 'Playmaker', 1, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(41, 25, 41, 25, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(42, 25, 42, 26, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(43, 25, 43, 27, 'Ala forte', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(44, 25, 44, 28, 'Centro', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(45, 25, 45, 29, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(46, 25, 46, 30, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(47, 26, 47, 4, 'Playmaker', 1, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(48, 26, 48, 5, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(49, 26, 49, 6, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(50, 26, 50, 7, 'Ala forte', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(51, 26, 51, 8, 'Centro', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(52, 26, 52, 9, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(53, 26, 53, 10, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(54, 27, 54, 14, 'Playmaker', 1, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(55, 27, 55, 15, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(56, 27, 56, 16, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(57, 27, 57, 17, 'Ala forte', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(58, 27, 58, 18, 'Centro', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(59, 27, 59, 19, 'Guardia', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(60, 27, 60, 20, 'Ala', 0, 1, '2026-05-27 12:37:26', '2026-06-03 13:50:19'),
(61, 23, 3, NULL, 'Player', 0, 1, '2026-06-03 17:31:35', '2026-06-03 17:31:35'),
(62, 31, 6, 5, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(63, 31, 7, 6, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(64, 31, 8, 7, 'Ala forte', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(65, 31, 9, 8, 'Centro', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(66, 31, 10, 9, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(67, 31, 11, 10, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(68, 32, 12, 14, 'Playmaker', 1, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(69, 32, 13, 15, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(70, 32, 14, 16, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(71, 32, 15, 17, 'Ala forte', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(72, 32, 16, 18, 'Centro', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(73, 32, 17, 19, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(74, 32, 18, 20, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(75, 33, 19, 24, 'Playmaker', 1, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(76, 33, 20, 25, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(77, 33, 21, 26, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(78, 33, 22, 27, 'Ala forte', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(79, 33, 23, 28, 'Centro', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(80, 33, 24, 29, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(81, 33, 25, 30, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(82, 34, 26, 4, 'Playmaker', 1, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(83, 34, 27, 5, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(84, 34, 28, 6, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(85, 34, 29, 7, 'Ala forte', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(86, 34, 30, 8, 'Centro', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(87, 34, 31, 9, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(88, 34, 32, 10, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(89, 35, 33, 14, 'Playmaker', 1, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(90, 35, 34, 15, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(91, 35, 35, 16, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(92, 35, 36, 17, 'Ala forte', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(93, 35, 37, 18, 'Centro', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(94, 35, 38, 19, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(95, 35, 39, 20, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(96, 36, 40, 24, 'Playmaker', 1, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(97, 36, 41, 25, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(98, 36, 42, 26, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(99, 36, 43, 27, 'Ala forte', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(100, 36, 44, 28, 'Centro', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(101, 36, 45, 29, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(102, 36, 46, 30, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(103, 37, 47, 4, 'Playmaker', 1, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(104, 37, 48, 5, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(105, 37, 49, 6, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(106, 37, 50, 7, 'Ala forte', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(107, 37, 51, 8, 'Centro', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(108, 37, 52, 9, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(109, 37, 53, 10, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(110, 38, 54, 14, 'Playmaker', 1, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(111, 38, 55, 15, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(112, 38, 56, 16, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(113, 38, 57, 17, 'Ala forte', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(114, 38, 58, 18, 'Centro', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(115, 38, 59, 19, 'Guardia', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(116, 38, 60, 20, 'Ala', 0, 1, '2026-06-03 15:23:47', '2026-06-03 15:52:32'),
(117, 31, 5, 4, 'Playmaker', 1, 1, '2026-06-03 15:52:32', '2026-06-03 15:52:32'),
(118, 39, 64, 7, 'Playmaker', 1, 1, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(119, 40, 65, 12, 'Guardia tiratrice', 0, 1, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(120, 41, 66, 23, 'Ala piccola', 1, 1, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(121, 42, 67, 34, 'Ala forte', 0, 1, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(122, 43, 68, 45, 'Centro', 1, 1, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(123, 44, 69, 6, 'Sesto uomo', 0, 1, '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(188, 53, 134, 4, 'Playmaker', 1, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(189, 53, 135, 8, 'Guardia', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(190, 53, 136, 11, 'Ala', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(191, 53, 137, 15, 'Ala forte', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(192, 53, 138, 21, 'Centro', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(193, 53, 139, 30, 'Guardia', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(194, 54, 140, 5, 'Playmaker', 1, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(195, 54, 141, 9, 'Guardia', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(196, 54, 142, 13, 'Ala', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(197, 54, 143, 18, 'Ala forte', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(198, 54, 144, 24, 'Centro', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(199, 54, 145, 33, 'Ala', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(200, 55, 146, 6, 'Playmaker', 1, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(201, 55, 147, 10, 'Guardia', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(202, 55, 148, 14, 'Ala', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(203, 55, 149, 19, 'Ala forte', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(204, 55, 150, 27, 'Centro', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(205, 55, 151, 35, 'Guardia', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(206, 56, 152, 7, 'Playmaker', 1, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(207, 56, 153, 12, 'Guardia', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(208, 56, 154, 16, 'Ala', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(209, 56, 155, 22, 'Ala forte', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(210, 56, 156, 28, 'Centro', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(211, 56, 157, 41, 'Ala', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(212, 57, 158, 3, 'Playmaker', 1, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(213, 57, 159, 17, 'Guardia', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(214, 57, 160, 23, 'Ala', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(215, 57, 161, 31, 'Ala forte', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(216, 57, 162, 44, 'Centro', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(217, 57, 163, 55, 'Guardia', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(218, 58, 164, 2, 'Playmaker', 1, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(219, 58, 165, 20, 'Guardia', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(220, 58, 166, 25, 'Ala', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(221, 58, 167, 32, 'Ala forte', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(222, 58, 168, 45, 'Centro', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15'),
(223, 58, 169, 51, 'Ala', 0, 1, '2026-06-08 01:55:15', '2026-06-08 01:55:15');

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

--
-- Dump dei dati per la tabella `three_point_contest_entries`
--

INSERT INTO `three_point_contest_entries` (`id`, `competition_event_id`, `team_id`, `player_id`, `seed_order`, `total_score`, `final_position`) VALUES
(1, 2, 20, 5, 1, 21, NULL),
(2, 2, 21, 12, 2, 20, NULL),
(3, 2, 22, 19, 3, 18, NULL),
(4, 2, 23, 26, 4, 20, NULL),
(5, 2, 24, 33, 5, 19, NULL),
(6, 2, 25, 40, 6, 21, NULL),
(7, 2, 26, 47, 7, 19, NULL),
(8, 2, 27, 54, 8, 18, NULL),
(9, 3, 31, 5, 1, 21, NULL),
(10, 3, 32, 12, 2, 20, NULL),
(11, 3, 33, 19, 3, 18, NULL),
(12, 3, 34, 26, 4, 20, NULL),
(13, 3, 35, 33, 5, 19, NULL),
(14, 3, 36, 40, 6, 21, NULL),
(15, 3, 37, 47, 7, 19, NULL),
(16, 3, 38, 54, 8, 18, NULL),
(17, 5, 39, 64, 1, 27, 1),
(18, 5, 40, 65, 2, 18, 5),
(19, 5, 41, 66, 3, 23, 3),
(20, 5, 42, 67, 4, 16, 6),
(21, 5, 43, 68, 5, 25, 2),
(22, 5, 44, 69, 6, 21, 4),
(31, 19, 53, 136, 1, 24, 3),
(32, 19, 54, 143, 2, 19, 6),
(33, 19, 55, 150, 3, 27, 1),
(34, 19, 56, 157, 4, 21, 5),
(35, 19, 57, 158, 5, 23, 4),
(36, 19, 58, 165, 6, 26, 2);

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

--
-- Dump dei dati per la tabella `three_point_contest_rounds`
--

INSERT INTO `three_point_contest_rounds` (`id`, `entry_id`, `round_number`, `round_type`, `station1_score`, `station2_score`, `station3_score`, `station4_score`, `station5_score`, `total_score`, `notes`) VALUES
(1, 1, 1, 'Qualification', 5, 4, 4, 5, 3, 21, NULL),
(2, 2, 1, 'Qualification', 6, 4, 2, 5, 3, 20, NULL),
(3, 3, 1, 'Qualification', 3, 4, 3, 5, 3, 18, NULL),
(4, 4, 1, 'Qualification', 4, 4, 4, 5, 3, 20, NULL),
(5, 5, 1, 'Qualification', 5, 4, 2, 5, 3, 19, NULL),
(6, 6, 1, 'Qualification', 6, 4, 3, 5, 3, 21, NULL),
(7, 7, 1, 'Qualification', 3, 4, 4, 5, 3, 19, NULL),
(8, 8, 1, 'Qualification', 4, 4, 2, 5, 3, 18, NULL),
(9, 9, 1, 'Qualification', 5, 4, 4, 5, 3, 21, NULL),
(10, 10, 1, 'Qualification', 6, 4, 2, 5, 3, 20, NULL),
(11, 11, 1, 'Qualification', 3, 4, 3, 5, 3, 18, NULL),
(12, 12, 1, 'Qualification', 4, 4, 4, 5, 3, 20, NULL),
(13, 13, 1, 'Qualification', 5, 4, 2, 5, 3, 19, NULL),
(14, 14, 1, 'Qualification', 6, 4, 3, 5, 3, 21, NULL),
(15, 15, 1, 'Qualification', 3, 4, 4, 5, 3, 19, NULL),
(16, 16, 1, 'Qualification', 4, 4, 2, 5, 3, 18, NULL),
(17, 17, 1, 'Qualification', 6, 5, 6, 5, 5, 27, 'Primo classificato con alta precisione dall’angolo.'),
(18, 18, 1, 'Qualification', 3, 4, 4, 3, 4, 18, 'Partenza lenta, miglioramento nella quarta stazione.'),
(19, 19, 1, 'Qualification', 5, 5, 4, 5, 4, 23, 'Buona serie centrale e punteggio costante.'),
(20, 20, 1, 'Qualification', 2, 4, 3, 4, 3, 16, 'Percentuali basse nelle prime due postazioni.'),
(21, 21, 1, 'Qualification', 5, 5, 5, 4, 6, 25, 'Secondo posto grazie alla money ball finale.'),
(22, 22, 1, 'Qualification', 4, 5, 4, 4, 4, 21, 'Prestazione regolare senza errori consecutivi.'),
(39, 31, 1, 'Qualification', 4, 5, 6, 4, 5, 24, 'Qualificazione BLU'),
(40, 32, 1, 'Qualification', 3, 4, 5, 3, 4, 19, 'Qualificazione RED'),
(41, 33, 1, 'Qualification', 5, 5, 6, 5, 6, 27, 'Qualificazione VER'),
(42, 34, 1, 'Qualification', 4, 4, 5, 4, 4, 21, 'Qualificazione NER'),
(43, 35, 1, 'Qualification', 5, 4, 5, 4, 5, 23, 'Qualificazione GLD'),
(44, 36, 1, 'Qualification', 5, 5, 5, 5, 6, 26, 'Qualificazione WHT'),
(45, 33, 2, 'Final', 6, 5, 6, 6, 5, 28, 'Vincitore dopo serie finale'),
(46, 36, 2, 'Final', 5, 6, 5, 5, 5, 26, 'Secondo posto'),
(47, 31, 2, 'Final', 4, 5, 5, 5, 5, 24, 'Terzo posto');

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

--
-- Dump dei dati per la tabella `tournaments`
--

INSERT INTO `tournaments` (`id`, `name`, `description`, `created_at`, `updated_at`) VALUES
(3, 'test', 'test', '2026-06-03 15:28:40', '2026-06-03 15:28:43'),
(4, 'Trofeo Riviera Marche 2026', 'Torneo estivo con squadre locali e calendario completo a gironi.', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(5, 'Coppa Città di Ancona 2026', 'Competizione cittadina con fase eliminatoria e final four.', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(6, 'Summer Basket Challenge 2026', 'Evento serale su campo outdoor con referti digitali.', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(7, 'Memorial Paolo Rossi 2026', 'Memorial sportivo con formula rapida e premiazioni finali.', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(8, 'Adriatic Street Cup 2026', 'Coppa ispirata ai tornei della costa adriatica.', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(9, 'Notte dei Campioni 2026', 'Evento conclusivo con partite, gara da tre e premiazioni.', '2026-06-08 00:26:32', '2026-06-08 01:22:56'),
(12, 'Memorial Carlo Venturi 3x3', 'Torneo estivo di basket con fase a gironi, final four, gara del tiro da tre punti e premiazioni.', '2026-06-08 01:55:15', '2026-06-08 01:55:15');

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
-- Dump dei dati per la tabella `tournament_groups`
--

INSERT INTO `tournament_groups` (`id`, `edition_id`, `name`, `code`, `sort_order`) VALUES
(3, 2, 'Girone A', 'A', 1),
(4, 2, 'Girone B', 'B', 2),
(5, 2, 'test', 'TEST', 3),
(6, 5, 'Girone B', 'B', 2),
(7, 2, 'lo', 'LO', 4),
(8, 5, 'Girone A', 'A', 1),
(9, 6, 'Girone A', 'A', 1),
(10, 6, 'Girone B', 'B', 2),
(11, 6, 'Girone Est', 'EST', 3),
(12, 6, 'Girone Ovest', 'OVEST', 4),
(13, 6, 'Girone Elite', 'ELITE', 5),
(14, 6, 'Girone Challenger', 'CHAL', 6),
(19, 14, 'Girone Adriatico', 'A', 1),
(20, 14, 'Girone Monti', 'B', 2);

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
-- Indici per le tabelle `free_throw_sequences`
--
ALTER TABLE `free_throw_sequences`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_free_throw_sequences_match_id` (`match_id`),
  ADD KEY `ix_free_throw_sequences_team_id` (`team_id`),
  ADD KEY `ix_free_throw_sequences_player_id` (`player_id`);

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
-- Indici per le tabelle `match_fouls`
--
ALTER TABLE `match_fouls`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_match_fouls_match_id` (`match_id`),
  ADD KEY `ix_match_fouls_team_id` (`team_id`),
  ADD KEY `ix_match_fouls_player_id` (`player_id`),
  ADD KEY `fk_match_fouls_event` (`created_event_id`);

--
-- Indici per le tabelle `match_periods`
--
ALTER TABLE `match_periods`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_match_periods_match_number` (`match_id`,`period_number`);

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
-- Indici per le tabelle `match_timeouts`
--
ALTER TABLE `match_timeouts`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_match_timeouts_match_id` (`match_id`),
  ADD KEY `ix_match_timeouts_team_id` (`team_id`),
  ADD KEY `fk_match_timeouts_player` (`requested_by_player_id`);

--
-- Indici per le tabelle `online_sync_log`
--
ALTER TABLE `online_sync_log`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_online_sync_log_entity` (`entity_type`,`entity_id`),
  ADD KEY `ix_online_sync_log_created` (`created_at`);

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
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=26;

--
-- AUTO_INCREMENT per la tabella `courts`
--
ALTER TABLE `courts`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=28;

--
-- AUTO_INCREMENT per la tabella `editions`
--
ALTER TABLE `editions`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=15;

--
-- AUTO_INCREMENT per la tabella `forfeit_results`
--
ALTER TABLE `forfeit_results`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=10;

--
-- AUTO_INCREMENT per la tabella `free_throw_sequences`
--
ALTER TABLE `free_throw_sequences`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=38;

--
-- AUTO_INCREMENT per la tabella `group_teams`
--
ALTER TABLE `group_teams`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=68;

--
-- AUTO_INCREMENT per la tabella `matches`
--
ALTER TABLE `matches`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=52;

--
-- AUTO_INCREMENT per la tabella `match_events`
--
ALTER TABLE `match_events`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=182;

--
-- AUTO_INCREMENT per la tabella `match_fouls`
--
ALTER TABLE `match_fouls`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=70;

--
-- AUTO_INCREMENT per la tabella `match_periods`
--
ALTER TABLE `match_periods`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=70;

--
-- AUTO_INCREMENT per la tabella `match_players`
--
ALTER TABLE `match_players`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=375;

--
-- AUTO_INCREMENT per la tabella `match_player_stats`
--
ALTER TABLE `match_player_stats`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=262;

--
-- AUTO_INCREMENT per la tabella `match_teams`
--
ALTER TABLE `match_teams`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=106;

--
-- AUTO_INCREMENT per la tabella `match_team_stats`
--
ALTER TABLE `match_team_stats`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=78;

--
-- AUTO_INCREMENT per la tabella `match_timeouts`
--
ALTER TABLE `match_timeouts`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=38;

--
-- AUTO_INCREMENT per la tabella `online_sync_log`
--
ALTER TABLE `online_sync_log`
  MODIFY `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=23;

--
-- AUTO_INCREMENT per la tabella `players`
--
ALTER TABLE `players`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=197;

--
-- AUTO_INCREMENT per la tabella `scoreboard_states`
--
ALTER TABLE `scoreboard_states`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=47;

--
-- AUTO_INCREMENT per la tabella `sponsors`
--
ALTER TABLE `sponsors`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=28;

--
-- AUTO_INCREMENT per la tabella `standings`
--
ALTER TABLE `standings`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=86;

--
-- AUTO_INCREMENT per la tabella `teams`
--
ALTER TABLE `teams`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=60;

--
-- AUTO_INCREMENT per la tabella `team_rosters`
--
ALTER TABLE `team_rosters`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=251;

--
-- AUTO_INCREMENT per la tabella `three_point_contest_entries`
--
ALTER TABLE `three_point_contest_entries`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=38;

--
-- AUTO_INCREMENT per la tabella `three_point_contest_rounds`
--
ALTER TABLE `three_point_contest_rounds`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=54;

--
-- AUTO_INCREMENT per la tabella `tournaments`
--
ALTER TABLE `tournaments`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=13;

--
-- AUTO_INCREMENT per la tabella `tournament_groups`
--
ALTER TABLE `tournament_groups`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=22;

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
-- Limiti per la tabella `free_throw_sequences`
--
ALTER TABLE `free_throw_sequences`
  ADD CONSTRAINT `fk_free_throw_sequences_match` FOREIGN KEY (`match_id`) REFERENCES `matches` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_free_throw_sequences_player` FOREIGN KEY (`player_id`) REFERENCES `players` (`id`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_free_throw_sequences_team` FOREIGN KEY (`team_id`) REFERENCES `teams` (`id`) ON DELETE RESTRICT;

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
-- Limiti per la tabella `match_fouls`
--
ALTER TABLE `match_fouls`
  ADD CONSTRAINT `fk_match_fouls_event` FOREIGN KEY (`created_event_id`) REFERENCES `match_events` (`id`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_match_fouls_match` FOREIGN KEY (`match_id`) REFERENCES `matches` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_match_fouls_player` FOREIGN KEY (`player_id`) REFERENCES `players` (`id`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_match_fouls_team` FOREIGN KEY (`team_id`) REFERENCES `teams` (`id`) ON DELETE RESTRICT;

--
-- Limiti per la tabella `match_periods`
--
ALTER TABLE `match_periods`
  ADD CONSTRAINT `fk_match_periods_match` FOREIGN KEY (`match_id`) REFERENCES `matches` (`id`) ON DELETE CASCADE;

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
-- Limiti per la tabella `match_timeouts`
--
ALTER TABLE `match_timeouts`
  ADD CONSTRAINT `fk_match_timeouts_match` FOREIGN KEY (`match_id`) REFERENCES `matches` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_match_timeouts_player` FOREIGN KEY (`requested_by_player_id`) REFERENCES `players` (`id`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_match_timeouts_team` FOREIGN KEY (`team_id`) REFERENCES `teams` (`id`) ON DELETE RESTRICT;

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
