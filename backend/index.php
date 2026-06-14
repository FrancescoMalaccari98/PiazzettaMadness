<?php
declare(strict_types=1);

require_once __DIR__ . '/config/database.php';
require_once __DIR__ . '/lib/response.php';
require_once __DIR__ . '/lib/auth.php';
require_once __DIR__ . '/lib/helpers.php';

// ── CORS headers ─────────────────────────────────────────────
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
header('Access-Control-Allow-Headers: Authorization, Content-Type');

// Handle CORS preflight
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(204);
    exit;
}

// ── URL parsing ──────────────────────────────────────────────
$uri    = parse_url($_SERVER['REQUEST_URI'] ?? '/', PHP_URL_PATH);
$uri    = rawurldecode($uri);
$path   = preg_replace('#^/api-ocr/?#', '', $uri);
$path   = trim($path, '/');
$parts  = $path !== '' ? explode('/', $path) : [];
$seg0   = $parts[0] ?? '';
$seg1   = $parts[1] ?? '';
$method = $_SERVER['REQUEST_METHOD'];

// ── Routing ──────────────────────────────────────────────────
try {

    // GET /api-ocr/health  — liveness check, no auth required
    if (($seg0 === '' || $seg0 === 'health') && $method === 'GET') {
        send_json(['status' => 'ok', 'service' => 'PiazzettaMadness OCR Import API']);
    }

    // GET /api-ocr/matches/today  — list match options for the current game day
    if ($seg0 === 'matches' && $seg1 === 'today') {
        if ($method !== 'GET') {
            send_error('Method not allowed. Use GET /api-ocr/matches/today', 405);
        }

        require_ocr_auth();

        $matches_endpoint = __DIR__ . '/endpoints/matches.php';
        if (!is_file($matches_endpoint)) {
            send_error('Match lookup endpoint file not found on server', 500);
        }

        require_once $matches_endpoint;
        handle_today_matches(get_pdo());
    }

    // POST /api-ocr/import/{match_id}  — import a ProcessingResult into the DB
    if ($seg0 === 'import') {
        if ($method !== 'POST') {
            send_error('Method not allowed. Use POST /api-ocr/import/{match_id}', 405);
        }

        require_ocr_auth();

        $match_id = filter_var($seg1, FILTER_VALIDATE_INT, ['options' => ['min_range' => 1]]);
        if ($match_id === false) {
            send_error('Invalid match_id. Expected: POST /api-ocr/import/{match_id}', 400);
        }

        require_once __DIR__ . '/endpoints/import.php';
        handle_import(get_pdo(), (int)$match_id);
    }

    send_error('Endpoint not found: ' . htmlspecialchars($path), 404);

} catch (PDOException $e) {
    error_log('[api-ocr] PDOException: ' . $e->getMessage());
    send_error('Internal database error', 500);
} catch (Throwable $e) {
    error_log('[api-ocr] Error: ' . $e->getMessage());
    send_error('Internal server error', 500);
}
