// src/lib/api.ts
// Helper centralizzato per le chiamate al backend PHP.
// Importa e usa le funzioni tipizzate invece di fetch() diretto nelle pagine.

const BASE = (import.meta.env.VITE_API_URL ?? "").replace(/\/$/, "");

async function apiFetch<T>(path: string): Promise<T | null> {
  try {
    const res = await fetch(`${BASE}${path}`);
    if (!res.ok) return null;
    return res.json() as Promise<T>;
  } catch {
    return null;
  }
}

// ── Tipi condivisi ──────────────────────────────────────────

export interface ApiEdition {
  id: number;
  name: string;
  year: number;
  status: string;
  start_date: string | null;
  end_date: string | null;
  tournament: { id: number; name: string };
}

export interface ApiTeam {
  id: number;
  name: string;
  short_name: string | null;
  logo_path: string | null;
  group_code: string | null;
  group_name: string | null;
}

export interface ApiRosterPlayer {
  player_id: number;
  first_name: string;
  last_name: string;
  name: string;
  /** Formato: "nome-cognome-{numero}" es. "mario-rossi-7" */
  slug: string;
  jersey_number: string | null;
  role: string | null;
  is_captain: boolean;
}

export interface ApiStandingRow {
  rank: number;
  team_id: number;
  team_name: string;
  group_code: string | null;
  games_played: number;
  wins: number;
  losses: number;
  points_for: number;
  points_against: number;
  point_diff: number;
  standing_points: number;
  qualifies: boolean;
}

export interface ApiLiveState {
  match_id: number;
  has_live: boolean;
  status: "none" | "pre" | "live" | "paused" | "finished";
  version: number;
  period: number;
  clock: string;
  clock_seconds: number;
  clock_tenths: number;
  clock_running: boolean;
  home_score: number;
  away_score: number;
  home_timeouts_used: number;
  away_timeouts_used: number;
  last_event_type: string | null;
  home_team: { id: number; name: string; players: LivePlayer[] };
  away_team: { id: number; name: string; players: LivePlayer[] };
}

interface LivePlayer {
  player_id: number;
  name: string;
  jersey_number: string | null;
  points: number;
  fouls: number;
}

export interface ApiSnapshot {
  match_id: number;
  kind: "live" | "stats";
  version: number;
  json_path: string;
  updated_at: string | null;
}

// ── Endpoint functions ──────────────────────────────────────

export const api = {
  // Edizione attiva
  getActiveEdition: () =>
    apiFetch<ApiEdition>("/api-web/editions/active"),

  // Squadre
  getSquadre: () =>
    apiFetch<{ id: number; nome: string; girone: string }[]>("/api-web/squadre"),

  getTeams: (editionId?: number) =>
    apiFetch<ApiTeam[]>(`/api-web/teams${editionId ? `?edition_id=${editionId}` : ""}`),

  getTeamRoster: (teamId: number) =>
    apiFetch<{ team: ApiTeam; players: ApiRosterPlayer[] }>(`/api-web/teams/${teamId}/roster`),

  // Partite
  getPartite: () =>
    apiFetch<unknown[]>("/api-web/partite"),

  getPartita: (id: string | number) =>
    apiFetch<unknown>(`/api-web/partite/${id}`),

  getMatches: (editionId?: number) =>
    apiFetch<unknown[]>(`/api-web/matches${editionId ? `?edition_id=${editionId}` : ""}`),

  getMatch: (id: number) =>
    apiFetch<unknown>(`/api-web/matches/${id}`),

  // Live
  getMatchLive: (matchId: number) =>
    apiFetch<ApiLiveState>(`/api-web/matches/${matchId}/live`),

  // Statistiche
  getStatistiche: () =>
    apiFetch<unknown>("/api-web/statistiche"),

  getMatchStats: (matchId: number) =>
    apiFetch<unknown>(`/api-web/matches/${matchId}/stats`),

  // Giocatori
  getGiocatori: () =>
    apiFetch<unknown[]>("/api-web/giocatori"),

  getGiocatore: (slug: string) =>
    apiFetch<unknown>(`/api-web/giocatori/${slug}`),

  getPlayerEditionStats: (playerId: number, editionId?: number) =>
    apiFetch<unknown>(`/api-web/players/${playerId}/edition-stats${editionId ? `?edition_id=${editionId}` : ""}`),

  // Classifica
  getStandings: (editionId?: number) =>
    apiFetch<ApiStandingRow[]>(`/api-web/standings${editionId ? `?edition_id=${editionId}` : ""}`),

  // Snapshot
  getSnapshot: (matchId: number, kind: "live" | "stats") =>
    apiFetch<ApiSnapshot>(`/api-web/snapshots/${matchId}/${kind}`),
};
