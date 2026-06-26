<?php
// ============================================================
// api/endpoints/sponsors.php
//
// GET /api-web/sponsor → lista sponsor con foto, nome, sito, instagram
// ============================================================

function handle_sponsors(PDO $pdo): void {
    require_method('GET');

    $stmt = $pdo->query("
        SELECT id, filename, nome, sito, instagram
        FROM sponsor_links
        ORDER BY id ASC
    ");
    $rows = $stmt->fetchAll();

    $result = [];
    foreach ($rows as $r) {
        if (!$r['filename']) continue;
        $result[] = [
            'id'   => (int)$r['id'],
            'nome' => $r['nome'] ?? '',
            'logo' => '/img/sponsor/' . $r['filename'],
            'sito' => $r['sito'] ?? '',
            'ig'   => $r['instagram'] ?? '',
        ];
    }

    send_json($result);
}
