# Project Context

## Product Scope

BasketPdfStats is a local desktop converter for one image-based FIBA Live Stats
boxscore PDF at a time. The stable PDF layout should be exploited rather than
treated as arbitrary free-form OCR input.

Primary goal: produce one correct, validated JSON file for one game.

## User Workflow

```text
Upload PDF
  -> show selected PDF path
  -> if already processed, ask whether to reprocess
  -> Elabora
  -> save one final JSON
  -> display final extracted result
```

Rules:

- no batch processing;
- no input-folder processing;
- no automatic folder scan;
- no multi-file queue;
- never move the original uploaded PDF;
- working copies are temporary and must be cleaned.

Already-processed detection may use reliable existing evidence such as a matching
document hash, `processed-index.json`, an existing final JSON, or legacy history.
The UI decides whether to reprocess. OCR, mapping, validation, reconciliation, and
JSON writing must not silently skip a PDF.

## Target Architecture

```text
one selected PDF
  -> TesseractFullPage
       -> normalized baseline ProcessingResult
       -> OCR word boxes / geometry
  -> LayoutCalibration
       -> fixed pdf-structure contract
       -> rule-based geometry anchors
       -> corrected runtime crops or fail closed
  -> TesseractLayoutCrops
  -> PaddleLayoutCrops
  -> PaddleTableRows
  -> channel-specific raw parsing
  -> normalized ProcessingResult per channel
  -> light validation per normalized channel
  -> reconciliation across normalized channels
  -> strict final validation
  -> explicit safe derivation when needed
  -> final validation after derivation
  -> one final JSON
  -> result viewer
```

The ensemble is local OCR only: Tesseract and PaddleOCR. Do not compare raw OCR
outputs directly. Do not let one channel silently replace another.

## Provider Policy

### Tesseract

`TesseractFullPage` and `TesseractLayoutCrops` are local Tesseract channels. They
share rendering, preprocessing, canonical mapping, and infrastructure where
possible while remaining meaningfully different OCR readings.

`TesseractFullPage` is the active baseline. It runs OCR on the full rendered PDF
page and produces one normalized `ProcessingResult`. It does not consume macro
crops, semantic crops, table rows, or cells. It also exports OCR word boxes for
layout calibration. Only geometry may flow from `TesseractFullPage` into
`LayoutCalibration`; extracted game values must not flow into crop OCR.

`TesseractLayoutCrops` consumes the 15 calibrated generic Tier 1 and Tier 2
layout crops, OCRs each complete crop, writes its own raw artifact, and produces
a conservative partial normalized `ProcessingResult`. It never copies scores,
teams, players, or stats from `TesseractFullPage`. It does not split player tables
into rows or cells; that is the PaddleOCR row channel's job.

### PaddleOCR

PaddleOCR runs in the dedicated `.venv-paddle` environment and provides two
channels that consume the same calibrated crops:

- `PaddleLayoutCrops` (`ocr.paddle.crop`): OCRs the calibrated zone crops with
  PaddleOCR, producing the same raw schema as the Tesseract crop channel. It is
  expected to read the small numeric cells (teamTotals) that Tesseract fails on.
- `PaddleTableRows` (`ocr.paddle.row`): reads the player-table crops with
  PaddleOCR native line detection and parses each player row into per-player
  evidence.

Both emit the shared `evidence_records.json`, mapped by `EvidenceRecordMapper`.
Neither copies values from another channel.

### GLM-OCR and external API providers

GLM-OCR and Adobe are removed from the active project direction. Do not add GLM
or Adobe UI, API calls, local inference, model dependencies, or new paths. The
ensemble is local OCR only.

### Generic Layout Crops

`pdf_crop_runner` is a lightweight crop-only helper. Its active
`calibrate_layout` command consumes the canonical page image and word boxes
exported by `TesseractFullPage`, starts from the fixed structural contract under
`pdf-structure/`, and uses rule-based anchors only to align known zones at
runtime. It corrects offset and scale when evidence is reliable, creates
operational PNG crops, and writes one calibrated `crop-metadata.json`. It does
not infer the whole layout from scratch, perform OCR, or affect the final JSON.

Stable FIBA regions live under `pdf-structure/`:

- Tier 1: macro crops for broad regions;
- Tier 2: semantic crops for headers, tables, totals, and comparative stats;
- Tier 3: future row crops, documented but not implemented.

`pdf-structure/` is the structural source of truth. It defines the 15 layout
zones, crop roles, Home-then-Away team-table order, fixed player-table column
order, known headers, and canonical stat mappings.

Table crops must use known structure rather than a fixed player-row count:

- the column-header row is the top anchor and stays inside the crop;
- the Home table appears first and the Away table second;
- visible player rows may vary;
- `Squadra/Allenatore` and `Totali` are the final two structural rows and strong
  bottom anchors.

The final-score crop starts from the known header zone and uses team-name plus
score geometry anchors. Period scores and interval scores must not be mistaken
for the final-score anchor.

Calibration fails closed when required anchors are missing, ambiguous, or
misaligned. The report must explain the failure, bad crops must not be generated
silently, and `TesseractLayoutCrops` must reject uncalibrated or failed crops.

## Normalized Output Contract

Every OCR strategy must produce the shared `ProcessingResult` model before
reconciliation. Preserve:

- canonical entity IDs;
- canonical stat keys;
- provider provenance;
- source/raw values where useful;
- confidence;
- warnings;
- failed validation rules.

Canonical team IDs:

```text
team:Home
team:Away
```

Canonical player IDs:

```text
player:<side>:jersey:<number>
player:<side>:name:<normalized-name>
player:<side>:row:<row-index>
```

Use jersey IDs only when safe. Use name fallback for missing or ambiguous jerseys.
Use row fallback only as a last resort.

For the same player:

```text
PlayerStats.EntityId == StatValue.EntityId
```

Provider-specific identifiers may survive only as provenance:

```text
SourceEntityId
SourceFieldId
RawValue
ProviderValues
```

Canonical player stat keys:

```text
minutes
fieldGoals.made
fieldGoals.attempted
fieldGoals.percentage
twoPoints.made
twoPoints.attempted
twoPoints.percentage
threePoints.made
threePoints.attempted
threePoints.percentage
freeThrows.made
freeThrows.attempted
freeThrows.percentage
rebounds.offensive
rebounds.defensive
rebounds.total
assists
turnovers
steals
blocks
fouls.committed
fouls.drawn
plusMinus
evaluation
points
```

Normalize legacy aliases such as `efficiency` to `evaluation`.

## JSON Schema Policy

Keep the current final JSON schema unless a focused schema redesign task is
explicitly requested. The single final JSON should preserve selected values,
quality status, warnings, provenance, provider values where useful, and derivation
metadata when applicable.

## Runtime Policy

Normal mode should minimize files:

```text
runtime/OutputJson/<final-json>
runtime/Working/        # temporary only
runtime/Logs/           # optional minimal logs
```

Do not move uploaded PDFs to `Elaborati` or `Errori`. Avoid raw OCR JSON,
provider reports, crops, preprocessed images, candidate artifacts, and
intermediate provider JSON by default. Diagnostic mode may write them under a
coherent `runtime/Diagnostics/` hierarchy.

## Result Viewer

Show final reconciled data only. Keep missing values visible as `-`. Do not flood
the main view with raw OCR output or provider comparisons. Player rows should use
fixed FIBA columns and join stats through canonical `EntityId`.

## Logging

Log concise lifecycle events: selected PDF, processing start, already-processed
decision, selected Tesseract strategies, strategy success/failure, reconciliation,
final validation status, JSON save, and viewer display. Never log credentials or
large raw OCR payloads.
