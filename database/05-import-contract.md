# Fase 5 — Contratto import OCR → DB

Definisce come il **JSON finale riconciliato** dell'OCR (ProcessingResult,
`runtime/OutputJson/<file>.json`) viene importato nelle tabelle statistiche del DB
Live Basket. L'import lo esegue il **backend PHP** (endpoint `/api/stats/snapshot`):
l'app OCR invia il JSON, PHP risolve le entità e fa upsert.

Riferimento schema: `database/live_basket_schema_mysql.sql`.

---

## 1. Struttura del JSON in ingresso (campi usati)

```
schemaVersion
processedFile { fileName, documentHash, status }
game {
  competition, venue, date, time, number,
  homeTeamId="team:Home", awayTeamId="team:Away",
  finalScore="29-52", periods[ {period:"Q1", home:15, away:35}, ... ]
}
teams[ { teamId, side:"Home"|"Away", name, abbreviation, coach } ]
players[ { entityId, teamId, side, number, fullName, firstName, lastName,
           starter, captain, didNotPlay } ]
stats[ { scope:"Team"|"Player"|"Result", entityId, side, statKey, value, score, status } ]
```

Note:
- Gli `entityId` sono interni all'OCR (`team:Home`, `player:Home:jersey:0`); **non**
  sono gli id del DB: vanno **risolti** (sezione 3).
- `finalScore` qui è la lettura OCR. Per le partite con tabellone, il punteggio
  ufficiale resta quello del tabellone (`matches.final_*_score`); l'OCR popola solo
  le statistiche. Per partite **senza** tabellone, l'OCR è l'unica fonte e può
  valorizzare anche `final_*_score`.

---

## 2. Precondizioni

- Il `match` esiste già nel DB (creato dall'admin con `edition_id`,
  `home_team_id`, `away_team_id`). L'import riceve il `match_id` target
  (parametro della chiamata API, non dedotto dal nome file).
- I `teams` del match e i loro `team_rosters` (giocatore + numero maglia) sono
  già popolati per l'edizione.

---

## 3. Risoluzione delle entità

### Squadra (`side` → `team_id`)
Dal `match`: `home_team_id` per `side="Home"`, `away_team_id` per `side="Away"`.
(Non si usa il nome OCR, spesso rumoroso es. "I Los aiche ride".)

### Giocatore (`side` + numero maglia → `player_id`)
1. Determina `team_id` dal lato (sopra).
2. Cerca in `team_rosters`: `WHERE team_id = ? AND jersey_number = ?`
   (il `number` del giocatore OCR, normalizzato, togliendo l'eventuale `*` starter).
3. Trovato → `player_id`. **Non trovato** → record in warning
   (`ocr_import_log.warnings_json`), riga statistica **saltata o in `draft`**; non
   si crea un giocatore "fantasma".

> Coerente con la decisione: identità giocatore via **roster edizione + maglia**,
> non per nome. Riusa il roster dinamico del DB (`roster_context.py` / `OcrMatchContext`);
> `known_names.py` è stato rimosso in Fase 9.

---

## 4. Mappatura statKey canonico → colonna DB

Vale sia per `match_player_stats` (scope `Player`) sia per `match_team_stats`
(scope `Team`). Naming già allineato 1:1.

| statKey OCR | Colonna DB |
|---|---|
| `minutes` | `minutes_seconds` (converti `"MM:SS"`→secondi; vedi §6) |
| `fieldGoals.made` / `.attempted` | `fg_made` / `fg_att` |
| `twoPoints.made` / `.attempted` | `two_made` / `two_att` |
| `threePoints.made` / `.attempted` | `three_made` / `three_att` |
| `freeThrows.made` / `.attempted` | `ft_made` / `ft_att` |
| `rebounds.offensive` / `.defensive` | `reb_off` / `reb_def` |
| `assists`, `turnovers`, `steals`, `blocks` | omonime |
| `fouls.committed` / `.drawn` | `fouls_committed` / `fouls_drawn` |
| `plusMinus` | `plus_minus` |
| `evaluation` | `evaluation` |
| `points` | `points` |

Comparative (solo `match_team_stats`, scope `Team`): `points_in_paint`,
`fast_break_points`, `second_chance_points`, `points_off_turnovers`,
`bench_points`, `biggest_lead`, `biggest_run`, `lead_changes`, `times_tied`,
`time_in_lead_seconds`, `points_per_possession` (mappare se l'OCR le espone;
altrimenti restano NULL).

Ignorati (derivati o non in DB): `fieldGoals.percentage` e ogni `*.percentage`,
`rebounds.total` (calcolati nelle viste).

---

## 5. Upsert (idempotente sulle UNIQUE già nello schema)

`stats_version` = numero progressivo del nuovo import (ultimo `ocr_import_log
.stats_version` per il match + 1).

### Giocatore — `match_player_stats` (UNIQUE `match_id, player_id`)
```sql
INSERT INTO match_player_stats
  (match_id, team_id, player_id, jersey_number, is_starter, did_not_play,
   minutes_seconds, fg_made, fg_att, two_made, two_att, three_made, three_att,
   ft_made, ft_att, reb_off, reb_def, assists, turnovers, steals, blocks,
   fouls_committed, fouls_drawn, plus_minus, evaluation, points,
   source, stats_version)
VALUES (?, ?, ?, ?, ?, ?,  ?, ?, ?, ?, ?, ?, ?,  ?, ?, ?, ?, ?, ?, ?, ?,
        ?, ?, ?, ?, ?,  'ocr', ?)
ON DUPLICATE KEY UPDATE
  team_id=VALUES(team_id), jersey_number=VALUES(jersey_number),
  is_starter=VALUES(is_starter), did_not_play=VALUES(did_not_play),
  minutes_seconds=VALUES(minutes_seconds), fg_made=VALUES(fg_made), /* ...tutte... */
  points=VALUES(points), stats_version=VALUES(stats_version);
```
`is_starter` ← `player.starter`; `did_not_play` ← `player.didNotPlay`;
`jersey_number` ← `player.number` (snapshot denormalizzato).

### Squadra — `match_team_stats` (UNIQUE `match_id, team_id`)
Analogo, due righe (home/away).

Tutto in **una transazione**; al termine `INSERT` in `ocr_import_log`.

---

## 6. Conversioni e casi limite

- **Minuti**: `"24:00"` → `24*60+0 = 1440`. Se vuoto/assente → NULL.
- **N.E.** (`didNotPlay=true`): riga con `did_not_play=1` e tutte le statistiche
  **NULL** (non 0).
- **Numero maglia**: rimuovi `*` (starter) prima del lookup roster; conserva il
  valore originale in `jersey_number`.
- **Valore stat assente o non numerico**: colonna NULL.
- **plus_minus / evaluation** possono essere negativi (colonne SMALLINT signed).

---

## 7. Validazione punteggio (riuso controllo cross-entity)

Prima del commit, per ogni lato: `Σ points giocatori == punteggio squadra`.
- Coerente → ok.
- Incoerente → voce in `warnings_json` (ruleId `import.pointsSumMismatch`); l'import
  resta `draft` per revisione. È lo stesso controllo già in `NormalizedOcrReconciler`
  (bug "29-2 → 29-52"): il totale OCR sbagliato non sovrascrive la somma giocatori.

Se il match ha già un risultato da tabellone (`final_*_score` non NULL), l'import
**non** lo tocca; confronta soltanto e segnala eventuali divergenze.

---

## 8. Ciclo di vita (bozza → pubblicato)

1. Import scrive le stat + `ocr_import_log.status='draft'`.
2. Un admin verifica (warning, punteggi) e passa a `published`.
3. Le viste `v_match_*_stats_public` espongono i dati; la generazione di
   `stats-state.json` (Fase 6) usa l'ultimo import `published`.

Re-import dello stesso PDF: nuovo `stats_version`, upsert sovrascrive le righe
(solo ultimo snapshot per match), nuova riga di log.

---

## 8.bis Risoluzione squadra/giocatore
Il DB è l'unica fonte di identità: `teams`/`players`/`team_rosters` sono il roster canonico.
L'OCR riceve il roster della partita via `--roster-json` (`roster_context.py` / `OcrMatchContext`)
e il lookup per maglia combacia con il DB. L'anagrafica hardcoded (`known_names.py`) è stata
**rimossa in Fase 9**: non esiste più alcuna tabella di nomi nel codice del worker.

---

## 9. Verifica
Importare `runtime/OutputJson/partita 4 q2...json` su un match di prova:
- 8 giocatori Away risolti via maglia → `match_player_stats`;
- somma punti Away = 52 → warning se `final_away_score` (tabellone) ≠ 52;
- re-import → nessuna riga duplicata (upsert su UNIQUE), nuovo `stats_version`.
