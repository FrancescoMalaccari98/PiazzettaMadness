# CLAUDE.md

Leggi **[AGENTS.md](AGENTS.md)** prima di tutto. Contiene le regole obbligatorie, l'architettura target e il sistema di caricamento contesto.

## Ambiente

- **OS:** Windows 11, PowerShell
- **Runtime C#:** .NET 10 SDK
- **UI:** WPF + WinForms (`net10.0-windows`)
- **Python:** 3.10+ richiesto per `pdf_crop_runner` e `fiba_pdf_to_json_programma_v2`
- **OCR locale:** Tesseract installato e nel PATH (`tesseract --version` per verificare)
- **Venv crop:** `.venv-crop\Scripts\python.exe` (calibrazione layout `pdf_crop_runner`)
- **Venv OCR full-page:** `fiba_pdf_to_json_programma_v2\fiba_pdf_to_json\.venv` (worker Tesseract)
- **Venv PaddleOCR:** `.venv-paddle\Scripts\python.exe` (paddleocr 3.6.0, canali 3-4)

## Comandi principali

```powershell
# Build
dotnet build .\BasketPdfStats.sln

# Test mirati (sviluppo normale)
dotnet test .\BasketPdfStats.sln --filter "FullyQualifiedName~Tesseract|FullyQualifiedName~Pipeline|FullyQualifiedName~Preparation"

# Suite completa (solo a checkpoint)
dotnet test .\BasketPdfStats.sln

# Avvio app
dotnet run --project .\src\BasketPdfStats.App\BasketPdfStats.App.csproj
```

## Contesto aggiuntivo

Carica solo il documento rilevante al task corrente (vedi AGENTS.md §Context Loading Rule):

- Architettura e workflow: [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md)
- Piano di migrazione: [MIGRATION_PLAN.md](MIGRATION_PLAN.md)
- Stato implementazione: [TASK_STATUS.md](TASK_STATUS.md)
- Regole validazione: [VALIDATION_RULES.md](VALIDATION_RULES.md)
- Piano OCR ensemble: [TESSERACT_ENSEMBLE_PLAN.md](TESSERACT_ENSEMBLE_PLAN.md)
- Struttura layout FIBA: [pdf-structure/README.md](pdf-structure/README.md)
