# Skill: run-regression

## Descrizione

Esegue i test di regressione sul PDF campione e produce un report delle differenze rispetto all'output atteso.

## Quando usarla

- Prima di un commit su una modifica alla pipeline, riconciliatore o validazione
- Dopo una modifica a un canale OCR o al layout-map
- Per verificare che Fase N non abbia introdotto regressioni rispetto a Fase N-1
- Su richiesta esplicita dell'utente

## Prerequisiti

- Python/Tesseract disponibili e configurati
- PDF campione in `samples/pdf/TABELLINO FINALE 1-2 POSTO.pdf`
- Output atteso in `samples/test_import_match1.json`
- Build verde: `dotnet build .\BasketPdfStats.sln`

## Procedura

1. Esegui i test di regressione:
   ```powershell
   dotnet test .\BasketPdfStats.sln --filter "FullyQualifiedName~Regression"
   ```

2. Se i test di regressione richiedono un ambiente Python completo:
   ```powershell
   dotnet test .\BasketPdfStats.sln --filter "Category=Integration"
   ```

3. Confronta l'output JSON generato con `samples/test_import_match1.json`:
   - Campi statistici: ogni `statKey` per ogni `entityId`
   - Status di validazione
   - OcrRuns: canali eseguiti e loro status

4. Identifica le differenze:
   - Regressione: valore prima corretto, ora sbagliato → investigare
   - Miglioramento: valore prima null/errato, ora corretto → documentare
   - Rumore: differenza non significativa (variazione confidence, ordine lista) → ignorare

5. Produci report:
   - N test passati / N falliti
   - Differenze per campo: entityId, statKey, valore atteso, valore ottenuto
   - Classificazione: regressione / miglioramento / rumore

## Gestione snapshot

- Se le differenze sono **miglioramenti** approvati: aggiornare `samples/test_import_match1.json` con approvazione dell'utente
- Se le differenze sono **regressioni**: non aggiornare lo snapshot; investigare e correggere
- Non aggiornare mai lo snapshot per nascondere una regressione

## Output atteso

```
Regression test: PASSED (o FAILED)
Campi testati: N
Regressioni: 0 (o lista)
Miglioramenti: N (lista)
Rumore: N (lista)
```

## Nota sulla disponibilità ambiente

Se l'ambiente Python non è disponibile (venv mancante, Tesseract non nel PATH):
- Segnalare il blocco
- Non cercare di riconfigurare l'ambiente
- Eseguire solo i test unitari non-Integration
