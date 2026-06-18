# Regole testing — BasketPdfStats

## Framework e strumenti

- xUnit 2.9.3
- coverlet.collector 6.0.4
- Nessuna libreria di mock esterna (Moq ecc.): usare `MockOcrEngine` in `BasketPdfStats.Ocr.Mock`

## Comandi test

```powershell
# Test mirati — sviluppo normale
dotnet test .\BasketPdfStats.sln --filter "FullyQualifiedName~Tesseract|FullyQualifiedName~Pipeline|FullyQualifiedName~Preparation"

# Test completi — checkpoint o pre-commit
dotnet test .\BasketPdfStats.sln

# Test singolo per nome
dotnet test .\BasketPdfStats.sln --filter "FullyQualifiedName~NomeTestEsatto"
```

## Categorie test

| Categoria | Trait | Quando eseguire |
|-----------|-------|----------------|
| Unitari | nessun trait | sempre |
| Integrazione (Python/OCR) | `[Trait("Category", "Integration")]` | solo su richiesta esplicita |
| Regressione PDF | `[Trait("Category", "Integration")]` | solo su richiesta esplicita o checkpoint |

Non eseguire i test `Integration` durante sviluppo normale: richiedono Python, venv, Tesseract e PDF di campione.

## Test obbligatori per ogni modifica

### Modifica a pipeline o riconciliazione
- Eseguire test completi: `dotnet test .\BasketPdfStats.sln`
- Verificare che `NormalizedOcrReconcilerTests` e `MultiEnginePipelineTests` siano verdi

### Modifica a un motore OCR (CH1-CH4)
- Eseguire test con filter corrispondente: `~Tesseract` o `~Paddle`
- Aggiungere test unitario che copre il caso modificato

### Modifica al modello `ProcessingResult` o `EvidenceRecord`
- Eseguire `ProcessingResultSerializationTests`
- Verificare `EvidenceRecordReaderTests`
- Aggiornare `samples/test_import_match1.json` se il contratto JSON cambia (con approvazione)

### Modifica a `StatsValidationService`
- Eseguire tutti i test di validazione
- Verificare che i test di regressione su PDF campione siano ancora verdi

### Nuova feature (PlayerIdentityMatcher, MatchContext, ecc.)
- Aggiungere test unitari prima di integrare nella pipeline
- Soglie fuzzy matching: validare su `samples/pdf/` prima di scegliere valori definitivi

## Dataset di regressione

- PDF campione: `samples/pdf/TABELLINO FINALE 1-2 POSTO.pdf`
- Output atteso: `samples/test_import_match1.json`
- I test di regressione (`TesseractPythonSampleRegressionTests`, `TesseractLayoutCropsSampleRegressionTests`) confrontano l'output JSON con lo snapshot

## Snapshot e aggiornamento output atteso

- Non aggiornare `samples/test_import_match1.json` solo per far passare i test.
- Aggiornare lo snapshot solo quando il cambiamento nel JSON è intenzionale e approvato.
- Documentare nel commit perché il JSON è cambiato.

## Test di regressione: confronto output

- I confronti di output devono ignorare: timestamp, hash del documento, percorsi di file.
- Devono essere sensibili a: valori statistici, nomi giocatori, numeri di maglia, struttura JSON.

## Ambienti Python

- Se un venv Python non è disponibile, non cercare di ricrearlo: segnalare il blocco.
- Non eseguire test integration se l'ambiente OCR non è configurato: usare `MockOcrEngine`.
- Non riprovare test falliti per problemi di ambiente: diagnosticare la causa.

## Cosa verificare prima di dichiarare un test "passato"

- Il test deve essere stato eseguito (`dotnet test`) con exit code 0.
- Non dichiarare "test verdi" senza aver effettivamente eseguito il comando.
- Se i test non possono essere eseguiti (ambiente bloccato), dichiararlo esplicitamente.
