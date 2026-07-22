# Database locale Piazzetta Madness

Questa cartella contiene gli strumenti per creare un database MySQL locale
partendo dall'export Aruba.

Il risultato atteso e':

- database locale: `piazzetta_madness_local`
- edizione 2026: dati reali dell'export, marcata `Completed`
- edizione 2025: copia completa della 2026, con date spostate al 2025 e status `Completed`

Il dump originale Aruba non viene modificato. La duplicazione 2025 e' solo una
fixture locale per testare storico, albo d'oro, selector edizioni e statistiche.

## Prerequisiti

Serve un server MySQL/MariaDB locale. Lo script cerca automaticamente il client
`mysql` nel PATH e nei percorsi standard di MySQL Community Server su macOS,
incluso `/usr/local/mysql/bin/mysql`.

Esempio con Homebrew:

```bash
brew install mysql
brew services start mysql
```

## Import

Dal root del repository:

```bash
bash database/local/bootstrap-local-db.sh "/Users/francscomalaccari/Downloads/Sql1938817_1.sql"
```

Se il tuo MySQL locale richiede password:

```bash
PM_DB_USER=root PM_DB_PASS='la-password' bash database/local/bootstrap-local-db.sh "/Users/francscomalaccari/Downloads/Sql1938817_1.sql"
```

## Import con MySQL Workbench

Workbench va bene: e' solo importante essere collegati a un server MySQL locale,
non al database Aruba.

1. Crea uno schema locale chiamato `piazzetta_madness_local` con charset
   `utf8mb4`.
2. Importa l'export Aruba `Sql1938817_1.sql` dentro quello schema.
3. Apri `database/local/seed-local-editions.sql`, seleziona lo schema
   `piazzetta_madness_local` come schema attivo ed esegui tutto lo script.

## Backend in localhost

`backend/config/database.php` usa automaticamente questo DB solo quando il
backend gira su `localhost` o `127.0.0.1`. Su Aruba continua a usare la
configurazione di produzione.

Variabili locali supportate:

- `PM_DB_HOST`, default `127.0.0.1`
- `PM_DB_PORT`, default `3306`
- `PM_DB_NAME`, default `piazzetta_madness_local`
- `PM_DB_USER`, default `root`
- `PM_DB_PASS`, default vuota
