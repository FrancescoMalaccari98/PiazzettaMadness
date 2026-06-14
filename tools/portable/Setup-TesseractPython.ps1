param(
    [string]$PythonCommand = "py -3.12-64",
    [string]$TesseractSource = "C:\Program Files\Tesseract-OCR"
)

$ErrorActionPreference = "Stop"

$portableRoot = (Get-Location).Path
$appPath = Join-Path $portableRoot "BasketPdfStats.App.exe"
if (-not (Test-Path $appPath)) {
    throw "Run this script from the generated portable folder, next to BasketPdfStats.App.exe."
}

$pythonVenv = Join-Path $portableRoot "tools\python-tesseract"
$requirements = Join-Path $portableRoot "fiba_pdf_to_json_programma_v2\fiba_pdf_to_json\requirements.txt"
$venvPython = Join-Path $pythonVenv "Scripts\python.exe"

if (-not (Test-Path $venvPython)) {
    Invoke-Expression "$PythonCommand -m venv `"$pythonVenv`""
}

& $venvPython -m pip install --upgrade pip
& $venvPython -m pip install -r $requirements

if (Test-Path $TesseractSource) {
    $target = Join-Path $portableRoot "tools\tesseract"
    robocopy $TesseractSource $target /E | Out-Host
    if ($LASTEXITCODE -gt 7) {
        throw "robocopy failed while copying Tesseract with exit code $LASTEXITCODE."
    }
}
else {
    Write-Warning "Tesseract source folder not found: $TesseractSource"
}

& $venvPython -c "import fitz, cv2, pytesseract, PIL, numpy; print('python OCR ok')"

$tesseractExe = Join-Path $portableRoot "tools\tesseract\tesseract.exe"
if (Test-Path $tesseractExe) {
    & $tesseractExe --version
}
else {
    Write-Warning "Portable tesseract.exe not found. Install Tesseract or copy it into tools\tesseract."
}
