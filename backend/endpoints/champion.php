<?php
// ============================================================
// api/endpoints/champion.php
//
// GET /api-web/campione
//   → Restituisce il campione dell'edizione attiva (vincitore della Finale).
//     Se la Finale non è ancora stata giocata risponde con has_champion=false.
//
// Risposta:
// {
//   "has_champion": true,
//   "team": { "id", "name", "short_name", "primary_color", "secondary_color" },
//   "final": { "match_id", "date", "home_team", "home_score", "away_team", "away_score" }
// }
// oppure:
// { "has_champion": false, "team": null, "final": null }
// ============================================================

function handle_champion(PDO $pdo): void {
    require_method('GET');

    $eid = get_active_edition_id($pdo);
    if (!$eid) {
        send_json(['has_champion' => false, 'team' => null, 'final' => null]);
        return;
    }

    // Recupera la partita con phase=Final dell'edizione attiva che ha un vincitore
    $sql = "
        SELECT
            m.id              AS match_id,
            m.scheduled_start_at,
            m.winner_team_id,
            wt.name           AS winner_name,
            wt.short_name     AS winner_short,
            wt.primary_color  AS winner_color,
            wt.secondary_color AS winner_color2,
            ht.name           AS home_name,
            mt_h.score        AS home_score,
            at.name           AS away_name,
            mt_a.score        AS away_score
        FROM matches m
        LEFT JOIN match_teams mt_h ON mt_h.match_id = m.id AND mt_h.side = 'Home'
        LEFT JOIN match_teams mt_a ON mt_a.match_id = m.id AND mt_a.side = 'Away'
        LEFT JOIN teams ht ON ht.id = mt_h.team_id
        LEFT JOIN teams at ON at.id = mt_a.team_id
        LEFT JOIN teams wt ON wt.id = m.winner_team_id
        WHERE m.edition_id = ?
          AND m.phase = 'Final'
        LIMIT 1
    ";
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$eid]);
    $row = $stmt->fetch();

    // Finale non ancora inserita nel DB oppure non ancora giocata
    if (!$row || !$row['winner_team_id']) {
        send_json(['has_champion' => false, 'team' => null, 'final' => null]);
        return;
    }

    send_json([
        'has_champion' => true,
        'team' => [
            'id'              => (int)$row['winner_team_id'],
            'name'            => $row['winner_name'],
            'short_name'      => $row['winner_short'],
            'primary_color'   => $row['winner_color'],
            'secondary_color' => $row['winner_color2'],
        ],
        'final' => [
            'match_id'   => (int)$row['match_id'],
            'date'       => format_match_date($row['scheduled_start_at']),
            'home_team'  => $row['home_name'],
            'home_score' => (int)($row['home_score'] ?? 0),
            'away_team'  => $row['away_name'],
            'away_score' => (int)($row['away_score'] ?? 0),
        ],
    ]);
}
