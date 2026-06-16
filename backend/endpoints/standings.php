<?php
// ============================================================
// api/endpoints/standings.php
//
// GET /api/standings?edition_id={id}
//
// Schema Sql1938817_1 — standings table:
//   - NON ha edition_id → filtro via group_id → tournament_groups.edition_id
//   - Colonne: played (non games_played), point_difference (non point_diff),
//              ranking_points (pre-computato, non wins*2+losses)
//   - position: rank locale nel girone (già salvato dalla C# app)
// ============================================================

function handle_standings(PDO $pdo): void {
    require_method('GET');

    $edition_id = intval_positive($_GET['edition_id'] ?? null)
               ?? get_active_edition_id($pdo);

    if (!$edition_id) {
        send_error('edition_id non valido o nessuna edizione attiva', 400);
    }

    // standings non ha edition_id: si filtra tramite group_id → tournament_groups
    $sql = "
        SELECT
            s.team_id,
            t.name          AS team_name,
            t.short_name,
            tg.code         AS group_code,
            tg.name         AS group_name,
            tg.sort_order   AS group_order,
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
                 t.name
    ";

    $stmt = $pdo->prepare($sql);
    $stmt->execute([$edition_id]);
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
