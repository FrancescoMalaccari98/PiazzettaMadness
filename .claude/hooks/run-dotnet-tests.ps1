# run-dotnet-tests.ps1
# Esegue i test .NET con livello di intensita' configurabile.
# Non esegue OCR, non modifica file, non cancella nulla.
# Eseguire dalla root del repository.

param(
    [ValidateSet("targeted", "full")]
    [string]$Level = "targeted"
)

$ErrorActionPreference = "Continue"

Write-Host "=== BasketPdfStats: run-dotnet-tests (Level=$Level) ===" -ForegroundColor Cyan

# Verifica prerequisiti
if (-not (Test-Path "BasketPdfStats.sln")) {
    Write-Host "ERRORE: BasketPdfStats.sln non trovato. Eseguire dalla root del repository." -ForegroundColor Red
    exit 1
}

if ($Level -eq "targeted") {
    Write-Host "`nEsecuzione test mirati (Pipeline, Tesseract, Preparation)..." -ForegroundColor Yellow
    dotnet test .\BasketPdfStats.sln `
        --filter "FullyQualifiedName~Tesseract|FullyQualifiedName~Pipeline|FullyQualifiedName~Preparation" `
        --logger "console;verbosity=minimal" `
        --no-build
} elseif ($Level -eq "full") {
    Write-Host "`nEsecuzione suite completa (ESCLUSI test Integration)..." -ForegroundColor Yellow
    dotnet test .\BasketPdfStats.sln `
        --filter "Category!=Integration" `
        --logger "console;verbosity=minimal" `
        --no-build
}

$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "`nOK: Test superati." -ForegroundColor Green
} else {
    Write-Host "`nERRORE: Uno o piu' test falliti (exit code $exitCode)." -ForegroundColor Red
}

exit $exitCode
