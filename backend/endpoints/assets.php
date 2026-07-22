<?php
// Endpoint leggeri per asset opzionali usati dal frontend.

function handle_final_cup_asset(): void {
    require_method('GET');

    $filename = 'Coppa_Finale.png';
    $scheme = (isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on') ? 'https' : 'http';
    $host = $_SERVER['HTTP_HOST'] ?? '';
    $origin = $host !== '' ? ($scheme . '://' . $host) : '';

    $candidates = [
        [
            'path' => __DIR__ . '/../../img/' . $filename,
            'src' => $origin . '/img/' . rawurlencode($filename),
        ],
        [
            'path' => __DIR__ . '/../../frontend/public/assets/' . $filename,
            'src' => '/assets/' . rawurlencode($filename),
        ],
        [
            'path' => __DIR__ . '/../../assets/' . $filename,
            'src' => '/assets/' . rawurlencode($filename),
        ],
    ];

    foreach ($candidates as $candidate) {
        if (is_file($candidate['path'])) {
            send_json([
                'found' => true,
                'src' => $candidate['src'],
            ]);
        }
    }

    send_json([
        'found' => false,
        'src' => null,
    ]);
}
