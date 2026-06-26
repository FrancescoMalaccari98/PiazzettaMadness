# Validazione roster dinamico reale (Fase 10E)

Obiettivo: dimostrare che la pipeline usa il **roster dinamico del DB** (`OcrMatchContext`) come fonte
primaria di identità, e che `known_names.py` resta **solo fallback**. È il prerequisito di correttezza
per rimuovere `known_names.py` (Fase 9 residua).

> Va eseguita **in locale**: richiede i venv OCR + Tesseract e i PDF reali della partita. I dati DB
> reali e i PDF **non vengono committati** (cartella `samples/private/` ignorata da git).

## Differenza rispetto alla Fase 10 (CH1–CH4)

| | Fase 10/10C/10D (`ChannelValidationHarnessTests`) | Fase 10E (`RosterContextValidationHarnessTests`) |
|---|---|---|
| MatchContext | **assente** → CH1 usa fallback `known_names.py` | **presente** (da `context.json`) → CH1 usa roster DB |
| Scopo | qualità canali / pesi / warning math | uso roster dinamico + matching identità per-squadra |
| Input | `samples/pdf/*.pdf` | `samples/private/db-context/<match>/{context.json, pdfs/*.pdf}` |

## Dove mettere i dati (cartella privata, non committata)

```text
samples/private/db-context/aurora-lynx-nebula-bears-08-lug-2030/
  context.json      <- copia di context.template.json compilata con i dati REALI del DB
  pdfs/             <- i 5 PDF reali della partita (uno per momento di gara)
```

`samples/private/` è in `.gitignore`: PDF reali e context con identità canoniche non finiscono mai in git.
È fornito `context.template.json` (placeholder) da copiare in `context.json` e compilare.

## Formato esatto di `context.json`

È un **superset** del modello C# `OcrMatchContext` (camelCase). I campi che `OcrMatchContext` non ha
(`matchDate`, `time`, `group`, `displayName`) sono **metadati**: l'harness li riporta, ma C# e
`roster_context.py` li ignorano (deserializzazione tollerante ai campi extra). Nessuna modifica allo
schema `OcrMatchContext`.

```json
{
  "matchId": 1042,
  "matchDate": "2030-07-08",
  "time": "20:30",
  "group": "GIRONE A",
  "homeTeam": {
    "teamId": 11,
    "name": "AURORA LYNX",
    "players": [
      { "playerId": 2001, "teamId": 11, "jerseyNumber": 4, "firstName": "Marco", "lastName": "Bianchi", "displayName": "M. Bianchi" }
    ]
  },
  "awayTeam": {
    "teamId": 12,
    "name": "NEBULA BEARS",
    "players": [
      { "playerId": 3001, "teamId": 12, "jerseyNumber": 6, "firstName": "Luca", "lastName": "Verdi", "displayName": "L. Verdi" }
    ]
  }
}
```

Campi richiesti per ogni giocatore: `playerId`, `teamId`, `jerseyNumber`, `firstName`, `lastName`
(+ `displayName` se disponibile). Il `playerId` e il `teamId` sono gli **ID canonici del DB**.

> ⚠ I valori (matchId, teamId, playerId, nomi, numeri) devono venire dal **DB reale** (endpoint PHP
> `GET /api-ocr/matches/{matchId}/context`). Non vanno inventati: senza dati reali l'harness esce in SKIP.

### Dati DB che servono e che il template NON contiene

`matchId`, `homeTeam.teamId`, `awayTeam.teamId` e, per ogni giocatore, `playerId` + `jerseyNumber` +
`firstName`/`lastName`. Il template ha questi a `0`/`PLACEHOLDER`: vanno sostituiti con i valori del DB.

## Come eseguire la validazione

```powershell
# dalla root del repository (dopo aver compilato context.json + messo i PDF in pdfs/)
dotnet test .\BasketPdfStats.sln --filter "FullyQualifiedName~RosterContextValidationHarnessTests" --logger "console;verbosity=detailed"
```

Output: `TestResults/RosterContextValidation/roster-context-report.{md,json}`.
Se mancano `context.json`/PDF o l'ambiente OCR, l'harness **esce senza fallire** (SKIP con istruzioni).

## Cosa verifica il report

Per ogni PDF della partita:

- **context provided**: sì (OcrMatchContext passato alla pipeline);
- **context source**: JSON fixture (`context.json`);
- **matchId**, **home/away team** (+ teamId), **roster players per team**, **PDF processati**;
- **CH1 roster source**: `dynamic-context` / `legacy-known-names` / `none` / `unknown` — dedotto dai log
  stderr del worker CH1 (`Roster dinamico attivo` vs marker di fallback `known_names`), catturati in
  `OcrRun.Error`;
- **fallback known_names.py used**: yes/no (stessi marker);
- **matching giocatori** per lato: `certain` / `probable` / `conflict` / `numberNotFound` / `unmatched`
  + `notInPdf` (giocatori DB non trovati nel PDF), via `PlayerIdentityMatcher`;
- **review required**: yes/no (`CompletedWithReviewRequired` o presenza di `Conflict`/`NotInPdf`);
- **nessun playerId inventato da Python**: i giocatori OCR hanno `entityId` in formato OCR
  (`player:...`); i `playerId` canonici DB compaiono solo in `IdentityReview` (risolti da C#) e devono
  appartenere al roster del DB;
- **matching per-squadra, non combinato**: `crossTeamLeak` = candidati abbinati a una squadra diversa
  da quella del lato (deve essere **0**). Il matcher riceve il roster del **solo** lato (Home vs roster
  Home, Away vs roster Away), come fa `IdentityReviewBuilder`.

## Catena tecnica (come il roster dinamico arriva a CH1)

1. C# costruisce `OcrMatchContext` da `context.json`.
2. `PdfProcessingPipeline.ProcessPdfAsync(pdf, selection, matchContext)` lo mette in `OcrProcessingRequest.MatchContext`.
3. `TesseractFullPageOcrEngine` (CH1) serializza il contesto in `roster-context.json` e passa
   `--roster-json <path>` al worker **solo se** MatchContext è presente.
4. `worker.py` chiama `roster_context.load_and_activate(path)`: se il roster è valido logga
   `Roster dinamico attivo: N squadre, M giocatori.`; altrimenti logga il fallback `known_names`.
5. `parser.py` usa `roster_context.*` (teams/players/jersey/fuzzy): roster dinamico se attivo, altrimenti `known_names`.
6. Il matching canonico (riga OCR → `playerId` DB) avviene in **C#** (`PlayerIdentityMatcher`),
   non in Python: Python non emette `playerId` canonici.

## Cosa manca prima di poter rimuovere `known_names.py`

`known_names.py` (e il fallback in `roster_context.py`) si rimuovono **solo dopo** che questo harness,
eseguito con `context.json` reale, mostra su ≥1 partita reale (idealmente i 5 PDF della stessa gara):

1. `CH1 roster source = dynamic-context` su **tutti** i PDF (mai `legacy-known-names`);
2. `fallback known_names used = no` ovunque;
3. matching identità per-squadra corretto: `crossTeamLeak = 0`, niente `playerId` inventati;
4. risultati equivalenti o migliori rispetto al fallback (meno `conflict`/`unmatched`/`notInPdf`
   ingiustificati) — confronto con la run senza contesto della Fase 10C.

Finché questi punti non sono verificati con dati reali, `known_names.py` **resta** come fallback
(vincolo di progetto). Questa Fase 10E fornisce lo strumento; l'esecuzione con i dati DB reali è a
carico dell'utente in locale.

## Risultati esecuzione reale (2026-06-24)

Eseguito su **matchId 63 — Aurora Lynx (Home, teamId 46) vs Nebula Bears (Away, teamId 47)**,
2026-07-08 20:30, Girone A. Roster DB: 8 + 8 giocatori. PDF reali: 5 (4 quarti + 1 parziale). Durata ~3m47s.
Esito harness: **passato**. `context.json` da dump DB reale (nessun dato inventato).

| PDF | Stato | CH1 roster source | Fallback known_names | Review | playerId inventato | Home (certain/…) | Away (certain/…) | crossTeamLeak |
|---|---|---|---|---|---|---|---|---|
| Tabellino 1 quarto | CompletedWithWarnings | dynamic-context | no | no | no | 8/8 | 8/8 | 0 |
| Tabellino 2 quarto | CompletedWithWarnings | dynamic-context | no | no | no | 8/8 | 8/8 | 0 |
| Tabellino 3 quarto | CompletedWithWarnings | dynamic-context | no | no | no | 8/8 | 8/8 | 0 |
| Tabellino 4 quarto | CompletedWithWarnings | dynamic-context | no | no | no | 8/8 | 8/8 | 0 |
| Tabellino parziale 3 quarto | CompletedWithWarnings | dynamic-context | no | no | no | 8/8 | 8/8 | 0 |

Log worker su tutti i PDF: `INFO fiba_pdf_to_json.roster_context: Roster dinamico attivo: 2 squadre, 16 giocatori.`
Matching aggregato: **80/80 certain** (16 giocatori × 5 PDF), 0 probable/conflict/numberNotFound/unmatched/notInPdf,
0 crossTeamLeak, 0 playerId inventati.

**Gate per rimuovere `known_names.py`: i 4 criteri sono soddisfatti** su questa partita reale
(dynamic-context ovunque, nessun fallback, matching per-squadra corretto, nessun `playerId` inventato).
La rimozione effettiva di `known_names.py` + fallback resta un'azione **Fase 9** che richiede approvazione
esplicita dell'utente (vincolo di progetto): non è inclusa in questa fase. Raccomandazione: validare su
almeno un'altra partita prima della rimozione, per coprire più combinazioni di nomi/numeri.
