from __future__ import annotations

import argparse
import json
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Run lightweight Tesseract OCR on reusable layout crops.")
    parser.add_argument("--metadata", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--evidence-output", default="", help="Optional path for evidence_records.json.")
    parser.add_argument("--preprocessed-output-folder", required=True)
    parser.add_argument("--zone-id-filter", default="")
    parser.add_argument("--scale", type=int, default=2)
    parser.add_argument("--threshold", type=int, default=170)
    parser.add_argument("--preprocess", action="store_true")
    parser.add_argument("--save-preprocessed-images", action="store_true")
    parser.add_argument("--sharpen", action="store_true")
    return parser


def _preprocess_image(image: Any, args: argparse.Namespace) -> Any:
    from PIL import ImageEnhance, ImageFilter, ImageOps
    image = ImageOps.autocontrast(image)
    scale = max(1, args.scale)
    if scale > 1:
        from PIL import Image as PILImage
        image = image.resize((image.width * scale, image.height * scale), PILImage.Resampling.LANCZOS)
    image = ImageEnhance.Contrast(image).enhance(1.15)
    threshold = max(0, min(args.threshold, 255))
    image = image.point(lambda p: 255 if p >= threshold else 0)
    if args.sharpen:
        image = image.filter(ImageFilter.SHARPEN)
    return image


def _ocr_zone(image: Any) -> tuple[str, float | None, list[dict[str, Any]]]:
    """Run Tesseract; return (raw_text, avg_confidence, tokens).

    tokens are X-ordered {x, width, text, conf} so a downstream parser can map
    them to table columns by position instead of guessing from a flat string.
    """
    import pytesseract

    data = pytesseract.image_to_data(image, output_type=pytesseract.Output.DICT)
    tokens: list[dict[str, Any]] = []
    confidences = []
    for i, text in enumerate(data.get("text") or []):
        txt = (text or "").strip()
        if not txt:
            continue
        try:
            conf = float(data["conf"][i])
            token_conf = conf / 100.0 if conf >= 0 else None
        except (TypeError, ValueError):
            token_conf = None
        if token_conf is not None:
            confidences.append(token_conf)
        try:
            left = int(data["left"][i])
            width = int(data["width"][i])
        except (TypeError, ValueError, KeyError):
            left, width = 0, 0
        tokens.append({"x": left, "width": width, "text": txt, "conf": token_conf})

    tokens.sort(key=lambda token: token["x"])
    raw_text = " ".join(token["text"] for token in tokens)
    avg_conf = (sum(confidences) / len(confidences)) if confidences else None
    return raw_text, avg_conf, tokens


def main(argv: list[str] | None = None) -> int:
    started = time.perf_counter()
    args = build_parser().parse_args(argv)
    metadata_path = Path(args.metadata)
    output_path = Path(args.output)

    if not metadata_path.exists():
        print(f"Layout crop metadata not found: {metadata_path}", file=sys.stderr)
        return 2

    try:
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        print(f"Layout crop metadata is not readable: {exc}", file=sys.stderr)
        return 2

    calibration = metadata.get("layoutCalibration", {})
    if calibration.get("status") not in {"Calibrated", "Partial"}:
        print("Layout crop metadata is not calibrated or partially usable.", file=sys.stderr)
        return 2

    try:
        import pytesseract
        from PIL import Image
    except ImportError as exc:
        print(f"Tesseract crop OCR dependencies are unavailable: {exc}", file=sys.stderr)
        return 3

    selected = {v.strip() for v in args.zone_id_filter.split(",") if v.strip()}
    preprocessed_folder = Path(args.preprocessed_output_folder)
    if args.save_preprocessed_images:
        preprocessed_folder.mkdir(parents=True, exist_ok=True)

    zone_results: list[dict[str, Any]] = []
    warnings_list: list[str] = []
    errors_list: list[str] = []

    for zone in metadata.get("crops", metadata.get("zones", [])):
        if zone.get("usable") is False:
            continue
        zone_id = str(zone.get("zoneId") or "")
        if selected and zone_id not in selected:
            continue

        image_path = Path(str(zone.get("imagePath") or ""))
        zone_warnings: list[str] = []
        zone_errors: list[str] = []
        raw_text = ""
        avg_conf: float | None = None
        tokens: list[dict[str, Any]] = []
        preprocessed_path = ""

        if not image_path.exists():
            zone_errors.append(f"Crop image not found: {image_path}")
        else:
            try:
                with Image.open(image_path) as src:
                    image = src.convert("L")
                    if args.preprocess:
                        image = _preprocess_image(image, args)

                    if args.save_preprocessed_images:
                        safe = "".join(c if c.isalnum() or c in "._-" else "_" for c in zone_id)
                        saved = preprocessed_folder / f"{safe}.preprocessed.png"
                        image.save(saved)
                        preprocessed_path = str(saved)

                    raw_text, avg_conf, tokens = _ocr_zone(image)
                    if not raw_text:
                        zone_warnings.append("Tesseract returned no text for this crop.")
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
            "preprocessedImagePath": preprocessed_path,
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
        "engine": "TesseractLayoutCrops",
        "strategy": "TesseractLayoutCrops",
        "outputKind": "OcrRaw",
        "isFinalNormalizedJson": False,
        "createdAtUtc": datetime.now(timezone.utc).isoformat(),
        "originalFileName": metadata.get("originalFileName", ""),
        "sourcePdf": metadata.get("sourcePdf", ""),
        "documentHash": metadata.get("documentHash") or metadata.get("sourceHash", ""),
        "layoutMapVersion": metadata.get("layoutMapVersion", ""),
        "layoutMapName": metadata.get("layoutMapName", ""),
        "calibrationId": metadata.get("layoutCalibration", {}).get("calibrationId", ""),
        "renderWidth": metadata.get("renderedImageWidth"),
        "renderHeight": metadata.get("renderedImageHeight"),
        "dpi": metadata.get("renderDpi"),
        "layoutCropMetadataPath": str(metadata_path),
        "durationMs": int((time.perf_counter() - started) * 1000),
        "zones": zone_results,
        "warnings": warnings_list,
        "errors": errors_list,
    }
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(output_raw, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote Tesseract layout-crop raw OCR: {output_path}")

    # Produce evidence records if requested.
    evidence_output = args.evidence_output.strip() if args.evidence_output else ""
    if evidence_output:
        try:
            from .evidence_mapper import map_crop_ocr_to_evidence
            from .evidence_record import write as write_evidence
            envelope = map_crop_ocr_to_evidence(
                output_raw,
                document_hash=output_raw["documentHash"],
                render_width=output_raw.get("renderWidth"),
                render_height=output_raw.get("renderHeight"),
                dpi=output_raw.get("dpi"),
            )
            write_evidence(envelope, evidence_output)
            print(f"Wrote evidence records: {evidence_output}")
        except Exception as exc:
            print(f"Warning: could not write evidence records: {exc}", file=sys.stderr)

    return 0 if zone_results else 4


if __name__ == "__main__":
    raise SystemExit(main())
