# AGENTS.md

## Start Here

Read this file first. Then read only the extra context documents needed for the
current task. Prefer targeted file inspection. Do not scan the whole repository,
load generated folders, or reread unrelated planning documents unless required.

## Project Goal

This is a local desktop application for one FIBA Live Stats boxscore PDF at a
time. The PDF is usually image-based and has a stable recurring layout. The app
must run local Tesseract OCR strategies, normalize their outputs into the same
model, reconcile only normalized values, apply mathematical and logical checks,
save one validated JSON file, and display the final extracted result. Correctness
of the final JSON is the first priority.

## Mandatory Rules

- Keep the workflow single-PDF: `Upload PDF -> Elabora -> final JSON -> viewer`.
- Never add batch processing, input-folder scanning, or a multi-file queue.
- Never move the selected original PDF. Temporary working copies are allowed and
  must be cleaned after processing.
- Keep exactly one final JSON output. Do not create provider-specific final JSON
  alternatives.
- Normalize every OCR strategy to the shared `ProcessingResult` model before
  reconciliation. Never compare raw OCR outputs directly.
- Preserve canonical entity IDs, canonical stat keys, provenance, warnings,
  confidence, and validation evidence.
- Make incremental, narrowly scoped changes. Keep the project functional after
  each step.
- Reuse shared Tesseract infrastructure. Do not create three duplicated OCR
  implementations.

## Do Not Do

- Do not reintroduce GLM-OCR UI, API calls, local runners, dependencies, or paths.
- Do not reintroduce Adobe PDF Extract API code, dependencies, or paths.
- Do not change the final JSON schema during unrelated work.
- Do not silently overwrite OCR values or hide conflicts.
- Do not add external API dependencies, process real PDFs, or run OCR-heavy
  tests unless explicitly requested.
- Do not log credentials or large raw OCR text.
- Do not perform broad refactors unless explicitly requested.

## Architecture Direction

The target OCR ensemble is local Tesseract and PaddleOCR only:

1. `TesseractFullPage`
2. `TesseractLayoutCrops`
3. `PaddleLayoutCrops` (Channel 3 — to implement)
4. `PaddleTableRows` (Channel 4 — to implement)

No external API providers (Adobe or others). Each channel must produce its own
normalized `ProcessingResult`. Reconciliation happens only after normalization.
See [TESSERACT_ENSEMBLE_PLAN.md](TESSERACT_ENSEMBLE_PLAN.md).

`pdf-structure/` is the structural source of truth for layout work.
`LayoutCalibration` starts from that fixed FIBA contract and uses full-page OCR
geometry only to align known zones. It must not rediscover the document layout
from scratch or copy extracted values between OCR strategies.

## Context Loading Rule

Load only the document relevant to the task:

- Architecture, workflow, canonical model: [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md)
- Migration sequence and checkpoints: [MIGRATION_PLAN.md](MIGRATION_PLAN.md)
- Current implementation state and known gaps: [TASK_STATUS.md](TASK_STATUS.md)
- Validation, identity, and derivation rules: [VALIDATION_RULES.md](VALIDATION_RULES.md)
- OCR strategy details: [TESSERACT_ENSEMBLE_PLAN.md](TESSERACT_ENSEMBLE_PLAN.md)
- Layout crop contract and FIBA regions: [pdf-structure/README.md](pdf-structure/README.md)

Inspect only the code paths relevant to the requested change. Ignore `bin`, `obj`,
generated runtime output, diagnostics, and sample PDFs unless the task explicitly
requires them.

## Testing Policy

- Use targeted tests during normal development.
- Do not run OCR-heavy tests or real-PDF processing unless requested.
- Run the full suite only when the user asks, at a major checkpoint, or before a
  commit suggestion.
- Report exactly which commands ran and whether the full suite ran.

### Environment Failure Policy

- Do not repeatedly retry tests against broken, missing, unauthorized, or
  misconfigured environments.
- If a command fails because of permissions, sandbox restrictions, a missing
  virtual environment, a broken interpreter, a missing dependency, an
  unavailable OCR runtime, or an unavailable local tool, stop after the first
  clear failure and report the blocker.
- Do not recreate, repair, or replace Python virtual environments unless the
  current task explicitly requires Python execution and no working environment
  is available. Do not create a new environment just to try again.
- Prefer already-working project commands and targeted tests. For routine
  documentation, configuration, and refactor tasks, use lightweight checks only.
- Do not run OCR-heavy, integration, or PDF-processing tests unless the
  current task explicitly requests them.
- If OCR or Python execution is optional and the environment is blocked, report
  the blocker instead of fixing the environment.

## Commit Policy

Do not commit unless the user explicitly requests a commit.

## Reporting

Report: problem addressed, files changed, concise change summary, tests added or
updated, commands executed and results, residual risks, useful manual verification
commands, whether the full suite ran, and whether a commit was made.
