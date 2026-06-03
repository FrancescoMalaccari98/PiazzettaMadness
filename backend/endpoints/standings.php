<?php
// ============================================================
// api/endpoints/standings.php
//
// GET /api/standings?edition_id={id}
//   → classifica ordinata per punti (V=2pt, P=1pt), poi diff canestri
//   → se edition_id assente usa l'edizione attiva
//
// Tabella/Vista: v_standings JOIN teams JOIN tournament_groups
// ============================================================

// Risposta:
// [
//   {
//     "rank": 1,
//     "team_id": 3,
//     "team_name": "Saluta Andonio Spurs",
//     "group_code": "A",
//     "games_played": 2,
//     "wins": 2,
//     "losses": 0,
//     "points_for": 156,
//     "points_against": 128,
//     "point_diff": 28,
//     "standing_points": 4,
//     "qualifies": true
//   }, ...
// ]

function handle_standings(PDO $pdo): void {
    require_method('GET');

    $edition_id = intval_positive($_GET['edition_id'] ?? null)
               ?? get_active_edition_id($pdo);

    if (!$edition_id) {
        send_error('edition_id non valido o nessuna edizione attiva', 400);
    }

    $sql = "
        SELECT
            vs.team_id,
            t.name          AS team_name,
            t.short_name,
            tg.code         AS group_code,
            tg.name         AS group_name,
            tg.sort_order   AS group_order,
            vs.games_played,
            vs.wins,
            vs.losses,
            vs.points_for,
            vs.points_against,
            vs.point_diff,
            (vs.wins * 2 + vs.losses) AS standing_points
        FROM v_standings vs
        JOIN teams t  ON t.id  = vs.team_id
        LEFT JOIN group_teams gt ON gt.team_id = vs.team_id
        LEFT JOIN tournament_groups tg
            ON tg.id = gt.group_id AND tg.edition_id = ?
        WHERE vs.edition_id = ?
        ORDER BY tg.sort_order, tg.id,
                 (vs.wins * 2 + vs.losses) DESC,
                 vs.point_diff DESC,
                 vs.points_for DESC,
                 t.name
    ";

    $stmt = $pdo->prepare($sql);
    $stmt->execute([$edition_id, $edition_id]);
    $rows = $stmt->fetchAll();

    // Raggruppa per girone e calcola rank locale
    $groups = [];
    foreach ($rows as $r) {
        $gcode = $r['group_code'] ?? 'N/A';
        $groups[$gcode][] = $r;
    }

    $result = [];
    foreach ($groups as $gcode => $group_rows) {
        $rank = 1;
        foreach ($group_rows as $r) {
            $result[] = [
                'rank'            => $rank,
                'team_id'         => (int)$r['team_id'],
                'team_name'       => $r['team_name'],
                'team_short_name' => $r['short_name'],
                'group_code'      => $r['group_code'],
                'group_name'      => $r['group_name'],
                'games_played'    => (int)$r['games_played'],
                'wins'            => (int)$r['wins'],
                'losses'          => (int)$r['losses'],
                'points_for'      => (int)$r['points_for'],
                'points_against'  => (int)$r['points_against'],
                'point_diff'      => (int)$r['point_diff'],
                'standing_points' => (int)$r['standing_points'],
                'qualifies'       => $rank <= 2,
            ];
            $rank++;
        }
    }

    send_json($result);
}
