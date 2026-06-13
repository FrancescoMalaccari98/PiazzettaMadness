from __future__ import annotations

import argparse
import json
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

from .image_preparation import describe_input, prepare_images


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Prepare reusable layout-map crops for OCR providers. No model inference is performed.")
    parser.add_argument("--pdf", default="", help="Input PDF path. Alias for --input.")
    parser.add_argument("--input", default="", help="Input PDF or image path.")
    parser.add_argument("--layout-map", required=True, help="Layout map JSON path.")
    parser.add_argument("--output-folder", required=True, help="Document-specific crop output folder.")
    parser.add_argument("--image-output-folder", default="", help="Optional folder for rendered page images.")
    parser.add_argument("--input-mode", default="Crops", choices=["FullPage", "Crops", "Both"], help="Artifact mode.")
    parser.add_argument("--max-pages", type=int, default=1, help="Maximum PDF pages to render.")
    parser.add_argument("--document-hash", default="", help="Source document hash.")
    parser.add_argument("--original-file-name", default="", help="Original input file name.")
    return parser


def main(argv: list[str] | None = None) -> int:
    started = time.perf_counter()
    args = build_parser().parse_args(argv)
    input_value = args.input or args.pdf
    if not input_value:
        print("Input path is required. Use --pdf or --input.", file=sys.stderr)
        return 2

    input_path = Path(input_value)
    if not input_path.exists():
        print(f"Input path does not exist: {input_path}", file=sys.stderr)
        return 2

    output_folder = Path(args.output_folder)
    image_output_folder = Path(args.image_output_folder) if args.image_output_folder else output_folder / "pages"
    output_folder.mkdir(parents=True, exist_ok=True)
    image_output_folder.mkdir(parents=True, exist_ok=True)

    try:
        prepared = prepare_images(
            input_path=str(input_path),
            input_mode=args.input_mode,
            image_output_folder=str(image_output_folder),
            zone_output_folder=str(output_folder),
            layout_map_path=args.layout_map,
            max_pages=args.max_pages,
        )
    except Exception as exc:
        print(f"Crop preparation failed: {exc}", file=sys.stderr)
        return 3

    pages = prepared.get("pages", [])
    first_page = pages[0] if pages else {}
    metadata = {
        "schemaVersion": "2.0",
        "engine": "LayoutCropPreparer",
        "outputKind": "LayoutCropPreparation",
        "isFinalNormalizedJson": False,
        "createdAtUtc": datetime.now(timezone.utc).isoformat(),
        "input": describe_input(str(input_path)),
        "documentFileName": args.original_file_name or input_path.name,
        "documentHash": args.document_hash,
        "sourcePdf": str(input_path),
        "sourceFileName": input_path.name,
        "sourceHash": args.document_hash,
        "originalFileName": args.original_file_name or input_path.name,
        "pageIndex": 0,
        "renderDpi": prepared.get("renderDpi"),
        "renderScale": prepared.get("renderScale"),
        "renderedImageWidth": first_page.get("width", 0),
        "renderedImageHeight": first_page.get("height", 0),
        "layoutMapVersion": prepared.get("layoutMapVersion", ""),
        "layoutMapName": prepared.get("layoutMapName", ""),
        "layoutMapPath": args.layout_map,
        "outputFolder": str(output_folder),
        "imageOutputFolder": str(image_output_folder),
        "inputMode": args.input_mode,
        "durationMs": int((time.perf_counter() - started) * 1000),
        "pages": pages,
        "crops": prepared.get("crops", []),
        "artifacts": prepared.get("artifacts", []),
        "warnings": prepared.get("warnings", []),
        "errors": [],
    }
    metadata_path = output_folder / "crop-metadata.json"
    metadata_path.write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote reusable layout crop metadata: {metadata_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
