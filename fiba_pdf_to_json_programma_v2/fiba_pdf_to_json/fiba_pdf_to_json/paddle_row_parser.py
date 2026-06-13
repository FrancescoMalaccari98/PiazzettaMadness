"""paddle_row_parser.py — parse player rows from a PaddleOCR player-table crop.

PaddleOCR reads the full player table as positioned tokens. Each player row has
a fixed column order, so we cluster tokens into rows by Y and parse each row by
TYPE (jersey, name, minutes, four shooting ratio+percentage groups, then the
trailing integer columns). Robust to multi-token names and N.E. rows.
"""
from __future__ import annotations

import re
from typing import Any

_RATIO_RE = re.compile(r"^\s*(\d{1,3})\s*/\s*(\d{1,3})\s*$")
_PCT_RE = re.compile(r"^\s*\d{1,3}[.,]\d\s*$")
_MIN_RE = re.compile(r"^\s*\d{1,3}:\d{2}\s*$")
_JERSEY_RE = re.compile(r"^\s*\*?\s*(\d{1,2})\s*$")
_INT_RE = re.compile(r"^\s*[-+]?\d{1,3}\s*$")
_ROW_GAP_PX = 18

_SHOOTING_GROUPS = ["fieldGoals", "twoPoints", "threePoints", "freeThrows"]
_INTEGER_COLUMNS = [
    "rebounds.offensive", "rebounds.defensive", "rebounds.total",
    "assists", "turnovers", "steals", "blocks",
    "fouls.committed", "fouls.drawn", "plusMinus", "evaluation", "points",
]
_HEADER_HINTS = {"nome", "min", "tiri", "rimbalzi", "punti", "liberi", "falli"}
_BENCH_HINTS = {"squadra", "allenatore", "totali", "total"}


def _norm(text: str) -> str:
    return re.sub(r"[^a-z0-9]+", "", (text or "").casefold())


def _cluster_rows(tokens: list[dict[str, Any]]) -> list[list[dict[str, Any]]]:
    ordered = sorted(tokens, key=lambda t: t.get("y", 0))
    if not ordered:
        return []
    rows: list[list[dict[str, Any]]] = [[ordered[0]]]
    for token in ordered[1:]:
        if token.get("y", 0) - rows[-1][-1].get("y", 0) > _ROW_GAP_PX:
            rows.append([token])
        else:
            rows[-1].append(token)
    return rows


def _is_player_row(texts: list[str]) -> bool:
    joined = {_norm(t) for t in texts}
    if joined & {_norm(h) for h in _HEADER_HINTS}:
        return False
    if any(_norm(t).startswith(h) for t in texts for h in _BENCH_HINTS):
        return False
    # A player row starts with a jersey number (optionally a starter '*').
    return bool(texts) and _JERSEY_RE.match(texts[0]) is not None


def _parse_pct(text: str) -> float:
    return float(text.strip().replace(",", "."))


def _parse_player_row(row_tokens: list[dict[str, Any]]) -> dict[str, Any] | None:
    tokens = sorted(row_tokens, key=lambda t: t.get("x", 0))
    texts = [str(t.get("text") or "").strip() for t in tokens]
    if not _is_player_row(texts):
        return None

    jersey = _JERSEY_RE.match(texts[0]).group(1)
    starter = "*" in texts[0]
    index = 1

    name_parts: list[str] = []
    while index < len(texts) and not _MIN_RE.match(texts[index]) and _norm(texts[index]) not in {"ne"}:
        name_parts.append(texts[index])
        index += 1
    name = " ".join(name_parts).strip()

    confs = [t.get("conf") for t in tokens if t.get("conf") is not None]
    avg_conf = sum(confs) / len(confs) if confs else None

    # N.E. (did not play): no stats.
    if index < len(texts) and _norm(texts[index]) == "ne":
        return {"jersey": jersey, "name": name, "starter": starter, "dnp": True, "stats": {}, "conf": avg_conf}

    stats: dict[str, Any] = {}
    if index < len(texts) and _MIN_RE.match(texts[index]):
        stats["minutes"] = texts[index].strip()
        index += 1

    for group in _SHOOTING_GROUPS:
        if index < len(texts) and (match := _RATIO_RE.match(texts[index])):
            stats[f"{group}.made"] = int(match.group(1))
            stats[f"{group}.attempted"] = int(match.group(2))
            index += 1
        if index < len(texts) and _PCT_RE.match(texts[index]):
            stats[f"{group}.percentage"] = _parse_pct(texts[index])
            index += 1

    integers = [int(t.replace("+", "")) for t in texts[index:] if _INT_RE.match(t)]
    for key, value in zip(_INTEGER_COLUMNS, integers):
        stats[key] = value

    return {"jersey": jersey, "name": name, "starter": starter, "dnp": False, "stats": stats, "conf": avg_conf}


def parse_player_table(tokens: list[dict[str, Any]]) -> list[dict[str, Any]]:
    """Return one dict per player row: {jersey, name, starter, dnp, stats, conf}."""
    players: list[dict[str, Any]] = []
    for row in _cluster_rows(tokens):
        parsed = _parse_player_row(row)
        if parsed is not None:
            players.append(parsed)
    return players
