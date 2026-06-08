# CONTEXT — PiazzettaMadness

> Ultimo aggiornamento: 2026-06-08
> Generato da Claude Code durante la sessione di sviluppo

---

## 1. Cos'è il progetto

**Piazzetta Madness** è un sito web per un torneo di basket 5vs5 che si tiene a Porto Potenza Picena (estate 2026).

**URL produzione:** https://www.piazzettamadness.it  
**Hosting:** Aruba Linux shared hosting (Apache + PHP 8.0 + MySQL 8.0)  
**DB host:** 31.11.39.155:3306

---

## 2. Struttura del repository locale

```
PiazzettaMadness/PiazzettaMadness/
├── frontend/                 ← App React (sorgente + build)
│   ├── src/
│   │   ├── pages/            ← Tutte le pagine React
│   │   ├── components/       ← Navigation, Footer, ScrollToTop, CookieBanner
│   │   ├── data/stats.ts     ← Dati statici fallback per statistiche
│   │   ├── lib/
│   │   │   ├── analytics.ts  ← Google Analytics (GA_ID placeholder!)
│   │   │   ├── api.ts        ← Helper fetch centralizzato
│   │   │   └── utils.ts
│   │   └── index.css
│   ├── public/
│   │   ├── .htaccess         ← Copiato in dist/ da Vite (routing SPA)
│   │   └── assets/           ← beer.png, logo.png
│   ├── dist/                 ← Build compilata (gitignored)
│   ├── frontend.zip          ← ZIP pronto per upload Aruba
│   ├── .env.production.local ← VITE_API_URL=https://www.piazzettamadness.it
│   └── package.json
│
├── backend/                  ← Backend PHP
│   ├── index.php             ← Router principale
│   ├── .htaccess             ← Rewrite per Apache
│   ├── config/
│   │   ├── database.php      ← CREDENZIALI REALI (in .gitignore)
│   │   └── cors.php
│   ├── lib/
│   │   ├── response.php      ← send_json(), send_error()
│   │   ├── auth.php          ← Bearer token per endpoint write
│   │   └── helpers.php       ← slug, date IT, avg(), format_clock()
│   ├── endpoints/
│   │   ├── editions.php
│   │   ├── teams.php
│   │   ├── matches.php
│   │   ├── live.php
│   │   ├── stats.php
│   │   ├── players.php
│   │   ├── standings.php
│   │   ├── snapshots.php
│   │   └── foto.php
│   └── backend.zip           ← ZIP pronto per upload Aruba
│
├── .gitignore
└── CONTEXT.md                ← questo file
```

---

## 3. Stack tecnico

| Tecnologia | Versione | Ruolo |
|---|---|---|
| React | 19 | UI framework |
| TypeScript | - | Tipizzazione |
| Vite | 6 | Build tool |
| Tailwind CSS | v4 | Stile (tema custom) |
| React Router | v7 | Routing client-side |
| Motion (Framer) | - | Animazioni |
| Lucide React | - | Icone |
| PHP | 8.0 | Backend Aruba |
| PDO + MySQL | 8.0 | Prepared statements, utf8mb4 |

---

## 4. Pagine frontend e routing

| Route | File | Dati da API |
|---|---|---|
| `/` | `Home.tsx` | `/api/partite` (countdown prima partita) |
| `/info` | `Info.tsx` | Statico |
| `/foto` | `Photos.tsx` | `/api/foto` |
| `/match` | `Matches.tsx` | `/api/squadre` + `/api/partite` |
| `/statistiche` | `Stats.tsx` | `/api/statistiche` |
| `/statistiche/:slug` | `PlayerDetail.tsx` | `/api/giocatori/{slug}` |
| `/giocatori` | `Players.tsx` | `/api/giocatori` |
| `/staff` | `Staff.tsx` | Statico |
| `/scoreboard` | `Scoreboard.tsx` | `/api/squadre` |
| `/projection` | `Projection.tsx` | localStorage + BroadcastChannel |
| `/sponsor` | `Sponsors.tsx` | Statico |
| `/privacy` | `Privacy.tsx` | Statico |

---

## 5. Endpoint API PHP implementati

| Endpoint | File | Descrizione |
|---|---|---|
| `GET /api/squadre` | `teams.php` | Lista squadre con girone (A/B) |
| `GET /api/partite` | `matches.php` | Calendario partite |
| `GET /api/partite/{id}` | `matches.php` | Box score completo |
| `GET /api/statistiche` | `stats.php` | StatsData: players, mvps, teamStats |
| `GET /api/giocatori` | `players.php` | Lista giocatori con medie |
| `GET /api/giocatori/{slug}` | `players.php` | Dettaglio giocatore + matchLog |
| `GET /api/foto` | `foto.php` | Lista URL immagini |
| `GET /api/health` | `index.php` | Health check |
| `GET /api/editions/active` | `editions.php` | Edizione attiva |
| `GET /api/teams` | `teams.php` | Lista squadre (nuova struttura) |
| `GET /api/matches` | `matches.php` | Partite (nuova struttura) |
| `GET /api/matches/{id}` | `matches.php` | Dettaglio partita |
| `GET /api/matches/{id}/live` | `live.php` | Stato live |
| `GET /api/matches/{id}/stats` | `stats.php` | Stats OCR partita |
| `GET /api/standings` | `standings.php` | Classifica |

---

## 6. Database — configurazione attuale

**DB attivo:** `Sql1938817_1` (schema nuovo, app C# tabellone)  
**DB host:** 31.11.39.155:3306  
**User:** Sql1938817

### Edizione attiva automatica

`backend/config/database.php` — riga chiave:
```php
define('ACTIVE_EDITION_ID', 0); // 0 = usa status='Active' dal DB
```

- `0` → il backend legge `SELECT id FROM editions WHERE status='Active'`
- `N > 0` → forza sempre l'edizione con quell'ID, indipendentemente dal DB

**Per cambiare edizione:** impostare `status='Active'` nell'edizione desiderata su phpMyAdmin (e `status='Draft'` nelle altre), oppure impostare `ACTIVE_EDITION_ID = <id>` in database.php.

### Edizione corrente in produzione

| Campo | Valore |
|---|---|
| edition_id | 14 |
| name | Memorial Carlo Venturi 2026 |
| status | Active |
| start_date | 2026-07-10 |
| end_date | 2026-07-14 |
| tournament | Memorial Carlo Venturi 3x3 (id=12) |

### Schema differenze chiave (Sql1938817_1 vs vecchio)

| Aspetto | Nuovo schema |
|---|---|
| Home/Away team | `match_teams` (side='Home'/'Away'), NO `matches.home_team_id` |
| Punteggio | `match_teams.score`, NO `matches.final_home_score/away_score` |
| Data partita | `matches.scheduled_start_at` (non `scheduled_at`) |
| Round | `matches.round` (non `round_label`) |
| Durata periodo | `period_duration_ms` (ms, non secondi) |
| Live state | `scoreboard_states` (clock: `game_clock_ms_remaining`) |
| Giocatori live | `match_players` (falli: `personal_fouls`) |
| Classifica | `standings` TABLE (non view); filtro via `group_id → tournament_groups.edition_id` |
| Colonne standings | `played`, `point_difference`, `ranking_points` |
| Parziali | `match_periods.home_score_end` / `away_score_end` |
| Tempo vantaggio | `match_team_stats.time_in_lead` (varchar "MM:SS") |
| Status enum | PascalCase: `'Active'`, `'Finished'`, `'Cancelled'`, ecc. |
| Girone partite | `matches.phase` PascalCase: `'GroupStage'`, `'SemiFinal'`, `'ThirdPlaceFinal'`, `'Final'` |

### Struttura edition_id=14 (Memorial Carlo Venturi 2026)

**Gironi:**
| group_id | code | name | Teams |
|---|---|---|---|
| 19 | A | Girone Adriatico | Adriatica Blu(53), Macerata Reds(54), Porto Verde(55) |
| 20 | B | Girone Monti | Collina Nera(56), Riviera Gold(57), Monte Bianco(58) |

**Partite:**
| match_id | Nome | Phase | Round | Status |
|---|---|---|---|---|
| 37 | Adriatica Blu vs Macerata Reds | GroupStage | Girone A - giornata 1 | Finished |
| 38 | Adriatica Blu vs Porto Verde | GroupStage | Girone A - giornata 2 | Finished |
| 39 | Macerata Reds vs Porto Verde | GroupStage | Girone A - giornata 3 | Finished |
| 40 | Collina Nera vs Riviera Gold | GroupStage | Girone B - giornata 1 | Finished |
| 41 | Collina Nera vs Monte Bianco | GroupStage | Girone B - giornata 2 | Finished |
| 42 | Riviera Gold vs Monte Bianco | GroupStage | Girone B - giornata 3 | Finished |
| 43 | Semifinale 1 - Porto Verde vs Collina Nera | SemiFinal | Final Four | Finished |
| 44 | Semifinale 2 - Monte Bianco vs Macerata Reds | SemiFinal | Final Four | Finished |
| 45 | Finale terzo posto | ThirdPlaceFinal | Final Four | Finished |
| 46 | Finale - Porto Verde vs Macerata Reds | Final | Final Four | Finished |

**Nota:** Il backend sovrascrive il `round` per i playoff (SemiFinal→'Semifinale', Final→'Finale') per compatibilità con il frontend.

---

## 7. Modifiche backend effettuate (sessione 2026-06-08)

### helpers.php
- `get_active_edition_id()`: usa `ACTIVE_EDITION_ID` se > 0, altrimenti legge `status='Active'` dal DB
- `map_match_status()`: PascalCase (`'Live'`, `'Paused'`, `'Finished'`)

### matches.php
- JOIN `match_teams` per home/away/score (non più colonne dirette su `matches`)
- `scheduled_start_at` invece di `scheduled_at`
- `round` invece di `round_label`
- `match_periods`: usa `home_score_end`/`away_score_end`
- `match_team_stats`: usa `time_in_lead` (varchar) invece di `time_in_lead_seconds`
- Playoff: il phase sovrascrive sempre il round (`SemiFinal`→`'Semifinale'`, ecc.)
- Status PascalCase: `'Finished'`, `'Cancelled'`

### live.php
- `scoreboard_states`: colonne `current_period`, `game_clock_ms_remaining`, `is_game_clock_running`, `home_timeouts_used_total`, `away_timeouts_used_total`
- `match_players`: colonna `personal_fouls` (non `fouls`)
- Status PascalCase: `'Live'`, `'Paused'`, `'Finished'`

### standings.php
- Query su `standings` TABLE (non view `v_standings`)
- Filtro via `JOIN tournament_groups ON tg.edition_id = ?` (standings non ha edition_id)
- Colonne: `played`, `point_difference`, `ranking_points`

### players.php / stats.php / teams.php
- Sostituiti `v_player_edition_stats`, `v_team_edition_stats`, `v_match_player_stats_public`, `v_match_team_stats_public` con query dirette
- `match_player_stats`: usa `fg_made`/`fg_att` direttamente (non calcolati)
- Status PascalCase

### Matches.tsx (frontend)
- `computeStandings()`: inizializza entry per team non presenti nella lista iniziale del girone (evita crash React/schermata nera)

---

## 8. Deploy su Aruba — procedura

### Struttura su server
```
www.piazzettamadness.it/
├── .htaccess, index.html, assets/ ← frontend React
└── api/
    ├── .htaccess, index.php       ← router PHP
    ├── config/database.php        ← credenziali DB (NON nel git)
    ├── lib/
    └── endpoints/
```

### Procedura upload via File Manager Aruba
1. Carica `frontend/frontend.zip` in `www.piazzettamadness.it/` → Estrai → Sovrascivi → Elimina zip
2. Carica `backend/backend.zip` in `www.piazzettamadness.it/api/` → Estrai → Sovrascivi → Elimina zip

### Build + rigenera zip (locale)
```bash
# Frontend
cd frontend && npm run build
cd dist && zip -r ../frontend.zip . --exclude "./.DS_Store"

# Backend
cd backend
zip -r backend.zip . --exclude "./.DS_Store" --exclude "./backend.zip"
```

---

## 9. Tabellone live (Scoreboard/Projection)

- **Scoreboard** (`/scoreboard`): pannello staff, password `"madness26"` hardcoded nel JS
- **Projection** (`/projection`): schermo pubblico fullscreen
- Comunicazione via `localStorage` + `BroadcastChannel('scoreboard_sync')`
- Squadre caricate dinamicamente da `/api/squadre`

### Endpoint live `/api/matches/{id}/live`
1. **`source: "live"`** → partita in corso → `scoreboard_states` + `match_players`
2. **`source: "ocr"`** → partita finita → `match_player_stats` + `match_team_stats`
3. **`source: "none"`** → nessun dato

---

## 10. Slug giocatore

Formato: `{nome}-{cognome}-{numero_maglia}`  
Esempio: `luca-verdi-4`

Generato da `player_slug(first_name, last_name, jersey_number)` in `helpers.php`.

---

## 11. Bug noti / TODO

| # | Severità | Elemento | Problema |
|---|---|---|---|
| 1 | Bassa | `src/lib/analytics.ts:3` | GA_ID = `"G-XXXXXXXXXX"` placeholder |
| 2 | Bassa | `Scoreboard.tsx` | Password `"madness26"` hardcoded nel JS |
| 3 | Info | `match_player_stats` | Vuota per edition_id=14 (nessun dato OCR ancora) |
| 4 | Info | `Registration.tsx` | Form iscrizioni senza route in App.tsx — intenzionalmente inattivo |

---

## 12. Query utili phpMyAdmin

### Cambiare edizione attiva
```sql
-- Disattiva tutte
UPDATE editions SET status = 'Draft' WHERE status = 'Active';
-- Attiva quella desiderata
UPDATE editions SET status = 'Active' WHERE id = 14;
```

### Verificare struttura edizione attiva
```sql
SELECT e.id, e.name, e.status,
       tg.id AS group_id, tg.code, tg.name AS girone,
       t.id AS team_id, t.name AS team_name
FROM editions e
JOIN tournament_groups tg ON tg.edition_id = e.id
JOIN group_teams gt ON gt.group_id = tg.id
JOIN teams t ON t.id = gt.team_id
WHERE e.status = 'Active'
ORDER BY tg.sort_order, t.name;
```

### Verificare partite edizione attiva
```sql
SELECT m.id, m.name, m.phase, m.round, m.status,
       mt_h.score AS home_score, mt_a.score AS away_score
FROM matches m
JOIN match_teams mt_h ON mt_h.match_id = m.id AND mt_h.side = 'Home'
JOIN match_teams mt_a ON mt_a.match_id = m.id AND mt_a.side = 'Away'
WHERE m.edition_id = (SELECT id FROM editions WHERE status = 'Active' LIMIT 1)
ORDER BY m.scheduled_start_at;
```

---

*Fine documento di contesto*
