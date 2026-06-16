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
        respond(200, ['ok' => true, 'api' => 'piazzetta-madness-snapshot', 'version' => '2026-06-03.1']);
    }

    match ($method) {
        'GET' => getSnapshot($pdo),
        'POST', 'PUT' => respond(410, ['error' => 'Snapshot writes are disabled; use incremental REST operations']),
        default => respond(405, ['error' => 'Method not allowed']),
    };
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

function tableColumns(): array
{
    return [
        'tournaments' => ['id', 'name', 'description', 'created_at', 'updated_at'],
        'editions' => ['id', 'tournament_id', 'name', 'year', 'start_date', 'end_date', 'status', 'created_at', 'updated_at'],
        'courts' => ['id', 'edition_id', 'name', 'location'],
        'teams' => ['id', 'edition_id', 'name', 'short_name', 'primary_color', 'secondary_color', 'logo_path', 'created_at', 'updated_at'],
        'sponsors' => ['id', 'name', 'description', 'image_path', 'is_active', 'sort_order', 'created_at', 'updated_at'],
        'players' => ['id', 'first_name', 'last_name', 'nickname', 'fiscal_code', 'address', 'phone_number', 'email', 'birth_date', 'photo_path', 'created_at', 'updated_at'],
        'team_rosters' => ['id', 'team_id', 'player_id', 'jersey_number', 'role', 'is_captain', 'is_active', 'created_at', 'updated_at'],
        'tournament_groups' => ['id', 'edition_id', 'name', 'code', 'sort_order'],
        'group_teams' => ['id', 'group_id', 'team_id', 'seed_label'],
        'standings' => ['id', 'group_id', 'team_id', 'played', 'wins', 'losses', 'points_for', 'points_against', 'point_difference', 'ranking_points', 'position', 'tie_break_note'],
        'matches' => ['id', 'edition_id', 'group_id', 'court_id', 'name', 'phase', 'round', 'scheduled_start_at', 'scheduled_end_at', 'status', 'period_duration_ms', 'shot_clock_ms', 'max_score', 'max_score_enabled', 'winner_team_id', 'win_reason', 'created_at', 'updated_at'],
        'match_teams' => ['id', 'match_id', 'team_id', 'side', 'score', 'fouls_current_period', 'timeouts_used_total', 'timeouts_used_period', 'is_winner', 'forfeit_score'],
        'match_players' => ['id', 'match_id', 'team_id', 'player_id', 'jersey_number', 'is_starting_five', 'is_on_court', 'points', 'personal_fouls', 'is_fouled_out', 'is_ejected'],
        'match_events' => ['id', 'match_id', 'period', 'period_type', 'game_clock_ms_remaining', 'shot_clock_ms_remaining', 'team_id', 'player_id', 'event_type', 'points', 'is_correction', 'reverts_event_id', 'description', 'payload_json', 'created_at', 'synced_at'],
        'scoreboard_states' => ['id', 'match_id', 'current_period', 'current_period_type', 'game_clock_ms_remaining', 'shot_clock_ms_remaining', 'is_game_clock_running', 'is_shot_clock_running', 'home_score', 'away_score', 'last_updated_at'],
        'competition_events' => ['id', 'edition_id', 'event_type', 'name', 'scheduled_start_at', 'scheduled_end_at', 'status'],
        'three_point_contest_entries' => ['id', 'competition_event_id', 'team_id', 'player_id', 'seed_order', 'total_score', 'final_position'],
        'three_point_contest_rounds' => ['id', 'entry_id', 'round_number', 'round_type', 'station1_score', 'station2_score', 'station3_score', 'station4_score', 'station5_score', 'total_score', 'notes'],
        'forfeit_results' => ['id', 'match_id', 'winning_team_id', 'losing_team_id', 'home_assigned_score', 'away_assigned_score', 'reason', 'notes', 'created_at'],
    ];
}

function replaceOrder(): array
{
    return array_keys(tableColumns());
}

function deleteOrder(): array
{
    return array_reverse(replaceOrder());
}

function ensureSchema(PDO $pdo): void
{
    $sql = file_get_contents(__DIR__ . '/schema.sql');
    if ($sql !== false && trim($sql) !== '') {
        $pdo->exec($sql);
        return;
    }

    respond(500, ['error' => 'Missing schema.sql']);
}

function getSnapshot(PDO $pdo): void
{
    $snapshot = [];
    foreach (tableColumns() as $table => $columns) {
        $statement = $pdo->query(sprintf(
            'SELECT %s FROM %s ORDER BY id',
            implode(', ', array_map(fn($column) => "`$column`", $columns)),
            "`$table`"
        ));
        $snapshot[$table] = $statement->fetchAll();
    }

    respond(200, $snapshot);
}

function replaceSnapshot(PDO $pdo): void
{
    $body = readJsonBody();
    $columnsByTable = tableColumns();

    $pdo->beginTransaction();
    try {
        $pdo->exec('SET FOREIGN_KEY_CHECKS = 0');

        foreach (deleteOrder() as $table) {
            $pdo->exec("DELETE FROM `$table`");
        }

        foreach (replaceOrder() as $table) {
            $rows = $body[$table] ?? [];
            if (!is_array($rows)) {
                throw new RuntimeException("Invalid table payload: $table");
            }

            foreach ($rows as $row) {
                if (!is_array($row)) {
                    throw new RuntimeException("Invalid row payload: $table");
                }

                insertRow($pdo, $table, $columnsByTable[$table], $row);
            }
        }

        $pdo->exec('SET FOREIGN_KEY_CHECKS = 1');
        $pdo->commit();
    } catch (Throwable $exception) {
        $pdo->rollBack();
        throw $exception;
    }

    respond(200, ['ok' => true]);
}

function insertRow(PDO $pdo, string $table, array $columns, array $row): void
{
    $filtered = [];
    foreach ($columns as $column) {
        if (array_key_exists($column, $row)) {
            $filtered[$column] = normalizeValue($row[$column]);
        }
    }

    if ($filtered === []) {
        return;
    }

    $columnSql = implode(', ', array_map(fn($column) => "`$column`", array_keys($filtered)));
    $valueSql = implode(', ', array_map(fn($column) => ":$column", array_keys($filtered)));
    $statement = $pdo->prepare("INSERT INTO `$table` ($columnSql) VALUES ($valueSql)");

    foreach ($filtered as $column => $value) {
        $statement->bindValue(":$column", $value);
    }

    $statement->execute();
}

function normalizeValue(mixed $value): mixed
{
    if (is_bool($value)) {
        return $value ? 1 : 0;
    }

    if ($value === '') {
        return null;
    }

    return $value;
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

function respond(int $status, mixed $payload)
{
    http_response_code($status);
    echo json_encode($payload, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    exit;
}
