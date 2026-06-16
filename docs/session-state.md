# Stato Progetto

Aggiornato al 2026-06-16.

## Obiettivo

Applicazione desktop Windows per gestire il torneo Piazzetta Madness:

- anagrafiche torneo, squadre, giocatori, gironi, calendario e sponsor;
- console operatore per partite live;
- tabelloni pubblici in finestre separate;
- carosello sponsor;
- statistiche giocatori durante la partita;
- 3 Point Contest;
- sincronizzazione con database online tramite API PHP.

## Stack

- .NET 8 / WPF.
- WebView2 per i tabelloni HTML/CSS/JavaScript.
- SQLite locale per cache/sessione live.
- MySQL online come sorgente principale delle anagrafiche e dei dati condivisi.
- API PHP in `server/api`.

## Avvio E Configurazione

L'app richiede `online-api.local.json`.

Se il file manca, l'app mostra errore e blocca l'interfaccia. Questo evita di lavorare per errore con dati online non configurati.

File esempio:

```text
online-api.example.json
```

Il file locale non va caricato su Git.

## Database E File Locali

Percorso dati runtime:

```text
%LOCALAPPDATA%\PiazzettaMadness
```

File/cartelle principali:

```text
piazzetta-madness-live.db
assets/
```

`assets/` contiene immagini importate da app per sponsor, squadre e giocatori. I percorsi salvati sono relativi alla cartella dati locale, cosi la cartella puo essere copiata su un altro PC.

## CRUD

Sono presenti CRUD per:

- tornei;
- edizioni;
- campi;
- squadre;
- giocatori;
- roster;
- gironi;
- squadre nei gironi;
- partite;
- eventi competizione;
- partecipanti 3 Point Contest;
- prove 3 Point Contest;
- risultati a tavolino;
- classifiche;
- sponsor.

Pattern UI:

- tabella non editabile;
- pulsanti `Nuovo`, `Modifica`, `Elimina`;
- doppio click per modifica;
- form modale;
- eliminazione bloccata quando ci sono dati collegati.

## Partita Live

Modalita:

- `Partita DB`;
- `Libera`;
- `3 Point Contest`.

Funzioni principali:

- prepara, inizia, pausa, riprendi, chiudi;
- cronometro partita;
- 24 secondi con reset 24 e reset 14;
- tasti rapidi: spazio per tempo partita, 0 per 24 secondi, 1 reset 14, 2 reset 24;
- 2 tempi regolamentari;
- overtime;
- punteggi squadra;
- falli squadra;
- punti/falli per singolo giocatore;
- animazioni opzionali sul tabellone;
- azioni grafiche `+3`, tiro libero, dunk, block;
- reset partita e reset cronometri.

## Display Tabelloni

La tab `Display tabelloni` applica sempre il contenuto a tutti i tabelloni aperti.

Modalita disponibili:

- partita e punteggi;
- carosello sponsor;
- statistiche giocatori;
- 3 Point Contest.

Per il carosello sponsor e disponibile l'intervallo configurabile in secondi.

La barra partita nella schermata sponsor viene mostrata solo se esiste una partita in corso o in pausa.

Le statistiche giocatori possono essere mostrate solo durante una partita ufficiale in corso o in pausa.

## Tabellone Pubblico

File reali usati dall'app:

```text
src/PiazzettaMadness.App/Scoreboard/index.html
src/PiazzettaMadness.App/Scoreboard/styles.css
src/PiazzettaMadness.App/Scoreboard/app.js
```

Le pagine in `html/` sono reference/demo grafiche.

Il tabellone riceve messaggi JSON dal `ScoreboardBroadcaster`.

## 3 Point Contest

Flusso:

- creare evento, partecipanti e prove dal CRUD;
- selezionare modalita `3 Point Contest`;
- selezionare evento, tiratore e prova;
- registrare punteggio per postazione con `+1`, `+2`, `-1`;
- usare timer da 60 secondi;
- mostrare grafica dedicata sul tabellone.

Non vengono registrati i singoli palloni, solo il punteggio totale delle 5 postazioni.

Durante una prova attiva o in pausa non si possono cambiare evento, tiratore o prova.

## Database Online

Schema reale:

```text
server/migrations/db_struttura.sql
```

Seed dati test/ripristino:

```text
server/migrations/piazzetta_test_edition_seed.sql
```

Il seed contiene solo dati, pulisce le tabelle interessate e reinserisce record con ID espliciti. Va usato su ambienti di test/ripristino, non su produzione con dati da conservare.

## File Da Pubblicare Su Git

Da includere:

- `src/`;
- `docs/`;
- `html/`;
- `server/` escluso `server/api/config.php`;
- `README.md`;
- regolamento PDF.

Da non includere:

- `online-api.local.json`;
- database locali;
- `publish/`, `bin/`, `obj/`;
- `Fonts/`.
