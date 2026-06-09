<?php
// ============================================================
// api/config/cors.php
// Imposta gli header CORS e Content-Type per ogni risposta.
// In produzione sostituire '*' con il dominio del sito.
// ============================================================

function set_cors_headers(): void {
    // Origini consentite: in produzione usa 'https://piazzettamadness.it'
    $allowed_origin = '*';

    header('Access-Control-Allow-Origin: ' . $allowed_origin);
    header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
    header('Access-Control-Allow-Headers: Content-Type, Authorization, X-Requested-With');
    header('Access-Control-Max-Age: 86400');
    header('Content-Type: application/json; charset=utf-8');
    header('X-Content-Type-Options: nosniff');

    // Risponde subito alle preflight OPTIONS
    if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
        http_response_code(204);
        exit;
    }
}
