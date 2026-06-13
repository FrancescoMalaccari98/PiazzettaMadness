# Migration Plan

## Principles

- Migrate incrementally.
- Keep the app functional after every step.
- Change one architectural concern at a time.
- Prefer targeted tests.
- Do not redesign the JSON schema during OCR or runtime cleanup.

## Completed Steps

1. Removed GLM-OCR from the active solution, composition root, configuration,
   UI, scripts, and active documentation.
2. Simplified the main UI to one selected PDF and one `Elabora PDF` action.
3. Removed active batch/input-folder processing APIs and UI.
4. Preserved the already-processed popup for the selected PDF.
5. Stopped moving the original uploaded PDF to `Elaborati` or `Errori`.
6. Ensured temporary `Working` copies are cleaned after processing.
7. Removed Adobe entirely: deleted the `BasketPdfStats.Ocr.Adobe` project, the
   `IOcrCandidateProvider` abstraction, candidate-provider infrastructure, and
   all Adobe config. The ensemble is local OCR only (Tesseract + PaddleOCR).
8. Renamed the historical crop-only helper to `pdf_crop_runner`.
9. Moved stable layout references to `pdf-structure/` and introduced Tier 1
   macro crops plus Tier 2 semantic crops.
10. Deleted the quarantined GLM C# project, obsolete GLM diagnostics, historical
    audit material, and an accidentally tracked broken Python environment.
11. Established `TesseractFullPage` as the complete explicit full-rendered-page
    OCR baseline: local Tesseract raw diagnostics, canonical normalized
    `ProcessingResult`, provider provenance, and unchanged final JSON schema.
12. Promoted the legacy crop pre-pass into an independent diagnostic
    `TesseractLayoutCrops` strategy. It OCRs 15 calibrated fixed-template crops,
    writes isolated raw diagnostics, and emits a conservative partial normalized
    `ProcessingResult` without delegating data extraction to `TesseractFullPage`.
13. Added `LayoutCalibration`: `TesseractFullPage` exports canonical page
    geometry and word boxes; the calibrator starts from the fixed
    `pdf-structure/` contract and uses that geometry only to align known zones.
14. Made calibration real: bounded **affine Y transform** (scale + offset) from
    least-squares on robust anchors (final-score line + `Squadra/Allenatore`
    rows). Zone crops became reliable; tolerances moved to
    `layout-calibration-rules.json`. Removed fragile geometric row crops.
15. Introduced the shared **evidence-records schema** (`evidence_record.py`) and
    mappers (`evidence_mapper.py`). All channels emit the same intermediate JSON.
16. Wired the C# side: `EvidenceRecord` model, `EvidenceRecordReader`,
    `EvidenceRecordMapper` (evidence -> `ProcessingResult`); extended
    `OcrCandidate` with channel provenance (sourceId, granularity,
    confidenceNormalized, mapperId, warnings).
17. Made the reconciler **weighted** (`EngineWeightOptions`, configurable) plus a
    **math tiebreaker** (a formula-consistent minority candidate can win). No
    blind majority vote.
18. Connected **Channel 2** (Tesseract crops) to the pipeline via evidence
    records; removed the diagnostic exclusion. Off by default, opt-in via
    `ocr.enabledEngines`. finalScore parsed reliably; teamTotals ratios parsed
    with a math self-check.
19. Verified **PaddleOCR** works in `.venv-paddle` (3.6.0). On the full
    player-table crop it reads ~225 lines at ~1.00 confidence — the basis for
    Channels 3 and 4.

## Next Recommended Steps

### Step 1: Remove All Non-Local Providers ✅

- Removed `BasketPdfStats.Ocr.Adobe` project entirely.
- Removed `IOcrCandidateProvider` abstraction and all candidate-provider
  infrastructure (`CandidateArtifactMapper`, pipeline candidate paths).
- `OcrRunSelection` now has `UseTesseract` and `UsePaddle` only.
- `appsettings.json` no longer contains Adobe sections.
- All tests updated; 174/174 pass.

Checkpoint: only local OCR engines (Tesseract, PaddleOCR) are present in the
solution. No external API provider can be invoked.

### Step 2: Simplify Runtime Output

- Add a clear normal-vs-diagnostic mode.
- In normal mode write only final JSON, temporary working files, and minimal logs.
- Stop creating unused diagnostics folders by default.
- Move legacy runtime folder support behind compatibility-only code where needed.

Checkpoint: normal processing leaves one final JSON and no stale working file.

### Step 3: Measure Layout Calibration, Then Extend `TesseractLayoutCrops`

- Keep the diagnostic strategy separate from the default UI flow.
- Inspect overlay-before, overlay-after, report, contact sheet, and corrected
  final-score crop on controlled samples.
- Implement and verify fail-closed behavior when required anchors are missing,
  ambiguous, or incompatible with the known FIBA structure.
- Add bounded anchor-derived offset/scale refinements only when anchor evidence
  justifies them; preserve the source template.
- Derive player-table bounds from the known header row plus
  `Squadra/Allenatore` and `Totali`, not from a fixed player-row count.
- Measure crop-level OCR quality on controlled samples.
- Extend only safe semantic mappings before row parsing.
- Do not merge it automatically with `TesseractFullPage` yet.

Checkpoint: the crop strategy remains independently inspectable and its partial
mapping is measured before automatic reconciliation. Failed calibration never
produces silently trusted crops.

### Step 4: Validate the PaddleOCR Channels (`PaddleLayoutCrops`, `PaddleTableRows`)

- Both Paddle channels are implemented (C# engines + Python workers) and enabled.
- Run the full pipeline end-to-end on a controlled real PDF and measure crop/row
  OCR quality.
- Parse the fixed FIBA table columns by expected type.
- Reject fake rows and map `Totali` to team totals.

Checkpoint: player-table extraction has independent row-based normalized output
validated on a real sample.

### Step 5: Reconcile the Four Local Channels

- Reconcile only normalized outputs.
- Preserve agreement, conflicts, provenance, warnings, and channel failures.
- Do not overtrust correlated agreement.
- Tune `reconciliation.engineWeights` once channel quality is measured.

Checkpoint: one final JSON is produced from available normalized channels.

### Step 6: Tighten Final Validation and Derivation

- Ensure strict final validation runs after reconciliation.
- Add explicit safe derivations in a dedicated service.
- Run final validation again after derivation.

Checkpoint: derived values are visible, traceable, and contradiction-free.

### Step 7: Improve Viewer and Logging

- Keep final viewer focused on reconciled data.
- Add concise lifecycle logging.
- Avoid raw diagnostic flood in the main view.

## Safe Checkpoints

At each checkpoint:

1. inspect only touched paths;
2. run targeted unit tests;
3. verify no OCR-heavy or real-PDF test ran unless explicitly requested;
4. run the full suite only at a major checkpoint or when requested;
5. report residual risk before starting the next step.

## Tasks That Must Not Be Combined

- JSON schema redesign with OCR strategy implementation.
- Runtime artifact cleanup with Tesseract parsing changes.
- PaddleOCR channel validation with reconciler weight changes.
- Player identity changes with unrelated viewer styling.
- Validation rule changes with reconciler refactors.
- Crop metadata changes with OCR behavior changes.
- Dependency upgrades with feature work unless required.
