# Regole sicurezza — BasketPdfStats

## Segreti e credenziali

### File da non leggere, modificare o committare

- `Config/appsettings.json` — contiene bearer token API per il backend PHP
- `Config/appsettings.portable.json` — potrebbe contenere token
- `backend/config/database.php` — credenziali MySQL (escluso dal repo via .gitignore)
- `.claude/settings.local.json` — impostazioni locali
- `CLAUDE.local.md` — configurazione locale personale
- Qualsiasi file `.env`

### Azione urgente (token già in git)

`Config/appsettings.json` è stato tracciato da git con un token API reale.
1. Ruotare il token sul backend PHP (Aruba environment: `OCR_API_TOKEN`)
2. `git rm --cached Config/appsettings.json`
3. Verificare la storia: `git log --all --follow -- Config/appsettings.json`

### Template sicuri

- Usare `Config/appsettings.example.json` (senza valori reali) come template committato
- Usare `.claude/settings.local.json.example` per configurazioni locali

## Processi esterni (Python workers)

- I worker Python vengono lanciati con argomenti CLI controllati da C# (`PythonProcessRunner`)
- Non passare mai il token API come argomento CLI a un worker Python (visibile in `ps aux`)
- I percorsi passati ai worker devono essere validati (solo path all'interno del repository o del runtime)
- I timeout sono obbligatori: nessun processo Python deve girare senza `TimeSpan` configurato

### Command injection

- Non costruire argomenti CLI concatenando input utente: usare liste di argomenti (`ProcessStartInfo.ArgumentList`)
- Il path del PDF selezionato dall'utente deve essere validato prima di essere passato ai worker

### Path traversal

- Verificare che i percorsi di output siano dentro `runtime/` prima di scrivere
- Non seguire symlink arbitrari
- Non accettare path assoluti arbitrari da config esterna non fidata

## File forniti dall'utente

- I PDF vengono letti ma mai eseguiti
- Il path del PDF viene hashato e usato solo per creare una copia temporanea in `runtime/Working/`
- Il nome originale del file non viene mai usato direttamente come nome di file senza sanitizzazione

## Backend PHP

- L'autenticazione usa un bearer token (timing-safe compare in `auth.php`)
- Non loggare il token nelle richieste HTTP o nei log di errore
- Il backend non espone credenziali DB al client C#

## Git

- Non committare: `Config/appsettings*.json`, `.claude/settings.local.json`, `CLAUDE.local.md`, file `.env`
- Questi file sono in `.gitignore` (verificare che rimangano esclusi dopo ogni modifica al .gitignore)
- Prima di ogni `git add -A`: verificare `git status` per file sensibili accidentalmente modificati

## Comandi distruttivi vietati

Non eseguire senza approvazione esplicita:
- `git reset --hard`
- `git push --force`
- `Remove-Item -Recurse` su cartelle non-runtime
- Eliminazione di file .csproj o .sln
- `dotnet publish` (deploy non pianificato)
