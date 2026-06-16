# Decisioni Database - Piazzetta Madness

Versione: 0.1

Questo documento raccoglie le decisioni operative per trasformare il modello E/R e lo schema SQLite in codice applicativo.

## Stack Database

- Database locale: SQLite.
- ORM previsto: Entity Framework Core.
- File database locale previsto: `piazzetta-madness.db`.
- Schema iniziale: `docs/sqlite-schema-v1.sql`.
- Tutte le tabelle dello schema v1 sono incluse fin dall'inizio.

## Convenzioni

### Nomi Tabelle E Colonne

- Database: `snake_case`.
- C# entities: `PascalCase`.
- C# properties: `PascalCase`.
- Enum C#: `PascalCase`.
- Valori enum salvati su DB: `PascalCase` come testo.

Esempio:

```text
tabella DB: match_events
classe C#: MatchEvent
colonna DB: game_clock_ms_remaining
proprieta C#: GameClockMsRemaining
```

### Chiavi Primarie

- Tutte le chiavi primarie locali sono `INTEGER PRIMARY KEY AUTOINCREMENT`.
- Nel codice C# saranno `int`.
- Non usiamo `Guid` nella prima versione per mantenere il database piu leggibile e semplice da ispezionare.

### Date E Orari

- SQLite salva date e orari come `TEXT`.
- Formato applicativo: ISO-8601.
- Per eventi creati localmente si salva il timestamp locale.
- Per futura sincronizzazione online si potra aggiungere conversione UTC nel payload JSON.

Nota: per la logica live non usare `DateTime.Now` come fonte di verita del cronometro. Il tempo gara usa un clock monotono in C#.

### Booleani

- Salvati come `INTEGER`.
- Valori ammessi: `0` e `1`.
- In C# saranno `bool`.

### Cronometri

- Tutti i cronometri sono salvati in millisecondi interi.
- Campi principali:
  - `period_duration_ms`
  - `break_duration_ms`
  - `overtime_duration_ms`
  - `shot_clock_ms`
  - `timeout_duration_ms`
  - `game_clock_ms_remaining`
  - `shot_clock_ms_remaining`

Il rendering puo mostrare secondi o decimi, ma il dato persistito resta in millisecondi.

## Regole Default Edizione 2026

Seed consigliato per la prima edizione:

```text
Tournament:
- Name: Piazzetta Madness

Edition:
- Name: Piazzetta Madness 2026
- Year: 2026
- Status: Draft

Groups:
- Girone A, code A
- Girone B, code B

Court:
- Piazzetta Verde
```

## Regole Default Partita

Per ogni partita:

```text
period_count = 2
period_duration_ms = 720000
break_duration_ms = 120000
overtime_duration_ms = 120000
shot_clock_ms = 24000
timeout_duration_ms = 30000
timeouts_per_team = 2
timeouts_per_period = 1
personal_foul_limit = 5
team_foul_bonus_threshold = 5
stop_clock_on_free_throws = true
```

Per fase gironi:

```text
phase = GroupStage
max_score = 45
max_score_enabled = true
```

Per semifinali e finali:

```text
phase = SemiFinal | ThirdPlaceFinal | Final
max_score = null
max_score_enabled = false
```

## Dati Calcolati E Dati Salvati

### Salvati

Questi dati sono salvati per performance, storico o ripresa dopo crash:

- anagrafiche torneo, edizione, squadre, giocatori;
- roster;
- gironi;
- partite;
- partecipanti partita;
- periodi;
- eventi partita;
- falli;
- timeout;
- sequenze tiri liberi;
- snapshot stato tabellone;
- risultati speciali;
- eventi 3 Point Contest;
- coda sync.

### Calcolati O Ricalcolabili

Questi dati possono essere ricalcolati dagli eventi o dai risultati:

- punteggio corrente;
- punti giocatore;
- falli personali;
- falli squadra;
- timeout usati;
- classifica girone.

Per praticita vengono comunque salvati anche in tabelle aggregate (`match_teams`, `match_players`, `scoreboard_states`, `standings`). La regola e:

```text
MatchEvent e la sorgente storica.
ScoreboardState e MatchTeam/MatchPlayer sono snapshot/aggregati correnti.
```

Se c'e incoerenza, prevale la sequenza eventi, salvo correzione manuale esplicita.

## Eventi Partita

`match_events` e la tabella centrale per:

- live online;
- undo/correzioni;
- ricostruzione della partita;
- statistiche;
- audit delle azioni operate dalla console.

Gli eventi non vanno cancellati durante l'uso normale.

Per correggere:

- creare un nuovo evento con `is_correction = 1`;
- valorizzare `reverts_event_id` se si annulla un evento specifico;
- aggiornare snapshot e aggregati coerentemente.

## Classifica Gironi

La classifica puo essere ricalcolata dalle partite finite, ma la tabella `standings` viene mantenuta per:

- consultazione veloce;
- visualizzazione tabellone o sito;
- blocco della classifica al termine del girone;
- annotazione tie-break.

Tie-break:

1. scontro diretto;
2. differenza canestri;
3. punti fatti;
4. sorteggio.

Quando il girone e chiuso, `position` e `tie_break_note` diventano dati ufficiali.

## Risultati A Tavolino

Gestione consigliata:

- creare o aggiornare `forfeit_results`;
- aggiornare `matches.winner_team_id`;
- impostare `matches.win_reason = 'Forfeit'` o motivo piu specifico;
- aggiornare `match_teams.score`;
- creare un evento `ForfeitAssigned` in `match_events`.

Punteggio default:

```text
20 - 0
```

## 3 Point Contest

Il 3 Point Contest non e una partita.

Va gestito tramite:

- `competition_events`
- `three_point_contest_entries`
- `three_point_contest_rounds`

Regola iniziale:

- un solo partecipante per squadra;
- 5 postazioni;
- punteggio per postazione salvato nei campi `station*_score`;
- eventuali spareggi come round `TieBreak`.

## Sincronizzazione Online

La tabella `sync_queue` serve per inviare dati senza bloccare il live.

Regole:

- la partita deve funzionare anche senza internet;
- ogni evento importante genera un payload in coda;
- il fallimento della sync non deve influenzare cronometro o tabellone;
- lo stato `Pending` viene ritentato;
- `Failed` conserva `last_error`;
- `Sent` valorizza `sent_at`.

Primi payload da sincronizzare:

- `MatchEvent`;
- `ScoreboardState` come snapshot;
- `Match` a fine partita;
- `Standing` dopo aggiornamento classifica.

## Seed Demo

Quando verra creato lo scheletro applicativo, il seed demo dovra creare:

- 1 torneo;
- 1 edizione;
- 1 campo;
- 2 gironi;
- 8 squadre demo;
- almeno 8 giocatori per squadra;
- assegnazione squadre ai gironi;
- calendario completo fase gironi;
- semifinali/finali placeholder;
- 1 partita pronta in stato `Ready`;
- 1 evento `ThreePointContest` placeholder.

## Validazioni Applicative

Alcune regole sono piu adatte al codice che al database:

- una partita deve avere esattamente due squadre;
- una squadra deve iniziare con almeno 5 giocatori disponibili;
- massimo 1 timeout per squadra per tempo;
- massimo 2 timeout totali per squadra;
- bonus dal quinto fallo squadra del periodo;
- limite 45 punti solo fase gironi;
- supplementari diversi tra gironi e fasi finali;
- 3 Point Contest: massimo un partecipante per squadra.

## Decisioni Aperte

- Gestire o no reset shot clock a 14 secondi.
- Aggiungere statistiche avanzate oltre punti e falli.
- Definire API online o accesso diretto a database remoto.
- Definire formato definitivo dei payload JSON in `sync_queue`.

