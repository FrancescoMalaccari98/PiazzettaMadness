param(
    [ValidateSet("win-arm64", "win-x64")]
    [string]$Runtime = "win-arm64",

    [string]$Configuration = "Release",
    [string]$OutputRoot = "artifacts\portable",
    [switch]$Zip
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot "..\..")).Path
$publishRoot = Join-Path $repoRoot $OutputRoot
$portableRoot = Join-Path $publishRoot "BasketPdfStats-$Runtime"
$toolsBackupRoot = Join-Path $publishRoot "_tools-backup-$Runtime"
$configBackupPath = Join-Path $publishRoot "_appsettings-backup-$Runtime.json"
$projectPath = Join-Path $repoRoot "src\BasketPdfStats.App\BasketPdfStats.App.csproj"

$runningApp = Get-Process -Name "BasketPdfStats.App" -ErrorAction SilentlyContinue
if ($runningApp) {
    throw "Close BasketPdfStats.App.exe before rebuilding the portable folder."
}

if (Test-Path $toolsBackupRoot) {
    Remove-Item $toolsBackupRoot -Recurse -Force
}

if (Test-Path $configBackupPath) {
    Remove-Item $configBackupPath -Force
}

$existingToolsRoot = Join-Path $portableRoot "tools"
if (Test-Path $existingToolsRoot) {
    Move-Item $existingToolsRoot $toolsBackupRoot -Force
}

$existingConfigPath = Join-Path $portableRoot "Config\appsettings.json"
if (Test-Path $existingConfigPath) {
    Copy-Item $existingConfigPath $configBackupPath -Force
}

if (Test-Path $portableRoot) {
    Remove-Item $portableRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $portableRoot | Out-Null

if (Test-Path $toolsBackupRoot) {
    Move-Item $toolsBackupRoot (Join-Path $portableRoot "tools") -Force
}

dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $portableRoot

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$configDir = Join-Path $portableRoot "Config"
New-Item -ItemType Directory -Force -Path $configDir | Out-Null
$configPath = Join-Path $configDir "appsettings.json"
Copy-Item (Join-Path $repoRoot "Config\appsettings.portable.json") $configPath -Force
if (Test-Path $configBackupPath) {
    $config = Get-Content $configPath -Raw | ConvertFrom-Json
    $previousConfig = Get-Content $configBackupPath -Raw | ConvertFrom-Json

    if ($config.ocrApi -and $previousConfig.ocrApi) {
        foreach ($property in @("baseUrl", "token", "matchLookupDate")) {
            if ($previousConfig.ocrApi.PSObject.Properties.Name -contains $property) {
                $config.ocrApi.$property = $previousConfig.ocrApi.$property
            }
        }

        $config | ConvertTo-Json -Depth 20 | Set-Content $configPath -Encoding UTF8
    }
}

foreach ($folder in @("fiba_pdf_to_json_programma_v2", "pdf_crop_runner", "pdf-structure")) {
    $source = Join-Path $repoRoot $folder
    $target = Join-Path $portableRoot $folder
    if (Test-Path $target) {
        Remove-Item $target -Recurse -Force
    }
    Copy-Item $source $target -Recurse -Force
}

foreach ($folder in @(
    "runtime\Working",
    "runtime\OutputJson",
    "runtime\Logs",
    "runtime\Dataset",
    "runtime\Debug",
    "tools\portable",
    "tools\python-tesseract",
    "tools\python-paddle",
    "tools\tesseract\tessdata",
    "tools\paddle-models"
)) {
    New-Item -ItemType Directory -Force -Path (Join-Path $portableRoot $folder) | Out-Null
}

Copy-Item (Join-Path $repoRoot "tools\portable\Setup-TesseractPython.ps1") (Join-Path $portableRoot "tools\portable\Setup-TesseractPython.ps1") -Force

$toolNote = @"
Portable OCR tools expected here:

- tools\python-tesseract\Scripts\python.exe
  Python virtual environment with PyMuPDF, opencv-python, pytesseract, numpy, Pillow.

- tools\tesseract\tesseract.exe
  Local Tesseract runtime.

- tools\tesseract\tessdata\eng.traineddata
  Required Tesseract language data. Add ita.traineddata if you want Italian OCR data too.

- tools\python-paddle\Scripts\python.exe
  Optional virtual environment for Paddle channels; required when Paddle is enabled in the app flow.

- tools\paddle-models\
  Optional local PaddleOCR models. Keep this populated for offline PCs.
"@

Set-Content -Path (Join-Path $portableRoot "PORTABLE_TOOLS_REQUIRED.txt") -Value $toolNote -Encoding UTF8

if ($Zip) {
    $zipPath = Join-Path $publishRoot "BasketPdfStats-$Runtime.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    Compress-Archive -Path (Join-Path $portableRoot "*") -DestinationPath $zipPath
    Write-Host "Portable ZIP created: $zipPath"
}

Write-Host "Portable folder created: $portableRoot"
