# Sincronizzazione online

## Modello operativo

- MySQL su Aruba e la sorgente principale per anagrafiche, tornei e configurazione.
- Le CRUD amministrative richiedono la configurazione online e inviano modifiche puntuali alle API REST.
- I dati usati dalle schermate CRUD vengono tenuti in una cache SQLite temporanea per processo.
- La cache temporanea viene ricreata a ogni avvio e non rappresenta una replica persistente offline.
- Un secondo database SQLite persistente contiene esclusivamente il pacchetto della partita live.

Percorsi:

- cache UI temporanea: `%TEMP%/piazzetta-madness-cache-<processo>.db`;
- sessione live persistente: `%LOCALAPPDATA%/PiazzettaMadness/piazzetta-madness-live.db`.

## Dati live

Durante una partita l'app salva prima localmente e sincronizza in modo incrementale:

- `match_teams`;
- `match_players`;
- `match_events` non ancora sincronizzati;
- `scoreboard_states` tramite inserimento o aggiornamento per `match_id`.

Gli eventi locali ricevono `synced_at` solo dopo una risposta positiva dell'API. Se la rete non e disponibile restano in attesa e vengono ritentati al salvataggio successivo.

Il database live conserva un pacchetto JSON con:

- partita, edizione e torneo necessari al ripristino;
- squadre e giocatori convocati;
- record Home/Away;
- stato tabellone;
- eventi della partita, inclusi quelli non ancora sincronizzati.

Al riavvio il pacchetto viene ripristinato e sincronizzato prima del normale refresh online, evitando che dati remoti piu vecchi sovrascrivano eventi locali pendenti. Un timer riprova la sincronizzazione ogni 10 secondi.

## Snapshot

La pubblicazione completa del database locale e stata rimossa dall'app. `snapshot.php` accetta ancora letture diagnostiche, ma rifiuta `POST` e `PUT` con stato HTTP 410.

Questo impedisce che una cache locale cancelli o sovrascriva dati modificati da altri programmi.

## Tabelle statistiche esterne

Le tabelle seguenti appartengono al programma statistico dei collaboratori e non vengono lette, scritte o incluse nella sincronizzazione dell'app:

- `match_player_stats`;
- `match_team_stats`.

Le cancellazioni di partite, edizioni, tornei, squadre e giocatori vengono rifiutate con HTTP 409 quando eliminerebbero statistiche collegate.

## Operazioni atomiche

Le operazioni che coinvolgono piu tabelle usano endpoint transazionali:

- `action=save_match`: salva `matches` e i due record Home/Away di `match_teams`;
- `action=delete_match`: elimina una partita solo se non esistono statistiche collegate;
- `action=save_forfeit`: salva il risultato a tavolino, la partita e i punteggi delle squadre;
- `action=delete_forfeit`: elimina il risultato e ripristina coerentemente partita e punteggi;
- `action=replace_standings`: sostituisce la classifica in una singola transazione.
- `action=initialize_match_players`: al primo avvio live congela in `match_players` tutti i giocatori attivi dei roster Home e Away.

Un errore in una delle scritture annulla l'intera operazione sul server.

La selezione manuale dei convocati non esiste piu. Se `match_players` contiene gia righe per la partita, l'inizializzazione le restituisce senza modificarle; in caso contrario richiede almeno un giocatore attivo per ciascuna squadra.

## Tabelle rimosse

La migrazione `server/migrations/2026-06-08-remove-unused-tables.sql` elimina strutture non utilizzate dall'app:

- `free_throw_sequences`;
- `match_fouls`;
- `match_periods`;
- `match_timeouts`;
- `online_sync_log`.

Le tabelle statistiche esterne `match_player_stats` e `match_team_stats` restano invariate.

## Riconciliazione cache

Quando una lettura online riesce, l'elenco remoto e considerato autorevole. La cache SQLite:

- aggiunge le righe nuove;
- aggiorna quelle esistenti;
- elimina quelle non piu presenti online.

Se la lettura online fallisce, la cache non viene modificata e continua a essere usata come fallback.
