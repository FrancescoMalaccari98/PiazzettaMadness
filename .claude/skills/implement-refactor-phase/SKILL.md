# Skill: implement-refactor-phase

## Descrizione

Implementa una singola fase del piano di refactoring approvato, verifica i test e aggiorna la documentazione.

## Quando usarla

Quando l'utente dice "implementa la Fase N" o "procedi con la fase [nome]" — e il piano è già stato approvato.

## Prerequisiti

- `docs/refactor-plan.md` contiene la fase da implementare con obiettivo, file e test
- La fase precedente è completata (build verde, test verdi)
- La fase NON richiede modifiche allo schema JSON (se sì: approvazione separata)

## Procedura

1. Leggi `CLAUDE.md` e la fase corrispondente in `docs/refactor-plan.md`
2. Leggi i file C# e Python che verranno modificati (sola lettura prima di toccare)
3. Verifica lo stato di build: `dotnet build .\BasketPdfStats.sln`
4. Implementa le modifiche nell'ordine: Core → Infrastructure → Ocr.* → App
5. Dopo ogni gruppo di modifiche: `dotnet build` per verifica immediata
6. Aggiungi i test indicati nel piano
7. Esegui i test della fase: usa il filter indicato nel piano
8. Se i test falliscono: analizza il motivo; non aggiornare snapshot per nascondere regressioni
9. Se c'è una regressione non spiegata: fermati e segnala prima di continuare
10. Aggiorna `TASK_STATUS.md` con lo stato della fase
11. Mostra riepilogo: file modificati, test aggiunti, output `dotnet test`

## Output atteso

- Modifiche applicate
- Test eseguiti con risultato (exit code, numero test passati)
- `TASK_STATUS.md` aggiornato
- Riepilogo diff concettuale

## Condizioni di arresto

- Regressione non spiegata nei test esistenti
- Build rosso dopo le modifiche
- La fase richiede modifica allo schema JSON non pre-approvata
- Scoperta di un impatto non previsto nel piano (fermarsi e segnalare)

## Procedure speciali

### Fase 2 (AppComposition)
Verifica che MainWindow.xaml.cs sia ridotto a ≤25 righe e che il comportamento sia identico all'attuale prima di dichiarare completata.

### Fase 8 (schema JSON)
Richiede approvazione esplicita prima di toccare ProcessingResult o samples/test_import_match1.json.

### Fase 8B (pacchetto backend)
Questa fase NON modifica codice applicativo C#/XAML/Python.
Procedura alternativa:
1. Leggere `docs/refactor-plan.md` §Fase 8B completa
2. Verificare che Fase 3 e Fase 8 siano completate e testate
3. Eseguire `php -l` su tutti i file PHP di `backend/`
4. Creare `release/backend.zip` da `backend/` (non sovrascrivere `backend/backend.zip`)
5. Generare `release/backend-manifest.txt` (senza segreti)
6. Presentare checklist deploy all'utente e attendere esecuzione manuale
7. Claude non si connette o pubblica su Aruba automaticamente

### Fase 9 (pulizia legacy)
Ogni elemento richiede approvazione separata prima della rimozione.

## Divieti

- Non lavorare su più fasi contemporaneamente
- Non aggiornare snapshot solo per far passare i test
- Non committare senza richiesta esplicita dell'utente
- Non rimuovere codice legacy senza approvazione esplicita per ogni elemento
- Non modificare schema JSON senza approvazione
- Non pubblicare su Aruba automaticamente in nessuna fase
