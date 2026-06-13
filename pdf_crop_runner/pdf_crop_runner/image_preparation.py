from __future__ import annotations

import json
import shutil
import struct
from dataclasses import dataclass
from pathlib import Path
from typing import Any

@dataclass(frozen=True)
class LayoutZone:
    zone_id: str
    zone_type: str
    page_number: int
    x: float
    y: float
    width: float
    height: float
    coordinate_unit: str
    field_ids: list[str]
    stat_keys: list[str]
    source: str
    parent_zone_id: str
    derive_mode: str
    semantic_role: str
    file_name: str
    tier: str
    crop_type: str
    side: str
    expected_content_type: str
    row_index: int | None = None
    column_id: str | None = None


def describe_input(input_path: str) -> dict[str, str | bool]:
    path = Path(input_path)
    return {
        "path": str(path),
        "exists": path.exists(),
        "suffix": path.suffix.lower(),
    }


def prepare_images(
    *,
    input_path: str,
    input_mode: str,
    image_output_folder: str,
    zone_output_folder: str,
    layout_map_path: str | None,
    max_pages: int = 1,
) -> dict[str, Any]:
    path = Path(input_path)
    suffix = path.suffix.lower()
    if suffix in {".png", ".jpg", ".jpeg"}:
        return _prepare_image_input(path, input_mode, image_output_folder, zone_output_folder, layout_map_path)

    if suffix == ".pdf":
        return _prepare_pdf_input(path, input_mode, image_output_folder, zone_output_folder, layout_map_path, max_pages)

    raise ValueError(f"Unsupported crop input type: {suffix or '<none>'}. Expected PDF, PNG, JPG, or JPEG.")


def _prepare_image_input(
    path: Path,
    input_mode: str,
    image_output_folder: str,
    zone_output_folder: str,
    layout_map_path: str | None,
) -> dict[str, Any]:
    image_dir = Path(image_output_folder)
    zone_dir = Path(zone_output_folder)
    image_dir.mkdir(parents=True, exist_ok=True)
    zone_dir.mkdir(parents=True, exist_ok=True)

    width, height = _read_image_size(path)
    pages: list[dict[str, Any]] = []
    crops: list[dict[str, Any]] = []
    artifacts: list[dict[str, Any]] = []
    warnings: list[str] = []

    source_image = path
    page_number = 1
    pages.append(_page_record(source_image, page_number, width, height, "SourceImage"))
    if input_mode in {"FullPage", "Both"}:
        page_path = image_dir / f"{path.stem}.p1.full{path.suffix.lower()}"
        shutil.copyfile(path, page_path)
        artifacts.append(_artifact_record("PageImage", page_path, page_number, width, height))

    if input_mode in {"Crops", "Both"}:
        layout_zones, layout_warnings = _load_layout_zones(layout_map_path)
        warnings.extend(layout_warnings)
        zones_for_page = [zone for zone in layout_zones if zone.page_number == page_number]
        crop_result = _crop_zones_from_image(
            source_image=source_image,
            zones=zones_for_page,
            zone_dir=zone_dir,
            source_width=width,
            source_height=height,
        )
        crops.extend(crop_result["crops"])
        artifacts.extend(crop_result["artifacts"])
        warnings.extend(crop_result["warnings"])

    return {
        "pages": pages,
        "crops": crops,
        "artifacts": artifacts,
        "warnings": warnings,
        "renderDpi": None,
        "renderScale": 1,
        **_layout_map_identity(layout_map_path),
    }


def _prepare_pdf_input(
    path: Path,
    input_mode: str,
    image_output_folder: str,
    zone_output_folder: str,
    layout_map_path: str | None,
    max_pages: int,
) -> dict[str, Any]:
    try:
        import fitz  # type: ignore
    except ImportError as exc:
        raise RuntimeError("PDF rendering requires PyMuPDF. Install the runner with PDF support before using PDF inputs.") from exc

    image_dir = Path(image_output_folder)
    zone_dir = Path(zone_output_folder)
    image_dir.mkdir(parents=True, exist_ok=True)
    zone_dir.mkdir(parents=True, exist_ok=True)

    pages: list[dict[str, Any]] = []
    crops: list[dict[str, Any]] = []
    artifacts: list[dict[str, Any]] = []
    warnings: list[str] = []
    layout_zones, layout_warnings = _load_layout_zones(layout_map_path)
    warnings.extend(layout_warnings)

    with fitz.open(path) as document:
        page_count = min(max_pages, len(document))
        for index in range(page_count):
            page = document[index]
            pixmap = page.get_pixmap(matrix=fitz.Matrix(2, 2), alpha=False)
            page_number = index + 1
            rendered_path = image_dir / f"{path.stem}.p{page_number}.rendered.png"
            pixmap.save(rendered_path)
            width, height = pixmap.width, pixmap.height

            pages.append(_page_record(rendered_path, page_number, width, height, "RenderedPdfPage"))
            if input_mode in {"FullPage", "Both"}:
                artifacts.append(_artifact_record("PageImage", rendered_path, page_number, width, height))
            else:
                artifacts.append(_artifact_record("RenderedPageForCropping", rendered_path, page_number, width, height))

            if input_mode in {"Crops", "Both"}:
                zones_for_page = [zone for zone in layout_zones if zone.page_number == page_number]
                crop_result = _crop_zones_from_image(
                    source_image=rendered_path,
                    zones=zones_for_page,
                    zone_dir=zone_dir,
                    source_width=width,
                    source_height=height,
                )
                crops.extend(crop_result["crops"])
                artifacts.extend(crop_result["artifacts"])
                warnings.extend(crop_result["warnings"])

    return {
        "pages": pages,
        "crops": crops,
        "artifacts": artifacts,
        "warnings": warnings,
        "renderDpi": 144,
        "renderScale": 2,
        **_layout_map_identity(layout_map_path),
    }


def _crop_zones_from_image(
    *,
    source_image: Path,
    zones: list[LayoutZone],
    zone_dir: Path,
    source_width: int,
    source_height: int,
) -> dict[str, Any]:
    if not zones:
        return {"crops": [], "artifacts": [], "warnings": ["No layout-map zones matched the prepared page; no crop artifacts were created."]}

    try:
        from PIL import Image
    except ImportError:
        Image = None  # type: ignore[assignment]

    zone_dir.mkdir(parents=True, exist_ok=True)
    cropped_zones: list[dict[str, Any]] = []
    artifacts: list[dict[str, Any]] = []
    warnings: list[str] = []

    if Image is None:
        warnings.append("Pillow is not available; only full-image layout zones can be copied without pixel cropping.")

    created_by_zone_id: dict[str, dict[str, Any]] = {}
    for zone in zones:
        if zone.source == "derived-from-zone":
            continue
        result = _try_create_crop(
            image_module=Image,
            zone=zone,
            source_image=source_image,
            source_width=source_width,
            source_height=source_height,
            zone_dir=zone_dir,
        )
        if result["warning"]:
            warnings.append(result["warning"])
            continue
        cropped_zones.append(result["zone"])
        artifacts.append(result["artifact"])
        created_by_zone_id[zone.zone_id] = result["zone"]

    for zone in zones:
        if zone.source != "derived-from-zone":
            continue
        parent = created_by_zone_id.get(zone.parent_zone_id)
        if not parent:
            warnings.append(f"Derived layout zone '{zone.zone_id}' skipped because parent zone '{zone.parent_zone_id}' was not created.")
            continue
        result = _try_create_crop(
            image_module=Image,
            zone=zone,
            source_image=Path(parent["imagePath"]),
            source_width=int(parent["pixelBox"]["width"]),
            source_height=int(parent["pixelBox"]["height"]),
            zone_dir=zone_dir,
        )
        if result["warning"]:
            warnings.append(result["warning"])
            continue
        cropped_zones.append(result["zone"])
        artifacts.append(result["artifact"])
        created_by_zone_id[zone.zone_id] = result["zone"]

    return {"crops": cropped_zones, "artifacts": artifacts, "warnings": warnings}


def _try_create_crop(
    *,
    image_module: Any,
    zone: LayoutZone,
    source_image: Path,
    source_width: int,
    source_height: int,
    zone_dir: Path,
) -> dict[str, Any]:
    bbox = _zone_to_bbox(zone, source_width, source_height)
    if bbox is None:
        return {"zone": None, "artifact": None, "warning": f"Layout zone '{zone.zone_id}' has invalid crop coordinates and was skipped."}

    left, top, right, bottom = bbox
    crop_width = right - left
    crop_height = bottom - top
    zone_file = zone_dir / zone.file_name
    if image_module is None:
        if (left, top, right, bottom) != (0, 0, source_width, source_height) or source_image.suffix.lower() != ".png":
            return {"zone": None, "artifact": None, "warning": f"Layout zone '{zone.zone_id}' requires Pillow for real cropping and was skipped."}
        shutil.copyfile(source_image, zone_file)
    else:
        with image_module.open(source_image) as image:
            crop = image.crop((left, top, right, bottom))
            crop.save(zone_file)

    return {
        "zone": _zone_record(zone, zone_file, left, top, crop_width, crop_height),
        "artifact": _artifact_record("ZoneCrop", zone_file, zone.page_number, crop_width, crop_height, zone.zone_id),
        "warning": "",
    }


def _zone_record(zone: LayoutZone, zone_file: Path, left: int, top: int, crop_width: int, crop_height: int) -> dict[str, Any]:
    return {
        "cropId": zone.zone_id,
        "fileName": zone.file_name,
        "zoneId": zone.zone_id,
        "zoneType": zone.zone_type,
        "tier": zone.tier,
        "cropType": zone.crop_type,
        "side": zone.side or None,
        "expectedContentType": zone.expected_content_type,
        "pageIndex": zone.page_number - 1,
        "pageNumber": zone.page_number,
        "x": zone.x,
        "y": zone.y,
        "width": zone.width,
        "height": zone.height,
        "coordinateUnit": zone.coordinate_unit,
        "rectNormalized": {
            "x": zone.x,
            "y": zone.y,
            "width": zone.width,
            "height": zone.height,
        },
        "rectPixels": {
            "x": left,
            "y": top,
            "width": crop_width,
            "height": crop_height,
        },
        "pixelBox": {
            "x": left,
            "y": top,
            "width": crop_width,
            "height": crop_height,
        },
        "pixelX": left,
        "pixelY": top,
        "pixelWidth": crop_width,
        "pixelHeight": crop_height,
        "imagePath": str(zone_file),
        "fieldIds": zone.field_ids,
        "statKeys": zone.stat_keys,
        "source": zone.source,
        "parentZoneId": zone.parent_zone_id,
        "deriveMode": zone.derive_mode,
        "semanticRole": zone.semantic_role,
        "rowIndex": zone.row_index,
        "columnId": zone.column_id,
        "isPlaceholder": False,
    }


def _load_layout_zones(layout_map_path: str | None) -> tuple[list[LayoutZone], list[str]]:
    if not layout_map_path:
        return [], ["Layout map path was not provided; no crop zones were created."]

    path = Path(layout_map_path)
    if not path.exists():
        return [], [f"Layout map not found: {path}; no crop zones were created."]

    try:
        layout = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        return [], [f"Layout map could not be read: {path}; {exc}"]

    page_defaults = layout.get("page", {})
    default_unit = _canonical_coordinate_unit(
        str(layout.get("coordinateUnit") or page_defaults.get("coordinateUnit") or page_defaults.get("coordinateSystem") or "normalized")
    )

    zones: list[LayoutZone] = []
    warnings: list[str] = []
    for index, item in enumerate(layout.get("zones", []), start=1):
        try:
            zone = _parse_layout_zone(item, default_unit, index)
        except (TypeError, ValueError) as exc:
            warnings.append(f"Layout zone '{item.get('zoneId') or item.get('cropId') or '<missing>'}' skipped: {exc}")
            continue
        zones.append(zone)

    if not zones:
        warnings.append(f"Layout map contains no usable zones: {path}")

    return zones, warnings


def _parse_layout_zone(item: dict[str, Any], default_unit: str, index: int) -> LayoutZone:
    zone_id = str(item.get("zoneId") or item.get("cropId") or "").strip()
    if not zone_id:
        raise ValueError("zoneId or cropId is required")

    box = item.get("rectNormalized") if isinstance(item.get("rectNormalized"), dict) else {}
    if not box:
        box = item.get("box") if isinstance(item.get("box"), dict) else {}
    x = _number(item.get("x", item.get("relativeX", box.get("x"))))
    y = _number(item.get("y", item.get("relativeY", box.get("y"))))
    width = _number(item.get("width", item.get("relativeWidth", box.get("width"))))
    height = _number(item.get("height", item.get("relativeHeight", box.get("height"))))
    if width <= 0 or height <= 0:
        raise ValueError("width and height must be greater than zero")

    fields = item.get("fields") if isinstance(item.get("fields"), list) else []
    expected_field_ids = item.get("expectedFieldIds") if isinstance(item.get("expectedFieldIds"), list) else []
    expected_stat_keys = item.get("expectedStatKeys") if isinstance(item.get("expectedStatKeys"), list) else []
    field_ids = [str(value) for value in expected_field_ids if value]
    stat_keys = [str(value) for value in expected_stat_keys if value]
    for field in fields:
        if isinstance(field, dict):
            if field.get("fieldId"):
                field_ids.append(str(field["fieldId"]))
            if field.get("statKey"):
                stat_keys.append(str(field["statKey"]))

    raw_row_index = item.get("rowIndex")
    row_index = int(raw_row_index) if raw_row_index is not None else None
    raw_col_id = item.get("columnId")
    column_id = str(raw_col_id) if raw_col_id is not None else None

    return LayoutZone(
        zone_id=zone_id,
        zone_type=str(item.get("zoneType") or zone_id),
        page_number=int(item.get("pageNumber") or 1),
        x=x,
        y=y,
        width=width,
        height=height,
        coordinate_unit=_canonical_coordinate_unit(str(item.get("coordinateUnit") or default_unit)),
        field_ids=_dedupe(field_ids),
        stat_keys=_dedupe(stat_keys),
        source=str(item.get("source") or "layout-map"),
        parent_zone_id=str(item.get("parentZoneId") or ""),
        derive_mode=str(item.get("deriveMode") or ""),
        semantic_role=str(item.get("semanticRole") or item.get("zoneType") or zone_id),
        file_name=_crop_file_name(str(item.get("fileName") or f"{index:03d}-{zone_id}.png")),
        tier=str(item.get("tier") or "semantic"),
        crop_type=str(item.get("cropType") or item.get("zoneType") or zone_id),
        side=str(item.get("side") or ""),
        expected_content_type=str(item.get("expectedContentType") or item.get("zoneType") or zone_id),
        row_index=row_index,
        column_id=column_id,
    )


def _zone_to_bbox(zone: LayoutZone, image_width: int, image_height: int) -> tuple[int, int, int, int] | None:
    if zone.coordinate_unit == "pixel":
        left = round(zone.x)
        top = round(zone.y)
        right = round(zone.x + zone.width)
        bottom = round(zone.y + zone.height)
    else:
        left = round(zone.x * image_width)
        top = round(zone.y * image_height)
        right = round((zone.x + zone.width) * image_width)
        bottom = round((zone.y + zone.height) * image_height)

    left = max(0, min(left, image_width))
    top = max(0, min(top, image_height))
    right = max(0, min(right, image_width))
    bottom = max(0, min(bottom, image_height))
    if right <= left or bottom <= top:
        return None
    return left, top, right, bottom


def _read_image_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as stream:
        header = stream.read(32)

    if header.startswith(b"\x89PNG\r\n\x1a\n"):
        return struct.unpack(">II", header[16:24])

    if header.startswith(b"\xff\xd8"):
        return _read_jpeg_size(path)

    return (0, 0)


def _read_jpeg_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as stream:
        stream.read(2)
        while True:
            marker_start = stream.read(1)
            if not marker_start:
                break
            if marker_start != b"\xff":
                continue
            marker = stream.read(1)
            while marker == b"\xff":
                marker = stream.read(1)
            if marker in {b"\xc0", b"\xc1", b"\xc2", b"\xc3"}:
                stream.read(3)
                height, width = struct.unpack(">HH", stream.read(4))
                return (width, height)
            length_bytes = stream.read(2)
            if len(length_bytes) != 2:
                break
            length = struct.unpack(">H", length_bytes)[0]
            stream.seek(length - 2, 1)
    return (0, 0)


def _page_record(path: Path, page_number: int, width: int, height: int, source: str) -> dict[str, Any]:
    return {
        "pageNumber": page_number,
        "imagePath": str(path),
        "width": width,
        "height": height,
        "source": source,
    }


def _artifact_record(kind: str, path: Path, page_number: int, width: int, height: int, zone_id: str | None = None) -> dict[str, Any]:
    return {
        "artifactType": kind,
        "path": str(path),
        "pageNumber": page_number,
        "width": width,
        "height": height,
        "zoneId": zone_id or "",
    }


def _canonical_coordinate_unit(value: str) -> str:
    lowered = value.strip().lower()
    if lowered in {"pixel", "pixels", "px"}:
        return "pixel"
    if lowered in {"relative", "normalized", "normalised", "0..1"}:
        return "normalized"
    if lowered in {"relative-to-parent", "parent", "parent-relative"}:
        return "relative-to-parent"
    return "normalized"


def _number(value: Any) -> float:
    if value is None:
        raise ValueError("coordinate value is required")
    return float(value)


def _dedupe(values: list[str]) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for value in values:
        if value not in seen:
            result.append(value)
            seen.add(value)
    return result


def _safe_zone_id(zone_id: str) -> str:
    return "".join(ch if ch.isalnum() or ch in "._-" else "_" for ch in zone_id)


def _crop_file_name(value: str) -> str:
    safe = _safe_zone_id(Path(value).name)
    return safe if safe.lower().endswith(".png") else f"{safe}.png"


def _layout_map_identity(layout_map_path: str | None) -> dict[str, str]:
    if not layout_map_path:
        return {"layoutMapVersion": "", "layoutMapName": ""}
    try:
        layout = json.loads(Path(layout_map_path).read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return {"layoutMapVersion": "", "layoutMapName": ""}
    return {
        "layoutMapVersion": str(layout.get("schemaVersion") or ""),
        "layoutMapName": str(layout.get("name") or ""),
    }
