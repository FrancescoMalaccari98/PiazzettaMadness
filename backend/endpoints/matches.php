<?php
// ============================================================
// api/endpoints/matches.php
//
// GET /api/partite                  → compat frontend (lista partite)
// GET /api/partite/{id}             → compat frontend (dettaglio + box score)
// GET /api/matches?edition_id={id}  → nuova API lista partite
// GET /api/matches/{id}             → nuova API dettaglio partita
<<<<<<< HEAD
//
// Schema Sql1938817_1:
//   - home/away: match_teams (side='Home'/'Away') invece di matches.home_team_id
//   - score:     match_teams.score invece di matches.final_home_score/away_score
//   - data:      matches.scheduled_start_at invece di scheduled_at
//   - round:     matches.round invece di round_label
//   - status:    PascalCase ('Finished','Cancelled','Live','Paused','Scheduled','Ready')
=======
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
// ============================================================

// ── Shared: recupera row partita con squadre ────────────────

function fetch_match_row(PDO $pdo, int $match_id): ?array {
    $sql = "
        SELECT
            m.id,
            m.edition_id,
            m.group_id,
            m.phase,
<<<<<<< HEAD
            m.round               AS round_label,
            m.scheduled_start_at  AS scheduled_at,
            m.status,
            m.started_at,
            m.finished_at,
            m.period_count,
            m.period_duration_ms,
            m.notes,
            mt_h.team_id          AS home_team_id,
            mt_h.score            AS final_home_score,
            mt_a.team_id          AS away_team_id,
            mt_a.score            AS final_away_score,
            ht.name               AS home_name,
            ht.short_name         AS home_short,
            at.name               AS away_name,
            at.short_name         AS away_short,
            tg.code               AS group_code,
            tg.name               AS group_name
        FROM matches m
        JOIN match_teams mt_h ON mt_h.match_id = m.id AND mt_h.side = 'Home'
        JOIN match_teams mt_a ON mt_a.match_id = m.id AND mt_a.side = 'Away'
        JOIN teams ht ON ht.id = mt_h.team_id
        JOIN teams at ON at.id = mt_a.team_id
=======
            m.round_label,
            m.scheduled_at,
            m.status,
            m.started_at,
            m.finished_at,
            m.final_home_score,
            m.final_away_score,
            m.period_count,
            m.period_duration_seconds,
            m.notes,
            m.home_team_id,
            m.away_team_id,
            ht.name       AS home_name,
            ht.short_name AS home_short,
            at.name       AS away_name,
            at.short_name AS away_short,
            tg.code       AS group_code,
            tg.name       AS group_name
        FROM matches m
        JOIN teams ht ON ht.id = m.home_team_id
        JOIN teams at ON at.id = m.away_team_id
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
        LEFT JOIN tournament_groups tg ON tg.id = m.group_id
        WHERE m.id = ?
    ";
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$match_id]);
    return $stmt->fetch() ?: null;
}

// Recupera i parziali per quarto di una partita.
<<<<<<< HEAD
// Fallback a [] se match_periods non esiste nello schema.
function fetch_match_periods(PDO $pdo, int $match_id): array {
    try {
        $stmt = $pdo->prepare(
            "SELECT period_number, period_type,
                    home_score_end AS home_score,
                    away_score_end AS away_score
             FROM match_periods
             WHERE match_id = ?
             ORDER BY period_number"
        );
        $stmt->execute([$match_id]);
        return $stmt->fetchAll();
    } catch (PDOException $e) {
        return [];
    }
=======
function fetch_match_periods(PDO $pdo, int $match_id): array {
    $stmt = $pdo->prepare(
        "SELECT period_number, period_type, home_score, away_score
         FROM match_periods
         WHERE match_id = ?
         ORDER BY period_number"
    );
    $stmt->execute([$match_id]);
    return $stmt->fetchAll();
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
}

// ── GET /api/partite (COMPAT) ───────────────────────────────
// Risposta: Match[] atteso dal componente Matches.tsx
// { id, round, date, status, team1:{name,score}, team2:{name,score}, details:{mvp,summary} }

function handle_partite_compat(PDO $pdo): void {
    require_method('GET');

    $eid = get_active_edition_id($pdo);
    if (!$eid) {
        send_json([]);
        return;
    }

    $sql = "
        SELECT
            m.id,
<<<<<<< HEAD
            m.round              AS round_label,
            m.scheduled_start_at AS scheduled_at,
            m.status,
            m.phase,
            mt_h.score           AS final_home_score,
            mt_a.score           AS final_away_score,
            ht.name              AS home_name,
            at.name              AS away_name,
            tg.code              AS group_code
        FROM matches m
        JOIN match_teams mt_h ON mt_h.match_id = m.id AND mt_h.side = 'Home'
        JOIN match_teams mt_a ON mt_a.match_id = m.id AND mt_a.side = 'Away'
        JOIN teams ht ON ht.id = mt_h.team_id
        JOIN teams at ON at.id = mt_a.team_id
        LEFT JOIN tournament_groups tg ON tg.id = m.group_id
        WHERE m.edition_id = ?
          AND m.status != 'Cancelled'
        ORDER BY m.scheduled_start_at, m.id
=======
            m.round_label,
            m.scheduled_at,
            m.status,
            m.phase,
            m.final_home_score,
            m.final_away_score,
            ht.name AS home_name,
            at.name AS away_name,
            tg.code AS group_code
        FROM matches m
        JOIN teams ht ON ht.id = m.home_team_id
        JOIN teams at ON at.id = m.away_team_id
        LEFT JOIN tournament_groups tg ON tg.id = m.group_id
        WHERE m.edition_id = ?
          AND m.status != 'cancelled'
        ORDER BY m.scheduled_at, m.id
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
    ";
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$eid]);
    $rows = $stmt->fetchAll();

    $result = [];
    foreach ($rows as $r) {
        $status = map_match_status($r['status']);

<<<<<<< HEAD
        // Per i playoff forza sempre il label dal phase (compatibilità filtro frontend).
        // Per i gironi usa il round del DB se contiene 'Girone', altrimenti fallback.
        $round = $r['round_label'];
        switch ($r['phase']) {
            case 'SemiFinal':       $round = 'Semifinale'; break;
            case 'ThirdPlaceFinal': $round = 'Finale 3°-4° Posto'; break;
            case 'Final':           $round = 'Finale'; break;
            case 'GroupStage':
                if (!$round || !str_contains((string)$round, 'Girone')) {
                    $round = 'Girone ' . ($r['group_code'] ?? '');
                }
                break;
            default:
                if (!$round) $round = ucfirst($r['phase'] ?? '');
=======
        // Costruisce il label round: usa round_label dal DB oppure fallback dal phase
        $round = $r['round_label'];
        if (!$round) {
            switch ($r['phase']) {
                case 'group_stage':       $round = 'Girone ' . ($r['group_code'] ?? ''); break;
                case 'semifinal':         $round = 'Semifinale'; break;
                case 'third_place_final': $round = 'Finale 3°-4° Posto'; break;
                case 'final':             $round = 'Finale'; break;
                default:                  $round = ucfirst($r['phase']);
            }
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
        }

        $isPending = ($status === 'IN PROGRAMMA');

        $result[] = [
            'id'     => (string)$r['id'],
            'round'  => $round,
            'date'   => format_match_date($r['scheduled_at']),
            'status' => $status,
            'team1'  => [
                'name'  => $r['home_name'],
                'score' => $isPending ? 0 : (int)($r['final_home_score'] ?? 0),
            ],
            'team2'  => [
                'name'  => $r['away_name'],
                'score' => $isPending ? 0 : (int)($r['final_away_score'] ?? 0),
            ],
            'details' => [
                'mvp'     => 'TBD',
                'summary' => $isPending ? 'Match in programma.' : 'Partita completata.',
            ],
        ];
    }

    send_json($result);
}

// ── GET /api/partite/{id} (COMPAT) ─────────────────────────
// Risposta: MatchDetail atteso da Matches.tsx (box score completo)

function handle_partita_detail_compat(PDO $pdo, int $match_id): void {
    require_method('GET');

    $match = fetch_match_row($pdo, $match_id);
    if (!$match) {
        send_error('Partita non trovata', 404);
    }

    // Parziali per quarto
    $periods = fetch_match_periods($pdo, $match_id);
    $quartiCasa      = [];
    $quartiTrasferta = [];
    foreach ($periods as $p) {
        $quartiCasa[]      = (int)$p['home_score'];
        $quartiTrasferta[] = (int)$p['away_score'];
    }

<<<<<<< HEAD
    $home_id = (int)$match['home_team_id'];
    $away_id = (int)$match['away_team_id'];

    // Box score giocatori (join diretto su match_player_stats, senza view)
    $sql_players = "
        SELECT
            mps.match_id,
            mps.player_id,
            mps.team_id,
            mps.jersey_number,
            mps.is_starter,
            mps.did_not_play,
            mps.minutes_seconds,
            mps.points,
            mps.fg_made,    mps.fg_att,
            mps.two_made,   mps.two_att,
            mps.three_made, mps.three_att,
            mps.ft_made,    mps.ft_att,
            mps.reb_off,    mps.reb_def,
            COALESCE(mps.reb_off,0)+COALESCE(mps.reb_def,0) AS reb_tot,
            mps.assists,    mps.turnovers,
            mps.steals,     mps.blocks,
            mps.fouls_committed, mps.fouls_drawn,
            mps.plus_minus, mps.evaluation,
            p.first_name,   p.last_name
        FROM match_player_stats mps
        JOIN players p ON p.id = mps.player_id
        WHERE mps.match_id = ?
        ORDER BY mps.team_id, CAST(mps.jersey_number AS UNSIGNED), p.last_name
=======
    // Box score giocatori (vista pubblica)
    $sql_players = "
        SELECT *
        FROM v_match_player_stats_public
        WHERE match_id = ?
        ORDER BY team_id, CAST(jersey_number AS UNSIGNED), last_name
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
    ";
    $stmt = $pdo->prepare($sql_players);
    $stmt->execute([$match_id]);
    $all_players = $stmt->fetchAll();

<<<<<<< HEAD
    // Stats di squadra (join diretto su match_team_stats, senza view)
    $sql_team = "
        SELECT
            mts.match_id,
            mts.team_id,
            mts.points,
            mts.fg_made, mts.fg_att,
            mts.two_made, mts.two_att, mts.three_made, mts.three_att,
            mts.ft_made, mts.ft_att,
            COALESCE(mts.reb_off,0)+COALESCE(mts.reb_def,0)    AS reb_tot,
            mts.assists, mts.turnovers, mts.steals, mts.blocks,
            mts.points_in_paint, mts.fast_break_points,
            mts.second_chance_points, mts.points_off_turnovers,
            mts.bench_points, mts.biggest_lead, mts.biggest_run,
            mts.lead_changes, mts.times_tied,
            mts.time_in_lead, mts.points_per_possession
        FROM match_team_stats mts
        WHERE mts.match_id = ?
    ";
=======
    // Stats di squadra (vista pubblica)
    $sql_team = "SELECT * FROM v_match_team_stats_public WHERE match_id = ?";
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
    $stmt = $pdo->prepare($sql_team);
    $stmt->execute([$match_id]);
    $all_team_stats = $stmt->fetchAll();

    // Indicizza per team_id
    $team_stats_map = [];
    foreach ($all_team_stats as $ts) {
        $team_stats_map[(int)$ts['team_id']] = $ts;
    }

    // Costruisce boxscore squadra casa / trasferta
<<<<<<< HEAD
=======
    $home_id = (int)$match['home_team_id'];
    $away_id = (int)$match['away_team_id'];

>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
    $home_players = array_values(array_filter($all_players, fn($p) => (int)$p['team_id'] === $home_id));
    $away_players = array_values(array_filter($all_players, fn($p) => (int)$p['team_id'] === $away_id));

    $home_ts = $team_stats_map[$home_id] ?? [];
    $away_ts = $team_stats_map[$away_id] ?? [];

<<<<<<< HEAD
    // Capitani da team_rosters
=======
    // Recupera i player_id dei capitani per entrambe le squadre (da team_rosters).
    // La vista v_match_player_stats_public non include is_captain, quindi serve
    // una query separata su team_rosters.
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
    $cap_stmt = $pdo->prepare(
        "SELECT player_id FROM team_rosters WHERE team_id IN (?, ?) AND is_captain = 1"
    );
    $cap_stmt->execute([$home_id, $away_id]);
    $captain_ids = array_flip(array_map('intval', array_column($cap_stmt->fetchAll(), 'player_id')));

<<<<<<< HEAD
=======
    // Funzione per formattare un giocatore nel formato MatchDetail.PlayerStats
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
    $fmt_player = function(array $p) use ($captain_ids): array {
        return [
            'numero'      => (int)($p['jersey_number'] ?? 0),
            'nome'        => trim($p['first_name'] . ' ' . $p['last_name']),
            'minuti'      => format_minutes((int)($p['minutes_seconds'] ?? 0)),
            'tdcR'        => (int)($p['fg_made'] ?? 0),
            'tdcT'        => (int)($p['fg_att']  ?? 0),
            'p2R'         => (int)($p['two_made'] ?? 0),
            'p2T'         => (int)($p['two_att']  ?? 0),
            'p3R'         => (int)($p['three_made'] ?? 0),
            'p3T'         => (int)($p['three_att']  ?? 0),
            'tlR'         => (int)($p['ft_made'] ?? 0),
            'tlT'         => (int)($p['ft_att']  ?? 0),
            'rebOff'      => (int)($p['reb_off']  ?? 0),
            'rebDef'      => (int)($p['reb_def']  ?? 0),
            'rebTot'      => (int)($p['reb_tot']  ?? 0),
            'ast'         => (int)($p['assists']   ?? 0),
            'palPerse'    => (int)($p['turnovers'] ?? 0),
            'palRecup'    => (int)($p['steals']    ?? 0),
            'stoppate'    => (int)($p['blocks']    ?? 0),
            'falliF'      => (int)($p['fouls_committed'] ?? 0),
            'falliS'      => (int)($p['fouls_drawn']     ?? 0),
            'plusMinus'   => (int)($p['plus_minus']  ?? 0),
            'valutazione' => (int)($p['evaluation']  ?? 0),
            'punti'       => (int)($p['points']      ?? 0),
            'isCapitano'  => isset($captain_ids[(int)($p['player_id'] ?? 0)]),
            'isQuintetto' => (bool)($p['is_starter'] ?? false),
        ];
    };

<<<<<<< HEAD
    $fmt_team_stats = function(array $ts): array {
        // time_in_lead è già varchar "MM:SS" nel DB (non secondi)
        return [
            'puntiDaPallePerse'     => (int)($ts['points_off_turnovers'] ?? 0),
            'puntiInArea'           => (int)($ts['points_in_paint']      ?? 0),
            'puntiInAreaR'          => 0,
=======
    // Funzione per formattare le stat squadra nel formato statsCasa/statsTrasferta
    $fmt_team_stats = function(array $ts): array {
        $lead_sec = (int)($ts['time_in_lead_seconds'] ?? 0);
        $lead_min = intdiv($lead_sec, 60);
        $lead_s   = $lead_sec % 60;
        return [
            'puntiDaPallePerse'     => (int)($ts['points_off_turnovers'] ?? 0),
            'puntiInArea'           => (int)($ts['points_in_paint']      ?? 0),
            'puntiInAreaR'          => 0, // non nel DB
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
            'puntiInAreaT'          => 0,
            'puntiDaSecondiTiri'    => (int)($ts['second_chance_points'] ?? 0),
            'puntiContropiede'      => (int)($ts['fast_break_points']    ?? 0),
            'fastBreakPoints'       => (int)($ts['fast_break_points']    ?? 0),
            'puntiPanchina'         => (int)($ts['bench_points']         ?? 0),
            'massimoVantaggio'      => (int)($ts['biggest_lead']         ?? 0),
            'massimoVantaggioScore' => $ts['biggest_run'] ?? '',
<<<<<<< HEAD
            'massimoParziale'       => 0,
            'massimoParzialePeriodo'=> '',
            'pointsPerPossession'   => (float)($ts['points_per_possession'] ?? 0),
            'tempoInVantaggio'      => $ts['time_in_lead'] ?? '00:00',
=======
            'massimoParziale'       => 0, // non nel DB
            'massimoParzialePeriodo'=> '',
            'pointsPerPossession'   => (float)($ts['points_per_possession'] ?? 0),
            'tempoInVantaggio'      => sprintf('%02d:%02d', $lead_min, $lead_s),
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
        ];
    };

    $total_home = (int)($match['final_home_score'] ?? 0);
    $total_away = (int)($match['final_away_score'] ?? 0);

<<<<<<< HEAD
    $n_gara_stmt = $pdo->prepare(
        "SELECT COUNT(*) FROM matches WHERE edition_id = ? AND id <= ? AND status = 'Finished'"
=======
    // Numero gara progressivo dell'edizione
    $n_gara_stmt = $pdo->prepare(
        "SELECT COUNT(*) FROM matches WHERE edition_id = ? AND id <= ? AND status = 'finished'"
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
    );
    $n_gara_stmt->execute([(int)$match['edition_id'], $match_id]);
    $n_gara = (int)$n_gara_stmt->fetchColumn();

<<<<<<< HEAD
    $lead_changes = (int)($home_ts['lead_changes'] ?? 0);
    $times_tied   = (int)($home_ts['times_tied']   ?? 0);
=======
    // cambiDiGuida e parita (da match_team_stats se disponibili)
    $lead_changes = 0;
    $times_tied   = 0;
    if ($home_ts) {
        $lead_changes = (int)($home_ts['lead_changes'] ?? 0);
        $times_tied   = (int)($home_ts['times_tied']   ?? 0);
    }
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97

    send_json([
        'id'               => (string)$match_id,
        'nGara'            => $n_gara,
        'spettatori'       => 0,
        'durata'           => '',
        'data'             => format_match_date($match['scheduled_at']),
        'luogo'            => '',
        'arbitri'          => [],
        'quartiCasa'       => $quartiCasa,
        'quartiTrasferta'  => $quartiTrasferta,
        'totaleCasa'       => $total_home,
        'totaleTrasferta'  => $total_away,
        'squadraCasa'      => [
            'nome'       => $match['home_name'],
            'allenatore' => null,
            'giocatori'  => array_map($fmt_player, $home_players),
        ],
        'squadraTrasferta' => [
            'nome'       => $match['away_name'],
            'allenatore' => null,
            'giocatori'  => array_map($fmt_player, $away_players),
        ],
        'statsCasa'        => $fmt_team_stats($home_ts),
        'statsTrasferta'   => $fmt_team_stats($away_ts),
        'cambiDiGuida'     => $lead_changes,
        'parita'           => $times_tied,
    ]);
}

// ── GET /api/matches (nuova API) ────────────────────────────
<<<<<<< HEAD
=======
// Risposta più completa con ISO dates e dati strutturati
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97

function handle_matches_list(PDO $pdo): void {
    require_method('GET');

    $eid = intval_positive($_GET['edition_id'] ?? null)
        ?? get_active_edition_id($pdo);

    if (!$eid) {
        send_error('edition_id non valido o nessuna edizione attiva', 400);
    }

    $sql = "
        SELECT
            m.id,
            m.phase,
<<<<<<< HEAD
            m.round              AS round_label,
            m.scheduled_start_at AS scheduled_at,
            m.status,
            mt_h.score           AS final_home_score,
            mt_a.score           AS final_away_score,
            mt_h.team_id         AS home_team_id,
            mt_a.team_id         AS away_team_id,
            ht.name              AS home_name,
            ht.short_name        AS home_short,
            at.name              AS away_name,
            at.short_name        AS away_short,
            tg.code              AS group_code,
            tg.name              AS group_name
        FROM matches m
        JOIN match_teams mt_h ON mt_h.match_id = m.id AND mt_h.side = 'Home'
        JOIN match_teams mt_a ON mt_a.match_id = m.id AND mt_a.side = 'Away'
        JOIN teams ht ON ht.id = mt_h.team_id
        JOIN teams at ON at.id = mt_a.team_id
        LEFT JOIN tournament_groups tg ON tg.id = m.group_id
        WHERE m.edition_id = ?
          AND m.status != 'Cancelled'
        ORDER BY m.scheduled_start_at, m.id
=======
            m.round_label,
            m.scheduled_at,
            m.status,
            m.final_home_score,
            m.final_away_score,
            m.home_team_id,
            m.away_team_id,
            ht.name       AS home_name,
            ht.short_name AS home_short,
            at.name       AS away_name,
            at.short_name AS away_short,
            tg.code       AS group_code,
            tg.name       AS group_name
        FROM matches m
        JOIN teams ht ON ht.id = m.home_team_id
        JOIN teams at ON at.id = m.away_team_id
        LEFT JOIN tournament_groups tg ON tg.id = m.group_id
        WHERE m.edition_id = ?
          AND m.status != 'cancelled'
        ORDER BY m.scheduled_at, m.id
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
    ";
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$eid]);
    $rows = $stmt->fetchAll();

    $result = [];
    foreach ($rows as $r) {
        $result[] = [
            'id'               => (int)$r['id'],
            'phase'            => $r['phase'],
            'round_label'      => $r['round_label'],
            'scheduled_at'     => format_iso($r['scheduled_at']),
            'status'           => $r['status'],
            'group_code'       => $r['group_code'],
            'group_name'       => $r['group_name'],
            'home_team_id'     => (int)$r['home_team_id'],
            'away_team_id'     => (int)$r['away_team_id'],
            'home_team_name'   => $r['home_name'],
            'away_team_name'   => $r['away_name'],
            'final_home_score' => $r['final_home_score'] !== null ? (int)$r['final_home_score'] : null,
            'final_away_score' => $r['final_away_score'] !== null ? (int)$r['final_away_score'] : null,
        ];
    }

    send_json($result);
}

// ── GET /api/matches/{id} (nuova API) ──────────────────────

function handle_match_detail(PDO $pdo, int $match_id): void {
    require_method('GET');

    $match = fetch_match_row($pdo, $match_id);
    if (!$match) {
        send_error('Partita non trovata', 404);
    }

    $periods = fetch_match_periods($pdo, $match_id);
    $periods_out = [];
    foreach ($periods as $p) {
        $periods_out[] = [
            'period_number' => (int)$p['period_number'],
            'period_type'   => $p['period_type'],
            'home_score'    => (int)$p['home_score'],
            'away_score'    => (int)$p['away_score'],
        ];
    }

    send_json([
        'id'               => (int)$match['id'],
        'edition_id'       => (int)$match['edition_id'],
        'phase'            => $match['phase'],
        'round_label'      => $match['round_label'],
        'group_code'       => $match['group_code'],
        'group_name'       => $match['group_name'],
        'scheduled_at'     => format_iso($match['scheduled_at']),
        'started_at'       => format_iso($match['started_at']),
        'finished_at'      => format_iso($match['finished_at']),
        'status'           => $match['status'],
        'period_count'     => (int)$match['period_count'],
        'home_team_id'     => (int)$match['home_team_id'],
        'away_team_id'     => (int)$match['away_team_id'],
        'home_team_name'   => $match['home_name'],
        'away_team_name'   => $match['away_name'],
        'final_home_score' => $match['final_home_score'] !== null ? (int)$match['final_home_score'] : null,
        'final_away_score' => $match['final_away_score'] !== null ? (int)$match['final_away_score'] : null,
        'periods'          => $periods_out,
        'notes'            => $match['notes'],
    ]);
}
