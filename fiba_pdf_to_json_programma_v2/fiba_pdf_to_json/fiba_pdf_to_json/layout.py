from __future__ import annotations

from dataclasses import dataclass
from typing import List, Sequence, Tuple

import cv2
import numpy as np


@dataclass
class TableGrid:
    box: Tuple[int, int, int, int]
    x_lines: List[int]
    y_lines: List[int]


def cluster_positions(values: np.ndarray, threshold: float, max_gap: int = 5) -> List[int]:
    idx = np.where(values > threshold)[0]
    if len(idx) == 0:
        return []
    out: List[int] = []
    start = int(idx[0])
    prev = int(idx[0])
    for i0 in idx[1:]:
        i = int(i0)
        if i - prev > max_gap:
            out.append((start + prev) // 2)
            start = i
        prev = i
    out.append((start + prev) // 2)
    return out


def build_line_masks(gray: np.ndarray) -> Tuple[np.ndarray, np.ndarray, np.ndarray]:
    # Le linee e il testo sono scuri su sfondo bianco/grigio.
    binary = cv2.threshold(gray, 205, 255, cv2.THRESH_BINARY_INV)[1]
    h_kernel_len = max(60, gray.shape[1] // 28)
    v_kernel_len = max(45, gray.shape[0] // 60)
    horizontal_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (h_kernel_len, 1))
    vertical_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (1, v_kernel_len))
    hmask = cv2.morphologyEx(binary, cv2.MORPH_OPEN, horizontal_kernel, iterations=1)
    vmask = cv2.morphologyEx(binary, cv2.MORPH_OPEN, vertical_kernel, iterations=1)
    return binary, hmask, vmask


def remove_grid_lines(gray: np.ndarray, hmask: np.ndarray, vmask: np.ndarray, *, dilation: int = 3) -> np.ndarray:
    line_mask = cv2.add(hmask, vmask)
    if dilation:
        line_mask = cv2.dilate(line_mask, cv2.getStructuringElement(cv2.MORPH_RECT, (dilation, dilation)), iterations=1)
    clean = gray.copy()
    clean[line_mask > 0] = 255
    return clean


def detect_table_boxes(hmask: np.ndarray, vmask: np.ndarray, page_shape: Tuple[int, int]) -> List[Tuple[int, int, int, int]]:
    h, w = page_shape
    lines = cv2.add(hmask, vmask)
    contours, _ = cv2.findContours(lines, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    boxes: List[Tuple[int, int, int, int]] = []
    for c in contours:
        x, y, bw, bh = cv2.boundingRect(c)
        if bw > w * 0.04 and bh > h * 0.02:
            boxes.append((x, y, bw, bh))
    boxes.sort(key=lambda b: (b[1], b[0]))
    return boxes


def _rel_box(page_shape: Tuple[int, int], rel: Tuple[float, float, float, float]) -> Tuple[int, int, int, int]:
    h, w = page_shape
    x, y, bw, bh = rel
    return int(w * x), int(h * y), int(w * bw), int(h * bh)


def detect_player_table_boxes(boxes: Sequence[Tuple[int, int, int, int]], page_shape: Tuple[int, int]) -> List[Tuple[int, int, int, int]]:
    h, w = page_shape
    candidates = [
        b for b in boxes
        if b[2] > w * 0.82 and h * 0.20 < b[1] < h * 0.62 and h * 0.12 < b[3] < h * 0.20
    ]
    candidates.sort(key=lambda b: b[1])
    if len(candidates) >= 2:
        return candidates[:2]
    # Fallback ricavato dai 4 esempi: funziona anche se la morphology perde una linea.
    return [
        _rel_box(page_shape, (0.0476, 0.2523, 0.9048, 0.1551)),
        _rel_box(page_shape, (0.0476, 0.4418, 0.9048, 0.1551)),
    ]


def detect_comparative_boxes(boxes: Sequence[Tuple[int, int, int, int]], page_shape: Tuple[int, int]) -> List[Tuple[int, int, int, int]]:
    h, w = page_shape
    candidates = [
        b for b in boxes
        if w * 0.38 < b[2] < w * 0.50 and h * 0.58 < b[1] < h * 0.73 and h * 0.07 < b[3] < h * 0.12
    ]
    candidates.sort(key=lambda b: b[0])
    if len(candidates) >= 2:
        return candidates[:2]
    return [
        _rel_box(page_shape, (0.0577, 0.6035, 0.4336, 0.0910)),
        _rel_box(page_shape, (0.5087, 0.6035, 0.4336, 0.0910)),
    ]


def detect_score_interval_boxes(boxes: Sequence[Tuple[int, int, int, int]], page_shape: Tuple[int, int]) -> List[Tuple[int, int, int, int]]:
    h, w = page_shape
    candidates = [
        b for b in boxes
        if h * 0.17 < b[1] < h * 0.23 and w * 0.05 < b[2] < w * 0.11 and h * 0.02 < b[3] < h * 0.05
    ]
    candidates.sort(key=lambda b: b[0])
    return candidates


def grid_from_box(binary: np.ndarray, box: Tuple[int, int, int, int]) -> TableGrid:
    x, y, w, h = box
    roi = binary[y:y + h, x:x + w]
    horizontal_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (max(50, w // 32), 1))
    vertical_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (1, max(30, h // 12)))
    hmask = cv2.morphologyEx(roi, cv2.MORPH_OPEN, horizontal_kernel, iterations=1)
    vmask = cv2.morphologyEx(roi, cv2.MORPH_OPEN, vertical_kernel, iterations=1)
    x_lines = cluster_positions(np.sum(vmask > 0, axis=0), h * 0.30, max_gap=max(4, w // 450))
    y_lines = cluster_positions(np.sum(hmask > 0, axis=1), w * 0.35, max_gap=max(4, h // 120))
    # Garantisci bordi anche nei fallback o con piccole interruzioni di linea.
    if not x_lines or x_lines[0] > 8:
        x_lines = [1] + x_lines
    if x_lines[-1] < w - 8:
        x_lines.append(w - 1)
    if not y_lines or y_lines[0] > 8:
        y_lines = [1] + y_lines
    if y_lines[-1] < h - 8:
        y_lines.append(h - 1)
    return TableGrid(box=box, x_lines=x_lines, y_lines=y_lines)


def default_player_x_lines(width: int) -> List[int]:
    # Profili relativi ricavati dalle linee reali della tabella FIBA Live Stats.
    rel = [0.000, 0.0392, 0.2394, 0.2960, 0.3576, 0.4075, 0.4543, 0.5011, 0.5390, 0.5769, 0.6148, 0.6630, 0.6920, 0.7196, 0.7463, 0.7744, 0.8020, 0.8301, 0.8577, 0.8800, 0.9059, 0.9313, 0.9661, 0.999]
    return [int(width * r) for r in rel]
