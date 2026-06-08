<?php
// ============================================================
// api/endpoints/teams.php
//
// GET /api/squadre                          → compat frontend esistente
// GET /api/teams?edition_id={id}            → lista squadre nuova API
// GET /api/teams/{id}/roster               → roster squadra
// GET /api/teams/{id}/edition-stats?edition_id={id} → stat squadra
// ============================================================

// ── GET /api/squadre (COMPAT) ───────────────────────────────
// Risposta: [{id, nome, girone}]
// Tabelle: teams JOIN group_teams JOIN tournament_groups

function handle_squadre_compat(PDO $pdo): void {
    require_method('GET');

    $eid = get_active_edition_id($pdo);
    if (!$eid) {
        send_json([]); // nessuna edizione attiva → lista vuota (fallback frontend)
        return;
    }

    $sql = "
        SELECT
            t.id,
            t.name AS nome,
            COALESCE(tg.code, '') AS girone
        FROM teams t
        LEFT JOIN group_teams gt ON gt.team_id = t.id
        LEFT JOIN tournament_groups tg ON tg.id = gt.group_id
        WHERE t.edition_id = ?
        ORDER BY tg.sort_order, tg.code, t.name
    ";
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$eid]);
    $rows = $stmt->fetchAll();

    $result = [];
    foreach ($rows as $r) {
        $result[] = [
            'id'    => (int)$r['id'],
            'nome'  => $r['nome'],
            'girone'=> $r['girone'],
        ];
    }

    send_json($result);
}

// ── GET /api/teams ──────────────────────────────────────────
// Risposta: [{id, name, short_name, group_code, logo_path}]

function handle_teams_list(PDO $pdo): void {
    require_method('GET');

    $eid = intval_positive($_GET['edition_id'] ?? null)
        ?? get_active_edition_id($pdo);

    if (!$eid) {
        send_error('edition_id non valido o nessuna edizione attiva', 400);
    }

    $sql = "
        SELECT
            t.id,
            t.name,
            t.short_name,
            t.logo_path,
            t.primary_color,
            t.secondary_color,
            tg.code AS group_code,
            tg.name AS group_name
        FROM teams t
        LEFT JOIN group_teams gt  ON gt.team_id = t.id
        LEFT JOIN tournament_groups tg ON tg.id = gt.group_id
        WHERE t.edition_id = ?
        ORDER BY tg.sort_order, t.name
    ";
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$eid]);
    $rows = $stmt->fetchAll();

    $result = [];
    foreach ($rows as $r) {
        $result[] = [
            'id'              => (int)$r['id'],
            'name'            => $r['name'],
            'short_name'      => $r['short_name'],
            'logo_path'       => $r['logo_path'],
            'primary_color'   => $r['primary_color'],
            'secondary_color' => $r['secondary_color'],
            'group_code'      => $r['group_code'],
            'group_name'      => $r['group_name'],
        ];
    }

    send_json($result);
}

// ── GET /api/teams/{id}/roster ──────────────────────────────
// Risposta: { team: {...}, players: [{jersey_number, is_captain, player_id, ...}] }
// Tabelle: teams + team_rosters JOIN players

function handle_team_roster(PDO $pdo, int $team_id): void {
    require_method('GET');

    // Info squadra
    $stmt = $pdo->prepare("SELECT id, name, short_name, logo_path FROM teams WHERE id = ?");
    $stmt->execute([$team_id]);
    $team = $stmt->fetch();

    if (!$team) {
        send_error('Squadra non trovata', 404);
    }

    // Roster
    $sql = "
        SELECT
            tr.id,
            tr.jersey_number,
            tr.role,
            tr.is_captain,
            p.id         AS player_id,
            p.first_name,
            p.last_name
        FROM team_rosters tr
        JOIN players p ON p.id = tr.player_id
        WHERE tr.team_id = ?
        ORDER BY CAST(tr.jersey_number AS UNSIGNED), p.last_name, p.first_name
    ";
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$team_id]);
    $roster_rows = $stmt->fetchAll();

    $players = [];
    foreach ($roster_rows as $r) {
        $players[] = [
            'player_id'     => (int)$r['player_id'],
            'first_name'    => $r['first_name'],
            'last_name'     => $r['last_name'],
            'name'          => trim($r['first_name'] . ' ' . $r['last_name']),
            'slug'          => player_slug($r['first_name'], $r['last_name'], $r['jersey_number'] ?? null),
            'jersey_number' => $r['jersey_number'],
            'role'          => $r['role'],
            'is_captain'    => (bool)$r['is_captain'],
        ];
    }

    send_json([
        'team' => [
            'id'        => (int)$team['id'],
            'name'      => $team['name'],
            'short_name'=> $team['short_name'],
            'logo_path' => $team['logo_path'],
        ],
        'players' => $players,
    ]);
}

// ── GET /api/teams/{id}/edition-stats ──────────────────────
// Risposta: aggregati squadra per edizione (da match_team_stats)

function handle_team_edition_stats(PDO $pdo, int $team_id): void {
    require_method('GET');

    $eid = intval_positive($_GET['edition_id'] ?? null)
        ?? get_active_edition_id($pdo);

    if (!$eid) {
        send_error('edition_id non valido o nessuna edizione attiva', 400);
    }

    // Aggregazione diretta da match_team_stats (senza v_team_edition_stats)
    $sql = "
        SELECT
            mts.team_id,
            m.edition_id,
            t.name      AS team_name,
            t.short_name,
            COUNT(DISTINCT mts.match_id)                            AS games_with_stats,
            COALESCE(SUM(mts.points),             0)                AS points,
            COALESCE(SUM(mts.reb_off),            0)                AS reb_off,
            COALESCE(SUM(mts.reb_def),            0)                AS reb_def,
            COALESCE(SUM(mts.reb_off),0)+COALESCE(SUM(mts.reb_def),0) AS reb_tot,
            COALESCE(SUM(mts.assists),            0)                AS assists,
            COALESCE(SUM(mts.turnovers),          0)                AS turnovers,
            COALESCE(SUM(mts.steals),             0)                AS steals,
            COALESCE(SUM(mts.blocks),             0)                AS blocks,
            COALESCE(SUM(mts.points_in_paint),    0)                AS points_in_paint,
            COALESCE(SUM(mts.fast_break_points),  0)                AS fast_break_points,
            COALESCE(SUM(mts.second_chance_points),0)               AS second_chance_points,
            COALESCE(SUM(mts.points_off_turnovers),0)               AS points_off_turnovers,
            COALESCE(SUM(mts.bench_points),       0)                AS bench_points
        FROM match_team_stats mts
        JOIN matches m ON m.id = mts.match_id AND m.edition_id = ?
        JOIN teams   t ON t.id = mts.team_id
        WHERE mts.team_id = ?
        GROUP BY mts.team_id, m.edition_id, t.name, t.short_name
    ";
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$eid, $team_id]);
    $row = $stmt->fetch();

    if (!$row) {
        send_error('Statistiche squadra non trovate', 404);
    }

    $gp = max(1, (int)$row['games_with_stats']);

    send_json([
        'team_id'               => (int)$row['team_id'],
        'team_name'             => $row['team_name'],
        'team_short_name'       => $row['short_name'],
        'edition_id'            => $eid,
        'games_with_stats'      => (int)$row['games_with_stats'],
        'points'                => (int)$row['points'],
        'reb_off'               => (int)$row['reb_off'],
        'reb_def'               => (int)$row['reb_def'],
        'reb_tot'               => (int)$row['reb_tot'],
        'assists'               => (int)$row['assists'],
        'turnovers'             => (int)$row['turnovers'],
        'steals'                => (int)$row['steals'],
        'blocks'                => (int)$row['blocks'],
        'points_in_paint'       => (int)$row['points_in_paint'],
        'fast_break_points'     => (int)$row['fast_break_points'],
        'second_chance_points'  => (int)$row['second_chance_points'],
        'points_off_turnovers'  => (int)$row['points_off_turnovers'],
        'bench_points'          => (int)$row['bench_points'],
        // medie per partita
        'avg_points'            => avg((int)$row['points'],   $gp),
        'avg_assists'           => avg((int)$row['assists'],  $gp),
        'avg_reb'               => avg((int)$row['reb_tot'],  $gp),
    ]);
}
