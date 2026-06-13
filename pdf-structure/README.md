# PDF Structure

This folder describes the fixed structure of the FIBA Live Stats PDF layout used by BasketPdfStats.

It contains stable layout references, crop definitions, table structure documentation, and machine-readable column metadata.

This folder must not contain generated OCR output, generated crops, final JSON files, runtime logs, or temporary artifacts.

## Structural Source Of Truth

This folder is the primary structural contract for `LayoutCalibration`.
Calibration must start from the known FIBA layout documented here. It must not
derive the whole document layout dynamically from OCR.

The contract defines:

- exactly 15 active Tier 1 and Tier 2 crop zones;
- macro and semantic crop roles;
- the Home table before the Away table;
- fixed player-table column order and headers;
- canonical stat mappings;
- `Squadra/Allenatore` and `Totali` as the final two structural rows of each
  team table.
- calibration anchors, bounded fallbacks, and per-crop usability rules.

## Files

| File | Purpose | Used by code |
|---|---|---|
| `layout-map.default.json` | Defines operational crop zones using normalized coordinates | Yes |
| `layout-calibration-rules.json` | Defines anchor rules, fallbacks, and crop usability policy | Yes |
| `crop-zones.md` | Human documentation of crop zones | No |
| `table-boundary-rules.md` | Human explanation of player-table boundary calibration | No |
| `player-table-structure.md` | Human documentation of the fixed player table | No |
| `player-table-columns.json` | Machine-readable player table columns and expected types | Future parser |
| `crop-metadata.schema.json` | Schema for generated `crop-metadata.json` | Optional validation |
| `reference-images/` | Human visual references for crop zones | No |

## Runtime Output

Generated crops are written under:

```text
runtime/Dataset/LayoutCrops/<document-name>.<short-hash>/
runtime/Dataset/LayoutImages/<document-name>.<short-hash>/
runtime/Dataset/LayoutDebug/<document-name>.<short-hash>/
```

Generated crop metadata is written as:

```text
crop-metadata.json
```

The active runtime path is `python -m pdf_crop_runner.calibrate_layout`.
`TesseractFullPage` first exports a canonical page image and OCR word boxes.
`LayoutCalibration` uses only that geometry to align the fixed normalized
template with rule-based anchors, then writes corrected crops, overlays, a
calibration report, and a contact sheet. It does not copy extracted game data
into crop OCR.

Calibration is fail-closed per crop. Bounded structural fallbacks are allowed
only with warnings and recorded evidence. If a crop cannot be calibrated
safely, it must be marked unusable. `TesseractLayoutCrops` may consume only
generated crop entries explicitly marked usable.

Generated files must not be committed.

## Current Crop Model

The current layout has two operational crop tiers:

1. 4 macro zones;
2. 11 semantic zones.

Row-by-row and cell-by-cell table crops are future work.

## Active Crop Inventory

The active layout contains exactly 15 crops. Crop IDs and filenames are:

| File | Crop ID |
|---|---|
| `001-macro.header.png` | `macro.header` |
| `002-macro.periodIntervalScores.png` | `macro.periodIntervalScores` |
| `003-macro.mainTables.png` | `macro.mainTables` |
| `004-macro.bottomComparative.png` | `macro.bottomComparative` |
| `101-header.full.png` | `header.full` |
| `102-header.finalScore.png` | `header.finalScore` |
| `103-header.officials.png` | `header.officials` |
| `104-header.periodScores.png` | `header.periodScores` |
| `105-game.periodIntervalScores.png` | `game.periodIntervalScores` |
| `201-home.playerTable.png` | `home.playerTable` |
| `202-home.teamTotals.png` | `home.teamTotals` |
| `301-away.playerTable.png` | `away.playerTable` |
| `302-away.teamTotals.png` | `away.teamTotals` |
| `401-comparativeStats.scoringBreakdown.png` | `comparativeStats.scoringBreakdown` |
| `402-comparativeStats.gameFlow.png` | `comparativeStats.gameFlow` |

`header.gameInfo` is intentionally not an active crop.

## Important Policies

- One PDF is one game and one page.
- Page format and orientation are stable. Margins are mostly stable, but there
  is no strict hardcoded content area.
- Coordinates are relative from `0` to `1`.
- `layout-map.default.json` is the stable source template. Calibration changes
  runtime rectangles only; do not overwrite verified source coordinates blindly.
- Full-page geometry calibrates known zones. It does not replace this structural
  contract.
- Final-score calibration uses the single-line team-name and score structure.
- Player-table calibration uses the known header plus structural bottom rows,
  never a fixed player-row count.
- GLM-OCR is not part of this project.
- `pdf_crop_runner` only generates calibrated crop images, metadata, and visual
  debug artifacts.
- Crop generation does not perform OCR.
- Crop generation does not change the final JSON.

## Reference Images

The images in `reference-images/` are human references only. They are not
required at runtime and must not be treated as the exact runtime-render
coordinate source.

- `001-macro-zones-operational.png`: broad macro zones.
- `002-semantic-crop-zones-operational.png`: semantic target zones.
- `macro-zones-reference.png`: legacy filename for the macro-zone human reference.
- `target-crop-zones-reference.png`: legacy filename for the semantic-zone human reference.
