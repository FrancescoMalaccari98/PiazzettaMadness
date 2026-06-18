# AGENTS.md

Leggi **[CLAUDE.md](CLAUDE.md)** per l'architettura completa, i comandi, le regole obbligatorie e il flusso target.

---

## Regole "do not" essenziali (riepilogo rapido)

- Non reintrodurre Adobe PDF Extract, GLM-OCR o qualsiasi provider OCR esterno.
- Non usare l'OCR come fonte canonica dell'identità (squadre, giocatori, numeri di maglia).
- Non elaborare PDF se il contesto DB (roster) non è stato caricato correttamente.
- Non inserire giocatori con valori inventati, null-zero o dati non presenti nel PDF.
- Non modificare lo schema JSON senza analisi di impatto e approvazione esplicita.
- Non fare batch processing, scansioni cartelle o code multi-PDF.
- Non spostare il PDF originale selezionato.
- Non aggiornare snapshot solo per far passare i test.
- Non committare `Config/appsettings.json` (contiene token API).
- Non eliminare CH4 senza benchmark misurati.
- Non eseguire OCR pesanti o test su PDF reali senza richiesta esplicita.
- Non riprovare test su ambienti Python rotti: segnalare il blocco.

## Policy testing

- Test mirati durante sviluppo normale; suite completa a checkpoint o pre-commit.
- Non eseguire test integration (PDF reali, worker Python) senza richiesta esplicita.
- Se un ambiente Python è rotto o non configurato: fermarsi e segnalare.

## Commit

Non committare senza richiesta esplicita dell'utente.

## Contesto di architettura

→ Vedere [CLAUDE.md](CLAUDE.md) §Architettura attuale e §Flusso target  
→ Vedere [docs/refactor-plan.md](docs/refactor-plan.md) per le fasi di refactoring  
→ Vedere [.claude/rules/architecture.md](.claude/rules/architecture.md) per dipendenze tra layer
