# Confronto schema database condiviso

Data analisi: 7 giugno 2026

## Obiettivo

Confrontare il database attualmente usato da PiazzettaMadness (`Sql1938817_1`) con il nuovo database condiviso (`Sql1938817_2`) predisposto anche per OCR, statistiche e servizi live.

L'obiettivo futuro e usare il nuovo database senza eliminare le funzionalita gia presenti e senza stravolgere il codice dell'app desktop. In questa fase non sono previste modifiche SQL, PHP o C#.

## Valutazione generale

Il nuovo database non e una semplice revisione del precedente. Condivide alcune anagrafiche, ma adotta modelli differenti soprattutto per:

- composizione delle partite;
- eventi live;
- stato del tabellone;
- statistiche OCR;
- autenticazione dei dispositivi;
- nomi e valori degli enum;
- unita di misura dei cronometri.

La strategia consigliata e mantenere il nuovo database come base, preservare tutte le strutture dei colleghi e aggiungere un livello di compatibilita per l'app.

## Tabelle condivise

Le seguenti tabelle esistono in entrambi gli schemi, ma non sono necessariamente compatibili colonna per colonna:

1. `tournaments`
2. `editions`
3. `tournament_groups`
4. `group_teams`
5. `teams`
6. `players`
7. `team_rosters`
8. `matches`
9. `match_events`
10. `match_periods`

## Tabelle del vecchio schema assenti nel nuovo

1. `competition_events`
2. `courts`
3. `forfeit_results`
4. `free_throw_sequences`
5. `match_fouls`
6. `match_players`
7. `match_teams`
8. `match_timeouts`
9. `online_sync_log`
10. `scoreboard_states`
11. `sponsors`
12. `standings`
13. `three_point_contest_entries`
14. `three_point_contest_rounds`

### Tabelle usate direttamente dall'app desktop

Tra le tabelle mancanti, l'app usa attualmente:

- `competition_events`;
- `courts`;
- `forfeit_results`;
- `match_players`;
- `match_teams`;
- `scoreboard_states`;
- `sponsors`;
- `standings`;
- `three_point_contest_entries`;
- `three_point_contest_rounds`.

Le tabelle `free_throw_sequences`, `match_fouls`, `match_timeouts` e `online_sync_log` appartengono allo schema precedente completo, ma non risultano ancora modellate direttamente nell'app desktop.

## Tabelle presenti solo nel nuovo schema

Queste strutture devono essere preservate:

1. `api_users`
2. `live_match_state`
3. `live_player_state`
4. `match_player_stats`
5. `match_team_stats`
6. `ocr_import_log`
7. `snapshot_state`

Sono destinate ad autenticazione, idempotenza, OCR, statistiche e pubblicazione dello stato live.

## Differenze per tabella

### `tournaments`

Nel nuovo schema manca `description`.

Proposta: aggiungere `description TEXT NULL`. E una modifica non distruttiva.

### `editions`

Differenze principali:

- `year` passa da `INT` a `SMALLINT UNSIGNED`;
- gli enum del vecchio schema sono PascalCase;
- gli enum del nuovo schema sono lowercase;
- nel nuovo schema esiste un vincolo univoco su `(tournament_id, year)`.

Il tipo dell'anno e compatibile con gli anni reali. Il mapping degli enum dovrebbe essere gestito dalle API.

### `players`

Nel nuovo schema mancano:

- `nickname`;
- `fiscal_code`;
- `address`;
- `phone_number`;
- `email`;
- `birth_date`;
- `photo_path`.

Proposta: aggiungere queste colonne come nullable. Non interferiscono con OCR o statistiche.

### `teams`

La struttura e quasi compatibile. `short_name` e lungo 30 caratteri nel nuovo schema e 40 nel vecchio.

Proposta: valutare l'estensione a 40 caratteri, oppure confermare che 30 siano sufficienti.

### `team_rosters`

Differenze:

- `jersey_number` e `INT` nel vecchio schema e `VARCHAR(10)` nel nuovo;
- nel nuovo manca `is_active`;
- `role` ha lunghezze diverse.

Il tipo testuale del numero di maglia e piu flessibile, ma richiedera conversione nel client o nelle API finche il modello C# restera `int?`.

Proposta: mantenere `VARCHAR(10)` e aggiungere `is_active TINYINT(1) NOT NULL DEFAULT 1`.

### `tournament_groups`

Nel vecchio schema `code` e obbligatorio; nel nuovo e nullable. Cambiano inoltre le lunghezze di `name` e `code`.

Decisione necessaria: stabilire se ogni girone debba avere obbligatoriamente un codice.

### `group_teams`

Le strutture sono compatibili. Il nuovo schema aggiunge `created_at` e amplia `seed_label`.

### `matches`

Questa e una delle incompatibilita principali.

Il vecchio modello usa:

- tabella `match_teams` per le squadre Home e Away;
- `court_id`;
- `name`;
- `round`;
- `scheduled_start_at` e `scheduled_end_at`;
- durate espresse in millisecondi;
- shot clock;
- durata intervallo e timeout;
- limite di punteggio;
- opzione di arresto cronometro sui liberi.

Il nuovo modello usa:

- `home_team_id` e `away_team_id` direttamente in `matches`;
- `round_label`;
- `scheduled_at`;
- durate espresse in secondi;
- `final_home_score` e `final_away_score`;
- campi `started_at` e `finished_at`.

Proposta preliminare:

- mantenere le colonne del nuovo modello;
- aggiungere solo le colonne realmente necessarie all'app;
- aggiungere `match_teams` per evitare una riscrittura estesa dell'app;
- definire nelle API il riallineamento tra `match_teams` e `home_team_id`/`away_team_id`;
- scegliere un formato canonico per le durate e convertire secondi/millisecondi nel backend.

### `match_events`

Questa e l'altra incompatibilita critica.

Il nuovo modello richiede:

- `device_id`;
- `local_event_id`;
- `client_sequence`;
- `client_created_at`;
- idempotenza tramite `(device_id, local_event_id)`;
- un elenco limitato di eventi;
- tempo espresso in secondi e decimi;
- delta del punteggio.

Il vecchio modello usa:

- tempo partita e shot clock in millisecondi;
- tipo periodo incluso `SuddenDeath`;
- campo `points`;
- descrizione libera;
- `payload_json`;
- `created_at` e `synced_at`;
- tipi evento non vincolati da enum.

Non e consigliabile forzare immediatamente entrambi i protocolli nella stessa tabella.

Opzione preferita: mantenere `match_events` del nuovo sistema per il protocollo idempotente e creare una tabella separata per gli eventi dettagliati del tabellone, ad esempio `scoreboard_match_events`. In seguito le API potranno tradurre gli eventi condivisibili.

### Stato live

Il vecchio schema usa `scoreboard_states`; il nuovo usa `live_match_state` e `live_player_state`.

Le strutture hanno scopi simili ma non equivalenti:

- il vecchio stato include shot clock e diversi contatori;
- il nuovo stato include versione, heartbeat implicito e snapshot temporale;
- il nuovo schema separa lo stato dei giocatori;
- le unita di tempo sono differenti.

Proposta iniziale: mantenere entrambi i modelli e sincronizzarli tramite API. Non usare trigger complessi finche le regole non saranno concordate.

### `match_periods`

Il vecchio schema registra durata e punteggio iniziale/finale. Il nuovo registra punteggio del periodo e timestamp, ma non la durata.

Occorre stabilire se estendere la tabella nuova o mantenere i dati aggiuntivi in una struttura separata.

### Classifiche

Il vecchio schema usa la tabella persistente `standings`, con posizione, punti classifica e note di spareggio.

Il nuovo schema usa la vista `v_standings`, calcolata dai punteggi finali e priva di:

- girone;
- punti classifica configurabili;
- posizione esplicita;
- regole e note di spareggio.

La vista non sostituisce completamente `standings`. Per mantenere le funzioni dell'app e necessario aggiungere la tabella oppure progettare una nuova struttura per regole e override.

### Gare da tre punti e altri eventi

Nel nuovo schema mancano completamente:

- `competition_events`;
- `three_point_contest_entries`;
- `three_point_contest_rounds`.

Queste tabelle possono essere aggiunte senza interferire con il sottosistema OCR.

### Sponsor e campi

`sponsors` e `courts` non hanno equivalenti nel nuovo schema e devono essere aggiunte se le relative funzioni resteranno nell'app.

## Differenze trasversali

### Enum

Il vecchio sistema usa valori PascalCase, ad esempio:

- `Draft`;
- `Active`;
- `GroupStage`;
- `Scheduled`;
- `Regular`.

Il nuovo usa valori lowercase e snake_case:

- `draft`;
- `active`;
- `group_stage`;
- `scheduled`;
- `regular`.

Non e consigliabile inserire entrambe le varianti negli enum MySQL. Il database condiviso dovrebbe mantenere un formato canonico e le API dovrebbero effettuare la conversione.

### Unita di misura

L'app e il vecchio schema usano prevalentemente millisecondi. Il nuovo schema usa secondi e decimi.

Serve una regola unica per:

- durata periodi;
- cronometro partita;
- overtime;
- shot clock;
- timeout;
- snapshot temporali.

La conversione dovrebbe essere centralizzata nelle API, evitando conversioni distribuite nella UI.

### Snapshot distruttivo

L'attuale `snapshot.php` dell'app cancella e reinserisce tutte le righe delle tabelle conosciute. Questo comportamento non e compatibile con un database condiviso contenente OCR, utenti API e dati live dei colleghi.

Prima del passaggio al nuovo database lo snapshot integrale deve essere disabilitato o sostituito da sincronizzazione selettiva e transazionale.

## Strategia raccomandata

1. Usare il nuovo database come base condivisa.
2. Non eliminare, rinominare o riutilizzare semanticamente le strutture dei colleghi.
3. Estendere le anagrafiche comuni con colonne nullable o compatibili.
4. Aggiungere le tabelle funzionali dell'app che non hanno equivalenti.
5. Mantenere inizialmente separati i due modelli di eventi e stato live.
6. Conservare `match_teams` come livello di compatibilita per ridurre le modifiche all'app.
7. Demandare alle API mapping di enum, unita di misura e modelli duplicati.
8. Rimuovere la sincronizzazione tramite snapshot distruttivo prima di collegare l'app al database condiviso.

## Piano di lavoro proposto

### Fase 1 - Accordo sul modello condiviso

Concordare con i colleghi:

- proprietario di ogni tabella;
- formato canonico degli enum;
- unita canonica dei tempi;
- sorgente autorevole per punteggi e stato live;
- gestione delle squadre della partita;
- formato degli eventi;
- regole della classifica;
- politica di cancellazione e aggiornamento.

### Fase 2 - Script SQL additivo

Preparare uno script versionato che:

- aggiunga le colonne compatibili mancanti;
- crei le tabelle dell'app non presenti;
- non elimini dati o strutture esistenti;
- includa indici e foreign key;
- sia verificabile prima su una copia del database.

### Fase 3 - Compatibilita dei modelli

Definire il mapping per:

- `match_teams` e squadre incluse in `matches`;
- `scoreboard_states` e `live_match_state`;
- `match_players` e `live_player_state`;
- punteggi correnti e punteggi finali;
- secondi, decimi e millisecondi;
- valori enum C# e MySQL.

### Fase 4 - Revisione REST

Solo dopo la stabilizzazione dello schema:

- aggiornare `entities.php`;
- sostituire lo snapshot integrale;
- introdurre endpoint live dedicati;
- preservare autenticazione e idempotenza;
- aggiungere validazioni e transazioni per operazioni composte.

### Fase 5 - Adeguamento minimo dell'app

Mantenere i modelli C# esistenti dove possibile e concentrare la traduzione nei client REST. Modificare l'app solo dove i concetti sono realmente differenti e non possono essere adattati dal backend.

## Decisioni da prendere con i colleghi

1. `home_team_id`/`away_team_id` sono la sorgente autorevole oppure lo e `match_teams`?
2. Quale sistema scrive il punteggio durante il live?
3. Quale sistema chiude una partita e determina il risultato finale?
4. Gli eventi del tabellone devono entrare nella loro `match_events` oppure restare separati?
5. Il loro protocollo richiede sempre un record valido in `api_users` per l'app desktop?
6. Le durate canoniche saranno memorizzate in secondi o millisecondi?
7. I numeri di maglia possono contenere caratteri non numerici?
8. La classifica deve essere solo calcolata o deve supportare punti e correzioni manuali?
9. Servono ancora gare da tre punti, sponsor, campi e risultati a tavolino?
10. Chi puo cancellare tornei, edizioni, squadre, giocatori e partite?
11. Le cancellazioni a cascata attuali sono accettabili per tutti i sottosistemi?
12. Come verranno versionate e distribuite le future migrazioni SQL?

## Stato

Documento di analisi preliminare. Nessuna modifica e stata applicata ai database, alle API PHP o all'app desktop.
