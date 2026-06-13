from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import cv2
import pytesseract

from .image_loader import load_first_page_gray


def export_full_page_geometry(pdf_path: str, output_folder: str, document_hash: str, *, fallback_scale: int = 4) -> dict[str, Any]:
    folder = Path(output_folder)
    folder.mkdir(parents=True, exist_ok=True)
    # Geometry calibration needs a stable PDF-page coordinate frame. The
    # extraction pipeline may prefer native image slices for OCR quality, but
    # those slices are not a reliable page-layout basis.
    gray, page_count, metadata = load_first_page_gray(
        pdf_path,
        fallback_scale=fallback_scale,
        use_native_images=False,
    )
    rendered_path = folder / "full-page-rendered.png"
    overlay_path = folder / "full-page-word-overlay.png"
    boxes_path = folder / "full-page-word-boxes.json"
    cv2.imwrite(str(rendered_path), gray)
    rendered_gray = cv2.imread(str(rendered_path), cv2.IMREAD_GRAYSCALE)
    if rendered_gray is None:
        raise RuntimeError(f"Unable to reopen canonical full-page render: {rendered_path}")

    data = pytesseract.image_to_data(
        rendered_gray,
        lang="eng",
        config="--psm 6 --oem 3",
        output_type=pytesseract.Output.DICT,
        timeout=30,
    )
    words: list[dict[str, Any]] = []
    overlay = cv2.cvtColor(rendered_gray, cv2.COLOR_GRAY2BGR)
    for index, text in enumerate(data.get("text", [])):
        value = (text or "").strip()
        if not value:
            continue
        left = int(data["left"][index])
        top = int(data["top"][index])
        width = int(data["width"][index])
        height = int(data["height"][index])
        try:
            confidence = float(data["conf"][index])
        except (TypeError, ValueError):
            confidence = -1.0
        words.append(
            {
                "text": value,
                "x": left,
                "y": top,
                "width": width,
                "height": height,
                "confidence": confidence,
                "blockIndex": int(data["block_num"][index]),
                "paragraphIndex": int(data["par_num"][index]),
                "lineIndex": int(data["line_num"][index]),
                "wordIndex": int(data["word_num"][index]),
            }
        )
        cv2.rectangle(overlay, (left, top), (left + width, top + height), (0, 140, 255), 1)
    cv2.imwrite(str(overlay_path), overlay)

    artifact = {
        "schemaVersion": "1.0",
        "provider": "TesseractFullPage",
        "strategy": "TesseractFullPage",
        "outputKind": "FullPageOcrGeometry",
        "isFinalNormalizedJson": False,
        "createdAtUtc": datetime.now(timezone.utc).isoformat(),
        "sourcePdf": str(Path(pdf_path)),
        "documentHash": document_hash,
        "pageIndex": 0,
        "pageCount": page_count,
        "renderMethod": metadata.get("metodo", ""),
        "renderScale": int(metadata.get("fallback_scale") or fallback_scale),
        "renderDpi": int((metadata.get("fallback_scale") or fallback_scale) * 72),
        "coordinateBasis": "full-page-rendered.png",
        "coordinateSystem": "pixel-top-left-origin",
        "renderedImagePath": str(rendered_path),
        "wordOverlayPath": str(overlay_path),
        "renderedImageWidth": int(rendered_gray.shape[1]),
        "renderedImageHeight": int(rendered_gray.shape[0]),
        "words": words,
    }
    boxes_path.write_text(json.dumps(artifact, ensure_ascii=False, indent=2), encoding="utf-8")
    return artifact
