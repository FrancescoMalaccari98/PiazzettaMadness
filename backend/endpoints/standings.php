<?php
// ============================================================
// api/endpoints/standings.php
//
// GET /api/standings?edition_id={id}
<<<<<<< HEAD
//
// Schema Sql1938817_1 — standings table:
//   - NON ha edition_id → filtro via group_id → tournament_groups.edition_id
//   - Colonne: played (non games_played), point_difference (non point_diff),
//              ranking_points (pre-computato, non wins*2+losses)
//   - position: rank locale nel girone (già salvato dalla C# app)
// ============================================================

=======
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

>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
function handle_standings(PDO $pdo): void {
    require_method('GET');

    $edition_id = intval_positive($_GET['edition_id'] ?? null)
               ?? get_active_edition_id($pdo);

    if (!$edition_id) {
        send_error('edition_id non valido o nessuna edizione attiva', 400);
    }

<<<<<<< HEAD
    // standings non ha edition_id: si filtra tramite group_id → tournament_groups
    $sql = "
        SELECT
            s.team_id,
=======
    $sql = "
        SELECT
            vs.team_id,
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
            t.name          AS team_name,
            t.short_name,
            tg.code         AS group_code,
            tg.name         AS group_name,
            tg.sort_order   AS group_order,
<<<<<<< HEAD
            s.played        AS games_played,
            s.wins,
            s.losses,
            s.points_for,
            s.points_against,
            s.point_difference AS point_diff,
            s.ranking_points   AS standing_points
        FROM standings s
        JOIN tournament_groups tg ON tg.id = s.group_id AND tg.edition_id = ?
        JOIN teams t ON t.id = s.team_id
        ORDER BY tg.sort_order, tg.id,
                 s.ranking_points DESC,
                 s.point_difference DESC,
                 s.points_for DESC,
=======
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
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
                 t.name
    ";

    $stmt = $pdo->prepare($sql);
<<<<<<< HEAD
    $stmt->execute([$edition_id]);
=======
    $stmt->execute([$edition_id, $edition_id]);
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
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
