from __future__ import annotations

import argparse
import json
import logging
import subprocess
import sys
import time
from pathlib import Path
from typing import Any, Dict, List

from .normalizers import file_phase_from_name
from .schema import document_template


def build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(description="Converte PDF tabellino FIBA Live Stats in JSON strutturato.")
    p.add_argument("input", help="PDF singolo o cartella contenente PDF")
    p.add_argument("-o", "--output", default="output_json", help="Cartella di output JSON")
    p.add_argument("--fallback-scale", type=int, default=4, help="Scala rendering PDF se non sono disponibili immagini native")
    p.add_argument("--no-native-images", action="store_true", help="Disabilita estrazione immagini native e forza rendering PDF")
    p.add_argument("--debug-dir", default=None, help="Cartella opzionale in cui salvare immagini debug OCR/layout")
    p.add_argument("--no-isolate", action="store_true", help="Esegue tutti i PDF nello stesso processo. Più veloce, ma meno robusto con Tesseract su batch lunghi")
    p.add_argument("--timeout-per-pdf", type=int, default=180, help="Timeout massimo in secondi per ogni PDF quando l'isolamento processo è attivo")
    p.add_argument("--verbose", action="store_true", help="Log dettagliati")
    return p


def iter_input_pdfs(input_path: str | Path) -> List[Path]:
    p = Path(input_path)
    if p.is_file() and p.suffix.lower() == ".pdf":
        return [p]
    if p.is_dir():
        return sorted(x for x in p.iterdir() if x.is_file() and x.suffix.lower() == ".pdf")
    return []


def _error_document(pdf: Path, message: str) -> Dict[str, Any]:
    data = document_template()
    data["metadata"]["nome_file"] = pdf.name
    data["metadata"]["fase_partita"] = file_phase_from_name(pdf.name)
    data["affidabilita"]["livello_globale"] = "bassa"
    data["affidabilita"]["warnings"].append({
        "campo": "pdf",
        "motivo": message,
        "affidabilita": "bassa",
    })
    return data


def _write_error(pdf: Path, out_file: Path, message: str) -> None:
    import json

    out_file.parent.mkdir(parents=True, exist_ok=True)
    with out_file.open("w", encoding="utf-8") as f:
        json.dump(_error_document(pdf, message), f, ensure_ascii=False, indent=2)
        f.write("\n")


def _json_is_complete(path: Path) -> bool:
    """True se il worker ha già scritto un JSON valido e parsabile."""
    if not path.exists() or path.stat().st_size < 20:
        return False
    try:
        with path.open("r", encoding="utf-8") as f:
            json.load(f)
        return True
    except Exception:
        return False


def _stop_process(proc: subprocess.Popen[Any]) -> None:
    if proc.poll() is not None:
        return
    try:
        proc.terminate()
        proc.wait(timeout=4)
    except Exception:
        try:
            proc.kill()
        except Exception:
            pass


def _convert_isolated(pdf: Path, out_file: Path, args: argparse.Namespace) -> bool:
    """Converte un PDF in un processo separato.

    Alcune versioni di Tesseract/PyTesseract possono restare vive per qualche
    secondo dopo aver scritto il JSON, soprattutto in batch. Per questo il
    processo padre considera completata la conversione appena il JSON è valido,
    poi termina l'eventuale worker rimasto appeso in fase di shutdown.
    """
    out_file.parent.mkdir(parents=True, exist_ok=True)
    cmd = [
        sys.executable,
        "-m",
        "fiba_pdf_to_json.worker",
        str(pdf),
        str(out_file),
        "--fallback-scale",
        str(args.fallback_scale),
    ]
    if args.no_native_images:
        cmd.append("--no-native-images")
    if args.debug_dir:
        cmd.extend(["--debug-dir", str(Path(args.debug_dir) / pdf.stem)])
    if args.verbose:
        cmd.append("--verbose")

    proc = subprocess.Popen(cmd)
    deadline = time.monotonic() + args.timeout_per_pdf
    json_seen_at: float | None = None
    last_size = -1

    while True:
        rc = proc.poll()
        if rc is not None:
            if rc == 0 and _json_is_complete(out_file):
                logging.info("Creato %s", out_file)
                return True
            if _json_is_complete(out_file):
                logging.warning("Worker terminato con codice %s, ma JSON valido creato: %s", rc, out_file)
                return True
            _write_error(pdf, out_file, f"Errore conversione processo isolato: exit code {rc}")
            logging.error("Errore su %s; creato JSON di errore %s", pdf, out_file)
            return False

        if _json_is_complete(out_file):
            size = out_file.stat().st_size
            now = time.monotonic()
            if size == last_size:
                if json_seen_at is None:
                    json_seen_at = now
                elif now - json_seen_at >= 1.0:
                    _stop_process(proc)
                    logging.info("Creato %s", out_file)
                    return True
            else:
                last_size = size
                json_seen_at = now

        if time.monotonic() > deadline:
            had_json = _json_is_complete(out_file)
            _stop_process(proc)
            if had_json:
                logging.warning("Timeout worker dopo output JSON valido: %s", out_file)
                return True
            _write_error(pdf, out_file, f"Timeout OCR dopo {args.timeout_per_pdf} secondi. Nessun dato affidabile estratto.")
            logging.error("Timeout su %s; creato JSON di errore %s", pdf, out_file)
            return False

        time.sleep(0.5)

def main() -> int:
    args = build_parser().parse_args()
    logging.basicConfig(level=logging.DEBUG if args.verbose else logging.INFO, format="%(levelname)s %(name)s: %(message)s")
    out_dir = Path(args.output)
    pdfs = iter_input_pdfs(args.input)
    if not pdfs:
        logging.warning("Nessun PDF trovato in input")
        return 1

    ok_all = True
    if args.no_isolate:
        from .converter import FibaPdfConverter

        converter = FibaPdfConverter(
            fallback_scale=args.fallback_scale,
            use_native_images=not args.no_native_images,
            debug_dir=args.debug_dir,
        )
        for pdf in pdfs:
            data = converter.convert_pdf(pdf)
            out_file = out_dir / (pdf.stem + ".json")
            converter.save_json(data, out_file)
            logging.info("Creato %s", out_file)
        return 0

    for pdf in pdfs:
        out_file = out_dir / (pdf.stem + ".json")
        ok = _convert_isolated(pdf, out_file, args)
        ok_all = ok_all and ok
    return 0 if ok_all else 2


if __name__ == "__main__":
    raise SystemExit(main())
