# run-channel-validation.ps1
# Esegue l'harness di validazione end-to-end CH1-CH4 (Fase 10) su samples/pdf/.
# Richiede i venv OCR (.venv, .venv-crop, .venv-paddle) e Tesseract nel PATH.
# Eseguire dalla root del repository.

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

Write-Host "=== Validazione canali CH1-CH4 (Fase 10) ===" -ForegroundColor Cyan

if (-not (Test-Path (Join-Path $repo "Config\appsettings.json"))) {
    Write-Host "ERRORE: Config/appsettings.json mancante (necessario per i percorsi dei venv)." -ForegroundColor Red
    exit 1
}

# Verifica Tesseract nel PATH (CH1 obbligatorio)
$tess = Get-Command tesseract -ErrorAction SilentlyContinue
if (-not $tess) {
    Write-Host "ATTENZIONE: tesseract non nel PATH. CH1 fallirà e l'harness uscirà senza report." -ForegroundColor Yellow
}

Write-Host "`nEsecuzione harness (puo' richiedere minuti: lancia tutti i canali su ogni PDF)..." -ForegroundColor Yellow
dotnet test (Join-Path $repo "BasketPdfStats.sln") `
    --filter "FullyQualifiedName~ChannelValidationHarnessTests" `
    --logger "console;verbosity=detailed"

$reportMd = Join-Path $repo "TestResults\ChannelValidation\channel-validation-report.md"
$reportJson = Join-Path $repo "TestResults\ChannelValidation\channel-validation-report.json"

Write-Host "`n=== Report ===" -ForegroundColor Cyan
if (Test-Path $reportMd) {
    Write-Host "Markdown: $reportMd" -ForegroundColor Green
    Write-Host "JSON:     $reportJson" -ForegroundColor Green
    Write-Host "`n--- Anteprima ---`n" -ForegroundColor Gray
    Get-Content $reportMd
} else {
    Write-Host "Nessun report generato: l'ambiente OCR potrebbe non essere disponibile (vedi output sopra)." -ForegroundColor Yellow
}
