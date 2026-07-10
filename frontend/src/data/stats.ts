export type MatchLog = {
  matchId: string;
  date: string;
  opponent: string;
  result: "V" | "P" | "-";
  played?: boolean;
  pts: number; ast: number; reb: number; stl: number;
};

export type Player = {
  slug: string;
  name: string;
  team: string;
  teamColor?: string | null;
  number?: number;
  photo?: string;
  // medie per partita — calcolate dal backend dai tabellini FIBA
  pts: number;
  ptsTotal?: number; // punti totali stagione (somma di tutte le partite)
  ptsTop?: number;   // miglior punteggio in una singola partita
  ast: number;
  reb: number;       // rimbalzi totali
  rebOff: number;    // rimbalzi offensivi
  rebDef: number;    // rimbalzi difensivi
  stl: number;       // palle recuperate
  pp: number;        // palle perse
  sd: number;        // stoppate date
  ff: number;        // falli fatti
  fs: number;        // falli subiti
  plusMinus: number; // +/-
  val: number;       // valutazione FIBA
  p2pct: number;     // % 2 punti
  p3pct: number;     // % 3 punti
  tlpct: number;     // % tiri liberi
  p2m?: number;      // 2pt realizzati
  p2a?: number;      // 2pt tentati
  p3m?: number;      // 3pt realizzati
  p3a?: number;      // 3pt tentati
  tlm?: number;      // tiri liberi realizzati
  tla?: number;      // tiri liberi tentati
  matchLog: MatchLog[];
};

export type TeamStats = {
  squadra: string;
  partiteGiocate: number;
  punti: number;
  puntiSubiti: number;
  assist: number;
  rimbalzi: number;
  recuperi: number;
  stoppate: number;
  pallePerse: number;
  p2m?: number;
  p2a?: number;
  p2pct?: number;
  p3m?: number;
  p3a?: number;
  p3pct?: number;
  tlm?: number;
  tla?: number;
  tlpct?: number;
};

export type MatchMvp = {
  match: string;
  date: string;
  player: string;
  team: string;
  stat: string;
};

export type StatsData = {
  tournamentMvpSlug: string;
  players: Player[];
  matchMvps: MatchMvp[];
  teamStats: TeamStats[];
};

// — DATI STATICI DI FALLBACK —
// Sostituiti automaticamente dai dati reali del backend quando disponibile

const players: Player[] = [
  // — SALUTA ANDONIO SPURS —
  { slug: "marco-rossi",    name: "Marco Rossi",    team: "Saluta Andonio Spurs", number: 10, pts: 24.5, ast: 5.1, reb: 4.2, rebOff: 1.0, rebDef: 3.2, stl: 1.5, pp: 2.1, sd: 0.5, ff: 2.0, fs: 3.5, plusMinus: 8.5, val: 22.0, p2pct: 52.0, p3pct: 38.0, tlpct: 80.0, matchLog: [{ matchId: "ga1", date: "05 Ago", opponent: "Boston Lopez", result: "V", pts: 22, ast: 6, reb: 4, stl: 2 }, { matchId: "ga3", date: "07 Ago", opponent: "Miami Spritz", result: "V", pts: 28, ast: 5, reb: 5, stl: 1 }, { matchId: "ga5", date: "10 Ago", opponent: "Golden State Worriers", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "giovanni-neri",  name: "Giovanni Neri",  team: "Saluta Andonio Spurs", number: 5,  pts: 15.2, ast: 3.0, reb: 8.5, rebOff: 3.0, rebDef: 5.5, stl: 0.8, pp: 1.5, sd: 1.2, ff: 2.5, fs: 2.0, plusMinus: 6.0, val: 14.5, p2pct: 48.0, p3pct: 0.0,  tlpct: 70.0, matchLog: [{ matchId: "ga1", date: "05 Ago", opponent: "Boston Lopez", result: "V", pts: 18, ast: 3, reb: 9, stl: 1 }, { matchId: "ga3", date: "07 Ago", opponent: "Miami Spritz", result: "V", pts: 14, ast: 3, reb: 8, stl: 0 }, { matchId: "ga5", date: "10 Ago", opponent: "Golden State Worriers", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "luca-bianchi",   name: "Luca Bianchi",   team: "Saluta Andonio Spurs", number: 3,  pts: 8.5,  ast: 6.2, reb: 2.1, rebOff: 0.5, rebDef: 1.6, stl: 2.0, pp: 1.8, sd: 0.2, ff: 1.5, fs: 3.0, plusMinus: 5.0, val: 11.0, p2pct: 44.0, p3pct: 35.0, tlpct: 85.0, matchLog: [{ matchId: "ga1", date: "05 Ago", opponent: "Boston Lopez", result: "V", pts: 9, ast: 7, reb: 2, stl: 3 }, { matchId: "ga3", date: "07 Ago", opponent: "Miami Spritz", result: "V", pts: 18, ast: 6, reb: 2, stl: 1 }, { matchId: "ga5", date: "10 Ago", opponent: "Golden State Worriers", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "matteo-verdi",   name: "Matteo Verdi",   team: "Saluta Andonio Spurs", number: 14, pts: 6.0,  ast: 1.5, reb: 10.2, rebOff: 4.0, rebDef: 6.2, stl: 0.2, pp: 1.0, sd: 1.5, ff: 3.0, fs: 1.5, plusMinus: 4.0, val: 9.0,  p2pct: 50.0, p3pct: 0.0,  tlpct: 60.0, matchLog: [{ matchId: "ga1", date: "05 Ago", opponent: "Boston Lopez", result: "V", pts: 10, ast: 2, reb: 11, stl: 0 }, { matchId: "ga3", date: "07 Ago", opponent: "Miami Spritz", result: "V", pts: 7, ast: 1, reb: 10, stl: 0 }, { matchId: "ga5", date: "10 Ago", opponent: "Golden State Worriers", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "andrea-gialli",  name: "Andrea Gialli",  team: "Saluta Andonio Spurs", number: 21, pts: 4.5,  ast: 2.0, reb: 3.0, rebOff: 1.0, rebDef: 2.0, stl: 1.1, pp: 1.2, sd: 0.1, ff: 1.8, fs: 2.0, plusMinus: 3.0, val: 5.0,  p2pct: 40.0, p3pct: 30.0, tlpct: 75.0, matchLog: [{ matchId: "ga1", date: "05 Ago", opponent: "Boston Lopez", result: "V", pts: 7, ast: 2, reb: 3, stl: 2 }, { matchId: "ga3", date: "07 Ago", opponent: "Miami Spritz", result: "V", pts: 6, ast: 2, reb: 3, stl: 1 }, { matchId: "ga5", date: "10 Ago", opponent: "Golden State Worriers", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },

  // — BOSTON LOPEZ —
  { slug: "filippo-conti",  name: "Filippo Conti",  team: "Boston Lopez", number: 7,  pts: 16.2, ast: 4.0, reb: 3.5, rebOff: 1.0, rebDef: 2.5, stl: 1.3, pp: 2.0, sd: 0.3, ff: 2.5, fs: 3.0, plusMinus: 2.0, val: 15.0, p2pct: 50.0, p3pct: 36.0, tlpct: 78.0, matchLog: [{ matchId: "ga1", date: "05 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 17, ast: 4, reb: 3, stl: 1 }, { matchId: "ga4", date: "07 Ago", opponent: "Golden State Worriers", result: "V", pts: 17, ast: 4, reb: 4, stl: 2 }, { matchId: "ga6", date: "10 Ago", opponent: "Miami Spritz", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "roberto-serra",  name: "Roberto Serra",  team: "Boston Lopez", number: 11, pts: 12.0, ast: 2.5, reb: 6.0, rebOff: 2.5, rebDef: 3.5, stl: 0.7, pp: 1.5, sd: 0.8, ff: 2.8, fs: 2.5, plusMinus: 1.0, val: 11.0, p2pct: 46.0, p3pct: 0.0,  tlpct: 65.0, matchLog: [{ matchId: "ga1", date: "05 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 15, ast: 3, reb: 6, stl: 1 }, { matchId: "ga4", date: "07 Ago", opponent: "Golden State Worriers", result: "V", pts: 12, ast: 2, reb: 6, stl: 0 }, { matchId: "ga6", date: "10 Ago", opponent: "Miami Spritz", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "diego-palma",    name: "Diego Palma",    team: "Boston Lopez", number: 4,  pts: 9.5,  ast: 3.2, reb: 4.0, rebOff: 1.2, rebDef: 2.8, stl: 0.5, pp: 1.8, sd: 0.2, ff: 2.0, fs: 2.2, plusMinus: -1.0, val: 8.0, p2pct: 42.0, p3pct: 33.0, tlpct: 70.0, matchLog: [{ matchId: "ga1", date: "05 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 12, ast: 3, reb: 4, stl: 0 }, { matchId: "ga4", date: "07 Ago", opponent: "Golden State Worriers", result: "V", pts: 9, ast: 4, reb: 4, stl: 1 }, { matchId: "ga6", date: "10 Ago", opponent: "Miami Spritz", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "enrico-mora",    name: "Enrico Mora",    team: "Boston Lopez", number: 9,  pts: 6.0,  ast: 1.0, reb: 7.5, rebOff: 3.0, rebDef: 4.5, stl: 0.3, pp: 1.0, sd: 1.0, ff: 2.5, fs: 1.5, plusMinus: -2.0, val: 7.0, p2pct: 45.0, p3pct: 0.0,  tlpct: 55.0, matchLog: [{ matchId: "ga1", date: "05 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 10, ast: 1, reb: 8, stl: 1 }, { matchId: "ga4", date: "07 Ago", opponent: "Golden State Worriers", result: "V", pts: 6, ast: 1, reb: 7, stl: 0 }, { matchId: "ga6", date: "10 Ago", opponent: "Miami Spritz", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "stefano-riva",   name: "Stefano Riva",   team: "Boston Lopez", number: 22, pts: 3.5,  ast: 0.8, reb: 2.0, rebOff: 0.5, rebDef: 1.5, stl: 0.2, pp: 0.8, sd: 0.0, ff: 1.5, fs: 1.0, plusMinus: -1.0, val: 3.0, p2pct: 38.0, p3pct: 25.0, tlpct: 60.0, matchLog: [{ matchId: "ga1", date: "05 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 6, ast: 1, reb: 2, stl: 0 }, { matchId: "ga4", date: "07 Ago", opponent: "Golden State Worriers", result: "V", pts: 4, ast: 1, reb: 2, stl: 0 }, { matchId: "ga6", date: "10 Ago", opponent: "Miami Spritz", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },

  // — MIAMI SPRITZ —
  { slug: "carlo-neri",     name: "Carlo Neri",     team: "Miami Spritz", number: 6,  pts: 17.5, ast: 3.8, reb: 3.0, rebOff: 0.8, rebDef: 2.2, stl: 1.3, pp: 2.5, sd: 0.4, ff: 2.0, fs: 3.5, plusMinus: 3.0, val: 16.0, p2pct: 54.0, p3pct: 40.0, tlpct: 82.0, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Golden State Worriers", result: "V", pts: 19, ast: 4, reb: 3, stl: 1 }, { matchId: "ga3", date: "07 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 22, ast: 4, reb: 3, stl: 2 }, { matchId: "ga6", date: "10 Ago", opponent: "Boston Lopez", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "pietro-russo",   name: "Pietro Russo",   team: "Miami Spritz", number: 8,  pts: 13.0, ast: 2.0, reb: 5.0, rebOff: 1.5, rebDef: 3.5, stl: 0.6, pp: 1.5, sd: 0.5, ff: 2.5, fs: 2.0, plusMinus: 2.0, val: 12.0, p2pct: 48.0, p3pct: 0.0,  tlpct: 72.0, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Golden State Worriers", result: "V", pts: 14, ast: 2, reb: 5, stl: 1 }, { matchId: "ga3", date: "07 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 18, ast: 2, reb: 5, stl: 0 }, { matchId: "ga6", date: "10 Ago", opponent: "Boston Lopez", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "fabio-costa",    name: "Fabio Costa",    team: "Miami Spritz", number: 15, pts: 8.0,  ast: 4.5, reb: 2.5, rebOff: 0.5, rebDef: 2.0, stl: 0.9, pp: 2.0, sd: 0.1, ff: 1.8, fs: 2.5, plusMinus: 1.5, val: 9.0,  p2pct: 42.0, p3pct: 34.0, tlpct: 78.0, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Golden State Worriers", result: "V", pts: 8, ast: 5, reb: 3, stl: 1 }, { matchId: "ga3", date: "07 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 12, ast: 4, reb: 2, stl: 1 }, { matchId: "ga6", date: "10 Ago", opponent: "Boston Lopez", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "nicola-bruno",   name: "Nicola Bruno",   team: "Miami Spritz", number: 33, pts: 5.5,  ast: 1.2, reb: 6.0, rebOff: 2.5, rebDef: 3.5, stl: 0.4, pp: 1.2, sd: 0.8, ff: 2.2, fs: 1.8, plusMinus: -1.0, val: 6.0, p2pct: 44.0, p3pct: 0.0,  tlpct: 58.0, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Golden State Worriers", result: "V", pts: 7, ast: 1, reb: 6, stl: 0 }, { matchId: "ga3", date: "07 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 8, ast: 2, reb: 6, stl: 1 }, { matchId: "ga6", date: "10 Ago", opponent: "Boston Lopez", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "davide-ricci",   name: "Davide Ricci",   team: "Miami Spritz", number: 19, pts: 2.0,  ast: 0.5, reb: 1.5, rebOff: 0.3, rebDef: 1.2, stl: 0.1, pp: 0.5, sd: 0.0, ff: 1.0, fs: 0.8, plusMinus: -2.0, val: 2.0, p2pct: 33.0, p3pct: 20.0, tlpct: 50.0, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Golden State Worriers", result: "V", pts: 3, ast: 1, reb: 2, stl: 0 }, { matchId: "ga3", date: "07 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 4, ast: 0, reb: 1, stl: 0 }, { matchId: "ga6", date: "10 Ago", opponent: "Boston Lopez", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },

  // — GOLDEN STATE WORRIERS —
  { slug: "alessandro-mari", name: "Alessandro Mari", team: "Golden State Worriers", number: 30, pts: 11.0, ast: 3.5, reb: 4.0, rebOff: 1.2, rebDef: 2.8, stl: 1.0, pp: 1.8, sd: 0.3, ff: 2.0, fs: 2.5, plusMinus: -3.0, val: 9.0,  p2pct: 46.0, p3pct: 33.0, tlpct: 72.0, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Miami Spritz", result: "P", pts: 11, ast: 4, reb: 4, stl: 1 }, { matchId: "ga4", date: "07 Ago", opponent: "Boston Lopez", result: "P", pts: 12, ast: 3, reb: 4, stl: 1 }, { matchId: "ga5", date: "10 Ago", opponent: "Saluta Andonio Spurs", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "lorenzo-galli",   name: "Lorenzo Galli",   team: "Golden State Worriers", number: 2,  pts: 9.0,  ast: 2.0, reb: 5.5, rebOff: 2.0, rebDef: 3.5, stl: 0.5, pp: 1.5, sd: 0.5, ff: 2.2, fs: 2.0, plusMinus: -4.0, val: 8.0,  p2pct: 44.0, p3pct: 0.0,  tlpct: 60.0, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Miami Spritz", result: "P", pts: 9, ast: 2, reb: 6, stl: 0 }, { matchId: "ga4", date: "07 Ago", opponent: "Boston Lopez", result: "P", pts: 10, ast: 2, reb: 5, stl: 1 }, { matchId: "ga5", date: "10 Ago", opponent: "Saluta Andonio Spurs", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "mattia-sani",     name: "Mattia Sani",     team: "Golden State Worriers", number: 12, pts: 7.5,  ast: 4.0, reb: 2.0, rebOff: 0.5, rebDef: 1.5, stl: 0.8, pp: 2.0, sd: 0.1, ff: 1.8, fs: 2.2, plusMinus: -2.0, val: 7.0,  p2pct: 40.0, p3pct: 32.0, tlpct: 68.0, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Miami Spritz", result: "P", pts: 8, ast: 4, reb: 2, stl: 1 }, { matchId: "ga4", date: "07 Ago", opponent: "Boston Lopez", result: "P", pts: 7, ast: 4, reb: 2, stl: 0 }, { matchId: "ga5", date: "10 Ago", opponent: "Saluta Andonio Spurs", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "federico-poli",   name: "Federico Poli",   team: "Golden State Worriers", number: 24, pts: 5.0,  ast: 1.0, reb: 7.0, rebOff: 3.0, rebDef: 4.0, stl: 0.3, pp: 1.0, sd: 1.2, ff: 2.5, fs: 1.5, plusMinus: -3.0, val: 5.0,  p2pct: 42.0, p3pct: 0.0,  tlpct: 55.0, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Miami Spritz", result: "P", pts: 5, ast: 1, reb: 7, stl: 0 }, { matchId: "ga4", date: "07 Ago", opponent: "Boston Lopez", result: "P", pts: 6, ast: 1, reb: 7, stl: 1 }, { matchId: "ga5", date: "10 Ago", opponent: "Saluta Andonio Spurs", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "claudio-amati",   name: "Claudio Amati",   team: "Golden State Worriers", number: 17, pts: 3.0,  ast: 0.5, reb: 2.5, rebOff: 0.8, rebDef: 1.7, stl: 0.2, pp: 0.8, sd: 0.0, ff: 1.5, fs: 1.0, plusMinus: -2.0, val: 2.0,  p2pct: 36.0, p3pct: 22.0, tlpct: 50.0, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Miami Spritz", result: "P", pts: 3, ast: 1, reb: 3, stl: 0 }, { matchId: "ga4", date: "07 Ago", opponent: "Boston Lopez", result: "P", pts: 3, ast: 0, reb: 2, stl: 0 }, { matchId: "ga5", date: "10 Ago", opponent: "Saluta Andonio Spurs", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },

  // — MINNESODE TIMBERMILF —
  { slug: "alessio-bianchi", name: "Alessio Bianchi", team: "Minnesode Timbermilf", number: 1,  pts: 20.1, ast: 4.5, reb: 5.6, rebOff: 2.0, rebDef: 3.6, stl: 1.2, pp: 2.2, sd: 0.5, ff: 2.2, fs: 3.8, plusMinus: 7.0, val: 19.0, p2pct: 52.0, p3pct: 36.0, tlpct: 80.0, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Denver McNuggets", result: "V", pts: 22, ast: 5, reb: 6, stl: 1 }, { matchId: "gb3", date: "07 Ago", opponent: "Atlanta Robba", result: "P", pts: 24, ast: 4, reb: 5, stl: 2 }, { matchId: "gb5", date: "10 Ago", opponent: "Chicago Poolls", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "simone-neri",     name: "Simone Neri",     team: "Minnesode Timbermilf", number: 3,  pts: 10.5, ast: 8.2, reb: 2.0, rebOff: 0.5, rebDef: 1.5, stl: 2.5, pp: 3.0, sd: 0.2, ff: 1.8, fs: 3.5, plusMinus: 5.0, val: 13.0, p2pct: 44.0, p3pct: 30.0, tlpct: 76.0, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Denver McNuggets", result: "V", pts: 15, ast: 12, reb: 2, stl: 3 }, { matchId: "gb3", date: "07 Ago", opponent: "Atlanta Robba", result: "P", pts: 10, ast: 7, reb: 2, stl: 2 }, { matchId: "gb5", date: "10 Ago", opponent: "Chicago Poolls", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "francesco-rossi", name: "Francesco Rossi", team: "Minnesode Timbermilf", number: 8,  pts: 18.0, ast: 2.1, reb: 4.5, rebOff: 1.5, rebDef: 3.0, stl: 0.9, pp: 1.8, sd: 0.4, ff: 2.0, fs: 3.0, plusMinus: 6.0, val: 17.0, p2pct: 50.0, p3pct: 38.0, tlpct: 82.0, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Denver McNuggets", result: "V", pts: 20, ast: 2, reb: 5, stl: 1 }, { matchId: "gb3", date: "07 Ago", opponent: "Atlanta Robba", result: "P", pts: 18, ast: 2, reb: 4, stl: 1 }, { matchId: "gb5", date: "10 Ago", opponent: "Chicago Poolls", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "davide-verdi",    name: "Davide Verdi",    team: "Minnesode Timbermilf", number: 13, pts: 7.2,  ast: 1.1, reb: 9.0, rebOff: 3.5, rebDef: 5.5, stl: 0.5, pp: 1.2, sd: 1.0, ff: 2.8, fs: 1.5, plusMinus: 3.0, val: 8.0,  p2pct: 46.0, p3pct: 0.0,  tlpct: 62.0, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Denver McNuggets", result: "V", pts: 8, ast: 1, reb: 10, stl: 1 }, { matchId: "gb3", date: "07 Ago", opponent: "Atlanta Robba", result: "P", pts: 7, ast: 1, reb: 8, stl: 0 }, { matchId: "gb5", date: "10 Ago", opponent: "Chicago Poolls", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "lorenzo-gialli",  name: "Lorenzo Gialli",  team: "Minnesode Timbermilf", number: 25, pts: 2.0,  ast: 0.5, reb: 2.1, rebOff: 0.5, rebDef: 1.6, stl: 0.1, pp: 0.5, sd: 0.0, ff: 1.0, fs: 0.8, plusMinus: 1.0, val: 2.0,  p2pct: 35.0, p3pct: 22.0, tlpct: 50.0, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Denver McNuggets", result: "V", pts: 3, ast: 0, reb: 2, stl: 0 }, { matchId: "gb3", date: "07 Ago", opponent: "Atlanta Robba", result: "P", pts: 2, ast: 1, reb: 2, stl: 0 }, { matchId: "gb5", date: "10 Ago", opponent: "Chicago Poolls", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },

  // — DENVER MCNUGGETS —
  { slug: "roberto-piccoli", name: "Roberto Piccoli", team: "Denver McNuggets", number: 5,  pts: 9.0,  ast: 2.0, reb: 7.8, rebOff: 3.0, rebDef: 4.8, stl: 0.5, pp: 1.5, sd: 0.8, ff: 2.5, fs: 2.0, plusMinus: -5.0, val: 8.0,  p2pct: 44.0, p3pct: 0.0,  tlpct: 60.0, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Minnesode Timbermilf", result: "P", pts: 12, ast: 2, reb: 8, stl: 1 }, { matchId: "gb4", date: "07 Ago", opponent: "Chicago Poolls", result: "P", pts: 10, ast: 2, reb: 7, stl: 0 }, { matchId: "gb6", date: "10 Ago", opponent: "Atlanta Robba", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "angelo-serra",    name: "Angelo Serra",    team: "Denver McNuggets", number: 7,  pts: 8.5,  ast: 3.0, reb: 3.5, rebOff: 1.0, rebDef: 2.5, stl: 0.8, pp: 1.5, sd: 0.2, ff: 2.0, fs: 2.5, plusMinus: -4.0, val: 7.0,  p2pct: 42.0, p3pct: 30.0, tlpct: 68.0, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Minnesode Timbermilf", result: "P", pts: 9, ast: 3, reb: 4, stl: 1 }, { matchId: "gb4", date: "07 Ago", opponent: "Chicago Poolls", result: "P", pts: 10, ast: 3, reb: 3, stl: 1 }, { matchId: "gb6", date: "10 Ago", opponent: "Atlanta Robba", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "luigi-russo",     name: "Luigi Russo",     team: "Denver McNuggets", number: 11, pts: 6.0,  ast: 1.5, reb: 4.0, rebOff: 1.2, rebDef: 2.8, stl: 0.4, pp: 1.0, sd: 0.3, ff: 1.8, fs: 1.8, plusMinus: -3.0, val: 5.0,  p2pct: 40.0, p3pct: 0.0,  tlpct: 58.0, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Minnesode Timbermilf", result: "P", pts: 7, ast: 2, reb: 4, stl: 0 }, { matchId: "gb4", date: "07 Ago", opponent: "Chicago Poolls", result: "P", pts: 6, ast: 1, reb: 4, stl: 1 }, { matchId: "gb6", date: "10 Ago", opponent: "Atlanta Robba", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "piero-neri",      name: "Piero Neri",      team: "Denver McNuggets", number: 20, pts: 4.0,  ast: 0.8, reb: 5.5, rebOff: 2.0, rebDef: 3.5, stl: 0.2, pp: 0.8, sd: 0.5, ff: 2.0, fs: 1.5, plusMinus: -3.0, val: 4.0,  p2pct: 38.0, p3pct: 0.0,  tlpct: 52.0, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Minnesode Timbermilf", result: "P", pts: 5, ast: 1, reb: 6, stl: 0 }, { matchId: "gb4", date: "07 Ago", opponent: "Chicago Poolls", result: "P", pts: 4, ast: 1, reb: 5, stl: 0 }, { matchId: "gb6", date: "10 Ago", opponent: "Atlanta Robba", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "aldo-mari",       name: "Aldo Mari",       team: "Denver McNuggets", number: 32, pts: 2.5,  ast: 0.5, reb: 2.0, rebOff: 0.5, rebDef: 1.5, stl: 0.1, pp: 0.5, sd: 0.0, ff: 1.2, fs: 1.0, plusMinus: -2.0, val: 2.0,  p2pct: 34.0, p3pct: 20.0, tlpct: 48.0, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Minnesode Timbermilf", result: "P", pts: 3, ast: 0, reb: 2, stl: 0 }, { matchId: "gb4", date: "07 Ago", opponent: "Chicago Poolls", result: "P", pts: 3, ast: 1, reb: 2, stl: 0 }, { matchId: "gb6", date: "10 Ago", opponent: "Atlanta Robba", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },

  // — ATLANTA ROBBA —
  { slug: "leo-verdi",       name: "Leo Verdi",       team: "Atlanta Robba", number: 23, pts: 21.8, ast: 6.5, reb: 3.2, rebOff: 0.8, rebDef: 2.4, stl: 2.1, pp: 2.5, sd: 0.3, ff: 2.0, fs: 4.0, plusMinus: 9.0, val: 21.0, p2pct: 56.0, p3pct: 38.0, tlpct: 84.0, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Chicago Poolls", result: "V", pts: 24, ast: 6, reb: 3, stl: 2 }, { matchId: "gb3", date: "07 Ago", opponent: "Minnesode Timbermilf", result: "V", pts: 21, ast: 7, reb: 4, stl: 2 }, { matchId: "gb6", date: "10 Ago", opponent: "Denver McNuggets", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "marco-bianchi",   name: "Marco Bianchi",   team: "Atlanta Robba", number: 4,  pts: 16.5, ast: 2.0, reb: 5.5, rebOff: 2.0, rebDef: 3.5, stl: 1.0, pp: 1.8, sd: 0.6, ff: 2.2, fs: 3.0, plusMinus: 7.0, val: 15.0, p2pct: 50.0, p3pct: 0.0,  tlpct: 76.0, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Chicago Poolls", result: "V", pts: 18, ast: 2, reb: 6, stl: 1 }, { matchId: "gb3", date: "07 Ago", opponent: "Minnesode Timbermilf", result: "V", pts: 16, ast: 2, reb: 5, stl: 1 }, { matchId: "gb6", date: "10 Ago", opponent: "Denver McNuggets", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "giovanni-rossi",  name: "Giovanni Rossi",  team: "Atlanta Robba", number: 9,  pts: 12.0, ast: 3.5, reb: 4.0, rebOff: 1.2, rebDef: 2.8, stl: 0.5, pp: 1.5, sd: 0.3, ff: 2.0, fs: 2.5, plusMinus: 5.0, val: 11.0, p2pct: 48.0, p3pct: 32.0, tlpct: 72.0, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Chicago Poolls", result: "V", pts: 13, ast: 4, reb: 4, stl: 0 }, { matchId: "gb3", date: "07 Ago", opponent: "Minnesode Timbermilf", result: "V", pts: 12, ast: 3, reb: 4, stl: 1 }, { matchId: "gb6", date: "10 Ago", opponent: "Denver McNuggets", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "luca-neri-atl",   name: "Luca Neri",       team: "Atlanta Robba", number: 16, pts: 8.5,  ast: 1.0, reb: 8.5, rebOff: 3.5, rebDef: 5.0, stl: 0.8, pp: 1.0, sd: 1.0, ff: 2.5, fs: 2.0, plusMinus: 4.0, val: 9.0,  p2pct: 46.0, p3pct: 0.0,  tlpct: 64.0, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Chicago Poolls", result: "V", pts: 9, ast: 1, reb: 9, stl: 1 }, { matchId: "gb3", date: "07 Ago", opponent: "Minnesode Timbermilf", result: "V", pts: 8, ast: 1, reb: 8, stl: 0 }, { matchId: "gb6", date: "10 Ago", opponent: "Denver McNuggets", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "matteo-gialli",   name: "Matteo Gialli",   team: "Atlanta Robba", number: 31, pts: 4.0,  ast: 0.5, reb: 1.5, rebOff: 0.3, rebDef: 1.2, stl: 0.2, pp: 0.8, sd: 0.0, ff: 1.2, fs: 1.0, plusMinus: 2.0, val: 3.0,  p2pct: 38.0, p3pct: 25.0, tlpct: 55.0, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Chicago Poolls", result: "V", pts: 5, ast: 1, reb: 2, stl: 0 }, { matchId: "gb3", date: "07 Ago", opponent: "Minnesode Timbermilf", result: "V", pts: 4, ast: 0, reb: 1, stl: 0 }, { matchId: "gb6", date: "10 Ago", opponent: "Denver McNuggets", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },

  // — CHICAGO POOLLS —
  { slug: "davide-mori",     name: "Davide Mori",     team: "Chicago Poolls", number: 11, pts: 14.0, ast: 4.8, reb: 3.5, rebOff: 1.0, rebDef: 2.5, stl: 1.0, pp: 2.0, sd: 0.3, ff: 2.5, fs: 3.2, plusMinus: 1.0, val: 13.0, p2pct: 50.0, p3pct: 34.0, tlpct: 78.0, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Atlanta Robba", result: "P", pts: 15, ast: 5, reb: 4, stl: 1 }, { matchId: "gb4", date: "07 Ago", opponent: "Denver McNuggets", result: "V", pts: 16, ast: 5, reb: 3, stl: 1 }, { matchId: "gb5", date: "10 Ago", opponent: "Minnesode Timbermilf", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "riccardo-sala",   name: "Riccardo Sala",   team: "Chicago Poolls", number: 6,  pts: 10.5, ast: 2.5, reb: 5.0, rebOff: 1.5, rebDef: 3.5, stl: 0.7, pp: 1.5, sd: 0.5, ff: 2.2, fs: 2.5, plusMinus: 0.0, val: 10.0, p2pct: 46.0, p3pct: 0.0,  tlpct: 68.0, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Atlanta Robba", result: "P", pts: 12, ast: 3, reb: 5, stl: 1 }, { matchId: "gb4", date: "07 Ago", opponent: "Denver McNuggets", result: "V", pts: 11, ast: 2, reb: 5, stl: 0 }, { matchId: "gb5", date: "10 Ago", opponent: "Minnesode Timbermilf", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "pietro-conti",    name: "Pietro Conti",    team: "Chicago Poolls", number: 14, pts: 8.0,  ast: 1.8, reb: 4.5, rebOff: 1.5, rebDef: 3.0, stl: 0.5, pp: 1.2, sd: 0.4, ff: 2.0, fs: 2.0, plusMinus: -1.0, val: 7.0,  p2pct: 44.0, p3pct: 30.0, tlpct: 65.0, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Atlanta Robba", result: "P", pts: 9, ast: 2, reb: 5, stl: 1 }, { matchId: "gb4", date: "07 Ago", opponent: "Denver McNuggets", result: "V", pts: 8, ast: 2, reb: 4, stl: 0 }, { matchId: "gb5", date: "10 Ago", opponent: "Minnesode Timbermilf", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "marco-serra",     name: "Marco Serra",     team: "Chicago Poolls", number: 21, pts: 5.5,  ast: 1.0, reb: 6.5, rebOff: 2.5, rebDef: 4.0, stl: 0.3, pp: 1.0, sd: 0.8, ff: 2.2, fs: 1.8, plusMinus: -2.0, val: 5.0,  p2pct: 40.0, p3pct: 0.0,  tlpct: 58.0, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Atlanta Robba", result: "P", pts: 7, ast: 1, reb: 7, stl: 0 }, { matchId: "gb4", date: "07 Ago", opponent: "Denver McNuggets", result: "V", pts: 6, ast: 1, reb: 6, stl: 1 }, { matchId: "gb5", date: "10 Ago", opponent: "Minnesode Timbermilf", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "fabio-neri",      name: "Fabio Neri",      team: "Chicago Poolls", number: 28, pts: 3.0,  ast: 0.5, reb: 2.0, rebOff: 0.5, rebDef: 1.5, stl: 0.2, pp: 0.8, sd: 0.0, ff: 1.5, fs: 1.2, plusMinus: -1.0, val: 2.0,  p2pct: 35.0, p3pct: 22.0, tlpct: 50.0, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Atlanta Robba", result: "P", pts: 4, ast: 1, reb: 2, stl: 0 }, { matchId: "gb4", date: "07 Ago", opponent: "Denver McNuggets", result: "V", pts: 3, ast: 0, reb: 2, stl: 0 }, { matchId: "gb5", date: "10 Ago", opponent: "Minnesode Timbermilf", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
];

const matchMvpsData: MatchMvp[] = [
  { match: "Saluta Andonio Spurs vs Boston Lopez",      date: "05 Ago", player: "Marco Rossi",    team: "Saluta Andonio Spurs",  stat: "22 pts" },
  { match: "Miami Spritz vs Golden State Worriers",     date: "05 Ago", player: "Carlo Neri",     team: "Miami Spritz",          stat: "19 pts" },
  { match: "Minnesode Timbermilf vs Denver McNuggets",  date: "05 Ago", player: "Simone Neri",    team: "Minnesode Timbermilf",  stat: "15 pts · 12 ast" },
  { match: "Atlanta Robba vs Chicago Poolls",           date: "05 Ago", player: "Leo Verdi",      team: "Atlanta Robba",         stat: "24 pts" },
  { match: "Saluta Andonio Spurs vs Miami Spritz",      date: "07 Ago", player: "Luca Bianchi",   team: "Saluta Andonio Spurs",  stat: "18 pts · 10 reb" },
  { match: "Boston Lopez vs Golden State Worriers",     date: "07 Ago", player: "Filippo Conti",  team: "Boston Lopez",          stat: "17 pts" },
  { match: "Minnesode Timbermilf vs Atlanta Robba",     date: "07 Ago", player: "Leo Verdi",      team: "Atlanta Robba",         stat: "21 pts · game winner" },
  { match: "Denver McNuggets vs Chicago Poolls",        date: "07 Ago", player: "Davide Mori",    team: "Chicago Poolls",        stat: "16 pts" },
];

const teamStatsData: TeamStats[] = [
  { squadra: "Saluta Andonio Spurs",  partiteGiocate: 2, punti: 140, puntiSubiti: 110, assist: 32, rimbalzi: 56, recuperi: 12, stoppate: 6, pallePerse: 18 },
  { squadra: "Atlanta Robba",         partiteGiocate: 2, punti: 130, puntiSubiti: 115, assist: 28, rimbalzi: 50, recuperi: 10, stoppate: 8, pallePerse: 20 },
  { squadra: "Minnesode Timbermilf",  partiteGiocate: 2, punti: 125, puntiSubiti: 120, assist: 24, rimbalzi: 48, recuperi: 14, stoppate: 4, pallePerse: 16 },
  { squadra: "Miami Spritz",          partiteGiocate: 2, punti: 120, puntiSubiti: 118, assist: 30, rimbalzi: 44, recuperi: 8,  stoppate: 6, pallePerse: 22 },
  { squadra: "Boston Lopez",          partiteGiocate: 2, punti: 115, puntiSubiti: 125, assist: 22, rimbalzi: 52, recuperi: 10, stoppate: 5, pallePerse: 14 },
  { squadra: "Chicago Poolls",        partiteGiocate: 2, punti: 110, puntiSubiti: 130, assist: 20, rimbalzi: 46, recuperi: 6,  stoppate: 4, pallePerse: 24 },
  { squadra: "Denver McNuggets",      partiteGiocate: 2, punti: 105, puntiSubiti: 128, assist: 18, rimbalzi: 42, recuperi: 12, stoppate: 3, pallePerse: 20 },
  { squadra: "Golden State Worriers", partiteGiocate: 2, punti: 100, puntiSubiti: 135, assist: 16, rimbalzi: 40, recuperi: 8,  stoppate: 2, pallePerse: 26 },
];

export const defaultStatsData: StatsData = {
  tournamentMvpSlug: "marco-rossi",
  players,
  matchMvps: matchMvpsData,
  teamStats: teamStatsData,
};

// Esportazioni backward-compatible per PlayerDetail.tsx
export const allPlayers = players;
export const matchMvps = matchMvpsData;
export const tournamentMvpSlug = "marco-rossi";
export const teams = [...new Set(players.map(p => p.team))];
