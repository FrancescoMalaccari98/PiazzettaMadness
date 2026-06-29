<?php
// ============================================================
// api/endpoints/three_point_contest.php
//
// GET /api-web/three-point-contest → dati completi del 3PT Contest
// ============================================================

function handle_three_point_contest(PDO $pdo): void {
    require_method('GET');

    $eid = get_active_edition_id($pdo);
    if (!$eid) {
        send_json(['status' => 'none', 'message' => 'Nessuna edizione attiva']);
        return;
    }

    // Evento 3PT Contest per questa edizione
    $stmt = $pdo->prepare("
        SELECT id, name, status, scheduled_start_at, scheduled_end_at
        FROM competition_events
        WHERE edition_id = ? AND event_type = 'ThreePointContest'
        ORDER BY id DESC
        LIMIT 1
    ");
    $stmt->execute([$eid]);
    $event = $stmt->fetch();

    if (!$event) {
        send_json([
            'status'  => 'none',
            'message' => 'Nessun 3 Point Contest in programma',
            'entries' => [],
        ]);
        return;
    }

    $event_id = (int)$event['id'];

    // Iscritti con dati giocatore e squadra
    $stmt = $pdo->prepare("
        SELECT
            e.id            AS entry_id,
            e.seed_order,
            e.total_score,
            e.final_position,
            p.first_name,
            p.last_name,
            t.name          AS team_name,
            t.short_name    AS team_short,
            t.primary_color AS team_color,
            tr.jersey_number
        FROM three_point_contest_entries e
        JOIN players p ON p.id = e.player_id
        JOIN teams   t ON t.id = e.team_id
        LEFT JOIN team_rosters tr ON tr.player_id = e.player_id AND tr.team_id = e.team_id AND tr.is_active = 1
        WHERE e.competition_event_id = ?
        ORDER BY
            CASE WHEN e.final_position IS NOT NULL THEN 0 ELSE 1 END,
            e.final_position ASC,
            e.total_score DESC,
            e.seed_order ASC
    ");
    $stmt->execute([$event_id]);
    $entry_rows = $stmt->fetchAll();

    // Round per ogni iscritto
    $entry_ids = array_map(fn($e) => (int)$e['entry_id'], $entry_rows);
    $rounds_map = [];

    if (!empty($entry_ids)) {
        $placeholders = implode(',', array_fill(0, count($entry_ids), '?'));
        $stmt = $pdo->prepare("
            SELECT
                r.id            AS round_id,
                r.entry_id,
                r.round_number,
                r.round_type,
                r.station1_score,
                r.station2_score,
                r.station3_score,
                r.station4_score,
                r.station5_score,
                r.total_score
            FROM three_point_contest_rounds r
            WHERE r.entry_id IN ($placeholders)
            ORDER BY r.entry_id, r.round_number
        ");
        $stmt->execute($entry_ids);
        $all_rounds = $stmt->fetchAll();

        // Fetch tiri singoli per tutti i round trovati
        $round_ids = array_map(fn($r) => (int)$r['round_id'], $all_rounds);
        $shots_map = [];
        if (!empty($round_ids)) {
            $ph2 = implode(',', array_fill(0, count($round_ids), '?'));
            $stmt2 = $pdo->prepare("
                SELECT round_id, station_number, ball_number, point_value, result
                FROM three_point_contest_shots
                WHERE round_id IN ($ph2)
                ORDER BY round_id, station_number, ball_number
            ");
            $stmt2->execute($round_ids);
            foreach ($stmt2->fetchAll() as $s) {
                $rid  = (int)$s['round_id'];
                $snum = (int)$s['station_number'];
                $shots_map[$rid][$snum][] = [
                    'ball'   => (int)$s['ball_number'],
                    'value'  => (int)$s['point_value'],
                    'result' => $s['result'],
                ];
            }
        }

        foreach ($all_rounds as $r) {
            $eid_r  = (int)$r['entry_id'];
            $rid    = (int)$r['round_id'];
            if (!isset($rounds_map[$eid_r])) $rounds_map[$eid_r] = [];
            $rounds_map[$eid_r][] = [
                'round_number' => (int)$r['round_number'],
                'round_type'   => $r['round_type'],
                'stations'     => [
                    (int)$r['station1_score'],
                    (int)$r['station2_score'],
                    (int)$r['station3_score'],
                    (int)$r['station4_score'],
                    (int)$r['station5_score'],
                ],
                'total' => (int)$r['total_score'],
                'shots' => $shots_map[$rid] ?? [],
            ];
        }
    }

    $entries = [];
    foreach ($entry_rows as $e) {
        $eid_e = (int)$e['entry_id'];
        $entries[] = [
            'player'         => trim($e['first_name'] . ' ' . $e['last_name']),
            'jersey_number'  => $e['jersey_number'] !== null ? (int)$e['jersey_number'] : null,
            'team_color'     => $e['team_color'] ?? null,
            'team'           => $e['team_name'],
            'team_short'     => $e['team_short'],
            'seed_order'     => $e['seed_order'] !== null ? (int)$e['seed_order'] : null,
            'total_score'    => (int)$e['total_score'],
            'final_position' => $e['final_position'] !== null ? (int)$e['final_position'] : null,
            'rounds'         => $rounds_map[$eid_e] ?? [],
        ];
    }

    send_json([
        'status'        => $event['status'],
        'event_name'    => $event['name'],
        'scheduled_at'  => format_match_date($event['scheduled_start_at']),
        'entries'       => $entries,
    ]);
}
