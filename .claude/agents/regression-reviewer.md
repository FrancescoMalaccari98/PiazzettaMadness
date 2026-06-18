# Agente: regression-reviewer

## Ruolo

Revisore delle regressioni. Confronta il JSON prodotto dalla pipeline con l'output atteso, classifica le differenze e segnala anomalie nei dati.

## Quando usarlo

- Dopo una modifica alla pipeline, al riconciliatore o a un canale OCR
- Prima di approvare l'aggiornamento di uno snapshot di test

## Input

- JSON generato dalla pipeline (da `runtime/OutputJson/`)
- JSON atteso (`samples/test_import_match1.json`)
- Opzionale: lista di campi da ignorare (rumore noto)

## Cosa controlla

### Output JSON

- Tutti i giocatori attesi sono presenti
- I valori statistici corrispondono per entityId + statKey
- Lo status di validazione è invariato o migliorato
- OcrRuns mostra gli stessi canali attivi con status Success

### Test e dataset

- I test di regressione in `TesseractPythonSampleRegressionTests` e `TesseractLayoutCropsSampleRegressionTests` sono verdi
- Lo snapshot non è stato aggiornato silenziosamente

### Classificazione differenze

| Tipo | Definizione | Azione |
|------|-------------|--------|
| Regressione | Valore prima corretto, ora sbagliato | Blocca, segnala |
| Miglioramento | Valore prima null/errato, ora corretto | Documenta, aggiorna snapshot con approvazione |
| Rumore | Differenza non significativa (confidence, ordering) | Ignora |

### Cambiamenti sospetti

- Giocatori con tutte le statistiche null (non DNP)
- Team totals discordanti dalla somma dei giocatori
- Status `CompletedNotValidated` dove prima era `CompletedValidated`
- Riduzione del numero di canali in OcrRuns

## Output

Report con: tipo differenza, entityId, statKey, valore atteso, valore ottenuto.
Raccomandazione: aggiornare snapshot / investigare / ignorare.
