<?php
// ============================================================
// api/lib/response.php
// Helper per inviare risposte JSON standardizzate.
// ============================================================

// Invia JSON 200 e termina.
function send_json($data, int $code = 200): void {
    http_response_code($code);
    echo json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    exit;
}

// Invia errore JSON e termina.
function send_error(string $message, int $code = 400): void {
    http_response_code($code);
    echo json_encode([
        'error' => $message,
        'code'  => $code,
    ], JSON_UNESCAPED_UNICODE);
    exit;
}

// Verifica che il metodo HTTP sia quello atteso.
function require_method(string $method): void {
    if ($_SERVER['REQUEST_METHOD'] !== strtoupper($method)) {
        send_error('Metodo non consentito', 405);
    }
}

// Legge e valida un intero positivo da $source (default: path parts).
function intval_positive(?string $raw): ?int {
    if ($raw === null || $raw === '') return null;
    $v = filter_var($raw, FILTER_VALIDATE_INT);
    return ($v !== false && $v > 0) ? (int)$v : null;
}
