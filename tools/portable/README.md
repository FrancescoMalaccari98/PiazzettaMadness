# BasketPdfStats portable build

Run this from Windows, preferably after copying the repository from the shared
UTM folder to a local path such as `C:\dev\PiazzettaMadness`.

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\tools\portable\Build-Portable.ps1 -Runtime win-arm64
```

Use `win-arm64` for the UTM Windows VM on Apple Silicon. Use `win-x64` for a
normal Intel/AMD Windows PC.

The script creates:

```text
artifacts\portable\BasketPdfStats-win-arm64\
```

The app runtime is self-contained, so the final PC does not need the .NET
runtime installed. OCR tools still need to be placed inside the generated
`tools\` folder:

- `tools\python-tesseract\Scripts\python.exe`
- `tools\tesseract\tesseract.exe`
- `tools\tesseract\tessdata\eng.traineddata`
- optionally `tools\python-paddle\Scripts\python.exe`
- optionally `tools\paddle-models\`

The generated `Config\appsettings.json` uses only relative paths and has remote
DB import disabled.

From the generated portable folder, set up the Tesseract Python environment with:

```powershell
.\tools\portable\Setup-TesseractPython.ps1
```

Run it from the folder that contains `BasketPdfStats.App.exe`.
