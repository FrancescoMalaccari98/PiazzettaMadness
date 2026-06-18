# Regole C# — BasketPdfStats

## Versione e target

- .NET 10, `net10.0` per librerie, `net10.0-windows` per App
- Nullable reference types abilitati (`<Nullable>enable</Nullable>`)
- Implicit usings abilitati

## Async/await

- Tutti i metodi che chiamano `HttpClient`, `Process.Start`, o leggono/scrivono file devono essere `async Task<T>`.
- Non usare `.Result` o `.Wait()` su `Task` nel thread WPF (deadlock).
- Il thread WPF non deve mai essere bloccato: le operazioni lunghe vanno in `Task.Run` o in `async` command.
- `PdfProcessingPipeline.ProcessPdfAsync` deve restare awaitable.

## Dependency injection

- Non è presente un DI container. La composizione è manuale in `MainWindow.xaml.cs` (target: `AppComposition.cs` in Fase 9).
- Non aggiungere `Microsoft.Extensions.DependencyInjection` senza motivazione concreta e approvazione.
- Le dipendenze vengono iniettate via costruttore; non usare service locator o proprietà statiche.

## Logging

- Non esiste ancora `ILogger<T>` nel progetto. Errori e avvisi sono catturati nei return type.
- Non aggiungere `ILogger<T>` senza un piano: prima estrarre AppComposition (Fase 9).
- Non loggare su `Console.WriteLine` in codice di produzione.
- `TestOutputHelper` è ammesso solo nei test.

## Gestione errori

- Le operazioni che possono fallire ritornano modelli di risultato (`ProcessingResult`, `OcrRunResult`, `LayoutCropPreparationResult`) con campi `Error` o `ErrorMessage`.
- Non lanciare eccezioni per errori prevedibili (canale OCR non disponibile, timeout Python).
- Catturare `Win32Exception` e `TaskCanceledException` in `PythonProcessRunner`.
- La pipeline cattura tutte le eccezioni e le registra in `OcrRunResult.Error`.

## Naming

- Classi e metodi: `PascalCase`
- Campi privati: `_camelCase`
- Costanti: `PascalCase` nelle classi, `UPPER_CASE` solo per costanti globali
- Nomi delle estratture canoniche in `OcrStrategyNames` (non stringhe letterali sparse nel codice)
- Entity ID canonici: `team:Home`, `team:Away`, `player:<side>:jersey:<number>`

## Separazione UI / servizi

- `MainViewModel` non deve conoscere `TesseractPythonOptions` o `PaddleCropOptions` direttamente.
- La UI non deve fare parsing di JSON o calcoli su statistiche.
- `WpfTeamMismatchConfirmationService` e simili: solo presentazione + input utente; la logica di validazione è in Core.
- `ResultViewerForm` legge `ProcessingResult` in sola lettura.

## Testabilità

- Tutti i servizi di Infrastructure ricevono le dipendenze via costruttore (iniettabili in test).
- Non usare `new HttpClient()` direttamente: iniettare `HttpClient` o `IHttpClientFactory`.
- I mock per i test usano `MockOcrEngine` (già presente) o implementazioni in-memory.
- Non testare CH1/CH2/CH3/CH4 con Python reale nei test unitari: usare `MockOcrEngine`.

## Immutabilità e record

- I modelli di dominio in Core usano `init` o sono record dove appropriato.
- `ProcessingResult` non è immutabile (per ragioni di compatibilità): non aggiungere mutazioni non necessarie.

## Risorse e pulizia

- I processi Python vengono terminati via `PythonProcessRunner.TryKill()` in caso di timeout.
- I file temporanei in `runtime/Working/` vengono puliti dalla pipeline dopo l'elaborazione.
- Non tenere handle aperti su file JSON o immagini oltre il necessario.
