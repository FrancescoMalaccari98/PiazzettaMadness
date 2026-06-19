# Deploy backend su Aruba — checklist (Fase 8B)

Il pacchetto `release/backend.zip` viene generato dal sorgente aggiornato in `backend/`
(vedi `tools/build-backend-package.ps1`). NON sovrascrive mai `backend/backend.zip` (snapshot storico).

> Claude non si connette né pubblica su Aruba. Il deploy è manuale.

## Contenuto del pacchetto

```
backend.zip
├── .htaccess
├── index.php
├── config/
│   └── database.example.php      ← solo riferimento (placeholder, nessun segreto)
├── endpoints/
│   ├── matches.php
│   ├── context.php               ← Fase 3
│   └── import.php                ← Fase 8 (verifica playerId canonici, 422)
└── lib/
    ├── auth.php
    ├── helpers.php
    └── response.php
```

### Escluso dal pacchetto (sicurezza)

- `config/database.php` (credenziali MySQL reali + `OCR_API_TOKEN`)
- token API, file `.env`, log, cache, backup, file temporanei
- test e documentazione di sviluppo
- file Git (`.git/`), file Claude Code (`.claude/`)
- vecchi archivi (`backend/backend.zip`)

## Controlli prima della generazione (eseguiti dallo script)

1. `php -l` su tutti i file PHP inclusi  ⚠ **richiede PHP installato** (non disponibile in locale: validare prima del deploy)
2. Route pattern presenti in `index.php`: `health`, `matches/today`, `matches/{id}/context`, `import/{id}`
3. Presenza di `endpoints/context.php`
4. `endpoints/import.php` è la versione aggiornata (Fase 8: verifica canonica + 422)
5. Presenza di `.htaccess`
6. Scansione segreti (nessun token/credenziale hardcoded nei file inclusi)
7. Elenco file inclusi → `release/backend-manifest.txt`
8. Hash SHA-256 dell'archivio → manifest

## Checklist deploy manuale su Aruba

1. **Backup**: scaricare la cartella backend attualmente pubblicata (`/api-ocr/`) in locale.
2. Verificare il percorso di destinazione su Aruba (es. `httpdocs/api-ocr/` o equivalente).
3. **Non eliminare** il `config/database.php` esistente su Aruba (contiene i segreti).
4. Caricare `release/backend.zip` in una directory temporanea.
5. Estrarre in una cartella temporanea (non sovrascrivere direttamente la produzione).
6. Verificare la struttura risultante e la presenza di `.htaccess`.
7. Copiare i file nella destinazione **mantenendo** il `config/database.php` esistente
   (NON sovrascriverlo con `database.example.php`).
8. Verificare i permessi file e che `.htaccess` sia attivo (mod_rewrite).
9. Test funzionali (con token valido nell'header `Authorization: Bearer ...`):
   - `GET /api-ocr/health` → `{ "status": "ok" }` (no auth)
   - `GET /api-ocr/matches/today?date=YYYY-MM-DD` → lista partite
   - `GET /api-ocr/matches/{id}/context` → roster (o 404/409)
   - `POST /api-ocr/import/{id}` con payload di prova → 200 / 422 (se playerId non validi)
10. Controllare i log PHP per errori.
11. In caso di errore: eseguire il rollback (sotto).

## Rollback

1. Ripristinare dal backup (punto 1) i file backend precedenti nella destinazione.
2. Mantenere/ripristinare il `config/database.php` di produzione.
3. Verificare nuovamente i quattro endpoint (`health`, `matches/today`, `context`, `import`).
4. Controllare i log PHP.

## Note

- Il nuovo `import.php` si aspetta `players[].playerId` canonici (risolti da C#) e li verifica sul
  roster: rifiuta con **HTTP 422** gli ID non validi. Un client che invia ancora il vecchio payload
  (senza `playerId`) riceverà 422 / skip — coordinare il deploy dell'app C# (Fase 8) con il backend.
- `backend/backend.zip` resta come snapshot storico finché il deploy del nuovo pacchetto non è verificato (rimozione pianificata in Fase 9).
