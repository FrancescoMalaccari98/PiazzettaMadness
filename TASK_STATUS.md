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
- **Prossima fase:** Fase 3 (endpoint e modello del contesto partita) — in attesa di approvazione.

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
