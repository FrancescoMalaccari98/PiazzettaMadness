# MVP Tecnico - Piazzetta Madness

Versione: 0.1

Questo documento riduce il modello E/R completo a una prima versione implementabile.

## Scelte V1

- Applicazione desktop locale.
- Database locale SQLite.
- Chiavi primarie `INTEGER PRIMARY KEY AUTOINCREMENT`.
- Date e orari salvati come `TEXT` ISO-8601.
- Booleani salvati come `INTEGER` 0/1.
- Enum salvati come `TEXT` con vincoli `CHECK`.
- Cronometri salvati in millisecondi interi.
- Lo stato live vero resta in memoria; SQLite salva eventi e snapshot.

## Tabelle Database V1

La prima versione del database include tutte le tabelle dello schema, anche quelle che potrebbero essere usate solo dopo il primo prototipo UI. Questo evita migrazioni strutturali immediate e permette di progettare da subito import, export e sincronizzazione con un modello completo.

- `tournaments`
- `editions`
- `courts`
- `teams`
- `players`
- `team_rosters`
- `tournament_groups`
- `group_teams`
- `standings`
- `matches`
- `match_teams`
- `match_players`
- `match_periods`
- `scoreboard_states`
- `match_events`
- `match_timeouts`
- `match_fouls`
- `free_throw_sequences`
- `forfeit_results`
- `competition_events`
- `three_point_contest_entries`
- `three_point_contest_rounds`
- `sync_queue`

## Priorita UI

Anche se il database include tutto, l'interfaccia puo essere sviluppata in ordine progressivo:

1. Anagrafiche torneo, edizione, squadre, giocatori e roster.
2. Gironi, calendario e partite.
3. Console partita e tabellone live.
4. Classifiche e risultati speciali.
5. 3 Point Contest.
6. Sincronizzazione online.

## Flusso Minimo Partita

1. L'operatore crea o seleziona una partita.
2. Il software carica squadre e roster.
3. Il software crea `match_teams`, `match_players`, `match_periods` e `scoreboard_states`.
4. Durante la partita ogni azione importante crea un record in `match_events`.
5. Lo stato in memoria aggiorna subito la console e le finestre tabellone.
6. Periodicamente viene salvato uno snapshot in `scoreboard_states`.
7. A fine partita vengono aggiornati `matches`, `match_teams`, statistiche giocatori e sync queue.

## Eventi Minimi Per Il Tabellone

La prima versione deve gestire questi `event_type`:

- `GameStarted`
- `GamePaused`
- `GameResumed`
- `GameEnded`
- `PeriodStarted`
- `PeriodEnded`
- `Score1`
- `Score2`
- `Score3`
- `ScoreCorrection`
- `FoulPersonal`
- `PlayerFouledOut`
- `TeamBonusReached`
- `TimeoutStarted`
- `TimeoutEnded`
- `FreeThrowStarted`
- `FreeThrowMade`
- `FreeThrowMissed`
- `ShotClockReset24`
- `ShotClockViolation`
- `PossessionChanged`
- `ForfeitAssigned`

## Regole Da Implementare Nel Motore Partita

- 2 tempi da 12 minuti.
- Tempo continuato.
- Stop cronometro sui tiri liberi.
- 24 secondi.
- Timeout da 30 secondi.
- 2 timeout per squadra, massimo 1 per tempo.
- 5 falli personali = fuori per falli.
- Dal quinto fallo squadra del periodo: bonus 2 liberi.
- In girone, fine automatica al raggiungimento di 45 punti.
- In semifinali/finali, niente limite 45 punti.
- Supplementare da 2 minuti.
- Gironi: dopo un supplementare ancora pari, sudden death ai tiri liberi.
- Fasi finali: supplementari da 2 minuti finche c'e una vincente.

## Decisioni Ancora Aperte

- Se registrare anche assist, rimbalzi e palle recuperate oppure solo punti/falli.
- Se il cronometro 24 deve avere anche reset a 14 secondi.
- Se serve un input fisico dedicato oltre a mouse/tastiera.
- Se il sito online leggerà da API oppure da database remoto.
- Se la classifica gironi deve essere salvata o sempre calcolata dagli eventi/risultati.
