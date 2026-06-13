<?php
declare(strict_types=1);

/**
 * Sends a JSON response with the given HTTP status code and terminates.
 */
function send_json(mixed $data, int $status = 200): never
{
    http_response_code($status);
    header('Content-Type: application/json; charset=utf-8');
    echo json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    exit;
}

/**
 * Sends a JSON error response and terminates.
 */
function send_error(string $message, int $status = 400): never
{
    send_json(['error' => $message], $status);
}
