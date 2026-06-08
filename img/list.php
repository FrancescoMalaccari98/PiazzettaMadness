<?php
// Scansiona la directory corrente e restituisce le immagini come array JSON di URL assoluti.
// Da caricare una sola volta in: www.piazzettamadness.it/img/list.php

declare(strict_types=1);

header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Cache-Control: no-cache');

$base_url = 'https://www.piazzettamadness.it/img/';
$extensions = ['jpg', 'jpeg', 'png', 'webp', 'JPG', 'JPEG', 'PNG', 'WEBP'];

$files = [];
foreach ($extensions as $ext) {
    foreach (glob(__DIR__ . '/*.' . $ext) ?: [] as $path) {
        $filename = basename($path);
        if ($filename === basename(__FILE__)) continue; // salta se stesso
        $files[] = $base_url . $filename;
    }
}

sort($files);

echo json_encode($files, JSON_UNESCAPED_SLASHES);
