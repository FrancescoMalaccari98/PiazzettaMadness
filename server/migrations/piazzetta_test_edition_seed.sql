-- ============================================================
-- Piazzetta Madness - seed soli dati
-- Generato da dump phpMyAdmin, senza CREATE/ALTER TABLE.
-- Richiede struttura gia presente: server/migrations/db_struttura.sql
-- ============================================================

SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;
SET collation_connection = 'utf8mb4_unicode_ci';
SET SQL_MODE = 'NO_AUTO_VALUE_ON_ZERO';
SET FOREIGN_KEY_CHECKS = 0;
START TRANSACTION;

-- Pulizia dati esistenti: il seed usa ID espliciti, quindi va importato su tabelle vuote.
-- L'ordine e inverso rispetto alle dipendenze tra tabelle.
DELETE FROM `match_events`;
DELETE FROM `three_point_contest_rounds`;
DELETE FROM `three_point_contest_entries`;
DELETE FROM `competition_events`;
DELETE FROM `scoreboard_states`;
DELETE FROM `match_players`;
DELETE FROM `match_teams`;
DELETE FROM `matches`;
DELETE FROM `team_rosters`;
DELETE FROM `standings`;
DELETE FROM `group_teams`;
DELETE FROM `staff`;
DELETE FROM `sponsors`;
DELETE FROM `players`;
DELETE FROM `teams`;
DELETE FROM `tournament_groups`;
DELETE FROM `courts`;
DELETE FROM `editions`;
DELETE FROM `tournaments`;

-- Dati tabella tournaments
INSERT INTO `tournaments` (`id`, `name`, `description`, `created_at`, `updated_at`) VALUES
(4, 'Piazzetta Madness', 'Torneo estivo di basket organizzato alla Piazzetta Verde.', '2026-06-16 16:27:35', '2026-06-16 16:27:35');

-- Dati tabella editions
INSERT INTO `editions` (`id`, `tournament_id`, `name`, `year`, `start_date`, `end_date`, `status`, `created_at`, `updated_at`) VALUES
(4, 4, 'Piazzetta Madness 2026 - Test 8 Squadre', 2026, '2026-07-08', '2026-07-11', 'Active', '2026-06-16 16:27:35', '2026-06-16 16:27:35');

-- Dati tabella courts
INSERT INTO `courts` (`id`, `edition_id`, `name`, `location`) VALUES
(4, 4, 'Piazzetta Verde', 'Porto Potenza Picena');

-- Dati tabella tournament_groups
INSERT INTO `tournament_groups` (`id`, `edition_id`, `name`, `code`, `sort_order`) VALUES
(7, 4, 'Girone A', 'A', 1),
(8, 4, 'Girone B', 'B', 2);

-- Dati tabella teams
INSERT INTO `teams` (`id`, `edition_id`, `name`, `short_name`, `primary_color`, `secondary_color`, `logo_path`, `created_at`, `updated_at`) VALUES
(46, 4, 'Aurora Lynx', 'ALX', '#1E3A8A', '#F8FAFC', NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(47, 4, 'Nebula Bears', 'NBR', '#6D28D9', '#FDE68A', NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(48, 4, 'Vortex Owls', 'VOW', '#0F766E', '#ECFDF5', NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(49, 4, 'Titan Foxes', 'TFX', '#B45309', '#FFF7ED', NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(50, 4, 'Hydra Ravens', 'HRV', '#111827', '#A7F3D0', NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(51, 4, 'Quantum Bulls', 'QBL', '#BE123C', '#FEE2E2', NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(52, 4, 'Eclipse Wolves', 'EWL', '#334155', '#CBD5E1', NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(53, 4, 'Zenith Sharks', 'ZSH', '#0369A1', '#E0F2FE', NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35');

-- Dati tabella players
INSERT INTO `players` (`id`, `first_name`, `last_name`, `nickname`, `fiscal_code`, `address`, `phone_number`, `email`, `birth_date`, `photo_path`, `created_at`, `updated_at`) VALUES
(255, 'Nico', 'Ardesi', NULL, NULL, NULL, NULL, 'nico.ardesi.alx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(256, 'Loris', 'Bruma', NULL, NULL, NULL, NULL, 'loris.bruma.alx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(257, 'Tazio', 'Cervi', NULL, NULL, NULL, NULL, 'tazio.cervi.alx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(258, 'Elia', 'Dorsati', NULL, NULL, NULL, NULL, 'elia.dorsati.alx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(259, 'Milo', 'Evrani', NULL, NULL, NULL, NULL, 'milo.evrani.alx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(260, 'Dario', 'Foschi', NULL, NULL, NULL, NULL, 'dario.foschi.alx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(261, 'Ruben', 'Gavelli', NULL, NULL, NULL, NULL, 'ruben.gavelli.alx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(262, 'Ivan', 'Lunardi', NULL, NULL, NULL, NULL, 'ivan.lunardi.alx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(263, 'Samir', 'Velori', NULL, NULL, NULL, NULL, 'samir.velori.nbr@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(264, 'Tobia', 'Nerini', NULL, NULL, NULL, NULL, 'tobia.nerini.nbr@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(265, 'Aldo', 'Bramanti', NULL, NULL, NULL, NULL, 'aldo.bramanti.nbr@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(266, 'Gioele', 'Farnesi', NULL, NULL, NULL, NULL, 'gioele.farnesi.nbr@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(267, 'Mirko', 'Solari', NULL, NULL, NULL, NULL, 'mirko.solari.nbr@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(268, 'Enea', 'Carvani', NULL, NULL, NULL, NULL, 'enea.carvani.nbr@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(269, 'Pavel', 'Rimondi', NULL, NULL, NULL, NULL, 'pavel.rimondi.nbr@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(270, 'Leo', 'Granati', NULL, NULL, NULL, NULL, 'leo.granati.nbr@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(271, 'Mattia', 'Orvezi', NULL, NULL, NULL, NULL, 'mattia.orvezi.vow@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(272, 'Rocco', 'Mistrali', NULL, NULL, NULL, NULL, 'rocco.mistrali.vow@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(273, 'Alan', 'Serpieri', NULL, NULL, NULL, NULL, 'alan.serpieri.vow@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(274, 'Teo', 'Vardelli', NULL, NULL, NULL, NULL, 'teo.vardelli.vow@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(275, 'Goran', 'Lavecci', NULL, NULL, NULL, NULL, 'goran.lavecci.vow@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(276, 'Nadir', 'Colmari', NULL, NULL, NULL, NULL, 'nadir.colmari.vow@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(277, 'Oscar', 'Peveri', NULL, NULL, NULL, NULL, 'oscar.peveri.vow@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(278, 'Lapo', 'Ferendi', NULL, NULL, NULL, NULL, 'lapo.ferendi.vow@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(279, 'Bruno', 'Altieri', NULL, NULL, NULL, NULL, 'bruno.altieri.tfx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(280, 'Mauro', 'Cavalli', NULL, NULL, NULL, NULL, 'mauro.cavalli.tfx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(281, 'Dimitri', 'Levrini', NULL, NULL, NULL, NULL, 'dimitri.levrini.tfx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(282, 'Pietro', 'Savelli', NULL, NULL, NULL, NULL, 'pietro.savelli.tfx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(283, 'Kevin', 'Rontani', NULL, NULL, NULL, NULL, 'kevin.rontani.tfx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(284, 'Sandro', 'Bellora', NULL, NULL, NULL, NULL, 'sandro.bellora.tfx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(285, 'Yuri', 'Mancori', NULL, NULL, NULL, NULL, 'yuri.mancori.tfx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(286, 'Omar', 'Trevani', NULL, NULL, NULL, NULL, 'omar.trevani.tfx@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(287, 'Diego', 'Corvini', NULL, NULL, NULL, NULL, 'diego.corvini.hrv@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(288, 'Flavio', 'Nardelli', NULL, NULL, NULL, NULL, 'flavio.nardelli.hrv@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(289, 'Emil', 'Zanotti', NULL, NULL, NULL, NULL, 'emil.zanotti.hrv@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(290, 'Raul', 'Bernesi', NULL, NULL, NULL, NULL, 'raul.bernesi.hrv@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(291, 'Nando', 'Vitali', NULL, NULL, NULL, NULL, 'nando.vitali.hrv@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(292, 'Alvise', 'Montari', NULL, NULL, NULL, NULL, 'alvise.montari.hrv@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(293, 'Tomas', 'Speroni', NULL, NULL, NULL, NULL, 'tomas.speroni.hrv@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(294, 'Ciro', 'Dalmassi', NULL, NULL, NULL, NULL, 'ciro.dalmassi.hrv@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(295, 'Enrico', 'Marvelli', NULL, NULL, NULL, NULL, 'enrico.marvelli.qbl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(296, 'Luca', 'Quarzi', NULL, NULL, NULL, NULL, 'luca.quarzi.qbl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(297, 'Gabrio', 'Roventi', NULL, NULL, NULL, NULL, 'gabrio.roventi.qbl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(298, 'Michele', 'Torsani', NULL, NULL, NULL, NULL, 'michele.torsani.qbl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(299, 'Gino', 'Palmeri', NULL, NULL, NULL, NULL, 'gino.palmeri.qbl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(300, 'Fabian', 'Delori', NULL, NULL, NULL, NULL, 'fabian.delori.qbl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(301, 'Savio', 'Rocchi', NULL, NULL, NULL, NULL, 'savio.rocchi.qbl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(302, 'Marco', 'Bassetti', NULL, NULL, NULL, NULL, 'marco.bassetti.qbl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(303, 'Andrea', 'Noresi', NULL, NULL, NULL, NULL, 'andrea.noresi.ewl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(304, 'Valerio', 'Drusiani', NULL, NULL, NULL, NULL, 'valerio.drusiani.ewl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(305, 'Cristian', 'Menoli', NULL, NULL, NULL, NULL, 'cristian.menoli.ewl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(306, 'Manuel', 'Ravesi', NULL, NULL, NULL, NULL, 'manuel.ravesi.ewl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(307, 'Paco', 'Lentini', NULL, NULL, NULL, NULL, 'paco.lentini.ewl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(308, 'Giulio', 'Fersini', NULL, NULL, NULL, NULL, 'giulio.fersini.ewl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(309, 'Nicola', 'Baraldi', NULL, NULL, NULL, NULL, 'nicola.baraldi.ewl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(310, 'Filippo', 'Crespi', NULL, NULL, NULL, NULL, 'filippo.crespi.ewl@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(311, 'Davide', 'Zelati', NULL, NULL, NULL, NULL, 'davide.zelati.zsh@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(312, 'Alessio', 'Fulgori', NULL, NULL, NULL, NULL, 'alessio.fulgori.zsh@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(313, 'Riccardo', 'Moreni', NULL, NULL, NULL, NULL, 'riccardo.moreni.zsh@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(314, 'Simone', 'Calvani', NULL, NULL, NULL, NULL, 'simone.calvani.zsh@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(315, 'Ettore', 'Prandi', NULL, NULL, NULL, NULL, 'ettore.prandi.zsh@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(316, 'Giacomo', 'Verrini', NULL, NULL, NULL, NULL, 'giacomo.verrini.zsh@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(317, 'Federico', 'Sartori', NULL, NULL, NULL, NULL, 'federico.sartori.zsh@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(318, 'Lorenzo', 'Damiani', NULL, NULL, NULL, NULL, 'lorenzo.damiani.zsh@test.piazzetta.local', NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35');

-- Dati tabella sponsors
INSERT INTO `sponsors` (`id`, `name`, `description`, `image_path`, `is_active`, `sort_order`, `created_at`, `updated_at`) VALUES
(1, 'ADRIATICA OLI', 'ADRIATICA OLI', 'assets/images/sponsors/adriatica-oli-20260616163529596.png', 1, 1, '2026-06-16 16:35:32', '2026-06-16 16:35:32'),
(2, 'AVIS', 'AVIS', 'assets/images/sponsors/avis-20260616163542888.png', 1, 2, '2026-06-16 16:35:44', '2026-06-16 16:35:44'),
(3, 'BULLY', 'BULLY', 'assets/images/sponsors/bully-20260616163554495.png', 1, 3, '2026-06-16 16:35:55', '2026-06-16 16:35:55'),
(4, 'CASCIOTTI', 'CASCIOTTI', 'assets/images/sponsors/casciotti-20260616163606207.png', 1, 4, '2026-06-16 16:36:07', '2026-06-16 16:36:07'),
(5, 'CESETTI', 'CESETTI', 'assets/images/sponsors/cesetti-20260616163618185.png', 1, 5, '2026-06-16 16:36:19', '2026-06-16 16:36:19'),
(6, 'COAL', 'COAL', 'assets/images/sponsors/coal-20260616163626425.png', 1, 6, '2026-06-16 16:36:27', '2026-06-16 16:36:27'),
(7, 'EKO', 'EKO', 'assets/images/sponsors/eko-20260616163633841.png', 1, 7, '2026-06-16 16:36:35', '2026-06-16 16:36:35'),
(8, 'MARABINI', 'MARABINI', 'assets/images/sponsors/marabini-20260616163643006.png', 1, 8, '2026-06-16 16:36:44', '2026-06-16 16:36:44'),
(9, 'MILLEFRUTTI', 'MILLEFRUTTI', 'assets/images/sponsors/millefrutti-20260616163652824.png', 1, 9, '2026-06-16 16:36:53', '2026-06-16 16:36:53'),
(10, 'NATUAL VILLAGE', 'NATUAL VILLAGE', 'assets/images/sponsors/natual-village-20260616163701353.png', 1, 10, '2026-06-16 16:37:02', '2026-06-16 16:37:02'),
(11, 'NOI5', 'NOI5', 'assets/images/sponsors/noi5-20260616163719701.png', 1, 11, '2026-06-16 16:37:20', '2026-06-16 16:37:20'),
(12, 'QUOTA CS', 'QUOTA CS', 'assets/images/sponsors/quota-cs-20260616163748768.png', 1, 12, '2026-06-16 16:37:49', '2026-06-16 16:37:49'),
(13, 'RR', 'RR', 'assets/images/sponsors/rr-20260616163800125.png', 1, 13, '2026-06-16 16:38:04', '2026-06-16 16:38:04'),
(14, 'SEPA', 'SEPA', 'assets/images/sponsors/sepa-20260616163812567.png', 1, 14, '2026-06-16 16:38:13', '2026-06-16 16:38:13'),
(15, 'SKIFO', 'SKIFO', 'assets/images/sponsors/skifo_loghi-retro-20260616163834028.png', 1, 15, '2026-06-16 16:38:35', '2026-06-16 16:38:35'),
(16, 'SKYLAN', 'SKYLAN', 'assets/images/sponsors/skylan-20260616163849455.png', 1, 16, '2026-06-16 16:38:50', '2026-06-16 16:38:50'),
(17, 'SL', 'SL', 'assets/images/sponsors/sl-20260616163859594.png', 1, 17, '2026-06-16 16:39:00', '2026-06-16 16:39:00'),
(18, 'SOLERO', 'SOLERO', 'assets/images/sponsors/solero-20260616163944439.png', 1, 18, '2026-06-16 16:39:45', '2026-06-16 16:39:45'),
(19, 'SOTTOVOCE', 'SOTTOVOCE', 'assets/images/sponsors/sottovoce_tavola-disegno-1-20260616163955718.png', 1, 19, '2026-06-16 16:39:56', '2026-06-16 16:39:56'),
(20, 'THERA', 'THERA', 'assets/images/sponsors/thera-20260616164004025.png', 1, 20, '2026-06-16 16:40:05', '2026-06-16 16:40:05'),
(21, 'VIA MONTENAPOLEONE', 'VIA MONTENAPOLEONE', 'assets/images/sponsors/via-montenapoleone-20260616164013264.png', 1, 21, '2026-06-16 16:40:16', '2026-06-16 16:40:16');

-- Dati tabella staff
INSERT INTO `staff` (`id`, `nome`, `ruolo`, `categoria`, `bio`, `foto`, `ig`) VALUES
(1, 'Francesco Emiliani', 'Tiranno', 'founders', 'Il capo supremo della Piazzetta Madness. Nessuna decisione passa senza il suo benestare.', '', ''),
(2, 'NicolÃ² Purifico', 'Finto Fondatore', 'founders', 'Fondatore fondamentale... o quasi. Presente fin dal primo canestro, anche quando non si vede.', '', ''),
(3, 'Federico Pierleoni', 'Cofondatore', 'founders', 'Tra i padri fondatori della Madness. In campo e fuori, uno dei pilastri del torneo.', '', ''),
(4, 'Giorgio Monteriu\'', 'Cofondatore', 'founders', 'Cofondatore e anima del torneo fin dalla prima edizione. Sempre in prima linea.', '', ''),
(5, 'Daniele Raccosta', 'Cofondatore', 'founders', 'Tra i fondatori storici della Piazzetta Madness. Punto di riferimento dentro e fuori dal campo.', '', ''),
(6, 'Alessia Principi', 'Designer', 'social', 'Cura l\'identitÃ  visiva del torneo: grafiche, contenuti e tutto ciÃ² che si vede online.', '', ''),
(7, 'Roberto Carbone', 'Fotografo', 'social', 'Fotografo ufficiale della Madness. Cattura ogni momento epico del torneo.', '', ''),
(8, 'NicolÃ² Micucci', 'Designer', 'social', 'Graphic designer dei canali social della Madness. Trasforma idee in contenuti che bucano lo schermo.', '', ''),
(9, 'Lorenzo Rebichini', 'Full Stack Developer â€” Tabellone & Database', 'it', 'Sviluppatore del tabellone digitale e del database. Tutta la tecnologia del torneo passa da lui.', '', ''),
(10, 'Francesco Malaccari', 'Full Stack Developer & System Integrator', 'it', 'Progetta e integra il sistema backend della Madness. Dal server al campo, tutto connesso.', '', ''),
(11, 'Mattia Boschi', 'Frontend Developer & UI/UX Designer', 'it', 'Interfaccia, grafica e chiamate API del sito. La Piazzetta Madness ha la sua faccia grazie a lui.', '', ''),
(12, 'Paolo Tiberi', 'Responsabile statistiche', 'collaboratori', 'Raccoglie e gestisce tutte le statistiche del torneo con precisione chirurgica. I numeri non mentono.', '', '');

-- Dati tabella group_teams
INSERT INTO `group_teams` (`id`, `group_id`, `team_id`, `seed_label`) VALUES
(31, 7, 46, 'A1'),
(32, 7, 47, 'A2'),
(33, 7, 48, 'A3'),
(34, 7, 49, 'A4'),
(35, 8, 50, 'B1'),
(36, 8, 51, 'B2'),
(37, 8, 52, 'B3'),
(38, 8, 53, 'B4');

-- Dati tabella standings
INSERT INTO `standings` (`id`, `group_id`, `team_id`, `played`, `wins`, `losses`, `points_for`, `points_against`, `point_difference`, `ranking_points`, `position`, `tie_break_note`) VALUES
(31, 7, 46, 0, 0, 0, 0, 0, 0, 0, 1, NULL),
(32, 7, 47, 0, 0, 0, 0, 0, 0, 0, 2, NULL),
(33, 7, 48, 0, 0, 0, 0, 0, 0, 0, 3, NULL),
(34, 7, 49, 0, 0, 0, 0, 0, 0, 0, 4, NULL),
(35, 8, 50, 0, 0, 0, 0, 0, 0, 0, 1, NULL),
(36, 8, 51, 0, 0, 0, 0, 0, 0, 0, 2, NULL),
(37, 8, 52, 0, 0, 0, 0, 0, 0, 0, 3, NULL),
(38, 8, 53, 0, 0, 0, 0, 0, 0, 0, 4, NULL);

-- Dati tabella team_rosters
INSERT INTO `team_rosters` (`id`, `team_id`, `player_id`, `jersey_number`, `role`, `is_captain`, `is_active`, `created_at`, `updated_at`) VALUES
(255, 46, 255, 3, 'Playmaker', 1, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(256, 46, 256, 6, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(257, 46, 257, 9, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(258, 46, 258, 12, 'Ala forte', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(259, 46, 259, 15, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(260, 46, 260, 18, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(261, 46, 261, 21, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(262, 46, 262, 24, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(263, 47, 263, 3, 'Playmaker', 1, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(264, 47, 264, 6, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(265, 47, 265, 9, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(266, 47, 266, 12, 'Ala forte', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(267, 47, 267, 15, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(268, 47, 268, 18, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(269, 47, 269, 21, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(270, 47, 270, 24, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(271, 48, 271, 3, 'Playmaker', 1, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(272, 48, 272, 6, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(273, 48, 273, 9, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(274, 48, 274, 12, 'Ala forte', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(275, 48, 275, 15, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(276, 48, 276, 18, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(277, 48, 277, 21, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(278, 48, 278, 24, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(279, 49, 279, 3, 'Playmaker', 1, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(280, 49, 280, 6, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(281, 49, 281, 9, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(282, 49, 282, 12, 'Ala forte', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(283, 49, 283, 15, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(284, 49, 284, 18, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(285, 49, 285, 21, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(286, 49, 286, 24, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(287, 50, 287, 3, 'Playmaker', 1, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(288, 50, 288, 6, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(289, 50, 289, 9, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(290, 50, 290, 12, 'Ala forte', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(291, 50, 291, 15, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(292, 50, 292, 18, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(293, 50, 293, 21, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(294, 50, 294, 24, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(295, 51, 295, 3, 'Playmaker', 1, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(296, 51, 296, 6, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(297, 51, 297, 9, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(298, 51, 298, 12, 'Ala forte', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(299, 51, 299, 15, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(300, 51, 300, 18, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(301, 51, 301, 21, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(302, 51, 302, 24, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(303, 52, 303, 3, 'Playmaker', 1, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(304, 52, 304, 6, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(305, 52, 305, 9, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(306, 52, 306, 12, 'Ala forte', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(307, 52, 307, 15, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(308, 52, 308, 18, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(309, 52, 309, 21, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(310, 52, 310, 24, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(311, 53, 311, 3, 'Playmaker', 1, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(312, 53, 312, 6, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(313, 53, 313, 9, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(314, 53, 314, 12, 'Ala forte', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(315, 53, 315, 15, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(316, 53, 316, 18, 'Guardia', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(317, 53, 317, 21, 'Ala', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(318, 53, 318, 24, 'Centro', 0, 1, '2026-06-16 16:27:35', '2026-06-16 16:27:35');

-- Dati tabella matches
INSERT INTO `matches` (`id`, `edition_id`, `group_id`, `court_id`, `name`, `phase`, `round`, `scheduled_start_at`, `scheduled_end_at`, `actual_start_at`, `actual_end_at`, `status`, `period_count`, `period_duration_ms`, `break_duration_ms`, `overtime_duration_ms`, `shot_clock_ms`, `timeout_duration_ms`, `timeouts_per_team`, `timeouts_per_period`, `personal_foul_limit`, `team_foul_bonus_threshold`, `max_score`, `max_score_enabled`, `stop_clock_on_free_throws`, `winner_team_id`, `win_reason`, `notes`, `created_at`, `updated_at`) VALUES
(63, 4, 7, 4, 'Aurora Lynx vs Nebula Bears', 'GroupStage', 'Giornata 1', '2026-07-08 20:30:00', '2026-07-08 21:15:00', NULL, NULL, 'Ready', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:32:37'),
(64, 4, 8, 4, 'Hydra Ravens vs Quantum Bulls', 'GroupStage', 'Giornata 1', '2026-07-08 21:15:00', '2026-07-08 22:00:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(65, 4, 7, 4, 'Vortex Owls vs Titan Foxes', 'GroupStage', 'Giornata 1', '2026-07-08 22:30:00', '2026-07-08 23:15:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(66, 4, 8, 4, 'Eclipse Wolves vs Zenith Sharks', 'GroupStage', 'Giornata 1', '2026-07-08 23:15:00', '2026-07-09 00:00:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(67, 4, 8, 4, 'Hydra Ravens vs Eclipse Wolves', 'GroupStage', 'Giornata 2', '2026-07-09 20:30:00', '2026-07-09 21:15:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(68, 4, 7, 4, 'Aurora Lynx vs Vortex Owls', 'GroupStage', 'Giornata 2', '2026-07-09 21:15:00', '2026-07-09 22:00:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(69, 4, 8, 4, 'Quantum Bulls vs Zenith Sharks', 'GroupStage', 'Giornata 2', '2026-07-09 22:30:00', '2026-07-09 23:15:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(70, 4, 7, 4, 'Nebula Bears vs Titan Foxes', 'GroupStage', 'Giornata 2', '2026-07-09 23:15:00', '2026-07-10 00:00:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(71, 4, 7, 4, 'Aurora Lynx vs Titan Foxes', 'GroupStage', 'Giornata 3', '2026-07-10 20:30:00', '2026-07-10 21:15:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(72, 4, 8, 4, 'Hydra Ravens vs Zenith Sharks', 'GroupStage', 'Giornata 3', '2026-07-10 21:15:00', '2026-07-10 22:00:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(73, 4, 7, 4, 'Nebula Bears vs Vortex Owls', 'GroupStage', 'Giornata 3', '2026-07-10 22:30:00', '2026-07-10 23:15:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(74, 4, 8, 4, 'Quantum Bulls vs Eclipse Wolves', 'GroupStage', 'Giornata 3', '2026-07-10 23:15:00', '2026-07-11 00:00:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(75, 4, NULL, 4, 'Semifinale 1 - 1Âª A vs 2Âª B', 'SemiFinal', 'Final Four', '2026-07-11 20:00:00', '2026-07-11 20:45:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(76, 4, NULL, 4, 'Semifinale 2 - 1Âª B vs 2Âª A', 'SemiFinal', 'Final Four', '2026-07-11 20:45:00', '2026-07-11 21:30:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(77, 4, NULL, 4, 'Finale 3-4 - Perdente SF1 vs Perdente SF2', 'ThirdPlaceFinal', 'Final Four', '2026-07-11 22:15:00', '2026-07-11 23:00:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35'),
(78, 4, NULL, 4, 'Finale 1-2 - Vincente SF1 vs Vincente SF2', 'Final', 'Final Four', '2026-07-11 23:00:00', '2026-07-11 23:45:00', NULL, NULL, 'Scheduled', 2, 720000, 120000, 120000, 24000, 30000, 2, 1, 5, 5, NULL, 0, 1, NULL, NULL, NULL, '2026-06-16 16:27:35', '2026-06-16 16:27:35');

-- Dati tabella match_teams
INSERT INTO `match_teams` (`id`, `match_id`, `team_id`, `side`, `score`, `fouls_current_period`, `timeouts_used_total`, `timeouts_used_period`, `is_winner`, `forfeit_score`) VALUES
(1, 71, 46, 'Home', 0, 0, 0, 0, 0, NULL),
(2, 68, 46, 'Home', 0, 0, 0, 0, 0, NULL),
(3, 63, 46, 'Home', 0, 0, 0, 0, 0, NULL),
(4, 73, 47, 'Home', 0, 0, 0, 0, 0, NULL),
(5, 70, 47, 'Home', 0, 0, 0, 0, 0, NULL),
(6, 65, 48, 'Home', 0, 0, 0, 0, 0, NULL),
(7, 72, 50, 'Home', 0, 0, 0, 0, 0, NULL),
(8, 67, 50, 'Home', 0, 0, 0, 0, 0, NULL),
(9, 64, 50, 'Home', 0, 0, 0, 0, 0, NULL),
(10, 74, 51, 'Home', 0, 0, 0, 0, 0, NULL),
(11, 69, 51, 'Home', 0, 0, 0, 0, 0, NULL),
(12, 66, 52, 'Home', 0, 0, 0, 0, 0, NULL),
(16, 63, 47, 'Away', 0, 0, 0, 0, 0, NULL),
(17, 64, 51, 'Away', 0, 0, 0, 0, 0, NULL),
(18, 65, 49, 'Away', 0, 0, 0, 0, 0, NULL),
(19, 66, 53, 'Away', 0, 0, 0, 0, 0, NULL),
(20, 67, 52, 'Away', 0, 0, 0, 0, 0, NULL),
(21, 68, 48, 'Away', 0, 0, 0, 0, 0, NULL),
(22, 69, 53, 'Away', 0, 0, 0, 0, 0, NULL),
(23, 70, 49, 'Away', 0, 0, 0, 0, 0, NULL),
(24, 71, 49, 'Away', 0, 0, 0, 0, 0, NULL),
(25, 72, 53, 'Away', 0, 0, 0, 0, 0, NULL),
(26, 73, 48, 'Away', 0, 0, 0, 0, 0, NULL),
(27, 74, 52, 'Away', 0, 0, 0, 0, 0, NULL);

-- Dati tabella match_players
INSERT INTO `match_players` (`id`, `match_id`, `team_id`, `player_id`, `jersey_number`, `is_starting_five`, `is_on_court`, `points`, `personal_fouls`, `is_fouled_out`, `is_ejected`) VALUES
(1, 63, 46, 255, 3, 0, 0, 0, 0, 0, 0),
(2, 63, 46, 256, 6, 0, 0, 0, 0, 0, 0),
(3, 63, 46, 257, 9, 0, 0, 0, 0, 0, 0),
(4, 63, 46, 258, 12, 0, 0, 0, 0, 0, 0),
(5, 63, 46, 259, 15, 0, 0, 0, 0, 0, 0),
(6, 63, 46, 260, 18, 0, 0, 0, 0, 0, 0),
(7, 63, 46, 261, 21, 0, 0, 0, 0, 0, 0),
(8, 63, 46, 262, 24, 0, 0, 0, 0, 0, 0),
(9, 63, 47, 263, 3, 0, 0, 0, 0, 0, 0),
(10, 63, 47, 264, 6, 0, 0, 0, 0, 0, 0),
(11, 63, 47, 265, 9, 0, 0, 0, 0, 0, 0),
(12, 63, 47, 266, 12, 0, 0, 0, 0, 0, 0),
(13, 63, 47, 267, 15, 0, 0, 0, 0, 0, 0),
(14, 63, 47, 268, 18, 0, 0, 0, 0, 0, 0),
(15, 63, 47, 269, 21, 0, 0, 0, 0, 0, 0),
(16, 63, 47, 270, 24, 0, 0, 0, 0, 0, 0),
(17, 64, 50, 287, 3, 0, 0, 0, 0, 0, 0),
(18, 64, 50, 288, 6, 0, 0, 0, 0, 0, 0),
(19, 64, 50, 289, 9, 0, 0, 0, 0, 0, 0),
(20, 64, 50, 290, 12, 0, 0, 0, 0, 0, 0),
(21, 64, 50, 291, 15, 0, 0, 0, 0, 0, 0),
(22, 64, 50, 292, 18, 0, 0, 0, 0, 0, 0),
(23, 64, 50, 293, 21, 0, 0, 0, 0, 0, 0),
(24, 64, 50, 294, 24, 0, 0, 0, 0, 0, 0),
(25, 64, 51, 295, 3, 0, 0, 0, 0, 0, 0),
(26, 64, 51, 296, 6, 0, 0, 0, 0, 0, 0),
(27, 64, 51, 297, 9, 0, 0, 0, 0, 0, 0),
(28, 64, 51, 298, 12, 0, 0, 0, 0, 0, 0),
(29, 64, 51, 299, 15, 0, 0, 0, 0, 0, 0),
(30, 64, 51, 300, 18, 0, 0, 0, 0, 0, 0),
(31, 64, 51, 301, 21, 0, 0, 0, 0, 0, 0),
(32, 64, 51, 302, 24, 0, 0, 0, 0, 0, 0),
(33, 65, 48, 271, 3, 0, 0, 0, 0, 0, 0),
(34, 65, 48, 272, 6, 0, 0, 0, 0, 0, 0),
(35, 65, 48, 273, 9, 0, 0, 0, 0, 0, 0),
(36, 65, 48, 274, 12, 0, 0, 0, 0, 0, 0),
(37, 65, 48, 275, 15, 0, 0, 0, 0, 0, 0),
(38, 65, 48, 276, 18, 0, 0, 0, 0, 0, 0),
(39, 65, 48, 277, 21, 0, 0, 0, 0, 0, 0),
(40, 65, 48, 278, 24, 0, 0, 0, 0, 0, 0),
(41, 65, 49, 279, 3, 0, 0, 0, 0, 0, 0),
(42, 65, 49, 280, 6, 0, 0, 0, 0, 0, 0),
(43, 65, 49, 281, 9, 0, 0, 0, 0, 0, 0),
(44, 65, 49, 282, 12, 0, 0, 0, 0, 0, 0),
(45, 65, 49, 283, 15, 0, 0, 0, 0, 0, 0),
(46, 65, 49, 284, 18, 0, 0, 0, 0, 0, 0),
(47, 65, 49, 285, 21, 0, 0, 0, 0, 0, 0),
(48, 65, 49, 286, 24, 0, 0, 0, 0, 0, 0),
(49, 66, 52, 303, 3, 0, 0, 0, 0, 0, 0),
(50, 66, 52, 304, 6, 0, 0, 0, 0, 0, 0),
(51, 66, 52, 305, 9, 0, 0, 0, 0, 0, 0),
(52, 66, 52, 306, 12, 0, 0, 0, 0, 0, 0),
(53, 66, 52, 307, 15, 0, 0, 0, 0, 0, 0),
(54, 66, 52, 308, 18, 0, 0, 0, 0, 0, 0),
(55, 66, 52, 309, 21, 0, 0, 0, 0, 0, 0),
(56, 66, 52, 310, 24, 0, 0, 0, 0, 0, 0),
(57, 66, 53, 311, 3, 0, 0, 0, 0, 0, 0),
(58, 66, 53, 312, 6, 0, 0, 0, 0, 0, 0),
(59, 66, 53, 313, 9, 0, 0, 0, 0, 0, 0),
(60, 66, 53, 314, 12, 0, 0, 0, 0, 0, 0),
(61, 66, 53, 315, 15, 0, 0, 0, 0, 0, 0),
(62, 66, 53, 316, 18, 0, 0, 0, 0, 0, 0),
(63, 66, 53, 317, 21, 0, 0, 0, 0, 0, 0),
(64, 66, 53, 318, 24, 0, 0, 0, 0, 0, 0),
(65, 67, 50, 287, 3, 0, 0, 0, 0, 0, 0),
(66, 67, 50, 288, 6, 0, 0, 0, 0, 0, 0),
(67, 67, 50, 289, 9, 0, 0, 0, 0, 0, 0),
(68, 67, 50, 290, 12, 0, 0, 0, 0, 0, 0),
(69, 67, 50, 291, 15, 0, 0, 0, 0, 0, 0),
(70, 67, 50, 292, 18, 0, 0, 0, 0, 0, 0),
(71, 67, 50, 293, 21, 0, 0, 0, 0, 0, 0),
(72, 67, 50, 294, 24, 0, 0, 0, 0, 0, 0),
(73, 67, 52, 303, 3, 0, 0, 0, 0, 0, 0),
(74, 67, 52, 304, 6, 0, 0, 0, 0, 0, 0),
(75, 67, 52, 305, 9, 0, 0, 0, 0, 0, 0),
(76, 67, 52, 306, 12, 0, 0, 0, 0, 0, 0),
(77, 67, 52, 307, 15, 0, 0, 0, 0, 0, 0),
(78, 67, 52, 308, 18, 0, 0, 0, 0, 0, 0),
(79, 67, 52, 309, 21, 0, 0, 0, 0, 0, 0),
(80, 67, 52, 310, 24, 0, 0, 0, 0, 0, 0),
(81, 68, 46, 255, 3, 0, 0, 0, 0, 0, 0),
(82, 68, 46, 256, 6, 0, 0, 0, 0, 0, 0),
(83, 68, 46, 257, 9, 0, 0, 0, 0, 0, 0),
(84, 68, 46, 258, 12, 0, 0, 0, 0, 0, 0),
(85, 68, 46, 259, 15, 0, 0, 0, 0, 0, 0),
(86, 68, 46, 260, 18, 0, 0, 0, 0, 0, 0),
(87, 68, 46, 261, 21, 0, 0, 0, 0, 0, 0),
(88, 68, 46, 262, 24, 0, 0, 0, 0, 0, 0),
(89, 68, 48, 271, 3, 0, 0, 0, 0, 0, 0),
(90, 68, 48, 272, 6, 0, 0, 0, 0, 0, 0),
(91, 68, 48, 273, 9, 0, 0, 0, 0, 0, 0),
(92, 68, 48, 274, 12, 0, 0, 0, 0, 0, 0),
(93, 68, 48, 275, 15, 0, 0, 0, 0, 0, 0),
(94, 68, 48, 276, 18, 0, 0, 0, 0, 0, 0),
(95, 68, 48, 277, 21, 0, 0, 0, 0, 0, 0),
(96, 68, 48, 278, 24, 0, 0, 0, 0, 0, 0),
(97, 69, 51, 295, 3, 0, 0, 0, 0, 0, 0),
(98, 69, 51, 296, 6, 0, 0, 0, 0, 0, 0),
(99, 69, 51, 297, 9, 0, 0, 0, 0, 0, 0),
(100, 69, 51, 298, 12, 0, 0, 0, 0, 0, 0),
(101, 69, 51, 299, 15, 0, 0, 0, 0, 0, 0),
(102, 69, 51, 300, 18, 0, 0, 0, 0, 0, 0),
(103, 69, 51, 301, 21, 0, 0, 0, 0, 0, 0),
(104, 69, 51, 302, 24, 0, 0, 0, 0, 0, 0),
(105, 69, 53, 311, 3, 0, 0, 0, 0, 0, 0),
(106, 69, 53, 312, 6, 0, 0, 0, 0, 0, 0),
(107, 69, 53, 313, 9, 0, 0, 0, 0, 0, 0),
(108, 69, 53, 314, 12, 0, 0, 0, 0, 0, 0),
(109, 69, 53, 315, 15, 0, 0, 0, 0, 0, 0),
(110, 69, 53, 316, 18, 0, 0, 0, 0, 0, 0),
(111, 69, 53, 317, 21, 0, 0, 0, 0, 0, 0),
(112, 69, 53, 318, 24, 0, 0, 0, 0, 0, 0),
(113, 70, 47, 263, 3, 0, 0, 0, 0, 0, 0),
(114, 70, 47, 264, 6, 0, 0, 0, 0, 0, 0),
(115, 70, 47, 265, 9, 0, 0, 0, 0, 0, 0),
(116, 70, 47, 266, 12, 0, 0, 0, 0, 0, 0),
(117, 70, 47, 267, 15, 0, 0, 0, 0, 0, 0),
(118, 70, 47, 268, 18, 0, 0, 0, 0, 0, 0),
(119, 70, 47, 269, 21, 0, 0, 0, 0, 0, 0),
(120, 70, 47, 270, 24, 0, 0, 0, 0, 0, 0),
(121, 70, 49, 279, 3, 0, 0, 0, 0, 0, 0),
(122, 70, 49, 280, 6, 0, 0, 0, 0, 0, 0),
(123, 70, 49, 281, 9, 0, 0, 0, 0, 0, 0),
(124, 70, 49, 282, 12, 0, 0, 0, 0, 0, 0),
(125, 70, 49, 283, 15, 0, 0, 0, 0, 0, 0),
(126, 70, 49, 284, 18, 0, 0, 0, 0, 0, 0),
(127, 70, 49, 285, 21, 0, 0, 0, 0, 0, 0),
(128, 70, 49, 286, 24, 0, 0, 0, 0, 0, 0),
(129, 71, 46, 255, 3, 0, 0, 0, 0, 0, 0),
(130, 71, 46, 256, 6, 0, 0, 0, 0, 0, 0),
(131, 71, 46, 257, 9, 0, 0, 0, 0, 0, 0),
(132, 71, 46, 258, 12, 0, 0, 0, 0, 0, 0),
(133, 71, 46, 259, 15, 0, 0, 0, 0, 0, 0),
(134, 71, 46, 260, 18, 0, 0, 0, 0, 0, 0),
(135, 71, 46, 261, 21, 0, 0, 0, 0, 0, 0),
(136, 71, 46, 262, 24, 0, 0, 0, 0, 0, 0),
(137, 71, 49, 279, 3, 0, 0, 0, 0, 0, 0),
(138, 71, 49, 280, 6, 0, 0, 0, 0, 0, 0),
(139, 71, 49, 281, 9, 0, 0, 0, 0, 0, 0),
(140, 71, 49, 282, 12, 0, 0, 0, 0, 0, 0),
(141, 71, 49, 283, 15, 0, 0, 0, 0, 0, 0),
(142, 71, 49, 284, 18, 0, 0, 0, 0, 0, 0),
(143, 71, 49, 285, 21, 0, 0, 0, 0, 0, 0),
(144, 71, 49, 286, 24, 0, 0, 0, 0, 0, 0),
(145, 72, 50, 287, 3, 0, 0, 0, 0, 0, 0),
(146, 72, 50, 288, 6, 0, 0, 0, 0, 0, 0),
(147, 72, 50, 289, 9, 0, 0, 0, 0, 0, 0),
(148, 72, 50, 290, 12, 0, 0, 0, 0, 0, 0),
(149, 72, 50, 291, 15, 0, 0, 0, 0, 0, 0),
(150, 72, 50, 292, 18, 0, 0, 0, 0, 0, 0),
(151, 72, 50, 293, 21, 0, 0, 0, 0, 0, 0),
(152, 72, 50, 294, 24, 0, 0, 0, 0, 0, 0),
(153, 72, 53, 311, 3, 0, 0, 0, 0, 0, 0),
(154, 72, 53, 312, 6, 0, 0, 0, 0, 0, 0),
(155, 72, 53, 313, 9, 0, 0, 0, 0, 0, 0),
(156, 72, 53, 314, 12, 0, 0, 0, 0, 0, 0),
(157, 72, 53, 315, 15, 0, 0, 0, 0, 0, 0),
(158, 72, 53, 316, 18, 0, 0, 0, 0, 0, 0),
(159, 72, 53, 317, 21, 0, 0, 0, 0, 0, 0),
(160, 72, 53, 318, 24, 0, 0, 0, 0, 0, 0),
(161, 73, 47, 263, 3, 0, 0, 0, 0, 0, 0),
(162, 73, 47, 264, 6, 0, 0, 0, 0, 0, 0),
(163, 73, 47, 265, 9, 0, 0, 0, 0, 0, 0),
(164, 73, 47, 266, 12, 0, 0, 0, 0, 0, 0),
(165, 73, 47, 267, 15, 0, 0, 0, 0, 0, 0),
(166, 73, 47, 268, 18, 0, 0, 0, 0, 0, 0),
(167, 73, 47, 269, 21, 0, 0, 0, 0, 0, 0),
(168, 73, 47, 270, 24, 0, 0, 0, 0, 0, 0),
(169, 73, 48, 271, 3, 0, 0, 0, 0, 0, 0),
(170, 73, 48, 272, 6, 0, 0, 0, 0, 0, 0),
(171, 73, 48, 273, 9, 0, 0, 0, 0, 0, 0),
(172, 73, 48, 274, 12, 0, 0, 0, 0, 0, 0),
(173, 73, 48, 275, 15, 0, 0, 0, 0, 0, 0),
(174, 73, 48, 276, 18, 0, 0, 0, 0, 0, 0),
(175, 73, 48, 277, 21, 0, 0, 0, 0, 0, 0),
(176, 73, 48, 278, 24, 0, 0, 0, 0, 0, 0),
(177, 74, 51, 295, 3, 0, 0, 0, 0, 0, 0),
(178, 74, 51, 296, 6, 0, 0, 0, 0, 0, 0),
(179, 74, 51, 297, 9, 0, 0, 0, 0, 0, 0),
(180, 74, 51, 298, 12, 0, 0, 0, 0, 0, 0),
(181, 74, 51, 299, 15, 0, 0, 0, 0, 0, 0),
(182, 74, 51, 300, 18, 0, 0, 0, 0, 0, 0),
(183, 74, 51, 301, 21, 0, 0, 0, 0, 0, 0),
(184, 74, 51, 302, 24, 0, 0, 0, 0, 0, 0),
(185, 74, 52, 303, 3, 0, 0, 0, 0, 0, 0),
(186, 74, 52, 304, 6, 0, 0, 0, 0, 0, 0),
(187, 74, 52, 305, 9, 0, 0, 0, 0, 0, 0),
(188, 74, 52, 306, 12, 0, 0, 0, 0, 0, 0),
(189, 74, 52, 307, 15, 0, 0, 0, 0, 0, 0),
(190, 74, 52, 308, 18, 0, 0, 0, 0, 0, 0),
(191, 74, 52, 309, 21, 0, 0, 0, 0, 0, 0),
(192, 74, 52, 310, 24, 0, 0, 0, 0, 0, 0);

-- Dati tabella scoreboard_states
INSERT INTO `scoreboard_states` (`id`, `match_id`, `current_period`, `current_period_type`, `game_clock_ms_remaining`, `shot_clock_ms_remaining`, `timeout_clock_ms_remaining`, `break_clock_ms_remaining`, `is_game_clock_running`, `is_shot_clock_running`, `is_timeout_running`, `is_break_running`, `possession_team_id`, `home_score`, `away_score`, `home_fouls_current_period`, `away_fouls_current_period`, `home_timeouts_used_total`, `away_timeouts_used_total`, `last_event_id`, `last_updated_at`) VALUES
(1, 63, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:48:02'),
(2, 64, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(3, 65, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(4, 66, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(5, 67, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(6, 68, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(7, 69, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(8, 70, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(9, 71, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(10, 72, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(11, 73, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(12, 74, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(13, 75, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(14, 76, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(15, 77, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35'),
(16, 78, 1, 'Regular', 720000, 24000, NULL, NULL, 0, 0, 0, 0, NULL, 0, 0, 0, 0, 0, 0, NULL, '2026-06-16 16:27:35');

-- Dati tabella competition_events
INSERT INTO `competition_events` (`id`, `edition_id`, `event_type`, `name`, `scheduled_start_at`, `scheduled_end_at`, `status`) VALUES
(1, 4, 'Other', 'Intervallo - mercoledÃ¬ 8 luglio', '2026-07-08 22:00:00', '2026-07-08 22:30:00', 'Scheduled'),
(2, 4, 'Other', 'Intervallo - giovedÃ¬ 9 luglio', '2026-07-09 22:00:00', '2026-07-09 22:30:00', 'Scheduled'),
(3, 4, 'Other', 'Intervallo - venerdÃ¬ 10 luglio', '2026-07-10 22:00:00', '2026-07-10 22:30:00', 'Scheduled'),
(4, 4, 'ThreePointContest', '3 Point Contest', '2026-07-11 21:30:00', '2026-07-11 22:15:00', 'Scheduled'),
(5, 4, 'AwardCeremony', 'Premiazione', '2026-07-11 23:45:00', '2026-07-12 00:15:00', 'Scheduled');

-- Dati tabella three_point_contest_entries
INSERT INTO `three_point_contest_entries` (`id`, `competition_event_id`, `team_id`, `player_id`, `seed_order`, `total_score`, `final_position`) VALUES
(1, 4, 46, 255, 1, 0, NULL),
(2, 4, 52, 310, 2, 0, NULL),
(3, 4, 50, 294, 3, 0, NULL),
(4, 4, 47, 270, 4, 0, NULL);

-- Dati tabella three_point_contest_rounds
INSERT INTO `three_point_contest_rounds` (`id`, `entry_id`, `round_number`, `round_type`, `station1_score`, `station2_score`, `station3_score`, `station4_score`, `station5_score`, `total_score`, `notes`) VALUES
(1, 1, 1, 'Qualification', 0, 0, 0, 0, 0, 0, '{\"Status\":\"Ready\",\"Station\":1,\"ClockMs\":60000}'),
(2, 2, 1, 'Qualification', 0, 0, 0, 0, 0, 0, NULL),
(3, 3, 1, 'Qualification', 0, 0, 0, 0, 0, 0, NULL),
(4, 4, 1, 'Qualification', 0, 0, 0, 0, 0, 0, NULL);

-- Dati tabella match_events
INSERT INTO `match_events` (`id`, `match_id`, `period`, `period_type`, `game_clock_ms_remaining`, `shot_clock_ms_remaining`, `team_id`, `player_id`, `event_type`, `points`, `is_correction`, `reverts_event_id`, `description`, `payload_json`, `created_at`, `synced_at`) VALUES
(1, 63, 1, 'Regular', 720000, 24000, NULL, NULL, 'MatchReset', NULL, 1, NULL, 'Sessione live annullata e partita riportata allo stato Scheduled', NULL, '2026-06-16 16:29:19', NULL);

COMMIT;
SET FOREIGN_KEY_CHECKS = 1;

