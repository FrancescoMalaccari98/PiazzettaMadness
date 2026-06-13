from __future__ import annotations

from pathlib import Path
from typing import Dict, Tuple

import cv2
import fitz  # PyMuPDF
import numpy as np


def _pixmap_to_rgb_array(pix: fitz.Pixmap) -> np.ndarray:
    if pix.n == 4:
        pix = fitz.Pixmap(fitz.csRGB, pix)
    arr = np.frombuffer(pix.samples, dtype=np.uint8).reshape(pix.height, pix.width, pix.n)
    if pix.n == 1:
        arr = cv2.cvtColor(arr, cv2.COLOR_GRAY2RGB)
    elif pix.n == 3:
        pass
    else:
        arr = arr[:, :, :3]
    return arr


def load_first_page_gray(pdf_path: str | Path, *, fallback_scale: int = 4, use_native_images: bool = True) -> Tuple[np.ndarray, int, Dict[str, object]]:
    """Restituisce la prima pagina come immagine grayscale.

    I tabellini FIBA ricevuti sono composti da più immagini JPEG native
    impaginate una sotto l'altra. Quando possibile, le immagini vengono
    estratte e concatenate alla risoluzione originale: è molto più stabile
    dell'OCR su rendering PDF standard.
    """
    pdf_path = Path(pdf_path)
    doc = fitz.open(str(pdf_path))
    try:
        if doc.page_count < 1:
            raise ValueError("PDF senza pagine")
        page = doc[0]
        meta: Dict[str, object] = {"metodo": "render_pdf", "native_images": False}

        if use_native_images:
            infos = sorted(page.get_image_info(xrefs=True), key=lambda d: (float(d["bbox"][1]), float(d["bbox"][0])))
            # Pattern tipico: 7 slice, stessa larghezza nativa, bbox impilate verticalmente.
            if len(infos) >= 2:
                arrays = []
                widths = []
                ok = True
                for info in infos:
                    try:
                        pix = fitz.Pixmap(doc, int(info["xref"]))
                        arr = _pixmap_to_rgb_array(pix)
                        arrays.append(arr)
                        widths.append(arr.shape[1])
                    except Exception:
                        ok = False
                        break
                if ok and arrays and max(widths) == min(widths) and max(widths) >= 1500:
                    rgb = np.vstack(arrays)
                    gray = cv2.cvtColor(rgb, cv2.COLOR_RGB2GRAY)
                    meta.update({
                        "metodo": "native_image_slices",
                        "native_images": True,
                        "slice_count": len(arrays),
                        "dimensione": [int(gray.shape[1]), int(gray.shape[0])],
                    })
                    return gray, doc.page_count, meta

        matrix = fitz.Matrix(fallback_scale, fallback_scale)
        pix = page.get_pixmap(matrix=matrix, alpha=False)
        rgb = np.frombuffer(pix.samples, dtype=np.uint8).reshape(pix.height, pix.width, 3)
        gray = cv2.cvtColor(rgb, cv2.COLOR_RGB2GRAY)
        meta.update({"fallback_scale": fallback_scale, "dimensione": [int(gray.shape[1]), int(gray.shape[0])]})
        return gray, doc.page_count, meta
    finally:
        doc.close()
