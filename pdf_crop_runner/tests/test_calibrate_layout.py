from __future__ import annotations

import argparse
import json
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image


PACKAGE_ROOT = Path(__file__).resolve().parents[1]
REPOSITORY_ROOT = PACKAGE_ROOT.parent
sys.path.insert(0, str(PACKAGE_ROOT))

from pdf_crop_runner.calibrate_layout import (
    calibrate_layout,
    load_calibration_rules,
    normalize_layout_zone,
    normalize_layout_zones,
)


class CalibrateLayoutTests(unittest.TestCase):
    def setUp(self) -> None:
        self.layout_map_path = REPOSITORY_ROOT / "pdf-structure" / "layout-map.default.json"
        self.layout_map = json.loads(self.layout_map_path.read_text(encoding="utf-8"))
        self.rules_path = REPOSITORY_ROOT / "pdf-structure" / "layout-calibration-rules.json"

    def test_real_layout_map_normalizes_all_active_zones(self) -> None:
        zones = normalize_layout_zones(self.layout_map)

        self.assertEqual(15, len(zones))
        self.assertEqual(4, sum(zone["tier"] == "macro" for zone in zones))
        self.assertEqual(11, sum(zone["tier"] == "semantic" for zone in zones))
        self.assertNotIn("header.gameInfo", {zone["cropId"] for zone in zones})
        for zone in zones:
            self.assertTrue(zone["cropId"])
            self.assertTrue(zone["fileName"])
            self.assertEqual({"x", "y", "width", "height"}, set(zone["rectNormalized"]))

    def test_zone_normalizer_accepts_backward_compatible_canonical_shape(self) -> None:
        zone = normalize_layout_zone(
            {
                "cropId": "header.finalScore",
                "fileName": "102-header.finalScore.png",
                "tier": "semantic",
                "cropType": "headerFinalScore",
                "expectedContentType": "finalScore",
                "pageNumber": 1,
                "rectNormalized": {"x": 0.1, "y": 0.2, "width": 0.3, "height": 0.4},
            }
        )

        self.assertEqual("header.finalScore", zone["cropId"])
        self.assertEqual({"x": 0.1, "y": 0.2, "width": 0.3, "height": 0.4}, zone["rectNormalized"])

    def test_calibration_rules_load_and_reference_only_real_layout_zones(self) -> None:
        rules = load_calibration_rules(self.rules_path, self.layout_map)

        self.assertEqual("fixed-structure-with-ocr-geometry-calibration", rules["strategy"])
        self.assertEqual("variable", rules["tableRules"]["playerRows"]["count"])
        self.assertEqual(23, rules["columnCalibration"]["columnCount"])

    def test_manual_calibration_writes_overlay_and_canonical_crop_metadata(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            rendered_page = root / "full-page-rendered.png"
            Image.new("RGB", (600, 900), "white").save(rendered_page)
            geometry = root / "full-page-word-boxes.json"
            geometry.write_text(
                json.dumps(
                    {
                        "strategy": "TesseractFullPage",
                        "coordinateBasis": "full-page-rendered.png",
                        "renderedImageWidth": 600,
                        "renderedImageHeight": 900,
                        "renderedImagePath": str(rendered_page),
                        "words": _reliable_geometry_words(),
                    }
                ),
                encoding="utf-8",
            )
            pdf = root / "sample.pdf"
            pdf.write_bytes(b"%PDF-synthetic-static-test")
            crops = root / "crops"
            images = root / "images"
            debug = root / "debug"

            metadata = calibrate_layout(
                argparse.Namespace(
                    pdf=str(pdf),
                    word_boxes=str(geometry),
                    layout_map=str(self.layout_map_path),
                    calibration_rules=str(self.rules_path),
                    output_folder=str(crops),
                    image_output_folder=str(images),
                    layout_debug_folder=str(debug),
                    document_hash="static-test",
                    original_file_name="sample.pdf",
                    input_mode="manual-calibration",
                    max_pages=1,
                )
            )

            self.assertTrue((debug / "crop-overlay-before.png").exists())
            self.assertTrue((debug / "crop-overlay-after.png").exists())
            self.assertTrue((debug / "layout-calibration-report.json").exists())
            self.assertTrue((debug / "detected-anchors.json").exists())
            self.assertTrue((debug / "contact-sheet-after.html").exists())
            self.assertEqual("Crops", metadata["inputMode"])
            self.assertEqual("manual-calibration", metadata["requestedInputMode"])
            self.assertEqual(15, len(metadata["crops"]))
            self.assertEqual("macro.header", metadata["crops"][0]["cropId"])
            self.assertEqual("001-macro.header.png", metadata["crops"][0]["fileName"])
            self.assertTrue(all(crop["cropId"] and crop["fileName"] for crop in metadata["crops"]))
            self.assertEqual("Calibrated", metadata["layoutCalibration"]["status"])
            self.assertTrue(all(crop["usable"] for crop in metadata["crops"]))

            report = json.loads((debug / "layout-calibration-report.json").read_text(encoding="utf-8"))
            self.assertEqual(15, len(report["zones"]))
            self.assertEqual("macro.header", report["zones"][0]["cropId"])
            self.assertEqual("Aligned", report["geometryAlignment"]["status"])
            self.assertEqual("Reliable", report["detectedAnchors"]["finalScore"]["status"])

    def test_geometry_dimension_mismatch_fails_closed_without_generating_crops(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            rendered_page = root / "full-page-rendered.png"
            Image.new("RGB", (600, 900), "white").save(rendered_page)
            geometry = root / "full-page-word-boxes.json"
            geometry.write_text(
                json.dumps(
                    {
                        "strategy": "TesseractFullPage",
                        "coordinateBasis": "full-page-rendered.png",
                        "renderedImageWidth": 300,
                        "renderedImageHeight": 450,
                        "renderedImagePath": str(rendered_page),
                        "words": _reliable_geometry_words(),
                    }
                ),
                encoding="utf-8",
            )
            pdf = root / "sample.pdf"
            pdf.write_bytes(b"%PDF-synthetic-static-test")
            crops = root / "crops"
            metadata = calibrate_layout(
                argparse.Namespace(
                    pdf=str(pdf),
                    word_boxes=str(geometry),
                    layout_map=str(self.layout_map_path),
                    calibration_rules=str(self.rules_path),
                    output_folder=str(crops),
                    image_output_folder=str(root / "images"),
                    layout_debug_folder=str(root / "debug"),
                    document_hash="static-test",
                    original_file_name="sample.pdf",
                    input_mode="manual-calibration",
                    max_pages=1,
                )
            )

            self.assertEqual("Failed", metadata["layoutCalibration"]["status"])
            self.assertEqual([], metadata["crops"])
            self.assertEqual([], list(crops.glob("*.png")))


def _reliable_geometry_words() -> list[dict[str, object]]:
    words: list[dict[str, object]] = []

    def add(text: str, x: int, y: int, line: int) -> None:
        words.append(
            {
                "text": text,
                "x": x,
                "y": y,
                "width": max(8, len(text) * 6),
                "height": 12,
                "confidence": 95.0,
                "blockIndex": 1,
                "paragraphIndex": 1,
                "lineIndex": line,
                "wordIndex": len(words) + 1,
            }
        )

    for text, x in [("Miami", 190), ("Spritz", 225), ("51", 275), ("-", 295), ("53", 310), ("Spurs", 335)]:
        add(text, x, 115, 1)
    for y, line in [(270, 2), (520, 3)]:
        for text, x in [("Nome", 70), ("Min.", 140), ("Tiri", 190), ("Punti", 250), ("Liberi", 310), ("Rimbalzi", 370), ("AS", 450)]:
            add(text, x, y, line)
    add("Squadra/Allenatore", 80, 430, 4)
    add("Totali", 80, 448, 5)
    add("Squadra/Allenatore", 80, 645, 6)
    add("Totali", 80, 665, 7)
    add("24", 200, 225, 8)
    add("24", 230, 225, 8)
    return words


if __name__ == "__main__":
    unittest.main()
