<?php
// ============================================================
// api/endpoints/players.php
//
// GET /api/giocatori                 → lista giocatori con medie (compat)
// GET /api/giocatori/{slug}          → dettaglio giocatore con matchLog (compat)
// GET /api/players/{id}/edition-stats?edition_id={id} → nuova API
// ============================================================

// ── Query condivisa: aggregati + medie per giocatore ────────

function fetch_players_with_stats(PDO $pdo, int $edition_id, bool $only_with_stats = false): array {
    $sql = "
        SELECT
            p.id AS player_id,
            p.first_name,
            p.last_name,
            p.photo_path,

            t.id AS team_id,
            t.name AS team_name,

            tr.jersey_number,
            tr.is_captain,

            COALESCE(stats.games_played, 0) AS games_played,
            COALESCE(stats.total_points, 0) AS total_points,
            COALESCE(stats.total_assists, 0) AS total_assists,
            COALESCE(stats.total_reb_off, 0) AS total_reb_off,
            COALESCE(stats.total_reb_def, 0) AS total_reb_def,
            COALESCE(stats.total_reb, 0) AS total_reb,
            COALESCE(stats.total_steals, 0) AS total_steals,
            COALESCE(stats.total_blocks, 0) AS total_blocks,
            COALESCE(stats.total_turnovers, 0) AS total_turnovers,
            COALESCE(stats.total_ff, 0) AS total_ff,
            COALESCE(stats.total_fs, 0) AS total_fs,
            COALESCE(stats.total_plus_minus, 0) AS total_plus_minus,
            COALESCE(stats.total_eval, 0) AS total_eval,
            COALESCE(stats.two_made, 0) AS two_made,
            COALESCE(stats.two_att, 0) AS two_att,
            COALESCE(stats.three_made, 0) AS three_made,
            COALESCE(stats.three_att, 0) AS three_att,
            COALESCE(stats.ft_made, 0) AS ft_made,
            COALESCE(stats.ft_att, 0) AS ft_att
        FROM team_rosters tr
        INNER JOIN teams t
            ON t.id = tr.team_id
           AND t.edition_id = ?
        INNER JOIN players p
            ON p.id = tr.player_id
        LEFT JOIN (
            SELECT
                mps.player_id,
                mps.team_id,
                COUNT(DISTINCT mps.match_id) AS games_played,
                COALESCE(SUM(mps.points), 0) AS total_points,
                COALESCE(SUM(mps.assists), 0) AS total_assists,
                COALESCE(SUM(mps.reb_off), 0) AS total_reb_off,
                COALESCE(SUM(mps.reb_def), 0) AS total_reb_def,
                COALESCE(SUM(mps.reb_off), 0) + COALESCE(SUM(mps.reb_def), 0) AS total_reb,
                COALESCE(SUM(mps.steals), 0) AS total_steals,
                COALESCE(SUM(mps.blocks), 0) AS total_blocks,
                COALESCE(SUM(mps.turnovers), 0) AS total_turnovers,
                COALESCE(SUM(mps.fouls_committed), 0) AS total_ff,
                COALESCE(SUM(mps.fouls_drawn), 0) AS total_fs,
                COALESCE(SUM(mps.plus_minus), 0) AS total_plus_minus,
                COALESCE(SUM(mps.evaluation), 0) AS total_eval,
                COALESCE(SUM(mps.two_made), 0) AS two_made,
                COALESCE(SUM(mps.two_att), 0) AS two_att,
                COALESCE(SUM(mps.three_made), 0) AS three_made,
                COALESCE(SUM(mps.three_att), 0) AS three_att,
                COALESCE(SUM(mps.ft_made), 0) AS ft_made,
                COALESCE(SUM(mps.ft_att), 0) AS ft_att
            FROM match_player_stats mps
            INNER JOIN matches m
                ON m.id = mps.match_id
               AND m.edition_id = ?
            WHERE mps.did_not_play = 0
            GROUP BY
                mps.player_id,
                mps.team_id
        ) stats
            ON stats.player_id = tr.player_id
           AND stats.team_id = tr.team_id
        WHERE tr.is_active = 1
          AND (NOT ? OR COALESCE(stats.games_played, 0) > 0)
        ORDER BY total_points DESC, p.last_name, p.first_name
    ";
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$edition_id, $edition_id, $only_with_stats ? 1 : 0]);
    return $stmt->fetchAll();
}

// Converte una riga di aggregati in un oggetto Player del frontend.
function row_to_player(array $r): array {
    $gp = max(1, (int)$r['games_played']);
    return [
        'slug'       => player_slug($r['first_name'], $r['last_name'], $r['jersey_number'] ?? null),
        'name'       => trim($r['first_name'] . ' ' . $r['last_name']),
        'team'       => $r['team_name'],
        'number'     => $r['jersey_number'] !== null ? (int)$r['jersey_number'] : null,
        'photo'      => $r['photo_path'] ? ('/img/giocatori/' . $r['photo_path']) : null,
        'pts'        => avg((int)$r['total_points'],    $gp),
        'ast'        => avg((int)$r['total_assists'],   $gp),
        'reb'        => avg((int)$r['total_reb'],       $gp),
        'rebOff'     => avg((int)$r['total_reb_off'],   $gp),
        'rebDef'     => avg((int)$r['total_reb_def'],   $gp),
        'stl'        => avg((int)$r['total_steals'],    $gp),
        'pp'         => avg((int)$r['total_turnovers'], $gp),
        'sd'         => avg((int)$r['total_blocks'],    $gp),
        'ff'         => avg((int)$r['total_ff'],        $gp),
        'fs'         => avg((int)$r['total_fs'],        $gp),
        'plusMinus'  => avg((int)$r['total_plus_minus'],$gp),
        'val'        => avg((int)$r['total_eval'],      $gp),
        'p2pct'      => shooting_pct((int)$r['two_made'],   (int)$r['two_att']),
        'p3pct'      => shooting_pct((int)$r['three_made'], (int)$r['three_att']),
        'tlpct'      => shooting_pct((int)$r['ft_made'],    (int)$r['ft_att']),
        'matchLog'   => [],
    ];
}

// ── GET /api/giocatori (COMPAT) ─────────────────────────────

function handle_giocatori_list(PDO $pdo): void {
    require_method('GET');

    $eid = get_active_edition_id($pdo);
    if (!$eid) {
        send_json([]);
        return;
    }

    $rows   = fetch_players_with_stats($pdo, $eid);
    $result = array_map('row_to_player', $rows);
    send_json($result);
}

// ── GET /api/giocatori/{slug} (COMPAT) ──────────────────────

function handle_giocatore_detail(PDO $pdo, string $slug): void {
    require_method('GET');

    $eid = get_active_edition_id($pdo);
    if (!$eid) {
        send_error('Nessuna edizione attiva', 404);
    }

    $rows = fetch_players_with_stats($pdo, $eid);

    $target = null;
    foreach ($rows as $r) {
        if (player_slug($r['first_name'], $r['last_name'], $r['jersey_number'] ?? null) === $slug) {
            $target = $r;
            break;
        }
    }

    if (!$target) {
        send_error('Giocatore non trovato', 404);
    }

    $player = row_to_player($target);
    $player['matchLog'] = fetch_match_log($pdo, (int)$target['player_id'], (int)$target['team_id'], $eid);

    send_json($player);
}

// Recupera il log partite per un giocatore nell'edizione.
function fetch_match_log(PDO $pdo, int $player_id, int $team_id, int $edition_id): array {
    $sql = "
        SELECT
            m.id            AS match_id,
            m.scheduled_start_at AS scheduled_at,
            m.status,
            mt_h.score      AS final_home_score,
            mt_a.score      AS final_away_score,
            mt_h.team_id    AS home_team_id,
            mt_a.team_id    AS away_team_id,
            ht.name         AS home_name,
            at.name         AS away_name,
            mps.points,
            mps.assists,
            mps.reb_off,
            mps.reb_def,
            mps.steals,
            mps.did_not_play
        FROM match_player_stats mps
        JOIN matches m  ON m.id = mps.match_id AND m.edition_id = ?
        JOIN match_teams mt_h ON mt_h.match_id = m.id AND mt_h.side = 'Home'
        JOIN match_teams mt_a ON mt_a.match_id = m.id AND mt_a.side = 'Away'
        JOIN teams ht   ON ht.id = mt_h.team_id
        JOIN teams at   ON at.id = mt_a.team_id
        WHERE mps.player_id = ?
          AND mps.team_id = ?
        ORDER BY m.scheduled_start_at, m.id
    ";
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$edition_id, $player_id, $team_id]);
    $rows = $stmt->fetchAll();

    $log = [];
    foreach ($rows as $r) {
        $is_home  = (int)$r['home_team_id'] === $team_id;
        $opponent = $is_home ? $r['away_name'] : $r['home_name'];
        $dnp      = (bool)$r['did_not_play'];

        if ($r['status'] === 'Finished' && !$dnp) {
            $my_score  = $is_home ? (int)$r['final_home_score'] : (int)$r['final_away_score'];
            $opp_score = $is_home ? (int)$r['final_away_score'] : (int)$r['final_home_score'];
            $result    = $my_score > $opp_score ? 'V' : 'P';
        } else {
            $result = '-';
        }

        $reb = (int)($r['reb_off'] ?? 0) + (int)($r['reb_def'] ?? 0);

        $log[] = [
            'matchId'  => (string)$r['match_id'],
            'date'     => format_match_date($r['scheduled_at']),
            'opponent' => $opponent,
            'result'   => $result,
            'played'   => !$dnp,
            'pts'      => $dnp ? 0 : (int)($r['points']  ?? 0),
            'ast'      => $dnp ? 0 : (int)($r['assists']  ?? 0),
            'reb'      => $dnp ? 0 : $reb,
            'stl'      => $dnp ? 0 : (int)($r['steals']  ?? 0),
        ];
    }

    return $log;
}

// ── GET /api/players/{id}/edition-stats ─────────────────────

function handle_player_edition_stats(PDO $pdo, int $player_id): void {
    require_method('GET');

    $eid = intval_positive($_GET['edition_id'] ?? null)
        ?? get_active_edition_id($pdo);

    if (!$eid) {
        send_error('edition_id non valido o nessuna edizione attiva', 400);
    }

    // Aggregazione diretta da match_player_stats (senza v_player_edition_stats)
    $sql = "
        SELECT
            mps.player_id,
            mps.team_id,
            m.edition_id,
            p.first_name,
            p.last_name,
            t.name AS team_name,
            COUNT(DISTINCT mps.match_id) AS games_played,
            SUM(CASE WHEN mps.did_not_play = 0 THEN 1 ELSE 0 END) AS games_with_stats,
            COALESCE(SUM(CASE WHEN mps.did_not_play = 0 THEN mps.points    ELSE 0 END), 0) AS points,
            COALESCE(SUM(CASE WHEN mps.did_not_play = 0 THEN mps.assists   ELSE 0 END), 0) AS assists,
            COALESCE(SUM(CASE WHEN mps.did_not_play = 0 THEN COALESCE(mps.reb_off,0)+COALESCE(mps.reb_def,0) ELSE 0 END), 0) AS reb_tot,
            COALESCE(SUM(CASE WHEN mps.did_not_play = 0 THEN mps.steals    ELSE 0 END), 0) AS steals,
            COALESCE(SUM(CASE WHEN mps.did_not_play = 0 THEN mps.blocks    ELSE 0 END), 0) AS blocks,
            COALESCE(SUM(CASE WHEN mps.did_not_play = 0 THEN mps.evaluation ELSE 0 END), 0) AS evaluation
        FROM match_player_stats mps
        JOIN matches m ON m.id = mps.match_id AND m.edition_id = ?
        JOIN players p ON p.id = mps.player_id
        JOIN teams   t ON t.id = mps.team_id
        WHERE mps.player_id = ?
        GROUP BY mps.player_id, mps.team_id, m.edition_id, p.first_name, p.last_name, t.name
    ";
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$eid, $player_id]);
    $row = $stmt->fetch();

    if (!$row) {
        send_error('Statistiche giocatore non trovate', 404);
    }

    $gp = max(1, (int)$row['games_played']);

    send_json([
        'player_id'        => $player_id,
        'edition_id'       => $eid,
        'first_name'       => $row['first_name'],
        'last_name'        => $row['last_name'],
        'team_name'        => $row['team_name'],
        'games_played'     => (int)$row['games_played'],
        'games_with_stats' => (int)$row['games_with_stats'],
        'points_total'     => (int)$row['points'],
        'assists_total'    => (int)$row['assists'],
        'reb_tot_total'    => (int)$row['reb_tot'],
        'steals_total'     => (int)$row['steals'],
        'blocks_total'     => (int)$row['blocks'],
        'evaluation_total' => (int)$row['evaluation'],
        'pts_avg'          => avg((int)$row['points'],     $gp),
        'ast_avg'          => avg((int)$row['assists'],    $gp),
        'reb_avg'          => avg((int)$row['reb_tot'],    $gp),
        'stl_avg'          => avg((int)$row['steals'],     $gp),
        'blk_avg'          => avg((int)$row['blocks'],     $gp),
        'eval_avg'         => avg((int)$row['evaluation'], $gp),
    ]);
}
