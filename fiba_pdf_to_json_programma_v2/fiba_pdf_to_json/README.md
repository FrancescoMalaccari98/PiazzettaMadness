# fiba-pdf-to-json

Programma locale Python per convertire PDF di tabellini basket generati da **FIBA Live Stats** in file JSON strutturati per analisi statistiche.

Questa versione è pensata per il layout dei PDF di esempio di Piazzetta Madness: tabellino FIBA a 1 pagina, renderizzato come immagine, con due tabelle giocatori e due tabelle comparative finali.

## Cosa migliora questa versione

La prima versione usava troppo OCR generico. Questa versione sfrutta il fatto che il layout è stabile:

1. estrae l'immagine nativa della pagina PDF, senza ricampionarla inutilmente quando possibile;
2. riconosce le griglie della pagina con OpenCV;
3. usa coordinate/layout fissi come fallback quando la griglia non viene rilevata perfettamente;
4. legge header, titoli squadra, tabelle giocatori e statistiche comparative da regioni separate;
5. legge le tabelle **per colonna**, non come blocco unico: ogni colonna usa whitelist OCR coerenti con il tipo di dato;
6. assegna i valori alle righe usando le coordinate della griglia;
7. corregge errori OCR frequenti con regole deterministicamente controllabili:
   - percentuale incoerente con `realizzati/tentati`;
   - `tiri_dal_campo` incoerente con somma `2P + 3P`;
   - totali squadra mancanti compilati sommando i giocatori;
   - segno meno del `+/-` letto geometricamente quando OCR lo perde;
   - nomi squadra/giocatori corretti tramite dizionario/fuzzy matching basato sugli esempi;
8. mantiene warning espliciti quando un valore viene corretto o stimato.

Non usa API cloud. Tutta la conversione gira in locale.

## Requisiti di sistema

- Python 3.10+
- Tesseract OCR installato localmente
- Lingue Tesseract `ita` e `eng` consigliate. Il programma usa principalmente `eng` perché i tabellini hanno molte sigle e numeri.

### Installazione Tesseract

Ubuntu/Debian:

```bash
sudo apt-get update
sudo apt-get install -y tesseract-ocr tesseract-ocr-ita tesseract-ocr-eng
```

macOS con Homebrew:

```bash
brew install tesseract tesseract-lang
```

Windows:

1. installa Tesseract, ad esempio build UB Mannheim;
2. aggiungi `tesseract.exe` al `PATH`;
3. verifica con:

```bash
tesseract --version
```

## Installazione programma

Da questa cartella:

```bash
python -m venv .venv
source .venv/bin/activate      # Windows: .venv\Scripts\activate
pip install -r requirements.txt
pip install -e .
```

## Esecuzione

PDF singolo:

```bash
fiba-pdf-to-json "SEMIFINALE 1.pdf" -o output_json
```

Cartella di PDF:

```bash
fiba-pdf-to-json ./pdf_input -o ./json_output --timeout-per-pdf 180
```

Esecuzione senza installazione editable:

```bash
python -m fiba_pdf_to_json.cli ./pdf_input -o ./json_output
```

Debug immagini intermedie:

```bash
fiba-pdf-to-json ./pdf_input -o ./json_output --debug-dir ./debug_ocr
```

## Output

Per ogni PDF viene generato un file JSON con lo stesso nome base del PDF.

Esempio:

```text
pdf_input/SEMIFINALE 1.pdf
json_output/SEMIFINALE 1.json
```

La struttura JSON include:

- `metadata`
- `partita`
- `risultato`
- `squadre[]`
- `statistiche_comparative`
- `affidabilita`

Nella cartella `sample_output/` trovi esempi prodotti sui quattro PDF forniti.

## Note sul parsing

- I PDF FIBA Live Stats analizzati hanno griglia stabile: due tabelle giocatori centrali e due tabelle comparative finali.
- La legenda in fondo è ignorata.
- I giocatori `N.E.` sono inclusi con `non_entrato = true` e statistiche nulle.
- I minuti `MM:SS` restano in `raw` e vengono convertiti in secondi se riconosciuti.
- Le percentuali `23,8` vengono convertite in `23.8`.
- I tiri `realizzati/tentati` sono separati in `realizzati`, `tentati`, `percentuale`.
- Se l'OCR legge una percentuale in modo incoerente con il rapporto tiri, il programma ricalcola la percentuale e registra un warning.
- Se il totale di squadra è mancante ma le righe giocatori sono leggibili, il programma compila il totale sommando i giocatori e registra un warning.

## Moduli principali

- `image_loader.py`: estrae immagine nativa dal PDF o renderizza fallback con PyMuPDF;
- `layout.py`: rileva linee/griglie/tabelle e contiene fallback relativi al layout fisso;
- `ocr.py`: preprocessing immagini, OCR Tesseract e lettura colonne/celle;
- `parser.py`: parsing header, squadre, giocatori, totali e comparative;
- `normalizers.py`: conversioni numeriche, percentuali, minuti, nomi file;
- `roster_context.py`: roster dinamico dal DB (via `--roster-json`) + fuzzy matching nomi/squadre, supporto al parsing (sostituisce `known_names.py`, rimosso in Fase 9);
- `schema.py`: template JSON finale;
- `cli.py`: interfaccia comando;
- `worker.py`: conversione isolata di un PDF.

## Configurazione e manutenzione

Per adattare il programma a variazioni di layout, parti da:

- `layout.default_player_x_lines()` per il profilo colonne;
- `parser.PLAYER_COLUMNS` e `parser.COLUMN_KINDS` per mapping colonne e tipo OCR;
- `parse_header_regions()` per le coordinate dell'header;
- `_ocr_comparative_table()` e `parse_comparatives()` per le tabelle comparative.

I nomi di squadre/giocatori non sono più hardcoded: provengono dal DB tramite `--roster-json`
(`roster_context.py`). Per aggiungere una partita, fornisci il roster dal DB, non un file di nomi.

## Errori e warning

Il programma non interrompe l'estrazione per celle incerte. Compila il più possibile e registra in:

```json
"affidabilita": {
  "livello_globale": "media",
  "note": [],
  "warnings": [
    {
      "campo": "squadre.Miami Spritz.giocatori.Raoul PIERGENTILI.tiri_dal_campo",
      "motivo": "Tiri dal campo OCR incoerenti con 2P+3P: usata somma delle colonne 2P e 3P.",
      "affidabilita": "media"
    }
  ]
}
```

Livelli:

- `alta`: layout e valori chiari, nessun warning rilevante;
- `media`: alcune celle corrette, stimate o compilate con regole di fallback;
- `bassa`: tabella non rilevata, rendering fallito o dati molto incompleti.

## Nota pratica

Tesseract può essere sensibile a versione, font e sistema operativo. Per batch lunghi il comando usa un worker separato per PDF e un timeout configurabile con `--timeout-per-pdf`.
