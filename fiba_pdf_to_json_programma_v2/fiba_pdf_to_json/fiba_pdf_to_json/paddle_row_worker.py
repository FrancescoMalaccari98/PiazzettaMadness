"""paddle_row_worker.py — Channel 4 (ocr.paddle.row).

Runs PaddleOCR on the home/away player-table crops and parses each player row
into per-player evidence records. Reuses the PaddleOCR call from
`paddle_crop_worker` and the row parser from `paddle_row_parser`.

Runs in `.venv-paddle`.
"""
from __future__ import annotations

import argparse
import json
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from .evidence_mapper import map_paddle_rows_to_evidence
from .evidence_record import write as write_evidence
from .paddle_crop_worker import _ocr_zone

_TABLE_ZONES = ("home.playerTable", "away.playerTable")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Run PaddleOCR row extraction on player-table crops.")
    parser.add_argument("--metadata", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--evidence-output", default="")
    parser.add_argument("--lang", default="en")
    parser.add_argument("--paddle-model-dir", default="")
    return parser


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
    errors_list: list[str] = []
    crops = {str(z.get("zoneId") or ""): z for z in metadata.get("crops", metadata.get("zones", []))}

    for zone_id in _TABLE_ZONES:
        zone = crops.get(zone_id)
        if zone is None or zone.get("usable") is False:
            continue
        image_path = Path(str(zone.get("imagePath") or ""))
        tokens: list[dict[str, Any]] = []
        zone_errors: list[str] = []
        if not image_path.exists():
            zone_errors.append(f"Crop image not found: {image_path}")
        else:
            try:
                _raw_text, _avg_conf, tokens = _ocr_zone(ocr, image_path)
            except Exception as exc:
                zone_errors.append(str(exc))
        errors_list.extend(f"{zone_id}: {e}" for e in zone_errors)
        zone_results.append({
            "zoneId": zone_id,
            "side": zone.get("side"),
            "imagePath": str(image_path),
            "status": "Failed" if zone_errors else "Success",
            "tokens": tokens,
            "errors": zone_errors,
        })

    output_raw = {
        "schemaVersion": "1.0",
        "engine": "PaddleTableRows",
        "outputKind": "OcrRaw",
        "isFinalNormalizedJson": False,
        "createdAtUtc": datetime.now(timezone.utc).isoformat(),
        "originalFileName": metadata.get("originalFileName", ""),
        "sourcePdf": metadata.get("sourcePdf", ""),
        "documentHash": metadata.get("documentHash") or metadata.get("sourceHash", ""),
        "renderWidth": metadata.get("renderedImageWidth"),
        "renderHeight": metadata.get("renderedImageHeight"),
        "dpi": metadata.get("renderDpi"),
        "durationMs": int((time.perf_counter() - started) * 1000),
        "zones": zone_results,
        "errors": errors_list,
    }
    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(output_raw, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote PaddleOCR row raw OCR: {output_path}")

    evidence_output = args.evidence_output.strip()
    if evidence_output:
        envelope = map_paddle_rows_to_evidence(
            output_raw,
            document_hash=output_raw["documentHash"],
            render_width=output_raw.get("renderWidth"),
            render_height=output_raw.get("renderHeight"),
            dpi=output_raw.get("dpi"),
        )
        write_evidence(envelope, evidence_output)
        print(f"Wrote evidence records: {evidence_output}")

    return 0 if zone_results else 4


if __name__ == "__main__":
    raise SystemExit(main())
