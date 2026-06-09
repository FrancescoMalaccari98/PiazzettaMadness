<?php
// ============================================================
// api/lib/auth.php
// Autenticazione via Bearer token per endpoint di scrittura.
// I token sono memorizzati in api_users.token_hash (SHA-256).
// ============================================================

// Richiede un token Bearer valido. Blocca se non autenticato.
// $pdo: connessione PDO
// $roles: array di ruoli ammessi (es. ['admin','scoreboard_device'])
function require_auth(PDO $pdo, array $roles = []): array {
    $header = $_SERVER['HTTP_AUTHORIZATION']
           ?? getallheaders()['Authorization']
           ?? '';

    if (!preg_match('/^Bearer\s+(.+)$/i', trim($header), $m)) {
        http_response_code(401);
        echo json_encode(['error' => 'Token mancante', 'code' => 401]);
        exit;
    }

    $token      = $m[1];
    $token_hash = hash('sha256', $token);

    $sql  = 'SELECT id, name, role FROM api_users WHERE token_hash = ? AND is_active = 1';
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$token_hash]);
    $user = $stmt->fetch();

    if (!$user) {
        http_response_code(403);
        echo json_encode(['error' => 'Token non valido', 'code' => 403]);
        exit;
    }

    if (!empty($roles) && !in_array($user['role'], $roles, true)) {
        http_response_code(403);
        echo json_encode(['error' => 'Permessi insufficienti', 'code' => 403]);
        exit;
    }

    // Aggiorna last_used_at senza bloccare la risposta
    $upd = $pdo->prepare('UPDATE api_users SET last_used_at = NOW() WHERE id = ?');
    $upd->execute([$user['id']]);

    return $user;
}
