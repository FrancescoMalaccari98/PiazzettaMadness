export type MatchLog = {
  matchId: string;
  date: string;
  opponent: string;
  result: "V" | "P" | "-";
  pts: number; ast: number; reb: number; stl: number;
};

export type Player = {
  slug: string;
  name: string;
  team: string;
  number?: number;
  photo?: string; // path in /public, es. "/players/marco-rossi.jpg"
  pts: number; ast: number; reb: number; stl: number;
  matchLog: MatchLog[];
};

export type MatchMvp = {
  match: string;
  date: string;
  player: string;
  team: string;
  stat: string;
};

export const allPlayers: Player[] = [
  // — SALUTA ANDONIO SPURS —
  {
    slug: "marco-rossi", name: "Marco Rossi", team: "Saluta Andonio Spurs", number: 10,
    pts: 24.5, ast: 5.1, reb: 4.2, stl: 1.5,
    matchLog: [
      { matchId: "ga1", date: "05 Ago", opponent: "Boston Lopez",      result: "V", pts: 22, ast: 6, reb: 4, stl: 2 },
      { matchId: "ga3", date: "07 Ago", opponent: "Miami Spritz",       result: "V", pts: 28, ast: 5, reb: 5, stl: 1 },
      { matchId: "ga5", date: "10 Ago", opponent: "Golden State Worriers", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 },
    ],
  },
  {
    slug: "giovanni-neri", name: "Giovanni Neri", team: "Saluta Andonio Spurs", number: 5,
    pts: 15.2, ast: 3.0, reb: 8.5, stl: 0.8,
    matchLog: [
      { matchId: "ga1", date: "05 Ago", opponent: "Boston Lopez",      result: "V", pts: 18, ast: 3, reb: 9, stl: 1 },
      { matchId: "ga3", date: "07 Ago", opponent: "Miami Spritz",       result: "V", pts: 14, ast: 3, reb: 8, stl: 0 },
      { matchId: "ga5", date: "10 Ago", opponent: "Golden State Worriers", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 },
    ],
  },
  {
    slug: "luca-bianchi", name: "Luca Bianchi", team: "Saluta Andonio Spurs", number: 3,
    pts: 8.5, ast: 6.2, reb: 2.1, stl: 2.0,
    matchLog: [
      { matchId: "ga1", date: "05 Ago", opponent: "Boston Lopez",      result: "V", pts: 9,  ast: 7, reb: 2, stl: 3 },
      { matchId: "ga3", date: "07 Ago", opponent: "Miami Spritz",       result: "V", pts: 18, ast: 6, reb: 2, stl: 1 },
      { matchId: "ga5", date: "10 Ago", opponent: "Golden State Worriers", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 },
    ],
  },
  {
    slug: "matteo-verdi", name: "Matteo Verdi", team: "Saluta Andonio Spurs", number: 14,
    pts: 6.0, ast: 1.5, reb: 10.2, stl: 0.2,
    matchLog: [
      { matchId: "ga1", date: "05 Ago", opponent: "Boston Lopez",      result: "V", pts: 10, ast: 2, reb: 11, stl: 0 },
      { matchId: "ga3", date: "07 Ago", opponent: "Miami Spritz",       result: "V", pts: 7,  ast: 1, reb: 10, stl: 0 },
      { matchId: "ga5", date: "10 Ago", opponent: "Golden State Worriers", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 },
    ],
  },
  {
    slug: "andrea-gialli", name: "Andrea Gialli", team: "Saluta Andonio Spurs", number: 21,
    pts: 4.5, ast: 2.0, reb: 3.0, stl: 1.1,
    matchLog: [
      { matchId: "ga1", date: "05 Ago", opponent: "Boston Lopez",      result: "V", pts: 7,  ast: 2, reb: 3, stl: 2 },
      { matchId: "ga3", date: "07 Ago", opponent: "Miami Spritz",       result: "V", pts: 6,  ast: 2, reb: 3, stl: 1 },
      { matchId: "ga5", date: "10 Ago", opponent: "Golden State Worriers", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 },
    ],
  },

  // — BOSTON LOPEZ —
  {
    slug: "filippo-conti", name: "Filippo Conti", team: "Boston Lopez", number: 7,
    pts: 16.2, ast: 4.0, reb: 3.5, stl: 1.3,
    matchLog: [
      { matchId: "ga1", date: "05 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 17, ast: 4, reb: 3, stl: 1 },
      { matchId: "ga4", date: "07 Ago", opponent: "Golden State Worriers", result: "V", pts: 17, ast: 4, reb: 4, stl: 2 },
      { matchId: "ga6", date: "10 Ago", opponent: "Miami Spritz",           result: "-", pts: 0,  ast: 0, reb: 0, stl: 0 },
    ],
  },
  {
    slug: "roberto-serra", name: "Roberto Serra", team: "Boston Lopez", number: 11,
    pts: 12.0, ast: 2.5, reb: 6.0, stl: 0.7,
    matchLog: [
      { matchId: "ga1", date: "05 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 15, ast: 3, reb: 6, stl: 1 },
      { matchId: "ga4", date: "07 Ago", opponent: "Golden State Worriers", result: "V", pts: 12, ast: 2, reb: 6, stl: 0 },
      { matchId: "ga6", date: "10 Ago", opponent: "Miami Spritz",           result: "-", pts: 0,  ast: 0, reb: 0, stl: 0 },
    ],
  },
  {
    slug: "diego-palma", name: "Diego Palma", team: "Boston Lopez", number: 4,
    pts: 9.5, ast: 3.2, reb: 4.0, stl: 0.5,
    matchLog: [
      { matchId: "ga1", date: "05 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 12, ast: 3, reb: 4, stl: 0 },
      { matchId: "ga4", date: "07 Ago", opponent: "Golden State Worriers", result: "V", pts: 9,  ast: 4, reb: 4, stl: 1 },
      { matchId: "ga6", date: "10 Ago", opponent: "Miami Spritz",           result: "-", pts: 0,  ast: 0, reb: 0, stl: 0 },
    ],
  },
  {
    slug: "enrico-mora", name: "Enrico Mora", team: "Boston Lopez", number: 9,
    pts: 6.0, ast: 1.0, reb: 7.5, stl: 0.3,
    matchLog: [
      { matchId: "ga1", date: "05 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 10, ast: 1, reb: 8, stl: 1 },
      { matchId: "ga4", date: "07 Ago", opponent: "Golden State Worriers", result: "V", pts: 6,  ast: 1, reb: 7, stl: 0 },
      { matchId: "ga6", date: "10 Ago", opponent: "Miami Spritz",           result: "-", pts: 0,  ast: 0, reb: 0, stl: 0 },
    ],
  },
  {
    slug: "stefano-riva", name: "Stefano Riva", team: "Boston Lopez", number: 22,
    pts: 3.5, ast: 0.8, reb: 2.0, stl: 0.2,
    matchLog: [
      { matchId: "ga1", date: "05 Ago", opponent: "Saluta Andonio Spurs", result: "P", pts: 6, ast: 1, reb: 2, stl: 0 },
      { matchId: "ga4", date: "07 Ago", opponent: "Golden State Worriers", result: "V", pts: 4, ast: 1, reb: 2, stl: 0 },
      { matchId: "ga6", date: "10 Ago", opponent: "Miami Spritz",           result: "-", pts: 0, ast: 0, reb: 0, stl: 0 },
    ],
  },

  // — MIAMI SPRITZ —
  {
    slug: "carlo-neri", name: "Carlo Neri", team: "Miami Spritz", number: 6,
    pts: 17.5, ast: 3.8, reb: 3.0, stl: 1.3,
    matchLog: [
      { matchId: "ga2", date: "05 Ago", opponent: "Golden State Worriers", result: "V", pts: 19, ast: 4, reb: 3, stl: 1 },
      { matchId: "ga3", date: "07 Ago", opponent: "Saluta Andonio Spurs",  result: "P", pts: 22, ast: 4, reb: 3, stl: 2 },
      { matchId: "ga6", date: "10 Ago", opponent: "Boston Lopez",           result: "-", pts: 0,  ast: 0, reb: 0, stl: 0 },
    ],
  },
  {
    slug: "pietro-russo", name: "Pietro Russo", team: "Miami Spritz", number: 8,
    pts: 13.0, ast: 2.0, reb: 5.0, stl: 0.6,
    matchLog: [
      { matchId: "ga2", date: "05 Ago", opponent: "Golden State Worriers", result: "V", pts: 14, ast: 2, reb: 5, stl: 1 },
      { matchId: "ga3", date: "07 Ago", opponent: "Saluta Andonio Spurs",  result: "P", pts: 18, ast: 2, reb: 5, stl: 0 },
      { matchId: "ga6", date: "10 Ago", opponent: "Boston Lopez",           result: "-", pts: 0,  ast: 0, reb: 0, stl: 0 },
    ],
  },
  {
    slug: "fabio-costa", name: "Fabio Costa", team: "Miami Spritz", number: 15,
    pts: 8.0, ast: 4.5, reb: 2.5, stl: 0.9,
    matchLog: [
      { matchId: "ga2", date: "05 Ago", opponent: "Golden State Worriers", result: "V", pts: 8,  ast: 5, reb: 3, stl: 1 },
      { matchId: "ga3", date: "07 Ago", opponent: "Saluta Andonio Spurs",  result: "P", pts: 12, ast: 4, reb: 2, stl: 1 },
      { matchId: "ga6", date: "10 Ago", opponent: "Boston Lopez",           result: "-", pts: 0,  ast: 0, reb: 0, stl: 0 },
    ],
  },
  {
    slug: "nicola-bruno", name: "Nicola Bruno", team: "Miami Spritz", number: 33,
    pts: 5.5, ast: 1.2, reb: 6.0, stl: 0.4,
    matchLog: [
      { matchId: "ga2", date: "05 Ago", opponent: "Golden State Worriers", result: "V", pts: 7,  ast: 1, reb: 6, stl: 0 },
      { matchId: "ga3", date: "07 Ago", opponent: "Saluta Andonio Spurs",  result: "P", pts: 8,  ast: 2, reb: 6, stl: 1 },
      { matchId: "ga6", date: "10 Ago", opponent: "Boston Lopez",           result: "-", pts: 0,  ast: 0, reb: 0, stl: 0 },
    ],
  },
  {
    slug: "davide-ricci", name: "Davide Ricci", team: "Miami Spritz", number: 19,
    pts: 2.0, ast: 0.5, reb: 1.5, stl: 0.1,
    matchLog: [
      { matchId: "ga2", date: "05 Ago", opponent: "Golden State Worriers", result: "V", pts: 3, ast: 1, reb: 2, stl: 0 },
      { matchId: "ga3", date: "07 Ago", opponent: "Saluta Andonio Spurs",  result: "P", pts: 4, ast: 0, reb: 1, stl: 0 },
      { matchId: "ga6", date: "10 Ago", opponent: "Boston Lopez",           result: "-", pts: 0, ast: 0, reb: 0, stl: 0 },
    ],
  },

  // — GOLDEN STATE WORRIERS —
  { slug: "alessandro-mari",  name: "Alessandro Mari",  team: "Golden State Worriers", number: 30, pts: 11.0, ast: 3.5, reb: 4.0, stl: 1.0, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Miami Spritz", result: "P", pts: 11, ast: 4, reb: 4, stl: 1 }, { matchId: "ga4", date: "07 Ago", opponent: "Boston Lopez", result: "P", pts: 12, ast: 3, reb: 4, stl: 1 }, { matchId: "ga5", date: "10 Ago", opponent: "Saluta Andonio Spurs", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "lorenzo-galli",    name: "Lorenzo Galli",    team: "Golden State Worriers", number: 2,  pts: 9.0,  ast: 2.0, reb: 5.5, stl: 0.5, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Miami Spritz", result: "P", pts: 9, ast: 2, reb: 6, stl: 0 }, { matchId: "ga4", date: "07 Ago", opponent: "Boston Lopez", result: "P", pts: 10, ast: 2, reb: 5, stl: 1 }, { matchId: "ga5", date: "10 Ago", opponent: "Saluta Andonio Spurs", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "mattia-sani",      name: "Mattia Sani",      team: "Golden State Worriers", number: 12, pts: 7.5,  ast: 4.0, reb: 2.0, stl: 0.8, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Miami Spritz", result: "P", pts: 8, ast: 4, reb: 2, stl: 1 }, { matchId: "ga4", date: "07 Ago", opponent: "Boston Lopez", result: "P", pts: 7, ast: 4, reb: 2, stl: 0 }, { matchId: "ga5", date: "10 Ago", opponent: "Saluta Andonio Spurs", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "federico-poli",    name: "Federico Poli",    team: "Golden State Worriers", number: 24, pts: 5.0,  ast: 1.0, reb: 7.0, stl: 0.3, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Miami Spritz", result: "P", pts: 5, ast: 1, reb: 7, stl: 0 }, { matchId: "ga4", date: "07 Ago", opponent: "Boston Lopez", result: "P", pts: 6, ast: 1, reb: 7, stl: 1 }, { matchId: "ga5", date: "10 Ago", opponent: "Saluta Andonio Spurs", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "claudio-amati",    name: "Claudio Amati",    team: "Golden State Worriers", number: 17, pts: 3.0,  ast: 0.5, reb: 2.5, stl: 0.2, matchLog: [{ matchId: "ga2", date: "05 Ago", opponent: "Miami Spritz", result: "P", pts: 3, ast: 1, reb: 3, stl: 0 }, { matchId: "ga4", date: "07 Ago", opponent: "Boston Lopez", result: "P", pts: 3, ast: 0, reb: 2, stl: 0 }, { matchId: "ga5", date: "10 Ago", opponent: "Saluta Andonio Spurs", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },

  // — MINNESODE TIMBERMILF —
  {
    slug: "alessio-bianchi", name: "Alessio Bianchi", team: "Minnesode Timbermilf", number: 1,
    pts: 20.1, ast: 4.5, reb: 5.6, stl: 1.2,
    matchLog: [
      { matchId: "gb1", date: "05 Ago", opponent: "Denver McNuggets", result: "V", pts: 22, ast: 5, reb: 6, stl: 1 },
      { matchId: "gb3", date: "07 Ago", opponent: "Atlanta Robba",    result: "P", pts: 24, ast: 4, reb: 5, stl: 2 },
      { matchId: "gb5", date: "10 Ago", opponent: "Chicago Poolls",   result: "-", pts: 0,  ast: 0, reb: 0, stl: 0 },
    ],
  },
  {
    slug: "simone-neri", name: "Simone Neri", team: "Minnesode Timbermilf", number: 3,
    pts: 10.5, ast: 8.2, reb: 2.0, stl: 2.5,
    matchLog: [
      { matchId: "gb1", date: "05 Ago", opponent: "Denver McNuggets", result: "V", pts: 15, ast: 12, reb: 2, stl: 3 },
      { matchId: "gb3", date: "07 Ago", opponent: "Atlanta Robba",    result: "P", pts: 10, ast: 7,  reb: 2, stl: 2 },
      { matchId: "gb5", date: "10 Ago", opponent: "Chicago Poolls",   result: "-", pts: 0,  ast: 0,  reb: 0, stl: 0 },
    ],
  },
  { slug: "francesco-rossi", name: "Francesco Rossi", team: "Minnesode Timbermilf", number: 8,  pts: 18.0, ast: 2.1, reb: 4.5, stl: 0.9, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Denver McNuggets", result: "V", pts: 20, ast: 2, reb: 5, stl: 1 }, { matchId: "gb3", date: "07 Ago", opponent: "Atlanta Robba", result: "P", pts: 18, ast: 2, reb: 4, stl: 1 }, { matchId: "gb5", date: "10 Ago", opponent: "Chicago Poolls", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "davide-verdi",    name: "Davide Verdi",    team: "Minnesode Timbermilf", number: 13, pts: 7.2,  ast: 1.1, reb: 9.0, stl: 0.5, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Denver McNuggets", result: "V", pts: 8,  ast: 1, reb: 10, stl: 1 }, { matchId: "gb3", date: "07 Ago", opponent: "Atlanta Robba", result: "P", pts: 7, ast: 1, reb: 8, stl: 0 }, { matchId: "gb5", date: "10 Ago", opponent: "Chicago Poolls", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "lorenzo-gialli",  name: "Lorenzo Gialli",  team: "Minnesode Timbermilf", number: 25, pts: 2.0,  ast: 0.5, reb: 2.1, stl: 0.1, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Denver McNuggets", result: "V", pts: 3, ast: 0, reb: 2, stl: 0 }, { matchId: "gb3", date: "07 Ago", opponent: "Atlanta Robba", result: "P", pts: 2, ast: 1, reb: 2, stl: 0 }, { matchId: "gb5", date: "10 Ago", opponent: "Chicago Poolls", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },

  // — DENVER MCNUGGETS —
  { slug: "roberto-piccoli", name: "Roberto Piccoli", team: "Denver McNuggets", number: 5,  pts: 9.0, ast: 2.0, reb: 7.8, stl: 0.5, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Minnesode Timbermilf", result: "P", pts: 12, ast: 2, reb: 8, stl: 1 }, { matchId: "gb4", date: "07 Ago", opponent: "Chicago Poolls", result: "P", pts: 10, ast: 2, reb: 7, stl: 0 }, { matchId: "gb6", date: "10 Ago", opponent: "Atlanta Robba", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "angelo-serra",    name: "Angelo Serra",    team: "Denver McNuggets", number: 7,  pts: 8.5, ast: 3.0, reb: 3.5, stl: 0.8, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Minnesode Timbermilf", result: "P", pts: 9,  ast: 3, reb: 4, stl: 1 }, { matchId: "gb4", date: "07 Ago", opponent: "Chicago Poolls", result: "P", pts: 10, ast: 3, reb: 3, stl: 1 }, { matchId: "gb6", date: "10 Ago", opponent: "Atlanta Robba", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "luigi-russo",     name: "Luigi Russo",     team: "Denver McNuggets", number: 11, pts: 6.0, ast: 1.5, reb: 4.0, stl: 0.4, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Minnesode Timbermilf", result: "P", pts: 7,  ast: 2, reb: 4, stl: 0 }, { matchId: "gb4", date: "07 Ago", opponent: "Chicago Poolls", result: "P", pts: 6, ast: 1, reb: 4, stl: 1 }, { matchId: "gb6", date: "10 Ago", opponent: "Atlanta Robba", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "piero-neri",      name: "Piero Neri",      team: "Denver McNuggets", number: 20, pts: 4.0, ast: 0.8, reb: 5.5, stl: 0.2, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Minnesode Timbermilf", result: "P", pts: 5,  ast: 1, reb: 6, stl: 0 }, { matchId: "gb4", date: "07 Ago", opponent: "Chicago Poolls", result: "P", pts: 4, ast: 1, reb: 5, stl: 0 }, { matchId: "gb6", date: "10 Ago", opponent: "Atlanta Robba", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "aldo-mari",       name: "Aldo Mari",       team: "Denver McNuggets", number: 32, pts: 2.5, ast: 0.5, reb: 2.0, stl: 0.1, matchLog: [{ matchId: "gb1", date: "05 Ago", opponent: "Minnesode Timbermilf", result: "P", pts: 3,  ast: 0, reb: 2, stl: 0 }, { matchId: "gb4", date: "07 Ago", opponent: "Chicago Poolls", result: "P", pts: 3, ast: 1, reb: 2, stl: 0 }, { matchId: "gb6", date: "10 Ago", opponent: "Atlanta Robba", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },

  // — ATLANTA ROBBA —
  {
    slug: "leo-verdi", name: "Leo Verdi", team: "Atlanta Robba", number: 23,
    pts: 21.8, ast: 6.5, reb: 3.2, stl: 2.1,
    matchLog: [
      { matchId: "gb2", date: "05 Ago", opponent: "Chicago Poolls",         result: "V", pts: 24, ast: 6, reb: 3, stl: 2 },
      { matchId: "gb3", date: "07 Ago", opponent: "Minnesode Timbermilf",   result: "V", pts: 21, ast: 7, reb: 4, stl: 2 },
      { matchId: "gb6", date: "10 Ago", opponent: "Denver McNuggets",       result: "-", pts: 0,  ast: 0, reb: 0, stl: 0 },
    ],
  },
  { slug: "marco-bianchi",   name: "Marco Bianchi",   team: "Atlanta Robba", number: 4,  pts: 16.5, ast: 2.0, reb: 5.5, stl: 1.0, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Chicago Poolls", result: "V", pts: 18, ast: 2, reb: 6, stl: 1 }, { matchId: "gb3", date: "07 Ago", opponent: "Minnesode Timbermilf", result: "V", pts: 16, ast: 2, reb: 5, stl: 1 }, { matchId: "gb6", date: "10 Ago", opponent: "Denver McNuggets", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "giovanni-rossi",  name: "Giovanni Rossi",  team: "Atlanta Robba", number: 9,  pts: 12.0, ast: 3.5, reb: 4.0, stl: 0.5, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Chicago Poolls", result: "V", pts: 13, ast: 4, reb: 4, stl: 0 }, { matchId: "gb3", date: "07 Ago", opponent: "Minnesode Timbermilf", result: "V", pts: 12, ast: 3, reb: 4, stl: 1 }, { matchId: "gb6", date: "10 Ago", opponent: "Denver McNuggets", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "luca-neri-atl",   name: "Luca Neri",       team: "Atlanta Robba", number: 16, pts: 8.5,  ast: 1.0, reb: 8.5, stl: 0.8, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Chicago Poolls", result: "V", pts: 9,  ast: 1, reb: 9, stl: 1 }, { matchId: "gb3", date: "07 Ago", opponent: "Minnesode Timbermilf", result: "V", pts: 8, ast: 1, reb: 8, stl: 0 }, { matchId: "gb6", date: "10 Ago", opponent: "Denver McNuggets", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "matteo-gialli",   name: "Matteo Gialli",   team: "Atlanta Robba", number: 31, pts: 4.0,  ast: 0.5, reb: 1.5, stl: 0.2, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Chicago Poolls", result: "V", pts: 5,  ast: 1, reb: 2, stl: 0 }, { matchId: "gb3", date: "07 Ago", opponent: "Minnesode Timbermilf", result: "V", pts: 4, ast: 0, reb: 1, stl: 0 }, { matchId: "gb6", date: "10 Ago", opponent: "Denver McNuggets", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },

  // — CHICAGO POOLLS —
  {
    slug: "davide-mori", name: "Davide Mori", team: "Chicago Poolls", number: 11,
    pts: 14.0, ast: 4.8, reb: 3.5, stl: 1.0,
    matchLog: [
      { matchId: "gb2", date: "05 Ago", opponent: "Atlanta Robba",    result: "P", pts: 15, ast: 5, reb: 4, stl: 1 },
      { matchId: "gb4", date: "07 Ago", opponent: "Denver McNuggets", result: "V", pts: 16, ast: 5, reb: 3, stl: 1 },
      { matchId: "gb5", date: "10 Ago", opponent: "Minnesode Timbermilf", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 },
    ],
  },
  { slug: "riccardo-sala",  name: "Riccardo Sala",  team: "Chicago Poolls", number: 6,  pts: 10.5, ast: 2.5, reb: 5.0, stl: 0.7, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Atlanta Robba", result: "P", pts: 12, ast: 3, reb: 5, stl: 1 }, { matchId: "gb4", date: "07 Ago", opponent: "Denver McNuggets", result: "V", pts: 11, ast: 2, reb: 5, stl: 0 }, { matchId: "gb5", date: "10 Ago", opponent: "Minnesode Timbermilf", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "pietro-conti",   name: "Pietro Conti",   team: "Chicago Poolls", number: 14, pts: 8.0,  ast: 1.8, reb: 4.5, stl: 0.5, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Atlanta Robba", result: "P", pts: 9,  ast: 2, reb: 5, stl: 1 }, { matchId: "gb4", date: "07 Ago", opponent: "Denver McNuggets", result: "V", pts: 8, ast: 2, reb: 4, stl: 0 }, { matchId: "gb5", date: "10 Ago", opponent: "Minnesode Timbermilf", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "marco-serra",    name: "Marco Serra",    team: "Chicago Poolls", number: 21, pts: 5.5,  ast: 1.0, reb: 6.5, stl: 0.3, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Atlanta Robba", result: "P", pts: 7,  ast: 1, reb: 7, stl: 0 }, { matchId: "gb4", date: "07 Ago", opponent: "Denver McNuggets", result: "V", pts: 6, ast: 1, reb: 6, stl: 1 }, { matchId: "gb5", date: "10 Ago", opponent: "Minnesode Timbermilf", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
  { slug: "fabio-neri",     name: "Fabio Neri",     team: "Chicago Poolls", number: 28, pts: 3.0,  ast: 0.5, reb: 2.0, stl: 0.2, matchLog: [{ matchId: "gb2", date: "05 Ago", opponent: "Atlanta Robba", result: "P", pts: 4,  ast: 1, reb: 2, stl: 0 }, { matchId: "gb4", date: "07 Ago", opponent: "Denver McNuggets", result: "V", pts: 3, ast: 0, reb: 2, stl: 0 }, { matchId: "gb5", date: "10 Ago", opponent: "Minnesode Timbermilf", result: "-", pts: 0, ast: 0, reb: 0, stl: 0 }] },
];

export const matchMvps: MatchMvp[] = [
  { match: "Saluta Andonio Spurs vs Boston Lopez",      date: "05 Ago", player: "Marco Rossi",    team: "Saluta Andonio Spurs",  stat: "22 pts" },
  { match: "Miami Spritz vs Golden State Worriers",     date: "05 Ago", player: "Carlo Neri",     team: "Miami Spritz",          stat: "19 pts" },
  { match: "Minnesode Timbermilf vs Denver McNuggets",  date: "05 Ago", player: "Simone Neri",    team: "Minnesode Timbermilf",  stat: "15 pts · 12 ast" },
  { match: "Atlanta Robba vs Chicago Poolls",           date: "05 Ago", player: "Leo Verdi",      team: "Atlanta Robba",         stat: "24 pts" },
  { match: "Saluta Andonio Spurs vs Miami Spritz",      date: "07 Ago", player: "Luca Bianchi",   team: "Saluta Andonio Spurs",  stat: "18 pts · 10 reb" },
  { match: "Boston Lopez vs Golden State Worriers",     date: "07 Ago", player: "Filippo Conti",  team: "Boston Lopez",          stat: "17 pts" },
  { match: "Minnesode Timbermilf vs Atlanta Robba",     date: "07 Ago", player: "Leo Verdi",      team: "Atlanta Robba",         stat: "21 pts · game winner" },
  { match: "Denver McNuggets vs Chicago Poolls",        date: "07 Ago", player: "Davide Mori",    team: "Chicago Poolls",        stat: "16 pts" },
];

export const tournamentMvpSlug = "marco-rossi";

export const teams = [...new Set(allPlayers.map(p => p.team))];
