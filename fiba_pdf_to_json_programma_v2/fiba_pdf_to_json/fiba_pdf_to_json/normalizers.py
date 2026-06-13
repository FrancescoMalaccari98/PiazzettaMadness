from __future__ import annotations

import re
import unicodedata
from typing import Any, Optional, Tuple


def clean_text(text: Any) -> str:
    if text is None:
        return ""
    s = str(text)
    s = s.replace("\n", " ")
    s = s.replace("—", "-").replace("–", "-").replace("−", "-")
    s = s.replace("|", " ").replace("[", " ").replace("]", " ")
    s = s.replace("{", " ").replace("}", " ")
    s = s.replace("“", "").replace("”", "")
    s = s.replace("_", " ")
    s = re.sub(r"\s+", " ", s).strip()
    return s


def clean_ocr_numeric(text: Any) -> str:
    s = clean_text(text)
    trans = str.maketrans({
        "O": "0", "o": "0", "Q": "0", "D": "0",
        "I": "1", "l": "1", "|": "1", "!": "1",
        "S": "5", "s": "5",
        "B": "8",
        "€": "",
        "'": "",
        "`": "",
    })
    s = s.translate(trans)
    s = s.replace(" ", "")
    return s


def parse_int(text: Any, *, small_cell: bool = False) -> Optional[int]:
    s = clean_ocr_numeric(text)
    if not s:
        return None
    s = s.replace(",", ".")
    m = re.search(r"[-+]?\d+", s)
    if not m:
        return None
    try:
        value = int(m.group(0))
    except ValueError:
        return None
    if small_cell and abs(value) >= 100 and value % 10 == 0:
        value = int(value / 10)
    return value


def parse_float(text: Any) -> Optional[float]:
    s = clean_ocr_numeric(text)
    if not s:
        return None
    s = s.replace(",", ".")
    m = re.search(r"[-+]?\d+(?:\.\d+)?", s)
    if not m:
        return None
    try:
        return float(m.group(0))
    except ValueError:
        return None


def parse_percentage(text: Any) -> Optional[float]:
    s0 = clean_text(text)
    if not s0 or s0 in {",", ".", "%"}:
        return None
    s = clean_ocr_numeric(s0).replace("%", "")
    # OCR ricorrente: 66,/ al posto di 66,7.
    s = re.sub(r"([,.])[/\\]", r"\g<1>7", s)
    s = s.replace(",", ".")
    s = re.sub(r"[^0-9.+-]", "", s)
    if not s or s in {".", "+", "-"}:
        return None
    if "." in s:
        try:
            return round(float(s), 1)
        except ValueError:
            return None
    digits = re.sub(r"\D", "", s)
    if not digits:
        return None
    if set(digits) == {"0"}:
        return 0.0
    n = int(digits)
    if len(digits) <= 2:
        return float(n)
    if len(digits) == 3:
        return round(n / 10.0, 1)
    if len(digits) == 4:
        if digits.startswith("100"):
            return 100.0
        return round(n / 10.0, 1)
    return float(n)


def compute_percentage(made: Optional[int], attempts: Optional[int]) -> Optional[float]:
    if made is None or attempts is None:
        return None
    if attempts == 0:
        return 0.0
    return round((made / attempts) * 100.0, 1)


def infer_attempts_from_percentage(made: int, pct: Optional[float]) -> Optional[int]:
    if pct is None:
        return None
    if pct == 0:
        # Con 0 realizzati il numero di tentativi non è deducibile dalla percentuale.
        return 0 if made == 0 else None
    if pct == 100:
        return made if made > 0 else 0
    best = None
    best_delta = 999.0
    for att in range(max(1, made), 60):
        val = round((made / att) * 100.0, 1)
        delta = abs(val - pct)
        if delta < best_delta:
            best_delta = delta
            best = att
    if best_delta <= 1.0:
        return best
    return None


def parse_ratio(text: Any, pct: Optional[float] = None) -> Tuple[Optional[int], Optional[int], bool]:
    """Restituisce realizzati, tentati, stimato.

    Gestisce OCR senza slash: 13 -> 1/3, 413 -> 4/13, 510 -> 5/10.
    Per percentuale 0,0 il numero tentati viene preso dal testo se presente.
    """
    s = clean_ocr_numeric(text)
    s = s.replace("÷", "/").replace("\\", "/")
    if not s:
        return None, None, False
    m = re.search(r"(\d{1,2})\s*/\s*(\d{1,2})", s)
    if m:
        return int(m.group(1)), int(m.group(2)), False
    digits = re.sub(r"\D", "", s)
    if not digits:
        return None, None, False
    if len(digits) == 1:
        made = int(digits)
        att = infer_attempts_from_percentage(made, pct)
        return made, att, True
    if len(digits) == 2:
        made = int(digits[0])
        att_raw = int(digits[1])
        if made == 0 and pct == 0:
            return made, att_raw, True
        att_pct = infer_attempts_from_percentage(made, pct)
        if att_pct is not None and att_pct != att_raw:
            return made, att_pct, True
        return made, att_raw, True
    # Casi come 413 = 4/13, 002 = 0/2, 1017 = 10/17.
    # Prova tutte le possibili separazioni e scegli quella coerente con pct.
    candidates = []
    for split in range(1, min(3, len(digits)) + 1):
        made = int(digits[:split])
        att = int(digits[split:]) if digits[split:] else None
        if att is None:
            continue
        if att >= made:
            expected = compute_percentage(made, att)
            delta = abs((expected or 0) - pct) if pct is not None else 0
            candidates.append((delta, made, att))
    if candidates:
        candidates.sort(key=lambda x: (x[0], abs(x[2] - x[1])))
        return candidates[0][1], candidates[0][2], True
    return None, None, True


def parse_minutes(text: Any) -> Tuple[Optional[str], Optional[int]]:
    if re.search(r"N\s*\.?\s*E\s*\.?,?", clean_text(text), re.IGNORECASE):
        return "N.E.", None
    s = clean_ocr_numeric(text)
    if not s:
        return None, None
    m = re.search(r"(\d{1,3})\s*[:.]\s*(\d{2})", s)
    if m:
        mm, ss = int(m.group(1)), int(m.group(2))
        if ss < 60:
            raw = f"{mm:02d}:{ss:02d}"
            return raw, mm * 60 + ss
    digits = re.sub(r"\D", "", s)
    if len(digits) >= 3:
        mm = int(digits[:-2])
        ss = int(digits[-2:])
        if ss < 60:
            raw = f"{mm:02d}:{ss:02d}"
            return raw, mm * 60 + ss
    return clean_text(text) or None, None


def normalize_player_number(text: Any) -> Optional[str]:
    s = clean_ocr_numeric(text)
    if not s:
        return None
    is_starter = "*" in s
    s = re.sub(r"\D", "", s)
    if not s:
        return None
    number = str(int(s)) if s.isdigit() else s
    return ("*" if is_starter else "") + number


def split_name(full_name: str) -> Tuple[Optional[str], Optional[str]]:
    full_name = clean_text(full_name)
    if not full_name:
        return None, None
    full_name = re.sub(r"\(\s*C\s*\)", "", full_name, flags=re.IGNORECASE).strip()
    tokens = full_name.split()
    if len(tokens) == 1:
        return tokens[0], None

    def uppercase_score(t: str) -> float:
        letters = [c for c in t if c.isalpha()]
        if not letters:
            return 0
        return sum(1 for c in letters if c.isupper()) / len(letters)

    if uppercase_score(tokens[-1]) > 0.75:
        return " ".join(tokens[:-1]) or None, tokens[-1]
    if uppercase_score(tokens[0]) > 0.75 and uppercase_score(tokens[-1]) < 0.75:
        return " ".join(tokens[1:]) or None, tokens[0]
    return tokens[0], " ".join(tokens[1:])


def clean_player_name(text: Any) -> Tuple[Optional[str], bool]:
    s = clean_text(text)
    if not s:
        return None, False
    capitano = bool(re.search(r"\(\s*C\s*\)", s, re.IGNORECASE))
    s = re.sub(r"\(\s*C\s*\)", "", s, flags=re.IGNORECASE)
    s = s.replace("  ", " ").strip(" -_")
    return s or None, capitano


def file_phase_from_name(filename: str) -> Optional[str]:
    name = filename.rsplit("/", 1)[-1]
    name = re.sub(r"\.[^.]+$", "", name)
    name = re.sub(r"\(\d+\)$", "", name).strip()
    low = unicodedata.normalize("NFKD", name).encode("ascii", "ignore").decode("ascii").lower()
    low = low.replace("tabellino", "").strip()
    low = re.sub(r"[^a-z0-9]+", "_", low).strip("_")
    return low or None
