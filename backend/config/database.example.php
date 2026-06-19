<?php
declare(strict_types=1);

// =============================================================
// TEMPLATE — copiare in config/database.php e inserire i valori reali.
// NON committare database.php (contiene segreti). NON sovrascrivere su Aruba
// il database.php già presente: questo file è solo un riferimento.
// =============================================================

// Token Bearer atteso dall'header Authorization (verificato in lib/auth.php).
define('OCR_API_TOKEN', getenv('OCR_API_TOKEN') ?: 'CHANGE_ME');

// Credenziali MySQL.
define('DB_HOST', 'localhost');
define('DB_NAME', 'CHANGE_ME');
define('DB_USER', 'CHANGE_ME');
define('DB_PASS', 'CHANGE_ME');

/**
 * Restituisce una connessione PDO MySQL condivisa.
 */
function get_pdo(): PDO
{
    static $pdo = null;
    if ($pdo === null) {
        $dsn = sprintf('mysql:host=%s;dbname=%s;charset=utf8mb4', DB_HOST, DB_NAME);
        $pdo = new PDO($dsn, DB_USER, DB_PASS, [
            PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES   => false,
        ]);
    }
    return $pdo;
}
