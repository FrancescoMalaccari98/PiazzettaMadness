# Crop Zones

This document describes the operational crop zones defined in `layout-map.default.json`.

The crop coordinates are stored in JSON. This file is human documentation only.
They form a fixed source template. At runtime `LayoutCalibration` applies the
template to the canonical full-page image exported by `TesseractFullPage`, uses
full-page word-box geometry and rule-based anchors to align known regions, and
writes corrected crops. OCR geometry supports the fixed structure; it does not
rediscover the page layout from zero. Extracted score, team, and stat values are
never passed from full-page OCR into crop OCR.

## Coordinate System

All crop rectangles use relative coordinates from `0` to `1`.

```json
{
  "x": 0.10,
  "y": 0.20,
  "width": 0.30,
  "height": 0.40
}
```

## Tier 1 - Macro Zones

Macro zones are broad operational regions used by the first crop-based processing level.

| File | Crop ID | Expected content | Notes |
|---|---|---|---|
| `001-macro.header.png` | `macro.header` | Full top header | Logos, event title, final score, match info |
| `002-macro.periodIntervalScores.png` | `macro.periodIntervalScores` | 6-minute interval scores | Small interval table below the header |
| `003-macro.mainTables.png` | `macro.mainTables` | Main player tables | Home and Away player tables |
| `004-macro.bottomComparative.png` | `macro.bottomComparative` | Comparative stats | Bottom comparison tables, excluding legend |

## Tier 2 - Semantic Zones

Semantic zones are more specific crops used by the second crop-based processing level.

| File | Crop ID | Side | Expected content | Future use |
|---|---|---|---|---|
| `101-header.full.png` | `header.full` | - | Full header | Metadata fallback |
| `102-header.finalScore.png` | `header.finalScore` | - | Teams and final score | Score extraction |
| `103-header.officials.png` | `header.officials` | - | Referees / officials | Metadata extraction |
| `104-header.periodScores.png` | `header.periodScores` | - | Period scores | Period validation |
| `105-game.periodIntervalScores.png` | `game.periodIntervalScores` | - | 6-minute interval scores | Optional metadata |
| `201-home.playerTable.png` | `home.playerTable` | Home | Home full player table | Future row parser |
| `202-home.teamTotals.png` | `home.teamTotals` | Home | Home totals row | Team validation |
| `301-away.playerTable.png` | `away.playerTable` | Away | Away full player table | Future row parser |
| `302-away.teamTotals.png` | `away.teamTotals` | Away | Away totals row | Team validation |
| `401-comparativeStats.scoringBreakdown.png` | `comparativeStats.scoringBreakdown` | - | Left comparative table | Optional game stats |
| `402-comparativeStats.gameFlow.png` | `comparativeStats.gameFlow` | - | Right comparative table | Optional game stats |

## Runtime Calibration Rules

`LayoutCalibration` begins with these fixed zones and applies the machine-readable
rules in `layout-calibration-rules.json`. It may apply bounded runtime
offset/scale corrections when anchor evidence is reliable.

For `header.finalScore`:

- start from the known header region;
- use team-name and score word boxes as anchors;
- expect a single line: `Home Team <home score> - <away score> Away Team`;
- expect team names not to wrap and the score to be centered between them;
- do not use period-score or interval-score numbers as the final score;
- exclude bracketed partials;
- keep the crop focused on the team names and final score.

For `home.playerTable` and `away.playerTable`:

- keep the known table order: Home first, Away second;
- use the known column-header row as the top anchor;
- include the column header in each crop;
- use `Squadra/Allenatore` and `Totali` as the bottom structural anchors;
- expect `Totali` immediately after `Squadra/Allenatore`;
- do not calculate the bottom boundary from a fixed player-row count.

Visible player-row count is variable. A valid PDF may show 7, 8, 9, or another
valid count.

For table-boundary details and fallbacks, see `table-boundary-rules.md`.

Calibration fails closed per crop:

- if `Squadra/Allenatore` is missing but `Totali` is found, continue with warning;
- if `Totali` is missing, close shortly after the last reliable row with warning;
- if the header is missing but `Totali` is found, use a wider template crop with warning;
- if evidence remains insufficient, mark the crop unusable.

Bad crops must not be emitted silently. `layout-calibration-report.json` must
record each fallback or failure. `TesseractLayoutCrops` may consume only
generated crop entries explicitly marked usable.

## Optional Interval Crops

`macro.periodIntervalScores` and `game.periodIntervalScores` remain in the map
for compatibility. They are optional, low-priority crops and candidates for
future removal after their diagnostic value is measured.

## Column Calibration

The 23 player-table columns are defined in `player-table-columns.json`. There
are no reliable vertical separator lines. The PaddleOCR row channel
(`PaddleTableRows`) derives exact runtime x positions from header OCR geometry,
using fixed positions only as template hints.

## Legend

The legend is not cropped as runtime output.

If needed, it should be documented here as fixed reference information, not generated at runtime.

## Future Tier 3 - Rows and Cells

Future work may add:

```text
home.playerRows
home.teamBenchRow
home.teamTotalsRow
away.playerRows
away.teamBenchRow
away.teamTotalsRow
```

For now, row-level crops are not implemented.

## Layout Debug Artifacts

Each calibrated document writes human-inspection artifacts under:

```text
runtime/Dataset/LayoutDebug/<document-name>.<short-hash>/
```

Expected files include full-page word boxes, a word overlay, crop overlays
before and after calibration, `layout-calibration-report.json`, and
`contact-sheet-after.html`.

Reference images under `reference-images/` are human-only visual guides. They
are not exact runtime coordinate inputs.
