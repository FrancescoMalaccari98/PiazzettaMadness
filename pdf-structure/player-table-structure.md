# Player Table Structure

This document describes the fixed FIBA player table used by the PDFs.

The machine-readable column model is stored in:

```text
player-table-columns.json
```

Player-table crop-boundary calibration is documented in:

```text
table-boundary-rules.md
```

## Row Model

Each team table has a variable number of visible player rows followed by two
fixed structural rows:

| Row type | Count | Meaning |
|---|---:|---|
| `player` | Variable | Visible player rows, including `N.E.` players |
| `teamBench` | 1 | `Squadra/Allenatore` row |
| `teamTotals` | 1 | `Totali` row |

Do not assume that every PDF has exactly 9 visible players. Valid PDFs may show
7, 8, 9, or another player count. The row count must not be the primary crop
boundary.

The last two rows are always:

```text
Squadra/Allenatore
Totali
```

They are strong bottom anchors for table crop calibration.

`N.E.` players still count as player rows. `Squadra/Allenatore` must never
become a player. `Totali` maps to team totals.

`player-table-columns.json` mirrors this variable-row strategy. Its listed
player counts are examples, not hard crop boundaries.

## Table Order And Crop Boundaries

- Home player table appears first.
- Away player table appears second.
- The known column-header row is the top anchor and must remain inside the crop.
- `Squadra/Allenatore` and `Totali` are the bottom anchors.
- `Totali` immediately follows `Squadra/Allenatore`.
- Do not derive crop height from a hardcoded player-row count.
- If a structural anchor is missing, use only the bounded warning-producing
  fallbacks documented in `table-boundary-rules.md`.

## Column Groups

| Group | Printed label | Columns |
|---|---|---|
| Identity | - | `N.`, `Nome`, `Min.` |
| Field goals | `Tiri dal campo` | `R/T`, `%` |
| Two points | `2 Punti` | `R/T`, `%` |
| Three points | `3 Punti` | `R/T`, `%` |
| Free throws | `Tiri Liberi` | `R/T`, `%` |
| Rebounds | `Rimbalzi` | `RO`, `RD`, `RT` |
| Misc | - | `AS`, `PP`, `PR`, `SD` |
| Fouls | `Falli` | `FF`, `FS` |
| Summary | - | `+/-`, `Val.`, `PTI` |

The complete fixed printed order is:

```text
N., Nome, Min., Tiri dal campo R/T, Tiri dal campo %, 2 Punti R/T,
2 Punti %, 3 Punti R/T, 3 Punti %, Tiri Liberi R/T, Tiri Liberi %,
RO, RD, RT, AS, PP, PR, SD, FF, FS, +/-, Val., PTI
```

There are no reliable vertical separator lines. Column calibration must derive
exact runtime x positions from header OCR geometry. Any fixed x positions are
template hints only. This supports the PaddleOCR row channel (`PaddleTableRows`).

## Important Ambiguity

`R/T` under shooting groups means:

```text
realizzati / tentati
made / attempted
```

`RT` under `Rimbalzi` means:

```text
rimbalzi totali
rebounds.total
```

Do not treat them as the same field.

## Column Meaning

| Printed column | Canonical meaning | Type | Example |
|---|---|---|---|
| `N.` | jersey number | `jerseyNumber` | `0` |
| `Nome` | player name | `playerName` | `Francesco EMILIANI (C)` |
| `Min.` | minutes or did-not-play | `timeOrDnp` | `08:39`, `N.E.` |
| `Tiri dal campo R/T` | field goals made/attempted | `madeAttempted` | `1/2` |
| `Tiri dal campo %` | field goals percentage | `percentage` | `50,0` |
| `2 Punti R/T` | two points made/attempted | `madeAttempted` | `0/1` |
| `2 Punti %` | two points percentage | `percentage` | `0,0` |
| `3 Punti R/T` | three points made/attempted | `madeAttempted` | `1/1` |
| `3 Punti %` | three points percentage | `percentage` | `100,0` |
| `Tiri Liberi R/T` | free throws made/attempted | `madeAttempted` | `1/2` |
| `Tiri Liberi %` | free throws percentage | `percentage` | `50,0` |
| `RO` | offensive rebounds | `integer` | `0` |
| `RD` | defensive rebounds | `integer` | `1` |
| `RT` | total rebounds | `integer` | `1` |
| `AS` | assists | `integer` | `1` |
| `PP` | turnovers | `integer` | `0` |
| `PR` | steals | `integer` | `0` |
| `SD` | blocks | `integer` | `0` |
| `FF` | fouls committed | `integer` | `1` |
| `FS` | fouls drawn | `integer` | `1` |
| `+/-` | plus/minus | `signedInteger` | `1`, `+1`, `-4` |
| `Val.` | evaluation | `integer` | `4` |
| `PTI` | points | `integer` | `4` |

## OCR Notes

- Percentages may use comma or dot: `50,0`, `50.0`.
- Final JSON uses decimal numbers with dot.
- `PTI` may be confused by OCR as `PTl`, `PT1`, or `Ptl`.
- `Val.` maps to canonical `evaluation`.
- `PP` maps to `turnovers`.
- `PR` maps to `steals`.
- `SD` maps to `blocks`.
- `FF` maps to `fouls.committed`.
- `FS` maps to `fouls.drawn`.

## Example Row

| N. | Nome | Min. | FG | FG% | 2P | 2P% | 3P | 3P% | FT | FT% | RO | RD | RT | AS | PP | PR | SD | FF | FS | +/- | Val. | PTI |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | Francesco EMILIANI (C) | 08:39 | 1/2 | 50,0 | 0/1 | 0,0 | 1/1 | 100,0 | 1/2 | 50,0 | 0 | 1 | 1 | 1 | 0 | 0 | 0 | 1 | 1 | 1 | 4 | 4 |

## Normalized Meaning

```json
{
  "jerseyNumber": "0",
  "playerName": "Francesco EMILIANI",
  "captain": true,
  "minutes": "08:39",
  "fieldGoals": {
    "made": 1,
    "attempted": 2,
    "percentage": 50.0
  },
  "twoPoints": {
    "made": 0,
    "attempted": 1,
    "percentage": 0.0
  },
  "threePoints": {
    "made": 1,
    "attempted": 1,
    "percentage": 100.0
  },
  "freeThrows": {
    "made": 1,
    "attempted": 2,
    "percentage": 50.0
  },
  "rebounds": {
    "offensive": 0,
    "defensive": 1,
    "total": 1
  },
  "assists": 1,
  "turnovers": 0,
  "steals": 0,
  "blocks": 0,
  "fouls": {
    "committed": 1,
    "drawn": 1
  },
  "plusMinus": 1,
  "evaluation": 4,
  "points": 4
}
```
