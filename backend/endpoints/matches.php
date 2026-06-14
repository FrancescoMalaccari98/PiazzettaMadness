<?php
declare(strict_types=1);

// ============================================================
// GET /api-ocr/matches/today
//
// Returns the selectable match list for the current game day.
// Between midnight and 01:30 Europe/Rome, the game day is still
// considered to be the previous calendar day.
// ============================================================

function handle_today_matches(PDO $pdo)
{
    try {
        $tz = new DateTimeZone('Europe/Rome');
        $game_day = resolve_game_day($tz);

        $window_start = $game_day;
        $window_end = $game_day->modify('+1 day')->setTime(1, 30);

        $stmt = $pdo->prepare(build_matches_sql($pdo));
        $stmt->execute([
            $window_start->format('Y-m-d H:i:s'),
            $window_end->format('Y-m-d H:i:s'),
        ]);

        $matches = [];
        foreach ($stmt->fetchAll() as $row) {
            $matches[] = map_match_row($row, $tz);
        }

        send_json([
            'success' => true,
            'gameDay' => $game_day->format('Y-m-d'),
            'windowStart' => $window_start->format('Y-m-d H:i:s'),
            'windowEnd' => $window_end->format('Y-m-d H:i:s'),
            'matches' => $matches,
        ]);
    } catch (PDOException $e) {
        error_log('[api-ocr/matches/today] PDOException: ' . $e->getMessage());
        send_error('Match lookup database error: ' . $e->getMessage(), 500);
    } catch (Throwable $e) {
        error_log('[api-ocr/matches/today] Error: ' . $e->getMessage());
        send_error('Match lookup error: ' . $e->getMessage(), 500);
    }
}

function resolve_game_day(DateTimeZone $tz)
{
    $date_override = trim((string)($_GET['date'] ?? ''));
    if ($date_override !== '') {
        $date = DateTimeImmutable::createFromFormat('!Y-m-d', $date_override, $tz);
        $errors = DateTimeImmutable::getLastErrors();
        $has_errors = is_array($errors) &&
            (($errors['warning_count'] ?? 0) > 0 || ($errors['error_count'] ?? 0) > 0);

        if ($date === false || $has_errors) {
            send_error('Invalid date. Expected format: YYYY-MM-DD', 400);
        }

        return $date->setTime(0, 0);
    }

    $now = new DateTimeImmutable('now', $tz);
    $cutoff = $now->setTime(1, 30);
    return $now <= $cutoff
        ? $now->modify('-1 day')->setTime(0, 0)
        : $now->setTime(0, 0);
}

function build_matches_sql(PDO $pdo)
{
    $match_columns = table_columns($pdo, 'matches');
    $has_match_teams = table_exists($pdo, 'match_teams');

    if ($has_match_teams && isset($match_columns['scheduled_start_at'])) {
        return "SELECT
                m.id AS match_id,
                m.name AS match_name,
                m.phase,
                m.`round` AS round_label,
                m.scheduled_start_at,
                m.scheduled_end_at,
                m.status,
                home.team_id AS home_team_id,
                home_team.name AS home_team_name,
                home_team.short_name AS home_team_short_name,
                away.team_id AS away_team_id,
                away_team.name AS away_team_name,
                away_team.short_name AS away_team_short_name
             FROM matches m
             INNER JOIN match_teams home
                ON home.match_id = m.id AND home.side = 'Home'
             INNER JOIN teams home_team
                ON home_team.id = home.team_id
             INNER JOIN match_teams away
                ON away.match_id = m.id AND away.side = 'Away'
             INNER JOIN teams away_team
                ON away_team.id = away.team_id
             WHERE m.scheduled_start_at >= ?
               AND m.scheduled_start_at < ?
               AND LOWER(m.status) <> 'cancelled'
             ORDER BY m.scheduled_start_at ASC, m.id ASC";
    }

    if (isset($match_columns['scheduled_at'], $match_columns['home_team_id'], $match_columns['away_team_id'])) {
        return "SELECT
                m.id AS match_id,
                NULL AS match_name,
                m.phase,
                m.round_label AS round_label,
                m.scheduled_at AS scheduled_start_at,
                NULL AS scheduled_end_at,
                m.status,
                m.home_team_id,
                home_team.name AS home_team_name,
                home_team.short_name AS home_team_short_name,
                m.away_team_id,
                away_team.name AS away_team_name,
                away_team.short_name AS away_team_short_name
             FROM matches m
             INNER JOIN teams home_team
                ON home_team.id = m.home_team_id
             INNER JOIN teams away_team
                ON away_team.id = m.away_team_id
             WHERE m.scheduled_at >= ?
               AND m.scheduled_at < ?
               AND LOWER(m.status) <> 'cancelled'
             ORDER BY m.scheduled_at ASC, m.id ASC";
    }

    throw new RuntimeException('Unsupported matches schema for today lookup');
}

function table_exists(PDO $pdo, string $table)
{
    $stmt = $pdo->prepare(
        'SELECT COUNT(*)
         FROM INFORMATION_SCHEMA.TABLES
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME = ?'
    );
    $stmt->execute([$table]);
    return (int)$stmt->fetchColumn() > 0;
}

function table_columns(PDO $pdo, string $table)
{
    $stmt = $pdo->prepare(
        'SELECT COLUMN_NAME
         FROM INFORMATION_SCHEMA.COLUMNS
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME = ?'
    );
    $stmt->execute([$table]);
    $columns = [];
    foreach ($stmt->fetchAll() as $row) {
        $columns[(string)$row['COLUMN_NAME']] = true;
    }
    return $columns;
}

function map_match_row(array $row, DateTimeZone $tz)
{
    $home_name = (string)$row['home_team_name'];
    $away_name = (string)$row['away_team_name'];
    $start = $row['scheduled_start_at'] !== null
        ? new DateTimeImmutable((string)$row['scheduled_start_at'], $tz)
        : null;

    $display_name = $start !== null
        ? $start->format('H:i') . ' - ' . $home_name . ' vs ' . $away_name
        : $home_name . ' vs ' . $away_name;

    return [
        'matchId' => (int)$row['match_id'],
        'displayName' => $display_name,
        'matchName' => $row['match_name'],
        'phase' => $row['phase'],
        'round' => $row['round_label'],
        'scheduledStartAt' => $row['scheduled_start_at'],
        'scheduledEndAt' => $row['scheduled_end_at'],
        'status' => $row['status'],
        'homeTeam' => [
            'id' => (int)$row['home_team_id'],
            'name' => $home_name,
            'shortName' => $row['home_team_short_name'],
        ],
        'awayTeam' => [
            'id' => (int)$row['away_team_id'],
            'name' => $away_name,
            'shortName' => $row['away_team_short_name'],
        ],
    ];
}
