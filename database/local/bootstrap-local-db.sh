#!/usr/bin/env bash
set -euo pipefail

MYSQL_BIN="${MYSQL_BIN:-}"
if [[ -z "$MYSQL_BIN" ]]; then
  if command -v mysql >/dev/null 2>&1; then
    MYSQL_BIN="$(command -v mysql)"
  elif [[ -x /usr/local/mysql/bin/mysql ]]; then
    MYSQL_BIN="/usr/local/mysql/bin/mysql"
  elif [[ -x /opt/homebrew/bin/mysql ]]; then
    MYSQL_BIN="/opt/homebrew/bin/mysql"
  elif [[ -x /usr/local/bin/mysql ]]; then
    MYSQL_BIN="/usr/local/bin/mysql"
  fi
fi

if [[ -z "$MYSQL_BIN" || ! -x "$MYSQL_BIN" ]]; then
  echo "Errore: client mysql non trovato nel PATH." >&2
  echo "Installa MySQL/MariaDB locale oppure passa MYSQL_BIN=/path/mysql." >&2
  exit 1
fi

DUMP_PATH="${1:-}"
if [[ -z "$DUMP_PATH" || ! -f "$DUMP_PATH" ]]; then
  echo "Uso: bash database/local/bootstrap-local-db.sh /path/al/Sql1938817_1.sql" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DB_HOST="${PM_DB_HOST:-127.0.0.1}"
DB_PORT="${PM_DB_PORT:-3306}"
DB_NAME="${PM_DB_NAME:-piazzetta_madness_local}"
DB_USER="${PM_DB_USER:-root}"
DB_PASS="${PM_DB_PASS:-}"

MYSQL_ARGS=(-h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" --default-character-set=utf8mb4)
if [[ -n "$DB_PASS" ]]; then
  MYSQL_ARGS+=("-p$DB_PASS")
fi

echo "Creo database locale '$DB_NAME'..."
"$MYSQL_BIN" "${MYSQL_ARGS[@]}" -e "DROP DATABASE IF EXISTS \`$DB_NAME\`; CREATE DATABASE \`$DB_NAME\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

echo "Importo export Aruba..."
"$MYSQL_BIN" "${MYSQL_ARGS[@]}" "$DB_NAME" < "$DUMP_PATH"

echo "Genero fixture locale 2025/2026 concluse..."
"$MYSQL_BIN" "${MYSQL_ARGS[@]}" "$DB_NAME" < "$SCRIPT_DIR/seed-local-editions.sql"

echo "Database locale pronto: $DB_NAME"
