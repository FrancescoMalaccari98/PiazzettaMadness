from __future__ import annotations

from dataclasses import dataclass
import os
from typing import List, Optional, Sequence, Tuple

import cv2
import numpy as np
import pytesseract

os.environ.setdefault("OMP_THREAD_LIMIT", "1")


@dataclass
class OcrCell:
    text: str
    conf: Optional[float]


def preprocess_for_ocr(image: np.ndarray, *, upscale: int = 4, pad: int = 20, threshold: bool = True, trim: bool = True) -> np.ndarray:
    img = image.copy()
    if trim and img.size:
        ys, xs = np.where(img < 230)
        if len(xs) > 0 and len(ys) > 0:
            m = 4
            x1, x2 = max(0, int(xs.min()) - m), min(img.shape[1], int(xs.max()) + m + 1)
            y1, y2 = max(0, int(ys.min()) - m), min(img.shape[0], int(ys.max()) + m + 1)
            img = img[y1:y2, x1:x2]
    if upscale and upscale != 1:
        img = cv2.resize(img, None, fx=upscale, fy=upscale, interpolation=cv2.INTER_CUBIC)
    blur = cv2.GaussianBlur(img, (0, 0), 1)
    img = cv2.addWeighted(img, 1.55, blur, -0.55, 0)
    if threshold:
        img = cv2.threshold(img, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)[1]
    if pad:
        img = cv2.copyMakeBorder(img, pad, pad, pad, pad, cv2.BORDER_CONSTANT, value=255)
    return img


def whitelist_for_kind(kind: str) -> Optional[str]:
    return {
        "ratio": "0123456789/",
        "pct": "0123456789,.%/",
        "min": "0123456789:.NE",
        "small_int": "0123456789",
        "signed_int": "0123456789",
        "number": "0123456789*",
        "area": "0123456789(),./%+-:",
        "text": None,
    }.get(kind)


def ocr_text(image: np.ndarray, *, lang: str = "eng", psm: int = 6, whitelist: Optional[str] = None, upscale: int = 3, timeout: int = 10) -> str:
    img = preprocess_for_ocr(image, upscale=upscale, pad=20, threshold=True)
    cfg = f"--psm {psm} --oem 3 -c preserve_interword_spaces=1 -c load_system_dawg=0 -c load_freq_dawg=0"
    if whitelist:
        cfg += f" -c tessedit_char_whitelist={whitelist}"
    return pytesseract.image_to_string(img, lang=lang, config=cfg, timeout=timeout).strip()


def ocr_cell(image: np.ndarray, *, lang: str = "eng", kind: str = "text", psm: int = 7, timeout: int = 8) -> OcrCell:
    whitelist = whitelist_for_kind(kind)
    img = preprocess_for_ocr(image, upscale=4, pad=24, threshold=True)
    cfg = f"--psm {psm} --oem 3 -c preserve_interword_spaces=1 -c load_system_dawg=0 -c load_freq_dawg=0"
    if whitelist:
        cfg += f" -c tessedit_char_whitelist={whitelist}"
    try:
        data = pytesseract.image_to_data(img, lang=lang, config=cfg, output_type=pytesseract.Output.DICT, timeout=timeout)
    except Exception:
        try:
            return OcrCell(ocr_text(image, lang=lang, psm=psm, whitelist=whitelist, upscale=6, timeout=4), None)
        except Exception:
            return OcrCell("", None)
    parts = []
    confs = []
    for i, t in enumerate(data.get("text", [])):
        txt = (t or "").strip()
        if not txt:
            continue
        parts.append((data["left"][i], txt))
        try:
            c = float(data["conf"][i])
            if c >= 0:
                confs.append(c)
        except Exception:
            pass
    text = " ".join(t for _, t in sorted(parts)).strip()
    return OcrCell(text=text, conf=(sum(confs) / len(confs) if confs else None))


def detect_minus_sign(image: np.ndarray) -> bool:
    if image.size == 0:
        return False
    img = image.copy()
    binary = (img < 190).astype(np.uint8) * 255
    num_labels, labels, stats, _ = cv2.connectedComponentsWithStats(binary, connectivity=8)
    h, w = img.shape[:2]
    for lab in range(1, num_labels):
        x, y, bw, bh, area = stats[lab]
        if area < 3:
            continue
        if bh <= max(4, h * 0.18) and bw >= max(4, bh * 2.0) and x < w * 0.55:
            return True
    return False


def ocr_column_cells(cells: Sequence[np.ndarray], *, lang: str = "eng", kind: str = "text", upscale: int = 4, timeout: int = 6) -> List[OcrCell]:
    """OCR di una colonna intera, poi riassegnazione alle righe.

    Rispetto all'OCR cella-per-cella è più stabile perché Tesseract vede una
    colonna coerente. Usiamo sempre TSV (`image_to_data`) invece di `makebox`:
    su questi PDF `makebox` è più fragile e può bloccarsi su alcune colonne
    numeriche molto piccole.
    """
    if not cells:
        return []
    prepared = [preprocess_for_ocr(c, upscale=upscale, pad=22, threshold=True) for c in cells]
    max_w = max(im.shape[1] for im in prepared)
    gap = 34
    pieces: List[np.ndarray] = []
    bounds: List[Tuple[int, int]] = []
    ycur = 0
    for im in prepared:
        if im.shape[1] < max_w:
            im = cv2.copyMakeBorder(im, 0, 0, 0, max_w - im.shape[1], cv2.BORDER_CONSTANT, value=255)
        pieces.append(im)
        bounds.append((ycur, ycur + im.shape[0]))
        ycur += im.shape[0]
        pieces.append(np.full((gap, max_w), 255, dtype=np.uint8))
        ycur += gap
    sheet = np.vstack(pieces)
    whitelist = whitelist_for_kind(kind)

    cfg = "--psm 6 --oem 3 -c preserve_interword_spaces=1 -c load_system_dawg=0 -c load_freq_dawg=0"
    if whitelist:
        cfg += f" -c tessedit_char_whitelist={whitelist}"
    try:
        data = pytesseract.image_to_data(sheet, lang=lang, config=cfg, output_type=pytesseract.Output.DICT, timeout=timeout)
    except Exception:
        return [ocr_cell(c, lang=lang, kind=kind, psm=7, timeout=3) for c in cells]

    tokens: List[List[Tuple[int, str]]] = [[] for _ in cells]
    confs: List[List[float]] = [[] for _ in cells]
    for i, t in enumerate(data.get("text", [])):
        txt = (t or "").strip()
        if not txt:
            continue
        cy = float(data["top"][i]) + float(data["height"][i]) / 2.0
        row_idx = None
        for j, (a, b) in enumerate(bounds):
            if a <= cy < b:
                row_idx = j
                break
        if row_idx is None:
            continue
        tokens[row_idx].append((int(data["left"][i]), txt))
        try:
            c = float(data["conf"][i])
            if c >= 0:
                confs[row_idx].append(c)
        except Exception:
            pass

    out: List[OcrCell] = []
    for row_tokens, row_confs, original_cell in zip(tokens, confs, cells):
        if kind == "text":
            txt = " ".join(t for _, t in sorted(row_tokens)).strip()
        else:
            # Per numeri e rapporti non inserire spazi artificiali.
            txt = "".join(t for _, t in sorted(row_tokens)).strip()
        if kind == "signed_int" and txt and detect_minus_sign(original_cell):
            txt = "-" + txt.lstrip("+-")
        out.append(OcrCell(text=txt, conf=(sum(row_confs) / len(row_confs) if row_confs else None)))
    return out

def has_ink(image: np.ndarray, *, threshold: int = 215, min_pixels: int = 20) -> bool:
    return int(np.sum(image < threshold)) >= min_pixels
