<?php
declare(strict_types=1);

/**
 * Converts a "MM:SS" string to total seconds.
 * Returns null if the input is empty, null, or not in the expected format.
 */
function parse_minutes(mixed $mm_ss): ?int
{
    if ($mm_ss === null || $mm_ss === '') {
        return null;
    }

    $parts = explode(':', trim((string)$mm_ss));

    if (count($parts) !== 2) {
        return null;
    }

    return (int)$parts[0] * 60 + (int)$parts[1];
}

/**
 * Casts a value to int, returning null for empty / non-numeric input.
 * Prevents writing zero instead of NULL for missing stats.
 */
function int_or_null(mixed $value): ?int
{
    if ($value === null || $value === '') {
        return null;
    }

    if (!is_numeric($value)) {
        return null;
    }

    return (int)$value;
}

/**
 * Returns a trimmed string or null for empty / null input.
 * Used for varchar columns such as biggest_run and time_in_lead.
 */
function str_or_null(mixed $value): ?string
{
    if ($value === null || $value === '') {
        return null;
    }

    return (string)$value;
}
