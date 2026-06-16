# Stato progetto - Piazzetta Madness

Aggiornato al 2026-06-15.

## Obiettivo

Applicazione desktop Windows per gestire un torneo di basket locale:

- gestione dati torneo tramite CRUD locale;
- console operatore per partita live;
- uno o piu tabelloni pubblici su finestre separate;
- futura sincronizzazione verso database online per sito pubblico/live score.

## Stack scelto

- App desktop WPF in C#/.NET 8.
- Database locale SQLite tramite Entity Framework Core.
- Tabellone pubblico renderizzato con HTML/CSS/JavaScript dentro WebView2.
- Comunicazione app-tabelloni tramite `ScoreboardBroadcaster`, con stato JSON inviato alle WebView.

## Database e CRUD

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
- convocati partita;
- eventi 3 punti;
- partecipanti 3 punti;
- round 3 punti;
- risultati a tavolino;
- classifiche;
- sponsor.

Pattern CRUD scelto:

- tabella non editabile;
- pulsanti `Nuovo`, `Modifica`, `Elimina`;
- doppio click per modifica;
- form modale per inserimento/modifica;
- eliminazione bloccata quando esistono collegamenti con altre tabelle;
- niente popup di conferma salvataggio quando una modifica va a buon fine.

## Partita live

Sono implementati:

- modalita `Partita DB` e `Libera`;
- selezione partita da database oppure uso senza salvataggio;
- stati partita chiari: prepara, inizia, pausa, riprendi, chiudi;
- inizio partita non avvia automaticamente il cronometro;
- chiusura partita controllata, con blocco in caso di parita;
- periodo selezionabile direttamente;
- punteggi squadra;
- falli squadra;
- punti e falli per singolo giocatore;
- cronometro partita con tasto unico start/pausa;
- cronometro 24 secondi con tasto unico start/pausa e reset;
- reset tempo partita con conferma e reset dei 24 secondi;
- reset 24 secondi senza conferma, mantenendo la corsa se era gia attivo.

## Tabellone pubblico

Sono implementati:

- apertura di piu finestre tabellone;
- F11 per passare a fullscreen;
- Esc per uscire dal fullscreen;
- chiusura manuale quando la finestra e in modalita normale;
- visualizzazione tabellone partita;
- visualizzazione sponsor in stile carosello;
- tracking dei tabelloni aperti con id/nome;
- gestione contenuto pubblico da tab dedicata `Display tabelloni`.

Nella tab `Display tabelloni`:

- il contenuto scelto viene applicato sempre a tutti i tabelloni attivi;
- si sceglie tra partita, sponsor, statistiche giocatori e 3 Point Contest;
- statistiche giocatori e barra sponsor usano la partita in corso quando disponibile.

## 3 Point Contest

La modalita `3 Point Contest` e disponibile nella console live.

Flusso dati scelto:

- nel CRUD si creano evento, partecipanti e prove;
- una prova corrisponde a un tentativo da 60 secondi di un partecipante;
- ogni prova ha numero e fase: `Qualification`, `Final` oppure `TieBreak`;
- nel live si seleziona una prova esistente e si registrano i risultati;
- non vengono create prove automaticamente dalla schermata live;
- sono registrati i punteggi totali delle cinque postazioni, non i singoli palloni;
- il totale del partecipante viene ricalcolato dai risultati delle prove.

Controlli operatore:

- timer da 60 secondi con avvio, pausa, ripresa e conclusione;
- selezione postazione da 1 a 5;
- correzioni `+1`, `+2` e `-1`;
- reset completo della prova;
- durante una prova in corso o in pausa sono bloccati evento, tiratore, prova, cambio modalita e scheda Database;
- la selezione partita viene nascosta nella modalita contest;
- il CRUD `3 Point Prove` e `3 Point Partecipanti` si aggiorna immediatamente dopo modifiche o reset live;
- database locale e database online vengono aggiornati dalla gestione live.

Tabellone pubblico 3 Point Contest:

- grafica derivata da `html/gara-tre-punti.html`;
- mappa del campo con postazione attiva evidenziata;
- nome evento, squadra, giocatore, timer, stato e punteggi dinamici;
- colore squadra applicato al pannello giocatore e all'ombra del campo;
- fascia inferiore animata;
- non vengono mostrati palloni segnati/sbagliati perche il dato non viene registrato.

## Sponsor

CRUD sponsor implementato con:

- nome;
- descrizione;
- immagine;
- attivo/disattivo;
- ordinamento.

Gli sponsor attivi vengono mostrati nel carosello dei tabelloni pubblici.

## File principali

- `src/PiazzettaMadness.App/MainWindow.xaml`
- `src/PiazzettaMadness.App/MainWindow.xaml.cs`
- `src/PiazzettaMadness.App/ScoreboardWindow.xaml`
- `src/PiazzettaMadness.App/ScoreboardWindow.xaml.cs`
- `src/PiazzettaMadness.App/Live/ScoreboardBroadcaster.cs`
- `src/PiazzettaMadness.App/Live/ScoreboardState.cs`
- `src/PiazzettaMadness.App/Live/GameClock.cs`
- `src/PiazzettaMadness.App/Data/Entities.cs`
- `src/PiazzettaMadness.App/Data/AppDbContext.cs`
- `src/PiazzettaMadness.App/Data/DatabaseInitializer.cs`
- `src/PiazzettaMadness.App/Scoreboard/index.html`
- `src/PiazzettaMadness.App/Scoreboard/styles.css`
- `src/PiazzettaMadness.App/Scoreboard/app.js`

## Documenti di progetto

- `docs/er-model.md`
- `docs/sqlite-schema-v1.sql`
- `docs/mvp-scope.md`
- `docs/database-decisions.md`
- `docs/architecture.md`

## Comandi utili

Build:

```powershell
dotnet build PiazzettaMadness.sln
```

Avvio:

```powershell
dotnet run --project src\PiazzettaMadness.App\PiazzettaMadness.App.csproj
```

Se la build fallisce per file bloccato, probabilmente l'app e ancora aperta. Chiuderla oppure terminare il processo prima della build.

## Prossimi step consigliati

1. Eseguire test manuali completi del 3 Point Contest con database online attivo.
2. Verificare il tabellone contest su monitor/proiettore reale e rifinire le proporzioni.
3. Decidere se registrare in futuro anche l'esito dei singoli palloni.
4. Migliorare la gestione sponsor: anteprime e ordinamento visuale.
