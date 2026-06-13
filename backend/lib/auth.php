<?php
declare(strict_types=1);

/**
 * Validates the Bearer token from the Authorization header.
 *
 * On Aruba shared hosting (FastCGI), the Authorization header is exposed via
 * the REDIRECT_HTTP_AUTHORIZATION server variable instead of HTTP_AUTHORIZATION.
 * Both are checked to ensure compatibility.
 *
 * Terminates with 500 if OCR_API_TOKEN is not configured on the server.
 * Terminates with 401 if the token is missing or does not match.
 * Uses hash_equals() for timing-safe comparison to prevent timing attacks.
 */
function require_ocr_auth(): void
{
    // Prefer HTTP_AUTHORIZATION; fall back to REDIRECT_HTTP_AUTHORIZATION (Apache FastCGI)
    $header = $_SERVER['HTTP_AUTHORIZATION'] ?? $_SERVER['REDIRECT_HTTP_AUTHORIZATION'] ?? '';
    $token  = '';

    if (str_starts_with($header, 'Bearer ')) {
        $token = trim(substr($header, 7));
    }

    // Guard: server-side token must be configured before accepting any request
    if (!defined('OCR_API_TOKEN') || OCR_API_TOKEN === '') {
        send_error('OCR_API_TOKEN not configured on the server', 500);
    }

    // Timing-safe token comparison
    if ($token === '' || !hash_equals(OCR_API_TOKEN, $token)) {
        send_error('Unauthorized', 401);
    }
}
