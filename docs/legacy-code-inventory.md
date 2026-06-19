# Inventario codice legacy — BasketPdfStats

Data analisi: 2026-06-18  
Nota: le verifiche contrassegnate con ✓ sono state eseguite realmente. Le altre sono stimate dall'analisi documentale.

---

## 1. Riepilogo per stato

| Stato | Count | Note |
|-------|-------|------|
| Attivo | 18 | Non toccare |
| Attivo — da sostituire (Fase 5) | 1 | `known_names.py` |
| Legacy / superseded | 2 | `prepare_crops.py`, `backend/backend.zip` |
| Già rimosso — verificato parzialmente | 2 | Adobe OCR, GLM-OCR |
| Da verificare prima della Fase 9 | 3 | `prepare_crops` riferimenti, `import.php` divergenza Aruba, `.htaccess` Aruba |
| Sicurezza — azione immediata | 1 | Token API in git |

---

## 2. Codice attivo (non toccare)

| Componente | Path | Note |
|-----------|------|------|
| TesseractFullPageOcrEngine | `src/BasketPdfStats.Ocr.TesseractPython/TesseractFullPageOcrEngine.cs` | CH1 validato |
| TesseractLayoutCropsOcrEngine | `src/BasketPdfStats.Ocr.TesseractPython/TesseractLayoutCropsOcrEngine.cs` | CH2 parzialmente validato |
| PaddleCropOcrEngine | `src/BasketPdfStats.Ocr.TesseractPython/PaddleCropOcrEngine.cs` | CH3 implementato, non validato E2E |
| PaddleRowOcrEngine | `src/BasketPdfStats.Ocr.TesseractPython/PaddleRowOcrEngine.cs` | CH4 implementato, non validato E2E |
| PdfProcessingPipeline | `src/BasketPdfStats.Infrastructure/Pipeline/PdfProcessingPipeline.cs` | Orchestratore principale |
| NormalizedOcrReconciler | `src/BasketPdfStats.Infrastructure/Reconciliation/NormalizedOcrReconciler.cs` | Riconciliazione pesata |
| DocumentPreparationStage | `src/BasketPdfStats.Infrastructure/Preparation/DocumentPreparationStage.cs` | Calibrazione layout |
| OcrImportService | `src/BasketPdfStats.Infrastructure/Database/OcrImportService.cs` | Client PHP REST |
| worker.py | `fiba_pdf_to_json_programma_v2/fiba_pdf_to_json/fiba_pdf_to_json/worker.py` | CH1 Python worker |
| crop_worker.py | `fiba_pdf_to_json_programma_v2/fiba_pdf_to_json/fiba_pdf_to_json/crop_worker.py` | CH2 Python worker |
| paddle_crop_worker.py | `fiba_pdf_to_json_programma_v2/fiba_pdf_to_json/fiba_pdf_to_json/paddle_crop_worker.py` | CH3 Python worker |
| paddle_row_worker.py | `fiba_pdf_to_json_programma_v2/fiba_pdf_to_json/fiba_pdf_to_json/paddle_row_worker.py` | CH4 Python worker |
| calibrate_layout.py | `pdf_crop_runner/pdf_crop_runner/calibrate_layout.py` | Calibrazione affine Y con anchor detection |
| pdf-structure/ | `pdf-structure/` | Template FIBA (fonte di verità geometrica) |
| MainViewModel | `src/BasketPdfStats.App/ViewModels/MainViewModel.cs` | MVVM viewmodel principale |
| backend/endpoints/matches.php | `backend/endpoints/matches.php` | GET /matches/today |
| backend/endpoints/import.php | `backend/endpoints/import.php` | POST /import/{id} (489 righe) |
| database/ | `database/` | Riferimento schema MySQL |

---

## 3. Componente da sostituire

### 3.1 `known_names.py` — attivo ora, da disabilitare in Fase 5 e rimuovere in Fase 9

**Percorso:** `fiba_pdf_to_json_programma_v2/fiba_pdf_to_json/fiba_pdf_to_json/known_names.py`

**Stato attuale:** Attivo in CH1. Usato durante il parsing PDF per il fuzzy matching di nomi e numeri di maglia.

**Analisi struttura (verificata):**

```
Dati hardcoded:
  KNOWN_TEAMS: dict[str, str]     — 8 squadre; chiave=nome normalizzato, valore=nome ufficiale
  KNOWN_PLAYERS: dict[str, str]   — ~64 giocatori; chiave=nome normalizzato, valore=nome ufficiale
  KNOWN_ROSTER: dict[str, dict[int, str]]  — {team_name: {jersey_number: player_name}}

Funzioni esposte:
  roster_jersey_for(team: str, name: str) → int | None
    → cerca il numero di maglia di un giocatore nel roster hardcoded
  known_players_for_team(team: str) → list[str]
    → lista di nomi normalizzati per una squadra
  fuzzy_known_name(name: str, team: str, cutoff=0.84) → str | None
    → usa difflib.SequenceMatcher; restituisce il nome ufficiale se similarity ≥ cutoff
  _norm(s: str) → str
    → lowercase, rimuovi accenti, strip

Importato in:
  fiba_pdf_to_json_programma_v2/fiba_pdf_to_json/fiba_pdf_to_json/parser.py (UNICO file)

Siti di uso in parser.py:
  L.151-152: fuzzy team name matching (cutoff=0.78)
  L.207:     normalizzazione nome squadra per la sezione header
  L.377-379: candidati giocatori per numero + fuzzy name match (cutoff=0.80)
  L.388:     cross-validazione jersey (roster_jersey_for)
```

**Problema:** I dati sono hardcoded. Aggiunta di giocatori, cambio di numero di maglia, nuova squadra = modifica manuale al file.

**Strategia di sostituzione (Fase 5):**

1. Creare `roster_context.py` nella stessa cartella con:
   - Caricamento del roster dinamico da file JSON (`--roster-json`)
   - Normalizzazione nomi (stessa logica di `_norm`)
   - Ricerca candidati per jersey
   - Fuzzy matching sul roster dinamico
   - **Non produrre ID canonici** — solo supporto al parsing

2. `parser.py` usa `roster_context.py` quando `--roster-json` è presente; usa `known_names.py` come fallback (con log `WARNING: using legacy known_names.py`)

3. **Non spostare le funzioni inline in `parser.py`**: il componente dedicato è obbligatorio.

**Schema del file JSON dinamico:**

```json
{
  "homeTeam": {
    "teamId": 5,
    "name": "Virtus",
    "players": [
      {"playerId": 101, "jerseyNumber": 3, "firstName": "Mario", "lastName": "Rossi"}
    ]
  },
  "awayTeam": {
    "teamId": 7,
    "name": "Pallacanestro",
    "players": [...]
  }
}
```

Questo JSON viene scritto da C# (`TesseractFullPageOcrEngine`) in `runtime/Working/` da `OcrMatchContext`.

**Fase 9:** eliminare `known_names.py`, eliminare il fallback, rimuovere tutti i riferimenti nei test.

**Rischio:** Medio se Fase 5 non è validata su samples/. Non rimuovere prima della validazione.

---

## 4. Codice legacy / superseded

### 4.1 `prepare_crops.py` — ✅ RIMOSSO (Fase 9, 2026-06-19)

**Percorso (era):** `pdf_crop_runner/pdf_crop_runner/prepare_crops.py`

**Stato:** Rimosso. Era legacy, superseded da `calibrate_layout.py`.

**Verifica pre-rimozione:** `grep -i "prepare_crops"` su tutto il repo → riferimenti solo in docs e
nel README di pdf_crop_runner; **nessun import** in codice Python/C#. `__init__.py` e
`image_preparation.py` non lo referenziano. Rimozione sicura confermata e approvata.

**Aggiornamenti collegati:** rimosso il riferimento in `pdf_crop_runner/README.md`.

---

### 4.2 `backend/backend.zip`

**Percorso:** `backend/backend.zip`

**Stato:** Snapshot precedente del backend PHP. **Non usare per il deploy attuale.**

**Confronto reale eseguito (✓ 2026-06-18):**

| Voce | ZIP | backend/ folder |
|------|-----|-----------------|
| `.htaccess` | ✓ presente (261 B) | ✓ presente (270 B) — **contenuto funzionalmente identico** (solo line ending differente) |
| `endpoints/import.php` | ✓ presente (16.132 B, ~330 righe) | ✓ presente (20.463 B, 489 righe) — **DIVERSI** |
| `endpoints/matches.php` | ✗ assente | ✓ presente (195 righe) — aggiunto dopo la creazione dello ZIP |
| `endpoints/context.php` | ✗ assente | ✗ assente — da creare in Fase 3 |
| `index.php` | ✓ presente (2.496 B) | ✓ presente (3.152 B, 80 righe) — **DIVERSI** (folder ha routing `/matches/today`) |
| `lib/auth.php` | ✓ presente (1.234 B) | ✓ presente (1.269 B, 34 righe) — **DIVERSI** |
| `lib/helpers.php` | ✓ presente (1.062 B) | ✓ presente (1.095 B, 51 righe) — **DIVERSI** |
| `lib/response.php` | ✓ presente (527 B) | ✓ presente (528 B) — **DIVERSI** (1 byte, prob. line ending) |
| `config/database.php` | ✗ assente | ✗ assente (escluso da git) |

**Riepilogo:**
- `.htaccess` è presente in entrambi; il contenuto è funzionalmente identico (differenza = line ending CRLF→LF).
- Tutti i 5 file PHP condivisi sono stati aggiornati significativamente nel folder dopo la creazione dello ZIP.
- `endpoints/matches.php` non esisteva quando lo ZIP è stato creato.
- Il folder è la versione **più recente e corretta** da usare come sorgente per il deploy.

**Decisione su `.htaccess`:**
Il file `.htaccess` **è già tracciato in `backend/`** e il contenuto è funzionalmente identico a quello nello ZIP. Non è necessario ripristinarlo dallo ZIP. La versione del folder è quella da includere nel pacchetto di deploy (Fase 8B).

**Dubbio aperto:** La versione di `import.php` nel folder è più recente (+4 KB). Verificare con il backup su Aruba (se accessibile) se la versione deployata su Aruba corrisponde al folder o allo ZIP prima di Fase 8B.

**Stato classificazione:** Snapshot precedente da conservare come riferimento storico. Non eliminare finché Fase 8B non è completata e il deploy su Aruba verificato.

**Raccomandazione:** Rimuovere in Fase 9 dopo il deploy verificato di `release/backend.zip`.

---

### 4.3 Nomenclatura `fiba_pdf_to_json_programma_v2`

**Stato:** Nome legacy, contenuto attivo.

**Descrizione:** Il folder name suggerisce una "v2" ma non esiste una "v1" tracciabile in Git. Tutti i file sono attivi.

**Rischio del rinomino:** Medio. Il path è hardcoded in Config/appsettings.json e nei motori OCR.

**Raccomandazione:** Non rinominare ora. Inserire in backlog come "rinomina in fiba_pdf_to_json_worker" con analisi impatto completa.

---

## 5. Già rimosso — stato verificato

### 5.1 Adobe PDF Extract

**Stato atteso:** Progetto rimosso, riferimenti residui in mock e test.

**Verifiche eseguite:** ✓ `git grep -ni "adobe"` su `*.cs`, `*.csproj`, `*.py`, `*.php`, `*.json`, `*.xaml`

**Risultati:**

| File | Riga | Contenuto | Valutazione |
|------|------|-----------|-------------|
| `src/BasketPdfStats.Ocr.Mock/MockOcrEngine.cs` | 64 | `Engine = "AdobePdfServices"` | Stringa hardcoded in mock storico — non è integrazione reale |
| `tests/BasketPdfStats.Tests/NormalizedOcrReconcilerTests.cs` | 107 | nome metodo `...agrees_across_tesseract_and_adobe` | Nome metodo storico — non è integrazione reale |

**Conclusione:** Nessun progetto `BasketPdfStats.Ocr.Adobe`, nessuna dipendenza NuGet Adobe, nessuna configurazione Adobe.  
I 2 riferimenti residui sono in codice mock/test e non rappresentano una reintegrazione.  
Lo script `validate-project.ps1` controlla `AdobePdfExtract` (non `AdobePdfServices`) — aggiornare il pattern se si vuole catturare anche la stringa mock.

**Azione consigliata (opzionale):** Rinominare il metodo test e aggiornare la stringa mock in una fase di pulizia.

---

### 5.2 GLM-OCR

**Stato atteso:** Completamente rimosso.

**Verifiche eseguite:** ✓ `git grep -ni "glm"` su `*.cs`, `*.csproj`, `*.py`, `*.php`, `*.json`, `*.xaml`

**Risultato:** **0 riferimenti trovati.** GLM-OCR completamente assente.

---

## 6. Da verificare prima della Fase 9

### 6.1 Schema `evidence_records.json` — allineamento Python/C#

**Stato:** Da verificare.

**Rischio:** Se i worker Python sono stati modificati dopo l'ultimo allineamento del modello C# (`EvidenceRecord.cs`), i campi potrebbero non corrispondere silenziosamente.

**Comandi da eseguire:**

```powershell
# Campi nel modello C#
cat src/BasketPdfStats.Core/Models/EvidenceRecord.cs

# Campi nel dataclass Python
cat "fiba_pdf_to_json_programma_v2/fiba_pdf_to_json/fiba_pdf_to_json/evidence_record.py"
```

Confrontare campo per campo: `record_id`, `field_id`, `entity_id`, `entity_type`, `side`, `stat_key`, `scope`, `crop_id`, `zone_id`, `value_raw`, `value_normalized`, `confidence_raw`, `confidence_normalized`, `mapper_id`, `warnings`.

**Raccomandazione:** Eseguire prima della Fase 5 (pipeline usa roster).

---

### 6.2 `import.php` su Aruba — divergenza dalla versione folder

**Stato:** Non verificabile senza accesso ad Aruba.

**Rischio:** La versione di `import.php` su Aruba potrebbe essere quella dello ZIP (16 KB, ~330 righe) o una versione intermedia, non quella attuale del folder (20 KB, 489 righe).

**Azione:** Prima di Fase 8B, verificare con backup Aruba o eseguendo un test di import con payload noto.

---

### 6.3 Coordinate zone in `layout-map.default.json`

**Stato:** Segnalato in TASK_STATUS, non verificato sul codice attuale.

**Problemi segnalati:**
- Crop degli arbitri vuoto
- Crop dei periodi cattura solo un lato
- Crop comparativo inferiore posizionato sulla legenda

**Verifica:** Eseguire pipeline su `samples/pdf/TABELLINO FINALE 1-2 POSTO.pdf` e ispezionare:
- `runtime/Dataset/LayoutImages/` → crop visivi
- `runtime/Debug/` → calibration report

**Raccomandazione:** Se confermati, correggere come bug funzionale separato (non nel refactoring strutturale).

---

## 7. Sicurezza — azione immediata

### 7.1 Token API in `Config/appsettings.json`

**Problema:** `Config/appsettings.json` contiene un bearer token per il backend PHP.  
Il file è presente nella storia Git in **3 commit** (315d36d, e7db292, 19bb179).

**Stato attuale:**
- `.gitignore` aggiornato: `Config/appsettings.json` è ora ignorato ✓
- `Config/appsettings.example.json` creato senza token ✓
- Il file NON è ancora rimosso dal tracking Git

**Comandi manuali da eseguire (nell'ordine):**

```bash
# 1. Ruotare prima il token su Aruba (cambia OCR_API_TOKEN nell'environment PHP)
#    Il token attuale deve essere considerato compromesso.

# 2. Rimuovere il file dal tracking Git
git rm --cached Config/appsettings.json
git commit -m "chore: remove appsettings.json from git tracking"

# 3. Verificare la storia (non rimuovere i commit — repo privato)
git log --all --oneline -- Config/appsettings.json
```

**Nota:** `git rm --cached` non elimina la storia Git. Se il repo è privato, il rischio residuo è basso.  
Per eliminare dalla storia servirebbe `git filter-branch` o `git filter-repo` — operazione distruttiva non raccomandata senza backup.

**Rischio se non fatto:** Il token consente import arbitrari di statistiche nel DB tramite `/api-ocr/import`.

---

## 8. Inventario dipendenze Python

| Venv | Pacchetti principali | Stato |
|------|---------------------|-------|
| `fiba_pdf_to_json_programma_v2/.venv` | PyMuPDF, opencv, pytesseract, numpy, Pillow | Attivo |
| `.venv-crop` | Pillow, PyMuPDF | Attivo |
| `.venv-paddle` | PaddleOCR 3.6.0, paddlex, huggingface_hub (~2.5 GB) | Attivo |

Nessuna dipendenza Python da rimuovere ora.

---

## 9. Inventario dipendenze NuGet

| Progetto | Note |
|----------|------|
| BasketPdfStats.Core | Nessuna dipendenza esterna — solo BCL |
| BasketPdfStats.Infrastructure | Nessuna — solo BCL + project refs |
| BasketPdfStats.Ocr.TesseractPython | Nessuna — solo BCL + project refs |
| BasketPdfStats.Ocr.Mock | Nessuna |
| BasketPdfStats.App | Solo SDK Windows (WPF/WinForms) |
| BasketPdfStats.Tests | xunit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, xunit.runner.visualstudio 3.1.4, coverlet.collector 6.0.4 |

**Dipendenze runtime zero** in tutti i progetti non-test. Nessuna libreria terza parte da rimuovere.
