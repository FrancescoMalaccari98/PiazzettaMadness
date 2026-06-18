# BasketPdfStats

Applicazione desktop WPF .NET 10 per la conversione di tabellini PDF FIBA Live Stats in JSON normalizzato. Elabora un PDF alla volta. La correttezza dei dati ha priorità sulla velocità.

## Regola fondamentale

> **L'OCR estrae statistiche. Non crea identità.**

Partite, squadre, giocatori, numeri di maglia e nomi ufficiali provengono dal database. Il roster deve essere caricato dal DB prima dell'elaborazione del PDF. Se il DB non è disponibile, l'elaborazione è bloccata senza fallback.

## Architettura attuale

```
UI (WPF) — MainWindow / MainViewModel
  ├─ IOcrImportService → GET /api-ocr/matches/today?date=  (PHP REST → MySQL)
  │                    → POST /api-ocr/import/{matchId}
  └─ IPdfProcessingPipeline
       ├─ CH1 TesseractFullPageOcrEngine  → fiba_pdf_to_json.worker  (.venv)
       ├─ DocumentPreparationStage        → pdf_crop_runner           (.venv-crop)
       ├─ CH2 TesseractLayoutCropsOcrEngine → crop_worker.py         (.venv)
       ├─ CH3 PaddleCropOcrEngine          → paddle_crop_worker.py   (.venv-paddle)
       ├─ CH4 PaddleRowOcrEngine           → paddle_row_worker.py    (.venv-paddle)
       ├─ NormalizedOcrReconciler          (voto pesato + math tiebreaker)
       └─ StatsValidationService           (formule matematiche)
```

DB: MySQL su Aruba, accessibile solo via PHP REST API. C# non si connette direttamente al DB.

## Flusso target (refactoring in corso)

```
UI: DatePicker → data → GetMatchesForDateAsync → ComboBox partite
     → selezione partita → GetMatchContextAsync → match + squadre + roster completo
     → selezione PDF
     → pipeline OCR con MatchContext
     → PlayerIdentityMatcher: righe OCR → giocatori DB
     → riconciliazione + validazione
     → JSON con ID canonici DB (matchId, teamId, playerId)
```

Vedere [docs/refactor-plan.md](docs/refactor-plan.md) per le fasi.

## Ambiente

- OS: Windows 11, PowerShell
- Runtime: .NET 10 SDK
- UI: WPF + WinForms (`net10.0-windows`)
- Python 3.10+ richiesto per i worker
- Tesseract nel PATH (`tesseract --version` per verificare)
- Venv OCR: `fiba_pdf_to_json_programma_v2\fiba_pdf_to_json\.venv`
- Venv crop: `.venv-crop\Scripts\python.exe`
- Venv Paddle: `.venv-paddle\Scripts\python.exe`

## Comandi

```powershell
# Build
dotnet build .\BasketPdfStats.sln

# Test mirati (sviluppo normale)
dotnet test .\BasketPdfStats.sln --filter "FullyQualifiedName~Tesseract|FullyQualifiedName~Pipeline|FullyQualifiedName~Preparation"

# Test completi (checkpoint / prima di commit)
dotnet test .\BasketPdfStats.sln

# Avvio app
dotnet run --project .\src\BasketPdfStats.App\BasketPdfStats.App.csproj
```

## Struttura progetto

```
src/
  BasketPdfStats.Core/              interfacce, modelli, validazione
  BasketPdfStats.Infrastructure/    pipeline, riconciliazione, DB client, serializzazione
  BasketPdfStats.Ocr.TesseractPython/  4 motori OCR + PythonProcessRunner
  BasketPdfStats.Ocr.Mock/          mock per test
  BasketPdfStats.App/               WPF UI, composizione, MVVM
tests/
  BasketPdfStats.Tests/             xUnit (28+ file test)
fiba_pdf_to_json_programma_v2/     worker Python (Tesseract full-page + Paddle)
pdf_crop_runner/                    calibrazione layout Python
pdf-structure/                      template FIBA (fonte di verità geometrica)
backend/                            PHP REST API (deploy su Aruba)
database/                           schema MySQL e contratto import
Config/                             appsettings.json (⚠ non committare)
docs/                               refactor-plan.md, legacy-code-inventory.md
.claude/                            regole, skills, agenti, hook
```

## Canali OCR

| # | Classe C# | Worker Python | Venv | Stato |
|---|-----------|---------------|------|-------|
| 1 | TesseractFullPageOcrEngine | worker.py | .venv (fiba_pdf_to_json) | Validato |
| 2 | TesseractLayoutCropsOcrEngine | crop_worker.py | .venv (fiba_pdf_to_json) | Parz. validato |
| 3 | PaddleCropOcrEngine | paddle_crop_worker.py | .venv-paddle | Implementato |
| 4 | PaddleRowOcrEngine | paddle_row_worker.py | .venv-paddle | Implementato |

## Regole obbligatorie

1. Un PDF alla volta. Niente batch, code o scansioni cartelle.
2. Roster DB caricato prima dell'OCR. Nessuna discovery autonoma da OCR.
3. DB non disponibile → blocca elaborazione, mostra errore, nessun fallback OCR.
4. Giocatore DB non trovato nel PDF → anomalia revisionale, non inserimento silenzioso.
5. Valori mancanti → `null` o collezioni vuote, mai inventati.
6. Schema JSON stabile: qualsiasi modifica richiede approvazione esplicita.
7. Ogni modifica lascia la soluzione compilabile e i test verdi.
8. CH4 resta attivo finché non esistono benchmark misurati.
9. Non reintrodurre Adobe OCR, GLM-OCR o provider API esterni.
10. Non committare `Config/appsettings.json` (contiene token API).

## Gestione errori

- DB non disponibile → messaggio + disabilita elaborazione + retry
- Team mismatch → warning con squadre attese vs rilevate + scelta utente
- Inversione home/away → rilevazione + riallineamento + flag `SideInversionApplied`
- CH1 fallisce → elaborazione fallisce (CH1 è obbligatorio)
- CH2-CH4 falliscono → riconciliazione procede con i canali rimanenti
- Calibrazione fallisce → CH2-CH4 non eseguiti
- Giocatore non abbinato → `IdentityReviewItem`, status `CompletedWithReviewRequired`

## Sicurezza

- `Config/appsettings.json` contiene bearer token: non committare (già in `.gitignore`)
- Usa `Config/appsettings.example.json` come template
- Non loggare il token o dati OCR grezzi voluminosi su stdout

## Documenti di contesto

| Documento | Quando caricarlo |
|-----------|-----------------|
| [TASK_STATUS.md](TASK_STATUS.md) | Stato canali OCR, gap aperti |
| [docs/refactor-plan.md](docs/refactor-plan.md) | Fasi di refactoring approvate |
| [docs/legacy-code-inventory.md](docs/legacy-code-inventory.md) | Inventario codice legacy |
| [.claude/rules/](`.claude/rules/`) | Regole dettagliate per area |
| [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md) | Architettura completa e policy |
| [VALIDATION_RULES.md](VALIDATION_RULES.md) | Formule matematiche validazione |
| [pdf-structure/README.md](pdf-structure/README.md) | Zone layout FIBA |

## Checklist pre-commit

- [ ] `dotnet build` senza errori
- [ ] Test mirati verdi (o completi se modifica alla pipeline)
- [ ] Nessun valore inventato o hardcoded
- [ ] Schema JSON invariato (o approvazione documentata)
- [ ] `git status` non mostra `Config/appsettings.json` in staging
- [ ] TASK_STATUS.md aggiornato se il comportamento è cambiato
