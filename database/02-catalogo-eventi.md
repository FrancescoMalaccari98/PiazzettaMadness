# Fase 2 — Catalogo eventi del tabellone e consolidamento

Definisce i tipi di evento inviati dall'app tabellone (C#) al backend PHP, il loro
`payload`, l'effetto su stato live e DB, e le regole con cui a fine partita il
risultato live si **consolida** nelle tabelle storiche.

Tutti gli eventi condividono l'involucro comune (dal documento architettura):
`local_event_id`, `match_id`, `device_id`, `client_sequence`, `event_type`,
`client_created_at`, `payload`. L'idempotenza è garantita da
`UNIQUE(device_id, local_event_id)`: un evento già visto restituisce OK senza
riapplicare l'effetto.

`version` di `live_match_state` **incrementa a ogni evento** che cambia lo stato
(serve al polling/cache del frontend).

---

## 1. Tipi di evento (enum `event_type`)

| event_type | payload (campi chiave) | Effetto su stato/DB |
|---|---|---|
| `match_start` | — | `live_match_state.has_live=true, status=live`; azzera score/timeout; crea righe `live_player_state` per i giocatori a referto |
| `score` | `team`, `player_id`, `points` (1/2/3), `period`, `clock_*` | `live_player_state.points += points`; `live_match_state.<side>_score += points`; `version++` |
| `foul` | `team`, `player_id`, `foul_type`, `period` | `live_player_state.fouls += 1`; falli di squadra del periodo derivati; `version++` |
| `timeout` | `team` | `live_match_state.<side>_timeouts_used += 1`; `version++` |
| `clock_start` | `period`, `clock_seconds`, `clock_tenths` | `clock_running=true`, aggiorna snapshot clock; `version++` |
| `clock_stop` | `period`, `clock_seconds`, `clock_tenths` | `clock_running=false`, fissa snapshot clock; `version++` |
| `period_change` | `new_period`, `home_score`, `away_score` | aggiorna `clock_period`; **chiude il parziale** del periodo precedente in `match_periods`; `version++` |
| `correction` | `target` (cosa), `old_value`, `new_value`, riferimenti | applica la rettifica allo stato; `is_correction=true`, salva audit; `version++` |
| `heartbeat` | `clock_*` opzionale | aggiorna `updated_at` (sicurezza/recupero); **non** cambia i dati né `version` |
| `finish` | `home_score`, `away_score` | avvia il **consolidamento** (sezione 3); `has_live=false` |

Note:
- `team` nel payload è `home`/`away` lato tabellone; il backend lo traduce nel
  `team_id` corretto della partita.
- `score` aggiorna **sempre** sia il giocatore sia il totale squadra: per
  costruzione, la somma dei punti giocatori coincide col punteggio squadra (questo
  rende il live coerente per definizione; l'incoerenza 29-2 nasceva dall'OCR, non
  dal tabellone).

---

## 2. Effetto sui due livelli

- **Stato live corrente** (`live_match_state`, `live_player_state`): aggiornato in
  tempo reale, è la fonte da cui PHP rigenera `live-state.json`.
- **Log eventi** (`match_events`): ogni evento viene **anche** inserito come riga
  (storico leggero) per idempotenza, recupero e audit. Non è il play-by-play
  completo: traccia solo i tipi sopra.

Sequenza backend per ogni evento (dal documento): valida token/ruolo → valida
payload → controlla idempotenza → transazione: inserisci evento + aggiorna stato +
`version++` → commit → rigenera JSON (tmp + rename atomico) → risposta con
`server_version`.

---

## 3. Consolidamento a fine partita (`finish`)

Quando arriva `finish`, il risultato live diventa **storico**:

1. **Punteggio ufficiale** = punteggio del tabellone:
   `matches.final_home_score = live_match_state.home_score`,
   `matches.final_away_score = live_match_state.away_score`.
2. **Validazione cross-entity** (riuso del controllo già implementato nell'OCR):
   per ogni lato, `somma(live_player_state.points) == <side>_score`.
   - Coerente → ok.
   - Incoerente → registra un warning (in `ocr_import_log`/log tecnico) ma **il
     tabellone resta la fonte**; serve come allerta di sanità (sul tabellone
     dovrebbe sempre coincidere per costruzione).
3. **Winner**: `matches.winner_team_id` = lato con punteggio maggiore;
   `win_reason` = `Regular` (o overtime/forfeit secondo i casi).
4. **Parziali**: i `match_periods` sono già stati chiusi a ogni `period_change`;
   l'ultimo periodo si chiude qui con i punteggi finali.
5. **Stato live**: `live_match_state.has_live=false, status=finished`. Il frontend
   nasconde la tab live; le statistiche restano nella pagina dedicata.

Le **statistiche complete** (rimbalzi, assist, tiri dettagliati…) NON arrivano dal
`finish`: restano di competenza dell'OCR (`match_player_stats`/`match_team_stats`),
aggiornate quando si elabora il PDF.

---

## 4. Falli di squadra

I falli di squadra per periodo sono **derivati** dalla somma dei `foul` dei
giocatori di quel lato in quel periodo (per il bonus). Non si memorizza un
contatore separato in MVP; si calcola dagli eventi/dallo stato quando serve.

---

## 5. Relazione con le statistiche OCR (riepilogo conflitti)

| Dato | Fonte live | Fonte completa | In conflitto |
|---|---|---|---|
| Punteggio totale | tabellone | (OCR conferma) | vince tabellone |
| Punti giocatore | tabellone | OCR (tabellino) | vince tabellone nel live |
| Falli giocatore | tabellone | OCR | vince tabellone nel live |
| Tiri/rimbalzi/assist/… | — | OCR | solo OCR |
| Comparative squadra | — | OCR | solo OCR |

Nessuna tabella discrepanze in MVP. Il backend applica la regola "vince il
tabellone sul live, vince l'OCR sulle avanzate".

---

## 6. Punti aperti minori (non bloccanti per la Fase 3)

- `correction`: definire i `target` ammessi (punteggio, punti giocatore, fallo,
  timeout, periodo) — si dettaglia nel contratto API, non nello schema.
- Overtime: `period_change` con `new_period` oltre il `period_count` regolare →
  `period_type=overtime` nei `match_periods`.
