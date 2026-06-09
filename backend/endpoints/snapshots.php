<?php
// ============================================================
// api/endpoints/snapshots.php
//
// GET /api/snapshots/{match_id}/{kind}
//   → recupera percorso e versione JSON pubblicato su CDN/filesystem
//   → kind: 'live' | 'stats'
// ============================================================

// Tabella: snapshot_state
//
// Risposta:
// {
//   "match_id": 5,
//   "kind": "stats",
//   "version": 3,
//   "json_path": "/snapshots/match_5_stats_v3.json",
//   "updated_at": "2026-08-10T21:05:00Z"
// }
//
// Uso: il sito pubblico può usare json_path per scaricare direttamente
// il JSON statico dal CDN/hosting senza query live al DB.

function handle_snapshot(PDO $pdo, int $match_id, string $kind): void {
    require_method('GET');

    // Valida kind
    if (!in_array($kind, ['live', 'stats'], true)) {
        send_error("kind non valido: usare 'live' oppure 'stats'", 400);
    }

    // Verifica che la partita esista
    $stmt = $pdo->prepare("SELECT id FROM matches WHERE id = ?");
    $stmt->execute([$match_id]);
    if (!$stmt->fetch()) {
        send_error('Partita non trovata', 404);
    }

    // Recupera snapshot
    $stmt = $pdo->prepare("
        SELECT version, json_path, updated_at
        FROM snapshot_state
        WHERE match_id = ? AND kind = ?
    ");
    $stmt->execute([$match_id, $kind]);
    $row = $stmt->fetch();

    if (!$row) {
        send_error('Snapshot non disponibile', 404);
    }

    send_json([
        'match_id'   => $match_id,
        'kind'       => $kind,
        'version'    => (int)$row['version'],
        'json_path'  => $row['json_path'],
        'updated_at' => format_iso($row['updated_at']),
    ]);
}
