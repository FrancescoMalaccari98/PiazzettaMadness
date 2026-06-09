<?php
// GET /api/foto
// Legge la cartella img/ (public_html/img/) e restituisce array di URL immagini
// ordinati per data di upload (più recente prima).

function handle_foto(): void {
    $imgDir  = __DIR__ . '/../../img/';
    $scheme  = (isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on') ? 'https' : 'http';
    $baseUrl = $scheme . '://' . $_SERVER['HTTP_HOST'] . '/img/';

    $allowed = ['jpg', 'jpeg', 'png', 'gif', 'webp'];
    $files   = [];

    if (is_dir($imgDir)) {
        foreach (scandir($imgDir) as $file) {
            if ($file[0] === '.') continue;
            $ext = strtolower(pathinfo($file, PATHINFO_EXTENSION));
            if (in_array($ext, $allowed, true)) {
                $files[] = ['name' => $file, 'mtime' => filemtime($imgDir . $file)];
            }
        }
        usort($files, fn($a, $b) => $b['mtime'] - $a['mtime']);
    }

    send_json(array_map(fn($f) => $baseUrl . rawurlencode($f['name']), $files));
}
