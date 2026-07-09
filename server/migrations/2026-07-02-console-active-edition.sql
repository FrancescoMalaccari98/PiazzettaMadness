ALTER TABLE `editions`
  ADD COLUMN `is_console_active` tinyint(1) NOT NULL DEFAULT 0 AFTER `status`;

CREATE UNIQUE INDEX `ux_editions_console_active`
  ON `editions` ((CASE WHEN `is_console_active` = 1 THEN `is_console_active` ELSE NULL END));
