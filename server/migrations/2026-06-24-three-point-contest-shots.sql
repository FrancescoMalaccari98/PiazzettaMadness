-- Introduce i 25 esiti per ogni prova e azzera i dati contest non ufficiali.
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS three_point_contest_shots (
  id int UNSIGNED NOT NULL AUTO_INCREMENT,
  round_id int UNSIGNED NOT NULL,
  station_number tinyint UNSIGNED NOT NULL,
  ball_number tinyint UNSIGNED NOT NULL,
  point_value tinyint UNSIGNED NOT NULL,
  result enum('Pending','Made','Missed') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Pending',
  PRIMARY KEY (id),
  UNIQUE KEY ux_three_point_shots_round_station_ball (round_id,station_number,ball_number),
  CONSTRAINT fk_three_point_shots_round FOREIGN KEY (round_id)
    REFERENCES three_point_contest_rounds (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

DELETE FROM three_point_contest_shots;
DELETE FROM three_point_contest_rounds;
DELETE FROM three_point_contest_entries;
UPDATE competition_events SET status = 'Scheduled' WHERE event_type = 'ThreePointContest';

ALTER TABLE three_point_contest_shots AUTO_INCREMENT = 1;
ALTER TABLE three_point_contest_rounds AUTO_INCREMENT = 1;
ALTER TABLE three_point_contest_entries AUTO_INCREMENT = 1;
