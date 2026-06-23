# Piano di refactoring — BasketPdfStats

Data analisi: 2026-06-18  
Stato: approvato — nessun nuovo ciclo di pianificazione senza blocco tecnico dimostrato

---

## 1. Obiettivi

1. Fare del database la fonte canonica per identità di partite, squadre e giocatori.
2. Caricare il roster completo dal DB prima di avviare l'OCR.
3. Sostituire la discovery autonoma dell'identità da OCR con un matching controllato sul roster DB.
4. Abilitare la selezione di qualsiasi data (non solo "oggi") per il caricamento delle partite.
5. Gestire le associazioni dubbie in modo revisionale e tracciabile.
6. Usare ID canonici DB nel JSON finale e nel payload di import.
7. Eliminare il codice legacy in modo sicuro e incrementale.
8. Estrarre la composition root dalla UI prima di aggiungere nuovi servizi.
9. Lasciare il progetto compilabile e i test verdi dopo ogni fase.

---

## 2. Baseline tecnica (Fase 0A — completata il 2026-06-18)

### 2.1 Controlli eseguiti e risultati

| Controllo | Risultato |
|-----------|-----------|
| `dotnet build BasketPdfStats.sln` | 6 progetti compilati, 0 errori, 1 warning |
| Warning build | xUnit2029 in `NormalizedOcrReconcilerTests.cs` riga 36 (uso di Assert obsoleto) |
| `dotnet test --filter "Category!=Integration"` | **172 passed, 0 failed, 0 ignored** |
| `dotnet test --filter "Pipeline\|Tesseract\|Preparation"` | **40 passed, 0 failed, 2 ignored** |
| CH1 peso stat/score/default | 0.5 / 0.7 / 0.6 |
| CH2 peso stat/score/default | 0.85 / 1.0 / 0.85 |
| CH3 peso stat/score/default | 0.9 / 0.95 / 0.9 |
| CH4 peso stat/score/default | 1.0 / 0.8 / 1.0 |
| Timeout CH1 | 180 s |
| Timeout CH3+CH4 | 300 s |
| Token API | Presente in `Config/appsettings.json`; file in git tracking su 3 commit |
| `matchLookupDate` in config | "2026-07-08" (override hardcoded, non rimuovere ancora) |

### 2.2 Controlli non eseguiti — motivo e rischio residuo

| Controllo non eseguito | Motivo | Rischio |
|------------------------|--------|---------|
| Elaborazione PDF campione end-to-end (CH1–CH4) | Richiede venv attivi e PDF in `samples/pdf/` | **Medio** — tempi reali CH1–CH4, output JSON baseline, warning di elaborazione e validation error non misurati |
| Confronto output CH3/CH4 vs CH1/CH2 | Dipende dall'elaborazione E2E | Medio — qualità Paddle non validata |
| Verifica routing `.htaccess` su Aruba | Richiede accesso al pannello Aruba | Basso — funzionamento locale confermato |
| Test `[Category=Integration]` | Richiedono Python, venv, Tesseract attivi | Basso — non richiesti per lo sviluppo normale |

I timeout configurati (180 s, 300 s) sono parametri di config, non tempi di esecuzione misurati.  
La validazione end-to-end resta nella Fase 10.

### 2.3 Stato canali OCR

| # | Classe | Stato | Note |
|---|--------|-------|------|
| CH1 | TesseractFullPageOcrEngine | Validato | Fonte identità OCR corrente (da sostituire con DB) |
| CH2 | TesseractLayoutCropsOcrEngine | Parzialmente validato | finalScore OK; teamTotals spesso silent |
| CH3 | PaddleCropOcrEngine | Implementato | Non validato end-to-end su PDF reale |
| CH4 | PaddleRowOcrEngine | Implementato | Non validato end-to-end su PDF reale |

---

## 3. Flusso target

```
Avvio applicazione
  → data predefinita = oggi
  → GET elenco partite per la data
  → selezione partita
  → GET contesto partita e roster dal backend
  → creazione contesto canonico in memoria
  → selezione PDF
  → CH1
  → confronto squadre OCR/DB
  → rilevamento: ordine normale / inversione / mismatch
  → eventuale warning continua/annulla
  → CH2 + CH3 + CH4
  → riconciliazione delle statistiche
  → matching giocatori contro il roster DB
  → eventuale revisione manuale
  → validazioni matematiche e di completezza roster
  → CanonicalGameResult
  → JSON locale
  → POST import con ID canonici
  → verifica PHP
  → salvataggio nel database
```

**Principio obbligatorio:**
```
Il DB decide chi sono partita, squadra e giocatore.
L'OCR estrae le statistiche.
Il C# associa OCR e identità canoniche.
Il PHP verifica e salva.
```

---

## 4. Architettura target

```
┌──────────────────────────────────────────────────────────────┐
│  App (WPF)                                                   │
│  AppComposition — MainViewModel — UI controls                │
│  DatePicker → MatchComboBox → PdfPicker → ProcessButton      │
│  IdentityReviewPanel (Fase 7B)                               │
└───────────────────┬──────────────────────────────────────────┘
                    │
┌───────────────────▼──────────────────────────────────────────┐
│  Core                                                        │
│  IPdfProcessingPipeline   IOcrEngine   IStatsValidator       │
│  IOcrImportService        OcrMatchContext                    │
│  PlayerIdentityMatcher    TeamIdentityMatcher                │
│  CanonicalStatKeyMapper   NameNormalizer                     │
│  ProcessingResult → CanonicalGameResult                      │
└──────────┬────────────────────────────┬──────────────────────┘
           │                            │
┌──────────▼──────────┐     ┌───────────▼──────────────────────┐
│  Infrastructure      │     │  Ocr.TesseractPython             │
│  PdfProcessingPipeline    │  TesseractFullPageOcrEngine (CH1) │
│  NormalizedOcrReconciler  │  TesseractLayoutCropsOcrEngine(CH2│
│  StatsValidationService   │  PaddleCropOcrEngine (CH3)        │
│  OcrImportService         │  PaddleRowOcrEngine (CH4)         │
│  DocumentPreparationStage │  PythonProcessRunner              │
│  AppComposition (Fase 2)  └───────────────────────────────────┘
└──────────┬──────────┘
           │ HTTP Bearer
┌──────────▼──────────────────────────────────────────────────┐
│  PHP Backend (backend/)                                      │
│  GET  /api-ocr/matches/today?date=YYYY-MM-DD  (esiste)      │
│  GET  /api-ocr/matches/{id}/context           (Fase 3)      │
│  POST /api-ocr/import/{id}                    (aggiornato 8)│
└──────────┬──────────────────────────────────────────────────┘
           │ PDO
┌──────────▼──────────────────────────────────────────────────┐
│  MySQL: matches, teams, players, team_rosters                │
│         match_player_stats, match_team_stats                 │
└─────────────────────────────────────────────────────────────┘
```

---

## 5. Ruolo del database

Il database MySQL è la fonte canonica e immutabile per:

- **Partite**: ID, data/ora, fase, sede
- **Squadre**: ID, nome ufficiale
- **Giocatori**: ID, nome, cognome
- **Numeri di maglia**: da `team_rosters.jersey_number`

**Regola assoluta: l'OCR estrae statistiche e le associa ad entità già note. Non crea entità.**

Quando il DB non è disponibile:
- `GetMatchesForDateAsync` fallisce → lista partite vuota + messaggio di errore
- `GetMatchContextAsync` fallisce → ProcessButton disabilitato + messaggio di errore
- Nessun fallback al vecchio flusso basato solo su OCR

---

## 6. Chiamate al backend

### Chiamata 1 — Elenco partite (esiste già)

```
GET /api-ocr/matches/today?date=YYYY-MM-DD
Authorization: Bearer {token}

Response 200:
[
  { "matchId": 42, "homeTeam": { "id": 5, "name": "Virtus" },
                   "awayTeam": { "id": 7, "name": "Pallacanestro" } }
]
```

In Fase 1 cambia solo il lato C#: il parametro `date` proviene dal DatePicker.  
L'endpoint PHP non cambia se il parametro `?date=` funziona già correttamente.

### Chiamata 2 — Contesto partita (Fase 3 — nuova)

```
GET /api-ocr/matches/{matchId}/context
Authorization: Bearer {token}

Response 200:
{
  "matchId": 42,
  "homeTeam": {
    "teamId": 5,
    "name": "Virtus",
    "players": [
      { "playerId": 101, "teamId": 5, "firstName": "Mario", "lastName": "Rossi",
        "jerseyNumber": 3 }
    ]
  },
  "awayTeam": {
    "teamId": 7,
    "name": "Pallacanestro",
    "players": [ ... ]
  }
}

Errori:
  401 — token mancante o non valido
  404 — matchId non esiste
  409 — roster incompleto (home o away vuoto)
  500 — errore DB
```

Campi esplicitamente esclusi (non necessari al C# per matching e OCR):
`phase`, `scheduled_at`, `short_name`, `is_starter`, `is_captain`.
Aggiungere solo se una fase successiva dimostra la necessità.

Il contesto viene scaricato una volta sola alla selezione della partita e conservato in memoria.  
CH1–CH4, matching, revisione e validazioni non effettuano chiamate aggiuntive al backend.

### Chiamata 3 — Import finale (aggiornata in Fase 8)

```
POST /api-ocr/import/{matchId}
Authorization: Bearer {token}
Content-Type: application/json

Body: ImportPayload con ID canonici (matchId, teamId, playerId)

Response 200 — import riuscito
Response 422 — playerIds non validi per la partita/squadra (lista errori non sensibili)
Response 401/404/500 — standard
```

**Strategia definitiva:**
- C# risolve `playerId` (unico punto di matching)
- PHP non esegue fuzzy matching; verifica solo che ogni ID esista in `team_rosters`
- Nessun secondo algoritmo indipendente di matching
- Incoerenze restituiscono HTTP 422 con dettagli non sensibili

---

## 7. Confine C# / Python

### Python (workers OCR)

- Esegue OCR; legge numero, nome, valori, coordinate
- Produce `evidence_records.json` (entityId OCR-style: `player:Home:jersey:3`)
- Può usare il roster dinamico **solo come supporto al parsing** (es. validare plausibilità di un numero)
- **Non assegna `playerId`**
- **Non crea identità canoniche**
- **Non decide definitivamente il giocatore**

### C# (EvidenceRecordMapper, PlayerIdentityMatcher, NormalizedOcrReconciler)

- Riceve le evidenze
- Confronta jersey + nome con il roster DB
- Produce `CertainMatch`, `ProbableMatch`, `Conflict` o `NotFound`
- Gestisce la revisione manuale
- Costruisce il risultato canonico (`CanonicalGameResult`)

### Passaggio del roster ai worker

Non passare automaticamente `--roster-json` a tutti i worker.  
Per ciascun canale stabilire se il roster è effettivamente necessario:

| Canale | Roster utile? | Uso |
|--------|--------------|-----|
| CH1 | Sì | Guida lettura nomi/numeri (support parsing) |
| CH2 | No | Produce solo crop evidence; matching in C# |
| CH3 | No | Produce solo crop evidence; matching in C# |
| CH4 | No | Produce solo row evidence; matching in C# |

---

## 8. Sostituzione `known_names.py`

**Non spostare le funzioni direttamente inline in `parser.py`.**

Creare un componente dedicato `roster_context.py` con responsabilità limitate:
- Caricare il roster JSON dinamico (da `--roster-json`)
- Normalizzare nomi
- Fornire candidati per un numero di maglia
- Verificare plausibilità di numeri e nomi durante il parsing
- Aiutare il parsing di CH1
- Non produrre ID canonici

**Fase 5:**
- `roster_context.py` carica il roster dinamico
- `parser.py` usa `roster_context.py` invece di `known_names.py` quando `--roster-json` è presente
- `known_names.py` disabilitato con fallback esplicito (non silenzioso)
- Fallback legacy mantenuto solo durante la migrazione e chiaramente segnalato nei log

**Fase 9:**
- Eliminazione definitiva di `known_names.py`
- Eliminazione del fallback legacy
- Pulizia di tutti i riferimenti e dei test legacy

---

## 9. Matching identità giocatori (PlayerIdentityMatcher)

### Input

Riga OCR: jersey OCR (`string`), nome OCR grezzo (`string`).  
Roster DB: lista di `OcrRosterPlayer` per la squadra.

### Algoritmo

1. Normalizza jersey OCR: rimuovi caratteri non numerici
2. Trova giocatori DB con stesso numero di maglia
3. Normalizza nome OCR: rimuovi accenti, apostrofi→spazio, trattini→spazio, lowercase, strip
4. Calcola distanza fuzzy (Levenshtein o token-set ratio) tra nome OCR normalizzato e `fullName` DB normalizzato
5. Classifica esito:

| Esito | Condizione |
|-------|-----------|
| `CertainMatch` | Jersey OK **e** distanza nome ≤ 15% |
| `ProbableMatch` | Jersey OK **e** distanza nome ≤ 30% |
| `Conflict` | Jersey OK **e** distanza nome > 30% |
| `NumberNotFound` | Nessun giocatore DB con quel jersey |
| `Unmatched` | Riga OCR senza jersey leggibile |

### Normalizzazione nomi

```
à→a  è→e  é→e  ì→i  ò→o  ù→u
apostrofi/trattini → spazio
lowercase, rimuovi spazi multipli
sostituzioni OCR comuni: 0→O, 1→I, rn→m (solo se migliorano il match)
```

### Soglie

15% e 30% sono valori di partenza da validare su `samples/pdf/`.  
Non modificarli senza prima misurare su campioni reali.

### Giocatori DB non trovati nel PDF

- Non inserire nel JSON con valori null/zero
- Generare `IdentityReviewItem` con motivo `NotInPdf`
- Segnalare nel risultato come caso anomalo

---

## 10. Team identity matching (TeamIdentityMatcher)

**Timing: dopo CH1, non pre-OCR.**

1. CH1 legge i nomi delle squadre dalla testata del PDF
2. `TeamIdentityMatcher` riceve i nomi OCR da CH1
3. Confronta normalizzato fuzzy con HomeTeam DB e AwayTeam DB
4. Casi:
   - **Match corretto**: procede senza interruzione
   - **Inversione rilevata**: nome Home OCR corrisponde ad AwayTeam DB (e viceversa)
   - **Mismatch**: nessun team DB corrisponde ai nomi OCR

### Inversione home/away

Rilevamento:
- Punteggio corretto = `sim(HomeOcr, HomeDB) + sim(AwayOcr, AwayDB)`
- Punteggio inversione = `sim(HomeOcr, AwayDB) + sim(AwayOcr, HomeDB)`
- Se punteggio inversione > punteggio corretto → `SideInversionDetected = true`

Riallineamento:
- Tutti gli entityId `team:Home` ↔ `team:Away`
- Tutti i `player:Home:*` ↔ `player:Away:*`
- `ReconciliationMetadata.SideInversionApplied = true`

Dialog UI:
- Squadre attese (DB): Home=X, Away=Y
- Squadre rilevate (PDF): Home=A, Away=B
- Tipo anomalia: `InversioneRilevata | TeamDiverso | NonRiconosciuto`
- Scelta utente: Continua / Annulla

---

## 11. Separazione dei modelli

Direzione architetturale (da realizzare progressivamente nelle fasi 7A→8):

```
EvidenceRecord / OcrEvidence      ← output Python workers (esistente)
  ↓ EvidenceRecordMapper
ProcessingResult                  ← riconciliazione per-field + candidates (esistente)
  ↓ NormalizedOcrReconciler
ReconciliationResult              ← statistiche riconciliate (da introdurre)
  ↓ PlayerIdentityMatcher
IdentityResolutionResult          ← matching + review items (da introdurre Fase 7A)
  ↓
CanonicalGameResult               ← ID canonici DB su ogni campo (da introdurre Fase 8)
  ↓
JsonExportModel                   ← JSON file locale
ImportPayload                     ← payload POST /api-ocr/import
```

**Regola:** Non aggiungere identità, revisioni e payload direttamente a `ProcessingResult`.  
Se si usa una struttura transitoria, documentare la fase esatta di migrazione.

### Contratto JSON stabile

`ProcessingResult` non cambia schema prima della Fase 8.  
Qualsiasi modifica allo schema richiede: descrizione campi → impatto su test/PHP → approvazione.

---

## 12. Revisione manuale

### Fase 7A — modello

`IdentityReviewItem` nel risultato (non in `ProcessingResult` direttamente):

```
IdentityReviewItem
  OcrJersey: string
  OcrName: string
  MatchResult: PlayerMatchResultEnum (CertainMatch|ProbableMatch|Conflict|NumberNotFound|NotInPdf)
  CandidatePlayerId: int?
  CandidateName: string?
  ConfidenceScore: double
  Side: string (Home|Away)
```

- Stato elaborazione: `CompletedWithReviewRequired` se ci sono `Conflict` o `NotInPdf`
- `ProbableMatch` con confidenza ≥ soglia configurabile: può essere auto-confermato (documentare soglia)

### Fase 7B — UI funzionante

La UI di revisione deve permettere di:

- Visualizzare tutti i conflitti (Jersey, Nome OCR, Squadra)
- Vedere i candidati del roster per ogni conflitto
- Selezionare il giocatore corretto (solo dalla squadra corretta)
- Impedire selezioni dalla squadra opposta
- Confermare o modificare la scelta proposta
- Rieseguire matching e validazioni dopo le correzioni
- Vedere i conflitti ancora aperti
- Bloccare l'export se rimangono conflitti obbligatori non risolti

---

## 13. Risultato canonico e JSON (Fase 8)

`CanonicalGameResult` contiene:

```
CanonicalContext
  MatchId: int
  HomeTeamId: int
  AwayTeamId: int

PlayerEntry
  CanonicalPlayerId: int
  TeamId: int
  ...statistiche...

TeamEntry
  CanonicalTeamId: int
  ...statistiche di squadra...
```

`ImportPayload` = serializzazione di `CanonicalGameResult` per `POST /api-ocr/import`.

I campi OCR-style (`player:Home:jersey:5`, `team:Home`) restano nel `ProcessingResult` interno  
ma non compaiono nel payload inviato al PHP.

---

## 14. Composition root (AppComposition)

**Fase 2 — prima di aggiungere nuovi servizi.**

`MainWindow.xaml.cs` attuale: ~120 righe di wiring (engines, options, pipeline, services).

Target:

```csharp
// src/BasketPdfStats.App/AppComposition.cs
public static class AppComposition
{
    public static (IPdfProcessingPipeline pipeline,
                   IOcrImportService? importService,
                   IFilePicker filePicker,
                   ITeamMismatchConfirmationService mismatch,
                   IAlreadyProcessedPdfDecisionService processed)
        Build(AppSettings settings, string projectRoot)
    { ... }
}
```

`MainWindow.xaml.cs` dopo la Fase 2:

```csharp
public MainWindow()
{
    InitializeComponent();
    var root = FindRuntimeRoot();
    var settings = AppSettingsLoader.Load(...);
    var (pipeline, importService, ...) = AppComposition.Build(settings, root);
    DataContext = new MainViewModel(pipeline, importService, ...);
}
```

Nessun DI container. Se `AppComposition.Build` supera 200 righe dopo tutte le fasi,  
valutare `Microsoft.Extensions.DependencyInjection` come operazione separata.

---

## 15. Fasi di implementazione

### Fase 0A — Baseline tecnica e funzionale (completata)

Vedere §2.

### Fase 0B — Documentazione e struttura Claude Code (completata)

File creati: docs/refactor-plan.md, docs/legacy-code-inventory.md, CLAUDE.md, AGENTS.md,
.claude/rules/ (6), .claude/skills/ (5), .claude/agents/ (4), .claude/hooks/ (3),
Config/appsettings.example.json, .gitignore aggiornato.

---

### Fase 1 — Selezione della data e caricamento partite ✅ COMPLETATA (2026-06-18)

**Obiettivo:** DatePicker in UI; partite per qualsiasi data con comportamento robusto.

**Esito:** Build verde (0 errori, 1 warning preesistente). Test: 190 passed, 0 failed, 0 ignored
(172 preesistenti + 18 nuovi). 5 file applicativi modificati, 2 file di test aggiunti.
Nessuna modifica a Python, PHP, pipeline OCR, CH1–CH4, schema JSON, matching o backend ZIP.

**File modificati:**

1. `src/BasketPdfStats.App/MainWindow.xaml` — aggiunge DatePicker prima del ComboBox
2. `src/BasketPdfStats.App/ViewModels/MainViewModel.cs` — SelectedDate, CancellationToken, IsLoading
3. `src/BasketPdfStats.Core/Database/IOcrImportService.cs` — rinomina `GetTodayMatchesAsync` → `GetMatchesForDateAsync(DateOnly date)`
4. `src/BasketPdfStats.Infrastructure/Database/OcrImportService.cs` — implementa con `?date=`
5. `src/BasketPdfStats.Infrastructure/Configuration/AppSettings.cs` — marca `[Obsolete]` `MatchLookupDate`

**Comportamento richiesto:**

- `SelectedDate` default = oggi
- Caricamento automatico al cambio data
- Aggiornamento manuale con pulsante Aggiorna
- Cancellazione della richiesta precedente se cambia data durante caricamento (CancellationToken)
- Protezione dalla reentrancy: se caricamento in corso, non avviare un secondo
- `IsLoading = true` durante la chiamata → UI disabilitata
- `SelectedMatch` azzerato ad ogni cambio di data
- Risposta vuota → lista vuota + messaggio "Nessuna partita trovata per questa data"
- Errore API → messaggio comprensibile all'utente
- ProcessButton disabilitato se nessuna partita selezionata
- `MatchLookupDate` non controlla più il flusso; rimane in config come `[Obsolete]` fino alla Fase 9

**Test richiesti:**

- `SelectedDate_default_is_today()`
- `GetMatchesForDateAsync_returns_matches_for_given_date()`
- `GetMatchesForDateAsync_returns_empty_list_when_no_matches()`
- `GetMatchesForDateAsync_shows_error_on_api_failure()`
- `Date_change_cancels_previous_request()`
- `Date_change_clears_selected_match()`
- `IsLoading_true_during_fetch_false_after()`
- `ProcessButton_disabled_during_loading()`
- `ProcessButton_disabled_when_no_match_selected()`
- `Refresh_button_reloads_current_date()`
- `Selection_cleared_after_refresh_if_not_in_new_list()`
- `Phase1_does_not_affect_OCR_or_JSON_output()`

**Rollback:** Revert dei 5 file; riabilita lettura di `MatchLookupDate`.

**Criterio di completamento:** Build verde, DatePicker funzionante, test nuovi verdi.

---

### Fase 2 — Estrazione di AppComposition ✅ COMPLETATA (2026-06-18)

**Obiettivo:** Composition root stabile prima di aggiungere nuovi servizi.

**File:**
- Nuovo: `src/BasketPdfStats.App/AppComposition.cs`
- Modificato: `src/BasketPdfStats.App/MainWindow.xaml.cs` → ~30 righe (solo bootstrap + adapter WPF)

**Esito:** Build verde (0 errori). Test: 195 passed, 0 failed, 0 ignored (+5 vs Fase 1).
`AppComposition` è WPF-free (linkabile nei test net10.0); costruisce engine OCR, document
preparation, pipeline, `OcrImportService` e `AlreadyProcessedPdfDetector`. MainWindow non
referenzia più `TesseractPythonOptions`, `PaddleCropOptions` o `EngineWeightOptions`; crea solo
gli adapter WPF (file picker, presenter, dialog) e li combina con il grafo di `AppComposition`.

**Test:** `AppCompositionTests` (4: pipeline+detector senza UI, importService null/non-null,
FindRuntimeRoot) + `AlreadyProcessedPdfSelectionServiceTests` aggiornati (split:
`App_composition_builds_detector_and_pipeline`, `Main_window_wires_composition_and_wpf_adapters`).

**Criterio:** Comportamento identico all'attuale; MainWindow non conosce più TesseractPythonOptions o PaddleCropOptions. ✓

---

### Fase 3 — Endpoint e modello del contesto partita ✅ COMPLETATA (2026-06-18)

**Obiettivo:** Roster completo dal DB disponibile in memoria prima dell'OCR.

**Esito:** Build verde. Test: 203 passed, 0 failed, 0 ignored (+8 vs Fase 2).
Endpoint PHP `GET /matches/{matchId}/context` creato (`backend/endpoints/context.php`) + routing in
`backend/index.php`. Tabelle/colonne verificate sullo schema reale (`match_teams`, `teams`,
`team_rosters.is_active/jersey_number`, `players.first_name/last_name`) — nessuna assunzione.
Supporta entrambi gli schemi (`match_teams` e `matches.home_team_id`). Errori 404/409/500.
Lato C#: modelli `OcrMatchContext`/`OcrContextTeam`/`OcrRosterPlayer`/`OcrMatchContextResult`,
`GetMatchContextAsync` in interfaccia + `OcrImportService`. `MainViewModel`: `SelectedMatchContext`
caricato alla selezione partita, ProcessButton disabilitato senza contesto.
⚠ `php -l` non eseguibile (PHP non installato in locale): sintassi PHP verificata manualmente,
da validare con `php -l` prima del deploy (Fase 8B).

**Test:** `OcrImportServiceTests` (+3: roster completo, 404, roster vuoto),
`MainViewModelMatchLoadingTests` (+5: contesto caricato alla selezione, ProcessPdf senza/con contesto,
errore contesto, azzeramento alla deselezione).

**File modificati:**
1. `backend/endpoints/context.php` (NUOVO)
2. `backend/index.php` — aggiunge routing `GET /matches/{id}/context`
3. `src/BasketPdfStats.Core/Database/OcrMatchContext.cs` (NUOVO) — modelli `OcrMatchContext`, `OcrMatchTeam`, `OcrRosterPlayer`
4. `src/BasketPdfStats.Core/Database/IOcrImportService.cs` — aggiunge `GetMatchContextAsync(int matchId)`
5. `src/BasketPdfStats.Infrastructure/Database/OcrImportService.cs` — implementa
6. `src/BasketPdfStats.App/ViewModels/MainViewModel.cs` — `SelectedMatchContext`; ProcessButton disabilitato senza contesto

**Test:**
- `GetMatchContextAsync_returns_full_roster_for_known_match()`
- `GetMatchContextAsync_throws_on_404()`
- `GetMatchContextAsync_throws_on_empty_roster()`
- `ProcessButton_disabled_when_context_not_loaded()`
- `ProcessButton_enabled_after_context_loaded()`

**Rischio:** Medio — richiede modifica al backend PHP e deploy separato su Aruba.

---

### Fase 4 — PlayerIdentityMatcher ✅ COMPLETATA (2026-06-18)

**Obiettivo:** Matching controllato jersey+nome su roster DB; nessuna discovery autonoma.

**Esito:** Build verde. Test: 216 passed, 0 failed, 0 ignored (+13 vs Fase 3).
Fase isolata: classi create e testate, **non ancora integrate nella pipeline** (integrazione in Fase 5).
`NameNormalizer` (accenti, apostrofi/trattini→spazio, variante OCR 0→o/1→i/5→s/rn→m),
`PlayerMatchResult` (enum CertainMatch/ProbableMatch/Conflict/NumberNotFound/Unmatched + value object),
`PlayerIdentityMatcher` (parse jersey con strip '*', match per ordine nome/cognome e per sottoinsieme
di token, distanza Levenshtein normalizzata, soglie 0.15/0.30 configurabili). Nome non leggibile +
jersey univoco → ProbableMatch (NameMissing). Nessun dato scritto se incerto.

**Test:** `PlayerIdentityMatcherTests` (13): certain, surname-only, starter marker, probable, conflict,
not-found, unmatched, name-missing, substitution OCR, normalizzazione accenti/apostrofi, soglie.

**File nuovi:**
- `src/BasketPdfStats.Core/Identity/PlayerIdentityMatcher.cs`
- `src/BasketPdfStats.Core/Identity/PlayerMatchResult.cs` (enum + value object)
- `src/BasketPdfStats.Core/Identity/NameNormalizer.cs`
- `tests/.../PlayerIdentityMatcherTests.cs`

**Fase isolata:** `PlayerIdentityMatcher` non è ancora integrato nella pipeline — solo creato e testato.

**Test:** vedere §16 matrice test.

---

### Fase 5 — Passaggio del contesto canonico alla pipeline ✅ COMPLETATA (2026-06-18)

**Obiettivo:** CH1 riceve il roster dinamico; matching finale sempre in C#.

**Esito:** Build verde. Test C#: 220 passed, 0 failed (+4). Test Python: `py_compile` OK su 3 moduli +
test funzionale `roster_context` (fallback legacy / roster dinamico / roster vuoto) OK + import di
`parser`/`worker` nel venv reale OK. Modifiche **backward-compatible**: senza `--roster-json` il
comportamento è identico (known_names via wrapper). `roster_context.py` dedicato (non inline in
parser.py); `known_names.py` non modificato, usato come fallback con WARNING esplicito. Solo CH1
riceve `--roster-json`; CH2–CH4 invariati. Il matching canonico resta in C# (PlayerIdentityMatcher, Fase 4).
⚠ Esecuzione completa del worker su PDF reale NON eseguita (richiede ambiente integration + Tesseract):
validazione end-to-end rinviata alla Fase 10.

**Test:** C# `TesseractPythonOcrEngineTests` (+2 BuildArguments roster), `MultiEnginePipelineTests`
(+2 threading MatchContext). Python: test funzionale roster_context.

**File modificati:**
1. `src/BasketPdfStats.Core/Models/OcrProcessingRequest.cs` — aggiunge `MatchContext: OcrMatchContext?`
2. `src/BasketPdfStats.Infrastructure/Pipeline/PdfProcessingPipeline.cs` — passa MatchContext alla request
3. `src/BasketPdfStats.Ocr.TesseractPython/TesseractFullPageOcrEngine.cs` — scrive roster JSON temp; passa `--roster-json` al worker CH1
4. `fiba_pdf_to_json_programma_v2/fiba_pdf_to_json/fiba_pdf_to_json/roster_context.py` (NUOVO)
5. `fiba_pdf_to_json_programma_v2/fiba_pdf_to_json/fiba_pdf_to_json/parser.py` — usa `roster_context.py` quando `--roster-json` presente; `known_names.py` come fallback esplicito
6. CH2/CH3/CH4: non ricevono `--roster-json` (matching solo in C#)

**Test:**
- `Pipeline_passes_match_context_to_all_engines()`
- `TesseractFullPage_uses_roster_json_when_provided()`
- `Regression: ProcessingResult_fields_unchanged_when_roster_matches_known_names()`

**Rollback:** Revert di OcrProcessingRequest + engine + worker; riabilita `known_names.py` come primary.

---

### Fase 6 — TeamIdentityMatcher e inversione home/away ✅ COMPLETATA (2026-06-18)

**Obiettivo:** Rilevare inversione home/away dopo CH1; gestire mismatch con warning UI.

**Esito:** Build verde. Test: 227 passed, 0 failed (+7). `TeamIdentityMatcher` (similarità media
ordine corretto vs invertito, soglia 0.5) + `NameSimilarity` (Levenshtein normalizzata) +
`SideInversion.Apply` (scambio lati/entityId/punteggio/parziali; slot Game invariati). Pipeline:
dopo riconciliazione, se `MatchContext` presente confronta le squadre OCR (da CH1) con il DB →
inversione = riallineamento automatico + `reconciliation.sideInversionApplied=true` + warning Info;
mismatch = warning `team.mismatch` (non blocca). No-op senza contesto (backward-compatible).
I warning fluiscono in `WarningText` (UI) via `BuildWarnings`; la conferma continua/annulla resta
gestita dal guard esistente all'import. Campo JSON `reconciliation.sideInversionApplied` aggiunto.

**Test:** `TeamIdentityMatcherTests` (4) + `SideInversionTests` (1) + `MultiEnginePipelineTests`
(+2: riallineamento+flag su inversione, warning su mismatch).

**Timing: dopo CH1, non pre-OCR.**

**File nuovi:**
- `src/BasketPdfStats.Core/Identity/TeamIdentityMatcher.cs`
- `src/BasketPdfStats.Core/Identity/TeamMatchResult.cs`

**File modificati:**
- `src/BasketPdfStats.Infrastructure/Pipeline/PdfProcessingPipeline.cs` — chiama TeamIdentityMatcher sui risultati CH1; applica riallineamento se inversione
- `src/BasketPdfStats.App/ViewModels/MainViewModel.cs` — espone `TeamValidationResult`; chiama `WpfTeamMismatchConfirmationService` prima di procedere
- Aggiunta `ReconciliationMetadata.SideInversionApplied: bool` a `ProcessingResult`

**Test:**
- `TeamMatcher_detects_correct_side()`
- `TeamMatcher_detects_side_inversion()`
- `Pipeline_realigns_home_away_after_inversion()`
- `Pipeline_sets_SideInversionApplied_flag()`
- `Mismatch_warning_shown_before_processing()`

---

### Fase 7A — Modello e raccolta delle revisioni ✅ COMPLETATA (2026-06-18)

**Obiettivo:** Rappresentare le associazioni dubbie; nessuna UI ancora.

**Esito:** Build verde. Test: 233 passed, 0 failed (+6). `IdentityReviewItem` (+enum
`IdentityReviewReason`: ProbableMatch/Conflict/NumberNotFound/Unmatched/NotInPdf) e
`IdentityReviewBuilder` (usa `PlayerIdentityMatcher` Fase 4 in sola lettura: confronta i giocatori
OCR col roster DB per lato, rileva anche i giocatori DB assenti dal PDF = NotInPdf). Aggiunti
`FileProcessingStatus.CompletedWithReviewRequired` e `ProcessingResult.IdentityReview` (campo
transitorio, in Fase 8 → IdentityResolutionResult). Pipeline: se `MatchContext` presente, popola
IdentityReview e imposta lo stato review-required su Conflict/NotInPdf. Il matcher **non scrive**
canonicalPlayerId (quello è Fase 8). No-op senza contesto.

**Test:** `IdentityReviewBuilderTests` (5: nessun item su certain, Conflict, NotInPdf, NumberNotFound,
serializzazione) + `MultiEnginePipelineTests` (+1: stato review-required su conflitto).

**File nuovi:**
- `src/BasketPdfStats.Core/Identity/IdentityReviewItem.cs`

**File modificati:**
- `IdentityResolutionResult` (nuovo tipo o campo su risultato pipeline) — aggiunge `List<IdentityReviewItem>`
- `ProcessedFile.Status` = `CompletedWithReviewRequired` se ci sono `Conflict` o `NotInPdf`
- Serializzazione JSON aggiornata

**Test:**
- `ProcessingResult_contains_identity_review_items_on_conflict()`
- `Status_is_review_required_when_conflicts_present()`
- `IdentityReviewItem_serializes_correctly()`

---

### Fase 7B — Interfaccia di revisione manuale funzionante ✅ COMPLETATA (2026-06-18)

**Obiettivo:** UI funzionante (non solo scaffold) per la risoluzione dei conflitti.

**Esito:** Build verde. Test: 240 passed, 0 failed (+7). UI WPF reale `IdentityReviewWindow`
(DataGrid: lato, motivo, jersey/nome OCR, confidenza, ComboBox candidati del **solo lato corretto**,
Conferma per riga, checkbox Risolto; footer con conflitti aperti + "Conferma ed esporta" abilitato
solo se `CanExport`). Logica testabile in `IdentityReviewViewModel`/`IdentityReviewRowViewModel`:
candidati per lato corretto, conferma Conflict richiede candidato, NotInPdf si conferma come assenza,
export bloccato finché restano Conflict/NotInPdf aperti, ProbableMatch/NumberNotFound/Unmatched non
bloccanti. `IIdentityReviewService` + `WpfIdentityReviewService`. `MainViewModel`: `ReviewIdentityCommand`,
import bloccato su review-required finché la revisione non è confermata.

**Test:** `IdentityReviewViewModelTests` (5: candidati lato corretto, blocco/sblocco export, NotInPdf,
non-bloccanti, ConfirmCommand) + `MainViewModelReviewTests` (2: import bloccato/sbloccato dopo revisione).

**File:**
- `src/BasketPdfStats.App/Views/IdentityReviewWindow.xaml` (o panel in MainWindow)
- ViewModel corrispondente

**Funzionalità obbligatorie:** vedere §12.

**Test:** test UI manuali + `CanConfirmProbableMatch_when_above_threshold()`.

---

### Fase 8 — Risultato canonico, JSON e aggiornamento import PHP ✅ COMPLETATA (2026-06-18)

**Obiettivo:** JSON finale con ID canonici DB; payload import aggiornato.

**Approvazione:** schema approvato dall'utente — opzione "Solo ImportPayload" (JSON locale invariato)
+ contratto import.php aggiornato (verifica + 422). Build verde. Test: 246 passed, 0 failed (+6).

**Esito:** Nuovi modelli `ImportPayload`/`ImportTeam`/`ImportPlayer` (Core.Models) con
matchId/teamId/playerId canonici. `ImportPayloadBuilder` (Core.Identity) risolve i playerId via
`PlayerIdentityMatcher` (match certi/probabili) e applica gli **override manuali** della revisione
(entityId→playerId). `IOcrImportService.ImportAsync` invia `ImportPayload` invece di `ProcessingResult`.
`IIdentityReviewService.ReviewAndConfirm` ritorna la mappa delle risoluzioni; `MainViewModel` la passa
al builder. `import.php`: §3/§4 usano i playerId canonici e li **verificano** sul roster (HTTP 422
con lista `invalid`), nessun fuzzy matching; §5–§7 (stats + UPSERT) invariati. JSON locale e snapshot
di regressione invariati. ⚠ `php -l` non eseguibile localmente: da validare prima del deploy (Fase 8B).

**Test:** `ImportPayloadBuilderTests` (4: ID canonici, esclusione non risolti, override manuale, stats)
+ `OcrImportServiceTests` (+2: POST payload, errore 422).

**⚠ Questa fase modifica lo schema JSON. Richiede approvazione esplicita prima dell'implementazione.**

**File nuovi:**
- `src/BasketPdfStats.Core/Models/CanonicalGameResult.cs`
- `src/BasketPdfStats.Core/Models/ImportPayload.cs`

**File modificati:**
- `src/BasketPdfStats.Infrastructure/Database/OcrImportService.cs` — invia `ImportPayload` invece di `ProcessingResult`
- `backend/endpoints/import.php` — riceve ID canonici; verifica in DB; no fuzzy matching
- `samples/test_import_match1.json` — aggiornamento snapshot (con approvazione)

**Test:**
- `CanonicalGameResult_contains_match_team_player_ids()`
- `ImportPayload_uses_canonical_ids_not_ocr_ids()`
- `ImportPayload_serialization_snapshot_test()`
- `PHP_returns_422_for_invalid_player_id()` (integration)

---

### Fase 8B — Preparazione del pacchetto backend per Aruba ✅ COMPLETATA (2026-06-19)

**Obiettivo:** Pacchetto `release/backend.zip` aggiornato, senza segreti, pronto per il deploy.

**Esito:** Script riutilizzabile `tools/build-backend-package.ps1` genera `release/backend.zip`
(voci con separatore `/`, compatibile Linux/Aruba) + `release/backend-manifest.txt` (timestamp,
commit, hash SHA-256, endpoint, file inclusi/esclusi, stato `php -l`). NON sovrascrive
`backend/backend.zip`. Controlli automatici: presenza file, route in index.php, marker Fase 8 in
import.php, scansione segreti (pulita). `backend/config/database.example.php` creato (placeholder,
nessun segreto) e incluso come riferimento; `config/database.php` reale escluso. Checklist deploy +
rollback in `docs/backend-deploy-checklist.md`. `release/backend.zip` aggiunto a `.gitignore`.
`php -l` su tutti i file PHP: **eseguito con esito positivo (2026-06-19, PHP installato in locale)**;
manifest aggiornato (`php -l: OK`), SHA-256 invariato (contenuto byte-identico).
Claude non si connette/pubblica su Aruba.

**Pacchetto:** .htaccess, index.php, config/database.example.php, endpoints/{matches,context,import}.php,
lib/{auth,helpers,response}.php (9 file).

**Prerequisiti:** Fase 3 e Fase 8 completate, testate e approvate.

**Output:**
- `release/backend.zip` (non sovrascrive `backend/backend.zip`)
- `release/backend-manifest.txt`

**Struttura attesa del pacchetto:**

```
release/backend.zip
├── .htaccess
├── index.php
├── endpoints/
│   ├── matches.php
│   ├── context.php     ← aggiunto in Fase 3
│   └── import.php      ← aggiornato in Fase 8
└── lib/
    ├── auth.php
    ├── helpers.php
    └── response.php
```

**Esclusioni obbligatorie dal pacchetto:**
- `config/database.php` (segreti MySQL)
- Token API
- File `.env`
- Log, cache, backup, file temporanei
- Test e documentazione di sviluppo
- File Git (`.git/`)
- File Claude Code (`.claude/`)
- Vecchi ZIP
- `config/database.example.php` va incluso solo come riferimento, non sovrascrive la config su Aruba

**Controlli prima della generazione:**
1. `php -l` su tutti i file PHP inclusi
2. Verifica route pattern in index.php
3. Verifica presenza `context.php`
4. Verifica versione aggiornata di `import.php`
5. Verifica presenza `.htaccess`
6. Ricerca segreti (senza stamparli)
7. Generazione elenco file inclusi
8. Calcolo hash SHA-256 dell'archivio

**Manifest `release/backend-manifest.txt`:**
- Data e ora di generazione
- Commit Git (se disponibile)
- File inclusi / esclusi
- Hash SHA-256
- Versione / fase
- Endpoint contenuti
- Nessun segreto

**Checklist deploy manuale su Aruba:**
1. Effettuare backup della cartella backend attualmente pubblicata su Aruba
2. Verificare il percorso di destinazione (`/api-ocr/`)
3. Non eliminare la configurazione DB esistente su Aruba
4. Caricare `release/backend.zip`
5. Estrarre in directory temporanea (non sovrascrivere direttamente)
6. Verificare struttura risultante e `.htaccess`
7. Ripristinare o mantenere `config/database.php` locale (da Aruba, non da zip)
8. Testare endpoint: `/health`, `/matches/today`, `/matches/{id}/context`, `/import/{id}`
9. Verificare log PHP
10. In caso di errore: ripristinare backup

**Rollback backend:**
- Il backup pre-deploy viene estratto nella directory originale
- Verificare che tutti e tre gli endpoint rispondano correttamente dopo il rollback

**Claude non si connette o pubblica su Aruba automaticamente.**

---

### Fase 9 — Pulizia legacy approvata (IN CORSO — per-item)

**Prerequisiti:** Fase 5 validata; Fase 8 e 8B completate. Ogni elemento richiede approvazione separata.

**Checklist:**
- [x] `pdf_crop_runner/prepare_crops.py` — **rimosso (2026-06-19, approvato)**. Zero import verificati; README aggiornato.
- [ ] `MatchLookupDate` in `AppSettings` — non approvato in questo passaggio (resta `[Obsolete]`, non controlla il flusso).
- [ ] `known_names.py` — DA RINVIARE: rimuovere solo dopo validazione Fase 5 su samples/pdf/ (Fase 10).
- [ ] fallback legacy in `parser.py` / `roster_context.py` — DA RINVIARE con known_names.py.
- [ ] `backend/backend.zip` — DA RINVIARE: rimuovere solo dopo deploy verificato di `release/backend.zip` su Aruba.
- [ ] `config/database.example.php` — aggiorna se necessario.

---

### Fase 10 — Validazione end-to-end e benchmark CH3/CH4 (STRUMENTI PRONTI — esecuzione locale)

**Prerequisiti:** Fase 5 completata (pipeline usa roster DB). Ambiente OCR (venv + Tesseract) — locale.

**Strumenti preparati (2026-06-19):**
- `tests/.../ChannelValidationHarnessTests.cs` — harness `[Integration]`: esegue la pipeline reale
  (tutti i canali da appsettings.json via `AppComposition.Build`) su `samples/pdf/` e produce un report
  per-canale (copertura, accordi, conflitti, tempi) da `OcrRuns` + `Stats[].Reconciliation.ProviderValues`.
  Esce senza fallire se l'ambiente OCR è assente.
- `tools/run-channel-validation.ps1` — wrapper che lancia l'harness e mostra il report.
- `docs/ch3-ch4-validation.md` — procedura, lettura report, criteri di tuning pesi, gate per Fase 9.

**Procedura (da eseguire in locale):**
1. `.\tools\run-channel-validation.ps1`
2. Leggere `TestResults/ChannelValidation/channel-validation-report.md`
3. Tarare `reconciliation.engineWeights` in appsettings.json con i dati misurati; rilanciare.

**Criteri di accettazione:** CH3 e CH4 migliorano (o non peggiorano) la qualità finale su almeno 3 PDF campione.

**Decisione su CH4:** Rimuovere solo con benchmark negativi documentati.

**Nota:** l'harness gira senza `MatchContext` (CH1 usa ancora il fallback known_names.py). Per validare
il roster dinamico — prerequisito per rimuovere known_names.py — ripetere il flusso dall'app con una
partita reale o estendere l'harness con un `OcrMatchContext` di prova.

**Fase 10C — analisi risultati (2026-06-22):** validazione eseguita su 5 PDF (tutti i canali Success,
stato CompletedWithWarnings). Aggregato per provider: CH1 `ocr.tesseract.fullpage` cov 2187 / accordo
85.9% (tutti i 309 conflitti); CH2 `ocr.tesseract.crop` cov 32 / 93.8%; CH3 `ocr.paddle.crop` cov 80 /
100%; CH4 `ocr.paddle.row` cov 1816 / 99.8%. 2331 campi riconciliati, 2743 warnings (~1.18/campo).
Dettaglio + caveat metodologico (accordo = vs reconciler, non ground truth) e proposta di tuning **non
applicata** in `docs/ch3-ch4-validation.md`. Harness esteso con aggregato per provider, tempi medi e
breakdown warnings per severità/categoria. `engineWeights` invariati: tuning rinviato all'analisi dei
warning `math.*`.

**Fase 10D — dettaglio warning matematici (2026-06-23):** harness esteso per collegare ogni warning
`math.*` alle statistiche coinvolte (via `Validation.Warnings[].FieldIds` → `Stats[].FieldId`) ed
estrarre, per ognuno: PDF, entità (giocatore/squadra + side), regola fallita, statistica, valore finale
scelto dal reconciler, valori per provider, provider scelto/concordi, messaggio completo, severità,
categoria, flag bloccante/diagnostico e collegamento a `FailedRules`. Il report markdown/JSON ora include
una sezione `Dettaglio warning matematici` per-PDF + aggregata, oltre all'aggregato già presente.
Harness rieseguito in locale (2026-06-23, 4m19s, tutti i canali Success): **4 warning `math.*` su 2331
campi riconciliati**, tutti **non bloccanti** (2 `Info` rimbalzi squadra, 2 `Warning` gap estrazione
punti/periodi; stato `CompletedWithWarnings`). Tutti su totali squadra/periodi, riconducibili a gap di
estrazione non a selezione errata → **nessun tuning pesi raccomandato**. `engineWeights` e
`appsettings.json` invariati. Build verde, 246 test non-integration verdi. Vedere `docs/ch3-ch4-validation.md`.

---

## 16. Matrice dei test per fase

| Fase | Test |
|------|------|
| 1 | `SelectedDate_default_is_today`, `GetMatchesForDateAsync_returns_matches`, `GetMatchesForDateAsync_returns_empty`, `GetMatchesForDateAsync_api_error`, `Date_change_cancels_previous_request`, `Date_change_clears_selected_match`, `IsLoading_states`, `ProcessButton_disabled_during_loading`, `ProcessButton_disabled_without_match`, `Refresh_reloads_current_date`, `Selection_cleared_after_refresh`, `Phase1_no_ocr_json_changes` |
| 2 | `AppComposition_builds_all_services_without_UI`, `AppComposition_returns_null_import_when_token_missing` |
| 3 | `GetMatchContextAsync_full_roster`, `GetMatchContextAsync_404`, `GetMatchContextAsync_empty_roster`, `ProcessButton_disabled_without_context`, `ProcessButton_enabled_after_context` |
| 4 | `MatchPlayer_certain`, `MatchPlayer_probable`, `MatchPlayer_conflict`, `MatchPlayer_not_found`, `MatchPlayer_unmatched`, `NormalizeOcrName_accents`, `NormalizeOcrName_apostrophes`, `NormalizeOcrName_ocr_substitutions` |
| 5 | `Pipeline_passes_context`, `TesseractFullPage_uses_roster_json`, `Regression_fields_unchanged` |
| 6 | `TeamMatcher_correct_side`, `TeamMatcher_inversion`, `Pipeline_realigns_after_inversion`, `Pipeline_SideInversionApplied_flag`, `Mismatch_warning_shown` |
| 7A | `IdentityReview_items_on_conflict`, `Status_review_required_on_conflict`, `IdentityReviewItem_serializes` |
| 7B | `CanConfirmProbableMatch_above_threshold` (+ test manuali UI) |
| 8 | `CanonicalGameResult_ids`, `ImportPayload_canonical_ids`, `ImportPayload_snapshot`, `PHP_422_invalid_player_id` |
| 10 | `CH3_matches_CH1_on_sample_pdf` [Integration], `CH4_matches_CH1_on_sample_pdf` [Integration] |

---

## 17. Rischi e mitigazioni

| Rischio | Probabilità | Impatto | Mitigazione |
|---------|------------|---------|-------------|
| Token API compromesso prima della rotazione | Bassa | Alto | Ruotare immediatamente dopo `git rm --cached` |
| Soglie PlayerIdentityMatcher troppo permissive | Media | Alto | Validare su samples/ prima della Fase 5 |
| roster_context.py non copre tutti i casi di known_names.py | Media | Medio | Fallback esplicito in Fase 5; rimozione solo in Fase 9 |
| import.php folder diverge da quello su Aruba | Bassa | Alto | Confrontare con backup Aruba prima del deploy |
| CH3/CH4 peggiorano qualità riconciliazione | Media | Medio | Fase 10 misura prima; peso configurabile |
| Inversione side introduce regressioni | Bassa | Alto | Test regressione su samples/ con side swap simulato |
| JSON schema change rompe import PHP | Media | Alto | Approvazione esplicita + snapshot test in Fase 8 |
| CanonicalGameResult introduce breaking change | Bassa | Alto | Doppio output (ProcessingResult + CanonicalGameResult) nella fase di transizione |

---

## 18. Rollback generale

Ogni fase usa file nuovi o modifiche isolate. Rollback = `git revert` del commit della fase.

Fasi con impatto trasversale:
- **Fase 8**: schema JSON — tenere snapshot `samples/test_import_match1.json` versionato
- **Fase 5**: worker Python — tenere fallback known_names.py fino alla Fase 9

---

## 19. Decisioni rinviate

| Decisione | Motivo |
|-----------|--------|
| Split Ocr.TesseractPython → Ocr.PaddlePython | Beneficio/costo non giustifica ora; rivalutare dopo Fase 10 |
| Microsoft.Extensions.DependencyInjection | Rivalutare se AppComposition supera 200 righe dopo Fase 9 |
| Rimozione CH4 | Solo con benchmark negativi documentati in Fase 10 |
| Centralizzazione ILogger`<T>` | Bassa priorità; i result types già catturano errori |
| Aggiunta `phase`, `short_name`, `is_starter`, `is_captain` al context endpoint | Solo se una fase dimostra la necessità con codice concreto |
