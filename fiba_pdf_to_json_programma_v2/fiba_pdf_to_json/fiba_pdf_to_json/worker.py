from __future__ import annotations

import argparse
import logging
import os

from .converter import FibaPdfConverter
from .geometry import export_full_page_geometry


def main() -> int:
    p = argparse.ArgumentParser(description="Worker interno per conversione isolata di un PDF FIBA.")
    p.add_argument("pdf")
    p.add_argument("output")
    p.add_argument("--fallback-scale", type=int, default=4)
    p.add_argument("--no-native-images", action="store_true")
    p.add_argument("--debug-dir", default=None)
    p.add_argument("--verbose", action="store_true")
    p.add_argument("--layout-debug-dir", default=None)
    p.add_argument("--document-hash", default="")
    p.add_argument("--evidence-output", default="", help="Optional path for evidence_records.json (ocr.tesseract.fullpage).")
    p.add_argument("--roster-json", default="", help="Optional path to the canonical match roster JSON (DB). Enables the dynamic roster used as parsing support.")
    args = p.parse_args()
    logging.basicConfig(level=logging.DEBUG if args.verbose else logging.INFO, format="%(levelname)s %(name)s: %(message)s")
    if args.roster_json.strip():
        from .roster_context import load_and_activate
        load_and_activate(args.roster_json.strip())
    converter = FibaPdfConverter(
        fallback_scale=args.fallback_scale,
        use_native_images=not args.no_native_images,
        debug_dir=args.debug_dir,
    )
    data = converter.convert_pdf(args.pdf)
    converter.save_json(data, args.output)
    if args.layout_debug_dir:
        export_full_page_geometry(args.pdf, args.layout_debug_dir, args.document_hash, fallback_scale=args.fallback_scale)
    if args.evidence_output.strip():
        _write_evidence(data, args.document_hash, args.evidence_output.strip())
    logging.shutdown()
    os._exit(0)


def _write_evidence(data: dict, document_hash: str, evidence_output: str) -> None:
    """Emit the shared evidence_records.json for the full-page channel.

    Separate from the raw JSON: the raw output is unchanged; this only adds the
    normalized evidence view consumed by the reconciler.
    """
    try:
        from .evidence_mapper import map_fullpage_to_evidence
        from .evidence_record import write as write_evidence
        envelope = map_fullpage_to_evidence(data, document_hash=document_hash)
        write_evidence(envelope, evidence_output)
        logging.getLogger(__name__).info("Wrote evidence records: %s", evidence_output)
    except Exception as exc:  # evidence is auxiliary: never fail the raw conversion
        logging.getLogger(__name__).warning("Could not write evidence records: %s", exc)


if __name__ == "__main__":
    raise SystemExit(main())
