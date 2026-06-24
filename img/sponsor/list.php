<?php
declare(strict_types=1);

header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Cache-Control: no-cache');

$base_url = 'https://www.piazzettamadness.it/img/sponsor/';
$extensions = ['jpg', 'jpeg', 'png', 'webp', 'JPG', 'JPEG', 'PNG', 'WEBP'];

$files = [];
foreach ($extensions as $ext) {
    foreach (glob(__DIR__ . '/*.' . $ext) ?: [] as $path) {
        $filename = basename($path);
        $name = pathinfo($filename, PATHINFO_FILENAME);
        $name = preg_replace('/-\d{10,}$/', '', $name);
        $name = preg_replace('/_[a-z-]+$/i', '', $name);
        $name = str_replace(['-', '_'], ' ', $name);
        $name = mb_strtoupper(trim($name), 'UTF-8');
        $files[] = [
            'filename' => $filename,
            'url'      => $base_url . $filename,
            'nome'     => $name,
        ];
    }
}

usort($files, fn($a, $b) => strcmp($a['nome'], $b['nome']));

echo json_encode($files, JSON_UNESCAPED_SLASHES);
