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
      "candidates": [],
      "warnings": [],
      "failedRules": [],
      "reconciliation": {
        "selectedProvider": "ocr.tesseract.crop",
        "agreedProviders": ["ocr.tesseract.crop", "ocr.paddle.row"],
        "providerValues": { "ocr.tesseract.fullpage": 14, "ocr.paddle.row": 14 }
      }
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

## Campi pianificati (Fase 7)

Da aggiungere dopo approvazione:
- `processedFile.canonicalMatchId: int?`
- `teams[].canonicalTeamId: int?`
- `players[].canonicalPlayerId: int?`
- `reconciliation.sideInversionApplied: bool`
- `identityReview: IdentityReviewItem[]` (Fase 6)

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
