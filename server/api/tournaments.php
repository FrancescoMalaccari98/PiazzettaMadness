<?php

declare(strict_types=1);

header('Content-Type: application/json; charset=utf-8');

try {
    $configPath = __DIR__ . '/config.php';
    if (!is_file($configPath)) {
        respond(500, ['error' => 'Missing API config']);
    }

    $config = require $configPath;
    requireApiKey($config['api_key'] ?? '');

    $pdo = connect($config['db'] ?? []);
    ensureSchema($pdo);

    $method = $_SERVER['REQUEST_METHOD'] ?? 'GET';
    if ($method === 'GET' && isset($_GET['health'])) {
        respond(200, [
            'ok' => true,
            'api' => 'piazzetta-madness-tournaments',
            'version' => '2026-06-03.1',
            'method' => $method,
        ]);
    }

    match ($method) {
        'GET' => listTournaments($pdo),
        'POST' => createTournament($pdo),
        'PUT' => updateTournament($pdo),
        'DELETE' => deleteTournament($pdo),
        default => respond(405, ['error' => 'Method not allowed']),
    };
} catch (PDOException $exception) {
    if ((string)$exception->getCode() === '23000') {
        respond(409, ['error' => 'Operation blocked by linked data', 'detail' => $exception->getMessage()]);
    }
    respond(500, ['error' => 'Database error', 'detail' => $exception->getMessage()]);
} catch (Throwable $exception) {
    respond(500, ['error' => 'Server error', 'detail' => $exception->getMessage()]);
}

function requireApiKey(string $expected): void
{
    $actual = $_SERVER['HTTP_X_API_KEY'] ?? '';
    if ($expected === '' || !hash_equals($expected, $actual)) {
        respond(401, ['error' => 'Unauthorized']);
    }
}

function connect(array $db): PDO
{
    foreach (['host', 'database', 'user', 'password'] as $key) {
        if (!isset($db[$key]) || $db[$key] === '') {
            respond(500, ['error' => "Missing database config: $key"]);
        }
    }

    $charset = $db['charset'] ?? 'utf8mb4';
    $dsn = "mysql:host={$db['host']};dbname={$db['database']};charset={$charset}";

    return new PDO($dsn, $db['user'], $db['password'], [
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
        PDO::ATTR_EMULATE_PREPARES => false,
    ]);
}

function ensureSchema(PDO $pdo): void
{
    $pdo->exec(
        "CREATE TABLE IF NOT EXISTS tournaments (
            id INT UNSIGNED NOT NULL AUTO_INCREMENT,
            name VARCHAR(160) NOT NULL,
            description TEXT NULL,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
            PRIMARY KEY (id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci"
    );
}

function listTournaments(PDO $pdo): void
{
    $statement = $pdo->query(
        'SELECT id, name, description, created_at, updated_at
         FROM tournaments
         ORDER BY name'
    );

    respond(200, $statement->fetchAll());
}

function createTournament(PDO $pdo): void
{
    $body = readJsonBody();
    $name = trim((string)($body['name'] ?? ''));
    $description = normalizeNullableText($body['description'] ?? null);

    if ($name === '') {
        respond(422, ['error' => 'Name is required']);
    }

    $statement = $pdo->prepare(
        'INSERT INTO tournaments (name, description) VALUES (:name, :description)'
    );
    $statement->execute([
        ':name' => $name,
        ':description' => $description,
    ]);

    getTournament($pdo, (int)$pdo->lastInsertId(), 201);
}

function updateTournament(PDO $pdo): void
{
    $id = readId();
    $body = readJsonBody();
    $name = trim((string)($body['name'] ?? ''));
    $description = normalizeNullableText($body['description'] ?? null);

    if ($name === '') {
        respond(422, ['error' => 'Name is required']);
    }

    $statement = $pdo->prepare(
        'UPDATE tournaments
         SET name = :name, description = :description
         WHERE id = :id'
    );
    $statement->execute([
        ':id' => $id,
        ':name' => $name,
        ':description' => $description,
    ]);

    if ($statement->rowCount() === 0) {
        getTournament($pdo, $id);
        return;
    }

    getTournament($pdo, $id);
}

function deleteTournament(PDO $pdo): void
{
    $id = readId();
    foreach (['match_player_stats', 'match_team_stats'] as $statsTable) {
        if (!tableExists($pdo, $statsTable)) {
            continue;
        }
        $statement = $pdo->prepare(
            "SELECT COUNT(*) FROM `$statsTable` stats
             INNER JOIN matches m ON m.id = stats.match_id
             INNER JOIN editions e ON e.id = m.edition_id
             WHERE e.tournament_id = :id"
        );
        $statement->execute([':id' => $id]);
        if ((int)$statement->fetchColumn() > 0) {
            respond(409, ['error' => 'Operation blocked by linked statistics', 'detail' => "Tournament has statistics in $statsTable"]);
        }
    }
    $statement = $pdo->prepare('DELETE FROM tournaments WHERE id = :id');
    $statement->execute([':id' => $id]);
    respond(200, ['deleted' => $statement->rowCount() > 0]);
}

function tableExists(PDO $pdo, string $table): bool
{
    $statement = $pdo->prepare('SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = :table');
    $statement->execute([':table' => $table]);
    return (int)$statement->fetchColumn() > 0;
}

function getTournament(PDO $pdo, int $id, int $status = 200): void
{
    $statement = $pdo->prepare(
        'SELECT id, name, description, created_at, updated_at
         FROM tournaments
         WHERE id = :id'
    );
    $statement->execute([':id' => $id]);
    $row = $statement->fetch();

    if (!$row) {
        respond(404, ['error' => 'Tournament not found']);
    }

    respond($status, $row);
}

function readJsonBody(): array
{
    $raw = file_get_contents('php://input');
    $body = json_decode($raw === false ? '' : $raw, true);
    if (!is_array($body)) {
        respond(400, ['error' => 'Invalid JSON body']);
    }

    return $body;
}

function readId(): int
{
    $id = filter_input(INPUT_GET, 'id', FILTER_VALIDATE_INT);
    if (!$id || $id < 1) {
        respond(400, ['error' => 'Valid id query parameter is required']);
    }

    return $id;
}

function normalizeNullableText(mixed $value): ?string
{
    if ($value === null) {
        return null;
    }

    $trimmed = trim((string)$value);
    return $trimmed === '' ? null : $trimmed;
}

function respond(int $status, mixed $payload)
{
    http_response_code($status);
    echo json_encode($payload, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    exit;
}
