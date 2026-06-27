#requires -Version 5.1
<#
.SYNOPSIS
    Crea un pacchetto portable dell'app WPF Basket PDF Stats (Windows x64).

.DESCRIPTION
    Esegue `dotnet publish` dell'app, raccoglie i file in release/app-portable/,
    aggiunge un template di configurazione SENZA segreti e un README di lancio, e
    facoltativamente produce uno zip. Non include mai Config/appsettings.json reale,
    PDF privati, dump SQL, samples/private/, TestResults/ o release/backend.zip.

.PARAMETER Configuration
    Configurazione di build. Default: Release.

.PARAMETER Runtime
    RID di pubblicazione. Default: win-x64.

.PARAMETER SelfContained
    $true  (default) = pacchetto autonomo, NON richiede .NET installato (più grande).
    $false           = framework-dependent, più leggero ma richiede .NET 10 Desktop Runtime.

.PARAMETER Zip
    Se presente, crea anche release/BasketPdfStats-portable.zip.

.EXAMPLE
    .\tools\build-app-portable.ps1
.EXAMPLE
    .\tools\build-app-portable.ps1 -SelfContained:$false -Zip
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [bool]$SelfContained = $true,
    [switch]$Zip
)

$ErrorActionPreference = 'Stop'

# --- Percorsi (lo script vive in tools/) ---
$RepoRoot   = Split-Path -Parent $PSScriptRoot
$Project    = Join-Path $RepoRoot 'src/BasketPdfStats.App/BasketPdfStats.App.csproj'
$ReleaseDir = Join-Path $RepoRoot 'release'
$OutputDir  = Join-Path $ReleaseDir 'app-portable'
$ConfigSrc  = Join-Path $RepoRoot 'Config'

if (-not (Test-Path $Project)) {
    throw "Progetto app non trovato: $Project"
}

Write-Host "== Build portable Basket PDF Stats ==" -ForegroundColor Cyan
Write-Host "Configuration : $Configuration"
Write-Host "Runtime       : $Runtime"
Write-Host "SelfContained : $SelfContained"
Write-Host "Output        : $OutputDir"
Write-Host ""

# --- Pulizia output precedente ---
if (Test-Path $OutputDir) {
    Write-Host "Pulisco output precedente..."
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# --- Publish ---
$scArg = if ($SelfContained) { 'true' } else { 'false' }
Write-Host "Eseguo dotnet publish (self-contained=$scArg)..." -ForegroundColor Cyan
dotnet publish $Project `
    -c $Configuration `
    -r $Runtime `
    --self-contained $scArg `
    -o $OutputDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish fallito (exit $LASTEXITCODE). Vedi l'output sopra."
}

$exePath = Join-Path $OutputDir 'BasketPdfStats.App.exe'
if (-not (Test-Path $exePath)) {
    throw "Publish completato ma BasketPdfStats.App.exe non trovato in $OutputDir."
}

# --- Config template SENZA segreti ---
# Si usa appsettings.portable.json (token/baseUrl vuoti) come template e come appsettings.json
# di partenza, così l'app si avvia. NON si copia mai Config/appsettings.json reale.
$portable = Join-Path $ConfigSrc 'appsettings.portable.json'
$destConfigDir = Join-Path $OutputDir 'Config'
New-Item -ItemType Directory -Force -Path $destConfigDir | Out-Null

if (Test-Path $portable) {
    Copy-Item $portable (Join-Path $destConfigDir 'appsettings.portable.json') -Force
    Copy-Item $portable (Join-Path $destConfigDir 'appsettings.json') -Force
    Write-Host "Config template copiato (appsettings.portable.json -> Config/, niente segreti)."
} else {
    Write-Warning "Config/appsettings.portable.json non trovato: il pacchetto non includerà un template di config."
}

$example = Join-Path $ConfigSrc 'appsettings.example.json'
if (Test-Path $example) {
    Copy-Item $example (Join-Path $destConfigDir 'appsettings.example.json') -Force
}

# Guardia anti-segreti: assicura che il file reale non finisca nel pacchetto.
$leak = Join-Path $destConfigDir 'appsettings.json'
$realConfig = Join-Path $ConfigSrc 'appsettings.json'
if ((Test-Path $leak) -and (Test-Path $realConfig)) {
    $leakText = Get-Content $leak -Raw
    if ($leakText -match '"token"\s*:\s*"[^"]+"') {
        throw "Il Config/appsettings.json del pacchetto contiene un token non vuoto: pacchetto NON sicuro. Interrompo."
    }
}

# --- README di lancio ---
$readmePath = Join-Path $OutputDir 'README-LANCIO.txt'
$readme = @'
BASKET PDF STATS - PACCHETTO PORTABLE
=====================================

COME LANCIARE
-------------
1. Copia l'intera cartella "app-portable" sul PC Windows di destinazione.
2. Apri il file:  BasketPdfStats.App.exe
   (doppio clic). Non serve installare nulla se il pacchetto e' self-contained.

CONFIGURAZIONE (Config\appsettings.json)
----------------------------------------
- Accanto all'exe trovi la cartella "Config".
- E' incluso un TEMPLATE senza segreti: Config\appsettings.portable.json,
  gia' copiato anche come Config\appsettings.json per far partire l'app.
- Il Config\appsettings.json REALE (con token API e percorsi personali) NON e'
  incluso nel pacchetto perche' locale/segreto. Compila tu i campi:
    ocrApi.baseUrl  -> URL del backend PHP (es. https://tuo-dominio/api-ocr)
    ocrApi.token    -> bearer token del backend
  Senza questi due valori l'import nel DB e il caricamento partite restano disattivati,
  ma l'app si avvia comunque.

PREREQUISITI OCR (NON inclusi nel pacchetto)
--------------------------------------------
L'elaborazione PDF richiede strumenti OCR esterni, da fornire separatamente e
referenziare nei percorsi di Config\appsettings.json:
  - Tesseract OCR            (ocr.tesseractPython.tesseractExecutableFolder / tessdataPrefix)
  - Python venv "Tesseract"  (ocr.tesseractPython.pythonExecutablePath, + worker in fiba_pdf_to_json_programma_v2)
  - Python venv "Paddle"     (ocr.paddleCrop.pythonExecutablePath) per i canali PaddleOCR
I percorsi nel template puntano a cartelle "tools\..." relative alla cartella dell'app.
Posiziona li' gli strumenti oppure aggiorna i percorsi nel config.

SE L'APP NON TROVA OCR O BACKEND
--------------------------------
- "Import DB disattivato" / nessuna partita: ocrApi.baseUrl o token vuoti/errati.
- Errori in "Warning / Errori" durante l'elaborazione: Tesseract/Python non trovati
  o percorsi sbagliati nel config. Verifica i percorsi e che i venv esistano.
- L'app si avvia comunque: puoi configurare e riprovare senza reinstallare.

APP PORTABLE vs BACKEND ARUBA
-----------------------------
- Questo pacchetto e' SOLO l'app desktop Windows (lettura PDF -> JSON -> import).
- Il backend PHP/MySQL su Aruba e' un componente SEPARATO (cartella backend/ del
  repo, pacchettizzato a parte in release/backend.zip) e NON e' incluso qui.
- L'app comunica col backend via HTTP usando ocrApi.baseUrl/token. Il deploy del
  backend su Aruba e' un'operazione distinta, non gestita da questo pacchetto.
'@
Set-Content -Path $readmePath -Value $readme -Encoding UTF8
Write-Host "README di lancio scritto: $readmePath"

# --- Zip opzionale ---
if ($Zip) {
    $zipPath = Join-Path $ReleaseDir 'BasketPdfStats-portable.zip'
    if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
    Write-Host "Creo zip: $zipPath" -ForegroundColor Cyan
    Compress-Archive -Path (Join-Path $OutputDir '*') -DestinationPath $zipPath -Force
    Write-Host "Zip creato: $zipPath"
}

# --- Riepilogo ---
$sizeMb = [math]::Round(((Get-ChildItem -Recurse $OutputDir | Measure-Object Length -Sum).Sum / 1MB), 1)
Write-Host ""
Write-Host "== Pacchetto portable pronto ==" -ForegroundColor Green
Write-Host "Cartella : $OutputDir ($sizeMb MB)"
Write-Host "Exe      : $exePath"
if ($Zip) { Write-Host "Zip      : $(Join-Path $ReleaseDir 'BasketPdfStats-portable.zip')" }
Write-Host ""
Write-Host "Avvio: apri BasketPdfStats.App.exe. Configura Config\appsettings.json per backend e OCR."
