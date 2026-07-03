<?php
// GET /api/merchandise
// Legge la cartella img/merchandise/ e restituisce array di URL immagini
// ordinati per data di upload (più recente prima).
// Usato solo dalla galleria in fondo alla Home (endpoint separato da /foto
// per non toccare la pagina Foto, che mostra le immagini generali del torneo).

function handle_merchandise(): void {
    $imgDir  = __DIR__ . '/../../img/merchandise/';
    $scheme  = (isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on') ? 'https' : 'http';
    $baseUrl = $scheme . '://' . $_SERVER['HTTP_HOST'] . '/img/merchandise/';

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
