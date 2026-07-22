<?php
// ============================================================
// api/endpoints/winners.php
//
// GET /api-web/winners → albo d'oro per edizione conclusa
// ============================================================

function handle_winners(PDO $pdo): void {
    require_method('GET');

    $descriptions = load_winner_descriptions();

    $sql = "
        SELECT
            e.id,
            e.name,
            e.year,
            e.start_date,
            e.end_date,
            m.id AS final_match_id,
            mt_h.score AS final_home_score,
            mt_a.score AS final_away_score,
            ht.id AS home_team_id,
            ht.name AS home_team_name,
            at.id AS away_team_id,
            at.name AS away_team_name,
            wt.id AS champion_team_id,
            wt.name AS champion_team_name
        FROM editions e
        LEFT JOIN matches m
            ON m.edition_id = e.id
           AND LOWER(m.phase) = 'final'
           AND LOWER(m.status) = 'finished'
        LEFT JOIN match_teams mt_h
            ON mt_h.match_id = m.id
           AND mt_h.side = 'Home'
        LEFT JOIN match_teams mt_a
            ON mt_a.match_id = m.id
           AND mt_a.side = 'Away'
        LEFT JOIN teams ht ON ht.id = mt_h.team_id
        LEFT JOIN teams at ON at.id = mt_a.team_id
        LEFT JOIN teams wt ON wt.id = m.winner_team_id
        WHERE LOWER(e.status) = 'completed'
        ORDER BY e.year DESC, e.end_date DESC, e.id DESC
    ";
    $rows = $pdo->query($sql)->fetchAll();

    $three_point_winners = fetch_three_point_winners($pdo);

    $result = [];
    foreach ($rows as $row) {
        $year = (int)$row['year'];
        $cfg = $descriptions[(string)$year] ?? [];
        $three_point = $three_point_winners[(int)$row['id']] ?? null;

        $champion_description = $cfg['championDescription']
            ?? default_champion_description($row);
        $three_point_description = $cfg['threePointDescription']
            ?? default_three_point_description($three_point);

        $result[] = [
            'edition' => [
                'id'         => (int)$row['id'],
                'name'       => $row['name'],
                'year'       => $year,
                'start_date' => $row['start_date'],
                'end_date'   => $row['end_date'],
            ],
            'champion' => [
                'team_id'       => $row['champion_team_id'] !== null ? (int)$row['champion_team_id'] : null,
                'team_name'     => $row['champion_team_name'] ?? null,
                'photo'         => winner_photo_path($cfg['championPhoto'] ?? ('Winners_' . $year . '.jpg')),
                'description'   => $champion_description,
                'final_match'   => $row['final_match_id'] !== null ? [
                    'id'         => (int)$row['final_match_id'],
                    'home_team'  => $row['home_team_name'],
                    'away_team'  => $row['away_team_name'],
                    'home_score' => $row['final_home_score'] !== null ? (int)$row['final_home_score'] : null,
                    'away_score' => $row['final_away_score'] !== null ? (int)$row['final_away_score'] : null,
                ] : null,
            ],
            'threePoint' => $three_point ? [
                'player_id'     => (int)$three_point['player_id'],
                'player_name'   => trim($three_point['first_name'] . ' ' . $three_point['last_name']),
                'team_id'       => (int)$three_point['team_id'],
                'team_name'     => $three_point['team_name'],
                'jersey_number' => $three_point['jersey_number'] !== null ? (int)$three_point['jersey_number'] : null,
                'final_score'   => (int)$three_point['final_score'],
                'tiebreak_score'=> (int)$three_point['tiebreak_score'],
                'total_score'   => (int)$three_point['total_score'],
                'photo'         => winner_photo_path($cfg['threePointPhoto'] ?? ('Winners3PT_' . $year . '.jpg')),
                'description'   => $three_point_description,
            ] : null,
        ];
    }

    send_json(['editions' => $result]);
}

function fetch_three_point_winners(PDO $pdo): array {
    $stmt = $pdo->query("
        SELECT
            e.id AS edition_id,
            ce.id AS event_id,
            entry.id AS entry_id,
            entry.player_id,
            entry.team_id,
            entry.total_score,
            entry.final_position,
            p.first_name,
            p.last_name,
            t.name AS team_name,
            tr.jersey_number,
            COALESCE(SUM(CASE WHEN rounds.round_type = 'Final' THEN rounds.total_score ELSE 0 END), 0) AS final_score,
            COALESCE(SUM(CASE WHEN rounds.round_type = 'TieBreak' THEN rounds.total_score ELSE 0 END), 0) AS tiebreak_score
        FROM editions e
        JOIN competition_events ce
            ON ce.edition_id = e.id
           AND ce.event_type = 'ThreePointContest'
        JOIN three_point_contest_entries entry
            ON entry.competition_event_id = ce.id
        JOIN players p ON p.id = entry.player_id
        JOIN teams t ON t.id = entry.team_id
        LEFT JOIN team_rosters tr
            ON tr.team_id = entry.team_id
           AND tr.player_id = entry.player_id
           AND tr.is_active = 1
        LEFT JOIN three_point_contest_rounds rounds
            ON rounds.entry_id = entry.id
        WHERE LOWER(e.status) = 'completed'
        GROUP BY
            e.id,
            ce.id,
            entry.id,
            entry.player_id,
            entry.team_id,
            entry.total_score,
            entry.final_position,
            p.first_name,
            p.last_name,
            t.name,
            tr.jersey_number
        ORDER BY
            e.id ASC,
            CASE WHEN entry.final_position = 1 THEN 0 ELSE 1 END ASC,
            COALESCE(entry.final_position, 999) ASC,
            final_score DESC,
            tiebreak_score DESC,
            entry.total_score DESC,
            entry.id ASC
    ");

    $winners = [];
    foreach ($stmt->fetchAll() as $row) {
        $edition_id = (int)$row['edition_id'];
        if (!isset($winners[$edition_id])) {
            $winners[$edition_id] = $row;
        }
    }

    return $winners;
}

function load_winner_descriptions(): array {
    $path = __DIR__ . '/../config/winners-descriptions.json';
    if (!is_file($path)) return [];

    $raw = file_get_contents($path);
    if ($raw === false || trim($raw) === '') return [];

    $data = json_decode($raw, true);
    return is_array($data) ? $data : [];
}

function winner_photo_path(?string $filename): ?string {
    if ($filename === null || trim($filename) === '') return null;

    $safe = basename($filename);
    $path = dirname(__DIR__, 2) . '/img/winners/' . $safe;
    if (!is_file($path)) return null;

    return '/img/winners/' . $safe;
}

function default_champion_description(array $row): string {
    if (!empty($row['champion_team_name'])) {
        $score = '';
        if ($row['final_home_score'] !== null && $row['final_away_score'] !== null) {
            $score = ' con una finale chiusa ' . (int)$row['final_home_score'] . '-' . (int)$row['final_away_score'];
        }
        return $row['champion_team_name'] . ' conquista il torneo' . $score . '.';
    }

    return 'Edizione conclusa, vincitore in aggiornamento.';
}

function default_three_point_description(?array $winner): string {
    if (!$winner) {
        return 'Vincitore del 3 Point Contest in aggiornamento.';
    }

    $score = (int)$winner['final_score'];
    $extra = (int)$winner['tiebreak_score'] > 0
        ? ' + spareggio da ' . (int)$winner['tiebreak_score']
        : '';

    return trim($winner['first_name'] . ' ' . $winner['last_name']) . ' vince la gara da tre punti con ' . $score . ' punti in finale' . $extra . '.';
}
