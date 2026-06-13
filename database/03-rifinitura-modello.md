# Fase 2 (rifinitura) — Decisioni di modello prima del DDL

Rifiniture sul modello concettuale prima di scrivere il DDL MySQL.

## Decisioni confermate

### 1. Naming statistiche allineato all'OCR
Le colonne delle box score rispecchiano i canonical key che l'OCR già produce,
così l'import è quasi 1:1. Nomi colonna previsti (per `match_player_stats` e i
totali in `match_team_stats`):

```
minutes_seconds        (INT, secondi)        ← da "24:00"
fg_made, fg_att                              ← fieldGoals.made / attempted
two_made, two_att                            ← twoPoints.made / attempted
three_made, three_att                        ← threePoints.made / attempted
ft_made, ft_att                              ← freeThrows.made / attempted
reb_off, reb_def                             ← rebounds.offensive / defensive
assists                                      ← assists
turnovers                                    ← turnovers
steals                                       ← steals
blocks                                       ← blocks
fouls_committed, fouls_drawn                 ← fouls.committed / drawn
plus_minus                                   ← plusMinus
evaluation                                   ← evaluation
points                                       ← points
```

### 2. Derivate calcolate, non memorizzate
Si salvano solo i valori grezzi. Si calcolano nelle **viste**:
- percentuali: `fg_pct = fg_made / NULLIF(fg_att,0) * 100` (e 2P/3P/FT analoghi);
- `reb_tot = reb_off + reb_def`.
Niente colonne percentuale/totale → nessuna ridondanza, nessuna incoerenza.

### 3. Minuti in secondi
`minutes_seconds` INT. Il sito formatta in `MM:SS`. L'import converte da `"24:00"`.

### 4. Giocatori N.E. (non entrati)
La riga del giocatore esiste con `did_not_play = true` e tutte le statistiche
**NULL** (distingue "non ha giocato" da "ha fatto 0 giocando"). Coerente col
tabellino FIBA.

---

## Micro-temi residui (default proposti — confermare o cambiare)

| Tema | Default proposto |
|---|---|
| **Chiavi primarie** | `id` auto-increment `INT UNSIGNED` su tutte le tabelle. |
| **Timestamp** | Tutti in **UTC** (`DATETIME`), conversione fuso lato sito. |
| **Enum** | `phase`, `match.status`, `event_type`, `win_reason`, `edition.status`, `ocr_import_log.status` come `ENUM` MySQL (semplice, leggibile). |
| **Starter / capitano** | `is_captain` su `team_rosters`; `is_starter` su `match_player_stats` (il `*` del tabellino). |
| **Charset** | `utf8mb4` (nomi con accenti: Monteriù, Purifico…). |
| **Engine** | `InnoDB` (FK + transazioni, richieste dal flusso eventi). |
| **Stato pubblicazione stat** | `status` (bozza/pubblicato) su `ocr_import_log`; le `match_*_stats` mostrano l'ultimo import pubblicato. |
| **Soft delete** | Non in MVP (volume piccolo); eventuale `deleted_at` in futuro. |

---

## Cosa resta fuori (MVP), per chiarezza
- Tabella discrepanze tabellone/OCR (decisa fuori MVP dal documento architettura).
- Quintetti/sostituzioni dettagliati e play-by-play completo.
- Materializzazione degli aggregati (per ora viste).

---

## Stato
Modello concettuale + rifiniture **chiusi** una volta confermati i default residui.
Passo successivo: **Fase 3 — DDL MySQL** (tabelle, tipi, FK, indici, UNIQUE
idempotenza), pronto per Aruba.
