<?php
// ============================================================
// api/lib/helpers.php
// Utility: slug, formattazione date italiane, edition attiva.
// ============================================================

// ── Slug ────────────────────────────────────────────────────

// Genera uno slug URL-safe da una stringa (supporta accenti italiani).
function make_slug(string $str): string {
    $str = mb_strtolower($str, 'UTF-8');
    $from = ['à','á','â','è','é','ê','ë','ì','í','î','ï','ò','ó','ô','ù','ú','û','ü','ý','ÿ','ç','ñ'];
    $to   = ['a','a','a','e','e','e','e','i','i','i','i','o','o','o','u','u','u','u','y','y','c','n'];
    $str  = str_replace($from, $to, $str);
    $str  = preg_replace('/[^a-z0-9\s\-]/', '', $str);
    $str  = preg_replace('/[\s\-]+/', '-', trim($str));
    return $str;
}

// Genera lo slug di un giocatore dal nome + numero di maglia (opzionale).
// Esempio: ("Mario", "Rossi", "7") → "mario-rossi-7"
// Il numero di maglia evita collisioni tra omonimi nello stesso torneo.
function player_slug(string $first_name, string $last_name, ?string $jersey_number = null): string {
    $base = make_slug($first_name . ' ' . $last_name);
    if ($jersey_number !== null && $jersey_number !== '') {
        return $base . '-' . make_slug($jersey_number);
    }
    return $base;
}

// ── Date ────────────────────────────────────────────────────

// Nomi mesi abbreviati in italiano (1-indexed).
const IT_MONTHS = ['','Gen','Feb','Mar','Apr','Mag','Giu','Lug','Ago','Set','Ott','Nov','Dic'];

// Formatta una datetime SQL in "05 Ago 18:00" (per i compat endpoint).
// Restituisce '' se null.
function format_match_date(?string $datetime): string {
    if ($datetime === null || $datetime === '') return '';
    try {
        $dt = new DateTime($datetime, new DateTimeZone('UTC'));
        $day  = $dt->format('d');
        $mon  = (int)$dt->format('n');
        $time = $dt->format('H:i');
        return $day . ' ' . IT_MONTHS[$mon] . ' ' . $time;
    } catch (Exception $e) {
        return '';
    }
}

// Formatta una datetime SQL in ISO 8601 UTC.
function format_iso(?string $datetime): ?string {
    if ($datetime === null || $datetime === '') return null;
    try {
        $dt = new DateTime($datetime, new DateTimeZone('UTC'));
        return $dt->format('Y-m-d\TH:i:s\Z');
    } catch (Exception $e) {
        return null;
    }
}

// Converte secondi totali nel formato "MM:SS".
function format_minutes(?int $seconds): string {
    if ($seconds === null) return '00:00';
    $m = intdiv($seconds, 60);
    $s = $seconds % 60;
    return sprintf('%02d:%02d', $m, $s);
}

// ── Edition attiva ──────────────────────────────────────────

// Ritorna l'ID dell'edizione attiva (singleton per request).
// Se ACTIVE_EDITION_ID è definito in database.php, usa quello come override.
function get_active_edition_id(PDO $pdo): ?int {
    static $eid = false; // false = non ancora caricato, null = non trovato
    if ($eid !== false) return $eid;

    if (defined('ACTIVE_EDITION_ID') && ACTIVE_EDITION_ID > 0) {
        $eid = (int)ACTIVE_EDITION_ID;
        return $eid;
    }

    $stmt = $pdo->query("SELECT id FROM editions WHERE status = 'Active' ORDER BY year DESC LIMIT 1");
    $row  = $stmt->fetch();
    $eid  = $row ? (int)$row['id'] : null;
    return $eid;
}

// ── Mapping status partita ──────────────────────────────────

// Mappa lo status DB nel formato atteso dal frontend React.
function map_match_status(string $status): string {
    switch (strtolower($status)) {
        case 'live':    return 'LIVE';
        case 'paused':  return 'LIVE';
        case 'finished':return 'COMPLETA';
        default:        return 'IN PROGRAMMA'; // scheduled, ready, cancelled
    }
}

// ── Percentuale di tiro ──────────────────────────────────────

// Calcola la percentuale arrotondata (float o 0).
function shooting_pct(?int $made, ?int $att): float {
    if (!$att) return 0.0;
    return round($made / $att * 100, 1);
}

// ── Arrotondamento media ──────────────────────────────────────

function avg(int $total, int $games, int $decimals = 1): float {
    if ($games === 0) return 0.0;
    return round($total / $games, $decimals);
}
