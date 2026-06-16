# Piazzetta Madness App

Applicazione desktop Windows per gestire partite live, tabelloni pubblici, sponsor, statistiche giocatori e 3 Point Contest.

## Requisiti

- Windows
- .NET 8 SDK
- Microsoft Edge WebView2 Runtime
- File di configurazione online `online-api.local.json` nella cartella da cui viene avviata l'app

Il file `online-api.local.json` non e caricato su Git perche contiene configurazione locale/segreti. Come base usare:

```text
online-api.example.json
```

## Avvio in sviluppo

Dalla root del progetto:

```powershell
dotnet run --project src\PiazzettaMadness.App\PiazzettaMadness.App.csproj
```

Se l'app non trova `online-api.local.json`, si avvia mostrando errore e l'interfaccia resta bloccata.

## Build

Per verificare che il progetto compili:

```powershell
dotnet build src\PiazzettaMadness.App\PiazzettaMadness.App.csproj -c Release
```

Se la build Debug fallisce per file bloccato, chiudere l'app gia aperta. La Release usa una cartella diversa, ma e comunque meglio chiudere l'app prima di buildare.

## Publish

Per creare una cartella pronta da copiare su un altro PC:

```powershell
dotnet publish src\PiazzettaMadness.App\PiazzettaMadness.App.csproj -c Release -o publish\PiazzettaMadness
```

Output:

```text
publish\PiazzettaMadness
```

Da quella cartella si puo avviare `PiazzettaMadness.App.exe`.

## File Da Portare Su Un Altro PC

La publish contiene il programma, ma non contiene automaticamente dati locali, immagini o configurazione.

Portare:

```text
publish\PiazzettaMadness\
```

Configurazione online:

```text
publish\PiazzettaMadness\online-api.local.json
```

Dati locali e immagini:

```text
%LOCALAPPDATA%\PiazzettaMadness\
```

Dentro possono esserci:

```text
piazzetta-madness-live.db
assets\
```

La cartella `assets` contiene immagini importate per sponsor, squadre e giocatori.

## Database Online

File utili:

```text
server\migrations\db_struttura.sql
server\migrations\piazzetta_test_edition_seed.sql
```

Per creare un database di test da zero:

```text
1. Eseguire server\migrations\db_struttura.sql
2. Eseguire server\migrations\piazzetta_test_edition_seed.sql
```

`piazzetta_test_edition_seed.sql` contiene solo dati. Prima pulisce le tabelle interessate e poi reinserisce i dati con ID espliciti, quindi va usato per test/ripristino, non su un database di produzione con dati da conservare.

## Server E API

La cartella `server/` contiene API PHP, schema e migrazioni utili al database/sito.

Non caricare mai:

```text
server\api\config.php
```

Usare invece:

```text
server\api\config.example.php
```

## Tabelloni HTML

I tabelloni usati dall'app sono dentro:

```text
src\PiazzettaMadness.App\Scoreboard\
```

La cartella:

```text
html\
```

contiene pagine/demo HTML di riferimento usate per preparare le grafiche.

## Git

Da caricare:

- codice app in `src/`
- documentazione in `docs/`
- demo/reference HTML in `html/`
- API e migrazioni in `server/`
- regolamento PDF

Restano esclusi da `.gitignore`:

- build e publish: `bin/`, `obj/`, `publish/`, `.build-check/`
- database locali: `*.db`, `*.db-shm`, `*.db-wal`
- configurazioni locali/segreti: `*.local.json`, `server/api/config.php`
- file editor/utente: `.vs/`, `.vscode/`, `*.user`, `*.suo`
- font scaricati localmente non usati dalla build: `Fonts/`

## Comandi Utili

Stato Git:

```powershell
git status --short
```

Aggiungere i file:

```powershell
git add .
```

Commit:

```powershell
git commit -m "Aggiorna app Piazzetta Madness"
```

Push sul branch corrente:

```powershell
git push
```
