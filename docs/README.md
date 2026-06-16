# Documentazione Piazzetta Madness

Questa cartella contiene solo documentazione operativa ancora utile.

## File principali

- `session-state.md`: stato attuale del progetto e funzionalita implementate.
- `api-rest.md`: guida alle API PHP in `server/api`.
- `sincronizzazione-online.md`: modello di sincronizzazione tra app, SQLite locale e database online.
- `descrizione-tabelle-database.md`: descrizione semplice delle tabelle principali.
- `query-sito-partite.sql`: query di sola lettura da passare al sito.

## Schema database

La struttura reale del database online non vive piu in `docs/`.

Usare:

```text
server/migrations/db_struttura.sql
```

Per popolare un database di test/ripristino usare dopo:

```text
server/migrations/piazzetta_test_edition_seed.sql
```

## Documenti rimossi

Sono stati rimossi documenti di progettazione iniziale ormai superati, per evitare confusione:

- vecchi ER model/diagram;
- vecchio schema SQLite v1;
- vecchie decisioni database;
- vecchio scope MVP;
- vecchio schema MySQL duplicato;
- log sessione gia riassunti in `session-state.md`.
