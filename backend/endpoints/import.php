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

function handle_import(PDO $pdo, int $match_id): never
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
    $stmt = $pdo->prepare('SELECT team_id, side FROM match_teams WHERE match_id = ?');
    $stmt->execute([$match_id]);
    $sides = $stmt->fetchAll();

    $home_team_id = null;
    $away_team_id = null;

    foreach ($sides as $row) {
        if ($row['side'] === 'Home') $home_team_id = (int)$row['team_id'];
        if ($row['side'] === 'Away') $away_team_id = (int)$row['team_id'];
    }

    if (!$home_team_id || !$away_team_id) {
        send_error("match_id=$match_id not found or has no teams in match_teams", 404);
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
        $team_id = match($side) {
            'Home'  => $home_team_id,
            'Away'  => $away_team_id,
            default => null,
        };

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
