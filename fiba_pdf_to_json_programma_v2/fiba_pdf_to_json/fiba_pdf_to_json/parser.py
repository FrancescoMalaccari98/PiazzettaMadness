from __future__ import annotations

import re
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

import cv2
import numpy as np

from . import roster_context as names
from .layout import TableGrid, default_player_x_lines, grid_from_box
from .normalizers import (
    clean_player_name,
    clean_text,
    compute_percentage,
    file_phase_from_name,
    normalize_player_number,
    parse_float,
    parse_int,
    parse_minutes,
    parse_percentage,
    parse_ratio,
    split_name,
)
from .ocr import OcrCell, has_ink, ocr_cell, ocr_column_cells, ocr_text
from .schema import giocatore_template

PLAYER_COLUMNS = [
    "numero", "nome", "min", "tc_rt", "tc_pct", "2_rt", "2_pct", "3_rt", "3_pct", "tl_rt", "tl_pct",
    "ro", "rd", "rt", "as", "pp", "pr", "sd", "ff", "fs", "pm", "val", "pti",
]
COLUMN_KINDS = {
    "numero": "number",
    "nome": "text",
    "min": "min",
    "tc_rt": "ratio", "tc_pct": "pct",
    "2_rt": "ratio", "2_pct": "pct",
    "3_rt": "ratio", "3_pct": "pct",
    "tl_rt": "ratio", "tl_pct": "pct",
    "ro": "small_int", "rd": "small_int", "rt": "small_int", "as": "small_int", "pp": "small_int", "pr": "small_int", "sd": "small_int",
    "ff": "small_int", "fs": "small_int", "pm": "signed_int", "val": "small_int", "pti": "small_int",
}


def add_warning(doc: Dict[str, Any], campo: str, motivo: str, *, affidabilita: str = "media", valore: Any = None) -> None:
    warning = {"campo": campo, "motivo": motivo, "affidabilita": affidabilita}
    if valore is not None:
        warning["valore"] = valore
    doc["affidabilita"]["warnings"].append(warning)


def add_note(doc: Dict[str, Any], note: str) -> None:
    doc["affidabilita"].setdefault("note", []).append(note)


def apply_reliability(doc: Dict[str, Any]) -> None:
    warnings = doc["affidabilita"].get("warnings", [])
    if not warnings:
        doc["affidabilita"]["livello_globale"] = "alta"
    elif any(w.get("affidabilita") == "bassa" for w in warnings):
        doc["affidabilita"]["livello_globale"] = "bassa"
    else:
        doc["affidabilita"]["livello_globale"] = "media"


def _slice(gray: np.ndarray, rel: Tuple[float, float, float, float]) -> np.ndarray:
    h, w = gray.shape[:2]
    x, y, bw, bh = rel
    return gray[int(h * y): int(h * (y + bh)), int(w * x): int(w * (x + bw))]


def parse_header_regions(doc: Dict[str, Any], clean_page: np.ndarray) -> None:
    """Parsing OCR mirato dell'header.

    Il layout FIBA è fisso: leggere regioni piccole è molto più affidabile
    dell'OCR su tutta la pagina.
    """
    # Centro: competizione, campo/data, risultato e parziali.
    center = _slice(clean_page, (0.31, 0.06, 0.38, 0.13))
    center_text = clean_text(ocr_text(center, psm=6, upscale=3))
    # Destra: N gara/spettatori/durata/report/arbitri.
    right = _slice(clean_page, (0.64, 0.06, 0.33, 0.13))
    right_text = clean_text(ocr_text(right, psm=6, upscale=3))
    # Riga campo/data/ora spesso è piccola: OCR dedicato.
    event_line = _slice(clean_page, (0.30, 0.085, 0.42, 0.025))
    event_text = clean_text(ocr_text(event_line, psm=7, upscale=5))
    score_line = _slice(clean_page, (0.25, 0.125, 0.50, 0.035))
    score_text = clean_text(ocr_text(score_line, psm=7, upscale=4))
    period_line = _slice(clean_page, (0.34, 0.158, 0.30, 0.024))
    period_text = clean_text(ocr_text(period_line, psm=7, upscale=5, whitelist="0123456789-,() "))
    full = "\n".join(x for x in [center_text, event_text, score_text, period_text, right_text] if x)

    if "Piazzetta" in full or "Madness" in full:
        doc["partita"]["competizione"] = "Piazzetta Madness"
    else:
        # Fallback: prima riga non vuota del centro.
        first = center_text.split(" ")[0:4]
        if first:
            doc["partita"]["competizione"] = clean_text(" ".join(first))

    m = re.search(r"(?P<campo>[A-Za-zÀ-ÿ]*iazzetta\s+Verde)\s*,?\s*(?P<data>\w+\s+\d{1,2}\s+\w+\s+\d{4}).*?(?P<ora>\d{1,2}[:.]\d{2})", event_text, re.I)
    if not m:
        m = re.search(r"(?P<campo>[A-Za-zÀ-ÿ]*iazzetta\s+Verde)\s*,?\s*(?P<data>\w+\s+\d{1,2}\s+\w+\s+\d{4}).*?(?P<ora>\d{1,2}[:.]\d{2})", full, re.I)
    if m:
        doc["partita"]["campo"] = "Piazzetta Verde" if "iazzetta" in clean_text(m.group("campo")).lower() else clean_text(m.group("campo"))
        doc["partita"]["data"] = clean_text(m.group("data"))
        doc["partita"]["ora"] = m.group("ora").replace(".", ":")

    parse_score_line(doc, score_text)
    if not doc["risultato"].get("punteggio_finale"):
        parse_score_line(doc, center_text)
    parse_period_line(doc, period_text)
    if not doc["risultato"].get("periodi"):
        parse_period_line(doc, center_text)

    # Campi destri: regex tolleranti a OCR senza punti.
    rg = right_text
    m = re.search(r"N\s*\.?\s*Gara\s*[:.]?\s*(\d+)", rg, re.I)
    if m:
        doc["partita"]["numero_gara"] = parse_int(m.group(1))
    m = re.search(r"Spettatori\s*[:.]?\s*(\d+)", rg, re.I)
    if m:
        doc["partita"]["spettatori"] = parse_int(m.group(1))
    m = re.search(r"Durata\s+gar[ae]\s*[:.]?\s*(\d{1,2}[:.]\d{2}|\d{4})", rg, re.I)
    if m:
        durata = m.group(1).replace(".", ":")
        if re.fullmatch(r"\d{4}", durata):
            durata = durata[:2] + ":" + durata[2:]
        doc["partita"]["durata_gara"] = durata
    m = re.search(r"Report\s+creato\s*[:.]?\s*(.+?)(?:\s+Arbitri|$)", rg, re.I)
    if m:
        doc["partita"]["report_creato"] = clean_text(m.group(1))
    m = re.search(r"Arbitri\s*[:.]?\s*(.+)$", rg, re.I)
    if m:
        arbitri = [clean_text(x) for x in re.split(r"\s*,\s*", m.group(1)) if clean_text(x)]
        doc["partita"]["arbitri"] = [a.replace("Gulia", "Giulia").replace("Davde", "Davide").replace("SAMPAOLESI", "SAPAOLESI") for a in arbitri]

    if not doc["risultato"]["punteggio_finale"]:
        add_warning(doc, "risultato.punteggio_finale", "Punteggio finale non letto dall'header; verrà ricavato dai totali squadra se possibile.", affidabilita="media", valore=score_text)


def parse_score_line(doc: Dict[str, Any], text: str) -> None:
    text = clean_text(text)
    # Esempi: Miami Spritz 48 - 26 Philadelphia 70Sexers
    m = re.search(r"(?P<t1>[A-Za-zÀ-ÿ0-9' .]+?)\s+(?P<p1>\d{1,3})\s*[-–]\s*(?P<p2>\d{1,3})\s+(?P<t2>[A-Za-zÀ-ÿ0-9' .]+)", text)
    if not m:
        return
    t1 = clean_text(m.group("t1"))
    t2 = clean_text(m.group("t2"))
    # Correggi i nomi squadra se vicini a quelli degli esempi.
    t1_known = names.fuzzy_known_name(t1, names.known_teams(), cutoff=0.78)
    t2_known = names.fuzzy_known_name(t2, names.known_teams(), cutoff=0.78)
    t1 = t1_known or t1
    t2 = t2_known or t2
    p1 = parse_int(m.group("p1"))
    p2 = parse_int(m.group("p2"))
    doc["risultato"]["squadra_casa"].update({"nome": t1, "punti_finali": p1})
    doc["risultato"]["squadra_ospite"].update({"nome": t2, "punti_finali": p2})
    if p1 is not None and p2 is not None:
        doc["risultato"]["punteggio_finale"] = f"{p1}-{p2}"


def parse_period_line(doc: Dict[str, Any], text: str) -> None:
    cleaned = clean_text(text).replace("–", "-")
    # Preferisci i parziali tra parentesi: evita di scambiare il risultato finale per Q1.
    m = re.search(r"\((\d{1,3}\s*-\s*\d{1,3}(?:\s*,\s*\d{1,3}\s*-\s*\d{1,3})*)\)", cleaned)
    if not m:
        # Fallback: usa solo se ci sono almeno due coppie, tipicamente Q1,Q2 o più.
        pairs = re.findall(r"\d{1,3}\s*-\s*\d{1,3}", cleaned)
        if len(pairs) < 2:
            return
        raw = ", ".join(pairs)
    else:
        raw = clean_text(m.group(1))
    doc["risultato"]["punteggio_intermedio"] = raw
    periods = []
    for idx, part in enumerate(re.split(r"\s*,\s*", raw), start=1):
        mm = re.match(r"(\d{1,3})\s*-\s*(\d{1,3})", part)
        if not mm:
            continue
        periods.append({
            "numero": idx,
            "label": f"Q{idx}" if idx <= 4 else f"OT{idx - 4}",
            "squadra_casa": parse_int(mm.group(1)),
            "squadra_ospite": parse_int(mm.group(2)),
        })
    doc["risultato"]["periodi"] = periods

def parse_team_title(clean_page: np.ndarray, table_box: Tuple[int, int, int, int]) -> Tuple[Optional[str], Optional[str], Optional[str], Optional[str]]:
    x, y, w, h = table_box
    page_h, page_w = clean_page.shape[:2]
    y1 = max(0, y - int(page_h * 0.035))
    y2 = max(0, y - int(page_h * 0.002))
    x1 = max(0, x - int(page_w * 0.006))
    x2 = min(page_w, x + int(page_w * 0.58))
    roi = clean_page[y1:y2, x1:x2]
    text = clean_text(ocr_text(roi, psm=7, upscale=4))
    m = re.search(r"(.+?)\s*\(([A-Z0-9]{2,5})\)", text)
    team_name = None
    abbr = None
    if m:
        team_name = clean_text(m.group(1))
        abbr = clean_text(m.group(2))
    elif text:
        team_name = text
    if team_name:
        known = names.fuzzy_known_name(team_name, names.known_teams(), cutoff=0.78)
        if known:
            team_name = known

    # Allenatore e vice sono sulla destra della riga sopra la tabella.
    x3 = int(page_w * 0.82)
    x4 = min(page_w, x + w)
    coach_roi = clean_page[y1:y2, x3:x4]
    coach_text = clean_text(ocr_text(coach_roi, psm=6, upscale=4))
    allenatore = None
    vice = None
    m1 = re.search(r"Allenatore\s*:\s*([^:]+?)(?:Vice|$)", coach_text, re.I)
    if m1:
        val = clean_text(m1.group(1))
        allenatore = val or None
    m2 = re.search(r"Vice\s+Allenatore\s*:\s*(.+)$", coach_text, re.I)
    if m2:
        val = clean_text(m2.group(1))
        vice = val or None
    return team_name, abbr, allenatore, vice


def _cell_image(roi_clean: np.ndarray, x_lines: List[int], y_lines: List[int], ri: int, ci: int, *, pad: int = 4) -> np.ndarray:
    x1, x2 = x_lines[ci] + pad, x_lines[ci + 1] - pad
    y1, y2 = y_lines[ri] + pad, y_lines[ri + 1] - pad
    x1, y1 = max(0, x1), max(0, y1)
    x2, y2 = min(roi_clean.shape[1], x2), min(roi_clean.shape[0], y2)
    return roi_clean[y1:y2, x1:x2]


def extract_rows_by_column(clean_page: np.ndarray, binary: np.ndarray, table_box: Tuple[int, int, int, int], doc: Dict[str, Any], *, field_prefix: str) -> Tuple[List[Dict[str, OcrCell]], TableGrid]:
    grid = grid_from_box(binary, table_box)
    x, y, w, h = table_box
    x_lines = grid.x_lines if len(grid.x_lines) >= len(PLAYER_COLUMNS) + 1 else default_player_x_lines(w)
    y_lines = grid.y_lines
    if len(y_lines) < 5:
        add_warning(doc, field_prefix, "Rilevamento righe tabella incompleto.", affidabilita="bassa")
        return [], grid

    roi_gray = clean_page[y:y + h, x:x + w]
    roi_bin = binary[y:y + h, x:x + w]
    # Rimuovi di nuovo le linee interne della singola tabella per pulire eventuali residui.
    hmask = cv2.morphologyEx(roi_bin, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (max(50, w // 32), 1)))
    vmask = cv2.morphologyEx(roi_bin, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (1, max(30, h // 12))))
    line_mask = cv2.dilate(cv2.add(hmask, vmask), cv2.getStructuringElement(cv2.MORPH_RECT, (3, 3)), iterations=1)
    roi_clean = roi_gray.copy()
    roi_clean[line_mask > 0] = 255

    # Le prime due righe sono intestazioni; l'ultima è Totali; la penultima è Squadra/Allenatore.
    body_ris = list(range(2, len(y_lines) - 1))
    if len(body_ris) < 2:
        return [], grid
    rows: List[Dict[str, OcrCell]] = [{col: OcrCell("", None) for col in PLAYER_COLUMNS} for _ in body_ris]

    for ci, col in enumerate(PLAYER_COLUMNS):
        if ci >= len(x_lines) - 1:
            add_warning(doc, f"{field_prefix}.{col}", "Colonna non rilevata nel layout.", affidabilita="bassa")
            continue
        kind = COLUMN_KINDS[col]
        cells = [_cell_image(roi_clean, x_lines, y_lines, ri, ci) for ri in body_ris]
        try:
            ocrs = ocr_column_cells(cells, kind=kind, lang="eng", upscale=4)
        except Exception:
            # Fallback per-cell, più lento ma utile in ambienti OCR instabili.
            ocrs = [ocr_cell(c, kind=kind, lang="eng") for c in cells]
        for idx, cell_ocr in enumerate(ocrs):
            # Fallback mirato solo per colonne indispensabili/non numeriche.
            # Evitiamo retry cella-per-cella sulle statistiche: è molto più lento
            # e, su Tesseract, può introdurre blocchi/letture incoerenti.
            if col in {"numero", "nome", "minuti"} and not cell_ocr.text and has_ink(cells[idx], min_pixels=12):
                try:
                    cell_ocr = ocr_cell(cells[idx], kind=kind, lang="eng", timeout=3)
                except Exception:
                    pass
            rows[idx][col] = cell_ocr
    return rows, grid


def make_shot(cell_rt: str, cell_pct: str, doc: Dict[str, Any], field_path: str) -> Dict[str, Any]:
    pct_ocr = parse_percentage(cell_pct)
    made, att, estimated_ratio = parse_ratio(cell_rt, pct_ocr)
    pct_calc = compute_percentage(made, att)
    pct = pct_ocr
    if made is not None and att is not None:
        if pct is None:
            pct = pct_calc
        elif pct_calc is not None and abs(pct - pct_calc) > 1.0:
            add_warning(
                doc,
                field_path + ".percentuale",
                "Percentuale OCR incoerente con realizzati/tentati: usata percentuale calcolata.",
                affidabilita="media",
                valore={"ocr": cell_pct, "ocr_normalizzato": pct_ocr, "calcolato": pct_calc, "rapporto": cell_rt},
            )
            pct = pct_calc
    if estimated_ratio and cell_rt:
        add_warning(doc, field_path, "Rapporto tiri stimato/corretto da OCR senza separatore '/' o poco leggibile.", affidabilita="media", valore=cell_rt)
    return {"realizzati": made, "tentati": att, "percentuale": pct}




def _apply_safe_derivations(obj: Dict[str, Any], doc: Dict[str, Any], prefix: str) -> None:
    """Derive null values that can be computed with certainty from other fields.

    Only derives when all operands are present and non-null.
    Adds a warning for each derived value so the correction is traceable.
    """
    if obj.get("punti") is None:
        made2 = (obj.get("tiri_da_2") or {}).get("realizzati")
        made3 = (obj.get("tiri_da_3") or {}).get("realizzati")
        made_ft = (obj.get("tiri_liberi") or {}).get("realizzati")
        if all(v is not None for v in [made2, made3, made_ft]):
            derived = 2 * made2 + 3 * made3 + made_ft
            obj["punti"] = derived
            add_warning(doc, f"{prefix}.punti",
                        f"Punti OCR mancante: calcolato da 2*2P+3*3P+TL = {derived}.",
                        affidabilita="media")
    rimbalzi = obj.get("rimbalzi") or {}
    if rimbalzi.get("totali") is None:
        ro = rimbalzi.get("offensivi")
        rd = rimbalzi.get("difensivi")
        if ro is not None and rd is not None:
            derived_rt = ro + rd
            rimbalzi["totali"] = derived_rt
            add_warning(doc, f"{prefix}.rimbalzi.totali",
                        f"Rimbalzi totali OCR mancante: calcolato da RO+RD = {derived_rt}.",
                        affidabilita="media")


def correct_field_goal_consistency(obj: Dict[str, Any], doc: Dict[str, Any], prefix: str) -> None:
    """Corregge Tiri dal campo quando 2P + 3P è più affidabile dell'OCR della cella totale.

    È una correzione strutturale minima del tabellino: non fa validazioni
    statistiche avanzate, ma usa una relazione diretta della stessa riga.
    """
    tc = obj.get("tiri_dal_campo") or {}
    t2 = obj.get("tiri_da_2") or {}
    t3 = obj.get("tiri_da_3") or {}
    vals = [t2.get("realizzati"), t2.get("tentati"), t3.get("realizzati"), t3.get("tentati")]
    if any(v is None for v in vals):
        return
    made = int(t2["realizzati"]) + int(t3["realizzati"])
    att = int(t2["tentati"]) + int(t3["tentati"])
    if tc.get("realizzati") != made or tc.get("tentati") != att:
        add_warning(
            doc,
            prefix + ".tiri_dal_campo",
            "Tiri dal campo OCR incoerenti con 2P+3P: usata somma delle colonne 2P e 3P.",
            affidabilita="media",
            valore={"ocr": dict(tc), "corretto": {"realizzati": made, "tentati": att, "percentuale": compute_percentage(made, att)}},
        )
        obj["tiri_dal_campo"] = {"realizzati": made, "tentati": att, "percentuale": compute_percentage(made, att)}
    else:
        pct_calc = compute_percentage(tc.get("realizzati"), tc.get("tentati"))
        if pct_calc is not None and (tc.get("percentuale") is None or abs(tc.get("percentuale") - pct_calc) > 1.0):
            obj["tiri_dal_campo"]["percentuale"] = pct_calc
def row_to_player(row: Dict[str, OcrCell], doc: Dict[str, Any], team_name: str, row_idx: int) -> Optional[Dict[str, Any]]:
    name_raw = row.get("nome", OcrCell("", None)).text
    num_raw = row.get("numero", OcrCell("", None)).text
    min_raw = row.get("min", OcrCell("", None)).text
    if not clean_text(name_raw) and not normalize_player_number(num_raw):
        return None
    low_marker = clean_text(f"{num_raw} {name_raw}").lower()
    if "squadra" in low_marker or "allenatore" in low_marker or "totali" in low_marker:
        return None

    g = giocatore_template()
    g["numero"] = normalize_player_number(num_raw)
    name, cap = clean_player_name(name_raw)
    _team_candidates = names.known_players_for_team(team_name) if team_name else []
    _candidates = _team_candidates or names.known_players()
    known_name = names.fuzzy_known_name(name or "", _candidates, cutoff=0.80)
    if known_name:
        name = known_name
    g["nome_completo"] = name
    g["capitano"] = cap or bool(re.search(r"\(\s*C\s*\)", name_raw or "", re.I))
    g["nome"], g["cognome"] = split_name(name or "")

    # Validate jersey against known roster and correct OCR misreads (e.g. *1 → 4).
    if known_name and team_name:
        expected_jersey = names.roster_jersey_for(team_name, known_name)
        if expected_jersey is not None:
            ocr_raw = g["numero"] or ""
            digits_ocr = re.sub(r"\D", "", ocr_raw)
            if digits_ocr != expected_jersey:
                add_warning(
                    doc,
                    f"squadre.{team_name}.giocatori.{known_name}.numero",
                    f"Numero maglia OCR '{digits_ocr}' corretto con roster '{expected_jersey}'.",
                    affidabilita="media",
                )
                g["numero"] = ("*" if ocr_raw.startswith("*") else "") + expected_jersey
    raw_min, seconds = parse_minutes(min_raw)
    g["non_entrato"] = raw_min == "N.E."
    g["minuti"] = {"raw": raw_min, "secondi": seconds}

    if g["non_entrato"]:
        return g

    prefix = f"squadre.{team_name}.giocatori.{g.get('nome_completo') or g.get('numero') or row_idx}"
    g["tiri_dal_campo"] = make_shot(row["tc_rt"].text, row["tc_pct"].text, doc, prefix + ".tiri_dal_campo")
    g["tiri_da_2"] = make_shot(row["2_rt"].text, row["2_pct"].text, doc, prefix + ".tiri_da_2")
    g["tiri_da_3"] = make_shot(row["3_rt"].text, row["3_pct"].text, doc, prefix + ".tiri_da_3")
    g["tiri_liberi"] = make_shot(row["tl_rt"].text, row["tl_pct"].text, doc, prefix + ".tiri_liberi")
    g["rimbalzi"] = {
        "offensivi": parse_int(row["ro"].text, small_cell=True),
        "difensivi": parse_int(row["rd"].text, small_cell=True),
        "totali": parse_int(row["rt"].text, small_cell=True),
    }
    g["assist"] = parse_int(row["as"].text, small_cell=True)
    g["palle_perse"] = parse_int(row["pp"].text, small_cell=True)
    g["palle_recuperate"] = parse_int(row["pr"].text, small_cell=True)
    g["stoppate_date"] = parse_int(row["sd"].text, small_cell=True)
    g["falli"] = {"commessi": parse_int(row["ff"].text, small_cell=True), "subiti": parse_int(row["fs"].text, small_cell=True)}
    g["plus_minus"] = parse_int(row["pm"].text)
    g["valutazione"] = parse_int(row["val"].text)
    g["punti"] = parse_int(row["pti"].text)

    correct_field_goal_consistency(g, doc, prefix)
    _apply_safe_derivations(g, doc, prefix)

    conf_name = row.get("nome", OcrCell("", None)).conf
    if conf_name is not None and conf_name < 45:
        add_warning(doc, prefix + ".nome_completo", "OCR con confidenza bassa sul nome giocatore.", affidabilita="media", valore=name_raw)
    return g


def row_to_totals(row: Dict[str, OcrCell], doc: Dict[str, Any], team_name: str) -> Dict[str, Any]:
    raw_min, seconds = parse_minutes(row.get("min", OcrCell("", None)).text)
    prefix = f"squadre.{team_name}.totali_squadra"
    totals = {
        "minuti": {"raw": raw_min, "secondi": seconds},
        "tiri_dal_campo": make_shot(row["tc_rt"].text, row["tc_pct"].text, doc, prefix + ".tiri_dal_campo"),
        "tiri_da_2": make_shot(row["2_rt"].text, row["2_pct"].text, doc, prefix + ".tiri_da_2"),
        "tiri_da_3": make_shot(row["3_rt"].text, row["3_pct"].text, doc, prefix + ".tiri_da_3"),
        "tiri_liberi": make_shot(row["tl_rt"].text, row["tl_pct"].text, doc, prefix + ".tiri_liberi"),
        "rimbalzi": {
            "offensivi": parse_int(row["ro"].text, small_cell=True),
            "difensivi": parse_int(row["rd"].text, small_cell=True),
            "totali": parse_int(row["rt"].text, small_cell=True),
        },
        "assist": parse_int(row["as"].text, small_cell=True),
        "palle_perse": parse_int(row["pp"].text, small_cell=True),
        "palle_recuperate": parse_int(row["pr"].text, small_cell=True),
        "stoppate_date": parse_int(row["sd"].text, small_cell=True),
        "falli": {"commessi": parse_int(row["ff"].text, small_cell=True), "subiti": parse_int(row["fs"].text, small_cell=True)},
        "plus_minus": parse_int(row["pm"].text),
        "valutazione": parse_int(row["val"].text),
        "punti": parse_int(row["pti"].text),
    }
    correct_field_goal_consistency(totals, doc, prefix)
    _apply_safe_derivations(totals, doc, prefix)
    return totals


def parse_player_table(doc: Dict[str, Any], clean_page: np.ndarray, binary: np.ndarray, table_box: Tuple[int, int, int, int], squadra_idx: int) -> None:
    team = doc["squadre"][squadra_idx]
    side = "squadra_casa" if squadra_idx == 0 else "squadra_ospite"
    team_name, abbr, coach, vice = parse_team_title(clean_page, table_box)
    if team_name:
        team["nome"] = team_name
        doc["risultato"][side]["nome"] = team_name
    if abbr:
        team["abbreviazione"] = abbr
        doc["risultato"][side]["abbreviazione"] = abbr
    if coach:
        team["allenatore"] = coach
    if vice:
        team["vice_allenatore"] = vice

    field_prefix = f"squadre.{squadra_idx}.tabella_giocatori"
    rows, _grid = extract_rows_by_column(clean_page, binary, table_box, doc, field_prefix=field_prefix)
    if not rows:
        return

    # Le ultime due righe del blocco body sono Squadra/Allenatore e Totali nella struttura FIBA.
    data_rows = rows[:-2] if len(rows) >= 2 else rows
    totals_row = rows[-1] if rows else None

    for ridx, row in enumerate(data_rows, start=1):
        player = row_to_player(row, doc, team.get("nome") or str(squadra_idx), ridx)
        if player:
            team["giocatori"].append(player)

    if totals_row:
        team["totali_squadra"] = row_to_totals(totals_row, doc, team.get("nome") or str(squadra_idx))


def parse_area_value(text: str) -> Dict[str, Any]:
    res = {"punti": None, "realizzati": None, "tentati": None, "percentuale": None}
    text = clean_text(text)
    m = re.search(r"(\d+)\s*\(?\s*(\d+)\s*/\s*(\d+)\s*\)?\s*([\d,.]+)?", text)
    if m:
        res["punti"] = parse_int(m.group(1))
        res["realizzati"] = parse_int(m.group(2))
        res["tentati"] = parse_int(m.group(3))
        pct = parse_percentage(m.group(4)) if m.group(4) else None
        calc = compute_percentage(res["realizzati"], res["tentati"])
        res["percentuale"] = calc if pct is None or (calc is not None and abs(pct - calc) > 1.0) else pct
    else:
        res["punti"] = parse_int(text)
    return res


def _ocr_comparative_table(clean_page: np.ndarray, binary: np.ndarray, box: Tuple[int, int, int, int]) -> List[List[OcrCell]]:
    grid = grid_from_box(binary, box)
    x, y, w, h = box
    roi_gray = clean_page[y:y + h, x:x + w]
    roi_bin = binary[y:y + h, x:x + w]
    hmask = cv2.morphologyEx(roi_bin, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (max(40, w // 10), 1)))
    vmask = cv2.morphologyEx(roi_bin, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (1, max(20, h // 8))))
    line_mask = cv2.dilate(cv2.add(hmask, vmask), cv2.getStructuringElement(cv2.MORPH_RECT, (3, 3)), iterations=1)
    roi_clean = roi_gray.copy()
    roi_clean[line_mask > 0] = 255
    x_lines = grid.x_lines
    y_lines = grid.y_lines
    if len(x_lines) < 4:
        x_lines = [1, int(w * 0.52), int(w * 0.76), w - 1]
    row_indices = list(range(1, len(y_lines) - 1))
    rows: List[List[OcrCell]] = [[OcrCell("", None), OcrCell("", None)] for _ in row_indices]
    for out_ci, ci in enumerate(range(1, min(3, len(x_lines) - 1))):
        cells = [_cell_image(roi_clean, x_lines, y_lines, ri, ci, pad=5) for ri in row_indices]
        try:
            ocrs = ocr_column_cells(cells, kind="area", lang="eng", upscale=4, timeout=10)
        except Exception:
            ocrs = []
            for cell in cells:
                try:
                    ocrs.append(ocr_cell(cell, kind="area", lang="eng", psm=7, timeout=4))
                except Exception:
                    ocrs.append(OcrCell("", None))
        for ri, ocr_value in enumerate(ocrs):
            if ri < len(rows):
                # Fallback per celle con singolo numero centrato: Tesseract PSM 6/7 può restituire solo parentesi o stringa vuota.
                raw_text = ocr_value.text or ""
                needs_retry = not re.search(r"\d", raw_text)
                if needs_retry and ri < len(cells) and has_ink(cells[ri], min_pixels=8):
                    try:
                        retry = ocr_text(cells[ri], psm=8, upscale=8, whitelist="0123456789(),./%+-:", timeout=4)
                    except Exception:
                        retry = ""
                    if retry:
                        ocr_value = OcrCell(retry, ocr_value.conf)
                rows[ri][out_ci] = ocr_value
    return rows

def parse_comparatives(doc: Dict[str, Any], clean_page: np.ndarray, binary: np.ndarray, comp_boxes: List[Tuple[int, int, int, int]]) -> None:
    if len(comp_boxes) < 2:
        return
    left = _ocr_comparative_table(clean_page, binary, comp_boxes[0])
    right = _ocr_comparative_table(clean_page, binary, comp_boxes[1])
    comp = doc["statistiche_comparative"]

    def val(rowset: List[List[OcrCell]], r: int, c: int) -> str:
        try:
            return rowset[r][c].text
        except Exception:
            return ""

    # Sinistra: 6 righe fisse.
    comp["punti_da_palle_perse"] = {"squadra_casa": parse_int(val(left, 0, 0)), "squadra_ospite": parse_int(val(left, 0, 1))}
    comp["punti_in_area"] = {"squadra_casa": parse_area_value(val(left, 1, 0)), "squadra_ospite": parse_area_value(val(left, 1, 1))}
    comp["punti_da_secondi_tiri"] = {"squadra_casa": parse_int(val(left, 2, 0)), "squadra_ospite": parse_int(val(left, 2, 1))}
    comp["punti_contropiede"] = {"squadra_casa": parse_int(val(left, 3, 0)), "squadra_ospite": parse_int(val(left, 3, 1))}
    comp["fast_break_points_from_turnovers"] = {"squadra_casa": parse_int(val(left, 4, 0)), "squadra_ospite": parse_int(val(left, 4, 1))}
    comp["punti_panchina"] = {"squadra_casa": parse_int(val(left, 5, 0)), "squadra_ospite": parse_int(val(left, 5, 1))}

    # Destra: 6 righe fisse. Alcune sono stringhe composte, non solo numeri.
    comp["massimo_vantaggio"] = {"squadra_casa": clean_text(val(right, 0, 0)) or None, "squadra_ospite": clean_text(val(right, 0, 1)) or None}
    comp["massimo_parziale"] = {"squadra_casa": clean_text(val(right, 1, 0)) or None, "squadra_ospite": clean_text(val(right, 1, 1)) or None}
    comp["punti_per_possesso"] = {"squadra_casa": parse_float(val(right, 2, 0)), "squadra_ospite": parse_float(val(right, 2, 1))}
    # Cambi guida e parità sono celle centrali fuse: leggile da entrambe e scegli primo int.
    cg = parse_int(val(right, 3, 0))
    if cg is None:
        cg = parse_int(val(right, 3, 1))
    comp["cambi_guida"] = cg
    par = parse_int(val(right, 4, 0))
    if par is None:
        par = parse_int(val(right, 4, 1))
    comp["parita"] = par
    comp["tempo_in_vantaggio"] = {"squadra_casa": clean_text(val(right, 5, 0)) or None, "squadra_ospite": clean_text(val(right, 5, 1)) or None}



def _sum_player_values(players: List[Dict[str, Any]], path: List[str]) -> Optional[int]:
    vals: List[int] = []
    for g in players:
        cur: Any = g
        for key in path:
            if not isinstance(cur, dict):
                cur = None
                break
            cur = cur.get(key)
        if isinstance(cur, int):
            vals.append(cur)
    return sum(vals) if vals else None


def _fill_missing_team_totals_from_players(doc: Dict[str, Any], team: Dict[str, Any]) -> None:
    players = [g for g in team.get("giocatori", []) if not g.get("non_entrato")]
    totals = team.get("totali_squadra") or {}
    if not players or not totals:
        return
    team_name = team.get("nome") or "squadra"

    simple_paths = [
        (["assist"], "assist"),
        (["palle_perse"], "palle_perse"),
        (["palle_recuperate"], "palle_recuperate"),
        (["stoppate_date"], "stoppate_date"),
        (["valutazione"], "valutazione"),
        (["punti"], "punti"),
        (["falli", "commessi"], "falli.commessi"),
        (["falli", "subiti"], "falli.subiti"),
        (["rimbalzi", "offensivi"], "rimbalzi.offensivi"),
        (["rimbalzi", "difensivi"], "rimbalzi.difensivi"),
        (["rimbalzi", "totali"], "rimbalzi.totali"),
    ]
    for path, label in simple_paths:
        cur: Any = totals
        for key in path[:-1]:
            cur = cur.get(key) if isinstance(cur, dict) else None
        if isinstance(cur, dict) and cur.get(path[-1]) is None:
            s = _sum_player_values(players, path)
            if s is not None:
                cur[path[-1]] = s
                add_warning(doc, f"squadre.{team_name}.totali_squadra.{label}", "Totale squadra mancante da OCR: compilato sommando i giocatori.", affidabilita="media", valore=s)

    shot_fields = ["tiri_dal_campo", "tiri_da_2", "tiri_da_3", "tiri_liberi"]
    for field in shot_fields:
        t = totals.get(field) or {}
        if t.get("realizzati") is None or t.get("tentati") is None:
            made = _sum_player_values(players, [field, "realizzati"])
            att = _sum_player_values(players, [field, "tentati"])
            if made is not None and att is not None:
                totals[field] = {"realizzati": made, "tentati": att, "percentuale": compute_percentage(made, att)}
                add_warning(doc, f"squadre.{team_name}.totali_squadra.{field}", "Totale tiri mancante da OCR: compilato sommando i giocatori.", affidabilita="media", valore=totals[field])

def finalize_result_from_tables(doc: Dict[str, Any]) -> None:
    for idx, side in enumerate(["squadra_casa", "squadra_ospite"]):
        team = doc["squadre"][idx]
        res = doc["risultato"][side]
        if not res.get("nome"):
            res["nome"] = team.get("nome")
        if not res.get("abbreviazione"):
            res["abbreviazione"] = team.get("abbreviazione")
        total_pts = team.get("totali_squadra", {}).get("punti")
        if total_pts is not None:
            if res.get("punti_finali") is None:
                res["punti_finali"] = total_pts
            elif res.get("punti_finali") != total_pts:
                add_warning(doc, f"risultato.{side}.punti_finali", "Punti finali header diversi dai totali squadra: mantenuto totale squadra.", affidabilita="media", valore={"header": res.get("punti_finali"), "totale_squadra": total_pts})
                res["punti_finali"] = total_pts

    # Ricalcola minuti squadra dai giocatori se il totale OCR manca o è incoerente.
    for team in doc.get("squadre", []):
        total_seconds = sum((g.get("minuti") or {}).get("secondi") or 0 for g in team.get("giocatori", []))
        current_seconds = (team.get("totali_squadra") or {}).get("minuti", {}).get("secondi")
        if total_seconds and (current_seconds is None or abs(current_seconds - total_seconds) > 60):
            mm, ss = divmod(total_seconds, 60)
            team["totali_squadra"]["minuti"] = {"raw": f"{mm:02d}:{ss:02d}", "secondi": total_seconds}
        _fill_missing_team_totals_from_players(doc, team)

    p1 = doc["risultato"]["squadra_casa"].get("punti_finali")
    p2 = doc["risultato"]["squadra_ospite"].get("punti_finali")
    if p1 is not None and p2 is not None:
        doc["risultato"]["punteggio_finale"] = f"{p1}-{p2}"

    # Se l'header non ha letto le abbreviazioni, sono recuperate dai titoli tabella.
    # Se non ha letto i periodi, lascia []: validazione statistica esterna.
