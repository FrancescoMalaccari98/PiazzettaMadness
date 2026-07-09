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

    $method = $_SERVER['REQUEST_METHOD'] ?? 'GET';
    if ($method === 'GET' && isset($_GET['health'])) {
        respond(200, ['ok' => true, 'api' => 'piazzetta-madness-entities', 'version' => '2026-06-10.2']);
    }

    $action = $_GET['action'] ?? null;
    if (is_string($action) && $action !== '') {
        handleAction($pdo, $method, $action);
    }

    $table = readTable();

    match ($method) {
        'GET' => listRows($pdo, $table),
        'POST' => createRow($pdo, $table),
        'PUT' => updateRow($pdo, $table),
        'DELETE' => deleteRow($pdo, $table),
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

function handleAction(PDO $pdo, string $method, string $action)
{
    match ($action) {
        'save_match' => $method === 'POST' ? saveMatchBundle($pdo) : respond(405, ['error' => 'Method not allowed']),
        'delete_match' => $method === 'DELETE' ? deleteMatchBundle($pdo) : respond(405, ['error' => 'Method not allowed']),
        'save_forfeit' => $method === 'POST' ? saveForfeitBundle($pdo) : respond(405, ['error' => 'Method not allowed']),
        'delete_forfeit' => $method === 'DELETE' ? deleteForfeitBundle($pdo) : respond(405, ['error' => 'Method not allowed']),
        'replace_standings' => $method === 'PUT' ? replaceStandings($pdo) : respond(405, ['error' => 'Method not allowed']),
        'initialize_match_players' => $method === 'POST' ? initializeMatchPlayers($pdo) : respond(405, ['error' => 'Method not allowed']),
        'sync_live' => $method === 'POST' ? syncLive($pdo) : respond(405, ['error' => 'Method not allowed']),
        'initialize_contest_shots' => $method === 'POST' ? initializeContestShots($pdo) : respond(405, ['error' => 'Method not allowed']),
        'sync_contest' => $method === 'PUT' ? syncContest($pdo) : respond(405, ['error' => 'Method not allowed']),
        default => respond(400, ['error' => 'Unsupported action']),
    };
}

function initializeContestShots(PDO $pdo)
{
    $roundId = readId();
    $check = $pdo->prepare('SELECT id FROM three_point_contest_rounds WHERE id = :id');
    $check->execute([':id' => $roundId]);
    if ($check->fetchColumn() === false) {
        respond(404, ['error' => '3pt round not found']);
    }

    $statement = $pdo->prepare(
        "INSERT IGNORE INTO three_point_contest_shots
            (round_id, station_number, ball_number, point_value, result)
         VALUES (:round_id, :station, :ball, :point_value, 'Pending')"
    );
    for ($station = 1; $station <= 5; $station++) {
        for ($ball = 1; $ball <= 5; $ball++) {
            $statement->execute([
                ':round_id' => $roundId,
                ':station' => $station,
                ':ball' => $ball,
                ':point_value' => $ball === 5 ? 2 : 1,
            ]);
        }
    }

    $statement = $pdo->prepare(
        'SELECT id, round_id, station_number, ball_number, point_value, result
         FROM three_point_contest_shots
         WHERE round_id = :round_id
         ORDER BY station_number, ball_number'
    );
    $statement->execute([':round_id' => $roundId]);
    respond(200, $statement->fetchAll());
}

function syncContest(PDO $pdo)
{
    $body = readJsonBody();
    $event = requireObject($body, 'competition_event');
    $entry = requireObject($body, 'entry');
    $round = requireObject($body, 'round');
    $shots = $body['shots'] ?? null;
    if (!is_array($shots)) {
        respond(400, ['error' => 'shots array is required']);
    }

    $eventId = (int)($event['id'] ?? 0);
    $entryId = (int)($entry['id'] ?? 0);
    $roundId = (int)($round['id'] ?? 0);
    if ($eventId < 1 || $entryId < 1 || $roundId < 1 ||
        (int)($entry['competition_event_id'] ?? 0) !== $eventId ||
        (int)($round['entry_id'] ?? 0) !== $entryId) {
        respond(422, ['error' => 'Invalid 3pt contest payload']);
    }

    $pdo->beginTransaction();
    try {
        $savedEvent = writeRecord($pdo, 'competition_events', $event, $eventId);
        $savedEntry = writeRecord($pdo, 'three_point_contest_entries', $entry, $entryId);
        $savedRound = writeRecord($pdo, 'three_point_contest_rounds', $round, $roundId);
        $savedShots = [];
        foreach ($shots as $shot) {
            if (!is_array($shot) || (int)($shot['round_id'] ?? 0) !== $roundId || (int)($shot['id'] ?? 0) < 1) {
                throw new DomainException('Invalid 3pt shot payload');
            }
            $savedShots[] = writeRecord($pdo, 'three_point_contest_shots', $shot, (int)$shot['id']);
        }
        $pdo->commit();
        respond(200, [
            'competition_event' => $savedEvent,
            'entry' => $savedEntry,
            'round' => $savedRound,
            'shots' => $savedShots,
        ]);
    } catch (DomainException $exception) {
        $pdo->rollBack();
        respond(422, ['error' => 'Invalid 3pt contest payload', 'detail' => $exception->getMessage()]);
    } catch (Throwable $exception) {
        $pdo->rollBack();
        throw $exception;
    }
}

function syncLive(PDO $pdo)
{
    $body = readJsonBody();
    $matchData = requireObject($body, 'match');
    $scoreboardData = requireObject($body, 'scoreboard_state');
    $teams = $body['match_teams'] ?? [];
    $players = $body['match_players'] ?? [];
    $events = $body['match_events'] ?? [];
    if (!is_array($teams) || !is_array($players) || !is_array($events)) {
        respond(400, ['error' => 'Live arrays are required']);
    }

    $matchId = (int)($matchData['id'] ?? 0);
    if ($matchId < 1 || (int)($scoreboardData['match_id'] ?? 0) !== $matchId) {
        respond(422, ['error' => 'Invalid live match payload']);
    }

    $pdo->beginTransaction();
    try {
        writeRecord($pdo, 'matches', $matchData, $matchId);

        foreach ($teams as $team) {
            if (!is_array($team) || (int)($team['match_id'] ?? 0) !== $matchId) {
                throw new DomainException('Invalid match team payload');
            }
            upsertMatchTeam($pdo, $team);
        }

        foreach ($players as $player) {
            if (!is_array($player) || (int)($player['match_id'] ?? 0) !== $matchId || (int)($player['id'] ?? 0) < 1) {
                throw new DomainException('Invalid match player payload');
            }
            writeRecord($pdo, 'match_players', $player, (int)$player['id']);
        }

        $scoreboardId = findId($pdo, 'scoreboard_states', 'match_id', $matchId);
        writeRecord($pdo, 'scoreboard_states', $scoreboardData, $scoreboardId);

        $createdEvents = 0;
        foreach ($events as $event) {
            if (!is_array($event) || (int)($event['match_id'] ?? 0) !== $matchId) {
                throw new DomainException('Invalid match event payload');
            }
            if (!liveEventExists($pdo, $event)) {
                writeRecord($pdo, 'match_events', $event, null);
                $createdEvents++;
            }
        }

        $pdo->commit();
        respond(200, ['ok' => true, 'created_events' => $createdEvents]);
    } catch (DomainException $exception) {
        $pdo->rollBack();
        respond(422, ['error' => 'Invalid live payload', 'detail' => $exception->getMessage()]);
    } catch (Throwable $exception) {
        $pdo->rollBack();
        throw $exception;
    }
}

function liveEventExists(PDO $pdo, array $event): bool
{
    $statement = $pdo->prepare(
        'SELECT id FROM match_events
         WHERE match_id = :match_id
           AND period = :period
           AND game_clock_ms_remaining = :game_clock
           AND team_id <=> :team_id
           AND player_id <=> :player_id
           AND event_type = :event_type
           AND points <=> :points
           AND is_correction = :is_correction
           AND created_at = :created_at
         LIMIT 1'
    );
    $statement->execute([
        ':match_id' => (int)($event['match_id'] ?? 0),
        ':period' => (int)($event['period'] ?? 0),
        ':game_clock' => (int)($event['game_clock_ms_remaining'] ?? 0),
        ':team_id' => $event['team_id'] ?? null,
        ':player_id' => $event['player_id'] ?? null,
        ':event_type' => (string)($event['event_type'] ?? ''),
        ':points' => $event['points'] ?? null,
        ':is_correction' => !empty($event['is_correction']) ? 1 : 0,
        ':created_at' => (string)($event['created_at'] ?? ''),
    ]);
    return $statement->fetchColumn() !== false;
}

function initializeMatchPlayers(PDO $pdo)
{
    $matchId = readId();
    $existing = fetchMatchPlayers($pdo, $matchId);
    if ($existing !== []) {
        respond(200, $existing);
    }

    $statement = $pdo->prepare("SELECT team_id, side FROM match_teams WHERE match_id = :match_id ORDER BY side");
    $statement->execute([':match_id' => $matchId]);
    $sides = $statement->fetchAll();
    if (count($sides) !== 2) {
        respond(422, ['error' => 'Match must have exactly one Home and one Away team']);
    }

    $pdo->beginTransaction();
    try {
        foreach ($sides as $side) {
            $teamId = (int)$side['team_id'];
            $statement = $pdo->prepare(
                'SELECT player_id, jersey_number FROM team_rosters WHERE team_id = :team_id AND is_active = 1 ORDER BY jersey_number, id'
            );
            $statement->execute([':team_id' => $teamId]);
            $rosters = $statement->fetchAll();
            if ($rosters === []) {
                throw new DomainException("Team $teamId has no active roster players");
            }

            $insert = $pdo->prepare(
                'INSERT INTO match_players (match_id, team_id, player_id, jersey_number)
                 VALUES (:match_id, :team_id, :player_id, :jersey_number)'
            );
            foreach ($rosters as $roster) {
                $insert->execute([
                    ':match_id' => $matchId,
                    ':team_id' => $teamId,
                    ':player_id' => (int)$roster['player_id'],
                    ':jersey_number' => $roster['jersey_number'],
                ]);
            }
        }

        $created = fetchMatchPlayers($pdo, $matchId);
        $pdo->commit();
        respond(201, $created);
    } catch (DomainException $exception) {
        $pdo->rollBack();
        respond(422, ['error' => 'Active roster required', 'detail' => $exception->getMessage()]);
    } catch (Throwable $exception) {
        $pdo->rollBack();
        throw $exception;
    }
}

function fetchMatchPlayers(PDO $pdo, int $matchId): array
{
    $definition = tableDefinitions()['match_players'];
    $columns = implode(', ', array_map(fn($column) => "`$column`", $definition['columns']));
    $statement = $pdo->prepare("SELECT $columns FROM match_players WHERE match_id = :match_id ORDER BY team_id, jersey_number, id");
    $statement->execute([':match_id' => $matchId]);
    return $statement->fetchAll();
}

function saveMatchBundle(PDO $pdo)
{
    $body = readJsonBody();
    $matchData = requireObject($body, 'match');
    $homeData = requireObject($body, 'home');
    $awayData = requireObject($body, 'away');

    $pdo->beginTransaction();
    try {
        $matchId = (int)($matchData['id'] ?? 0);
        $match = writeRecord($pdo, 'matches', $matchData, $matchId > 0 ? $matchId : null);
        $homeData['match_id'] = $match['id'];
        $homeData['side'] = 'Home';
        $awayData['match_id'] = $match['id'];
        $awayData['side'] = 'Away';
        $home = upsertMatchTeam($pdo, $homeData);
        $away = upsertMatchTeam($pdo, $awayData);
        $pdo->commit();
        respond($matchId > 0 ? 200 : 201, ['match' => $match, 'home' => $home, 'away' => $away]);
    } catch (Throwable $exception) {
        $pdo->rollBack();
        throw $exception;
    }
}

function deleteMatchBundle(PDO $pdo)
{
    $matchId = readId();
    foreach (['match_player_stats', 'match_team_stats'] as $statsTable) {
        if (tableExists($pdo, $statsTable) && countRows($pdo, $statsTable, 'match_id', $matchId) > 0) {
            respond(409, ['error' => 'Match has linked statistics', 'detail' => "Delete the statistics in $statsTable before deleting this match"]);
        }
    }

    $statement = $pdo->prepare('DELETE FROM matches WHERE id = :id');
    $statement->execute([':id' => $matchId]);
    respond(200, ['deleted' => $statement->rowCount() > 0]);
}

function saveForfeitBundle(PDO $pdo)
{
    $body = readJsonBody();
    $forfeitData = requireObject($body, 'forfeit');
    $matchData = requireObject($body, 'match');
    $homeData = requireObject($body, 'home');
    $awayData = requireObject($body, 'away');

    $pdo->beginTransaction();
    try {
        $forfeitId = (int)($forfeitData['id'] ?? 0);
        if ($forfeitId < 1) {
            $existing = findId($pdo, 'forfeit_results', 'match_id', (int)($forfeitData['match_id'] ?? 0));
            $forfeitId = $existing ?? 0;
        }
        $forfeit = writeRecord($pdo, 'forfeit_results', $forfeitData, $forfeitId > 0 ? $forfeitId : null);
        $match = writeRecord($pdo, 'matches', $matchData, (int)$forfeit['match_id']);
        $home = upsertMatchTeam($pdo, $homeData);
        $away = upsertMatchTeam($pdo, $awayData);
        $pdo->commit();
        respond(200, ['forfeit' => $forfeit, 'match' => $match, 'home' => $home, 'away' => $away]);
    } catch (Throwable $exception) {
        $pdo->rollBack();
        throw $exception;
    }
}

function deleteForfeitBundle(PDO $pdo)
{
    $forfeitId = readId();
    $forfeit = fetchRecord($pdo, 'forfeit_results', $forfeitId);
    $matchId = (int)$forfeit['match_id'];

    $pdo->beginTransaction();
    try {
        $statement = $pdo->prepare('DELETE FROM forfeit_results WHERE id = :id');
        $statement->execute([':id' => $forfeitId]);
        $statement = $pdo->prepare("UPDATE matches SET status = 'Scheduled', winner_team_id = NULL, win_reason = NULL WHERE id = :id");
        $statement->execute([':id' => $matchId]);
        $statement = $pdo->prepare('UPDATE match_teams SET score = 0, is_winner = 0, forfeit_score = NULL WHERE match_id = :match_id');
        $statement->execute([':match_id' => $matchId]);
        $match = fetchRecord($pdo, 'matches', $matchId);
        $home = fetchMatchTeam($pdo, $matchId, 'Home');
        $away = fetchMatchTeam($pdo, $matchId, 'Away');
        $pdo->commit();
        respond(200, ['forfeit' => null, 'match' => $match, 'home' => $home, 'away' => $away]);
    } catch (Throwable $exception) {
        $pdo->rollBack();
        throw $exception;
    }
}

function replaceStandings(PDO $pdo)
{
    $body = readJsonBody();
    $rows = $body['standings'] ?? null;
    if (!is_array($rows)) {
        respond(400, ['error' => 'Standings array is required']);
    }

    $pdo->beginTransaction();
    try {
        $pdo->exec('DELETE FROM standings');
        $created = [];
        foreach ($rows as $row) {
            if (!is_array($row)) {
                throw new RuntimeException('Invalid standing payload');
            }
            $created[] = writeRecord($pdo, 'standings', $row, null);
        }
        $pdo->commit();
        respond(200, $created);
    } catch (Throwable $exception) {
        $pdo->rollBack();
        throw $exception;
    }
}

function validateConsoleActiveEdition(PDO $pdo, string $table, array $data, ?int $id): void
{
    if ($table !== 'editions' || (int)($data['is_console_active'] ?? 0) !== 1) {
        return;
    }

    $sql = 'SELECT id FROM editions WHERE is_console_active = 1';
    $params = [];
    if ($id !== null) {
        $sql .= ' AND id <> :id';
        $params[':id'] = $id;
    }
    $sql .= ' LIMIT 1';

    $statement = $pdo->prepare($sql);
    $statement->execute($params);
    if ($statement->fetchColumn()) {
        respond(422, ['error' => 'Only one edition can be active for console']);
    }
}

function writeRecord(PDO $pdo, string $table, array $body, ?int $id): array
{
    $definition = tableDefinitions()[$table];
    $data = filterWritable($body, $definition['writable']);
    validateRow($table, $data);
    validateConsoleActiveEdition($pdo, $table, $data, $id);
    if ($data === []) {
        throw new RuntimeException("No writable fields for $table");
    }

    if ($id === null) {
        $columns = array_keys($data);
        $columnSql = implode(', ', array_map(fn($column) => "`$column`", $columns));
        $valueSql = implode(', ', array_map(fn($column) => ":$column", $columns));
        $statement = $pdo->prepare("INSERT INTO `$table` ($columnSql) VALUES ($valueSql)");
        bindValues($statement, $data);
        $statement->execute();
        $id = (int)$pdo->lastInsertId();
    } else {
        $setSql = implode(', ', array_map(fn($column) => "`$column` = :$column", array_keys($data)));
        $statement = $pdo->prepare("UPDATE `$table` SET $setSql WHERE id = :id");
        bindValues($statement, $data);
        $statement->bindValue(':id', $id, PDO::PARAM_INT);
        $statement->execute();
    }

    return fetchRecord($pdo, $table, $id);
}

function upsertMatchTeam(PDO $pdo, array $data): array
{
    $id = (int)($data['id'] ?? 0);
    if ($id < 1) {
        $statement = $pdo->prepare('SELECT id FROM match_teams WHERE match_id = :match_id AND side = :side');
        $statement->execute([':match_id' => $data['match_id'], ':side' => $data['side']]);
        $id = (int)($statement->fetchColumn() ?: 0);
    }
    return writeRecord($pdo, 'match_teams', $data, $id > 0 ? $id : null);
}

function fetchRecord(PDO $pdo, string $table, int $id): array
{
    $definition = tableDefinitions()[$table];
    $columns = implode(', ', array_map(fn($column) => "`$column`", $definition['columns']));
    $statement = $pdo->prepare("SELECT $columns FROM `$table` WHERE id = :id");
    $statement->execute([':id' => $id]);
    $row = $statement->fetch();
    if (!$row) {
        throw new RuntimeException("Row not found: $table/$id");
    }
    return $row;
}

function fetchMatchTeam(PDO $pdo, int $matchId, string $side): array
{
    $statement = $pdo->prepare('SELECT id FROM match_teams WHERE match_id = :match_id AND side = :side');
    $statement->execute([':match_id' => $matchId, ':side' => $side]);
    $id = (int)($statement->fetchColumn() ?: 0);
    if ($id < 1) {
        throw new RuntimeException("Missing $side match team for match $matchId");
    }
    return fetchRecord($pdo, 'match_teams', $id);
}

function requireObject(array $body, string $key): array
{
    $value = $body[$key] ?? null;
    if (!is_array($value)) {
        respond(400, ['error' => "$key object is required"]);
    }
    return $value;
}

function findId(PDO $pdo, string $table, string $column, int $value): ?int
{
    $statement = $pdo->prepare("SELECT id FROM `$table` WHERE `$column` = :value LIMIT 1");
    $statement->execute([':value' => $value]);
    $id = $statement->fetchColumn();
    return $id === false ? null : (int)$id;
}

function tableExists(PDO $pdo, string $table): bool
{
    $statement = $pdo->prepare('SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = :table');
    $statement->execute([':table' => $table]);
    return (int)$statement->fetchColumn() > 0;
}

function countRows(PDO $pdo, string $table, string $column, int $value): int
{
    $statement = $pdo->prepare("SELECT COUNT(*) FROM `$table` WHERE `$column` = :value");
    $statement->execute([':value' => $value]);
    return (int)$statement->fetchColumn();
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

function tableDefinitions(): array
{
    return [
        'tournaments' => [
            'columns' => ['id', 'name', 'description', 'created_at', 'updated_at'],
            'writable' => ['name', 'description'],
            'order' => '`name`, `id`',
        ],
        'editions' => [
            'columns' => ['id', 'tournament_id', 'name', 'year', 'start_date', 'end_date', 'status', 'is_console_active', 'created_at', 'updated_at'],
            'writable' => ['tournament_id', 'name', 'year', 'start_date', 'end_date', 'status', 'is_console_active'],
            'order' => '`year` DESC, `name`, `id`',
        ],
        'courts' => [
            'columns' => ['id', 'edition_id', 'name', 'location'],
            'writable' => ['edition_id', 'name', 'location'],
            'order' => '`name`, `id`',
        ],
        'sponsors' => [
            'columns' => ['id', 'name', 'description', 'image_path', 'is_active', 'sort_order', 'created_at', 'updated_at'],
            'writable' => ['name', 'description', 'image_path', 'is_active', 'sort_order'],
            'order' => '`sort_order`, `name`, `id`',
        ],
        'merchandise_items' => [
            'columns' => ['id', 'name', 'description', 'price', 'image_path', 'is_active', 'sort_order', 'created_at', 'updated_at'],
            'writable' => ['name', 'description', 'price', 'image_path', 'is_active', 'sort_order'],
            'order' => '`sort_order`, `name`, `id`',
        ],
        'teams' => [
            'columns' => ['id', 'edition_id', 'name', 'short_name', 'primary_color', 'secondary_color', 'logo_path', 'created_at', 'updated_at'],
            'writable' => ['edition_id', 'name', 'short_name', 'primary_color', 'secondary_color', 'logo_path'],
            'order' => '`name`, `id`',
        ],
        'team_rosters' => [
            'columns' => ['id', 'team_id', 'player_id', 'jersey_number', 'role', 'is_captain', 'is_active', 'created_at', 'updated_at'],
            'writable' => ['team_id', 'player_id', 'jersey_number', 'role', 'is_captain', 'is_active'],
            'order' => '`team_id`, `jersey_number`, `id`',
        ],
        'tournament_groups' => [
            'columns' => ['id', 'edition_id', 'name', 'code', 'sort_order'],
            'writable' => ['edition_id', 'name', 'code', 'sort_order'],
            'order' => '`sort_order`, `code`, `id`',
        ],
        'group_teams' => [
            'columns' => ['id', 'group_id', 'team_id', 'seed_label'],
            'writable' => ['group_id', 'team_id', 'seed_label'],
            'order' => '`group_id`, `seed_label`, `id`',
        ],
        'matches' => [
            'columns' => ['id', 'edition_id', 'group_id', 'court_id', 'name', 'phase', 'round', 'scheduled_start_at', 'scheduled_end_at', 'actual_start_at', 'actual_end_at', 'status', 'period_duration_ms', 'shot_clock_ms', 'max_score', 'max_score_enabled', 'winner_team_id', 'win_reason', 'created_at', 'updated_at'],
            'writable' => ['edition_id', 'group_id', 'court_id', 'name', 'phase', 'round', 'scheduled_start_at', 'scheduled_end_at', 'actual_start_at', 'actual_end_at', 'status', 'period_duration_ms', 'shot_clock_ms', 'max_score', 'max_score_enabled', 'winner_team_id', 'win_reason'],
            'order' => '`scheduled_start_at`, `id`',
        ],
        'match_teams' => [
            'columns' => ['id', 'match_id', 'team_id', 'side', 'score', 'fouls_current_period', 'timeouts_used_total', 'timeouts_used_period', 'is_winner', 'forfeit_score'],
            'writable' => ['match_id', 'team_id', 'side', 'score', 'fouls_current_period', 'timeouts_used_total', 'timeouts_used_period', 'is_winner', 'forfeit_score'],
            'order' => '`match_id`, `side`, `id`',
        ],
        'match_players' => [
            'columns' => ['id', 'match_id', 'team_id', 'player_id', 'jersey_number', 'is_starting_five', 'is_on_court', 'points', 'personal_fouls', 'is_fouled_out', 'is_ejected'],
            'writable' => ['match_id', 'team_id', 'player_id', 'jersey_number', 'is_starting_five', 'is_on_court', 'points', 'personal_fouls', 'is_fouled_out', 'is_ejected'],
            'order' => '`match_id`, `team_id`, `jersey_number`, `id`',
        ],
        'match_events' => [
            'columns' => ['id', 'match_id', 'period', 'period_type', 'game_clock_ms_remaining', 'shot_clock_ms_remaining', 'team_id', 'player_id', 'event_type', 'points', 'is_correction', 'reverts_event_id', 'description', 'payload_json', 'created_at', 'synced_at'],
            'writable' => ['match_id', 'period', 'period_type', 'game_clock_ms_remaining', 'shot_clock_ms_remaining', 'team_id', 'player_id', 'event_type', 'points', 'is_correction', 'reverts_event_id', 'description', 'payload_json', 'created_at', 'synced_at'],
            'order' => '`match_id`, `created_at`, `id`',
        ],
        'scoreboard_states' => [
            'columns' => ['id', 'match_id', 'current_period', 'current_period_type', 'game_clock_ms_remaining', 'shot_clock_ms_remaining', 'timeout_clock_ms_remaining', 'break_clock_ms_remaining', 'is_game_clock_running', 'is_shot_clock_running', 'is_timeout_running', 'is_break_running', 'possession_team_id', 'home_score', 'away_score', 'home_fouls_current_period', 'away_fouls_current_period', 'home_timeouts_used_total', 'away_timeouts_used_total', 'last_event_id', 'last_updated_at'],
            'writable' => ['match_id', 'current_period', 'current_period_type', 'game_clock_ms_remaining', 'shot_clock_ms_remaining', 'timeout_clock_ms_remaining', 'break_clock_ms_remaining', 'is_game_clock_running', 'is_shot_clock_running', 'is_timeout_running', 'is_break_running', 'possession_team_id', 'home_score', 'away_score', 'home_fouls_current_period', 'away_fouls_current_period', 'home_timeouts_used_total', 'away_timeouts_used_total', 'last_event_id', 'last_updated_at'],
            'order' => '`match_id`, `id`',
        ],
        'competition_events' => [
            'columns' => ['id', 'edition_id', 'event_type', 'name', 'scheduled_start_at', 'scheduled_end_at', 'status'],
            'writable' => ['edition_id', 'event_type', 'name', 'scheduled_start_at', 'scheduled_end_at', 'status'],
            'order' => '`scheduled_start_at`, `name`, `id`',
        ],
        'three_point_contest_entries' => [
            'columns' => ['id', 'competition_event_id', 'team_id', 'player_id', 'seed_order', 'total_score', 'final_position'],
            'writable' => ['competition_event_id', 'team_id', 'player_id', 'seed_order', 'total_score', 'final_position'],
            'order' => '`competition_event_id`, `seed_order`, `id`',
        ],
        'three_point_contest_rounds' => [
            'columns' => ['id', 'entry_id', 'round_number', 'round_type', 'station1_score', 'station2_score', 'station3_score', 'station4_score', 'station5_score', 'total_score', 'notes'],
            'writable' => ['entry_id', 'round_number', 'round_type', 'station1_score', 'station2_score', 'station3_score', 'station4_score', 'station5_score', 'total_score', 'notes'],
            'order' => '`entry_id`, `round_number`, `round_type`, `id`',
        ],
        'three_point_contest_shots' => [
            'columns' => ['id', 'round_id', 'station_number', 'ball_number', 'point_value', 'result'],
            'writable' => ['round_id', 'station_number', 'ball_number', 'point_value', 'result'],
            'order' => 'round_id, station_number, ball_number, id',
        ],
        'forfeit_results' => [
            'columns' => ['id', 'match_id', 'winning_team_id', 'losing_team_id', 'home_assigned_score', 'away_assigned_score', 'reason', 'notes', 'created_at'],
            'writable' => ['match_id', 'winning_team_id', 'losing_team_id', 'home_assigned_score', 'away_assigned_score', 'reason', 'notes'],
            'order' => '`match_id`, `id`',
        ],
        'standings' => [
            'columns' => ['id', 'group_id', 'team_id', 'played', 'wins', 'losses', 'points_for', 'points_against', 'point_difference', 'ranking_points', 'position', 'tie_break_note'],
            'writable' => ['group_id', 'team_id', 'played', 'wins', 'losses', 'points_for', 'points_against', 'point_difference', 'ranking_points', 'position', 'tie_break_note'],
            'order' => '`group_id`, `position`, `id`',
        ],
        'players' => [
            'columns' => ['id', 'first_name', 'last_name', 'nickname', 'fiscal_code', 'address', 'phone_number', 'email', 'birth_date', 'photo_path', 'created_at', 'updated_at'],
            'writable' => ['first_name', 'last_name', 'nickname', 'fiscal_code', 'address', 'phone_number', 'email', 'birth_date', 'photo_path'],
            'order' => '`last_name`, `first_name`, `id`',
        ],
    ];
}

function readTable(): string
{
    $table = $_GET['table'] ?? '';
    if (!is_string($table) || !array_key_exists($table, tableDefinitions())) {
        respond(400, ['error' => 'Unsupported table']);
    }

    return $table;
}

function listRows(PDO $pdo, string $table): void
{
    $definition = tableDefinitions()[$table];
    $columns = implode(', ', array_map(fn($column) => "`$column`", $definition['columns']));
    $statement = $pdo->query("SELECT $columns FROM `$table` ORDER BY {$definition['order']}");
    respond(200, $statement->fetchAll());
}

function createRow(PDO $pdo, string $table): void
{
    $body = readJsonBody();
    $definition = tableDefinitions()[$table];
    $data = filterWritable($body, $definition['writable']);
    validateRow($table, $data);

    $columns = array_keys($data);
    $columnSql = implode(', ', array_map(fn($column) => "`$column`", $columns));
    $valueSql = implode(', ', array_map(fn($column) => ":$column", $columns));
    $statement = $pdo->prepare("INSERT INTO `$table` ($columnSql) VALUES ($valueSql)");
    bindValues($statement, $data);
    $statement->execute();

    getRow($pdo, $table, (int)$pdo->lastInsertId(), 201);
}

function updateRow(PDO $pdo, string $table): void
{
    $id = readId();
    $body = readJsonBody();
    $definition = tableDefinitions()[$table];
    $data = filterWritable($body, $definition['writable']);
    validateRow($table, $data);

    $setSql = implode(', ', array_map(fn($column) => "`$column` = :$column", array_keys($data)));
    $statement = $pdo->prepare("UPDATE `$table` SET $setSql WHERE id = :id");
    bindValues($statement, $data);
    $statement->bindValue(':id', $id, PDO::PARAM_INT);
    $statement->execute();

    getRow($pdo, $table, $id);
}

function deleteRow(PDO $pdo, string $table): void
{
    $id = readId();
    assertNoLinkedStatistics($pdo, $table, $id);
    $statement = $pdo->prepare("DELETE FROM `$table` WHERE id = :id");
    $statement->execute([':id' => $id]);
    respond(200, ['deleted' => $statement->rowCount() > 0]);
}

function assertNoLinkedStatistics(PDO $pdo, string $table, int $id): void
{
    $checks = [];
    if ($table === 'matches') {
        $checks = [
            ['match_player_stats', 'match_id', $id],
            ['match_team_stats', 'match_id', $id],
        ];
    } elseif ($table === 'players') {
        $checks = [['match_player_stats', 'player_id', $id]];
    } elseif ($table === 'teams') {
        $checks = [
            ['match_player_stats', 'team_id', $id],
            ['match_team_stats', 'team_id', $id],
        ];
    }

    foreach ($checks as [$statsTable, $column, $value]) {
        if (tableExists($pdo, $statsTable) && countRows($pdo, $statsTable, $column, $value) > 0) {
            respond(409, ['error' => 'Operation blocked by linked statistics', 'detail' => "Linked rows exist in $statsTable"]);
        }
    }

    if ($table === 'editions') {
        foreach (['match_player_stats', 'match_team_stats'] as $statsTable) {
            if (!tableExists($pdo, $statsTable)) continue;
            $statement = $pdo->prepare("SELECT COUNT(*) FROM `$statsTable` stats INNER JOIN matches m ON m.id = stats.match_id WHERE m.edition_id = :id");
            $statement->execute([':id' => $id]);
            if ((int)$statement->fetchColumn() > 0) {
                respond(409, ['error' => 'Operation blocked by linked statistics', 'detail' => "Edition has statistics in $statsTable"]);
            }
        }
    }
}

function getRow(PDO $pdo, string $table, int $id, int $status = 200): void
{
    $definition = tableDefinitions()[$table];
    $columns = implode(', ', array_map(fn($column) => "`$column`", $definition['columns']));
    $statement = $pdo->prepare("SELECT $columns FROM `$table` WHERE id = :id");
    $statement->execute([':id' => $id]);
    $row = $statement->fetch();

    if (!$row) {
        respond(404, ['error' => 'Row not found']);
    }

    respond($status, $row);
}

function filterWritable(array $body, array $writable): array
{
    $data = [];
    foreach ($writable as $column) {
        if (array_key_exists($column, $body)) {
            $data[$column] = normalizeValue($body[$column]);
        }
    }

    return $data;
}

function validateRow(string $table, array $data): void
{
    if ($table === 'tournaments' && trim((string)($data['name'] ?? '')) === '') {
        respond(422, ['error' => 'Tournament name is required']);
    }

    if ($table === 'editions') {
        if ((int)($data['tournament_id'] ?? 0) < 1 || trim((string)($data['name'] ?? '')) === '' || (int)($data['year'] ?? 0) < 1) {
            respond(422, ['error' => 'Edition tournament_id, name and year are required']);
        }
    }

    if ($table === 'courts') {
        if ((int)($data['edition_id'] ?? 0) < 1 || trim((string)($data['name'] ?? '')) === '') {
            respond(422, ['error' => 'Court edition_id and name are required']);
        }
    }

    if ($table === 'sponsors' && trim((string)($data['name'] ?? '')) === '') {
        respond(422, ['error' => 'Sponsor name is required']);
    }

    if ($table === 'merchandise_items' && trim((string)($data['name'] ?? '')) === '') {
        respond(422, ['error' => 'Merchandise item name is required']);
    }

    if ($table === 'teams') {
        if ((int)($data['edition_id'] ?? 0) < 1 || trim((string)($data['name'] ?? '')) === '') {
            respond(422, ['error' => 'Team edition_id and name are required']);
        }
    }

    if ($table === 'team_rosters') {
        if ((int)($data['team_id'] ?? 0) < 1 || (int)($data['player_id'] ?? 0) < 1) {
            respond(422, ['error' => 'Roster team_id and player_id are required']);
        }
    }

    if ($table === 'tournament_groups') {
        if ((int)($data['edition_id'] ?? 0) < 1 || trim((string)($data['name'] ?? '')) === '' || trim((string)($data['code'] ?? '')) === '') {
            respond(422, ['error' => 'Group edition_id, name and code are required']);
        }
    }

    if ($table === 'group_teams') {
        if ((int)($data['group_id'] ?? 0) < 1 || (int)($data['team_id'] ?? 0) < 1) {
            respond(422, ['error' => 'Group team group_id and team_id are required']);
        }
    }

    if ($table === 'matches') {
        if ((int)($data['edition_id'] ?? 0) < 1 || trim((string)($data['phase'] ?? '')) === '' || trim((string)($data['status'] ?? '')) === '') {
            respond(422, ['error' => 'Match edition_id, phase and status are required']);
        }
    }

    if ($table === 'match_teams') {
        if ((int)($data['match_id'] ?? 0) < 1 || (int)($data['team_id'] ?? 0) < 1 || !in_array((string)($data['side'] ?? ''), ['Home', 'Away'], true)) {
            respond(422, ['error' => 'Match team match_id, team_id and side are required']);
        }
    }

    if ($table === 'match_players') {
        if ((int)($data['match_id'] ?? 0) < 1 || (int)($data['team_id'] ?? 0) < 1 || (int)($data['player_id'] ?? 0) < 1) {
            respond(422, ['error' => 'Match player match_id, team_id and player_id are required']);
        }
    }

    if ($table === 'match_events') {
        if ((int)($data['match_id'] ?? 0) < 1 || (int)($data['period'] ?? 0) < 1 || trim((string)($data['period_type'] ?? '')) === '' || trim((string)($data['event_type'] ?? '')) === '') {
            respond(422, ['error' => 'Match event match_id, period, period_type and event_type are required']);
        }
    }

    if ($table === 'scoreboard_states') {
        if ((int)($data['match_id'] ?? 0) < 1 || (int)($data['current_period'] ?? 0) < 1 || trim((string)($data['current_period_type'] ?? '')) === '') {
            respond(422, ['error' => 'Scoreboard state match_id, current_period and current_period_type are required']);
        }
    }

    if ($table === 'competition_events') {
        if ((int)($data['edition_id'] ?? 0) < 1 || trim((string)($data['event_type'] ?? '')) === '' || trim((string)($data['name'] ?? '')) === '' || trim((string)($data['status'] ?? '')) === '') {
            respond(422, ['error' => 'Competition event edition_id, event_type, name and status are required']);
        }
    }

    if ($table === 'three_point_contest_entries') {
        if ((int)($data['competition_event_id'] ?? 0) < 1 || (int)($data['team_id'] ?? 0) < 1 || (int)($data['player_id'] ?? 0) < 1) {
            respond(422, ['error' => '3pt entry event, team and player are required']);
        }
    }

    if ($table === 'three_point_contest_rounds') {
        if ((int)($data['entry_id'] ?? 0) < 1 || (int)($data['round_number'] ?? 0) < 1 || trim((string)($data['round_type'] ?? '')) === '') {
            respond(422, ['error' => '3pt round entry_id, round_number and round_type are required']);
        }
    }

    if ($table === 'three_point_contest_shots') {
        $station = (int)($data['station_number'] ?? 0);
        $ball = (int)($data['ball_number'] ?? 0);
        $value = (int)($data['point_value'] ?? 0);
        $result = (string)($data['result'] ?? '');
        if ((int)($data['round_id'] ?? 0) < 1 || $station < 1 || $station > 5 ||
            $ball < 1 || $ball > 5 || !in_array($value, [1, 2], true) ||
            !in_array($result, ['Pending', 'Made', 'Missed'], true)) {
            respond(422, ['error' => 'Invalid 3pt shot']);
        }
    }

    if ($table === 'forfeit_results') {
        if ((int)($data['match_id'] ?? 0) < 1 || trim((string)($data['reason'] ?? '')) === '') {
            respond(422, ['error' => 'Forfeit match_id and reason are required']);
        }
    }

    if ($table === 'standings') {
        if ((int)($data['group_id'] ?? 0) < 1 || (int)($data['team_id'] ?? 0) < 1) {
            respond(422, ['error' => 'Standing group_id and team_id are required']);
        }
    }

    if ($table === 'players') {
        if (trim((string)($data['first_name'] ?? '')) === '' || trim((string)($data['last_name'] ?? '')) === '') {
            respond(422, ['error' => 'Player first_name and last_name are required']);
        }
    }
}

function bindValues(PDOStatement $statement, array $data): void
{
    foreach ($data as $column => $value) {
        $statement->bindValue(":$column", $value);
    }
}

function normalizeValue(mixed $value): mixed
{
    if (is_bool($value)) {
        return $value ? 1 : 0;
    }

    if (is_string($value)) {
        $trimmed = trim($value);
        return $trimmed === '' ? null : $trimmed;
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

function readId(): int
{
    $id = filter_input(INPUT_GET, 'id', FILTER_VALIDATE_INT);
    if (!$id || $id < 1) {
        respond(400, ['error' => 'Valid id query parameter is required']);
    }

    return $id;
}

function respond(int $status, mixed $payload)
{
    http_response_code($status);
    echo json_encode($payload, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    exit;
}
