# API REST Piazzetta Madness

Guida semplice per utilizzare le API del database da altri programmi.

## Indirizzo principale

```text
https://www.piazzettamadness.it/api/entities.php
```

Tutte le richieste e le risposte utilizzano JSON.

## Autenticazione

Ogni richiesta deve contenere l'header:

```text
X-Api-Key: CHIAVE_FORNITA_DALL_AMMINISTRATORE
```

La chiave non deve essere inserita in repository pubblici, pagine web o applicazioni distribuite senza protezione.

## Controllo funzionamento

```http
GET /api/entities.php?health=1
X-Api-Key: CHIAVE_API
```

Risposta indicativa:

```json
{
  "ok": true,
  "api": "piazzetta-madness-entities",
  "version": "2026-06-08.1"
}
```

## Operazioni normali

L'entita da utilizzare viene indicata con il parametro `table`.

### Leggere tutti i dati

```http
GET /api/entities.php?table=teams
```

Restituisce un array JSON con tutte le righe.

### Creare un dato

```http
POST /api/entities.php?table=teams
Content-Type: application/json
```

```json
{
  "edition_id": 1,
  "name": "Squadra Blu",
  "short_name": "BLU",
  "primary_color": "#2563EB",
  "secondary_color": "#FFFFFF"
}
```

Restituisce il dato creato, compreso il nuovo `id`.

### Modificare un dato

```http
PUT /api/entities.php?table=teams&id=3
Content-Type: application/json
```

```json
{
  "name": "Squadra Blu Modificata",
  "short_name": "BLU"
}
```

### Eliminare un dato

```http
DELETE /api/entities.php?table=teams&id=3
```

L'eliminazione puo essere rifiutata se il dato e utilizzato da altre tabelle o dalle statistiche ufficiali.

## Entita disponibili

Le operazioni `GET`, `POST`, `PUT` e `DELETE` sono disponibili per:

| Valore `table` | Contenuto |
|---|---|
| `tournaments` | Tornei |
| `editions` | Edizioni dei tornei |
| `courts` | Campi da gioco |
| `sponsors` | Sponsor |
| `teams` | Squadre |
| `players` | Giocatori |
| `team_rosters` | Giocatori appartenenti alle squadre |
| `tournament_groups` | Gironi |
| `group_teams` | Squadre inserite nei gironi |
| `matches` | Partite |
| `match_teams` | Squadre e punteggi delle partite |
| `match_players` | Giocatori e dati live delle partite |
| `match_events` | Eventi live delle partite |
| `scoreboard_states` | Stato corrente dei tabelloni |
| `standings` | Classifiche |
| `forfeit_results` | Risultati a tavolino |
| `competition_events` | Eventi aggiuntivi del torneo |
| `three_point_contest_entries` | Partecipanti alla gara da tre punti |
| `three_point_contest_rounds` | Round della gara da tre punti |

Le tabelle `match_player_stats` e `match_team_stats` non sono esposte da questo endpoint. Sono gestite dal programma dedicato alle statistiche.

## Azioni speciali

Queste operazioni aggiornano piu tabelle insieme e sono preferibili alle singole chiamate quando si gestiscono partite, risultati a tavolino e classifiche.

### Creare o modificare una partita

```http
POST /api/entities.php?action=save_match
Content-Type: application/json
```

```json
{
  "match": {
    "id": 0,
    "edition_id": 1,
    "phase": "GroupStage",
    "status": "Scheduled",
    "period_duration_ms": 720000,
    "shot_clock_ms": 24000
  },
  "home": {
    "team_id": 1
  },
  "away": {
    "team_id": 2
  }
}
```

Per modificare una partita esistente, indicare il suo `id`.

### Eliminare una partita

```http
DELETE /api/entities.php?action=delete_match&id=5
```

La partita non viene eliminata se contiene statistiche ufficiali in `match_player_stats` o `match_team_stats`.

### Preparare i giocatori della partita live

```http
POST /api/entities.php?action=initialize_match_players&id=5
```

Copia automaticamente in `match_players` tutti i giocatori attivi delle due squadre. Se i giocatori erano gia stati inizializzati, restituisce quelli esistenti.

### Creare o modificare un risultato a tavolino

```http
POST /api/entities.php?action=save_forfeit
Content-Type: application/json
```

Il corpo deve contenere gli oggetti `forfeit`, `match`, `home` e `away`.

### Eliminare un risultato a tavolino

```http
DELETE /api/entities.php?action=delete_forfeit&id=2
```

### Sostituire la classifica

```http
PUT /api/entities.php?action=replace_standings
Content-Type: application/json
```

```json
{
  "standings": [
    {
      "group_id": 1,
      "team_id": 1,
      "played": 2,
      "wins": 2,
      "losses": 0,
      "points_for": 42,
      "points_against": 30,
      "point_difference": 12,
      "ranking_points": 4,
      "position": 1
    }
  ]
}
```

## Codici di risposta principali

| Codice | Significato |
|---|---|
| `200` | Operazione completata |
| `201` | Dato creato |
| `400` | Tabella, azione o richiesta non supportata |
| `401` | Chiave API assente o errata |
| `404` | Dato non trovato |
| `409` | Operazione bloccata da dati collegati |
| `422` | Dati mancanti o non validi |
| `500` | Errore interno o del database |

Gli errori vengono restituiti in JSON:

```json
{
  "error": "Descrizione breve",
  "detail": "Informazione aggiuntiva"
}
```

## Endpoint precedenti

### `tournaments.php`

Endpoint precedente dedicato ai soli tornei. Continua a supportare `GET`, `POST`, `PUT` e `DELETE`, ma per nuove integrazioni e preferibile utilizzare:

```text
entities.php?table=tournaments
```

### `snapshot.php`

Permette ancora la lettura completa tramite `GET`, ma le scritture `POST` e `PUT` sono disabilitate. Non deve essere usato per nuove sincronizzazioni; utilizzare le operazioni incrementali di `entities.php`.

## Esempio cURL

```bash
curl -X GET "https://www.piazzettamadness.it/api/entities.php?table=teams" \
  -H "X-Api-Key: CHIAVE_API"
```
