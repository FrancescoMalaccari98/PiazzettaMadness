# Skill: fix-extraction-issue

## Descrizione

Analizza un errore di estrazione OCR (valore mancante, errato o non associato correttamente) e propone una correzione mirata e testabile.

## Quando usarla

- Un campo specifico nel JSON finale è null quando non dovrebbe esserlo
- Un giocatore ha statistiche sbagliate (es. punti 0 invece di 14)
- Un canale OCR emette `evidence_records.json` con errori sistematici su un tipo di campo
- Il matching dei giocatori produce Conflict su un giocatore specifico

## Prerequisiti

- Il PDF problematico è disponibile in `samples/pdf/` o in un percorso fornito
- Si sa quale campo o giocatore è sbagliato

## Procedura

1. Identifica il canale responsabile: controlla `ProcessingResult.stats[].reconciliation.selectedProvider`
2. Leggi i log e il `runtime/Dataset/NormalizedOcr/<provider>/` per il PDF problematico
3. Se il problema è in un worker Python: leggi `evidence_records.json` del canale corrispondente
4. Traccia il problema: è nella calibrazione (crop vuoto/sbagliato)? Nel parser? Nel mapper? Nel reconciler?
5. Non aggiungere eccezioni specifiche hardcoded (es. "se il giocatore si chiama X, fai Y")
6. Proponi una correzione generale che si applica a tutti i casi simili
7. Aggiungi un test unitario che riproduce il problema e verifica la correzione
8. Esegui `dotnet test --filter "FullyQualifiedName~<TestClass>"` per verificare

## Cosa non fare

- Non introdurre regex o condizioni hardcoded su nomi di giocatori specifici
- Non modificare le soglie di `PlayerIdentityMatcher` senza misurarle su campioni
- Non modificare `pdf-structure/layout-map.default.json` senza verificare l'impatto su calibrazione
- Non modificare `StatsValidationService` senza analizzare le formule matematiche
- Non aggiungere `known_names.py` o simili workaround: il problema deve essere risolto sistematicamente

## Output atteso

- Causa identificata (layer, file, funzione)
- Correzione proposta con diff concettuale
- Test aggiunto
- Conferma che il fix non introduce regressioni su altri campi
