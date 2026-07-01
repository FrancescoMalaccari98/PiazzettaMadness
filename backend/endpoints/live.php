<?php
// ============================================================
// api/endpoints/live.php
//
// GET /api/matches/{id}/live
//
// Logica a tre livelli:
//   1. Partita in corso (status Live/Paused)
//      → scoreboard_states (UNIQUE per match_id) + match_players
//      → game_clock_ms_remaining / 1000 = secondi
//   2. Partita finita (status Finished) con dati OCR
//      → match_player_stats + match_team_stats
//   3. Non ancora iniziata o nessun dato → payload vuoto
//
// Schema Sql1938817_1 — colonne reali:
//   scoreboard_states: current_period, game_clock_ms_remaining,
//     is_game_clock_running, home_score, away_score,
//     home_timeouts_used_total, away_timeouts_used_total
//   match_players: jersey_number, points, personal_fouls
// ============================================================

// ── GET /api-web/live ────────────────────────────────────────
// Trova la partita attualmente in diretta (status Live o Paused)
// e ne restituisce lo stato completo. Se nessuna partita è live,
// risponde con has_live=false. Usata dalla pagina pubblica LIVE
// che fa polling ogni 5 secondi.

function handle_live_current(PDO $pdo): void {
    require_method('GET');

    // Una sola partita può essere live alla volta; se per qualche motivo
    // ce ne fossero più di una, prende la più recente ad essere iniziata.
    $stmt = $pdo->query("
        SELECT id
        FROM matches
        WHERE status IN ('Live', 'Paused')
        ORDER BY COALESCE(actual_start_at, scheduled_start_at) DESC, id DESC
        LIMIT 1
    ");
    $row = $stmt->fetch();

    if (!$row) {
        // Nessuna partita in diretta: payload "vuoto" che la pagina
        // pubblica interpreta come "nessuna diretta in corso".
        send_json([
            'has_live' => false,
            'source'   => 'none',
            'status'   => 'none',
            'message'  => 'Nessuna partita in diretta al momento',
        ]);
        return;
    }

    // Delega alla logica esistente: trova lo stato live della partita
    // e fa send_json() (che termina la richiesta).
    handle_match_live($pdo, (int)$row['id']);
}

function handle_match_live(PDO $pdo, int $match_id): void {
    require_method('GET');

    // Info base partita + nomi squadre via match_teams
    $stmt = $pdo->prepare("
        SELECT
            m.id,
            m.status           AS match_status,
            m.period_count,
            mt_h.score         AS final_home_score,
            mt_a.score         AS final_away_score,
            mt_h.team_id       AS home_team_id,
            mt_a.team_id       AS away_team_id,
            ht.name            AS home_name,
            ht.short_name      AS home_short,
            at.name            AS away_name,
            at.short_name      AS away_short
        FROM matches m
        JOIN match_teams mt_h ON mt_h.match_id = m.id AND mt_h.side = 'Home'
        JOIN match_teams mt_a ON mt_a.match_id = m.id AND mt_a.side = 'Away'
        JOIN teams ht ON ht.id = mt_h.team_id
        JOIN teams at ON at.id = mt_a.team_id
        WHERE m.id = ?
    ");
    $stmt->execute([$match_id]);
    $match = $stmt->fetch();

    if (!$match) {
        send_error('Partita non trovata', 404);
    }

    $home_id = (int)$match['home_team_id'];
    $away_id = (int)$match['away_team_id'];

    // ── CASO 1: Partita in corso (Live / Paused) ─────────────
    if (in_array($match['match_status'], ['Live', 'Paused'], true)) {

        // scoreboard_states ha UNIQUE KEY su match_id → una riga per partita
        $stmt = $pdo->prepare(
            "SELECT * FROM scoreboard_states WHERE match_id = ?"
        );
        $stmt->execute([$match_id]);
        $live = $stmt->fetch();

        if ($live) {
            // game_clock_ms_remaining → secondi e decimi
            $clock_ms = (int)($live['game_clock_ms_remaining'] ?? 0);
            $sec      = (int)ceil($clock_ms / 1000);
            $tenths   = intdiv($clock_ms % 1000, 100);

            $players = fetch_live_players($pdo, $match_id, $home_id, $away_id);

            send_json([
                'match_id'           => $match_id,
                'source'             => 'live',
                'has_live'           => true,
                'status'             => $match['match_status'],
                'version'            => (int)($live['id'] ?? 0),
                'period'             => (int)($live['current_period'] ?? 1),
                'clock'              => format_clock($sec, $tenths),
                'clock_seconds'      => $sec,
                'clock_tenths'       => $tenths,
                'clock_running'      => (bool)($live['is_game_clock_running'] ?? false),
                'home_score'         => (int)($live['home_score'] ?? 0),
                'away_score'         => (int)($live['away_score'] ?? 0),
                'home_timeouts_used' => (int)($live['home_timeouts_used_total'] ?? 0),
                'away_timeouts_used' => (int)($live['away_timeouts_used_total'] ?? 0),
                'last_event_type'    => null,
                'home_team' => [
                    'id'        => $home_id,
                    'name'      => $match['home_name'],
                    'short_name'=> $match['home_short'],
                    'score'     => (int)($live['home_score'] ?? 0),
                    'players'   => $players['home'],
                ],
                'away_team' => [
                    'id'        => $away_id,
                    'name'      => $match['away_name'],
                    'short_name'=> $match['away_short'],
                    'score'     => (int)($live['away_score'] ?? 0),
                    'players'   => $players['away'],
                ],
            ]);
            return;
        }
    }

    // ── CASO 2: Partita finita → dati OCR da match_player_stats ──
    if ($match['match_status'] === 'Finished') {
        $ocr = fetch_ocr_live_data($pdo, $match_id, $home_id, $away_id);

        if ($ocr !== null) {
            send_json(array_merge([
                'match_id'           => $match_id,
                'source'             => 'ocr',
                'has_live'           => false,
                'status'             => 'Finished',
                'version'            => 0,
                'period'             => (int)$match['period_count'],
                'clock'              => '00:00',
                'clock_seconds'      => 0,
                'clock_tenths'       => 0,
                'clock_running'      => false,
                'home_score'         => (int)($match['final_home_score'] ?? 0),
                'away_score'         => (int)($match['final_away_score'] ?? 0),
                'home_timeouts_used' => 0,
                'away_timeouts_used' => 0,
                'last_event_type'    => 'finish',
                'home_team' => [
                    'id'        => $home_id,
                    'name'      => $match['home_name'],
                    'short_name'=> $match['home_short'],
                    'score'     => (int)($match['final_home_score'] ?? 0),
                    'players'   => $ocr['home'],
                    'team_stats'=> $ocr['home_stats'],
                ],
                'away_team' => [
                    'id'        => $away_id,
                    'name'      => $match['away_name'],
                    'short_name'=> $match['away_short'],
                    'score'     => (int)($match['final_away_score'] ?? 0),
                    'players'   => $ocr['away'],
                    'team_stats'=> $ocr['away_stats'],
                ],
            ]));
            return;
        }
    }

    // ── CASO 3: Non ancora iniziata o nessun dato ────────────
    $is_finished = ($match['match_status'] === 'Finished');
    send_json([
        'match_id'           => $match_id,
        'source'             => 'none',
        'has_live'           => false,
        'status'             => $is_finished ? 'Finished' : 'none',
        'version'            => 0,
        'period'             => 1,
        'clock'              => '10:00',
        'clock_seconds'      => 600,
        'clock_tenths'       => 0,
        'clock_running'      => false,
        'home_score'         => (int)($match['final_home_score'] ?? 0),
        'away_score'         => (int)($match['final_away_score'] ?? 0),
        'home_timeouts_used' => 0,
        'away_timeouts_used' => 0,
        'last_event_type'    => null,
        'home_team' => [
            'id'        => $home_id,
            'name'      => $match['home_name'],
            'short_name'=> $match['home_short'],
            'score'     => (int)($match['final_home_score'] ?? 0),
            'players'   => [],
            'team_stats'=> null,
        ],
        'away_team' => [
            'id'        => $away_id,
            'name'      => $match['away_name'],
            'short_name'=> $match['away_short'],
            'score'     => (int)($match['final_away_score'] ?? 0),
            'players'   => [],
            'team_stats'=> null,
        ],
    ]);
}

// ── Helper: giocatori live da match_players ───────────────────
// Colonne reali: jersey_number, points, personal_fouls

function fetch_live_players(PDO $pdo, int $match_id, int $home_id, int $away_id): array {
    $stmt = $pdo->prepare("
        SELECT
            mp.player_id,
            mp.team_id,
            mp.jersey_number,
            mp.points,
            mp.personal_fouls,
            p.first_name,
            p.last_name
        FROM match_players mp
        JOIN players p ON p.id = mp.player_id
        WHERE mp.match_id = ?
        ORDER BY mp.team_id, mp.jersey_number
    ");
    $stmt->execute([$match_id]);
    $rows = $stmt->fetchAll();

    $fmt = fn(array $p) => [
        'player_id'     => (int)$p['player_id'],
        'name'          => trim($p['first_name'] . ' ' . $p['last_name']),
        'jersey_number' => $p['jersey_number'],
        'points'        => (int)$p['points'],
        'fouls'         => (int)$p['personal_fouls'],
    ];

    return [
        'home' => array_values(array_map($fmt, array_filter($rows, fn($p) => (int)$p['team_id'] === $home_id))),
        'away' => array_values(array_map($fmt, array_filter($rows, fn($p) => (int)$p['team_id'] === $away_id))),
    ];
}

// ── Helper: dati OCR da match_player_stats + match_team_stats ─

function fetch_ocr_live_data(PDO $pdo, int $match_id, int $home_id, int $away_id): ?array {
    $stmt = $pdo->prepare("
        SELECT
            mps.player_id,
            mps.team_id,
            mps.jersey_number,
            mps.is_starter,
            mps.did_not_play,
            mps.minutes_seconds,
            mps.points,
            mps.fg_made, mps.fg_att,
            mps.two_made,   mps.two_att,
            mps.three_made, mps.three_att,
            mps.ft_made,    mps.ft_att,
            mps.reb_off,    mps.reb_def,
            COALESCE(mps.reb_off, 0) + COALESCE(mps.reb_def, 0) AS reb_tot,
            mps.assists,
            mps.turnovers,
            mps.steals,
            mps.blocks,
            mps.fouls_committed,
            mps.fouls_drawn,
            mps.plus_minus,
            mps.evaluation,
            p.first_name,
            p.last_name,
            COALESCE(tr.is_captain, 0) AS is_captain
        FROM match_player_stats mps
        JOIN players p ON p.id = mps.player_id
        LEFT JOIN team_rosters tr
            ON tr.player_id = mps.player_id AND tr.team_id = mps.team_id
        WHERE mps.match_id = ?
        ORDER BY mps.team_id,
                 mps.is_starter DESC,
                 CAST(mps.jersey_number AS UNSIGNED)
    ");
    $stmt->execute([$match_id]);
    $rows = $stmt->fetchAll();

    if (empty($rows)) return null;

    $stmt2 = $pdo->prepare("
        SELECT
            mts.team_id,
            mts.points,
            mts.fg_made, mts.fg_att,
            mts.two_made, mts.two_att,
            mts.three_made, mts.three_att,
            mts.ft_made, mts.ft_att,
            COALESCE(mts.reb_off, 0) + COALESCE(mts.reb_def, 0) AS reb_tot,
            mts.assists,
            mts.turnovers,
            mts.steals,
            mts.blocks,
            mts.points_in_paint,
            mts.fast_break_points,
            mts.second_chance_points,
            mts.points_off_turnovers,
            mts.bench_points,
            mts.biggest_lead,
            mts.points_per_possession,
            CASE WHEN mts.fg_att > 0 THEN ROUND(mts.fg_made / mts.fg_att * 100, 1) END AS fg_pct,
            CASE WHEN mts.two_att > 0 THEN ROUND(mts.two_made / mts.two_att * 100, 1) END AS two_pct,
            CASE WHEN mts.three_att > 0 THEN ROUND(mts.three_made / mts.three_att * 100, 1) END AS three_pct,
            CASE WHEN mts.ft_att > 0 THEN ROUND(mts.ft_made / mts.ft_att * 100, 1) END AS ft_pct
        FROM match_team_stats mts
        WHERE mts.match_id = ?
    ");
    $stmt2->execute([$match_id]);
    $team_stats_rows = $stmt2->fetchAll();

    $team_stats_map = [];
    foreach ($team_stats_rows as $ts) {
        $team_stats_map[(int)$ts['team_id']] = fmt_team_stats_ocr($ts);
    }

    $fmt_player = fn(array $p) => [
        'player_id'      => (int)$p['player_id'],
        'name'           => trim($p['first_name'] . ' ' . $p['last_name']),
        'jersey_number'  => $p['jersey_number'],
        'is_starter'     => (bool)$p['is_starter'],
        'did_not_play'   => (bool)$p['did_not_play'],
        'is_captain'     => (bool)$p['is_captain'],
        'minutes'        => format_minutes((int)($p['minutes_seconds'] ?? 0)),
        'points'         => (int)($p['points'] ?? 0),
        'two_made'       => (int)($p['two_made'] ?? 0),
        'two_att'        => (int)($p['two_att'] ?? 0),
        'three_made'     => (int)($p['three_made'] ?? 0),
        'three_att'      => (int)($p['three_att'] ?? 0),
        'ft_made'        => (int)($p['ft_made'] ?? 0),
        'ft_att'         => (int)($p['ft_att'] ?? 0),
        'reb_off'        => (int)($p['reb_off'] ?? 0),
        'reb_def'        => (int)($p['reb_def'] ?? 0),
        'reb_tot'        => (int)($p['reb_tot'] ?? 0),
        'assists'        => (int)($p['assists'] ?? 0),
        'turnovers'      => (int)($p['turnovers'] ?? 0),
        'steals'         => (int)($p['steals'] ?? 0),
        'blocks'         => (int)($p['blocks'] ?? 0),
        'fouls_committed'=> (int)($p['fouls_committed'] ?? 0),
        'fouls_drawn'    => (int)($p['fouls_drawn'] ?? 0),
        'plus_minus'     => (int)($p['plus_minus'] ?? 0),
        'evaluation'     => (int)($p['evaluation'] ?? 0),
    ];

    return [
        'home'       => array_values(array_map($fmt_player, array_filter($rows, fn($p) => (int)$p['team_id'] === $home_id))),
        'away'       => array_values(array_map($fmt_player, array_filter($rows, fn($p) => (int)$p['team_id'] === $away_id))),
        'home_stats' => $team_stats_map[$home_id] ?? null,
        'away_stats' => $team_stats_map[$away_id] ?? null,
    ];
}

function fmt_team_stats_ocr(array $ts): array {
    return [
        'points'                => $ts['points'] !== null ? (int)$ts['points'] : null,
        'fg_pct'                => $ts['fg_pct'] !== null ? (float)$ts['fg_pct'] : null,
        'two_pct'               => $ts['two_pct'] !== null ? (float)$ts['two_pct'] : null,
        'three_pct'             => $ts['three_pct'] !== null ? (float)$ts['three_pct'] : null,
        'ft_pct'                => $ts['ft_pct'] !== null ? (float)$ts['ft_pct'] : null,
        'reb_tot'               => $ts['reb_tot'] !== null ? (int)$ts['reb_tot'] : null,
        'assists'               => $ts['assists'] !== null ? (int)$ts['assists'] : null,
        'turnovers'             => $ts['turnovers'] !== null ? (int)$ts['turnovers'] : null,
        'steals'                => $ts['steals'] !== null ? (int)$ts['steals'] : null,
        'blocks'                => $ts['blocks'] !== null ? (int)$ts['blocks'] : null,
        'points_in_paint'       => $ts['points_in_paint'] !== null ? (int)$ts['points_in_paint'] : null,
        'fast_break_points'     => $ts['fast_break_points'] !== null ? (int)$ts['fast_break_points'] : null,
        'second_chance_points'  => $ts['second_chance_points'] !== null ? (int)$ts['second_chance_points'] : null,
        'points_off_turnovers'  => $ts['points_off_turnovers'] !== null ? (int)$ts['points_off_turnovers'] : null,
        'bench_points'          => $ts['bench_points'] !== null ? (int)$ts['bench_points'] : null,
        'biggest_lead'          => $ts['biggest_lead'] !== null ? (int)$ts['biggest_lead'] : null,
        'points_per_possession' => $ts['points_per_possession'] !== null ? (float)$ts['points_per_possession'] : null,
    ];
}

// ── Helper: formato clock ────────────────────────────────────

function format_clock(int $sec, int $tenths): string {
    if ($sec < 60) {
        return sprintf('%d.%d', $sec, $tenths);
    }
    return sprintf('%02d:%02d', intdiv($sec, 60), $sec % 60);
}
