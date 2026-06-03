<?php
// ============================================================
// api/endpoints/stats.php
//
// GET /api/statistiche              → StatsData completo (compat frontend)
// GET /api/matches/{id}/stats       → box score + team stats per partita
// ============================================================

// Dipendenza esplicita: fetch_players_with_stats() e row_to_player()
// sono definite in players.php. Il require_once è idempotente.
require_once __DIR__ . '/players.php';

// ── GET /api/statistiche (COMPAT) ───────────────────────────
// Risposta: StatsData atteso da Stats.tsx
// { tournamentMvpSlug, players:Player[], matchMvps:MatchMvp[], teamStats:TeamStats[] }

function handle_statistiche_compat(PDO $pdo): void {
    require_method('GET');

    $eid = get_active_edition_id($pdo);
    if (!$eid) {
        send_json([
            'tournamentMvpSlug' => '',
            'players'           => [],
            'matchMvps'         => [],
            'teamStats'         => [],
        ]);
        return;
    }

    // ── 1. Giocatori con medie ──────────────────────────────
    $rows    = fetch_players_with_stats($pdo, $eid);
    $players = [];
    foreach ($rows as $r) {
        $p = row_to_player($r); // matchLog è già [] in row_to_player
        $players[] = $p;
    }

    // ── 2. MVP del torneo: giocatore con più punti totali ───
    // Il slug DEVE essere generato con gli stessi parametri di row_to_player
    // (nome + jersey_number) altrimenti non coincide con player.slug in players[].
    $mvp_slug = '';
    if (!empty($rows)) {
        $mvp_slug = player_slug(
            $rows[0]['first_name'],
            $rows[0]['last_name'],
            $rows[0]['jersey_number'] ?? null  // bug fix: era senza jersey_number
        );
    }

    // ── 3. MVP per partita: top scorer di ogni gara conclusa ──
    $sql_mvp = "
        SELECT
            m.id            AS match_id,
            m.scheduled_at,
            m.round_label,
            ht.name         AS home_name,
            at.name         AS away_name,
            p.first_name,
            p.last_name,
            t.name          AS team_name,
            mps.points
        FROM match_player_stats mps
        JOIN (
            SELECT match_id, MAX(points) AS max_pts
            FROM match_player_stats
            WHERE did_not_play = 0
            GROUP BY match_id
        ) top ON top.match_id = mps.match_id AND top.max_pts = mps.points
        JOIN matches m  ON m.id = mps.match_id AND m.edition_id = ? AND m.status = 'finished'
        JOIN teams ht   ON ht.id = m.home_team_id
        JOIN teams at   ON at.id = m.away_team_id
        JOIN players p  ON p.id  = mps.player_id
        JOIN teams   t  ON t.id  = mps.team_id
        WHERE mps.did_not_play = 0
        ORDER BY m.scheduled_at, m.id
    ";
    $stmt = $pdo->prepare($sql_mvp);
    $stmt->execute([$eid]);
    $mvp_rows = $stmt->fetchAll();

    $match_mvps = [];
    foreach ($mvp_rows as $r) {
        $desc = $r['home_name'] . ' vs ' . $r['away_name'];
        if ($r['round_label']) $desc = $r['round_label'] . ': ' . $desc;
        $match_mvps[] = [
            'match'  => $desc,
            'date'   => format_match_date($r['scheduled_at']),
            'player' => trim($r['first_name'] . ' ' . $r['last_name']),
            'team'   => $r['team_name'],
            'stat'   => $r['points'] . ' pts',
        ];
    }

    // ── 4. Statistiche squadra aggregate ────────────────────
    $sql_team = "
        SELECT
            t.name          AS squadra,
            COUNT(DISTINCT m.id)                            AS partiteGiocate,
            COALESCE(SUM(mts.points_in_paint),      0)      AS puntiInArea,
            COALESCE(SUM(mts.bench_points),         0)      AS puntiPanchina,
            COALESCE(SUM(mts.fast_break_points),    0)      AS puntiContropiede,
            COALESCE(SUM(mts.points_off_turnovers), 0)      AS puntiDaPallePerse,
            ROUND(AVG(COALESCE(mts.points_per_possession, 0)), 3) AS pointsPerPossession,
            COALESCE(MAX(mts.biggest_lead),         0)      AS massimoVantaggio
        FROM match_team_stats mts
        JOIN matches m ON m.id = mts.match_id AND m.edition_id = ?
        JOIN teams   t ON t.id = mts.team_id
        GROUP BY t.id, t.name
        ORDER BY t.name
    ";
    $stmt = $pdo->prepare($sql_team);
    $stmt->execute([$eid]);
    $team_rows = $stmt->fetchAll();

    $team_stats = [];
    foreach ($team_rows as $r) {
        $team_stats[] = [
            'squadra'               => $r['squadra'],
            'partiteGiocate'        => (int)$r['partiteGiocate'],
            'puntiInArea'           => (int)$r['puntiInArea'],
            'puntiPanchina'         => (int)$r['puntiPanchina'],
            'puntiContropiede'      => (int)$r['puntiContropiede'],
            'puntiDaPallePerse'     => (int)$r['puntiDaPallePerse'],
            'pointsPerPossession'   => (float)$r['pointsPerPossession'],
            'massimoVantaggio'      => (int)$r['massimoVantaggio'],
        ];
    }

    send_json([
        'tournamentMvpSlug' => $mvp_slug,
        'players'           => $players,
        'matchMvps'         => $match_mvps,
        'teamStats'         => $team_stats,
    ]);
}

// ── GET /api/matches/{id}/stats ─────────────────────────────
// Risposta: box score completo + stats squadra per una singola partita

function handle_match_stats(PDO $pdo, int $match_id): void {
    require_method('GET');

    // Verifica esistenza partita
    $stmt = $pdo->prepare("SELECT id FROM matches WHERE id = ?");
    $stmt->execute([$match_id]);
    if (!$stmt->fetch()) {
        send_error('Partita non trovata', 404);
    }

    // Statistiche giocatori (vista pubblica)
    $stmt = $pdo->prepare("
        SELECT * FROM v_match_player_stats_public
        WHERE match_id = ?
        ORDER BY team_id, CAST(jersey_number AS UNSIGNED), last_name
    ");
    $stmt->execute([$match_id]);
    $player_rows = $stmt->fetchAll();

    // Statistiche squadra (vista pubblica)
    $stmt = $pdo->prepare("
        SELECT * FROM v_match_team_stats_public
        WHERE match_id = ?
    ");
    $stmt->execute([$match_id]);
    $team_rows = $stmt->fetchAll();

    // Verifica che ci siano statistiche OCR pubblicate
    if (empty($player_rows) && empty($team_rows)) {
        send_error('Statistiche non ancora disponibili per questa partita', 404);
    }

    // Versione statistiche (prendi la più alta disponibile)
    $stats_version = 0;
    foreach ($player_rows as $r) {
        if ((int)$r['stats_version'] > $stats_version) {
            $stats_version = (int)$r['stats_version'];
        }
    }

    // Raggruppa giocatori per squadra
    $teams_map = [];
    foreach ($player_rows as $r) {
        $tid = (int)$r['team_id'];
        if (!isset($teams_map[$tid])) {
            $teams_map[$tid] = [
                'team_id'        => $tid,
                'team_name'      => $r['team_name'],
                'team_short_name'=> $r['team_short_name'],
                'players'        => [],
            ];
        }
        $pct = fn(?int $m, ?int $a) => $a ? round($m / $a * 100, 1) : null;
        $teams_map[$tid]['players'][] = [
            'player_id'     => (int)$r['player_id'],
            'first_name'    => $r['first_name'],
            'last_name'     => $r['last_name'],
            'jersey_number' => $r['jersey_number'],
            'is_starter'    => (bool)$r['is_starter'],
            'did_not_play'  => (bool)$r['did_not_play'],
            'minutes'       => format_minutes((int)($r['minutes_seconds'] ?? 0)),
            'points'        => $r['points'] !== null ? (int)$r['points'] : null,
            'fg_made'       => $r['fg_made']  !== null ? (int)$r['fg_made']  : null,
            'fg_att'        => $r['fg_att']   !== null ? (int)$r['fg_att']   : null,
            'fg_pct'        => $r['fg_pct']   !== null ? (float)$r['fg_pct'] : null,
            'two_made'      => $r['two_made'] !== null ? (int)$r['two_made'] : null,
            'two_att'       => $r['two_att']  !== null ? (int)$r['two_att']  : null,
            'two_pct'       => $r['two_pct']  !== null ? (float)$r['two_pct']: null,
            'three_made'    => $r['three_made'] !== null ? (int)$r['three_made'] : null,
            'three_att'     => $r['three_att']  !== null ? (int)$r['three_att']  : null,
            'three_pct'     => $r['three_pct']  !== null ? (float)$r['three_pct']: null,
            'ft_made'       => $r['ft_made'] !== null ? (int)$r['ft_made'] : null,
            'ft_att'        => $r['ft_att']  !== null ? (int)$r['ft_att']  : null,
            'ft_pct'        => $r['ft_pct']  !== null ? (float)$r['ft_pct']: null,
            'reb_off'       => $r['reb_off'] !== null ? (int)$r['reb_off'] : null,
            'reb_def'       => $r['reb_def'] !== null ? (int)$r['reb_def'] : null,
            'reb_tot'       => $r['reb_tot'] !== null ? (int)$r['reb_tot'] : null,
            'assists'       => $r['assists']          !== null ? (int)$r['assists']          : null,
            'turnovers'     => $r['turnovers']        !== null ? (int)$r['turnovers']        : null,
            'steals'        => $r['steals']           !== null ? (int)$r['steals']           : null,
            'blocks'        => $r['blocks']           !== null ? (int)$r['blocks']           : null,
            'fouls_committed'=> $r['fouls_committed'] !== null ? (int)$r['fouls_committed']  : null,
            'fouls_drawn'   => $r['fouls_drawn']      !== null ? (int)$r['fouls_drawn']      : null,
            'plus_minus'    => $r['plus_minus']       !== null ? (int)$r['plus_minus']       : null,
            'evaluation'    => $r['evaluation']       !== null ? (int)$r['evaluation']       : null,
        ];
    }

    // Formatta stats squadra
    $team_stats_out = [];
    foreach ($team_rows as $r) {
        $team_stats_out[] = [
            'team_id'               => (int)$r['team_id'],
            'team_name'             => $r['team_name'],
            'points'                => $r['points']           !== null ? (int)$r['points']            : null,
            'fg_pct'                => $r['fg_pct']           !== null ? (float)$r['fg_pct']          : null,
            'two_pct'               => $r['two_pct']          !== null ? (float)$r['two_pct']         : null,
            'three_pct'             => $r['three_pct']        !== null ? (float)$r['three_pct']       : null,
            'ft_pct'                => $r['ft_pct']           !== null ? (float)$r['ft_pct']          : null,
            'reb_tot'               => $r['reb_tot']          !== null ? (int)$r['reb_tot']           : null,
            'assists'               => $r['assists']          !== null ? (int)$r['assists']           : null,
            'turnovers'             => $r['turnovers']        !== null ? (int)$r['turnovers']         : null,
            'steals'                => $r['steals']           !== null ? (int)$r['steals']            : null,
            'blocks'                => $r['blocks']           !== null ? (int)$r['blocks']            : null,
            'points_in_paint'       => $r['points_in_paint']       !== null ? (int)$r['points_in_paint']       : null,
            'fast_break_points'     => $r['fast_break_points']     !== null ? (int)$r['fast_break_points']     : null,
            'second_chance_points'  => $r['second_chance_points']  !== null ? (int)$r['second_chance_points']  : null,
            'points_off_turnovers'  => $r['points_off_turnovers']  !== null ? (int)$r['points_off_turnovers']  : null,
            'bench_points'          => $r['bench_points']          !== null ? (int)$r['bench_points']          : null,
            'biggest_lead'          => $r['biggest_lead']          !== null ? (int)$r['biggest_lead']          : null,
            'points_per_possession' => $r['points_per_possession'] !== null ? (float)$r['points_per_possession']: null,
        ];
    }

    send_json([
        'match_id'      => $match_id,
        'stats_version' => $stats_version,
        'teams'         => array_values($teams_map),
        'team_stats'    => $team_stats_out,
    ]);
}
