# validate-python.ps1
# Verifica la disponibilita' degli ambienti Python necessari.
# Non esegue OCR, non modifica file, non cancella nulla.
# Eseguire dalla root del repository.

$ErrorActionPreference = "Continue"
$hasErrors = $false

Write-Host "=== BasketPdfStats: validate-python ===" -ForegroundColor Cyan

# --- Venv Tesseract (CH1 e CH2) ---
Write-Host "`n[1/3] Venv Tesseract (fiba_pdf_to_json)..." -ForegroundColor Yellow
$tesseractPython = "fiba_pdf_to_json_programma_v2\fiba_pdf_to_json\.venv\Scripts\python.exe"
if (Test-Path $tesseractPython) {
    $version = & $tesseractPython --version 2>&1
    Write-Host "OK: $version" -ForegroundColor Green
} else {
    Write-Host "MANCANTE: $tesseractPython" -ForegroundColor Red
    Write-Host "  Setup: cd fiba_pdf_to_json_programma_v2\fiba_pdf_to_json && python -m venv .venv && .venv\Scripts\pip install -e ." -ForegroundColor Gray
    $hasErrors = $true
}

# --- Venv crop (calibrazione layout) ---
Write-Host "`n[2/3] Venv crop (.venv-crop)..." -ForegroundColor Yellow
$cropPython = ".venv-crop\Scripts\python.exe"
if (Test-Path $cropPython) {
    $version = & $cropPython --version 2>&1
    Write-Host "OK: $version" -ForegroundColor Green
} else {
    Write-Host "MANCANTE: $cropPython" -ForegroundColor Red
    Write-Host "  Setup: python -m venv .venv-crop && .venv-crop\Scripts\pip install -e pdf_crop_runner" -ForegroundColor Gray
    $hasErrors = $true
}

# --- Venv Paddle (CH3 e CH4) ---
Write-Host "`n[3/3] Venv PaddleOCR (.venv-paddle)..." -ForegroundColor Yellow
$paddlePython = ".venv-paddle\Scripts\python.exe"
if (Test-Path $paddlePython) {
    $version = & $paddlePython --version 2>&1
    Write-Host "OK: $version" -ForegroundColor Green
    # Verifica paddleocr
    $paddleVersion = & $paddlePython -c "import paddleocr; print(paddleocr.__version__)" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "OK: paddleocr $paddleVersion" -ForegroundColor Green
    } else {
        Write-Host "ATTENZIONE: paddleocr non importabile nel venv paddle." -ForegroundColor Yellow
    }
} else {
    Write-Host "MANCANTE: $paddlePython" -ForegroundColor Red
    Write-Host "  Setup: vedere tools\portable\Setup-TesseractPython.ps1" -ForegroundColor Gray
    $hasErrors = $true
}

# --- Tesseract nel PATH ---
Write-Host "`nVerifica Tesseract nel PATH..." -ForegroundColor Yellow
$tesseractCmd = Get-Command tesseract -ErrorAction SilentlyContinue
if ($tesseractCmd) {
    $tesseractVer = tesseract --version 2>&1 | Select-Object -First 1
    Write-Host "OK: $tesseractVer" -ForegroundColor Green
} else {
    Write-Host "ATTENZIONE: tesseract non trovato nel PATH. CH1 potrebbe non funzionare." -ForegroundColor Yellow
}

# --- Risultato ---
Write-Host "`n=== Risultato ===" -ForegroundColor Cyan
if ($hasErrors) {
    Write-Host "ATTENZIONE: Uno o piu' ambienti Python non disponibili." -ForegroundColor Yellow
    Write-Host "I test Integration e le elaborazioni PDF reali non funzioneranno." -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "OK: Tutti gli ambienti Python disponibili." -ForegroundColor Green
    exit 0
}
