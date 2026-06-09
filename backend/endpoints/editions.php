<?php
// ============================================================
// api/endpoints/editions.php
//
// GET /api/editions/active
//   → edizione del torneo attualmente attiva
// ============================================================

// Tabelle: editions JOIN tournaments
// Risposta:
// {
//   "id": 1,
//   "name": "Piazzetta Madness 2026",
//   "year": 2026,
//   "status": "active",
//   "start_date": "2026-08-05",
//   "end_date": "2026-08-14",
//   "tournament": { "id": 1, "name": "Piazzetta Madness" }
// }

function handle_editions_active(PDO $pdo): void {
    require_method('GET');

    $sql = "
        SELECT
            e.id,
            e.name,
            e.year,
            e.status,
            e.start_date,
            e.end_date,
            t.id   AS tournament_id,
            t.name AS tournament_name
        FROM editions e
        JOIN tournaments t ON t.id = e.tournament_id
<<<<<<< HEAD
        WHERE e.status = 'Active'
=======
        WHERE e.status = 'active'
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
        ORDER BY e.year DESC
        LIMIT 1
    ";

    $row = $pdo->query($sql)->fetch();

    if (!$row) {
        send_error('Nessuna edizione attiva trovata', 404);
    }

    send_json([
        'id'         => (int)$row['id'],
        'name'       => $row['name'],
        'year'       => (int)$row['year'],
        'status'     => $row['status'],
        'start_date' => $row['start_date'],
        'end_date'   => $row['end_date'],
        'tournament' => [
            'id'   => (int)$row['tournament_id'],
            'name' => $row['tournament_name'],
        ],
    ]);
}
