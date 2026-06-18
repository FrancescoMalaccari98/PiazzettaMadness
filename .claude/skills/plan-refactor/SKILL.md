# Skill: plan-refactor

## Descrizione

Analizza un'area specifica del repository e propone un piano di modifica incrementale senza toccare il codice applicativo.

## Quando usarla

- Prima di iniziare una fase del piano in `docs/refactor-plan.md`
- Quando si vuole capire l'impatto di una modifica prima di implementarla
- Per analizzare una nuova area non ancora coperta dal piano esistente

## Prerequisiti

- `docs/refactor-plan.md` aggiornato
- Build verde: `dotnet build .\BasketPdfStats.sln`

## Procedura

1. Leggi `CLAUDE.md` (regole obbligatorie)
2. Leggi la fase specifica in `docs/refactor-plan.md`
3. Leggi i file C# interessati (solo lettura)
4. Leggi i file Python interessati se la fase li coinvolge
5. Verifica eventuali dipendenze con altre fasi o componenti
6. Proponi un piano dettagliato con:
   - Obiettivo
   - File da creare
   - File da modificare (con diff concettuale)
   - Test da aggiungere
   - Test da eseguire
   - Risultato atteso
   - Rischi
   - Rollback
7. **NON modificare codice applicativo**
8. Presenta il piano e attendi approvazione

## Output atteso

- Piano testuale con sezioni: Obiettivo / File / Modifiche / Test / Rischi / Rollback
- Nessuna modifica al codice

## Divieti

- Non modificare codice, config o dipendenze durante questa skill
- Non proporre modifiche allo schema JSON senza analisi impatto completa
- Non proporre eliminazioni senza aver cercato tutti i riferimenti
- Non proporre DI container senza motivazione concreta
