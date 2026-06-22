# Task Status

## Current Known State

The project runs a local OCR **evidence-channel ensemble**. The workflow is one
selected PDF at a time:

```text
Select PDF -> Elabora PDF -> evidence channels -> weighted reconciliation -> final JSON -> result viewer
```

The selected original PDF stays in place. The pipeline creates a temporary
working copy and cleans it after processing.

## Evidence-Channel Architecture

Every OCR channel emits the same shared intermediate `evidence_records.json`
(Python `evidence_record.py` schema) and is mapped into the canonical
`ProcessingResult` by `EvidenceRecordMapper` (C#). Channels are evidence, not
final truth; the weighted reconciler + math validation decide each field.

| # | Channel | sourceId | Status |
|---|---|---|---|
| 1 | Tesseract full page | `ocr.tesseract.fullpage` | Active. Raw JSON + evidence. Anagraphic source (players, names, numbers). |
| 2 | Tesseract crops | `ocr.tesseract.crop` | Implemented and wired into the pipeline (enabled in config). Parses finalScore reliably; teamTotals shooting ratios parsed with a math self-check. |
| 3 | PaddleOCR crops | `ocr.paddle.crop` | **Implemented** (C# `PaddleCropOcrEngine` + Python `paddle_crop_worker.py`), enabled in config. Not yet validated end-to-end on a real PDF. PaddleOCR verified working in `.venv-paddle` (3.6.0). |
| 4 | PaddleOCR rows | `ocr.paddle.row` | **Implemented** (C# `PaddleRowOcrEngine` + Python `paddle_row_worker.py` + `paddle_row_parser.py`), enabled in config. Not yet validated end-to-end on a real PDF. |

All four channels are **local OCR only** (Tesseract + PaddleOCR). No external API
provider is part of the solution. Adobe and GLM-OCR are fully removed.

## Tesseract Full Page (Channel 1)

- Complete local full-rendered-page baseline; selected automatically by the UI.
- Renders the page, runs local Tesseract, writes raw diagnostic JSON, maps to the
  canonical normalized `ProcessingResult`, and exports OCR word-box geometry.
- Also emits `evidence_records.json` (`--evidence-output`).
- Known-roster fuzzy matching corrects player names and jersey numbers
  (`known_names.py`, `KNOWN_ROSTER`). Starter `*` markers preserved. Minutes kept
  as `MM:SS`. Safe derivations fill null `points` / `rebounds.total`.

## Crop Calibration (pdf_crop_runner)

- `calibrate_layout` starts from the fixed FIBA template (`pdf-structure/`) and
  applies a **bounded affine Y transform** (scale + offset) derived from robust
  anchors (final-score line + `Squadra/Allenatore` rows) via least squares.
  Tolerances live in `layout-calibration-rules.json`.
- Zone crops (header, finalScore, player tables, teamTotals, comparative) are
  reliable. The fixed template — not full-page OCR — is the geometric source of
  truth; OCR anchors only calibrate it.
- Per-row geometric crops were **removed**: FIBA player rows are too tightly
  packed to segment from word boxes. Row OCR is delegated to PaddleOCR native
  line detection on the full table crop (Channel 4, `PaddleTableRows`).
- The crop manifest (`crop-metadata.json`) carries calibrationId, transform,
  dpi, render size, and per-crop usability.

## Tesseract Crops (Channel 2)

- `TesseractLayoutCropsOcrEngine` runs `crop_worker.py` on the calibrated zone
  crops and consumes the resulting `evidence_records.json` via
  `EvidenceRecordReader` + `EvidenceRecordMapper` (legacy partial mapper kept as
  fallback).
- `crop_worker.py` stores X-ordered tokens per zone (x, width, text, conf) so a
  parser can map tokens to columns by position.
- finalScore parsed from the header crop with high confidence (~0.92).
- teamTotals shooting ratios (FG/2P/3P/FT) parsed only when all four are present
  and self-consistent (`FG = 2P + 3P`); otherwise the crop stays silent rather
  than emit misaligned values.
- Enabled in `ocr.enabledEngines`; participates in reconciliation.
- Limit: Tesseract on the small/thin number cells is the bottleneck; teamTotals
  often fail the self-check and the crop contributes only finalScore. The
  PaddleOCR channels (3, 4) are expected to unlock the numeric cells.

## PaddleOCR Crops (Channel 3) and Rows (Channel 4)

- `PaddleCropOcrEngine` runs `paddle_crop_worker.py` in `.venv-paddle` on the
  same calibrated zone crops; emits the shared `evidence_records.json` with
  `sourceId=ocr.paddle.crop`.
- `PaddleRowOcrEngine` runs `paddle_row_worker.py` (+ `paddle_row_parser.py`) on
  the player-table crops using PaddleOCR native line detection; emits per-player
  evidence with `sourceId=ocr.paddle.row`.
- Both reuse the shared Python process runner, evidence schema, and
  `EvidenceRecordMapper`. Both are enabled in `ocr.enabledEngines`.
- **Open:** not yet validated end-to-end on a real PDF; PaddleOCR call quality
  and the row parser need measuring on controlled samples.

## Reconciliation Status

- Shared `ProcessingResult` model; canonical team/player IDs and stat keys.
- `NormalizedOcrReconciler` is **weighted, not a blind majority vote**:
  - per-channel weights (`EngineWeightOptions`, configurable in
    `appsettings.json -> reconciliation.engineWeights`): full page weak on stat
    cells, crops stronger on the score, row OCR strongest on rows.
  - **math tiebreaker**: a conflicting field is overridden by the candidate that
    makes a basketball formula consistent (`points`, `fieldGoals.made/attempted`,
    `rebounds.total`), even a minority/lower-weighted channel.
- `OcrCandidate` carries channel provenance: sourceId, granularity,
  confidenceNormalized, mapperId, warnings.
- The pipeline no longer excludes the crop channel; all successful channels
  participate in reconciliation.

## Adobe Status

- Fully removed. `BasketPdfStats.Ocr.Adobe` project deleted. No Adobe API calls,
  dependencies, or config remain.

## GLM-OCR Status

- Fully removed from the active solution, flow, config, and scripts.

## UI / Runtime / Viewer Status

- Single PDF selection + one process action; already-processed popup; result
  viewer shows the reconciled result with fixed FIBA columns.
- Original PDFs are not moved; temporary `Working` copies are cleaned.
- `RuntimeFolderInitializer` creates only `Working`, `OutputJson`, `Logs`.

## Refactoring Progress (docs/refactor-plan.md)

- **Fase 0A** (baseline tecnica): completata.
- **Fase 0B** (documentazione + struttura Claude Code): completata.
- **Fase 1** (selezione data + caricamento partite): **completata 2026-06-18**.
  - DatePicker in `MainWindow.xaml` con data predefinita = oggi (binding `SelectedDateValue`).
  - `IOcrImportService.GetTodayMatchesAsync` rinominato in `GetMatchesForDateAsync(DateOnly date, …)`;
    endpoint invariato `GET /matches/today?date=YYYY-MM-DD`.
  - `MainViewModel`: `SelectedDate` (DateOnly, default oggi), ricaricamento al cambio data con
    `CancellationToken`, cancellazione della richiesta precedente, stale-response guard,
    `IsLoadingMatches`, azzeramento `SelectedMatchOption`/`MatchIdText`, gestione lista vuota/errore.
  - `OcrApiOptions.MatchLookupDate` marcato `[Obsolete]` (non controlla più il flusso; rimozione in Fase 9).
  - Test: `OcrImportServiceTests` (5) + `MainViewModelMatchLoadingTests` (13). Suite: 190 passed.
  - Nessuna modifica a Python, PHP, pipeline OCR, CH1–CH4, schema JSON, matching, backend ZIP.
- **Fase 2** (estrazione `AppComposition`): **completata 2026-06-18**.
  - Nuovo `src/BasketPdfStats.App/AppComposition.cs` (WPF-free): costruisce engine OCR, document
    preparation, pipeline, `OcrImportService`, `AlreadyProcessedPdfDetector`. Espone anche `FindRuntimeRoot()`.
  - `MainWindow.xaml.cs` ridotto a ~30 righe: solo bootstrap (root + settings), `AppComposition.Build`,
    e wiring degli adapter WPF (`WpfFilePicker`, `WinFormsProcessingResultPresenter`,
    `WpfAlreadyProcessedPdfDecisionService`, `WpfTeamMismatchConfirmationService`) nel `MainViewModel`.
    Non referenzia più `TesseractPythonOptions`/`PaddleCropOptions`/`EngineWeightOptions`.
  - Test: `AppCompositionTests` (4) + `AlreadyProcessedPdfSelectionServiceTests` aggiornati. Suite: 195 passed.
  - Nessun DI container. Nessuna modifica a Python, PHP, pipeline OCR, CH1–CH4, schema JSON, matching, backend ZIP.
- **Fase 3** (endpoint e modello del contesto partita): **completata 2026-06-18**.
  - PHP: `backend/endpoints/context.php` (`GET /matches/{matchId}/context`) + routing in `backend/index.php`.
    Query su tabelle reali verificate (`match_teams`/`teams`/`team_rosters`/`players`); supporta anche lo
    schema `matches.home_team_id`. Errori 404 (match assente), 409 (roster incompleto), 500.
  - C#: `OcrMatchContext`/`OcrContextTeam`/`OcrRosterPlayer`/`OcrMatchContextResult` in Core.Database;
    `GetMatchContextAsync(int matchId)` in `IOcrImportService` + `OcrImportService` (gestione timeout/cancellazione/404/roster vuoto).
  - `MainViewModel`: `SelectedMatchContext` caricato alla selezione partita (`LoadMatchContextAsync`),
    azzerato alla deselezione/cambio data; **ProcessButton disabilitato senza contesto** (rule #3: DB obbligatorio).
  - Test: `OcrImportServiceTests` (+3), `MainViewModelMatchLoadingTests` (+5). Suite: 203 passed.
  - ⚠ `php -l` non eseguibile localmente (PHP non installato): sintassi verificata manualmente.
  - La pipeline OCR **non usa ancora** il contesto (integrazione in Fase 5). Nessuna modifica a CH1–CH4, schema JSON, matching, backend ZIP.
- **Fase 4** (`PlayerIdentityMatcher`): **completata 2026-06-18**.
  - Nuove classi in `src/BasketPdfStats.Core/Identity/`: `NameNormalizer`, `PlayerMatchResult` (+enum `PlayerMatchKind`), `PlayerIdentityMatcher`.
  - Algoritmo: parse jersey (strip non-cifre e `*`) → giocatore roster con quel numero → distanza nome
    (Levenshtein normalizzata, ordini nome/cognome + match per sottoinsieme di token, variante OCR 0→o/1→i/5→s/rn→m).
    Soglie 0.15 (certo) / 0.30 (probabile), configurabili. Nome non leggibile + jersey univoco → ProbableMatch.
  - **Fase isolata**: il matcher NON è ancora collegato alla pipeline (integrazione in Fase 5). Non scrive dati se incerto.
  - Test: `PlayerIdentityMatcherTests` (13). Suite: 216 passed.
  - Nessuna modifica a Python, PHP, pipeline OCR, CH1–CH4, schema JSON, backend ZIP. `known_names.py` ancora attivo (sostituzione in Fase 5).
- **Fase 5** (passaggio del contesto canonico alla pipeline): **completata 2026-06-18**.
  - C#: `OcrProcessingRequest.MatchContext`; overload `IPdfProcessingPipeline.ProcessPdfAsync(path, selection, matchContext?, ct)`;
    `PdfProcessingPipeline` propaga il contesto nella request; `MainViewModel` passa `SelectedMatchContext`.
    `TesseractPythonOcrEngine` (CH1) scrive `roster-context.json` e passa `--roster-json` **solo se** MatchContext presente.
  - Python: nuovo `roster_context.py` (carica roster dinamico, stessa interfaccia di known_names, fallback legacy con WARNING);
    `worker.py` accetta `--roster-json` e attiva il roster; `parser.py` usa `roster_context` (5 siti) invece di import diretto da known_names.
    `known_names.py` **non modificato** (wrappato); rimozione definitiva in Fase 9. CH2–CH4 non ricevono `--roster-json`.
  - Backward-compatible: senza `--roster-json` comportamento identico a prima.
  - Test: C# +4 (`TesseractPythonOcrEngineTests`, `MultiEnginePipelineTests`) → suite 220 passed.
    Python: `py_compile` OK, test funzionale `roster_context` OK, import `parser`/`worker` nel venv reale OK.
  - ⚠ Esecuzione del worker su PDF reale NON eseguita (ambiente integration + Tesseract): validazione E2E in Fase 10.
  - Schema JSON finale invariato. Backend/PHP/backend ZIP invariati.
- **Fase 6** (`TeamIdentityMatcher` + inversione home/away): **completata 2026-06-18**.
  - Core: `NameSimilarity` (Levenshtein normalizzata), `TeamIdentityMatcher` (ordine corretto/invertito/mismatch, soglia 0.5),
    `SideInversion.Apply` (scambia Side, token Home/Away negli entityId di Teams/Players/Stats, punteggio finale e parziali; slot Game invariati).
  - `ReconciliationMetadata.SideInversionApplied: bool` (nuovo campo JSON, default false).
  - Pipeline: dopo riconciliazione, se `MatchContext` presente confronta squadre OCR (da CH1) vs DB:
    inversione → riallineamento automatico + flag + warning Info; mismatch → warning `team.mismatch` (non blocca).
    No-op senza contesto. I warning compaiono in `WarningText` via `BuildWarnings`; conferma continua/annulla resta al guard di import.
  - Test: +7 (`TeamIdentityMatcherTests` 4, `SideInversionTests` 1, `MultiEnginePipelineTests` 2). Suite: 227 passed.
  - Backend/PHP/backend ZIP invariati. CH1–CH4 invariati.
- **Fase 7A** (modello e raccolta delle revisioni): **completata 2026-06-18**.
  - Core: `IdentityReviewItem` (+enum `IdentityReviewReason`), `IdentityReviewBuilder` (usa `PlayerIdentityMatcher` in sola lettura).
  - `FileProcessingStatus.CompletedWithReviewRequired` (nuovo); `ProcessingResult.IdentityReview` (campo transitorio → Fase 8).
  - Pipeline: se `MatchContext` presente, popola `IdentityReview` confrontando giocatori OCR vs roster DB per lato
    (Conflict/NumberNotFound/Unmatched/ProbableMatch) e rileva i DB assenti dal PDF (NotInPdf).
    Stato = `CompletedWithReviewRequired` se ci sono Conflict o NotInPdf. Nessuna scrittura di canonicalPlayerId (è Fase 8).
  - `ProcessingResultPresentationService.ShouldPresent` include il nuovo stato (il viewer si apre comunque).
  - Test: +6 (`IdentityReviewBuilderTests` 5, `MultiEnginePipelineTests` 1). Suite: 233 passed.
  - Schema JSON: aggiunti `identityReview[]` e stato review-required (documentati in json-contract). UI di revisione = Fase 7B.
- **Fase 7B** (interfaccia di revisione manuale funzionante): **completata 2026-06-18**.
  - App: `IdentityReviewViewModel`/`IdentityReviewRowViewModel` (logica testabile), `Views/IdentityReviewWindow.xaml(.cs)` (UI WPF reale),
    `IIdentityReviewService` + `WpfIdentityReviewService`.
  - Funzionalità: candidati selezionabili solo dalla squadra corretta; conferma Conflict richiede un candidato;
    NotInPdf confermabile come assenza; export bloccato finché restano Conflict/NotInPdf aperti;
    ProbableMatch/NumberNotFound/Unmatched non bloccanti (auto-risolti per la sola visualizzazione).
  - `MainViewModel`: `ReviewIdentityCommand` apre la revisione; `ImportToDbCommand` bloccato su review-required finché non confermata.
    MainWindow espone il bottone "Revisiona"; AppComposition/MainWindow iniettano `WpfIdentityReviewService`.
  - Test: +7 (`IdentityReviewViewModelTests` 5, `MainViewModelReviewTests` 2). Suite: 240 passed.
  - Nessuna modifica a Python, PHP, CH1–CH4, schema JSON finale, backend ZIP.
- **Fase 8** (risultato canonico + ImportPayload + import.php): **completata 2026-06-18** (schema approvato dall'utente: "Solo ImportPayload").
  - Core: `ImportPayload`/`ImportTeam`/`ImportPlayer` (Models); `ImportPayloadBuilder` (Identity) risolve playerId via matcher + override manuali revisione.
  - `IdentityReviewItem.OcrEntityId` aggiunto; `IIdentityReviewService.ReviewAndConfirm` ritorna la mappa risoluzioni (entityId→playerId) o null.
  - `IOcrImportService.ImportAsync` invia `ImportPayload` (non più ProcessingResult); `MainViewModel` costruisce il payload con gli override di revisione.
  - PHP `import.php`: §3/§4 usano i playerId canonici dal payload e li verificano sul roster (HTTP **422** con lista `invalid`); nessun fuzzy matching. §5–§7 (stats + UPSERT) invariati.
  - JSON locale/diagnostico e snapshot di regressione **invariati**. `samples/test_import_match1.json` resta come riferimento layout (non fixture).
  - Test: +6 (`ImportPayloadBuilderTests` 4, `OcrImportServiceTests` import 2). Suite: 246 passed.
  - ⚠ `php -l` non eseguibile localmente: sintassi import.php da validare prima del deploy (Fase 8B).
- **Fase 8B** (pacchetto backend per Aruba): **completata 2026-06-19**.
  - `tools/build-backend-package.ps1`: genera `release/backend.zip` (voci `/`, no segreti) + `release/backend-manifest.txt` (timestamp, commit, SHA-256, endpoint, file, stato php -l).
  - Controlli: presenza file, route index.php, marker Fase 8 in import.php, scansione segreti (pulita). NON tocca `backend/backend.zip`.
  - `backend/config/database.example.php` (placeholder) creato e incluso; `config/database.php` reale escluso.
  - `docs/backend-deploy-checklist.md`: checklist deploy + rollback Aruba. `release/backend.zip` in `.gitignore`.
  - `php -l` su tutti i file PHP: **eseguito con esito positivo (2026-06-19)**; manifest aggiornato (`php -l: OK`), SHA-256 invariato. Claude non pubblica su Aruba.
- **Fase 9** (pulizia legacy approvata, per-item): **in corso**.
  - [x] `pdf_crop_runner/prepare_crops.py` rimosso (2026-06-19, approvato): 0 import verificati; README aggiornato. Build+test verdi.
  - [ ] `MatchLookupDate`: non approvato ora (resta `[Obsolete]`).
  - [ ] `known_names.py` + fallback `roster_context.py`: rinviati a dopo validazione Fase 10 su PDF reali.
  - [ ] `backend/backend.zip`: rinviato a dopo deploy verificato di `release/backend.zip` su Aruba.
- **Fase 10** (validazione end-to-end + benchmark CH3/CH4): **strumenti pronti (2026-06-19), esecuzione locale a carico utente**.
  - `tests/.../ChannelValidationHarnessTests.cs` (`[Integration]`): pipeline reale su samples/pdf/ via `AppComposition.Build`,
    report per-canale (copertura/accordi/conflitti/tempi) da `OcrRuns` + `Stats[].Reconciliation.ProviderValues`. Esce senza fallire se OCR assente.
  - `tools/run-channel-validation.ps1` (wrapper) + `docs/ch3-ch4-validation.md` (procedura, lettura, tuning pesi, gate Fase 9).
  - Output: `TestResults/ChannelValidation/channel-validation-report.{md,json}`.
  - Build verde, 246 test non-integration (harness escluso perché Integration).
  - ⚠ Esecuzione reale + tuning pesi + validazione roster dinamico (con MatchContext) restano da fare in locale.
- **Refactoring**: tutte le fasi 0A–8B implementate; Fase 9 (per-item) e Fase 10 (validazione/tuning) in mano all'utente per le parti che richiedono ambiente OCR / deploy Aruba.

## Risky or Unfinished Areas

1. PaddleOCR channels (3, 4) are implemented but not yet validated end-to-end on
   a real PDF; the PaddleOCR call quality and row parser need measuring.
2. teamTotals / per-row numeric extraction relies on PaddleOCR being reliable;
   Tesseract crops alone are not enough on small cells.
3. Some template zones still need coordinate fixes (officials crop blank;
   interval/period crops capture only one side; bottom comparative grabs legend).
4. Strict post-reconciliation validation and explicit derivation service can be
   strengthened.
5. Runtime output is not yet split into normal vs diagnostic mode; diagnostic
   folders are still created by default.

## Next Suggested Task

Validate Channels 3 and 4 end-to-end on a controlled real PDF: run the full
pipeline (calibration → crops → PaddleOCR → `evidence_records.json` →
reconciliation → final JSON), then measure crop/row OCR quality and tune
`reconciliation.engineWeights` accordingly.

## Avoid Touching

Unless the current task explicitly requires it:

- final JSON schema;
- mathematical validation formulas;
- known-name / player-identity logic;
- dependency versions;
- real PDFs and generated runtime artifacts.
