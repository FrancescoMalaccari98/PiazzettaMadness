<?php
declare(strict_types=1);

// ============================================================
// POST /api-ocr/import/{match_id}
//
// Imports a reconciled ProcessingResult JSON into the live DB.
//
// Flow:
//   1. Parse and validate the request body
//   2. READ match_teams  → resolve home_team_id / away_team_id
//   2b. Validate OCR team names against selected DB match teams
//   3. READ team_rosters → build (team_id, jersey) → player_id map
//   4. Map OCR players to DB player IDs; collect unresolved warnings
//   5. Group stats by entityId (Player scope and Team scope)
//   6. UPSERT match_player_stats for every resolved player (transaction)
//   7. UPSERT match_team_stats for Home and Away (same transaction)
//   8. Return { success, match_id, imported_players, warnings }
//
// Idempotent: re-importing the same match overwrites existing rows
// via ON DUPLICATE KEY UPDATE (UNIQUE key: match_id + player_id / team_id).
//
// TODO (schema additions pending):
//   - match_player_stats: add columns `source` (varchar) and `stats_version` (int)
//   - match_team_stats:   add columns `source` (varchar) and `stats_version` (int)
//   - Create table `ocr_import_log` for full audit trail per import attempt
// ============================================================

function handle_import(PDO $pdo, int $match_id)
{
    // ── 1. Parse request body ────────────────────────────────
    $body = file_get_contents('php://input');

    if ($body === false || $body === '') {
        send_error('Request body is empty', 400);
    }

    $data = json_decode($body, true);

    if (json_last_error() !== JSON_ERROR_NONE) {
        send_error('Invalid JSON: ' . json_last_error_msg(), 400);
    }

    if (!is_array($data)) {
        send_error('Request body must be a JSON object', 400);
    }

    // ── 2. Resolve home_team_id / away_team_id ───────────────
    $stmt = $pdo->prepare(
        'SELECT mt.team_id, mt.side, t.name, t.short_name
         FROM match_teams mt
         INNER JOIN teams t ON t.id = mt.team_id
         WHERE mt.match_id = ?'
    );
    $stmt->execute([$match_id]);
    $sides = $stmt->fetchAll();

    $home_team_id = null;
    $away_team_id = null;
    $match_teams = [];

    foreach ($sides as $row) {
        $side = (string)$row['side'];
        $team = [
            'team_id' => (int)$row['team_id'],
            'name' => (string)$row['name'],
            'short_name' => $row['short_name'],
        ];
        $match_teams[$side] = $team;

        if ($side === 'Home') $home_team_id = $team['team_id'];
        if ($side === 'Away') $away_team_id = $team['team_id'];
    }

    if (!$home_team_id || !$away_team_id) {
        send_error("match_id=$match_id not found or has no teams in match_teams", 404);
    }

    // ── 2b. Stop accidental imports into the wrong match ─────
    $team_mismatch = detect_team_mismatch($data, $match_teams);
    if ($team_mismatch !== null && !allow_team_mismatch_requested()) {
        send_json([
            'error' => 'Team mismatch: PDF teams do not match the selected match. Re-run import only after explicit confirmation.',
            'teamMismatch' => $team_mismatch,
        ], 409);
    }

    // ── 3. Build roster map: [team_id][jersey_number] => player_id ──
    $stmt = $pdo->prepare(
        'SELECT team_id, player_id, jersey_number
         FROM team_rosters
         WHERE team_id IN (?, ?) AND is_active = 1'
    );
    $stmt->execute([$home_team_id, $away_team_id]);

    $roster_map = [];
    foreach ($stmt->fetchAll() as $r) {
        $roster_map[(int)$r['team_id']][(int)$r['jersey_number']] = (int)$r['player_id'];
    }

    // ── 4. Map OCR players → DB player IDs ──────────────────
    $players_raw = $data['players'] ?? [];
    $stats_raw   = $data['stats']   ?? [];

    // entityId → ['team_id', 'player_id', 'jersey', 'is_starter', 'dnp']
    $entity_info = [];
    $warnings    = [];

    foreach ($players_raw as $p) {
        $entity_id = $p['entityId'] ?? '';
        $side      = $p['side']     ?? '';

        // Map OCR side label to DB team_id
        if ($side === 'Home') {
            $team_id = $home_team_id;
        } elseif ($side === 'Away') {
            $team_id = $away_team_id;
        } else {
            $team_id = null;
        }

        if (!$team_id) {
            $warnings[] = "entityId=$entity_id: unknown side '$side' — skipped";
            continue;
        }

        // Strip the starter marker (*) before jersey lookup
        $jersey    = (int)str_replace('*', '', trim((string)($p['number'] ?? '')));
        $player_id = $roster_map[$team_id][$jersey] ?? null;

        if (!$player_id) {
            $warnings[] = "entityId=$entity_id: jersey #$jersey not found in roster for team_id=$team_id — skipped";
            continue;
        }

        $entity_info[$entity_id] = [
            'team_id'    => $team_id,
            'player_id'  => $player_id,
            'jersey'     => $jersey,
            'is_starter' => (bool)($p['starter']   ?? false),
            'dnp'        => (bool)($p['didNotPlay'] ?? false),
        ];
    }

    // ── 5. Group stats by entityId ───────────────────────────
    // player_stats: entityId → [statKey => value]
    // team_stats:   entityId → [statKey => value]
    $player_stats = [];
    $team_stats   = [];

    foreach ($stats_raw as $s) {
        $scope     = $s['scope']    ?? '';
        $entity_id = $s['entityId'] ?? '';
        $stat_key  = $s['statKey']  ?? '';
        $value     = $s['value']    ?? null;

        if ($scope === 'Player') {
            $player_stats[$entity_id][$stat_key] = $value;
        } elseif ($scope === 'Team') {
            $team_stats[$entity_id][$stat_key] = $value;
        }
    }

    // ── 6 & 7. Atomic transaction: UPSERT player + team stats ──
    $pdo->beginTransaction();
    $imported_players = 0;

    try {

        // ── match_player_stats ───────────────────────────────────
        // UNIQUE KEY: (match_id, player_id)
        // DNP players: did_not_play = 1, all stat columns NULL (not zero)
        // TODO: add columns `source` and `stats_version` when available in schema
        $sql_player = "
            INSERT INTO match_player_stats
                (match_id, team_id, player_id, jersey_number, is_starter, did_not_play,
                 minutes_seconds,
                 fg_made, fg_att, two_made, two_att, three_made, three_att,
                 ft_made, ft_att, reb_off, reb_def,
                 assists, turnovers, steals, blocks,
                 fouls_committed, fouls_drawn, plus_minus, evaluation, points)
            VALUES
                (?, ?, ?, ?, ?, ?,
                 ?,
                 ?, ?, ?, ?, ?, ?,
                 ?, ?, ?, ?,
                 ?, ?, ?, ?,
                 ?, ?, ?, ?, ?)
            ON DUPLICATE KEY UPDATE
                team_id         = VALUES(team_id),
                jersey_number   = VALUES(jersey_number),
                is_starter      = VALUES(is_starter),
                did_not_play    = VALUES(did_not_play),
                minutes_seconds = VALUES(minutes_seconds),
                fg_made         = VALUES(fg_made),
                fg_att          = VALUES(fg_att),
                two_made        = VALUES(two_made),
                two_att         = VALUES(two_att),
                three_made      = VALUES(three_made),
                three_att       = VALUES(three_att),
                ft_made         = VALUES(ft_made),
                ft_att          = VALUES(ft_att),
                reb_off         = VALUES(reb_off),
                reb_def         = VALUES(reb_def),
                assists         = VALUES(assists),
                turnovers       = VALUES(turnovers),
                steals          = VALUES(steals),
                blocks          = VALUES(blocks),
                fouls_committed = VALUES(fouls_committed),
                fouls_drawn     = VALUES(fouls_drawn),
                plus_minus      = VALUES(plus_minus),
                evaluation      = VALUES(evaluation),
                points          = VALUES(points)
        ";
        $stmt_player = $pdo->prepare($sql_player);

        foreach ($entity_info as $entity_id => $info) {
            // DNP players: clear all stats to NULL as per contract
            $ps      = $info['dnp'] ? [] : ($player_stats[$entity_id] ?? []);
            $minutes = $info['dnp'] ? null : parse_minutes($ps['minutes'] ?? null);

            $stmt_player->execute([
                $match_id,
                $info['team_id'],
                $info['player_id'],
                $info['jersey'],
                $info['is_starter'] ? 1 : 0,
                $info['dnp']        ? 1 : 0,
                $minutes,
                int_or_null($ps['fieldGoals.made']       ?? null),
                int_or_null($ps['fieldGoals.attempted']  ?? null),
                int_or_null($ps['twoPoints.made']        ?? null),
                int_or_null($ps['twoPoints.attempted']   ?? null),
                int_or_null($ps['threePoints.made']      ?? null),
                int_or_null($ps['threePoints.attempted'] ?? null),
                int_or_null($ps['freeThrows.made']       ?? null),
                int_or_null($ps['freeThrows.attempted']  ?? null),
                int_or_null($ps['rebounds.offensive']    ?? null),
                int_or_null($ps['rebounds.defensive']    ?? null),
                int_or_null($ps['assists']               ?? null),
                int_or_null($ps['turnovers']             ?? null),
                int_or_null($ps['steals']                ?? null),
                int_or_null($ps['blocks']                ?? null),
                int_or_null($ps['fouls.committed']       ?? null),
                int_or_null($ps['fouls.drawn']           ?? null),
                int_or_null($ps['plusMinus']             ?? null),
                int_or_null($ps['evaluation']            ?? null),
                int_or_null($ps['points']                ?? null),
            ]);

            $imported_players++;
        }

        // ── match_team_stats ─────────────────────────────────────
        // UNIQUE KEY: (match_id, team_id)
        // Skipped silently if the JSON contains no team-scope stats for a side
        // TODO: add columns `source` and `stats_version` when available in schema
        // TODO: insert into `ocr_import_log` when the table is created
        $sql_team = "
            INSERT INTO match_team_stats
                (match_id, team_id,
                 fg_made, fg_att, two_made, two_att, three_made, three_att,
                 ft_made, ft_att, reb_off, reb_def,
                 assists, turnovers, steals, blocks,
                 fouls_committed, fouls_drawn, points,
                 points_in_paint, fast_break_points, fast_break_points_off_turnovers,
                 second_chance_points, points_off_turnovers, bench_points,
                 biggest_lead, biggest_run, lead_changes, times_tied, time_in_lead)
            VALUES
                (?, ?,
                 ?, ?, ?, ?, ?, ?,
                 ?, ?, ?, ?,
                 ?, ?, ?, ?,
                 ?, ?, ?,
                 ?, ?, ?,
                 ?, ?, ?,
                 ?, ?, ?, ?, ?)
            ON DUPLICATE KEY UPDATE
                fg_made                         = VALUES(fg_made),
                fg_att                          = VALUES(fg_att),
                two_made                        = VALUES(two_made),
                two_att                         = VALUES(two_att),
                three_made                      = VALUES(three_made),
                three_att                       = VALUES(three_att),
                ft_made                         = VALUES(ft_made),
                ft_att                          = VALUES(ft_att),
                reb_off                         = VALUES(reb_off),
                reb_def                         = VALUES(reb_def),
                assists                         = VALUES(assists),
                turnovers                       = VALUES(turnovers),
                steals                          = VALUES(steals),
                blocks                          = VALUES(blocks),
                fouls_committed                 = VALUES(fouls_committed),
                fouls_drawn                     = VALUES(fouls_drawn),
                points                          = VALUES(points),
                points_in_paint                 = VALUES(points_in_paint),
                fast_break_points               = VALUES(fast_break_points),
                fast_break_points_off_turnovers = VALUES(fast_break_points_off_turnovers),
                second_chance_points            = VALUES(second_chance_points),
                points_off_turnovers            = VALUES(points_off_turnovers),
                bench_points                    = VALUES(bench_points),
                biggest_lead                    = VALUES(biggest_lead),
                biggest_run                     = VALUES(biggest_run),
                lead_changes                    = VALUES(lead_changes),
                times_tied                      = VALUES(times_tied),
                time_in_lead                    = VALUES(time_in_lead)
        ";
        $stmt_team = $pdo->prepare($sql_team);

        // 'team:Home' and 'team:Away' are the canonical entityIds used by the OCR engine
        $team_sides_map = [
            'team:Home' => $home_team_id,
            'team:Away' => $away_team_id,
        ];

        foreach ($team_sides_map as $entity_id => $team_id) {
            $ts = $team_stats[$entity_id] ?? [];

            // Skip if the OCR produced no team-level stats for this side
            if (empty($ts)) {
                continue;
            }

            $stmt_team->execute([
                $match_id,
                $team_id,
                int_or_null($ts['fieldGoals.made']                 ?? null),
                int_or_null($ts['fieldGoals.attempted']            ?? null),
                int_or_null($ts['twoPoints.made']                  ?? null),
                int_or_null($ts['twoPoints.attempted']             ?? null),
                int_or_null($ts['threePoints.made']                ?? null),
                int_or_null($ts['threePoints.attempted']           ?? null),
                int_or_null($ts['freeThrows.made']                 ?? null),
                int_or_null($ts['freeThrows.attempted']            ?? null),
                int_or_null($ts['rebounds.offensive']              ?? null),
                int_or_null($ts['rebounds.defensive']              ?? null),
                int_or_null($ts['assists']                         ?? null),
                int_or_null($ts['turnovers']                       ?? null),
                int_or_null($ts['steals']                          ?? null),
                int_or_null($ts['blocks']                          ?? null),
                int_or_null($ts['fouls.committed']                 ?? null),
                int_or_null($ts['fouls.drawn']                     ?? null),
                int_or_null($ts['points']                          ?? null),
                int_or_null($ts['points_in_paint']                 ?? null),
                int_or_null($ts['fast_break_points']               ?? null),
                int_or_null($ts['fast_break_points_off_turnovers'] ?? null),
                int_or_null($ts['second_chance_points']            ?? null),
                int_or_null($ts['points_off_turnovers']            ?? null),
                int_or_null($ts['bench_points']                    ?? null),
                int_or_null($ts['biggest_lead']                    ?? null),
                str_or_null($ts['biggest_run']                     ?? null),
                int_or_null($ts['lead_changes']                    ?? null),
                int_or_null($ts['times_tied']                      ?? null),
                str_or_null($ts['time_in_lead']                    ?? null), // stored as "MM:SS" varchar
            ]);
        }

        $pdo->commit();

    } catch (Throwable $e) {
        $pdo->rollBack();
        error_log('[api-ocr/import] transaction rollback: ' . $e->getMessage());
        send_error('Import failed: ' . $e->getMessage(), 500);
    }

    // ── 8. Return summary ────────────────────────────────────
    send_json([
        'success'          => true,
        'match_id'         => $match_id,
        'imported_players' => $imported_players,
        'warnings'         => $warnings,
    ]);
}

function allow_team_mismatch_requested()
{
    $value = strtolower(trim((string)($_SERVER['HTTP_X_ALLOW_TEAM_MISMATCH'] ?? '')));
    return in_array($value, ['1', 'true', 'yes'], true);
}

function detect_team_mismatch(array $data, array $match_teams)
{
    $pdf_teams = teams_by_side($data['teams'] ?? []);
    $mismatches = [];

    foreach (['Home', 'Away'] as $side) {
        $pdf_team = $pdf_teams[$side] ?? null;
        $db_team = $match_teams[$side] ?? null;

        if ($db_team === null) {
            continue;
        }

        if (!team_matches($pdf_team, $db_team)) {
            $mismatches[$side] = [
                'pdf' => display_pdf_team($pdf_team),
                'match' => display_db_team($db_team),
            ];
        }
    }

    return empty($mismatches) ? null : $mismatches;
}

function teams_by_side($teams)
{
    $by_side = [];
    if (!is_array($teams)) {
        return $by_side;
    }

    foreach ($teams as $team) {
        if (!is_array($team)) {
            continue;
        }

        $side = (string)($team['side'] ?? '');
        if ($side === 'Home' || $side === 'Away') {
            $by_side[$side] = $team;
        }
    }

    return $by_side;
}

function team_matches($pdf_team, array $db_team)
{
    if (!is_array($pdf_team)) {
        return false;
    }

    $pdf_names = [
        normalize_team_name($pdf_team['name'] ?? ''),
        normalize_team_name($pdf_team['abbreviation'] ?? ''),
    ];
    $db_names = [
        normalize_team_name($db_team['name'] ?? ''),
        normalize_team_name($db_team['short_name'] ?? ''),
    ];

    foreach ($pdf_names as $pdf_name) {
        if ($pdf_name === '') {
            continue;
        }

        foreach ($db_names as $db_name) {
            if ($db_name !== '' && $pdf_name === $db_name) {
                return true;
            }
        }
    }

    return false;
}

function normalize_team_name($value)
{
    $text = strtolower(trim((string)$value));
    $text = strtr($text, [
        'à' => 'a', 'á' => 'a', 'â' => 'a', 'ä' => 'a',
        'è' => 'e', 'é' => 'e', 'ê' => 'e', 'ë' => 'e',
        'ì' => 'i', 'í' => 'i', 'î' => 'i', 'ï' => 'i',
        'ò' => 'o', 'ó' => 'o', 'ô' => 'o', 'ö' => 'o',
        'ù' => 'u', 'ú' => 'u', 'û' => 'u', 'ü' => 'u',
    ]);
    return preg_replace('/[^a-z0-9]+/', '', $text);
}

function display_pdf_team($team)
{
    if (!is_array($team)) {
        return 'not read';
    }

    $name = trim((string)($team['name'] ?? ''));
    $abbreviation = trim((string)($team['abbreviation'] ?? ''));
    if ($name === '') {
        return 'not read';
    }

    return $abbreviation === '' ? $name : $name . ' (' . $abbreviation . ')';
}

function display_db_team(array $team)
{
    $name = trim((string)($team['name'] ?? ''));
    $short_name = trim((string)($team['short_name'] ?? ''));
    return $short_name === '' ? $name : $name . ' (' . $short_name . ')';
}
