# Tabelle del database

Descrizione semplice delle tabelle presenti nel database di Piazzetta Madness.

| Tabella | Descrizione |
|---|---|
| `tournaments` | Contiene i tornei principali, per esempio "Piazzetta Madness". |
| `editions` | Contiene le singole edizioni annuali di un torneo, per esempio l'edizione 2026. |
| `courts` | Contiene i campi da gioco disponibili per una determinata edizione. |
| `teams` | Contiene le squadre partecipanti, con nome, colori e logo. |
| `players` | Contiene i dati anagrafici di tutti i giocatori. |
| `team_rosters` | Indica a quale squadra appartiene ogni giocatore, con numero di maglia e ruolo. |
| `tournament_groups` | Contiene i gironi di un'edizione, per esempio Girone A e Girone B. |
| `group_teams` | Indica quali squadre fanno parte di ciascun girone. |
| `matches` | Contiene le partite programmate e le loro impostazioni generali. |
| `match_teams` | Collega a ogni partita la squadra di casa e quella ospite, mantenendo anche punteggio, falli e timeout. |
| `match_players` | Contiene i giocatori coinvolti in una partita e i loro dati aggiornati durante il live, come punti e falli. |
| `match_events` | Registra gli eventi avvenuti durante una partita, per esempio canestri, correzioni e altre azioni live. |
| `scoreboard_states` | Salva lo stato corrente del tabellone di una partita: punteggio, periodo, cronometri, falli e timeout. |
| `standings` | Contiene la classifica di ogni girone con vittorie, sconfitte, punti e posizione. |
| `forfeit_results` | Contiene gli eventuali risultati assegnati a tavolino. |
| `competition_events` | Contiene gli eventi aggiuntivi del torneo, come la gara del tiro da tre punti e le premiazioni. |
| `three_point_contest_entries` | Contiene i partecipanti alla gara del tiro da tre punti e il loro risultato complessivo. |
| `three_point_contest_rounds` | Contiene i punteggi ottenuti dai partecipanti nei singoli round della gara da tre punti. |
| `sponsors` | Contiene gli sponsor da mostrare nell'applicazione o sul tabellone. |
| `match_player_stats` | Contiene le statistiche complete di ogni giocatore per una partita. È gestita dal programma dedicato alle statistiche. |
| `match_team_stats` | Contiene le statistiche complete delle squadre per una partita. È gestita dal programma dedicato alle statistiche. |

## Nota sul database locale

L'applicazione legge e modifica questi dati tramite il database online. Il database locale persistente viene usato solamente per conservare temporaneamente la partita live in corso, così da poterla recuperare in caso di chiusura o interruzione.
