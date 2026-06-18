# validate-project.ps1
# Controllo rapido post-modifica: build + verifica sicurezza.
# Non esegue OCR, non modifica file, non cancella nulla.
# Eseguire dalla root del repository.

param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Continue"
$hasErrors = $false

Write-Host "=== BasketPdfStats: validate-project ===" -ForegroundColor Cyan

# --- 1. Build ---
if (-not $SkipBuild) {
    Write-Host "`n[1/3] Build..." -ForegroundColor Yellow
    dotnet build .\BasketPdfStats.sln --no-restore -v minimal 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERRORE: Build fallita." -ForegroundColor Red
        $hasErrors = $true
    } else {
        Write-Host "OK: Build verde." -ForegroundColor Green
    }
} else {
    Write-Host "`n[1/3] Build saltata (--SkipBuild)." -ForegroundColor Gray
}

# --- 2. Sicurezza: config con segreti non in staging ---
Write-Host "`n[2/3] Verifica sicurezza..." -ForegroundColor Yellow

$sensitiveFiles = @(
    "Config\appsettings.json",
    "Config\appsettings.portable.json",
    ".claude\settings.local.json",
    "CLAUDE.local.md"
)

$stagedFiles = git diff --cached --name-only 2>$null
foreach ($f in $sensitiveFiles) {
    $normalized = $f.Replace("\", "/")
    if ($stagedFiles -contains $normalized) {
        Write-Host "ATTENZIONE: '$f' e' in staging. Contiene segreti? Rimuovere con: git restore --staged '$f'" -ForegroundColor Red
        $hasErrors = $true
    }
}

# Cerca pattern di token nel codice sorgente (non nei file di config)
$tokenPattern = '"token"\s*:\s*"[^"]{10,}"'
$found = Select-String -Path "src\**\*.cs", "src\**\*.json" -Pattern $tokenPattern -Recurse -ErrorAction SilentlyContinue 2>$null
if ($found) {
    Write-Host "ATTENZIONE: Possibile token hardcoded trovato in src/:" -ForegroundColor Red
    $found | ForEach-Object { Write-Host "  $($_.Filename):$($_.LineNumber)" -ForegroundColor Red }
    $hasErrors = $true
} else {
    Write-Host "OK: Nessun token hardcoded trovato in src/." -ForegroundColor Green
}

# --- 3. Verifica Adobe/GLM non reintrodotti ---
Write-Host "`n[3/3] Verifica assenza Adobe/GLM..." -ForegroundColor Yellow

$forbiddenPatterns = @("Ocr.Adobe", "AdobePdfExtract", "GlmOcr", "GLM-OCR")
$foundForbidden = $false
foreach ($pattern in $forbiddenPatterns) {
    $hits = Select-String -Path "src\**\*.cs", "src\**\*.csproj", "Config\**\*.json" -Pattern $pattern -Recurse -ErrorAction SilentlyContinue 2>$null
    if ($hits) {
        Write-Host "ERRORE: '$pattern' trovato nel codice sorgente:" -ForegroundColor Red
        $hits | ForEach-Object { Write-Host "  $($_.Filename):$($_.LineNumber): $($_.Line.Trim())" -ForegroundColor Red }
        $hasErrors = $true
        $foundForbidden = $true
    }
}
if (-not $foundForbidden) {
    Write-Host "OK: Adobe/GLM non presenti." -ForegroundColor Green
}

# --- Risultato finale ---
Write-Host "`n=== Risultato ===" -ForegroundColor Cyan
if ($hasErrors) {
    Write-Host "FALLITO: Uno o piu' controlli non superati." -ForegroundColor Red
    exit 1
} else {
    Write-Host "OK: Tutti i controlli superati." -ForegroundColor Green
    exit 0
}
