<?php
// ============================================================
// api/endpoints/staff.php
//
// GET /api-web/staff  →  lista membri dello staff
// ============================================================

function handle_staff(PDO $pdo): void {
    require_method('GET');

    $stmt = $pdo->query("
        SELECT id, nome, ruolo, categoria, bio, foto, ig
        FROM staff
        ORDER BY
            FIELD(categoria, 'founders', 'social', 'it', 'collaboratori'),
            id ASC
    ");
    $rows = $stmt->fetchAll();

    $result = [];
    foreach ($rows as $r) {
        $result[] = [
            'id'        => (int)$r['id'],
            'nome'      => $r['nome'],
            'ruolo'     => $r['ruolo'],
            'categoria' => $r['categoria'],
            'bio'       => $r['bio'],
            'foto'      => $r['foto'],
            'ig'        => $r['ig'],
        ];
    }

    send_json($result);
}
