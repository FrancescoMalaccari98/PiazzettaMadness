# Validazione end-to-end CH3/CH4 (Fase 10)

Obiettivo: misurare la qualità di ogni canale OCR su PDF reali e tarare
`reconciliation.engineWeights` con dati misurati (non a sensazione). È anche il prerequisito
per rimuovere `known_names.py` (Fase 9 residua).

> Va eseguita **in locale**: richiede i venv OCR e Tesseract, non disponibili nell'ambiente di sviluppo assistito.

## Prerequisiti

- `Config/appsettings.json` con i percorsi reali dei venv e i 4 canali abilitati.
- Venv: `fiba_pdf_to_json_programma_v2/fiba_pdf_to_json/.venv` (CH1/CH2), `.venv-crop`, `.venv-paddle` (CH3/CH4).
- `tesseract --version` funzionante (Tesseract nel PATH).
- Almeno un PDF in `samples/pdf/`.

## Come eseguire

```powershell
# dalla root del repository
.\tools\run-channel-validation.ps1
```

In alternativa, direttamente:

```powershell
dotnet test .\BasketPdfStats.sln --filter "FullyQualifiedName~ChannelValidationHarnessTests" --logger "console;verbosity=detailed"
```

L'harness esegue la pipeline reale (tutti i canali) su ogni PDF di `samples/pdf/` e scrive:

- `TestResults/ChannelValidation/channel-validation-report.md` (leggibile)
- `TestResults/ChannelValidation/channel-validation-report.json` (per ulteriori analisi)

Se l'ambiente OCR non è disponibile, l'harness **esce senza fallire** stampando il motivo (nessun report).

## Come leggere il report

Per ogni PDF:

- **Canale (run)**: stato (`Success`/`Failed`/`Timeout`/`NotConfigured`) e tempo reale in ms per canale.
  - Chiavi: `TesseractFullPage` (CH1), `TesseractLayoutCrops` (CH2), `PaddleLayoutCrops` (CH3), `PaddleTableRows` (CH4).
- **Provider** (per `sourceId`): per i campi dove il reconciler ha confrontato più canali:
  - `Copertura` = quanti campi quel canale ha fornito.
  - `Accordi` = quanti coincidono col valore selezionato dalla riconciliazione.
  - `Conflitti` = forniti ma diversi dal selezionato.
  - `Accordo %` = Accordi / Copertura.
  - Chiavi: `ocr.tesseract.fullpage`, `ocr.tesseract.crop`, `ocr.paddle.crop`, `ocr.paddle.row`.

Mappa run→provider: CH1=`ocr.tesseract.fullpage`, CH2=`ocr.tesseract.crop`, CH3=`ocr.paddle.crop`, CH4=`ocr.paddle.row`.

## Come tarare i pesi

In `Config/appsettings.json` → `reconciliation.engineWeights`:

- Alza il peso (`statWeight`/`scoreWeight`) dei canali con **alta copertura e alto accordo %** sui campi che contano.
- Abbassa il peso dei canali con **molti conflitti** (forniscono valori ma spesso sbagliati) o **bassa copertura**.
- CH4 (`ocr.paddle.row`) resta attivo finché i dati non dimostrano che peggiora la qualità: decidere la rimozione SOLO con benchmark negativi su ≥3 PDF.
- Dopo ogni modifica ai pesi, rilancia l'harness e confronta `Accordo %` e conflitti.

Criterio di accettazione (dal piano): CH3 e CH4 migliorano (o non peggiorano) la qualità finale su almeno 3 PDF campione.

## Gate per la Fase 9 residua

`known_names.py` può essere rimosso (insieme al fallback in `roster_context.py`) solo **dopo** che questa
validazione, eseguita **con `--roster-json` attivo** (cioè con un `MatchContext` reale caricato dal DB),
mostra che il roster dinamico produce risultati equivalenti o migliori rispetto al fallback hardcoded.

> Nota: l'harness attuale esegue la pipeline **senza** `MatchContext` (nessuna selezione partita), quindi
> CH1 usa ancora il fallback `known_names.py`. Per validare il roster dinamico, ripetere il flusso
> dall'app (data → partita → contesto → PDF) oppure estendere l'harness per iniettare un `OcrMatchContext`
> di prova prima di rimuovere `known_names.py`.
