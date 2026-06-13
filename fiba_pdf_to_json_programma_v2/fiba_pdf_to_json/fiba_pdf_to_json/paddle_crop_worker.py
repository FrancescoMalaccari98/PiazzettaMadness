"""paddle_crop_worker.py — Channel 3 (ocr.paddle.crop).

Runs PaddleOCR on the same calibrated zone crops listed in crop-metadata.json,
producing a raw OCR JSON in the SAME schema as the Tesseract crop_worker (zones
with rawText + X-ordered tokens) and a shared evidence_records.json.

Runs in `.venv-paddle`. Use `use_doc_orientation_classify=False` and
`use_doc_unwarping=False`: the doc-preprocessing pipeline distorts the thin
table crops; on the full player-table crop PaddleOCR's native line detection is
excellent.
"""
from __future__ import annotations

import argparse
import json
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from .evidence_mapper import map_crop_ocr_to_evidence
from .evidence_record import SOURCE_PADDLE_CROP
from .evidence_record import write as write_evidence


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Run PaddleOCR on reusable layout crops.")
    parser.add_argument("--metadata", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--evidence-output", default="")
    parser.add_argument("--lang", default="en")
    parser.add_argument("--paddle-model-dir", default="", help="Optional local PaddleOCR model directory.")
    return parser


def _extract_tokens(result: Any) -> list[dict[str, Any]]:
    """Pull (x, y, width, text, conf) tokens from one PaddleOCR predict() result."""
    texts = result.get("rec_texts") or []
    scores = result.get("rec_scores") or []
    polys = result.get("rec_polys") or result.get("dt_polys") or []
    tokens: list[dict[str, Any]] = []
    for index, text in enumerate(texts):
        value = (text or "").strip()
        if not value:
            continue
        conf = float(scores[index]) if index < len(scores) else None
        x = y = width = 0
        if index < len(polys) and polys[index] is not None:
            xs = [float(point[0]) for point in polys[index]]
            ys = [float(point[1]) for point in polys[index]]
            x, width, y = int(min(xs)), int(max(xs) - min(xs)), int(min(ys))
        tokens.append({"x": x, "y": y, "width": width, "text": value, "conf": conf})
    return tokens


def _ocr_zone(ocr: Any, image_path: Path) -> tuple[str, float | None, list[dict[str, Any]]]:
    """Run PaddleOCR on one crop; return (raw_text, avg_confidence, tokens)."""
    tokens: list[dict[str, Any]] = []
    for result in ocr.predict(str(image_path)):
        tokens.extend(_extract_tokens(result))
    confidences = [token["conf"] for token in tokens if token["conf"] is not None]
    avg_conf = (sum(confidences) / len(confidences)) if confidences else None
    # Reading order top-to-bottom; raw_text is X-ordered for single-line zones.
    tokens.sort(key=lambda token: (token["y"], token["x"]))
    raw_text = " ".join(token["text"] for token in sorted(tokens, key=lambda t: t["x"]))
    return raw_text, avg_conf, tokens


def main(argv: list[str] | None = None) -> int:
    started = time.perf_counter()
    args = build_parser().parse_args(argv)
    metadata_path = Path(args.metadata)
    if not metadata_path.exists():
        print(f"Layout crop metadata not found: {metadata_path}", file=sys.stderr)
        return 2

    try:
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        print(f"Layout crop metadata is not readable: {exc}", file=sys.stderr)
        return 2

    if metadata.get("layoutCalibration", {}).get("status") not in {"Calibrated", "Partial"}:
        print("Layout crop metadata is not calibrated or partially usable.", file=sys.stderr)
        return 2

    try:
        from paddleocr import PaddleOCR
    except ImportError as exc:
        print(f"PaddleOCR is unavailable: {exc}", file=sys.stderr)
        return 3

    ocr_kwargs: dict[str, Any] = {
        "use_doc_orientation_classify": False,
        "use_doc_unwarping": False,
        "use_textline_orientation": False,
        "lang": args.lang,
    }
    if args.paddle_model_dir.strip():
        ocr_kwargs["det_model_dir"] = args.paddle_model_dir.strip()
    ocr = PaddleOCR(**ocr_kwargs)

    zone_results: list[dict[str, Any]] = []
    warnings_list: list[str] = []
    errors_list: list[str] = []

    for zone in metadata.get("crops", metadata.get("zones", [])):
        if zone.get("usable") is False:
            continue
        zone_id = str(zone.get("zoneId") or "")
        image_path = Path(str(zone.get("imagePath") or ""))
        zone_warnings: list[str] = []
        zone_errors: list[str] = []
        raw_text = ""
        avg_conf: float | None = None
        tokens: list[dict[str, Any]] = []

        if not image_path.exists():
            zone_errors.append(f"Crop image not found: {image_path}")
        else:
            try:
                raw_text, avg_conf, tokens = _ocr_zone(ocr, image_path)
                if not raw_text:
                    zone_warnings.append("PaddleOCR returned no text for this crop.")
            except Exception as exc:
                zone_errors.append(str(exc))

        warnings_list.extend(f"{zone_id}: {w}" for w in zone_warnings)
        errors_list.extend(f"{zone_id}: {e}" for e in zone_errors)
        zone_results.append({
            "zoneId": zone_id,
            "zoneType": zone.get("zoneType", ""),
            "fileName": zone.get("fileName", ""),
            "tier": zone.get("tier", ""),
            "cropType": zone.get("cropType", ""),
            "side": zone.get("side"),
            "rowIndex": zone.get("rowIndex"),
            "columnId": zone.get("columnId"),
            "expectedContentType": zone.get("expectedContentType", ""),
            "imagePath": str(image_path),
            "fieldIds": zone.get("fieldIds", []),
            "statKeys": zone.get("statKeys", []),
            "rectNormalized": zone.get("rectNormalized"),
            "rectPixels": zone.get("rectPixels"),
            "calibrationId": zone.get("calibrationId"),
            "status": "Failed" if zone_errors else "Success",
            "rawText": raw_text,
            "confidence": avg_conf,
            "tokens": tokens,
            "warnings": zone_warnings,
            "errors": zone_errors,
        })

    output_raw = {
        "schemaVersion": "1.0",
        "engine": "PaddleLayoutCrops",
        "strategy": "PaddleLayoutCrops",
        "outputKind": "OcrRaw",
        "isFinalNormalizedJson": False,
        "createdAtUtc": datetime.now(timezone.utc).isoformat(),
        "originalFileName": metadata.get("originalFileName", ""),
        "sourcePdf": metadata.get("sourcePdf", ""),
        "documentHash": metadata.get("documentHash") or metadata.get("sourceHash", ""),
        "layoutMapVersion": metadata.get("layoutMapVersion", ""),
        "calibrationId": metadata.get("layoutCalibration", {}).get("calibrationId", ""),
        "renderWidth": metadata.get("renderedImageWidth"),
        "renderHeight": metadata.get("renderedImageHeight"),
        "dpi": metadata.get("renderDpi"),
        "durationMs": int((time.perf_counter() - started) * 1000),
        "zones": zone_results,
        "warnings": warnings_list,
        "errors": errors_list,
    }
    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(output_raw, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote PaddleOCR layout-crop raw OCR: {output_path}")

    evidence_output = args.evidence_output.strip()
    if evidence_output:
        envelope = map_crop_ocr_to_evidence(
            output_raw,
            document_hash=output_raw["documentHash"],
            render_width=output_raw.get("renderWidth"),
            render_height=output_raw.get("renderHeight"),
            dpi=output_raw.get("dpi"),
            source_id=SOURCE_PADDLE_CROP,
            engine="paddleocr",
        )
        write_evidence(envelope, evidence_output)
        print(f"Wrote evidence records: {evidence_output}")

    return 0 if zone_results else 4


if __name__ == "__main__":
    raise SystemExit(main())
