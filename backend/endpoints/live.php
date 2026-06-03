<?php
// ============================================================
// api/endpoints/live.php
//
// GET /api/matches/{id}/live
//   → legge live_match_state + live_player_state
//   → stato live: periodo, cronometro, punteggio, falli, giocatori
// ============================================================

// Tabelle: live_match_state, live_player_state, matches, teams, players
//
// Risposta:
// {
//   "match_id": 5,
//   "has_live": true,
//   "status": "live",            // none | pre | live | paused | finished
//   "version": 42,
//   "period": 2,
//   "clock": "07:34",            // clock_seconds + clock_tenths formattati
//   "clock_seconds": 454,
//   "clock_tenths": 3,
//   "clock_running": true,
//   "home_score": 38,
//   "away_score": 31,
//   "home_timeouts_used": 1,
//   "away_timeouts_used": 0,
//   "last_event_type": "score",
//   "home_team": { "id": 1, "name": "...", "players": [{...}] },
//   "away_team": { "id": 2, "name": "...", "players": [{...}] }
// }

function handle_match_live(PDO $pdo, int $match_id): void {
    require_method('GET');

    // Verifica che la partita esista e recupera info squadre
    $stmt = $pdo->prepare("
        SELECT
            m.id,
            m.status        AS match_status,
            m.home_team_id,
            m.away_team_id,
            ht.name         AS home_name,
            at.name         AS away_name
        FROM matches m
        JOIN teams ht ON ht.id = m.home_team_id
        JOIN teams at ON at.id = m.away_team_id
        WHERE m.id = ?
    ");
    $stmt->execute([$match_id]);
    $match = $stmt->fetch();

    if (!$match) {
        send_error('Partita non trovata', 404);
    }

    // Stato live
    $stmt = $pdo->prepare("
        SELECT *
        FROM live_match_state
        WHERE match_id = ?
    ");
    $stmt->execute([$match_id]);
    $live = $stmt->fetch();

    if (!$live || !(int)$live['has_live']) {
        // Nessun live attivo: ritorna payload minimo
        send_json([
            'match_id'           => $match_id,
            'has_live'           => false,
            'status'             => 'none',
            'version'            => 0,
            'period'             => 1,
            'clock'              => '10:00',
            'clock_seconds'      => 600,
            'clock_tenths'       => 0,
            'clock_running'      => false,
            'home_score'         => 0,
            'away_score'         => 0,
            'home_timeouts_used' => 0,
            'away_timeouts_used' => 0,
            'last_event_type'    => null,
            'home_team'          => ['id' => (int)$match['home_team_id'], 'name' => $match['home_name'], 'players' => []],
            'away_team'          => ['id' => (int)$match['away_team_id'], 'name' => $match['away_name'], 'players' => []],
        ]);
        return;
    }

    // Giocatori live (punti e falli)
    $stmt = $pdo->prepare("
        SELECT
            lps.player_id,
            lps.team_id,
            lps.points,
            lps.fouls,
            p.first_name,
            p.last_name,
            tr.jersey_number
        FROM live_player_state lps
        JOIN players p ON p.id = lps.player_id
        LEFT JOIN team_rosters tr ON tr.player_id = lps.player_id AND tr.team_id = lps.team_id
        WHERE lps.match_id = ?
        ORDER BY lps.team_id, CAST(tr.jersey_number AS UNSIGNED)
    ");
    $stmt->execute([$match_id]);
    $all_players = $stmt->fetchAll();

    $home_id = (int)$match['home_team_id'];
    $away_id = (int)$match['away_team_id'];

    $fmt_player = function(array $p): array {
        return [
            'player_id'     => (int)$p['player_id'],
            'name'          => trim($p['first_name'] . ' ' . $p['last_name']),
            'jersey_number' => $p['jersey_number'],
            'points'        => (int)$p['points'],
            'fouls'         => (int)$p['fouls'],
        ];
    };

    $home_players = array_values(array_map($fmt_player,
        array_filter($all_players, fn($p) => (int)$p['team_id'] === $home_id)
    ));
    $away_players = array_values(array_map($fmt_player,
        array_filter($all_players, fn($p) => (int)$p['team_id'] === $away_id)
    ));

    // Calcola il clock leggibile
    $sec    = (int)$live['clock_seconds'];
    $tenths = (int)$live['clock_tenths'];
    $clock_display = sprintf('%02d:%02d', intdiv($sec, 60), $sec % 60);
    if ($sec < 60) {
        // Sotto il minuto: mostra decimi
        $clock_display = sprintf('%d.%d', $sec, $tenths);
    }

    send_json([
        'match_id'           => $match_id,
        'has_live'           => (bool)$live['has_live'],
        'status'             => $live['status'],
        'version'            => (int)$live['version'],
        'period'             => (int)$live['period'],
        'clock'              => $clock_display,
        'clock_seconds'      => $sec,
        'clock_tenths'       => $tenths,
        'clock_running'      => (bool)$live['clock_running'],
        'home_score'         => (int)$live['home_score'],
        'away_score'         => (int)$live['away_score'],
        'home_timeouts_used' => (int)$live['home_timeouts_used'],
        'away_timeouts_used' => (int)$live['away_timeouts_used'],
        'last_event_type'    => $live['last_event_type'],
        'home_team' => [
            'id'      => $home_id,
            'name'    => $match['home_name'],
            'players' => $home_players,
        ],
        'away_team' => [
            'id'      => $away_id,
            'name'    => $match['away_name'],
            'players' => $away_players,
        ],
    ]);
}
