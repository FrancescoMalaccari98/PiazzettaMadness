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
  - [x] `known_names.py` + fallback `roster_context.py` **rimossi (2026-06-26, approvazione esplicita)** dopo
    gate Fase 10E superato. Logica fuzzy generica spostata in `roster_context.py`; `parser.py` invariato;
    degradazione pulita senza roster (liste vuote/None). Aggiornati C#/worker/harness/README/regole/doc.
    Build verde, 246 test non-integration verdi, smoke test Python OK. Vedi voce Fase 9 dettaglio sotto.
  - [ ] `backend/backend.zip`: rinviato a dopo deploy verificato di `release/backend.zip` su Aruba.
- **Fase 10** (validazione end-to-end + benchmark CH3/CH4): **strumenti pronti (2026-06-19), esecuzione locale a carico utente**.
  - `tests/.../ChannelValidationHarnessTests.cs` (`[Integration]`): pipeline reale su samples/pdf/ via `AppComposition.Build`,
    report per-canale (copertura/accordi/conflitti/tempi) da `OcrRuns` + `Stats[].Reconciliation.ProviderValues`. Esce senza fallire se OCR assente.
  - `tools/run-channel-validation.ps1` (wrapper) + `docs/ch3-ch4-validation.md` (procedura, lettura, tuning pesi, gate Fase 9).
  - Output: `TestResults/ChannelValidation/channel-validation-report.{md,json}`.
  - Build verde, 246 test non-integration (harness escluso perché Integration).
  - ⚠ Esecuzione reale + tuning pesi + validazione roster dinamico (con MatchContext) restano da fare in locale.
  - **Config locale corretto (2026-06-22)**: `Config/appsettings.json` rigenerato dal portable ma con i percorsi reali dello sviluppo:
    CH1/CH2 python = `fiba_pdf_to_json_programma_v2\fiba_pdf_to_json\.venv\Scripts\python.exe`, CH3/CH4 = `.venv-paddle\Scripts\python.exe`,
    calibrazione = `.venv-crop\Scripts\python.exe`; `tesseractExecutableFolder`/`tessdataPrefix` vuoti (Tesseract dal PATH).
    Causa del fallimento precedente: il portable puntava a un bundle inesistente (`tools\python-tesseract`, `tools\python-paddle`, `tools\tesseract`).
    Pesi `engineWeights` e resto invariati; nessun segreto. `Config/appsettings.json` ignorato da git (riga .gitignore).
- **Fase 10C** (analisi risultati validazione): **completata 2026-06-22**.
  - Validazione eseguita in locale su 5 PDF (tutti i canali Success, stato CompletedWithWarnings).
  - Aggregato per provider: CH1 cov 2187/accordo 85.9% (309 conflitti, tutti suoi); CH2 cov 32/93.8%; CH3 cov 80/100%; CH4 cov 1816/99.8%.
    Tempi medi: CH1 ~11s, CH2 ~4.8s, CH3 ~6.5s, CH4 ~16s. 2331 campi riconciliati, 2743 warnings (~1.18/campo).
  - Harness esteso (test): aggregato per provider, tempi medi per canale, breakdown warnings per severità/categoria (per PDF + aggregato).
  - Analisi + caveat (accordo = vs reconciler, non ground truth) + proposta tuning **NON applicata** in `docs/ch3-ch4-validation.md`.
  - `Config/appsettings.json` e `reconciliation.engineWeights` invariati. Build+test verdi (246). `TestResults/` non committato.
  - Prossimo passo per il tuning: rilanciare l'harness per il breakdown `math.*` e mappare i FailedRules ai provider.
- **Fase 10D** (dettaglio warning matematici): **completata 2026-06-23**.
  - Harness `ChannelValidationHarnessTests` esteso: collega ogni warning `math.*` alle statistiche
    coinvolte (`Validation.Warnings[].FieldIds` → `Stats[].FieldId`) ed estrae per ognuno PDF, entità
    (giocatore/squadra + side), regola fallita, statistica, valore finale scelto, valori per provider,
    provider scelto/concordi, messaggio completo, severità, categoria, flag bloccante/diagnostico e
    collegamento a `FailedRules`. Nuovi record `MathWarningDetail`/`MathWarningStat`.
  - Report markdown/JSON: aggiunta sezione `Dettaglio warning matematici` per-PDF + aggregata
    (l'aggregato esistente per provider/tempi/severità è mantenuto invariato).
  - Harness `[Integration]` **eseguito in locale (2026-06-23, 4m19s, tutti i canali Success)**:
    confermati **4 warning `math.*` su 2331 campi riconciliati** (~0.17%), tutti **non bloccanti**
    (3 su `SEMIFINALE 1`, 1 su `SPR-DEN`; stato `CompletedWithWarnings`). Dettaglio:
    (1) `math.reboundsTotal` Info Miami Spritz 35 vs 29; (2) `math.pointsFormula` Warning Philadelphia
    28 vs 26 (gap tiri realizzati squadra); (3) `math.periodSum` Warning Away 28 vs Q1+Q2=26 (crop
    periodi parziali); (4) `math.reboundsTotal` Info Denver 2 vs 27 (probabile misread totale rimbalzi).
  - Tutti i casi sono su **totali squadra/periodi** (non righe giocatore, solide al 99.8%) e riconducibili
    a gap/ambiguità di estrazione, non a selezione errata del reconciler → **nessun tuning pesi raccomandato**.
  - Documentazione: `docs/ch3-ch4-validation.md` (sezione 10D: tassonomia regole, 4 casi concreti,
    valutazione bloccante/diagnostico, nessun tuning raccomandato) e `docs/refactor-plan.md` aggiornati.
  - `Config/appsettings.json` e `reconciliation.engineWeights` invariati. Build verde, 246 test
    non-integration verdi. `TestResults/` non committato.
- **Fase 10E** (validazione roster dinamico reale): **strumento pronto 2026-06-24, esecuzione locale a carico utente**.
  - Nuovo harness `tests/.../RosterContextValidationHarnessTests.cs` (`[Integration]`): carica
    `context.json` (fixture DB, superset di `OcrMatchContext`) e processa i PDF della stessa partita
    **passando l'`OcrMatchContext`** alla pipeline (a differenza di `ChannelValidationHarnessTests`,
    che gira senza contesto). Esce in SKIP con istruzioni se mancano fixture/PDF/ambiente OCR.
  - Report `TestResults/RosterContextValidation/roster-context-report.{md,json}`: context provided/source,
    matchId, home/away (+teamId), roster per squadra, PDF processati, **CH1 roster source**
    (`dynamic-context`/`legacy-known-names`/`none`/`unknown`, dedotto dai log stderr del worker CH1),
    fallback `known_names` usato, matching identità per lato (certain/probable/conflict/numberNotFound/
    unmatched/notInPdf), review required, **nessun `playerId` inventato da Python**, **matching per-squadra
    non combinato** (`crossTeamLeak=0`).
  - Dati privati: `samples/private/db-context/aurora-lynx-nebula-bears-08-lug-2030/{context.json, pdfs/}`.
    `.gitignore` aggiornato (`samples/private/`); fornito `context.template.json` (placeholder, niente dati inventati).
  - Doc dedicata `docs/roster-context-validation.md` (formato context.json, dati DB richiesti, gate
    rimozione `known_names.py`); `docs/ch3-ch4-validation.md` e `docs/refactor-plan.md` aggiornati.
  - **Eseguito in locale (2026-06-24, 3m47s, harness passato)** su matchId 63 Aurora Lynx (Home, teamId 46)
    vs Nebula Bears (Away, teamId 47), 2026-07-08 20:30 Girone A, roster 8+8, 5 PDF reali. `context.json`
    costruito dal dump DB reale (nessun dato inventato; PDF/context in `samples/private/`, ignorati).
  - Risultato su tutti e 5 i PDF: **CH1 roster source = dynamic-context**, **fallback known_names = no**
    (log worker: "Roster dinamico attivo: 2 squadre, 16 giocatori."), review required = no, **playerId
    inventato = no**, matching **8/8 certain per lato** (80/80 aggregato), 0 conflict/probable/numberNotFound/
    unmatched/notInPdf, **crossTeamLeak = 0**. Report in `TestResults/RosterContextValidation/` (non committato).
  - **Gate rimozione `known_names.py`: 4 criteri soddisfatti** su questa partita (poi rimosso in Fase 9).
- **Fase 9 — rimozione `known_names.py`** (completata 2026-06-26, approvazione esplicita utente):
  - Eliminato `fiba_pdf_to_json/known_names.py` (8 squadre + 64 giocatori hardcoded, dati personali reali).
  - `roster_context.py`: rimosso `from . import known_names as _legacy` e tutto il fallback legacy
    (`_warn_legacy_once`, `_warned_legacy`); la logica fuzzy generica (`fuzzy_known_name`/`_norm`) è stata
    **spostata** qui (non persa). Senza roster attivo: `known_teams()/known_players()/...` → `[]`/`None`,
    il parser procede senza correzione nomi/numeri (degradazione pulita; identità risolte in C#).
  - `parser.py` invariato (già su `roster_context`). Aggiornati commento `TesseractPythonOcrEngine.cs`,
    help `worker.py`, harness (`legacy-known-names`→`roster-not-active`, `FallbackUsed`→`RosterInactive`),
    README worker, `.claude/rules/python-worker.md`, `.claude/skills/fix-extraction-issue`,
    `database/05-import-contract.md`, `docs/legacy-code-inventory.md`, `docs/refactor-plan.md`.
  - **Non toccati** `MatchLookupDate` e `backend/backend.zip` (altri item Fase 9, gating separato).
  - Verifiche: build verde, 246 test non-integration verdi, smoke test Python (degradazione + roster attivo) OK.
    Nessuna modifica a schema JSON, `engineWeights`, `Config/appsettings.json`, backend PHP. Nessun commit.
- **Bugfix — PDF↔match mismatch (2026-06-26):** se le squadre lette dal PDF non corrispondono al match
  selezionato, import e revisione sono **bloccati** dopo l'OCR (prima nessun controllo lo impediva).
  - Controllo in `MainViewModel.DetectTeamMismatch(result)`, chiamato in `AddResult` dopo l'OCR: usa
    `TeamIdentityMatcher.Match(homeOcr, awayOcr, SelectedMatchContext)` (squadre `result.Teams` vs match scelto).
  - Outcome `Mismatch` → `_teamMismatchMessage` valorizzato → `CanImport`/`CanReviewIdentity` = false,
    badge "PDF non corrispondente", messaggio chiaro (match selezionato vs PDF rilevato), nessun payload,
    nessuna chiamata `ImportAsync`. `CorrectOrder`/`Inverted` → OK (inversione side già gestita).
  - Falsi positivi evitati: se l'OCR non legge alcun nome squadra non si afferma un mismatch (resta la
    guardia all'import preesistente). Nessuna modifica a OCR Python, schema JSON, backend.
  - **Blocco anticipato in pipeline (dopo CH1) con popup:** nuovo `ITeamMismatchProcessingDecider`
    (Core) iniettato in `PdfProcessingPipeline`. Dopo CH1, se le squadre lette non corrispondono al
    match, mostra una popup WPF (`WpfTeamMismatchProcessingDecider`, wired in `AppComposition.Build` ←
    `MainWindow`): "interrompi (consigliato)" → salta CH2–CH4 (no OCR inutile su PDF sbagliato) +
    warning `ocr.processingAbortedByUser`; "continua" → completa l'OCR. In **entrambi** i casi l'import
    resta bloccato. `OcrProcessingRequest.TeamMismatchAborted` (transitorio). Senza decider iniettato
    comportamento invariato (tests/CLI).
  - Test: `MainViewModelTeamMismatchTests` (5 casi UI) + 3 casi pipeline in `MultiEnginePipelineTests`
    (abort salta canali / continue li esegue / match coerente non chiede). Build verde, 261 test non-integration verdi.
- **Ottimizzazione finale — riduzione rumore warning / JSON pulito (2026-06-26):** in modalità normale
  (nessun flag/DiagnosticMode) i warning OCR diagnostici non sporcano più UI/JSON.
  - **Causa**: il reconciler genera ~2735 `ocr.reconciliation.*` (conflict/missingProvider/objectConflict/
    mathTiebreak/pointsFromPlayerSum) in `Validation.Warnings` e per-stat, + `candidates`/`reconciliation.
    providerValues` su ogni stat; la UI mostrava anche lo stderr dei run OCR riusciti.
  - **Pulizia al confine pipeline** (`PdfProcessingPipeline.CleanupDiagnostics`): rimuove da
    `Validation.Warnings` e `stat.Warnings` tutto ciò che NON è importante. **Mantenuti**: severità `Error`,
    `math.*`, `team.*` (mismatch/inversione), `ocr.processingAbortedByUser`. **Rimossi**: tutti gli altri
    `ocr.*` diagnostici. Stato stat ricalcolato dopo la pulizia. Il reconciler è **invariato** (i suoi unit
    test verificano la logica in-memory).
  - **JSON pulito**: `StatValue.Candidates` e `StatValue.Reconciliation` marcati `[JsonIgnore]` (restano
    in-memory per math tiebreak/test, esclusi dal JSON e dal payload import → niente providerValues).
  - **UI**: `BuildWarnings` non mostra più lo stderr dei run riusciti (solo Failed/Timeout) né i duplicati
    per-stat; nuovo `ProcessingSummary` (es. "Elaborazione completata. / 0 errori bloccanti. / 0 revisioni
    richieste. / 1 avvisi matematici.") mostrato nel passo 3.
  - **Import protetto**: `CanImport` ora richiede anche `!HasBlockingError()` (status Failed/NotValidato o
    warning `Error`) oltre a review risolta e nessun mismatch PDF↔match. Solo warning `math.*` (non bloccanti)
    → import consentito. Payload sempre da dati finali.
  - **Performance**: l'OCR domina il tempo (Fase 10C: ~16s CH1, ~6s CH2, ~12s CH3, ~23s CH4 per PDF);
    warning/reconciliation/JSON/UI sono millisecondi. Le modifiche alleggeriscono JSON/UI/memoria ma non il
    tempo OCR. Proposte OCR (non applicate): worker Paddle unico crop+row per non caricare 2× il modello,
    parallelizzare CH2–CH4 dopo i crop, valutare scala/DPI CH1. Il blocco anticipato su mismatch (già
    implementato) risparmia CH2–CH4 sui PDF sbagliati.
  - Test: +2 pipeline (`Pipeline_removes_ocr_diagnostic_warnings_in_normal_mode`,
    `Pipeline_keeps_math_warning_and_emits_clean_json`) + 4 ViewModel (`MainViewModelNoiseTests`).
    Build verde, 267 test non-integration verdi. Nessuna modifica a OCR Python, `engineWeights`, backend, appsettings.
- **Fase UI — Vista "Risultati Telecronaca" (2026-06-26):** dopo l'elaborazione l'app mostra
  automaticamente una vista da commentatore (non tecnica OCR).
  - `TabControl` in `MainWindow.xaml`: tab **Elaborazione** (flusso esistente data→match→PDF→elabora→
    revisiona/importa, invariato) + tab **Risultati Telecronaca**; dopo `Elabora` passa al tab 2
    (`MainViewModel.SelectedTabIndex`).
  - `TelecronacaViewModel` (POCO, linkato nei test, no WPF): header partita (squadre, punteggio grande,
    matchId, data/ora, fase, banner stato), due squadre affiancate con **DataGrid** standard (sortable),
    tabella compatta `# | Giocatore | PTS | 2PT(%) | 3PT(%) | FT(%) | REB | AST | STL | FAL` ("-" se mancante),
    dettaglio giocatore al click (minuti, 2/3/TL made-att-%, rimbalzi off/dif/tot, assist, recuperi, stoppate,
    palle perse, falli fatti/subiti, +/-, valutazione), **top player** (top scorer/valutazione/rimbalzi/assist)
    e **spunti telecronaca** (doppie cifre, % da tre squadra, miglior valutazione). Solo dati finali
    riconciliati (niente provider/CH/diagnostica).
  - Ordinamento default: numero maglia crescente, **N.E./senza statistiche in fondo** (grigi, "-", label N.E.).
  - **Review required** → banner arancione; **PDF/match mismatch** → banner rosso e **tabelle nascoste**
    (niente vista fuorviante); import resta disabilitato (`CanImport` invariato). `OcrMatchContext`/flusso intatti.
  - Stili sobri in `App.xaml` (`PlayerGrid`, card, colori neutri); nessuna libreria esterna. Test
    `TelecronacaViewModelTests` (9 casi). `TelecronacaViewModel.cs` linkato in `BasketPdfStats.Tests.csproj`.
    Build verde, 276 test non-integration verdi.
- **Fase UI — Correzioni layout risultati e flusso (2026-06-26):**
  - **Niente popup** post-elaborazione: `ProcessingResultPresentationService.TryPresent` non più chiamato
    (campo rimosso); i risultati restano nell'interfaccia. Test con spy presenter (`ShowCount==0`).
  - **3 schede** a destra (`TabControl`): **Elaborazione** (compatta: riepilogo neutro, lista File/Stato/JSON,
    JSON path, warning importanti), **Risultati Telecronaca**, **PDF completo** (percorso + `Apri PDF` con
    app predefinita via `OpenPdfCommand`; nessun viewer embedded/libreria esterna).
  - **Colonna step sempre visibile a sinistra** (allargata 440→480): 1 Seleziona partita, 2 Seleziona PDF,
    3 Elabora (prima erano dentro la scheda Elaborazione).
  - **Importa nel DB sempre visibile in alto** (header) prima dello stato; + Revisiona. Disabilitati quando
    non applicabili ma sempre visibili.
  - **Niente badge verde "Completato"**: header mostra `HeaderStatus` solo per stati importanti
    (Revisione richiesta / Non importabile / PDF non corrispondente); banner telecronaca solo se
    `HasImportantStatus` (Review/Error).
  - **Telecronaca**: scoreboard compatto (una riga), top/spunti compatti, **tabelle giocatori area
    principale** (`*`), **dettaglio giocatore ampio** (riga con MinHeight 180, font 13–15).
  - Auto-tab: dopo elaborazione valida → Telecronaca (1); su mismatch → Elaborazione (0) con banner errore.
  - File: `MainWindow.xaml`, `MainViewModel.cs` (`OpenPdfCommand`, `HeaderStatus`, no popup),
    `TelecronacaViewModel.cs` (`HasImportantStatus`). Test `MainViewModelLayoutTests` (7). Build verde,
    283 test non-integration verdi.
- **Fase UI — Revisione layout v2 (2026-06-26):** schede in alto + 4 viste.
  - **Tab in alto** (header, tra titolo e pulsanti) via RadioButton+`IndexToBoolean`/`IndexToVisibility`
    converter; `SelectedTabIndex` pilota un solo pannello visibile alla volta. 4 schede:
    **Elaborazione** (step 1–2–3 a sinistra + 4 "Revisione e import" a destra con stato/JSON/warning;
    i pulsanti Revisiona/Importa restano **solo in alto**, sempre visibili), **Risultati giocatore**
    (ex Telecronaca, a tutta larghezza), **Risultati squadre** (nuova: confronto stat squadra Casa/Ospite),
    **PDF completo** (Apri PDF + riassunto schematico di tutto l'estratto da `ProcessingResultViewModel`).
  - **Risultati giocatore**: scoreboard compatto, tabelle area principale con **riga Totali squadra
    evidenziata** (sfondo accent) sotto ogni lista, **dettaglio giocatore ampio** (riga MinHeight 170).
  - Nessuna popup post-elaborazione (confermato dai test); auto-tab: valido→Risultati giocatore (1),
    mismatch→Elaborazione (0). Nessun badge verde "Completato".
  - VM: `TelecronacaViewModel` (`HomeTotals`/`AwayTotals`, `TeamComparison`, `HasImportantStatus`),
    `MainViewModel` (`FullResult`, `OpenPdfCommand`, `HeaderStatus`). Converter `IndexToVisibility`/
    `IndexToBoolean`. Test `MainViewModelLayoutTests` + `TelecronacaViewModelTests` aggiornati.
    Build verde, 286 test non-integration verdi.
- **Fase UI — Rifiniture v3 (2026-06-26):**
  - **Riga Totali parte della tabella**: i DataGrid giocatori bindano `HomeRows`/`AwayRows` = giocatori +
    riga vuota + riga **Totali** (evidenziata, grassetto, non selezionabile). Sort colonne disattivato per
    tenere i totali in fondo. `HomePlayers`/`AwayPlayers` restano puri (calcoli/test).
  - **Dettaglio giocatore a tabella**: `TelecronacaPlayer.DetailPairs` (8 righe da 2 coppie etichetta/valore)
    reso in un DataGrid; pannello più grande e in risalto.
  - **Risultati squadre**: tabella ridimensionata (larghezza fissa, valori a destra in grassetto, gridlines)
    e ora include **tutti** i campi squadra estratti dal PDF (righe curate + qualunque altra chiave team-scope
    via `ProcessingResultViewModel.Labels`).
  - **Fix avvio**: in `App.xaml` lo stile `TabRadio` usava `{StaticResource AccentBrushDark}` prima della sua
    definizione (forward reference) → crash a runtime; spostato dopo i brush.
  - Build verde, 290 test non-integration verdi (avvio app verificato).
- **Fase UI — Rifiniture v4 (2026-06-26):**
  - **Statistiche comparative** (`comparative.*`: punti da palle perse, in area, secondi tiri, contropiede,
    fast break da palle perse, panchina, massimo vantaggio/parziale, points per possession, cambi di guida,
    parità, tempo in vantaggio) ora **visibili in "Risultati squadre"** oltre ai totali squadra
    (`TelecronacaViewModel.BuildComparatives`). Estratte dall'OCR (scope Comparative), prima non mostrate.
  - **No scroll**: rimossi gli ScrollViewer di pagina (scheda giocatore, PDF, colonna step Elaborazione);
    layout a fit. "Risultati squadre" su **due colonne affiancate** (`TeamComparisonLeft`/`Right`) per stare
    in una schermata. (I DataGrid mantengono lo scroll interno solo come fallback per dati eccezionali.)
  - 4 test aggiunti/aggiornati. 290+ test non-integration verdi.
- **Refactoring**: tutte le fasi 0A–8B implementate; Fase 9 (per-item: `prepare_crops.py` + `known_names.py`
  rimossi; `MatchLookupDate`/`backend.zip` rinviati) e Fase 10 (validazione/tuning) in mano all'utente per le
  parti che richiedono ambiente OCR / deploy Aruba.
- **Fase UI + Portable** (2026-06-26): UI WPF ridisegnata a step + script pacchetto portable.
  - UI: `MainWindow.xaml` riorganizzata in 4 passi guidati (1. Seleziona partita / 2. Seleziona PDF /
    3. Risultato / 4. Revisione e import), 2 colonne, badge di stato generale, usabile a 1280×800.
    Stili sobri solo WPF in `App.xaml` (nessuna libreria esterna). `MainWindow.xaml.cs` invariato.
  - ViewModel: aggiunte SOLO proprietà calcolate (`WorkflowStatus`, `ProcessHint`, `HasSelectedMatch`,
    `SelectedMatchTeams/Time/Phase`, `HasSelectedPdf`, `IsContextReady`) + notifiche; logica e comandi
    invariati. `OcrMatchContext` resta collegato a `SelectedMatchOption → LoadMatchContextAsync`.
  - Match ID reso read-only nella UI (alimentato dalla selezione partita, non digitato).
  - Pulsanti: "Elabora PDF" abilitato solo con match+PDF e nessuna elaborazione in corso; "Revisiona"
    solo se serve review; "Importa nel DB" solo se risultato valido e review risolta (logica command preesistente).
  - Portable: `tools/build-app-portable.ps1` (param `-SelfContained` default true, `-Zip`): `dotnet publish`
    win-x64 → `release/app-portable/` + `Config/appsettings.portable.json` come template SENZA segreti +
    `README-LANCIO.txt`; guardia anti-token; zip opzionale `release/BasketPdfStats-portable.zip`.
    `.gitignore` aggiornato (`release/app-portable/`, `release/*.zip`).
  - **Non eseguito da Claude**: `dotnet publish` è in deny list di sicurezza del progetto e PowerShell ha
    deny rule → lo script va lanciato dall'utente (`.\tools\build-app-portable.ps1`). Build verde,
    253 test non-integration verdi (246 + 7 nuovi `MainViewModelUiStateTests`).
- **Ottimizzazione — parallelizzazione CH2–CH4 (2026-07-09):** in `PdfProcessingPipeline.RunEnginesAsync`
  i motori secondari (CH2 Tesseract crop, CH3 Paddle crop, CH4 Paddle row) vengono ora eseguiti **in
  parallelo** via `Task.WhenAll` invece del `foreach` sequenziale. CH1 (obbligatorio, esporta la geometria
  word-box per la calibrazione) e la calibrazione layout restano **prima** e bloccanti; il blocco anticipato
  su team mismatch resta invariato (salta CH2–CH4). Sicurezza verificata: ogni canale scrive su un path
  unico basato sul nome del motore (`NormalizedOcrResultWriter` stateless), `OcrProcessingRequest` è
  read-only durante CH2–CH4, `RunEngineAsync` cattura tutte le eccezioni internamente (nessuna propaga con
  `Task.WhenAll`). `Task.WhenAll` preserva l'ordine dei motori → lista `results` deterministica (CH1, CH2,
  CH3, CH4) → nessun impatto su riconciliazione/snapshot. Attesa la corsa critica CH4 (~16–23s) invece della
  somma sequenziale CH2+CH3+CH4. Nessuna modifica ai test (verificano `WasCalled`, non l'ordine); nessuna
  modifica a schema JSON, `engineWeights`, worker Python, backend. Build verde, 68 test non-integration
  mirati (Tesseract/Pipeline/Preparation/Reconciler) verdi; la regressione `Integration` rossa
  (`raw warnings were not preserved`) è **preesistente** (fallisce identica sul baseline, pipeline solo-CH1).

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
