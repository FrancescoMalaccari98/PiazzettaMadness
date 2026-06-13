# Validation Rules

This document is the structural contract for value-level validation in
BasketPdfStats. It is the human companion to the machine-readable rule catalog
(`validation-rules.json`, when present). Every rule here is identified by a
stable `ruleId` so that warnings, reports, and reconciliation decisions can
reference rules unambiguously.

Validation is provider-independent. It never depends on which OCR engine
produced a value. Across all passes, preserve: warnings, failed `ruleId`s,
affected field IDs, per-provider candidate values, confidence, and provenance.

Generic PDF crop preparation is outside validation. `pdf_crop_runner` creates
images and metadata only; it must not materialize values or alter final quality.
`LayoutCalibration` starts from the fixed `pdf-structure/` contract and must fail
closed before crop OCR when required geometry anchors are missing, ambiguous, or
misaligned. Validation must not compensate for silently bad crop preparation.

---

## 1. Rule Severity Model

Every rule has a severity that fixes its behavior on failure. This separation is
mandatory: an integer identity and a fragile diagnostic must never be treated
the same way.

| Severity | Meaning | Tolerance | Action on failure (`onFailure`) |
|---|---|---|---|
| `hard` | Exact arithmetic identity that must hold for valid data | Zero (exact integers) | First attempt to resolve via reconciliation/derivation. If still failing: mark affected fields, record evidence, set status `NeedsReview` or `Invalid`. Never silently drop. |
| `soft` | Redundant/derived check that should hold within tolerance | Documented epsilon | Warn, attach `ruleId`, lower trust of involved fields. Never hard-fail on its own. May act as a tie-breaker (see §3). |
| `diagnostic` | Plausibility signal on fragile inputs | Wide / informational | Warn only. Never lowers final status by itself. Useful as corroborating evidence during reconciliation. |

A rule's severity is fixed by this document. Implementations must not promote a
`diagnostic` to a gate, nor demote a `hard` identity to a warning.

---

## 2. Pipeline and Order of Operations

Validation is not a single trailing gate. It runs at three points and converges
to a fixpoint.

1. **Light per-strategy validation.** For each normalized OCR strategy
   independently, run the cell- and row-level checks where data is present.
   Attach warnings and lower trust. Do **not** rewrite values at this stage.
2. **Validation-driven reconciliation** (see §3). Validation scores candidate
   values; reconciliation selects per field.
3. **Strict validation** on the reconciled result, running the full catalog
   (§4).
4. **Derivation** (see §6), applied only when mathematically certain, then
   **strict validation again**.
5. **Fixpoint loop.** Repeat steps 3–4 until no value changes or a maximum
   iteration count is reached. If values oscillate or a `hard` contradiction
   persists, stop, freeze the involved fields, record full evidence, and let the
   status reflect the unresolved conflict.
6. **Final quality status** (§7) is assigned from the residual outcomes.

**Idempotency rule:** a converged result re-validated must produce no new
changes. Derivation must never reintroduce a contradiction that a previous pass
removed.

---

## 3. Validation-Driven Reconciliation

Reconciliation does not rely on majority vote alone. Provider agreement is one
input; **structural coherence is a stronger one**.

For each disputed field:

- Collect the candidate values from all strategies, with confidence and
  provenance.
- Score each candidate by how many `hard` rules the surrounding row, table, and
  document satisfy when that candidate is assumed.
- Prefer the candidate that makes the row/table internally consistent, even when
  it is the **minority** reading. A value agreed by two engines but inconsistent
  with the points/field-goal/rebound identities loses to a minority value that
  satisfies them.
- Break remaining ties with provider agreement and confidence.

**Percentage as a ratio tie-breaker (`RECON-PCT-TIEBREAK`).** Each shooting group
prints both `R/T` and `%`. They encode the same information twice. When the
made/attempted ratio is ambiguous, the printed percentage disambiguates it:
choose the ratio whose computed percentage matches the OCR'd percentage within
the §5 tolerance (e.g. `5/14`→35.7 vs `6/14`→42.9). This is a per-cell check that
runs during reconciliation, not only as post-hoc validation.

Reconciliation must record, per field, which rule or signal decided the value.

---

## 4. Rule Catalog

All numeric identities below operate on the canonical integer/decimal model in
`player-table-structure.md`. Percentage comparisons follow §5.

### 4.1 Cell-level

| ruleId | Expression / condition | Severity | Warning code |
|---|---|---|---|
| `CELL-TYPE` | Value matches the declared column type/shape (`madeAttempted`, `percentage`, `integer`, `signedInteger`, `timeOrDnp`, …) | `hard` | `validation.cell.typeMismatch` |
| `CELL-MADE-LE-ATT` | `made <= attempted` for every made/attempted pair | `hard` | `validation.cell.madeExceedsAttempted` |
| `CELL-NONNEG` | Counters are non-negative. **Excludes `plusMinus`**, which is signed | `hard` | `validation.cell.negativeCounter` |
| `CELL-PCT-RANGE` | `0 <= percentage <= 100` | `hard` | `validation.cell.percentOutOfRange` |
| `CELL-PCT-ZERO` | If `attempted == 0` then `percentage == 0.0`. Disables `CELL-PCT-FORMULA` for that cell (no division by zero) | `hard` | `validation.cell.percentZeroAttempts` |
| `CELL-PCT-FORMULA` | `percentage ~= made / attempted * 100` (only when `attempted > 0`) | `soft` | `validation.cell.percentMismatch` |

### 4.2 Player-row level (intra-row identities)

| ruleId | Expression | Severity | Warning code |
|---|---|---|---|
| `ROW-FG-MADE` | `fieldGoals.made = twoPoints.made + threePoints.made` | `hard` | `validation.row.fieldGoalsMadeMismatch` |
| `ROW-FG-ATT` | `fieldGoals.attempted = twoPoints.attempted + threePoints.attempted` | `hard` | `validation.row.fieldGoalsAttemptedMismatch` |
| `ROW-PTS` | `points = 2 * twoPoints.made + 3 * threePoints.made + freeThrows.made` | `hard` | `validation.row.pointsMismatch` |
| `ROW-REB` | `rebounds.total = rebounds.offensive + rebounds.defensive` | `hard` | `validation.row.reboundsMismatch` |
| `ROW-DNP` | `N.E.`/DNP rows must have empty/zero stats. Positive stats on a non-playing row → warning | `diagnostic` | `validation.row.dnpWithStats` |

`ROW-DNP` rows are exempt from `ROW-FG-*`, `ROW-PTS`, `ROW-REB`, and all
`CELL-PCT-*` checks: their cells are expected blank and must not trigger
arithmetic failures.

### 4.3 Team-table level

The `Totali` row in FIBA box scores includes team-only values carried on the
`Squadra/Allenatore` (team bench) row — most often team rebounds, sometimes team
turnovers. Summing player rows alone is **wrong** and produces false mismatches.

| ruleId | Expression | Severity | Warning code |
|---|---|---|---|
| `TEAM-SUM` | For every numeric stat: `Totali[stat] = sum(playerRows[stat]) + teamBench[stat]`, treating missing/blank cells as `0` | `hard` (downgrades to `soft` if the team-bench row is missing or unusable, with a recorded warning) | `validation.team.totalsMismatch` |
| `TEAM-PCT` | `Totali` percentages computed from `Totali` made/attempted per §5 | `soft` | `validation.team.totalsPercentMismatch` |

`Squadra/Allenatore` and `Totali` are structural rows and must never be treated
as players (see §8).

### 4.4 Document level (cross-zone)

These triangulate the score across three independent zones: the header final
score, the `Totali` points, and the period scores.

| ruleId | Expression | Severity | Warning code |
|---|---|---|---|
| `DOC-FINAL-SCORE` | `team Totali points = header final score` (per side) | `hard` | `validation.doc.finalScoreMismatch` |
| `DOC-PERIOD-SUM` | `team final score = sum(team period scores)`, OT-aware (count actual periods, including overtimes) | `hard` (when period scores are usable; otherwise `soft` with warning) | `validation.doc.periodSumMismatch` |
| `DOC-PM-MARGIN` | `sum(team player plusMinus) = 5 * scoreMargin` | `diagnostic` | `validation.doc.plusMinusMargin` |
| `DOC-MIN-SUM` | `sum(player minutes) ~= 5 * regulationPlayingTime` | `diagnostic` | `validation.doc.minutesSum` |

**`DOC-MIN-SUM` must use regulation playing time** — `periods * periodLength`,
overtime included — **not** the wall-clock "Durata gara" field. Playing-time
parameters are competition-dependent and must be configured per format, never
hardcoded to a single standard. Minutes OCR is fragile, hence `diagnostic`.

**`DOC-PM-MARGIN`** is mathematically exact in theory (each stint adds the stint
margin to all five on-court players), but real data-entry and OCR quirks make it
a strong corroborating signal rather than a gate. Use it to raise or lower trust
during reconciliation, never to reject on its own.

---

## 5. Numeric Semantics and Tolerances

- **Integer identities are exact.** All `hard` rules in §4.2–§4.4 that operate on
  counters use zero tolerance. A one-unit mismatch is a real failure, not noise.
- **Percentages are approximate.** FIBA prints one decimal. Compute
  `round(made / attempted * 100, 1)` and compare with tolerance `±0.1` to absorb
  rounding ambiguity. Percentages are checked/derived values, not primary truth.
- **Division by zero.** When `attempted == 0`, the percentage is `0.0` by
  convention (`CELL-PCT-ZERO`); the formula check is skipped, never evaluated.
- **Separator normalization.** OCR may produce `,` or `.` as the decimal mark
  (`50,0`, `50.0`). Normalize to a dot before comparison. Final JSON stores
  decimals with a dot.
- **Signed values.** `plusMinus` is the only signed counter and is exempt from
  non-negativity (`CELL-NONNEG`).

Tolerances are part of the contract: changing them is a versioned change to this
document and its machine-readable twin.

---

## 6. Derivation Policy

Derive values only when the rule is mathematically certain:

- all operands exist;
- **operands are cross-confirmed** (by provider consensus or by the §3
  percentage tie-breaker) — derivation must not consume an unconfirmed operand;
- the result is within a valid range;
- derivation introduces no new contradiction (re-validated in the fixpoint
  loop);
- provenance and a warning are recorded.

**Derivation never launders an error.** A derived value inherits the uncertainty
of its operands: `confidence(derived) = min(confidence(operands)) * ruleCertainty`.
Never silently overwrite an OCR value, and never let a derived value mask a
`hard` failure — record the derivation rule, source fields, original missing or
conflicting values, confidence, and warning metadata.

Derivation must never overwrite a value already confirmed by consensus.

### Safe derivations (all operands present and confirmed)

```text
fieldGoals.made       = twoPoints.made + threePoints.made
fieldGoals.attempted  = twoPoints.attempted + threePoints.attempted
rebounds.total        = rebounds.offensive + rebounds.defensive
points                = 2 * twoPoints.made + 3 * threePoints.made + freeThrows.made
percentage            = made / attempted * 100        (only when attempted > 0)
```

### Conditional derivations (only one missing value, operands reliable)

```text
twoPoints.made          = fieldGoals.made - threePoints.made
threePoints.made        = fieldGoals.made - twoPoints.made
twoPoints.attempted     = fieldGoals.attempted - threePoints.attempted
threePoints.attempted   = fieldGoals.attempted - twoPoints.attempted
missing player points   = team points - sum(other player points)
missing plusMinus       = 5 * margin - sum(known plusMinus)
```

### Do not automatically derive

- player names;
- player identity;
- jersey numbers unless logically certain and clearly warned;
- assists, steals, blocks, turnovers, fouls;
- more than one missing player value at once.

---

## 7. Final Quality Status

The final JSON may still be produced when warnings exist. Preserve a clear,
field-aware status:

| Status | Condition |
|---|---|
| `Valid` | No `hard` failures; no `soft` failures outside tolerance. |
| `ValidWithWarnings` | No `hard` failures; some `soft` or `diagnostic` warnings present. |
| `NeedsReview` | A `hard` rule failed and could not be resolved by reconciliation or derivation, but core data is largely present; or reconciliation resolved key fields only at low confidence. |
| `Invalid` | Unresolved `hard` contradictions on core fields, structural anchor failure, or required crops unusable. |

If these exact statuses are not yet present in the schema, introduce them only in
a focused task. Do not redesign the JSON schema during unrelated work.

---

## 8. Player Identity Rules

*(Unchanged — this logic is stable and works well.)*

Identity reconciliation must be conservative.

- Do not merge players only because jersey numbers match.
- Do not merge players only because names are similar.
- Use side, row/order, jersey, name similarity, table position, duplicate jersey
  detection, validation consistency, and provider provenance.
- When uncertain, keep players separate and add a warning.

Canonical IDs:

```text
team:Home
team:Away
player:<side>:jersey:<number>
player:<side>:name:<normalized-name>
player:<side>:row:<row-index>
```

Use jersey ID only when safe, then name fallback, then row fallback.

For each player:

```text
PlayerStats.EntityId == StatValue.EntityId
```

Roster checks:

- duplicate jersey within the same side;
- duplicated player;
- fake player names;
- empty names;
- numeric-only names;
- time values such as `18:13` used as names;
- `N.E.` used as a name;
- stats without player;
- player without stats;
- DNP or `N.E.` rows with positive stats;
- players with positive stats but no minutes.

Rows `Squadra/Allenatore` and `Totali` must not become players. `Totali` maps to
team totals.

---

## 9. Provenance and Reporting

Every value in the final result carries provenance:

- contributing provider(s) and their candidate values;
- agreement count and confidence;
- the set of `ruleId`s the value passed and failed;
- which rule or signal decided the value during reconciliation (§3);
- derivation metadata when applicable (§6).

Every fallback, warning, unusable input, and unresolved conflict must be recorded
with field-level evidence. Mismatches are flagged, never hidden.
