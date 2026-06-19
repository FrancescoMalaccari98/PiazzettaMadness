<?php
declare(strict_types=1);

// ============================================================
// GET /api-ocr/matches/{matchId}/context
//
// Returns the canonical match context for the OCR flow:
//   - matchId
//   - homeTeam / awayTeam (teamId, name, players)
//   - players: playerId, teamId, firstName, lastName, jerseyNumber
//
// The DB is the source of truth for identity. OCR only extracts
// statistics; it never creates teams or players.
//
// Errors:
//   404 — match not found
//   409 — roster incomplete (a team has no active players)
//   500 — database / schema error
// ============================================================

function handle_match_context(PDO $pdo, int $match_id)
{
    try {
        if (!context_match_exists($pdo, $match_id)) {
            send_error('Match not found: ' . $match_id, 404);
        }

        $sides = context_resolve_sides($pdo, $match_id);
        if ($sides === null) {
            send_error('Could not resolve home/away teams for match ' . $match_id, 500);
        }

        $home = $sides['Home'];
        $away = $sides['Away'];

        $roster = context_load_roster($pdo, $home['team_id'], $away['team_id']);

        $home_players = $roster[$home['team_id']] ?? [];
        $away_players = $roster[$away['team_id']] ?? [];

        if (count($home_players) === 0 || count($away_players) === 0) {
            send_error('Roster incomplete for match ' . $match_id, 409);
        }

        send_json([
            'matchId' => $match_id,
            'homeTeam' => [
                'teamId' => $home['team_id'],
                'name' => $home['name'],
                'players' => $home_players,
            ],
            'awayTeam' => [
                'teamId' => $away['team_id'],
                'name' => $away['name'],
                'players' => $away_players,
            ],
        ]);
    } catch (PDOException $e) {
        error_log('[api-ocr/matches/context] PDOException: ' . $e->getMessage());
        send_error('Match context database error', 500);
    } catch (Throwable $e) {
        error_log('[api-ocr/matches/context] Error: ' . $e->getMessage());
        send_error('Match context error', 500);
    }
}

function context_match_exists(PDO $pdo, int $match_id): bool
{
    $stmt = $pdo->prepare('SELECT 1 FROM matches WHERE id = ?');
    $stmt->execute([$match_id]);
    return (bool)$stmt->fetchColumn();
}

/**
 * Resolves Home/Away team_id + name. Supports both schemas:
 *   - match_teams (side Home/Away) + teams
 *   - matches.home_team_id / away_team_id + teams
 * Returns ['Home' => ['team_id','name'], 'Away' => [...]] or null.
 */
function context_resolve_sides(PDO $pdo, int $match_id)
{
    if (context_table_exists($pdo, 'match_teams')) {
        $stmt = $pdo->prepare(
            'SELECT mt.side, mt.team_id, t.name
             FROM match_teams mt
             INNER JOIN teams t ON t.id = mt.team_id
             WHERE mt.match_id = ?'
        );
        $stmt->execute([$match_id]);

        $sides = [];
        foreach ($stmt->fetchAll() as $row) {
            $side = (string)$row['side'];
            $sides[$side] = [
                'team_id' => (int)$row['team_id'],
                'name' => (string)$row['name'],
            ];
        }

        if (isset($sides['Home'], $sides['Away'])) {
            return $sides;
        }
    }

    // Fallback: home_team_id / away_team_id columns on matches.
    $match_columns = context_table_columns($pdo, 'matches');
    if (isset($match_columns['home_team_id'], $match_columns['away_team_id'])) {
        $stmt = $pdo->prepare(
            'SELECT m.home_team_id, ht.name AS home_name,
                    m.away_team_id, at.name AS away_name
             FROM matches m
             INNER JOIN teams ht ON ht.id = m.home_team_id
             INNER JOIN teams at ON at.id = m.away_team_id
             WHERE m.id = ?'
        );
        $stmt->execute([$match_id]);
        $row = $stmt->fetch();
        if ($row) {
            return [
                'Home' => ['team_id' => (int)$row['home_team_id'], 'name' => (string)$row['home_name']],
                'Away' => ['team_id' => (int)$row['away_team_id'], 'name' => (string)$row['away_name']],
            ];
        }
    }

    return null;
}

/**
 * Loads active roster players grouped by team_id.
 * Returns [team_id => [ {playerId, teamId, firstName, lastName, jerseyNumber}, ... ]].
 */
function context_load_roster(PDO $pdo, int $home_team_id, int $away_team_id): array
{
    $stmt = $pdo->prepare(
        'SELECT tr.team_id, tr.player_id, tr.jersey_number,
                p.first_name, p.last_name
         FROM team_rosters tr
         INNER JOIN players p ON p.id = tr.player_id
         WHERE tr.team_id IN (?, ?) AND tr.is_active = 1
         ORDER BY tr.team_id ASC, tr.jersey_number ASC'
    );
    $stmt->execute([$home_team_id, $away_team_id]);

    $roster = [];
    foreach ($stmt->fetchAll() as $row) {
        $team_id = (int)$row['team_id'];
        $roster[$team_id][] = [
            'playerId' => (int)$row['player_id'],
            'teamId' => $team_id,
            'firstName' => (string)$row['first_name'],
            'lastName' => (string)$row['last_name'],
            'jerseyNumber' => $row['jersey_number'] !== null ? (int)$row['jersey_number'] : 0,
        ];
    }

    return $roster;
}

function context_table_exists(PDO $pdo, string $table): bool
{
    $stmt = $pdo->prepare(
        'SELECT COUNT(*)
         FROM INFORMATION_SCHEMA.TABLES
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME = ?'
    );
    $stmt->execute([$table]);
    return (int)$stmt->fetchColumn() > 0;
}

function context_table_columns(PDO $pdo, string $table): array
{
    $stmt = $pdo->prepare(
        'SELECT COLUMN_NAME
         FROM INFORMATION_SCHEMA.COLUMNS
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME = ?'
    );
    $stmt->execute([$table]);
    $columns = [];
    foreach ($stmt->fetchAll() as $row) {
        $columns[(string)$row['COLUMN_NAME']] = true;
    }
    return $columns;
}
