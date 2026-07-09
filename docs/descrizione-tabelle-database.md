# Tabelle Del Database

Descrizione semplice delle tabelle presenti nel database di Piazzetta Madness.

| Tabella | Descrizione |
|---|---|
| `tournaments` | Tornei principali, per esempio "Piazzetta Madness". |
| `editions` | Singole edizioni annuali di un torneo. |
| `courts` | Campi da gioco disponibili per una edizione. |
| `teams` | Squadre partecipanti, con nome, colori e logo. |
| `players` | Anagrafica giocatori. |
| `team_rosters` | Collegamento giocatore-squadra, numero maglia, ruolo e capitano. |
| `tournament_groups` | Gironi di una edizione. |
| `group_teams` | Squadre inserite in ciascun girone. |
| `standings` | Classifica di ogni girone. |
| `matches` | Partite programmate, stato e impostazioni di gioco. |
| `match_teams` | Squadre Home/Away di ogni partita, punteggio, falli e timeout. |
| `match_players` | Giocatori coinvolti in una partita e dati live base: punti e falli. |
| `match_events` | Eventi live della partita, inclusi canestri, correzioni e reset. |
| `scoreboard_states` | Snapshot corrente del tabellone: cronometri, periodo, punteggio e falli. |
| `forfeit_results` | Risultati assegnati a tavolino. |
| `competition_events` | Eventi non partita: 3 Point Contest, premiazioni, intervalli. |
| `three_point_contest_entries` | Partecipanti alla gara da tre punti. |
| `three_point_contest_rounds` | Prove/fasi della gara da tre punti e punteggi per postazione. |
| `three_point_contest_shots` | I 25 tiri di ogni prova: postazione, numero palla, valore ed esito. |
| `sponsors` | Sponsor mostrati nell'app e nei tabelloni. |
| `staff` | Persone e ruoli mostrati sul sito. |
| `match_player_stats` | Statistiche complete dei giocatori. Gestita dal programma statistiche. |
| `match_team_stats` | Statistiche complete delle squadre. Gestita dal programma statistiche. |

## Schema

La struttura reale del database online e:

```text
server/migrations/db_struttura.sql
```

Il seed dati di test/ripristino e:

```text
server/migrations/piazzetta_test_edition_seed.sql
```

## Nota Sul Database Locale

L'app desktop usa SQLite locale per cache e sessione live, ma il database condiviso principale e MySQL online.

Percorso sessione live:

```text
%LOCALAPPDATA%/PiazzettaMadness/piazzetta-madness-live.db
```
