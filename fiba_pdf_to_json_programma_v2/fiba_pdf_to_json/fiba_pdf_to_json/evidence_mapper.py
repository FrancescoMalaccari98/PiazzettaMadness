"""evidence_mapper.py — map TesseractFullPage raw JSON to evidence records.

Reads the Italian-schema JSON produced by FibaPdfConverter and emits a
flat list of evidence records (one per stat field) in the shared schema.
"""
from __future__ import annotations

import re
from typing import Any

from .evidence_record import (
    GRANULARITY_FULLPAGE,
    GRANULARITY_ROW,
    SOURCE_TESSERACT_FULLPAGE,
    SOURCE_TESSERACT_CROP,
    SOURCE_PADDLE_ROW,
    empty_record,
    make_envelope,
)
from .paddle_row_parser import parse_player_table

_MAPPER_ID_FULLPAGE = "fiba_pdf_to_json.evidence_mapper.fullpage"
_MAPPER_ID_CROP = "fiba_pdf_to_json.evidence_mapper.crop"

# Canonical stat key → Italian path(s) in raw JSON
_PLAYER_STAT_PATHS: list[tuple[str, list[str]]] = [
    ("minutes",                ["minuti", "raw"]),
    ("fieldGoals.made",        ["tiri_dal_campo", "realizzati"]),
    ("fieldGoals.attempted",   ["tiri_dal_campo", "tentati"]),
    ("fieldGoals.percentage",  ["tiri_dal_campo", "percentuale"]),
    ("twoPoints.made",         ["tiri_da_2", "realizzati"]),
    ("twoPoints.attempted",    ["tiri_da_2", "tentati"]),
    ("twoPoints.percentage",   ["tiri_da_2", "percentuale"]),
    ("threePoints.made",       ["tiri_da_3", "realizzati"]),
    ("threePoints.attempted",  ["tiri_da_3", "tentati"]),
    ("threePoints.percentage", ["tiri_da_3", "percentuale"]),
    ("freeThrows.made",        ["tiri_liberi", "realizzati"]),
    ("freeThrows.attempted",   ["tiri_liberi", "tentati"]),
    ("freeThrows.percentage",  ["tiri_liberi", "percentuale"]),
    ("rebounds.offensive",     ["rimbalzi", "offensivi"]),
    ("rebounds.defensive",     ["rimbalzi", "difensivi"]),
    ("rebounds.total",         ["rimbalzi", "totali"]),
    ("assists",                ["assist"]),
    ("turnovers",              ["palle_perse"]),
    ("steals",                 ["palle_recuperate"]),
    ("blocks",                 ["stoppate_date"]),
    ("fouls.committed",        ["falli", "commessi"]),
    ("fouls.drawn",            ["falli", "subiti"]),
    ("plusMinus",              ["plus_minus"]),
    ("evaluation",             ["valutazione"]),
    ("points",                 ["punti"]),
]


def _get_nested(obj: Any, path: list[str]) -> Any:
    for key in path:
        if not isinstance(obj, dict):
            return None
        obj = obj.get(key)
    return obj


def _confidence_from_reliability(level: str | None) -> float:
    return {"alta": 0.90, "media": 0.65, "bassa": 0.35}.get(str(level or "").lower(), 0.60)


def _side_code(tipo: str | None, idx: int) -> str:
    if tipo in {"casa", "home"}:
        return "Home"
    if tipo in {"ospite", "away"}:
        return "Away"
    return "Home" if idx == 0 else "Away"


def _player_entity_id(side: str, player: dict[str, Any]) -> str:
    jersey = str(player.get("numero") or "").lstrip("*")
    if jersey:
        return f"player:{side}:jersey:{jersey}"
    name = str(player.get("nome_completo") or "")
    if name:
        slug = "".join(c if c.isalnum() else "-" for c in name.lower())
        return f"player:{side}:name:{slug}"
    return f"player:{side}:row:unknown"


def map_fullpage_to_evidence(
    raw: dict[str, Any],
    *,
    document_hash: str = "",
    render_width: int | None = None,
    render_height: int | None = None,
    dpi: int | None = None,
) -> dict[str, Any]:
    """Convert TesseractFullPage raw JSON → evidence envelope."""
    reliability = _get_nested(raw, ["affidabilita", "livello_globale"])
    confidence = _confidence_from_reliability(reliability)
    records: list[dict[str, Any]] = []

    # Per-player stats
    for team_idx, team in enumerate(raw.get("squadre") or []):
        side = _side_code(team.get("tipo"), team_idx)
        team_abbr = str(team.get("abbreviazione") or side)

        for player in team.get("giocatori") or []:
            entity_id = _player_entity_id(side, player)
            ne = bool(player.get("non_entrato"))
            for stat_key, path in _PLAYER_STAT_PATHS:
                raw_val = _get_nested(player, path)
                if raw_val is None:
                    continue
                field_id = f"{entity_id}:{stat_key}"
                records.append(empty_record(
                    field_id=field_id,
                    entity_id=entity_id,
                    entity_type="player",
                    side=side,
                    stat_key=stat_key,
                    scope="Player",
                    crop_id="",
                    zone_id="",
                    value_raw=str(raw_val),
                    value_normalized=raw_val,
                    confidence_raw=0.0 if ne else confidence,
                    confidence_normalized=0.0 if ne else confidence,
                    mapper_id=_MAPPER_ID_FULLPAGE,
                    warnings=[],
                ))

        # Team totals
        totals = team.get("totali_squadra") or {}
        team_entity = f"team:{side}"
        for stat_key, path in _PLAYER_STAT_PATHS:
            raw_val = _get_nested(totals, path)
            if raw_val is None:
                continue
            field_id = f"{team_entity}:{stat_key}"
            records.append(empty_record(
                field_id=field_id,
                entity_id=team_entity,
                entity_type="team",
                side=side,
                stat_key=stat_key,
                scope="Team",
                crop_id="",
                zone_id="",
                value_raw=str(raw_val),
                value_normalized=raw_val,
                confidence_raw=confidence,
                confidence_normalized=confidence,
                mapper_id=_MAPPER_ID_FULLPAGE,
            ))

    # Final score
    risultato = raw.get("risultato") or {}
    pf = risultato.get("punteggio_finale")
    if pf:
        game_id = f"game:{document_hash[:12]}"
        records.append(empty_record(
            field_id="game.finalScore",
            entity_id=game_id,
            entity_type="game",
            stat_key="finalScore",
            scope="Game",
            value_raw=str(pf),
            value_normalized=pf,
            confidence_raw=confidence,
            confidence_normalized=confidence,
            mapper_id=_MAPPER_ID_FULLPAGE,
        ))

    return make_envelope(
        source_id=SOURCE_TESSERACT_FULLPAGE,
        engine="tesseract",
        granularity=GRANULARITY_FULLPAGE,
        document_file_name=str(raw.get("metadata", {}).get("nome_file") or ""),
        document_hash=document_hash,
        dpi=dpi,
        render_width=render_width,
        render_height=render_height,
        records=records,
    )


# Final-score separators include OCR mojibake for the dash (e.g. U+FFFD).
_SCORE_RE = re.compile(r"(?<!\d)(\d{1,3})\s*[-‒–—−�]\s*(\d{1,3})(?!\d)")
_RATIO_RE = re.compile(r"(\d{1,3})\s*/\s*(\d{1,3})")
# The team-totals row lists shooting ratios in this fixed order.
_TOTALS_RATIO_FIELDS = ["fieldGoals", "twoPoints", "threePoints", "freeThrows"]


def _parse_totals_ratios(tokens: list[dict[str, Any]]) -> list[tuple[int, int]]:
    """Extract made/attempted ratios (FG, 2P, 3P, FT) from X-ordered tokens.

    Only clean ``N/N`` tokens are considered, so OCR noise and separators are
    ignored. The first four ratios map to FG, 2P, 3P, FT by position.
    """
    # Sort left-to-right: the totals row lists FG, 2P, 3P, FT in X order, but the
    # OCR engine may return tokens in detection order, not positional order.
    ordered = sorted(tokens, key=lambda token: token.get("x", 0))
    ratios: list[tuple[int, int]] = []
    for token in ordered:
        match = _RATIO_RE.search(str(token.get("text") or ""))
        if match:
            ratios.append((int(match.group(1)), int(match.group(2))))
        if len(ratios) >= len(_TOTALS_RATIO_FIELDS):
            break
    return ratios


def _parse_final_score(raw_text: str) -> str | None:
    """Extract the final score (first NN-NN not inside parentheses).

    Parenthesized partials such as "(19-19, 12-10)" are skipped so period
    scores are never mistaken for the final score.
    """
    for match in _SCORE_RE.finditer(raw_text):
        before = raw_text[: match.start()]
        if before.count("(") > before.count(")"):
            continue  # inside parentheses -> a partial, not the final score
        return f"{int(match.group(1))}-{int(match.group(2))}"
    return None


def _team_totals_records(zone_id: str, tokens: list[dict[str, Any]], conf: float | None) -> list[dict[str, Any]]:
    """Emit made/attempted evidence for the four shooting ratios of a totals row.

    These are exactly the cells the full page tends to misread; the isolated
    totals crop reads them cleanly. Confidence is reduced slightly so the
    reconciler treats them as supporting (not overriding) evidence.
    """
    side = "Home" if zone_id.startswith("home") else "Away"
    ratios = _parse_totals_ratios(tokens)
    # Positional parsing is only safe when all four ratios are present AND
    # internally consistent (FG = 2P + 3P). A skipped/misread ratio shifts the
    # alignment and would assign values to the wrong fields, so reject it: the
    # crop contributes evidence only when it is self-consistent, else stays silent.
    if len(ratios) < len(_TOTALS_RATIO_FIELDS):
        return []
    (fg_m, fg_a), (p2_m, p2_a), (p3_m, p3_a), _ft = ratios
    if fg_m != p2_m + p3_m or fg_a != p2_a + p3_a:
        return []

    team_conf = round(conf * 0.9, 4) if conf is not None else None
    records: list[dict[str, Any]] = []
    for field, (made, attempted) in zip(_TOTALS_RATIO_FIELDS, ratios):
        for sub, value in (("made", made), ("attempted", attempted)):
            records.append(empty_record(
                field_id=f"team:{side}:{field}.{sub}",
                entity_id=f"team:{side}",
                entity_type="team",
                side=side,
                stat_key=f"{field}.{sub}",
                scope="Team",
                crop_id=zone_id,
                zone_id=zone_id,
                value_raw=f"{made}/{attempted}",
                value_normalized=value,
                confidence_raw=team_conf,
                confidence_normalized=team_conf,
                mapper_id=_MAPPER_ID_CROP,
            ))
    return records


_PADDLE_ROW_MAPPER = "fiba_pdf_to_json.paddle_row_parser"


def _player_entity_id_from_jersey(side: str, jersey: str) -> str:
    return f"player:{side}:jersey:{jersey}" if jersey else f"player:{side}:row:unknown"


def map_paddle_rows_to_evidence(
    raw: dict[str, Any],
    *,
    document_hash: str = "",
    render_width: int | None = None,
    render_height: int | None = None,
    dpi: int | None = None,
) -> dict[str, Any]:
    """Convert a Paddle player-table raw JSON → per-player-row evidence.

    Parses each player row of the home/away player-table crops into one evidence
    record per stat (sourceId = ocr.paddle.row, granularity = row). DNP players
    contribute no stat records.
    """
    records: list[dict[str, Any]] = []
    for zone in raw.get("zones") or []:
        zone_id = str(zone.get("zoneId") or "")
        if zone_id not in ("home.playerTable", "away.playerTable"):
            continue
        side = "Home" if zone_id.startswith("home") else "Away"
        for player in parse_player_table(zone.get("tokens") or []):
            if player.get("dnp"):
                continue
            entity_id = _player_entity_id_from_jersey(side, str(player.get("jersey") or ""))
            conf = player.get("conf")
            for stat_key, value in (player.get("stats") or {}).items():
                records.append(empty_record(
                    field_id=f"{entity_id}:{stat_key}",
                    entity_id=entity_id,
                    entity_type="player",
                    side=side,
                    stat_key=stat_key,
                    scope="Player",
                    crop_id=zone_id,
                    zone_id=zone_id,
                    value_raw=str(value),
                    value_normalized=value,
                    confidence_raw=conf,
                    confidence_normalized=conf,
                    mapper_id=_PADDLE_ROW_MAPPER,
                ))

    return make_envelope(
        source_id=SOURCE_PADDLE_ROW,
        engine="paddleocr",
        granularity=GRANULARITY_ROW,
        document_file_name=str(raw.get("originalFileName") or ""),
        document_hash=document_hash,
        dpi=dpi,
        render_width=render_width,
        render_height=render_height,
        records=records,
    )


def map_crop_ocr_to_evidence(
    raw: dict[str, Any],
    *,
    document_hash: str = "",
    render_width: int | None = None,
    render_height: int | None = None,
    dpi: int | None = None,
    source_id: str = SOURCE_TESSERACT_CROP,
    engine: str = "tesseract",
) -> dict[str, Any]:
    """Convert a crop-OCR raw JSON → evidence envelope.

    Shared by the Tesseract-crop and PaddleOCR-crop channels: the only
    difference is ``source_id``/``engine``. Each zone keeps its full raw text
    (provenance); safely parseable zones (final score, self-consistent team
    totals) also emit per-field records for the reconciler.
    """
    records: list[dict[str, Any]] = []
    game_id = f"game:{document_hash[:12]}" if document_hash else "game:unknown"
    for zone in raw.get("zones") or []:
        zone_id = str(zone.get("zoneId") or "")
        side_raw = zone.get("side")
        side = str(side_raw) if side_raw else None
        row_index = zone.get("rowIndex")
        raw_text = str(zone.get("rawText") or "").strip()
        status = str(zone.get("status") or "")
        if status == "Failed" or not raw_text:
            continue
        conf = zone.get("confidence")
        conf_f = float(conf) if conf is not None else None

        # One record per zone with the full raw text (zone-level provenance).
        records.append(empty_record(
            field_id=f"zone:{zone_id}",
            entity_id=f"zone:{zone_id}",
            entity_type="zone",
            side=side,
            row_index=int(row_index) if row_index is not None else None,
            stat_key="",
            scope="Zone",
            crop_id=zone_id,
            zone_id=zone_id,
            value_raw=raw_text,
            value_normalized=None,
            confidence_raw=conf_f,
            confidence_normalized=None,
            mapper_id=_MAPPER_ID_CROP,
            warnings=list(zone.get("warnings") or []),
            rect_normalized=zone.get("rectNormalized"),
            rect_pixels=zone.get("rectPixels"),
        ))

        # Safe per-field parse: final score from the header.finalScore crop.
        if zone_id == "header.finalScore":
            score = _parse_final_score(raw_text)
            if score:
                records.append(empty_record(
                    field_id="game.finalScore",
                    entity_id=game_id,
                    entity_type="game",
                    stat_key="finalScore",
                    scope="Game",
                    crop_id=zone_id,
                    zone_id=zone_id,
                    value_raw=raw_text,
                    value_normalized=score,
                    confidence_raw=conf_f,
                    confidence_normalized=conf_f,
                    mapper_id=_MAPPER_ID_CROP,
                ))
        elif zone_id in ("home.teamTotals", "away.teamTotals"):
            records.extend(_team_totals_records(zone_id, zone.get("tokens") or [], conf_f))

    granularity = GRANULARITY_ROW if any(
        "row" in (str(z.get("tier") or "")) for z in raw.get("zones") or []
    ) else "table"

    return make_envelope(
        source_id=source_id,
        engine=engine,
        granularity=granularity,
        document_file_name=str(raw.get("originalFileName") or ""),
        document_hash=document_hash,
        dpi=dpi,
        render_width=render_width,
        render_height=render_height,
        records=records,
    )
