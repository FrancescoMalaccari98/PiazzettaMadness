# PDF Crop Runner

`pdf_crop_runner` is the generic lightweight crop-only helper used by the shared
document preparation stage.

```powershell
python -m pdf_crop_runner.calibrate_layout `
  --pdf "<pdf>" `
  --word-boxes "<layout-debug-folder>\full-page-word-boxes.json" `
  --layout-map "..\pdf-structure\layout-map.default.json" `
  --calibration-rules "..\pdf-structure\layout-calibration-rules.json" `
  --output-folder "<layout-crops-folder>" `
  --image-output-folder "<layout-images-folder>" `
  --layout-debug-folder "<layout-debug-folder>" `
  --input-mode Crops `
  --max-pages 1
```

Run the command with `pdf_crop_runner/` as the working directory after
`TesseractFullPage` exported its canonical page image and OCR word boxes. The
runner starts from the fixed FIBA structure under `pdf-structure/` and uses the
word boxes only to align known zones. It may apply bounded offset/scale
corrections when anchor evidence is reliable. It does not discover the whole
layout dynamically from OCR.

The runner writes `detected-anchors.json`, numbered usable PNG crops, visual
overlays, a calibration report, a contact sheet, and one calibrated
`crop-metadata.json`. It does not run OCR, call external APIs, load AI models,
or write normalized/final JSON.

Calibration must fail closed when required anchors are missing, ambiguous, or
misaligned. It must report the problem instead of silently generating crops that
look valid. `TesseractLayoutCrops` must consume only successful calibrated crops.

Known table rules include Home table first, Away table second, a fixed
column-header row as top anchor, and `Squadra/Allenatore` plus `Totali` as bottom
anchors. Player-row count is variable and must not define crop height.

`pdf_crop_runner.prepare_crops` remains a low-level fixed-template helper. Active
application preparation uses `pdf_crop_runner.calibrate_layout`.
