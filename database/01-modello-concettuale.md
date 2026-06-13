# Fase 1 — Modello concettuale (entità e relazioni)

Modello concettuale dello schema DB del sistema Live Basket. Nessun SQL: solo
entità, attributi principali, relazioni e cardinalità. È la base da validare
prima di scrivere il DDL.

Legenda cardinalità: `1—N` (uno a molti), `N—N` (molti a molti), `1—1` (uno a uno).

---

## 1. Mappa delle entità

```
Tournament 1─N Edition 1─┬─N TournamentGroup ─N─N─ Team   (via GroupTeam)
                         ├─N Team 1─N Roster ─N─1 Player
                         └─N Match
Match 1─N MatchPeriod
Match 1─1 LiveMatchState          (stato live corrente, dal tabellone)
Match 1─N LivePlayerState         (punti/falli live per giocatore)
Match 1─N MatchEvent              (log eventi leggero del tabellone)
Match 1─N MatchPlayerStats        (box score completa per giocatore, da OCR)
Match 1─2 MatchTeamStats          (totali + comparative, home/away, da OCR)
Match 1─N OcrImportLog            (tracciabilità import OCR)
Match 1─N SnapshotState           (versione dei JSON pubblicati: live/stats)
Device/ApiUser                    (autorizzazione API, indipendente)
```

---

## 2. Entità — anagrafica (persistente)

### Tournament
Il contenitore di più edizioni nel tempo (es. "Piazzetta Madness").
- Attributi: `name`.
- Relazioni: `1—N` Edition.

### Edition
Una specifica edizione/stagione di un torneo (es. "2026").
- Attributi: `name`, `year`, `status` (draft/active/completed).
- Relazioni: appartiene a `1` Tournament; `1—N` Group, Team, Match.
- **Nota**: è il livello a cui sono legati squadre, gironi e roster (un giocatore
  può cambiare squadra tra edizioni).

### TournamentGroup (girone)
- Attributi: `name`, `code`, `sort_order`.
- Relazioni: appartiene a `1` Edition; `N—N` Team (via GroupTeam).

### Team
Una squadra **dentro una edizione** (la stessa "squadra reale" in due edizioni è
modellata come due Team distinti, semplificando roster e statistiche per edizione).
- Attributi: `name`, `short_name`, colori.
- Relazioni: appartiene a `1` Edition; `1—N` Roster; partecipa a `N—N` Group.

### Player
Anagrafica globale, **solo nome e cognome** (privacy).
- Attributi: `first_name`, `last_name`.
- Relazioni: `1—N` Roster (può essere tesserato in più edizioni/squadre nel tempo).

### Roster (team_rosters)
Tesseramento di un giocatore in una squadra (quindi in una edizione).
- Attributi: `jersey_number`, `role`, `is_captain`.
- Relazioni: `N—1` Team, `N—1` Player.
- **Regola**: il numero di maglia vale per l'edizione. Un giocatore non sta in due
  squadre della stessa edizione (vincolo logico, non strutturale in MVP).

### GroupTeam
Associazione girone↔squadra.
- Attributi: `seed_label`.
- Relazioni: `N—1` Group, `N—1` Team.

---

## 3. Entità — partita

### Match
La partita. Lega due squadre della stessa edizione.
- Attributi: `phase` (girone/semifinale/finale 3-4/finale), `round`,
  `scheduled_at`, `status` (scheduled/live/finished), regole (durata periodi,
  bonus falli…), **risultato consolidato** a fine partita:
  `winner_team_id`, `win_reason`, `final_home_score`, `final_away_score`.
- Relazioni: appartiene a `1` Edition, opzionale `1` Group; ha `home_team` e
  `away_team` (entrambi Team); `1—N` MatchPeriod, MatchEvent, MatchPlayerStats;
  `1—1` LiveMatchState; `1—2` MatchTeamStats.
- **Punteggio ufficiale**: deriva dal tabellone (live), validato vs somma punti
  giocatori, e congelato in `final_*_score` a fine partita.

### MatchPeriod (parziali)
- Attributi: `period_number`, `period_type` (regular/overtime), `home_score`,
  `away_score`.
- Relazioni: `N—1` Match.

---

## 4. Entità — LIVE (dal tabellone, effimero)

### LiveMatchState (stato live corrente)
Una riga per la partita live (una sola partita live alla volta). È la fonte da cui
PHP genera `live-state.json`.
- Attributi: `has_live`, `status`, **`version`** (incrementa a ogni evento),
  `updated_at`, clock (`period`, `seconds`, `tenths`, `running`, `snapshot_at`),
  `home_score`, `away_score`, timeout usati per lato.
- Relazioni: `1—1` Match.
- **Ciclo di vita**: a fine partita `has_live=false`; il risultato si consolida in
  `Match.final_*_score` e `MatchPeriod`.

### LivePlayerState (punti/falli live)
I soli dati giocatore che il tabellone gestisce in tempo reale.
- Attributi: `team_side` (home/away), `points`, `fouls`.
- Relazioni: `N—1` Match, `N—1` Player.

---

## 5. Entità — eventi tabellone (log leggero, persistente)

### MatchEvent
Registro tecnico degli eventi inviati dall'app tabellone. **Non** è il play-by-play
FIBA completo: serve per idempotenza, recupero, audit, andamento.
- Attributi: `device_id`, **`local_event_id`**, `client_sequence`,
  `event_type` (enum: score, foul, timeout, clock_start, clock_stop,
  period_change, correction, heartbeat, finish), `payload` (JSON), `client_created_at`,
  `server_received_at`, `is_correction`, `reverts_event_id`.
- Relazioni: `N—1` Match; auto-relazione opzionale `reverts_event_id → MatchEvent`.
- **Vincolo**: univocità `(device_id, local_event_id)` per l'idempotenza.

---

## 6. Entità — statistiche complete (dall'OCR, ultimo snapshot)

### MatchPlayerStats (box score giocatore)
Tabellino completo per giocatore/partita. **Una sola versione** (ultimo snapshot).
- Attributi: `team_side`, `minutes`, tiri (`fg/2p/3p/ft` made+att), rimbalzi
  (`off/def/tot`), `assists`, `turnovers`, `steals`, `blocks`, falli
  (`committed/drawn`), `plus_minus`, `evaluation`, `points`, `source` (ocr),
  **`stats_version`**, `updated_at`.
- Relazioni: `N—1` Match, `N—1` Player.

### MatchTeamStats (totali + comparative)
Due righe per partita (home/away).
- Attributi: totali di squadra (stessi campi aggregati) + **comparative** FIBA
  (`points_in_paint`, `fast_break_points`, `second_chance_points`,
  `points_off_turnovers`, `bench_points`, `biggest_lead`, `biggest_run`,
  `lead_changes`, `times_tied`, `time_in_lead`, `points_per_possession`),
  `source`, `stats_version`, `updated_at`.
- Relazioni: `N—1` Match.

---

## 7. Entità — provenienza, pubblicazione, sicurezza

### OcrImportLog
Traccia ogni import OCR (sostituisce `online_sync_log` generico).
- Attributi: `source_pdf`, `document_hash`, `stats_version`, `imported_at`,
  `status` (bozza/pubblicato), `warnings_json`.
- Relazioni: `N—1` Match.

### SnapshotState
Versione corrente di ciascun JSON pubblicato (per cache/polling CDN).
- Attributi: `kind` (live/stats), `version`, `updated_at`, `json_path`.
- Relazioni: `N—1` Match.

### Device / ApiUser
Autorizzazione delle API (token), indipendente dai dati di gioco.
- Attributi: `name`, `role` (scoreboard_device/ocr/admin), `token_hash`.

---

## 8. Aggregati (viste, non tabelle)

Derivati da `MatchPlayerStats`/`MatchTeamStats`/`Match`:
- `v_player_season_stats` — medie/totali giocatore per edizione.
- `v_team_season_stats` — totali/medie squadra per edizione.
- `v_standings` — classifica girone (da risultati match).
- `v_stat_leaders` — leader per statistica.

Volume piccolo → query live sulle viste; materializzazione predisposta per il futuro.

---

## 9. Decisioni confermate (chiudono la Fase 1)

1. **Team per edizione**: la stessa squadra in due edizioni = **due `Team`
   distinti** (semplifica roster e statistiche per edizione).
2. **LivePlayerState**: si tiene lo **stato live pronto** per giocatore
   (punti/falli correnti), aggiornato a ogni evento → genera `live-state.json`
   senza ricalcolare dagli eventi.
3. **Collegamento squadra**: le statistiche usano **`team_id` esplicito** (legame
   robusto alla squadra). Il lato home/away si ricava dalla partita quando serve
   per i JSON. → Negli schemi `MatchPlayerStats`, `MatchTeamStats`,
   `LivePlayerState` il riferimento squadra è `team_id` (non `team_side`).
4. **Comparative**: si conservano **tutte** le comparative del tabellino FIBA
   (punti in area, contropiede, da palle perse, secondi tiri, panchina, massimo
   vantaggio, massimo parziale, cambi di guida, parità, tempo in vantaggio, punti
   per possesso).

La Fase 1 è chiusa. Si procede alla Fase 2 (catalogo eventi + consolidamento).
