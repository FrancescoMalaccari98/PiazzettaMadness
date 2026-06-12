<?php
// ============================================================
// api/index.php  —  Entry point / Router principale
//
// Deployare questo file (e tutta la dir api/) in public_html/api/
// su hosting Aruba. Il file .htaccess nella stessa dir gestisce
// il rewriting: tutte le richieste → index.php.
//
// URL structure (da frontend):
//   VITE_API_URL/api/partite          → /api/index.php → route "partite"
//   VITE_API_URL/api/matches/5/live   → route "matches/{5}/live"
// ============================================================

declare(strict_types=1);

// ── Bootstrap ───────────────────────────────────────────────

require_once __DIR__ . '/config/database.php';
require_once __DIR__ . '/config/cors.php';
require_once __DIR__ . '/lib/response.php';
require_once __DIR__ . '/lib/auth.php';
require_once __DIR__ . '/lib/helpers.php';

// Imposta header CORS + Content-Type su ogni risposta
set_cors_headers();

// ── Parsing URL ─────────────────────────────────────────────

$uri = parse_url($_SERVER['REQUEST_URI'] ?? '/', PHP_URL_PATH);
$uri = rawurldecode($uri);

// Rimuovi il prefisso /api-web (il file è in /api-web/, il browser chiede /api-web/partite)
$path = preg_replace('#^/api-web/?#', '', $uri);
$path = trim($path, '/');

// Separa in segmenti: "matches/5/live" → ['matches','5','live']
$parts = $path !== '' ? explode('/', $path) : [];
$seg0  = $parts[0] ?? '';
$seg1  = $parts[1] ?? '';
$seg2  = $parts[2] ?? '';

$method = $_SERVER['REQUEST_METHOD'];

// ── Connessione DB (lazy: solo se necessario) ────────────────
// get_pdo() è singleton: connette solo alla prima chiamata.

// ── Routing ─────────────────────────────────────────────────

try {

    // ── /editions ───────────────────────────────────────────
    if ($seg0 === 'editions' && $seg1 === 'active') {
        require_once __DIR__ . '/endpoints/editions.php';
        handle_editions_active(get_pdo());

    // ── /squadre  (compat) ──────────────────────────────────
    } elseif ($seg0 === 'squadre' && $seg1 === '') {
        require_once __DIR__ . '/endpoints/teams.php';
        handle_squadre_compat(get_pdo());

    // ── /teams ──────────────────────────────────────────────
    } elseif ($seg0 === 'teams') {
        require_once __DIR__ . '/endpoints/teams.php';

        $team_id = intval_positive($seg1 ?: null);

        if ($team_id && $seg2 === 'roster') {
            // GET /api/teams/{id}/roster
            handle_team_roster(get_pdo(), $team_id);

        } elseif ($team_id && $seg2 === 'edition-stats') {
            // GET /api/teams/{id}/edition-stats
            handle_team_edition_stats(get_pdo(), $team_id);

        } elseif (!$seg1) {
            // GET /api/teams
            handle_teams_list(get_pdo());

        } else {
            send_error('Endpoint teams non trovato', 404);
        }

    // ── /partite  (compat) ──────────────────────────────────
    } elseif ($seg0 === 'partite') {
        require_once __DIR__ . '/endpoints/matches.php';

        $match_id = intval_positive($seg1 ?: null);

        if ($match_id) {
            // GET /api/partite/{id}
            handle_partita_detail_compat(get_pdo(), $match_id);
        } else {
            // GET /api/partite
            handle_partite_compat(get_pdo());
        }

    // ── /matches ────────────────────────────────────────────
    } elseif ($seg0 === 'matches') {
        $match_id = intval_positive($seg1 ?: null);

        if ($match_id && $seg2 === 'live') {
            // GET /api/matches/{id}/live
            require_once __DIR__ . '/endpoints/live.php';
            handle_match_live(get_pdo(), $match_id);

        } elseif ($match_id && $seg2 === 'stats') {
            // GET /api/matches/{id}/stats
            // stats.php include già players.php tramite require_once interno
            require_once __DIR__ . '/endpoints/stats.php';
            handle_match_stats(get_pdo(), $match_id);

        } elseif ($match_id && !$seg2) {
            // GET /api/matches/{id}
            require_once __DIR__ . '/endpoints/matches.php';
            handle_match_detail(get_pdo(), $match_id);

        } elseif (!$seg1) {
            // GET /api/matches
            require_once __DIR__ . '/endpoints/matches.php';
            handle_matches_list(get_pdo());

        } else {
            send_error('Endpoint matches non trovato', 404);
        }

    // ── /statistiche  (compat) ──────────────────────────────
    } elseif ($seg0 === 'statistiche') {
        // stats.php include già players.php al suo interno
        require_once __DIR__ . '/endpoints/stats.php';
        handle_statistiche_compat(get_pdo());

    // ── /giocatori  (compat) ────────────────────────────────
    } elseif ($seg0 === 'giocatori') {
        require_once __DIR__ . '/endpoints/players.php';

        $slug = $seg1 !== '' ? $seg1 : null;

        if ($slug) {
            // GET /api/giocatori/{slug}
            handle_giocatore_detail(get_pdo(), $slug);
        } else {
            // GET /api/giocatori
            handle_giocatori_list(get_pdo());
        }

    // ── /players ────────────────────────────────────────────
    } elseif ($seg0 === 'players') {
        require_once __DIR__ . '/endpoints/players.php';

        $player_id = intval_positive($seg1 ?: null);

        if ($player_id && $seg2 === 'edition-stats') {
            // GET /api/players/{id}/edition-stats
            handle_player_edition_stats(get_pdo(), $player_id);
        } else {
            send_error('Endpoint players non trovato', 404);
        }

    // ── /standings ──────────────────────────────────────────
    } elseif ($seg0 === 'standings') {
        require_once __DIR__ . '/endpoints/standings.php';
        handle_standings(get_pdo());

    // ── /snapshots ──────────────────────────────────────────
    } elseif ($seg0 === 'snapshots') {
        require_once __DIR__ . '/endpoints/snapshots.php';

        $snap_match_id = intval_positive($seg1 ?: null);
        $snap_kind     = $seg2 !== '' ? $seg2 : null;

        if ($snap_match_id && $snap_kind) {
            handle_snapshot(get_pdo(), $snap_match_id, $snap_kind);
        } else {
            send_error('Parametri snapshot non validi: /api/snapshots/{match_id}/{kind}', 400);
        }

    // ── /staff ──────────────────────────────────────────────
    } elseif ($seg0 === 'staff') {
        require_once __DIR__ . '/endpoints/staff.php';
        handle_staff(get_pdo());

    // ── / (health check) ────────────────────────────────────
    } elseif ($seg0 === '' || $seg0 === 'health') {
        send_json([
            'status'  => 'ok',
            'service' => 'PiazzettaMadness API',
            'version' => '1.0.0',
        ]);

    // ── 404 ─────────────────────────────────────────────────
    } else {
        send_error('Endpoint non trovato: ' . htmlspecialchars($path), 404);
    }

} catch (PDOException $e) {
    error_log('[PiazzettaMadness] PDOException: ' . $e->getMessage());
    send_error('Errore database interno', 500);
} catch (Throwable $e) {
    error_log('[PiazzettaMadness] Errore: ' . $e->getMessage());
    send_error('Errore interno del server', 500);
}
