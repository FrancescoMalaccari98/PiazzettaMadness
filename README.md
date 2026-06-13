# BasketPdfStats

Applicazione desktop Windows per convertire PDF FIBA Live Stats in un unico JSON
normalizzato e validato.

Flusso principale:

```text
Seleziona un PDF
  -> Elabora PDF
  -> salva il JSON finale
  -> mostra il risultato
```

Il PDF originale resta nel percorso scelto dall'utente. Non esiste scansione
automatica di cartelle e non esiste una coda batch nel flusso applicativo.

## Prerequisiti

- **.NET 10 SDK** — [download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Tesseract OCR** installato e nel `PATH` (verificare con `tesseract --version`).
  Su Windows: build UB Mannheim con lingue `ita` e `eng`.
- **Python 3.10+**.

Virtual environment usati (uno per ruolo):

| Venv | Scopo |
|---|---|
| `fiba_pdf_to_json_programma_v2\fiba_pdf_to_json\.venv` | Worker Tesseract full-page (canale 1) |
| `.venv-crop` | Calibrazione layout + generazione crop (`pdf_crop_runner`) |
| `.venv-paddle` | PaddleOCR 3.6.0 (canali 3-4) |

```powershell
# Esempio: venv crop
python -m venv .venv-crop
.venv-crop\Scripts\pip install -e pdf_crop_runner
```

L'ensemble OCR usa **solo motori locali**: Tesseract e PaddleOCR. I canali
attivi si configurano da `Config/appsettings.json` (`ocr.enabledEngines` e
`reconciliation.engineWeights`).

## Direzione corrente

La baseline locale e' `TesseractFullPage`. Il progetto evolve in modo incrementale
verso un ensemble locale di quattro letture differenti (due Tesseract, due
PaddleOCR), mantenendo un solo formato normalizzato comune e un solo JSON finale.

Nessun provider OCR esterno (Adobe o altri). GLM-OCR non fa piu' parte del
percorso applicativo attivo.

## Struttura

```text
Convertitore-PDF-JSON/
  BasketPdfStats.sln
  CLAUDE.md                       <- entry point per Claude Code
  AGENTS.md                       <- regole per agenti AI
  Config/
    appsettings.json
  pdf-structure/                  <- fonte strutturale layout FIBA (read-only)
    layout-map.default.json
    layout-calibration-rules.json
    crop-zones.md
    player-table-structure.md
    table-boundary-rules.md
    player-table-columns.json
    crop-metadata.schema.json
  pdf_crop_runner/                <- tool Python calibrazione layout crop
  fiba_pdf_to_json_programma_v2/  <- worker Python OCR full-page
  runtime/                        <- generato a runtime, non committare
    Working/
    OutputJson/
    Logs/
    Dataset/
  src/
    BasketPdfStats.App/
    BasketPdfStats.Core/
    BasketPdfStats.Infrastructure/
    BasketPdfStats.Ocr.Mock/
    BasketPdfStats.Ocr.TesseractPython/  <- engine Tesseract + PaddleOCR (worker Python)
  tests/
    BasketPdfStats.Tests/
```

## Build

```powershell
dotnet build .\BasketPdfStats.sln
```

## Test

Durante lo sviluppo usare filtri mirati. Eseguire la suite completa soltanto ai
checkpoint principali.

```powershell
dotnet test .\BasketPdfStats.sln --filter "FullyQualifiedName~Tesseract|FullyQualifiedName~Pipeline|FullyQualifiedName~Preparation"
```

I test di regressione sui PDF reali sono integration test separati e non vanno
eseguiti durante le normali iterazioni:

```powershell
dotnet test .\BasketPdfStats.sln --filter Category=Integration
```

## Avvio app

```powershell
dotnet run --project .\src\BasketPdfStats.App\BasketPdfStats.App.csproj
```

## Runtime

- `runtime/Working`: copie temporanee durante l'elaborazione.
- `runtime/OutputJson`: unico JSON finale validato e indice dei documenti elaborati.
- `runtime/Logs`: log leggibili.
- `runtime/Dataset`: artifact diagnostici e intermedi opzionali.

Le cartelle legacy `Input`, `Elaborati` ed `Errori` possono ancora esistere in
runtime storiche. Non fanno parte del flusso normale e il PDF selezionato non
viene spostato.

## Tesseract

`TesseractFullPage` e' la baseline OCR locale completa e attiva. Acquisisce la
pagina PDF renderizzata intera, esegue Tesseract locale, salva il raw diagnostico
e produce il `ProcessingResult` normalizzato canonico usato dalla pipeline.
Esporta inoltre word boxes e coordinate OCR: sono geometria diagnostica per
`LayoutCalibration`, non dati partita da copiare nella strategia crop.

Gli artifact diagnostici full-page sono distinti dal JSON finale:

```text
runtime/Dataset/OcrRaw/TesseractFullPage/<documento>.<short-hash>.tesseract-full-page.raw.json
runtime/Dataset/NormalizedOcr/TesseractFullPage/<documento>.<short-hash>.normalized.json
runtime/Dataset/LayoutDebug/<documento>.<short-hash>/full-page-word-boxes.json
runtime/Dataset/LayoutDebug/<documento>.<short-hash>/full-page-word-overlay.png
```

`pdf-structure/` e' la fonte strutturale primaria: definisce le 15 zone, i ruoli
macro/semantic, l'ordine Home/Away e la struttura nota delle tabelle FIBA.
`LayoutCalibration` parte da questo contratto fisso e usa le coordinate OCR
full-page soltanto come anchor per correggere offset, scala e allineamento dei
rettangoli runtime. Non prova a riscoprire liberamente il layout del PDF.

Se gli anchor richiesti sono assenti, ambigui o incoerenti, la calibrazione deve
fallire senza generare crop affidabili solo in apparenza. Il report diagnostico
deve spiegare il problema. Le crop calibrate alimentano la seconda strategia
indipendente `TesseractLayoutCrops`. Non sono usate da `TesseractFullPage`.
`TesseractLayoutCrops` esegue OCR sull'immagine intera di ciascuna delle 15 crop
fisse, salva raw e `ProcessingResult` normalizzato diagnostici separati e
partecipa alla riconciliazione quando abilitato in `ocr.enabledEngines`:

```text
runtime/Dataset/LayoutCrops/<documento>.<short-hash>/
runtime/Dataset/LayoutImages/<documento>.<short-hash>/
runtime/Dataset/LayoutDebug/<documento>.<short-hash>/
runtime/Dataset/OcrRaw/TesseractLayoutCrops/<documento>.<short-hash>.tesseract-layout-crops.raw.json
runtime/Dataset/NormalizedOcr/TesseractLayoutCrops/<documento>.<short-hash>.normalized.json
```

Il worker full-page esistente alimenta il risultato normalizzato canonico senza
variazioni nell'estrazione. Il mapper layout-crops e' prudente e parziale: non
inventa dati non leggibili. L'estrazione per riga giocatore e' affidata al canale
PaddleOCR `PaddleTableRows`, che usa la line detection nativa sulle crop tabella.

## PaddleOCR

PaddleOCR (3.6.0) gira nel virtual environment dedicato `.venv-paddle` e alimenta
due canali aggiuntivi che consumano le stesse crop calibrate:

- `PaddleLayoutCrops` (canale 3, `ocr.paddle.crop`): OCR PaddleOCR sulle zone
  calibrate, stesso schema raw del crop worker Tesseract.
- `PaddleTableRows` (canale 4, `ocr.paddle.row`): legge le crop delle tabelle
  giocatori con la line detection nativa di PaddleOCR e produce evidence per
  riga giocatore.

Entrambi emettono lo stesso `evidence_records.json` condiviso, mappato in
`ProcessingResult` da `EvidenceRecordMapper`. Gli artifact diagnostici:

```text
runtime/Dataset/OcrRaw/PaddleLayoutCrops/<documento>.<short-hash>.paddle-layout-crops.evidence.json
runtime/Dataset/OcrRaw/PaddleTableRows/<documento>.<short-hash>.paddle-table-rows.evidence.json
```

## Preparatore crop Python

Il package leggero `pdf_crop_runner` genera crop PNG riusabili e un solo
`crop-metadata.json` calibrato per documento. Il percorso applicativo usa:

```powershell
Push-Location .\pdf_crop_runner
..\.venv-crop\Scripts\python.exe -m pdf_crop_runner.calibrate_layout `
  --pdf "<absolute-pdf-path>" `
  --word-boxes "<absolute-layout-debug-folder>\full-page-word-boxes.json" `
  --layout-map "..\pdf-structure\layout-map.default.json" `
  --output-folder "<absolute-layout-crops-folder>" `
  --image-output-folder "<absolute-layout-images-folder>" `
  --layout-debug-folder "<absolute-layout-debug-folder>" `
  --input-mode Crops `
  --max-pages 1
Pop-Location
```

Il comando prepara PNG, overlay, report e `crop-metadata.json`. Non esegue
inferenza, non carica modelli AI, non esegue OCR e non scrive il JSON finale.
La geometria richiesta deve essere gia' stata esportata da `TesseractFullPage`.
Le zone operative sono
documentate in [`pdf-structure/README.md`](pdf-structure/README.md).

Per le tabelle giocatori il confine non dipende da un numero fisso di atleti:
la riga header nota e' l'anchor superiore; `Squadra/Allenatore` e `Totali` sono
gli anchor inferiori. La tabella Home viene prima della tabella Away. Il crop
deve includere l'header delle colonne.
