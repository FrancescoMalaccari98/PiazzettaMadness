-- Aggiunge un campo dedicato alla console per le foto giocatore.
-- Non modifica players.photo_path, che resta riservato al sito.

ALTER TABLE `players`
  ADD COLUMN `console_photo_path` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL
  AFTER `photo_path`;
