<?php
declare(strict_types=1);

header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Cache-Control: no-cache');

$base_url = 'https://www.piazzettamadness.it/img/giocatori/';
$extensions = ['jpg', 'jpeg', 'png', 'webp', 'JPG', 'JPEG', 'PNG', 'WEBP'];

$files = [];
foreach ($extensions as $ext) {
    foreach (glob(__DIR__ . '/*.' . $ext) ?: [] as $path) {
        $filename = basename($path);
        $files[] = [
            'filename' => $filename,
            'url'      => $base_url . rawurlencode($filename),
        ];
    }
}

usort($files, fn($a, $b) => strcmp($a['filename'], $b['filename']));

echo json_encode($files, JSON_UNESCAPED_SLASHES);
