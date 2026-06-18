# Regole worker Python — BasketPdfStats

## Worker attivi

| Worker | Modulo | Venv | Canale |
|--------|--------|------|--------|
| Full-page Tesseract | `fiba_pdf_to_json.worker` | fiba_pdf_to_json/.venv | CH1 |
| Crop Tesseract | `fiba_pdf_to_json.crop_worker` | fiba_pdf_to_json/.venv | CH2 |
| Crop PaddleOCR | `fiba_pdf_to_json.paddle_crop_worker` | .venv-paddle | CH3 |
| Row PaddleOCR | `fiba_pdf_to_json.paddle_row_worker` | .venv-paddle | CH4 |
| Calibrazione layout | `pdf_crop_runner.calibrate_layout` | .venv-crop | Pre-pipeline |

## Contratto stdin/stdout

- I worker ricevono input esclusivamente tramite **argomenti CLI** (argparse).
- Lo **stdout** è riservato al JSON di output. Non scrivere mai testo diagnostico su stdout.
- Lo **stderr** è per log, warning e traceback. C# cattura stderr separatamente.
- Il JSON di output viene scritto su un **file** il cui path viene passato come argomento (`--output`, `--evidence-output`, `--output-folder`). Non scrivere il JSON su stdout.

## Codici di uscita

- `0` = successo (il file di output è stato creato)
- `1` = errore (il file di output può non esistere o essere incompleto)
- Usare `sys.exit(1)` o `raise SystemExit(1)` in caso di errore fatale
- Non usare `os._exit()` salvo casi estremi documentati

## Logging Python

```python
import logging
LOGGER = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO, format="%(levelname)s %(name)s: %(message)s")
```

- Usa `LOGGER.info/warning/error/exception` — non `print()`
- Il logging va su stderr automaticamente con questa configurazione
- Abilitare DEBUG con `--verbose` o variabile d'ambiente, non di default
- Non loggare il token API, dati sensibili o testi OCR grezzi voluminosi

## Schema evidence_records.json

Tutti i worker devono emettere il formato condiviso `evidence_records.json`:

```json
{
  "schemaVersion": "1.0",
  "outputKind": "evidence_records",
  "sourceId": "ocr.tesseract.fullpage",
  "engine": "TesseractFullPage",
  "granularity": "fullpage",
  "documentFileName": "...",
  "documentHash": "sha256:...",
  "records": [
    {
      "recordId": "...",
      "fieldId": "...",
      "entityId": "player:Home:jersey:5",
      "entityType": "Player",
      "side": "Home",
      "statKey": "points",
      "scope": "Player",
      "cropId": "...",
      "zoneId": "...",
      "valueRaw": "14",
      "valueNormalized": 14,
      "confidenceRaw": 0.92,
      "confidenceNormalized": 0.92,
      "mapperId": "...",
      "warnings": []
    }
  ]
}
```

Il modello C# corrispondente è `EvidenceRecord` in `src/BasketPdfStats.Core/Models/EvidenceRecord.cs`.
Se modifichi il schema Python, aggiorna il modello C# in modo sincrono.

## Gestione errori Python

- Errori non fatali: loggare con `LOGGER.warning/exception`, aggiungere `"warnings"` all'output JSON, continuare.
- Errori fatali (file non trovato, OCR engine non disponibile): loggare, `sys.exit(1)`.
- Non catturare `Exception` silenziosamente senza loggare.
- Grazia: se un campo non può essere estratto, ometterlo o impostare `null`, non inventare un valore.

## File temporanei

- Non creare file temporanei in cartelle di sistema o fuori da `runtime/`.
- Pulire i temp files prima di uscire (o affidarsi alla pipeline C# che pulisce `runtime/Working/`).

## Dipendenze Python

- Non aggiungere dipendenze heavy (ML, cloud SDK) senza motivazione documentata.
- Ogni worker usa il suo venv dedicato: non mescolare i venv.
- PaddleOCR usa `.venv-paddle` (pesante, ~2.5 GB); non includerlo nel venv Tesseract.
- Non aggiornare le versioni dei pacchetti senza verificare compatibilità.

## Roster context (da Fase 4)

I worker accetteranno un argomento `--roster-json <path>` contenente il roster del DB.
Il formato sarà definito in `OcrMatchContext` (C#) e serializzato in JSON temporaneo.
Non usare `known_names.py` quando `--roster-json` è disponibile.

## Timeout

- I worker vengono lanciati da `PythonProcessRunner` con un timeout configurabile.
- Se un worker supera il timeout, C# termina il processo e registra `OcrRunStatus.Timeout`.
- Non implementare loop infiniti o attese illimitate nei worker.
