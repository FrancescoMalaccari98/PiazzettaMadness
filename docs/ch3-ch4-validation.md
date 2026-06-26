# Validazione end-to-end CH3/CH4 (Fase 10)

Obiettivo: misurare la qualità di ogni canale OCR su PDF reali e tarare
`reconciliation.engineWeights` con dati misurati (non a sensazione). È anche il prerequisito
per rimuovere `known_names.py` (Fase 9 residua).

> Va eseguita **in locale**: richiede i venv OCR e Tesseract, non disponibili nell'ambiente di sviluppo assistito.

## Prerequisiti

- `Config/appsettings.json` con i percorsi reali dei venv e i 4 canali abilitati.
- Venv: `fiba_pdf_to_json_programma_v2/fiba_pdf_to_json/.venv` (CH1/CH2), `.venv-crop`, `.venv-paddle` (CH3/CH4).
- `tesseract --version` funzionante (Tesseract nel PATH).
- Almeno un PDF in `samples/pdf/`.

## Come eseguire

```powershell
# dalla root del repository
.\tools\run-channel-validation.ps1
```

In alternativa, direttamente:

```powershell
dotnet test .\BasketPdfStats.sln --filter "FullyQualifiedName~ChannelValidationHarnessTests" --logger "console;verbosity=detailed"
```

L'harness esegue la pipeline reale (tutti i canali) su ogni PDF di `samples/pdf/` e scrive:

- `TestResults/ChannelValidation/channel-validation-report.md` (leggibile)
- `TestResults/ChannelValidation/channel-validation-report.json` (per ulteriori analisi)

Se l'ambiente OCR non è disponibile, l'harness **esce senza fallire** stampando il motivo (nessun report).

## Come leggere il report

Per ogni PDF:

- **Canale (run)**: stato (`Success`/`Failed`/`Timeout`/`NotConfigured`) e tempo reale in ms per canale.
  - Chiavi: `TesseractFullPage` (CH1), `TesseractLayoutCrops` (CH2), `PaddleLayoutCrops` (CH3), `PaddleTableRows` (CH4).
- **Provider** (per `sourceId`): per i campi dove il reconciler ha confrontato più canali:
  - `Copertura` = quanti campi quel canale ha fornito.
  - `Accordi` = quanti coincidono col valore selezionato dalla riconciliazione.
  - `Conflitti` = forniti ma diversi dal selezionato.
  - `Accordo %` = Accordi / Copertura.
  - Chiavi: `ocr.tesseract.fullpage`, `ocr.tesseract.crop`, `ocr.paddle.crop`, `ocr.paddle.row`.

Mappa run→provider: CH1=`ocr.tesseract.fullpage`, CH2=`ocr.tesseract.crop`, CH3=`ocr.paddle.crop`, CH4=`ocr.paddle.row`.

## Come tarare i pesi

In `Config/appsettings.json` → `reconciliation.engineWeights`:

- Alza il peso (`statWeight`/`scoreWeight`) dei canali con **alta copertura e alto accordo %** sui campi che contano.
- Abbassa il peso dei canali con **molti conflitti** (forniscono valori ma spesso sbagliati) o **bassa copertura**.
- CH4 (`ocr.paddle.row`) resta attivo finché i dati non dimostrano che peggiora la qualità: decidere la rimozione SOLO con benchmark negativi su ≥3 PDF.
- Dopo ogni modifica ai pesi, rilancia l'harness e confronta `Accordo %` e conflitti.

Criterio di accettazione (dal piano): CH3 e CH4 migliorano (o non peggiorano) la qualità finale su almeno 3 PDF campione.

## Risultati campione 5 PDF (Fase 10C — 2026-06-22)

Eseguito in locale su 5 PDF (`SEMIFINALE 1`, `SPR-DEN TABELLINO`, `TABELLINO FINALE 1-2 POSTO`,
`TABELLINO FINALE 3-4 POSTO`, `TABELLINO SEMIFINALE 2`). Tutti i canali **Success**; stato finale
**CompletedWithWarnings** su tutti e 5 (nessun Failed, nessun review-required: harness senza MatchContext).

### Aggregato per provider (su 5 PDF)

| Provider (canale) | Copertura tot | Accordi tot | Conflitti tot | Accordo % | Tempo medio |
|---|---|---|---|---|---|
| `ocr.tesseract.fullpage` (CH1) | 2187 | 1878 | 309 | **85.9%** | ~11.0 s |
| `ocr.tesseract.crop` (CH2) | 32 | 30 | 2 | 93.8% | ~4.8 s |
| `ocr.paddle.crop` (CH3) | 80 | 80 | 0 | **100%** | ~6.5 s |
| `ocr.paddle.row` (CH4) | 1816 | 1813 | 3 | **99.8%** | ~15.9 s |

- Campi riconciliati totali: **2331**. Warnings totali: **2743** (~1.18 per campo). Giocatori: 16/PDF.
- **CH1**: copertura più ampia ma accordo più basso e **tutti i 309 conflitti** → canale "ampio ma rumoroso"
  (resta la fonte anagrafica: nomi/numeri). `statWeight` già basso (0.5).
- **CH4**: copertura altissima sulle righe giocatori, accordo ~99.8%, ma il più lento (~16 s).
- **CH3**: copertura piccola (16 campi/PDF = totali squadra), accordo perfetto, veloce.
- **CH2**: copertura minima (8/PDF, 0 su un PDF), contributo marginale.

### ⚠ Caveat metodologico (importante)

L'`Accordo %` è calcolato **rispetto al valore selezionato dal reconciler, non a una ground truth**.
Poiché CH4 ha `statWeight = 1.0`, i suoi valori vengono spesso *selezionati* → il suo 99.8% è in parte
**auto-confermante**. I 309 conflitti di CH1 sono punti dove CH1 diverge dal selezionato: non sappiamo
(senza i valori reali del PDF) se a sbagliare sia CH1 o il valore selezionato. **Conclusione: i numeri di
accordo NON bastano per ritarare i pesi.**

### Analisi warnings (taxonomia + cosa ispezionare)

Tassonomia emessa dalla pipeline (`ValidationWarning.RuleId` / `Severity`):

| Categoria RuleId | Severità | Significato | Impatto correttezza |
|---|---|---|---|
| `math.*` | Warning | Mismatch di formula (es. `points` ≠ 2·2P+3·3P+FT), collegato a `Stats[].FailedRules` | **Sì — segnale reale** |
| `ocr.*` (`Failed`/`Timeout`/`NotConfigured`/`plan.warning`/`layoutCrops.*`) | Info/Warning | Stato canale e diagnostica preparazione | No (diagnostico) |
| `team.mismatch` | Warning | Squadre OCR ≠ DB | Sì (solo con MatchContext) |
| `team.sideInversionApplied` | Info | Inversione Home/Away corretta | No |

Il rapporto ~1.18 warning/campo suggerisce che la maggior parte sono **per-campo** (statistiche
`NonValidato` / disaccordi tra provider loggati), non errori bloccanti. Lo stato è `CompletedWithWarnings`
(nessun `Error`, nessun review-required), quindi **non ci sono warning di severità Error**.

> I 2743 warning **non erano ancora suddivisi** per severità/categoria nella run iniziale: l'harness è
> stato esteso (Fase 10C) per produrre il breakdown (`Warnings per severità` / `per categoria`, per PDF e
> aggregato). Il breakdown misurato mostra che la stragrande maggioranza sono diagnostici `ocr.*`
> (~2735) e solo **4 sono `math.*`** (gli unici che indicano incoerenze matematiche nel risultato finale).
> La Fase 10D (sotto) ne aggiunge il dettaglio completo nel report.

## Dettaglio warning matematici (Fase 10D — 2026-06-23)

Sui 5 PDF campione la pipeline emette **4 warning `math.*` su 2331 campi riconciliati** — cioè i mismatch
di formula sono rarissimi (~0.17% dei campi). Tutti gli altri warning (~2735) sono diagnostici `ocr.*`
(stato canale, preparazione layout, disaccordi per-campo loggati) e **non** indicano un errore nel
risultato finale.

I `math.*` sono gli unici warning legati alla **correttezza matematica** (collegati a `Stats[].FailedRules`):

| RuleId | Formula verificata |
|---|---|
| `math.pointsFormula` | `points = 2·2P.made + 3·3P.made + FT.made` |
| `math.fieldGoalsMade` | `fieldGoals.made = 2P.made + 3P.made` |
| `math.fieldGoalsAttempted` | `fieldGoals.attempted = 2P.attempted + 3P.attempted` |
| `math.reboundsTotal` | `rebounds.total = rebounds.offensive + rebounds.defensive` (per i **team** è `Info`, non `Warning`) |
| `math.periodSum` | `points = somma punti per periodo` |

### Cosa mostra ora il report

L'harness (`ChannelValidationHarnessTests`) collega ogni warning `math.*` alle statistiche coinvolte
tramite `Validation.Warnings[].FieldIds` → `Stats[].FieldId`. Il report markdown/JSON include una sezione
**`Dettaglio warning matematici`** (per-PDF + aggregata) con, per ogni warning:

- **PDF** di provenienza;
- **entità** coinvolta (giocatore con numero/nome o squadra, + side);
- **regola fallita** (`RuleId`) e **collegamento a `FailedRules`** (`validation.failedRules` e/o `stats[].failedRules`);
- **statistica/e coinvolta/e** (`statKey`, `fieldId`);
- **valore finale scelto** dal reconciler per ciascuna stat;
- **valori disponibili per provider** (`Reconciliation.ProviderValues`), provider scelto e provider concordi;
- **messaggio completo** del warning (include `Valore=…, atteso=…`);
- **severità** (`Warning`/`Info`/`Error`) e **categoria** (`math`);
- flag **bloccante vs diagnostico**.

### Valutazione: bloccante o diagnostico?

Nella pipeline attuale un warning `math.*` ha severità `Warning` (o `Info` per i rimbalzi di squadra),
**non `Error`**. Lo stato finale del file resta `CompletedWithWarnings`: solo una severità `Error`
produrrebbe `CompletedNotValidated`, e solo le voci identità `Conflict`/`NotInPdf` bloccano l'import
(`CompletedWithReviewRequired`). Quindi i 4 `math.*`:

- **sono segnali reali** di incoerenza aritmetica nel risultato finale (vanno ispezionati);
- **non bloccano** automaticamente l'export/import;
- vanno classificati caso per caso — confrontando `valore finale` vs `valori per provider` — come
  **errore reale** (un provider sbagliato è stato selezionato), **falso positivo** (es. rimbalzi di
  squadra non separati nel PDF, o DNP/righe parziali) o **diagnostica non bloccante**.

### I 4 casi concreti (harness eseguito 2026-06-23)

Eseguito `.\tools\run-channel-validation.ps1` sui 5 PDF. I 4 warning `math.*` (3 su `SEMIFINALE 1`,
1 su `SPR-DEN`; gli altri 3 PDF non ne hanno):

| # | PDF | Regola | Entità | Severità | Valore→atteso | Valutazione |
|---|---|---|---|---|---|---|
| 1 | SEMIFINALE 1 | `math.reboundsTotal` | Squadra Miami Spritz [Home] | Info | 35 vs 8+21=29 | **Ambiguità nota** (Info): i totali rimbalzi di squadra FIBA includono spesso "rimbalzi di squadra" non ripartiti off/def. Non bloccante, by design. |
| 2 | SEMIFINALE 1 | `math.pointsFormula` | Squadra Philadelphia 70Sexers [Away] | Warning | 28 vs 2·8+3·3+1=26 | **Gap di estrazione reale**: i punti squadra (28) coincidono col punteggio finale (48-**28**), ma i tiri realizzati estratti (8/3/1) sommano 26 → 2 tiri realizzati non letti. Non è un errore di selezione del reconciler. |
| 3 | SEMIFINALE 1 | `math.periodSum` | Game [Away] | Warning | 28 vs Q1 17 + Q2 9 = 26 | **Gap di estrazione (template periodi)**: catturati solo Q1+Q2 (=26), non tutti i periodi → la somma non raggiunge 28. Coerente col limite noto sui crop periodo/intervallo. Non bloccante. |
| 4 | SPR-DEN | `math.reboundsTotal` | Squadra Denver McNuggets [Away] | Info | 2 vs 6+21=27 | **Probabile misread OCR** del totale rimbalzi squadra (2 invece di ~27), ma severità Info (diagnostico). Fornitore unico TesseractFullPage; nessun altro canale copre questo campo. |

Osservazioni trasversali:

- **Tutti e 4 sono concentrati sui totali squadra / periodi**, non sulle righe giocatore (dove CH4
  `ocr.paddle.row` ha copertura ~1816 e accordo ~99.8%). Le righe giocatore sono matematicamente solide.
- **Nessuno è bloccante**: 2 `Info` (ambiguità rimbalzi squadra) + 2 `Warning` (gap di estrazione). Lo
  stato resta `CompletedWithWarnings` su tutti i 5 PDF.
- I casi 2 e 3 sono **dati mancanti** (tiri/periodi non letti), non valori sbagliati selezionati: il
  campo che "vince" è corretto, manca un addendo. Il caso 4 è un misread su un campo a fornitore singolo.

### Serve tuning dei pesi? (solo proposta)

Con solo **4 mismatch su 2331 campi** (~99.83% coerente) e con i 4 casi tutti riconducibili a
**gap/ambiguità di estrazione su totali squadra e periodi** (non a un canale che vince con il valore
sbagliato contro uno corretto), **non si raccomanda alcun tuning dei pesi**. In nessuno dei 4 casi un
provider alternativo offriva il valore matematicamente corretto che il reconciler ha scartato: dove c'è
disaccordo (caso 2) i provider concordano tra loro (8/3/1) ed è il dato sorgente a mancare un addendo.
Azioni più utili dei pesi: migliorare i crop periodo (caso 3) e la cella totale rimbalzi squadra (caso 4),
e verificare l'estrazione dei tiri realizzati di squadra (caso 2). `reconciliation.engineWeights` resta
invariato.

### Proposta di tuning pesi (SOLO proposta — non applicata)

`Config/appsettings.json` e `reconciliation.engineWeights` **non sono stati toccati**. Proposta condizionata,
da decidere DOPO aver visto il breakdown `math.*` ed eventualmente un controllo a campione sui 309 conflitti CH1:

1. **Non alzare CH4 ora**: il suo accordo è auto-confermante (peso già 1.0). Alzarlo amplificherebbe
   eventuali suoi errori sistematici senza che i numeri attuali lo rivelino.
2. **CH3** (`ocr.paddle.crop`): forte e affidabile sui totali squadra → candidato a restare/salire leggermente
   **solo se** il breakdown mostra `math.*` concentrati sui totali squadra dove CH1 è l'unico altro fornitore.
3. **CH1**: mantenere `statWeight` basso (0.5) — è la fonte anagrafica, non statistica.
4. **CH2**: copertura trascurabile; nessuna modifica utile senza ground truth.

**Azione raccomandata prima di qualsiasi modifica pesi:** ottenere il breakdown `math.*`, mappare i campi
con `FailedRules` ai provider che li hanno forniti, e (idealmente) confrontare 1 PDF con i valori reali.
Solo allora il tuning sarà basato su correttezza e non su accordo circolare.

## Gate per la Fase 9 residua

`known_names.py` può essere rimosso (insieme al fallback in `roster_context.py`) solo **dopo** che la
validazione, eseguita **con `--roster-json` attivo** (cioè con un `MatchContext` reale caricato dal DB),
mostra che il roster dinamico produce risultati equivalenti o migliori rispetto al fallback hardcoded.

> Nota: questo harness (`ChannelValidationHarnessTests`) esegue la pipeline **senza** `MatchContext`,
> quindi CH1 usa ancora il fallback `known_names.py`. La validazione del roster dinamico è stata estratta
> in un harness dedicato — **`RosterContextValidationHarnessTests`** (Fase 10E) — che carica un
> `context.json` reale e passa l'`OcrMatchContext` alla pipeline. Vedere
> [docs/roster-context-validation.md](roster-context-validation.md) per la procedura e il gate completo
> di rimozione di `known_names.py`.
