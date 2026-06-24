<?php
// ============================================================
// api/endpoints/sponsors.php
//
// GET /api-web/sponsor-links → link sito e instagram per sponsor
// ============================================================

function handle_sponsor_links(PDO $pdo): void {
    require_method('GET');

    $stmt = $pdo->query("
        SELECT filename, nome, sito, instagram
        FROM sponsor_links
        ORDER BY id ASC
    ");
    $rows = $stmt->fetchAll();

    $result = [];
    foreach ($rows as $r) {
        $result[$r['filename']] = [
            'nome' => $r['nome'] ?? '',
            'sito' => $r['sito'] ?? '',
            'ig'   => $r['instagram'] ?? '',
        ];
    }

    send_json($result);
}
