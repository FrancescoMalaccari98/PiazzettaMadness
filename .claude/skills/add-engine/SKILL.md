# Skill: add-engine

## Descrizione

Aggiunge un nuovo canale OCR al progetto, garantendo che rispetti il contratto esistente e non modifichi il dominio.

## Quando usarla

Quando si vuole aggiungere un quinto (o successivo) canale OCR all'ensemble.

## Prerequisiti

- Il nuovo motore ha un worker Python funzionante che emette `evidence_records.json`
- L'interfaccia `IOcrEngine` in `src/BasketPdfStats.Core/Ocr/IOcrEngine.cs` non è cambiata
- La fase di validazione CH3/CH4 è completata (Fase 10)

## Procedura

1. Crea il worker Python nel folder `fiba_pdf_to_json_programma_v2/fiba_pdf_to_json/`
2. Verifica che emetta `evidence_records.json` con schema compatibile con `EvidenceRecord.cs`
3. Crea la classe C# in `src/BasketPdfStats.Ocr.TesseractPython/` implementando `IOcrEngine`:
   - `EngineName` deve corrispondere a un valore in `OcrStrategyNames`
   - `ProcessAsync` deve ritornare `ProcessingResult` tramite `EvidenceRecordMapper`
   - Deve gestire: canale non configurato (`NotConfigured`), timeout, errori del worker
4. Aggiungi le opzioni di configurazione (nuovo `XyzOptions.FromJsonElement(...)`)
5. Aggiungi il canale a `MainWindow.xaml.cs → CreateEngines` (o `AppComposition.Build`)
6. Aggiungi la voce in `ocr.enabledEngines` di `Config/appsettings.json`
7. Aggiungi peso in `reconciliation.engineWeights` in appsettings.json
8. Aggiorna `TASK_STATUS.md` con lo stato del nuovo canale
9. Aggiungi test:
   - Unitario: il motore ritorna `NotConfigured` quando non abilitato
   - Unitario: `EvidenceRecordMapper` mappa correttamente l'output del worker
   - Integrazione: solo se esplicitamente richiesto

## Checklist contratto

- [ ] Implementa `IOcrEngine`
- [ ] `EngineName` unico e registrato in `OcrStrategyNames`
- [ ] Non modifica `ProcessingResult`, `StatValue` o `EvidenceRecord`
- [ ] Non aggiunge campi specifici del motore nel JSON finale
- [ ] Gestisce timeout e worker non avviato
- [ ] Ha opzioni configurabili (non hardcoded)
- [ ] Disabilitabile via `ocr.enabledEngines` in appsettings.json
- [ ] Documentato in `TASK_STATUS.md`

## Divieti

- Non modificare `NormalizedOcrReconciler` per favorire il nuovo motore
- Non aggiungere campi specifici del motore nel JSON finale
- Non modificare `StatsValidationService`
- Non creare un nuovo progetto .csproj senza motivazione documentata
