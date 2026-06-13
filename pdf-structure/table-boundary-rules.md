# Table Boundary Rules

This document defines the structural rules used to calibrate FIBA player-table
crops. The machine-readable companion is `layout-calibration-rules.json`.

## Principle

Start from the fixed normalized rectangles in `layout-map.default.json`, then
align known table areas with `TesseractFullPage` OCR word-box geometry.
Calibration must not rediscover the full page layout dynamically.

The human reference images are visual guides only. They are not runtime
coordinate sources.

## Table Order

- The Home table appears first.
- The Away table appears second.
- Both tables use the same 23-column layout from `player-table-columns.json`.
- There are no reliable vertical separator lines.

## Top Boundary

The top boundary is the known column-header row. It must remain inside each
`playerTable` crop.

Useful header anchors include:

```text
N., Nome, Min., Tiri dal campo, 2 Punti, 3 Punti, Tiri Liberi,
Rimbalzi, AS, PP, PR, SD, FF, FS, +/-, Val., PTI
```

Exact column x positions should be derived from header OCR geometry. Fixed x
positions may be used only as template hints, not as final truth.

## Bottom Boundary

Visible player-row count is variable. Examples include 7, 8, and 9 rows, but
other valid counts may occur. Never derive crop height from a fixed row count.

The final two structural rows are always:

```text
Squadra/Allenatore
Totali
```

`Totali` immediately follows `Squadra/Allenatore`. These rows move upward when
fewer players are visible.

The `playerTable` crop includes:

```text
column header
visible player rows, including N.E. rows
Squadra/Allenatore
Totali
```

The `teamTotals` crop includes only the `Totali` row.

## Fallbacks

- If `Squadra/Allenatore` is missing but `Totali` is found, continue with a
  warning.
- If `Totali` is missing, close shortly after the last reliable detected row
  and emit a warning.
- If the header is missing but `Totali` is found, use a wider template-based
  crop and emit a warning.
- If reliable evidence is still insufficient, mark that crop unusable.

Fallback use must be written to `layout-calibration-report.json`. A crop that
cannot be calibrated safely must not silently appear successful.

## Future Column Calibration

The 23 known columns remain defined by `player-table-columns.json`. Deriving
their exact runtime x positions from header geometry is support for the
PaddleOCR row channel (`PaddleTableRows`).
