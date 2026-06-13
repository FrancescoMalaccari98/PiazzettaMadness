from __future__ import annotations

import json
import logging
from pathlib import Path
from typing import Any, Dict, List, Optional

import cv2
import numpy as np

from .image_loader import load_first_page_gray
from .layout import (
    build_line_masks,
    detect_comparative_boxes,
    detect_player_table_boxes,
    detect_table_boxes,
    remove_grid_lines,
)
from .normalizers import file_phase_from_name
from .parser import (
    add_note,
    add_warning,
    apply_reliability,
    finalize_result_from_tables,
    parse_comparatives,
    parse_header_regions,
    parse_player_table,
)
from .schema import document_template

LOGGER = logging.getLogger(__name__)


class FibaPdfConverter:
    def __init__(self, *, fallback_scale: int = 4, use_native_images: bool = True, debug_dir: Optional[str | Path] = None) -> None:
        self.fallback_scale = fallback_scale
        self.use_native_images = use_native_images
        self.debug_dir = Path(debug_dir) if debug_dir else None

    def convert_pdf(self, pdf_path: str | Path) -> Dict[str, Any]:
        pdf_path = Path(pdf_path)
        doc_json = document_template()
        doc_json["metadata"]["nome_file"] = pdf_path.name
        doc_json["metadata"]["fase_partita"] = file_phase_from_name(pdf_path.name)
        LOGGER.info("Conversione PDF: %s", pdf_path)

        try:
            gray, page_count, meta = load_first_page_gray(pdf_path, fallback_scale=self.fallback_scale, use_native_images=self.use_native_images)
            doc_json["metadata"]["numero_pagine"] = page_count
            add_note(doc_json, f"Immagine pagina acquisita con metodo: {meta.get('metodo')}; dimensione={meta.get('dimensione')}.")
            if page_count != 1:
                add_warning(doc_json, "metadata.numero_pagine", f"Il PDF contiene {page_count} pagine; viene convertita solo la prima.", affidabilita="media")
        except Exception as exc:
            add_warning(doc_json, "pdf", f"Errore rendering/estrazione PDF: {exc}", affidabilita="bassa")
            apply_reliability(doc_json)
            return doc_json

        binary, hmask, vmask = build_line_masks(gray)
        clean_page = remove_grid_lines(gray, hmask, vmask, dilation=3)

        if self.debug_dir:
            self.debug_dir.mkdir(parents=True, exist_ok=True)
            stem = pdf_path.stem
            cv2.imwrite(str(self.debug_dir / f"{stem}_gray.png"), gray)
            cv2.imwrite(str(self.debug_dir / f"{stem}_clean.png"), clean_page)
            cv2.imwrite(str(self.debug_dir / f"{stem}_lines.png"), cv2.add(hmask, vmask))

        try:
            parse_header_regions(doc_json, clean_page)
        except Exception as exc:
            add_warning(doc_json, "partita", f"Errore OCR/parsing header: {exc}", affidabilita="media")
            LOGGER.exception("Errore parsing header")

        boxes = detect_table_boxes(hmask, vmask, gray.shape)
        player_boxes = detect_player_table_boxes(boxes, gray.shape)
        if len(player_boxes) < 2:
            add_warning(doc_json, "squadre", f"Rilevate {len(player_boxes)} tabelle giocatori invece di 2.", affidabilita="bassa")
        for idx, table_box in enumerate(player_boxes[:2]):
            try:
                parse_player_table(doc_json, clean_page, binary, table_box, idx)
            except Exception as exc:
                add_warning(doc_json, f"squadre.{idx}", f"Errore parsing tabella giocatori: {exc}", affidabilita="bassa")
                LOGGER.exception("Errore parsing tabella %s", idx)

        comp_boxes = detect_comparative_boxes(boxes, gray.shape)
        if len(comp_boxes) < 2:
            add_warning(doc_json, "statistiche_comparative", f"Rilevate {len(comp_boxes)} tabelle comparative invece di 2.", affidabilita="media")
        else:
            try:
                parse_comparatives(doc_json, clean_page, binary, comp_boxes)
            except Exception as exc:
                add_warning(doc_json, "statistiche_comparative", f"Errore parsing statistiche comparative: {exc}", affidabilita="media")
                LOGGER.exception("Errore parsing comparative")

        finalize_result_from_tables(doc_json)
        apply_reliability(doc_json)
        return doc_json

    def save_json(self, data: Dict[str, Any], output_path: str | Path) -> None:
        output_path = Path(output_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        with output_path.open("w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
            f.write("\n")


def iter_input_pdfs(input_path: str | Path) -> List[Path]:
    p = Path(input_path)
    if p.is_file():
        return [p]
    if p.is_dir():
        return sorted([x for x in p.iterdir() if x.suffix.lower() == ".pdf"])
    raise FileNotFoundError(f"Input non trovato: {p}")
