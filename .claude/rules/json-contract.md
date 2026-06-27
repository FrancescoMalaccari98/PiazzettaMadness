# Contratto JSON — BasketPdfStats

## Stabilità

Lo schema JSON di `ProcessingResult` è stabile. Non modificarlo senza:
1. Descrivere i campi aggiunti/rimossi/modificati
2. Verificare l'impatto su: tests, samples/test_import_match1.json, import PHP, ResultViewerForm
3. Aggiornare i test snapshot
4. Ottenere approvazione esplicita

## Schema attuale (campi principali)

```json
{
  "schemaVersion": "1.0",
  "processedFile": {
    "fileName": "...",
    "documentHash": "sha256:...",
    "status": "CompletedValidated | CompletedWithWarnings | CompletedNotValidated | Failed | CompletedWithReviewRequired",
    "startedAt": "...",
    "completedAt": "..."
  },
  "ocrRuns": [
    { "engine": "TesseractFullPage", "status": "Success | Failed | Timeout | NotConfigured | Skipped", "durationMs": 0, "error": null }
  ],
  "game": {
    "homeTeamId": "team:Home",
    "awayTeamId": "team:Away",
    "competition": "...", "venue": "...", "date": "...", "time": "...",
    "finalScore": "52-44",
    "periods": [{ "home": 0, "away": 0 }],
    "referees": []
  },
  "teams": [
    { "teamId": "team:Home", "side": "Home", "name": "...", "abbreviation": "..." }
  ],
  "players": [
    {
      "entityId": "player:Home:jersey:5",
      "teamId": "team:Home",
      "side": "Home",
      "number": "5",
      "fullName": "...", "firstName": "...", "lastName": "...",
      "starter": true, "captain": false, "didNotPlay": false
    }
  ],
  "stats": [
    {
      "scope": "Player | Team | Game | Result | Comparative",
      "entityId": "player:Home:jersey:5",
      "statKey": "points",
      "fieldId": "...",
      "value": 14,
      "status": "Validato | NonValidato",
      "score": 0.95,
      "warnings": [],
      "failedRules": []
    }
  ],
  "validation": {
    "status": "CompletedValidated",
    "failedRules": [],
    "warnings": []
  },
  "reconciliation": {
    "enabled": true,
    "providers": ["ocr.tesseract.fullpage", "ocr.tesseract.crop", "ocr.paddle.crop", "ocr.paddle.row"],
    "strategy": "Weighted field-by-field"
  }
}
```

## Campi già aggiunti

- `reconciliation.sideInversionApplied: bool` — **implementato in Fase 6**. True quando la
  pipeline rileva e corregge un'inversione Home/Away rispetto al DB. Default `false`/assente.
- `identityReview: IdentityReviewItem[]` — **implementato in Fase 7A**. Voci di revisione
  identità (Side, Reason ∈ {ProbableMatch|Conflict|NumberNotFound|Unmatched|NotInPdf}, OcrJersey,
  OcrName, CandidatePlayerId?, CandidateName?, ConfidenceScore). Campo transitorio sul risultato:
  in Fase 8 si sposterà in IdentityResolutionResult/CanonicalGameResult.
- Stato `CompletedWithReviewRequired` aggiunto a `FileProcessingStatus`: impostato quando ci sono
  voci di revisione `Conflict` o `NotInPdf`.
- **Pulizia diagnostica (2026-06-26):** `stats[].candidates` e `stats[].reconciliation` (per-campo,
  con `providerValues`) **non sono più nel JSON** (marcati `[JsonIgnore]`): restano solo in-memory per
  la riconciliazione/test. In `stats[].warnings`/`failedRules` e in `validation.warnings` restano solo i
  warning importanti: severità `Error`, `math.*`, `team.*` (mismatch/inversione), `ocr.processingAbortedByUser`.
  I warning OCR diagnostici (`ocr.reconciliation.*`, plan/crop, disaccordo provider) non vengono più emessi
  nel risultato finale. Il blocco `reconciliation` a livello di risultato (enabled/providers/strategy/
  sideInversionApplied) resta invariato.

## Contratto import (Fase 8) — ImportPayload

Il POST `/api-ocr/import/{matchId}` NON invia più il `ProcessingResult` grezzo: invia un
`ImportPayload` con ID canonici DB già risolti da C#. Il JSON locale/diagnostico resta invariato
(entityId OCR). Forma del payload:

```json
{
  "matchId": 42,
  "teams": [ { "side": "Home", "teamId": 5, "name": "...", "abbreviation": "..." } ],
  "players": [
    { "entityId": "player:Home:jersey:5", "side": "Home", "playerId": 101, "teamId": 5,
      "number": "5", "starter": true, "didNotPlay": false }
  ],
  "stats": [ { "scope": "Player", "entityId": "player:Home:jersey:5", "statKey": "points", "value": 12 } ]
}
```

- `players[].playerId`/`teamId` = ID canonici DB risolti da C# (`PlayerIdentityMatcher` + revisione manuale).
- `stats[]` invariato: il PHP le collega per `entityId` ai giocatori del payload.
- PHP verifica che ogni `playerId` appartenga al roster della partita; **422** con lista `invalid` se no.
- Giocatori non risolti vengono esclusi dal payload (restano in `identityReview`).

`samples/test_import_match1.json` illustra la struttura player/stat (pre-Fase 8, senza i campi
canonici): resta come riferimento del layout, non è una fixture di test.

## Campi pianificati (futuro)

- `processedFile.canonicalMatchId: int?` / `teams[].canonicalTeamId: int?` /
  `players[].canonicalPlayerId: int?` nel JSON locale (solo se servirà esporli anche localmente).

## Entity ID canonici

| Formato | Esempio | Quando |
|---------|---------|--------|
| `team:Home` | `team:Home` | Squadra di casa |
| `team:Away` | `team:Away` | Squadra ospite |
| `player:<side>:jersey:<n>` | `player:Home:jersey:5` | Giocatore con numero maglia |
| `player:<side>:name:<nome>` | `player:Away:name:Rossi` | Fallback quando numero non leggibile |
| `player:<side>:row:<n>` | `player:Home:row:3` | Fallback ultimo: solo posizione riga |

## Stat key canoniche

Le chiavi sono definite in `CanonicalStatKeyMapper` in `src/BasketPdfStats.Core/Normalization/CanonicalStatKeyMapper.cs`.

Esempi principali:

| Chiave | Tipo | Note |
|--------|------|------|
| `points` | int | Punti totali |
| `fieldGoals.made` / `fieldGoals.attempted` | int | Tiri dal campo |
| `twoPoints.made` / `twoPoints.attempted` | int | Tiri da 2 |
| `threePoints.made` / `threePoints.attempted` | int | Tiri da 3 |
| `freeThrows.made` / `freeThrows.attempted` | int | Tiri liberi |
| `rebounds.offensive` / `rebounds.defensive` / `rebounds.total` | int | Rimbalzi |
| `assists` | int | Assist |
| `turnovers` | int | Palle perse |
| `steals` | int | Palle recuperate |
| `blocks` | int | Stoppate |
| `fouls.committed` / `fouls.drawn` | int | Falli |
| `plusMinus` | int | +/- |
| `evaluation` | int | Valutazione |
| `minutes` | string "MM:SS" | Minuti giocati |

Non inventare nuove stat key: aggiungerle in `CanonicalStatKeyMapper` se necessario.

## Valori mancanti

- Statistica non estratta: `"value": null` — **mai** inventare 0 o altro valore
- Lista vuota: `[]` (per `referees`, `warnings`, `failedRules`)
- Giocatore DNP: `"didNotPlay": true`, tutte le statistiche `null`

## Dati diagnostici

I dati diagnostici (output intermedi per canale, calibration report, crop immagini) NON fanno parte del JSON finale. Restano in `runtime/Dataset/` e `runtime/Debug/`. Non includere dati grezzi OCR nel JSON esportato.

## Formati numerici

- Interi: JSON number (non stringa)
- Percentuali: NON nel JSON (derivate dal PHP in fase di visualizzazione)
- Minuti: stringa "MM:SS" (es. "23:45"), mai secondi totali
- Score finale: stringa "XX-YY" in `game.finalScore`
