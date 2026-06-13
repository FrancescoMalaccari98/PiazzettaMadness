"""evidence_record.py — shared output schema for all 4 OCR evidence channels.

Every OCR worker (TesseractFullPage, TesseractCrop, PaddleCrop, PaddleRow)
writes one evidence_records.json file per run.  The C# EvidenceRecordMapper
reads this file and converts it to ProcessingResult + OcrCandidate provenance.

sourceId constants
------------------
  ocr.tesseract.fullpage   — full-page Tesseract
  ocr.tesseract.crop       — Tesseract on calibrated zone/row crops
  ocr.paddle.crop          — PaddleOCR on calibrated zone crops
  ocr.paddle.row           — PaddleOCR on row crops

granularity constants
---------------------
  fullpage   — whole page OCR (lowest spatial precision)
  table      — single table crop
  row        — single player row crop
  cell       — single cell crop (future)
"""
from __future__ import annotations

import hashlib
import json
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


SCHEMA_VERSION = "1.0"
OUTPUT_KIND = "EvidenceRecords"

# sourceId registry
SOURCE_TESSERACT_FULLPAGE = "ocr.tesseract.fullpage"
SOURCE_TESSERACT_CROP = "ocr.tesseract.crop"
SOURCE_PADDLE_CROP = "ocr.paddle.crop"
SOURCE_PADDLE_ROW = "ocr.paddle.row"

# granularity registry
GRANULARITY_FULLPAGE = "fullpage"
GRANULARITY_TABLE = "table"
GRANULARITY_ROW = "row"
GRANULARITY_CELL = "cell"


def empty_record(
    *,
    field_id: str,
    entity_id: str = "",
    entity_type: str = "player",
    side: str | None = None,
    row_index: int | None = None,
    column_id: str | None = None,
    stat_key: str = "",
    scope: str = "Player",
    crop_id: str = "",
    zone_id: str = "",
    value_raw: str = "",
    value_normalized: Any = None,
    confidence_raw: float | None = None,
    confidence_normalized: float | None = None,
    mapper_id: str = "",
    warnings: list[str] | None = None,
    rect_normalized: dict[str, float] | None = None,
    rect_pixels: dict[str, int] | None = None,
) -> dict[str, Any]:
    """Return a single evidence record with all required fields."""
    return {
        "recordId": _record_id(entity_id, field_id, crop_id),
        "fieldId": field_id,
        "entityId": entity_id,
        "entityType": entity_type,
        "side": side,
        "rowIndex": row_index,
        "columnId": column_id,
        "statKey": stat_key,
        "scope": scope,
        "cropId": crop_id,
        "zoneId": zone_id,
        "valueRaw": value_raw,
        "valueNormalized": value_normalized,
        "confidenceRaw": confidence_raw,
        "confidenceNormalized": confidence_normalized,
        "mapperId": mapper_id,
        "warnings": warnings or [],
        "rectNormalized": rect_normalized,
        "rectPixels": rect_pixels,
    }


def make_envelope(
    *,
    source_id: str,
    engine: str,
    granularity: str,
    document_file_name: str = "",
    document_hash: str = "",
    page_index: int = 0,
    dpi: int | None = None,
    render_width: int | None = None,
    render_height: int | None = None,
    records: list[dict[str, Any]] | None = None,
) -> dict[str, Any]:
    """Return the top-level evidence_records.json envelope."""
    return {
        "schemaVersion": SCHEMA_VERSION,
        "outputKind": OUTPUT_KIND,
        "isFinalNormalizedJson": False,
        "createdAtUtc": datetime.now(timezone.utc).isoformat(),
        "sourceId": source_id,
        "engine": engine,
        "granularity": granularity,
        "documentFileName": document_file_name,
        "documentHash": document_hash,
        "pageIndex": page_index,
        "dpi": dpi,
        "renderWidth": render_width,
        "renderHeight": render_height,
        "records": records or [],
    }


def write(envelope: dict[str, Any], output_path: str | Path) -> Path:
    path = Path(output_path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(envelope, ensure_ascii=False, indent=2), encoding="utf-8")
    return path


def _record_id(entity_id: str, field_id: str, crop_id: str) -> str:
    raw = f"{entity_id}|{field_id}|{crop_id}"
    return hashlib.sha1(raw.encode()).hexdigest()[:12]
