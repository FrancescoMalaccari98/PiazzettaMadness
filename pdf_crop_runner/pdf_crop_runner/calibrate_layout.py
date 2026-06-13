from __future__ import annotations

import argparse
import html
import json
import re
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw

from .image_preparation import describe_input, prepare_images


def _parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Calibrate and prepare fixed FIBA layout crops.")
    parser.add_argument("--pdf", required=True)
    parser.add_argument("--word-boxes", required=True)
    parser.add_argument("--layout-map", required=True)
    parser.add_argument("--calibration-rules", default="")
    parser.add_argument("--output-folder", required=True)
    parser.add_argument("--image-output-folder", required=True)
    parser.add_argument("--layout-debug-folder", required=True)
    parser.add_argument("--document-hash", default="")
    parser.add_argument("--original-file-name", default="")
    parser.add_argument("--input-mode", default="Crops")
    parser.add_argument("--max-pages", type=int, default=1)
    return parser.parse_args()


def _normalized_word(value: str) -> str:
    return re.sub(r"[^a-z0-9]+", "", value.casefold())


def normalize_layout_zone(source: dict[str, Any]) -> dict[str, Any]:
    crop_id = str(source.get("cropId") or source.get("zoneId") or "").strip()
    if not crop_id:
        raise ValueError("Layout zone requires zoneId or cropId.")

    source_rect = source.get("rectNormalized")
    if not isinstance(source_rect, dict):
        source_rect = source
    rect = {
        "x": float(source_rect["x"]),
        "y": float(source_rect["y"]),
        "width": float(source_rect["width"]),
        "height": float(source_rect["height"]),
    }
    if rect["width"] <= 0 or rect["height"] <= 0:
        raise ValueError(f"Layout zone '{crop_id}' requires positive width and height.")

    return {
        "cropId": crop_id,
        "fileName": str(source.get("fileName") or f"{crop_id}.png"),
        "tier": str(source.get("tier") or "semantic"),
        "cropType": str(source.get("cropType") or crop_id),
        "side": source.get("side") or None,
        "expectedContentType": str(source.get("expectedContentType") or source.get("cropType") or crop_id),
        "pageNumber": int(source.get("pageNumber") or 1),
        "rectNormalized": rect,
        "coordinateUnit": "normalized",
        "expectedFieldIds": list(source.get("expectedFieldIds") or []),
        "expectedStatKeys": list(source.get("expectedStatKeys") or []),
        "semanticRole": str(source.get("semanticRole") or ""),
        "source": str(source.get("source") or "layout-map"),
        "parentZoneId": str(source.get("parentZoneId") or ""),
        "deriveMode": str(source.get("deriveMode") or ""),
    }


def normalize_layout_zones(layout_map: dict[str, Any]) -> list[dict[str, Any]]:
    return [normalize_layout_zone(zone) for zone in layout_map.get("zones", [])]


def load_calibration_rules(path: str | Path, layout_map: dict[str, Any]) -> dict[str, Any]:
    rules_path = Path(path).resolve()
    if not rules_path.exists():
        raise FileNotFoundError(f"Layout calibration rules not found: {rules_path}")
    rules = json.loads(rules_path.read_text(encoding="utf-8"))
    required = ["strategy", "pageAssumptions", "zoneRules", "tableRules", "columnCalibration", "failurePolicy"]
    missing = [name for name in required if name not in rules]
    if missing:
        raise ValueError(f"Layout calibration rules missing required sections: {', '.join(missing)}")

    zone_ids = {str(zone.get("zoneId") or zone.get("cropId") or "") for zone in layout_map.get("zones", [])}
    rule_zone_ids = {
        str(rule.get("templateZoneId") or "")
        for rule in rules["zoneRules"].values()
        if isinstance(rule, dict) and rule.get("templateZoneId")
    }
    unknown = sorted(rule_zone_ids - zone_ids)
    if unknown:
        raise ValueError(f"Layout calibration rules reference unknown layout-map zones: {', '.join(unknown)}")
    return rules


def _crop_input_mode(value: str) -> str:
    normalized = value.strip().casefold()
    if normalized in {"crops", "manual-calibration"}:
        return "Crops"
    if normalized == "both":
        return "Both"
    raise ValueError(f"Unsupported calibration input mode: {value}")


def _rect_pixels(zone: dict[str, Any], width: int, height: int) -> dict[str, int]:
    rect = zone["rectNormalized"]
    return {
        "x": round(float(rect["x"]) * width),
        "y": round(float(rect["y"]) * height),
        "width": round(float(rect["width"]) * width),
        "height": round(float(rect["height"]) * height),
    }


def _expanded_rect(zone: dict[str, Any], width: int, height: int, padding: float = 0.02) -> dict[str, int]:
    rect = zone["rectNormalized"]
    left = max(0, round((float(rect["x"]) - padding) * width))
    top = max(0, round((float(rect["y"]) - padding) * height))
    right = min(width, round((float(rect["x"]) + float(rect["width"]) + padding) * width))
    bottom = min(height, round((float(rect["y"]) + float(rect["height"]) + padding) * height))
    return {"x": left, "y": top, "width": max(0, right - left), "height": max(0, bottom - top)}


def _draw_overlay(source: Path, output: Path, zones: list[dict[str, Any]], color: tuple[int, int, int]) -> None:
    with Image.open(source) as source_image:
        image = source_image.convert("RGB")
        draw = ImageDraw.Draw(image)
        for zone in zones:
            rect = _rect_pixels(zone, image.width, image.height)
            left = rect["x"]
            top = rect["y"]
            right = left + rect["width"]
            bottom = top + rect["height"]
            draw.rectangle((left, top, right, bottom), outline=color, width=3)
            draw.text((left + 3, top + 3), zone["cropId"], fill=color)
        image.save(output)


def _write_contact_sheet(output: Path, crops: list[dict[str, Any]]) -> None:
    cards = []
    for crop in crops:
        image_uri = Path(crop["imagePath"]).resolve().as_uri()
        cards.append(
            "<figure>"
            f"<img src=\"{html.escape(image_uri)}\" alt=\"{html.escape(crop['cropId'])}\">"
            f"<figcaption>{html.escape(crop['cropId'])} - {html.escape(crop['fileName'])}</figcaption>"
            "</figure>"
        )
    output.write_text(
        """<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Calibrated layout crops</title>
<style>
body { font-family: Arial, sans-serif; margin: 20px; }
main { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; }
figure { margin: 0; border: 1px solid #ccc; padding: 8px; }
img { width: 100%; height: auto; display: block; }
figcaption { margin-top: 6px; font-weight: 700; }
</style>
</head>
<body>
<h1>Calibrated layout crops</h1>
<main>"""
        + "".join(cards)
        + "</main></body></html>",
        encoding="utf-8",
    )


def _box_inside(word: dict[str, Any], width: int, height: int, tolerance: int = 2) -> bool:
    try:
        left = int(word["x"])
        top = int(word["y"])
        right = left + int(word["width"])
        bottom = top + int(word["height"])
    except (KeyError, TypeError, ValueError):
        return False
    return left >= -tolerance and top >= -tolerance and right <= width + tolerance and bottom <= height + tolerance


def _validate_geometry(geometry: dict[str, Any], rendered_page: Path) -> dict[str, Any]:
    warnings: list[str] = []
    errors: list[str] = []
    with Image.open(rendered_page) as image:
        width, height = image.size
    if geometry.get("strategy") != "TesseractFullPage":
        errors.append("Geometry strategy is not TesseractFullPage.")
    if geometry.get("coordinateBasis") != "full-page-rendered.png":
        errors.append("Geometry coordinate basis is not the canonical full-page-rendered.png artifact.")
    if int(geometry.get("renderedImageWidth") or 0) != width or int(geometry.get("renderedImageHeight") or 0) != height:
        errors.append("Geometry dimensions do not match full-page-rendered.png.")

    words = list(geometry.get("words") or [])
    if not words:
        errors.append("Geometry contains no OCR word boxes.")
    outside_count = sum(not _box_inside(word, width, height) for word in words)
    if outside_count:
        errors.append(f"{outside_count} OCR word boxes fall outside the canonical rendered page.")

    return {
        "status": "Aligned" if not errors else "Failed",
        "coordinateBasis": "full-page-rendered.png",
        "coordinateSystem": "pixel-top-left-origin",
        "renderedImageWidth": width,
        "renderedImageHeight": height,
        "wordCount": len(words),
        "outsideWordBoxCount": outside_count,
        "warnings": warnings,
        "errors": errors,
    }


def _word_record(word: dict[str, Any]) -> dict[str, Any]:
    return {
        "text": str(word.get("text") or ""),
        "normalizedText": _normalized_word(str(word.get("text") or "")),
        "x": int(word.get("x") or 0),
        "y": int(word.get("y") or 0),
        "width": int(word.get("width") or 0),
        "height": int(word.get("height") or 0),
        "confidence": word.get("confidence"),
        "blockIndex": int(word.get("blockIndex") or 0),
        "paragraphIndex": int(word.get("paragraphIndex") or 0),
        "lineIndex": int(word.get("lineIndex") or 0),
        "wordIndex": int(word.get("wordIndex") or 0),
    }


def _line_records(words: list[dict[str, Any]]) -> list[dict[str, Any]]:
    groups: dict[tuple[int, int, int], list[dict[str, Any]]] = {}
    for raw_word in words:
        word = _word_record(raw_word)
        key = (word["blockIndex"], word["paragraphIndex"], word["lineIndex"])
        if key == (0, 0, 0):
            key = (0, 0, round(word["y"] / 8))
        groups.setdefault(key, []).append(word)

    lines = []
    for key, line_words in groups.items():
        ordered = sorted(line_words, key=lambda item: item["x"])
        left = min(item["x"] for item in ordered)
        top = min(item["y"] for item in ordered)
        right = max(item["x"] + item["width"] for item in ordered)
        bottom = max(item["y"] + item["height"] for item in ordered)
        lines.append(
            {
                "key": list(key),
                "text": " ".join(item["text"] for item in ordered),
                "normalizedTokens": [item["normalizedText"] for item in ordered if item["normalizedText"]],
                "rectPixels": {"x": left, "y": top, "width": right - left, "height": bottom - top},
                "words": ordered,
            }
        )
    return sorted(lines, key=lambda item: (item["rectPixels"]["y"], item["rectPixels"]["x"]))


def _center_inside(rect: dict[str, int], area: dict[str, int]) -> bool:
    center_x = rect["x"] + rect["width"] / 2
    center_y = rect["y"] + rect["height"] / 2
    return area["x"] <= center_x <= area["x"] + area["width"] and area["y"] <= center_y <= area["y"] + area["height"]


def _lines_inside(lines: list[dict[str, Any]], area: dict[str, int]) -> list[dict[str, Any]]:
    return [line for line in lines if _center_inside(line["rectPixels"], area)]


def _words_inside(words: list[dict[str, Any]], area: dict[str, int]) -> list[dict[str, Any]]:
    records = [_word_record(word) for word in words]
    return [word for word in records if _center_inside(word, area)]


def _score_candidates(lines: list[dict[str, Any]], area: dict[str, int]) -> list[dict[str, Any]]:
    pattern = re.compile(r"\b\d{1,3}\s*[-\u2013\u2014]\s*\d{1,3}\b")
    candidates = []
    for line in _lines_inside(lines, area):
        if pattern.search(line["text"]):
            candidates.append(line)
    return candidates


def _table_header_candidate(words: list[dict[str, Any]], zone: dict[str, Any], width: int, height: int) -> dict[str, Any] | None:
    area = _expanded_rect(zone, width, height, padding=0.03)
    area["height"] = max(1, round(area["height"] * 0.32))
    candidates = _words_inside(words, area)
    normalized = {_normalized_word(str(word.get("text") or "")) for word in candidates}
    expected = {"nome", "min", "tiri", "punti", "liberi", "rimbalzi", "as", "pp", "pr", "sd"}
    matched = sorted(expected.intersection(normalized))
    if len(matched) < 3:
        return None
    return {"zoneId": zone["cropId"], "matchedLabels": matched, "rectPixels": area, "words": candidates}


def _anchor_words(words: list[dict[str, Any]], normalized_terms: set[str]) -> list[dict[str, Any]]:
    records = [_word_record(word) for word in words]
    return [word for word in records if word["normalizedText"] in normalized_terms]


def _detect_anchors(geometry: dict[str, Any], zones: list[dict[str, Any]], width: int, height: int) -> dict[str, Any]:
    words = list(geometry.get("words") or [])
    lines = _line_records(words)
    zones_by_id = {zone["cropId"]: zone for zone in zones}
    score_area = _expanded_rect(zones_by_id["header.finalScore"], width, height, padding=0.02)
    score_candidates = _score_candidates(lines, score_area)
    table_headers = [
        candidate
        for zone_id in ("home.playerTable", "away.playerTable")
        if (candidate := _table_header_candidate(words, zones_by_id[zone_id], width, height)) is not None
    ]
    totals = _anchor_words(words, {"totali"})
    # Bench rows print "Squadra/Allenatore"; OCR mangles it (e.g. "[Squadra/Mlenatore"),
    # so match any token whose normalized text CONTAINS "squadra". This excludes the
    # header label "Allenatore:" (which has no "squadra") that an exact match caught.
    team_bench = [
        record
        for record in (_word_record(word) for word in words)
        if "squadra" in record["normalizedText"]
    ]
    period_area = _expanded_rect(zones_by_id["header.periodScores"], width, height, padding=0.02)
    period_tokens = [
        word for word in _words_inside(words, period_area)
        if re.fullmatch(r"\d{1,3}", str(word.get("text") or "").strip())
    ]
    return {
        "schemaVersion": "1.0",
        "outputKind": "LayoutCalibrationDetectedAnchors",
        "isFinalNormalizedJson": False,
        "createdAtUtc": datetime.now(timezone.utc).isoformat(),
        "finalScore": {
            "status": "Reliable" if len(score_candidates) == 1 else ("Missing" if not score_candidates else "Ambiguous"),
            "searchRectPixels": score_area,
            "candidates": score_candidates,
        },
        "tableHeaders": {
            "status": "Reliable" if len(table_headers) == 2 else ("Missing" if not table_headers else "Partial"),
            "candidates": table_headers,
        },
        "totaliRows": {"status": "Detected" if totals else "Missing", "candidates": totals},
        "teamBenchRows": {"status": "Detected" if team_bench else "Missing", "candidates": team_bench},
        "periodScores": {"status": "Detected" if period_tokens else "Missing", "candidates": period_tokens},
    }


def _anchor_in_zone(anchor: dict[str, Any], zone: dict[str, Any], width: int, height: int, padding: float = 0.03) -> bool:
    return _center_inside(anchor, _expanded_rect(zone, width, height, padding))


def _zone_usability(
    zones: list[dict[str, Any]],
    anchors: dict[str, Any],
    geometry_alignment: dict[str, Any],
    width: int,
    height: int,
) -> list[dict[str, Any]]:
    if geometry_alignment["status"] != "Aligned":
        return [
            {"cropId": zone["cropId"], "usable": False, "status": "Failed", "warnings": [], "errors": ["geometryMisaligned"]}
            for zone in zones
        ]

    table_headers = {candidate["zoneId"] for candidate in anchors["tableHeaders"]["candidates"]}
    totals = anchors["totaliRows"]["candidates"]
    records = []
    for zone in zones:
        crop_id = zone["cropId"]
        warnings: list[str] = []
        errors: list[str] = []
        usable = True
        # Philosophy: the fixed template (calibrated by the finalScore offset) is
        # the geometric source of truth. Full-page OCR anchors only RAISE
        # confidence; their absence lowers it via a warning but must NOT discard a
        # crop, otherwise mediocre OCR would "discover" which crops exist.
        # A crop is only Failed when page geometry itself is misaligned (handled
        # above) or the critical final-score anchor is unreliable.
        if crop_id == "header.finalScore" and anchors["finalScore"]["status"] != "Reliable":
            usable = False
            errors.append(f"finalScoreAnchor{anchors['finalScore']['status']}")
        elif crop_id in {"home.playerTable", "away.playerTable"}:
            has_header = crop_id in table_headers
            has_totals = any(_anchor_in_zone(anchor, zone, width, height) for anchor in totals)
            if not has_header:
                warnings.append("layout.table.headerAnchorMissing")
            if not has_totals:
                warnings.append("layout.table.totalsAnchorMissing")
            if not has_header and not has_totals:
                # Template fallback: usable but flagged as weakly calibrated.
                warnings.append("layout.table.calibrationEvidenceWeak")
        elif crop_id in {"home.teamTotals", "away.teamTotals"}:
            if not any(_anchor_in_zone(anchor, zone, width, height, padding=0.02) for anchor in totals):
                warnings.append("layout.table.totalsAnchorMissing")
        elif crop_id == "header.periodScores" and anchors["periodScores"]["status"] == "Missing":
            warnings.append("layout.periodScores.anchorMissing")

        records.append(
            {
                "cropId": crop_id,
                "usable": usable,
                "status": "Calibrated" if usable else "Failed",
                "warnings": warnings,
                "errors": errors,
            }
        )
    return records


def _annotate_crops(crops: list[dict[str, Any]], usability: list[dict[str, Any]]) -> list[dict[str, Any]]:
    by_id = {record["cropId"]: record for record in usability}
    for crop in crops:
        record = by_id.get(crop["cropId"])
        if record is None:
            # Derived crops (e.g. player rows) have no usability record of their
            # own; they inherit it from the parent zone they were sliced from.
            parent_id = str(crop.get("parentZoneId") or "")
            record = by_id.get(parent_id, {})
        crop["usable"] = bool(record.get("usable"))
        crop["calibrationStatus"] = str(record.get("status") or "Failed")
        crop["calibrationWarnings"] = list(record.get("warnings") or [])
        crop["calibrationErrors"] = list(record.get("errors") or [])
    return crops


def _status_for(geometry_alignment: dict[str, Any], usability: list[dict[str, Any]]) -> str:
    if geometry_alignment["status"] != "Aligned":
        return "Failed"
    return "Calibrated" if all(record["usable"] for record in usability) else "Partial"


def _collect_calibration_points(
    anchors: dict[str, Any],
    zones_by_id: dict[str, dict[str, Any]],
    image_height: int,
) -> tuple[list[tuple[float, float]], list[str]]:
    """Collect (template_y, real_y) anchor pairs for the affine Y fit.

    Anchors used (the ones full-page OCR reads reliably):
      - final-score line   -> template header.finalScore centre
      - bench rows         -> template home/away teamTotals top
        (Squadra/Allenatore sits immediately above the Totali row)
    """
    points: list[tuple[float, float]] = []
    used: list[str] = []

    fs = anchors.get("finalScore", {})
    if fs.get("status") == "Reliable" and fs.get("candidates") and image_height:
        rect = fs["candidates"][0].get("rectPixels") or {}
        tmpl = zones_by_id.get("header.finalScore")
        if rect.get("height") and tmpl:
            real_y = (rect["y"] + rect["height"] / 2.0) / image_height
            tr = tmpl["rectNormalized"]
            points.append((tr["y"] + tr["height"] / 2.0, real_y))
            used.append("finalScore")

    bench = sorted(
        anchors.get("teamBenchRows", {}).get("candidates", []),
        key=lambda c: c.get("y", 0),
    )
    bench_specs = []
    if bench:
        bench_specs.append((bench[0], "home.teamTotals", "benchHome"))
    if len(bench) >= 2:
        bench_specs.append((bench[-1], "away.teamTotals", "benchAway"))
    for cand, tmpl_id, label in bench_specs:
        tmpl = zones_by_id.get(tmpl_id)
        if tmpl and image_height:
            real_y = (cand.get("y", 0) + cand.get("height", 0) / 2.0) / image_height
            points.append((tmpl["rectNormalized"]["y"], real_y))
            used.append(label)

    return points, used


def _least_squares_line(points: list[tuple[float, float]]) -> tuple[float, float]:
    """Fit real = scale * template + offset. Returns (scale, offset)."""
    n = len(points)
    sx = sum(p[0] for p in points)
    sy = sum(p[1] for p in points)
    sxx = sum(p[0] * p[0] for p in points)
    sxy = sum(p[0] * p[1] for p in points)
    denom = n * sxx - sx * sx
    if abs(denom) < 1e-12:
        return 1.0, (sy - sx) / n
    scale = (n * sxy - sx * sy) / denom
    offset = (sy - scale * sx) / n
    return scale, offset


def _compute_calibration_transform(
    anchors: dict[str, Any],
    zones: list[dict[str, Any]],
    image_height: int,
    rules: dict[str, Any],
) -> tuple[dict[str, Any], list[str]]:
    """Derive a bounded affine Y transform (scale + offset) from anchors.

    The fixed template is the geometric source of truth; OCR anchors only
    calibrate its vertical scale/offset within tolerance. X is left unchanged
    (FIBA PDFs have stable left/right margins).
    """
    zones_by_id = {z["cropId"]: z for z in zones}
    page = rules.get("pageAssumptions", {})
    max_offset = float(page.get("maxOffsetNormalized", 0.15))
    max_scale_dev = float(page.get("maxScaleDeviation", 0.30))
    warnings: list[str] = []

    points, used = _collect_calibration_points(anchors, zones_by_id, image_height)

    def _identity(source: str, msg: list[str]) -> tuple[dict[str, Any], list[str]]:
        return (
            {"offsetX": 0.0, "offsetY": 0.0, "scaleX": 1.0, "scaleY": 1.0,
             "appliedAnchors": len(points), "usedAnchors": used, "source": source},
            msg,
        )

    if not points:
        return _identity("identity", ["No reliable anchors found; identity transform."])

    if len(points) == 1:
        offset = max(-max_offset, min(max_offset, points[0][1] - points[0][0]))
        return (
            {"offsetX": 0.0, "offsetY": round(offset, 6), "scaleX": 1.0, "scaleY": 1.0,
             "appliedAnchors": 1, "usedAnchors": used, "source": "offset-only"},
            warnings,
        )

    raw_scale, raw_offset = _least_squares_line(points)
    scale, offset, clamped = raw_scale, raw_offset, False
    if abs(raw_scale - 1.0) > max_scale_dev:
        scale = max(1.0 - max_scale_dev, min(1.0 + max_scale_dev, raw_scale))
        clamped = True
        warnings.append(f"Calibration scaleY {raw_scale:.4f} out of tolerance; clamped to {scale:.4f}.")
    if abs(raw_offset) > max_offset:
        offset = max(-max_offset, min(max_offset, raw_offset))
        clamped = True
        warnings.append(f"Calibration offsetY {raw_offset:.4f} out of tolerance; clamped to {offset:.4f}.")

    transform = {
        "offsetX": 0.0,
        "offsetY": round(offset, 6),
        "scaleX": 1.0,
        "scaleY": round(scale, 6),
        "rawScaleY": round(raw_scale, 6),
        "rawOffsetY": round(raw_offset, 6),
        "appliedAnchors": len(points),
        "usedAnchors": used,
        "clamped": clamped,
        "source": "affine-least-squares",
    }
    return transform, warnings


def _apply_transform_to_zones(
    zones: list[dict[str, Any]],
    transform: dict[str, Any],
) -> list[dict[str, Any]]:
    """Return new zone list with rectNormalized shifted by the transform."""
    scale_y = float(transform.get("scaleY", 1.0))
    offset_y = float(transform.get("offsetY", 0.0))
    if abs(scale_y - 1.0) < 1e-9 and abs(offset_y) < 1e-9:
        return zones
    result = []
    for zone in zones:
        z = dict(zone)
        r = dict(z["rectNormalized"])
        # Affine Y: both position and height scale with the page content.
        new_y = r["y"] * scale_y + offset_y
        new_h = r["height"] * scale_y
        # Clamp to [0, 1] while preserving as much height as possible.
        if new_y < 0.0:
            new_h = max(0.0, new_h + new_y)
            new_y = 0.0
        if new_y + new_h > 1.0:
            new_h = max(0.0, 1.0 - new_y)
        r["y"] = round(new_y, 6)
        r["height"] = round(new_h, 6)
        z["rectNormalized"] = r
        result.append(z)
    return result




def calibrate_layout(args: argparse.Namespace) -> dict[str, Any]:
    started = time.perf_counter()
    geometry_path = Path(args.word_boxes).resolve()
    geometry = json.loads(geometry_path.read_text(encoding="utf-8"))
    rendered_page = Path(geometry["renderedImagePath"]).resolve()
    if not rendered_page.exists():
        raise FileNotFoundError(f"Full-page rendered image not found: {rendered_page}")

    layout_map_path = Path(args.layout_map).resolve()
    layout_map = json.loads(layout_map_path.read_text(encoding="utf-8"))
    zones = normalize_layout_zones(layout_map)
    if not zones:
        raise ValueError("Layout map does not define zones.")
    rules_path = Path(args.calibration_rules).resolve() if args.calibration_rules else layout_map_path.with_name("layout-calibration-rules.json")
    rules = load_calibration_rules(rules_path, layout_map)
    input_mode = _crop_input_mode(args.input_mode)

    debug_folder = Path(args.layout_debug_folder).resolve()
    debug_folder.mkdir(parents=True, exist_ok=True)
    output_folder = Path(args.output_folder).resolve()
    output_folder.mkdir(parents=True, exist_ok=True)
    image_output_folder = Path(args.image_output_folder).resolve()
    image_output_folder.mkdir(parents=True, exist_ok=True)

    before_overlay = debug_folder / "crop-overlay-before.png"
    after_overlay = debug_folder / "crop-overlay-after.png"
    report_path = debug_folder / "layout-calibration-report.json"
    anchors_path = debug_folder / "detected-anchors.json"
    contact_sheet = debug_folder / "contact-sheet-after.html"
    runtime_layout_map_path = debug_folder / "calibrated-layout-map.runtime.json"

    with Image.open(rendered_page) as image:
        image_width, image_height = image.size
    geometry_alignment = _validate_geometry(geometry, rendered_page)
    anchors = _detect_anchors(geometry, zones, image_width, image_height)
    anchors["sourceGeometryPath"] = str(geometry_path)
    anchors_path.write_text(json.dumps(anchors, ensure_ascii=False, indent=2), encoding="utf-8")
    usability = _zone_usability(zones, anchors, geometry_alignment, image_width, image_height)
    calibration_status = _status_for(geometry_alignment, usability)

    # Compute bounded calibration transform from anchor evidence.
    transform, _transform_warnings = _compute_calibration_transform(
        anchors, zones, image_height, rules
    )
    calibration_id = f"cal-{args.document_hash[:12]}" if args.document_hash else "cal-unknown"
    transform["calibrationId"] = calibration_id
    transform["renderWidth"] = image_width
    transform["renderHeight"] = image_height
    transform["dpi"] = int(geometry.get("renderDpi") or 144)
    transform["layoutMapVersion"] = str(layout_map.get("schemaVersion") or "")

    _draw_overlay(rendered_page, before_overlay, zones, (255, 140, 0))
    usable_ids = {record["cropId"] for record in usability if record["usable"]}
    usable_zones = [zone for zone in zones if zone["cropId"] in usable_ids]
    # Apply transform to usable zones so crops are anchored to detected positions.
    calibrated_zones = _apply_transform_to_zones(usable_zones, transform)
    # NOTE: per-row crops are intentionally NOT pre-cut here. FIBA player rows are
    # too tightly packed to segment geometrically; the paddle.row channel instead
    # runs line-detection OCR on the calibrated player-table crop.
    _draw_overlay(rendered_page, after_overlay, calibrated_zones, (0, 150, 0))

    prepared: dict[str, Any] = {"pages": [], "crops": [], "artifacts": [], "warnings": []}
    if calibration_status != "Failed" and calibrated_zones:
        calibrated_layout_map = dict(layout_map)
        calibrated_layout_map["zones"] = calibrated_zones
        calibrated_layout_map["calibration"] = {
            "status": calibration_status,
            "mode": "FixedStructureWithOcrGeometryCalibration",
            "rulesPath": str(rules_path),
            "sourceGeometryPath": str(geometry_path),
            "calibrationId": calibration_id,
            "transformNormalized": transform,
        }
        runtime_layout_map_path.write_text(json.dumps(calibrated_layout_map, ensure_ascii=False, indent=2), encoding="utf-8")
        prepared = prepare_images(
            input_path=str(rendered_page),
            input_mode=input_mode,
            image_output_folder=str(image_output_folder),
            zone_output_folder=str(output_folder),
            layout_map_path=str(runtime_layout_map_path),
            max_pages=1,
        )
        prepared["crops"] = _annotate_crops(prepared.get("crops", []), usability)
        # Enrich each crop record with manifest-level fields.
        for crop in prepared["crops"]:
            crop["calibrationId"] = calibration_id
            crop["dpi"] = transform["dpi"]
            crop["renderWidth"] = image_width
            crop["renderHeight"] = image_height
            crop["layoutMapVersion"] = transform["layoutMapVersion"]
            crop["sourceImagePath"] = str(rendered_page)
        _write_contact_sheet(contact_sheet, prepared["crops"])

    template_page = layout_map.get("page", {})
    template_width = int(template_page.get("width") or image_width)
    template_height = int(template_page.get("height") or image_height)
    missing_anchors = [
        name for name, detail in anchors.items()
        if isinstance(detail, dict) and detail.get("status") == "Missing"
    ]
    ambiguous_anchors = [
        name for name, detail in anchors.items()
        if isinstance(detail, dict) and detail.get("status") in {"Ambiguous", "Partial"}
    ]
    report_warnings = (
        [warning for record in usability for warning in record["warnings"]]
        + list(prepared.get("warnings", []))
        + list(_transform_warnings)
    )
    report_errors = list(geometry_alignment["errors"]) + [
        error
        for record in usability
        for error in record["errors"]
    ]
    report = {
        "schemaVersion": "1.0",
        "outputKind": "LayoutCalibrationReport",
        "isFinalNormalizedJson": False,
        "createdAtUtc": datetime.now(timezone.utc).isoformat(),
        "strategy": rules["strategy"],
        "status": calibration_status,
        "sourcePdf": str(Path(args.pdf).resolve()),
        "documentHash": args.document_hash,
        "sourceGeometryPath": str(geometry_path),
        "sourceRenderedImagePath": str(rendered_page),
        "templateLayoutMapPath": str(layout_map_path),
        "calibrationRulesPath": str(rules_path),
        "runtimeLayoutMapPath": str(runtime_layout_map_path) if runtime_layout_map_path.exists() else "",
        "layoutMapName": layout_map.get("name", ""),
        "layoutMapVersion": layout_map.get("schemaVersion", ""),
        "calibrationMode": "FixedStructureWithOcrGeometryCalibration",
        "geometryAlignment": geometry_alignment,
        "templatePageWidth": template_width,
        "templatePageHeight": template_height,
        "runtimePageWidth": image_width,
        "runtimePageHeight": image_height,
        "pixelScaleX": image_width / template_width,
        "pixelScaleY": image_height / template_height,
        "detectedAnchorsPath": str(anchors_path),
        "detectedAnchors": anchors,
        "missingAnchors": missing_anchors,
        "ambiguousAnchors": ambiguous_anchors,
        "zones": zones,
        "perCropUsability": usability,
        "warnings": report_warnings,
        "errors": report_errors,
    }
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    metadata = {
        "schemaVersion": "2.0",
        "engine": "LayoutCalibration",
        "outputKind": "LayoutCropPreparation",
        "isFinalNormalizedJson": False,
        "createdAtUtc": datetime.now(timezone.utc).isoformat(),
        "input": describe_input(str(Path(args.pdf).resolve())),
        "documentFileName": args.original_file_name or Path(args.pdf).name,
        "originalFileName": args.original_file_name or Path(args.pdf).name,
        "sourcePdf": str(Path(args.pdf).resolve()),
        "sourceFileName": Path(args.pdf).name,
        "documentHash": args.document_hash,
        "sourceHash": args.document_hash,
        "pageIndex": 0,
        "renderDpi": geometry.get("renderDpi"),
        "renderScale": geometry.get("renderScale") or 1,
        "renderedImageWidth": image_width,
        "renderedImageHeight": image_height,
        "layoutMapPath": str(layout_map_path),
        "layoutMapName": layout_map.get("name", ""),
        "layoutMapVersion": layout_map.get("schemaVersion", ""),
        "calibrationRulesPath": str(rules_path),
        "outputFolder": str(output_folder),
        "imageOutputFolder": str(image_output_folder),
        "inputMode": input_mode,
        "requestedInputMode": args.input_mode,
        "durationMs": int((time.perf_counter() - started) * 1000),
        "pages": prepared.get("pages", []),
        "crops": prepared.get("crops", []),
        "zones": prepared.get("crops", []),
        "artifacts": prepared.get("artifacts", []),
        "layoutCalibration": {
            "status": calibration_status,
            "mode": "FixedStructureWithOcrGeometryCalibration",
            "calibrationId": calibration_id,
            "transformNormalized": transform,
            "sourceGeometryPath": str(geometry_path),
            "rulesPath": str(rules_path),
            "reportPath": str(report_path),
            "detectedAnchorsPath": str(anchors_path),
            "cropOverlayBeforePath": str(before_overlay),
            "cropOverlayAfterPath": str(after_overlay),
            "contactSheetAfterPath": str(contact_sheet) if contact_sheet.exists() else "",
            "runtimeLayoutMapPath": str(runtime_layout_map_path) if runtime_layout_map_path.exists() else "",
        },
        "warnings": report_warnings,
        "errors": report_errors,
    }
    metadata_path = output_folder / "crop-metadata.json"
    metadata_path.write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
    return metadata


def main() -> None:
    calibrate_layout(_parse_args())


if __name__ == "__main__":
    main()
