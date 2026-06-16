# Architettura Applicativa - Piazzetta Madness

Versione: 0.1

## Scelta Tecnologica

La scelta proposta per la prima versione e:

```text
C#/.NET
WPF per la console operatore
SQLite + Entity Framework Core per il database locale
WebView2 per le finestre tabellone pubbliche
HTML/CSS/JavaScript locale per la grafica del tabellone
```

## Motivazione

La console operatore deve essere stabile, locale e integrata con il PC Windows usato al torneo.

Il tabellone pubblico deve invece essere graficamente curato, animabile e facile da modificare. Per questo viene renderizzato con tecnologie web dentro WebView2, mantenendo pero il controllo dello stato partita in C#.

## Componenti Principali

```text
WPF Operator Console
  -> Game Engine C#
  -> Official Clock Services C#
  -> SQLite / EF Core
  -> Scoreboard Broadcaster
  -> WebView2 Scoreboard Windows
```

### WPF Operator Console

Interfaccia visibile solo all'operatore.

Responsabilita:

- selezionare/avviare partita;
- controllare cronometro gara;
- controllare 24 secondi;
- assegnare punti;
- assegnare falli;
- gestire timeout;
- correggere eventi;
- aprire una o piu finestre tabellone.

### Game Engine

Modulo C# che contiene le regole partita.

Responsabilita:

- applicare punti;
- gestire falli personali e squadra;
- gestire bonus;
- gestire timeout;
- gestire fine periodo;
- gestire limite 45 punti;
- gestire supplementari;
- produrre eventi partita.

### Official Clock Services

Fonte ufficiale del tempo gara e del tempo 24 secondi.

Regola fondamentale:

```text
Il tempo ufficiale vive solo in C#.
La WebView2 visualizza il tempo, ma non lo decide.
```

Il clock deve usare un riferimento monotono, per esempio `Stopwatch`, non `DateTime.Now`.

Quando il tempo parte:

```text
remaining_ms_at_start
monotonic_started_at
is_running = true
```

Il tempo corrente viene calcolato come:

```text
remaining_ms = remaining_ms_at_start - elapsed_monotonic_ms
```

Quando il tempo viene fermato:

```text
remaining_ms = valore calcolato al momento dello stop
is_running = false
```

### Scoreboard Broadcaster

Modulo C# che invia snapshot JSON alle finestre WebView2.

Responsabilita:

- inviare stato partita al tabellone;
- inviare sync clock;
- inviare eventi grafici, per esempio cambio punteggio o timeout;
- mantenere tutte le finestre tabellone coerenti.

### WebView2 Scoreboard Windows

Finestre pubbliche spostabili su monitor esterni.

Responsabilita:

- renderizzare tabellone fullscreen;
- mostrare punteggio;
- mostrare cronometro gara;
- mostrare 24 secondi;
- mostrare falli, timeout, periodo;
- mostrare animazioni, loghi, colori squadra, sponsor;
- non decidere mai lo stato partita.

## Sincronizzazione Clock Con WebView2

C# invia alla WebView2 uno snapshot simile:

```json
{
  "type": "clockSync",
  "sentAtMonotonicMs": 1845323,
  "gameClockMsRemaining": 222000,
  "shotClockMsRemaining": 17000,
  "isGameClockRunning": true,
  "isShotClockRunning": true
}
```

La WebView2 puo aggiornare il rendering con `requestAnimationFrame`, ma deve derivare il valore visualizzato dall'ultimo sync ricevuto.

C# invia nuovi snapshot:

- all'avvio/pausa/ripresa/reset;
- a ogni cambio importante;
- periodicamente durante il running clock.

## Database Nel Live

Il database non guida il rendering realtime.

Flusso corretto:

```text
Input operatore
  -> Game Engine
  -> Stato partita in memoria
  -> WebView2 tabellone
  -> MatchEvent su SQLite
  -> ScoreboardState snapshot su SQLite
  -> SyncQueue per online
```

## Finestre Tabellone Multiple

L'app deve poter aprire piu finestre pubbliche.

Esempi:

- tabellone principale;
- tabellone secondario;
- grafica classifica;
- schermata sponsor;
- grafica 3 Point Contest.

Tutte ricevono stato dallo stesso `ScoreboardBroadcaster`.

## Principio Di Affidabilita

Durante una partita:

- il tabellone deve continuare anche se la sync online fallisce;
- il cronometro non deve dipendere dal database;
- il database deve poter ripristinare lo stato dopo crash tramite eventi e snapshot;
- ogni comando importante deve produrre un evento persistito.

