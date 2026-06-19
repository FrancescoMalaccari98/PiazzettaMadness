# build-backend-package.ps1
# Genera release/backend.zip dal sorgente aggiornato in backend/ (Fase 8B).
# NON tocca backend/backend.zip (snapshot storico). NON include segreti.
# Eseguire dalla root del repository.

$ErrorActionPreference = "Stop"

$repo       = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$backendDir = Join-Path $repo "backend"
$releaseDir = Join-Path $repo "release"
$zipPath    = Join-Path $releaseDir "backend.zip"
$manifest   = Join-Path $releaseDir "backend-manifest.txt"

# File inclusi nel pacchetto (relativi a backend/). database.php reale ESCLUSO.
$included = @(
    ".htaccess",
    "index.php",
    "config/database.example.php",
    "endpoints/matches.php",
    "endpoints/context.php",
    "endpoints/import.php",
    "lib/auth.php",
    "lib/helpers.php",
    "lib/response.php"
)

$errors = @()

Write-Host "=== build-backend-package (Fase 8B) ===" -ForegroundColor Cyan

# --- Presenza dei file richiesti ---
foreach ($rel in $included) {
    $full = Join-Path $backendDir $rel
    if (-not (Test-Path $full)) {
        $errors += "File mancante: backend/$rel"
    }
}

# --- Controlli di contenuto ---
$indexPhp = Get-Content (Join-Path $backendDir "index.php") -Raw
foreach ($route in @("health", "matches", "context", "import")) {
    if ($indexPhp -notmatch [regex]::Escape($route)) {
        $errors += "Route '$route' non trovata in index.php"
    }
}
$importPhp = Get-Content (Join-Path $backendDir "endpoints/import.php") -Raw
if ($importPhp -notmatch "Invalid canonical player IDs") {
    $errors += "endpoints/import.php non sembra la versione Fase 8 (manca la verifica canonica/422)"
}

# --- php -l su tutti i file PHP (se PHP disponibile) ---
$phpExe = (Get-Command php -ErrorAction SilentlyContinue)
$phpLintStatus = "NON ESEGUITO (php non disponibile in locale)"
if ($phpExe) {
    $phpLintStatus = "OK"
    foreach ($rel in $included) {
        if ($rel.EndsWith(".php")) {
            $out = & php -l (Join-Path $backendDir $rel) 2>&1
            if ($LASTEXITCODE -ne 0) {
                $errors += "php -l fallito su backend/$rel : $out"
                $phpLintStatus = "FALLITO"
            }
        }
    }
}
Write-Host "php -l: $phpLintStatus" -ForegroundColor Yellow

# --- Scansione segreti nei file inclusi (no stampa dei valori) ---
$secretPattern = 'pm_2026_[A-Za-z0-9]+|DB_PASS\s*[=,]\s*[''"][^''"]{3,}|password\s*=>\s*[''"][^''"]{3,}'
foreach ($rel in $included) {
    $full = Join-Path $backendDir $rel
    if (Test-Path $full) {
        $hit = Select-String -Path $full -Pattern $secretPattern -ErrorAction SilentlyContinue
        if ($hit) {
            $errors += "Possibile segreto in backend/$rel (riga $($hit[0].LineNumber)) - NON impacchettare"
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host "`nERRORI:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

# --- Zip (voci con separatore '/', compatibile Linux/Aruba) ---
if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null }
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($rel in $included) {
        $entryName = $rel.Replace("\", "/")
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $backendDir $rel), $entryName) | Out-Null
    }
} finally {
    $archive.Dispose()
}

# --- Hash + manifest ---
$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash
$commit = (& git -C $repo rev-parse --short HEAD 2>$null)
$now = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss zzz")

$lines = @()
$lines += "BasketPdfStats backend package manifest"
$lines += "Generato:        $now"
$lines += "Commit Git:      $commit"
$lines += "Fase:            8B"
$lines += "Hash SHA-256:    $hash"
$lines += "php -l:          $phpLintStatus"
$lines += ""
$lines += "Endpoint inclusi:"
$lines += "  GET  /api-ocr/health"
$lines += "  GET  /api-ocr/matches/today?date=YYYY-MM-DD"
$lines += "  GET  /api-ocr/matches/{id}/context"
$lines += "  POST /api-ocr/import/{id}"
$lines += ""
$lines += "File inclusi:"
$included | ForEach-Object { $lines += "  $_" }
$lines += ""
$lines += "Esclusi (sicurezza): config/database.php, token, .env, log, cache, backup, test, .git, .claude, backend.zip"
$lines | Set-Content -Path $manifest -Encoding utf8

Write-Host "`nOK: pacchetto creato." -ForegroundColor Green
Write-Host "  ZIP:      $zipPath"
Write-Host "  Manifest: $manifest"
Write-Host "  SHA-256:  $hash"
