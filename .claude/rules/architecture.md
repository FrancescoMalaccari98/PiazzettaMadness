# Regole architetturali — BasketPdfStats

## Layer e dipendenze

```
Core               ← nessuna dipendenza esterna (solo BCL)
Infrastructure     ← Core
Ocr.TesseractPython ← Core, Infrastructure
Ocr.Mock           ← Core
App                ← Core, Infrastructure, Ocr.*
Tests              ← tutti
```

### Dipendenze vietate

- Core NON può dipendere da Infrastructure, App o Ocr.*
- Infrastructure NON può dipendere da App
- Nessun layer può importare direttamente classi UI WPF/WinForms
- I worker Python non devono scrivere testo diagnostico sullo stdout destinato al JSON

## Responsabilità per layer

### Core

- Interfacce: `IPdfProcessingPipeline`, `IOcrEngine`, `IStatsValidator`, `IOcrImportService`
- Modelli di dominio: `ProcessingResult`, `StatValue`, `OcrCandidate`, `GameStats`, `PlayerStats`, `TeamStats`, `EvidenceRecord`, `OcrMatchContext`, `IdentityReviewItem`
- Normalizzazione: `CanonicalStatKeyMapper`
- Identity: `PlayerIdentityMatcher`, `TeamIdentityMatcher`, `NameNormalizer`
- Validazione: `StatsValidationService`

### Infrastructure

- Pipeline: `PdfProcessingPipeline`, `DocumentPreparationStage`
- Riconciliazione: `NormalizedOcrReconciler`, `EngineWeightOptions`
- DB client: `OcrImportService` (HTTP client verso PHP REST API)
- Serializzazione: `NormalizedOcrResultWriter`, `JsonDefaults`
- FileSystem: `DocumentHasher`, `ProcessedIndexStore`, `AlreadyProcessedPdfDetector`
- Configurazione: `AppSettings`, `AppSettingsLoader`, `RuntimeOptions`

### Ocr.TesseractPython

- `TesseractFullPageOcrEngine` (CH1)
- `TesseractLayoutCropsOcrEngine` (CH2)
- `PaddleCropOcrEngine` (CH3)
- `PaddleRowOcrEngine` (CH4)
- `PythonProcessRunner`, `PythonJsonReader`
- `EvidenceRecordReader`, `EvidenceRecordMapper`

### App

- `MainWindow.xaml` / `MainWindow.xaml.cs`
- `AppComposition` (composizione manuale — da estrarre da MainWindow in Fase 9)
- `MainViewModel`
- `WpfFilePicker`, `WpfTeamMismatchConfirmationService`, `WpfAlreadyProcessedPdfDecisionService`
- `WinFormsProcessingResultPresenter`, `ResultViewerForm`

## Regole di modifica

- Non aggiungere logica specifica di un canale OCR nel dominio normalizzato (Core).
- Non creare un secondo client DB: riutilizzare `OcrImportService`.
- Non aggiungere campi specifici di un motore OCR nel modello `ProcessingResult`.
- Non aggiungere astrazioni che non risolvono un problema concreto già presente nel codice.
- Non introdurre un DI container senza prima estrarre AppComposition (Fase 9).
- Ogni nuovo motore OCR deve implementare `IOcrEngine` e non modificare `ProcessingResult` o `EvidenceRecord`.

## Flusso canonico dati

```
PHP REST API (OcrImportService)
  → OcrMatchContext (Core model)
  → OcrProcessingRequest (passa MatchContext alla pipeline)
  → IOcrEngine.ProcessAsync() → ProcessingResult (per canale)
  → NormalizedOcrReconciler.Reconcile([...]) → ProcessingResult finale
  → StatsValidationService.Validate()
  → JSON file in runtime/OutputJson/
```

## DB come fonte canonica

- Identità partite, squadre, giocatori e numeri di maglia vengono esclusivamente dal DB.
- `OcrMatchContext` è il contratto tra il caricamento DB e la pipeline.
- `PlayerIdentityMatcher` abbina righe OCR a giocatori DB: non scrive dati se il match è incerto.
- Se `MatchContext` è null o non caricato, la pipeline non deve partire.
