import { useState, useEffect, useMemo } from "react";
import { motion, AnimatePresence } from "motion/react";
import { Trophy, Flame, X, Swords, Calendar, ChevronRight } from "lucide-react";

type TeamScore = { name: string; score: number };
type Match = {
  id: string;
  round: string;
  date: string;       // es. "05 Ago 18:00" — viene dal backend
  status: "COMPLETA" | "LIVE" | "IN PROGRAMMA";
  team1: TeamScore;
  team2: TeamScore;
  details: { mvp: string; summary: string };
};

// Risposta da GET /api/squadre
type TeamApi = {
  id: number;
  nome: string;
  girone: "A" | "B";
};

// ── Tipi per il box score completo (da API /api/partite/{id}) ─────────────

type PlayerStats = {
  numero: number;
  nome: string;
  minuti: string;
  tdcR: number; tdcT: number;           // tiri dal campo
  p2R: number;  p2T: number;            // 2 punti
  p3R: number;  p3T: number;            // 3 punti
  tlR: number;  tlT: number;            // tiri liberi
  rebOff: number; rebDef: number; rebTot: number;
  ast: number;
  palPerse: number;
  palRecup: number;
  stoppate: number;
  falliF: number; falliS: number;
  plusMinus: number;
  valutazione: number;
  punti: number;
  isCapitano: boolean;
  isQuintetto: boolean;
};

type TeamBoxScore = {
  nome: string;
  allenatore?: string;
  giocatori: PlayerStats[];
};

type MatchDetail = {
  id: string;
  nGara: number;
  spettatori: number;
  durata: string;
  data: string;
  luogo: string;
  arbitri: string[];
  // Punteggi per quarto [Q1, Q2, ...] + totale
  quartiCasa:       number[];
  quartiTrasferta:  number[];
  totaleCasa:       number;
  totaleTrasferta:  number;
  squadraCasa:      TeamBoxScore;
  squadraTrasferta: TeamBoxScore;
  // Stats di riepilogo squadra
  statsCasa: {
    puntiDaPallePerse: number;
    puntiInArea: number; puntiInAreaR: number; puntiInAreaT: number;
    puntiDaSecondiTiri: number;
    puntiContropiede: number;
    fastBreakPoints: number;
    puntiPanchina: number;
    massimoVantaggio: number; massimoVantaggioScore: string;
    massimoParziale: number; massimoParzialePeriodo: string;
    pointsPerPossession: number;
    tempoInVantaggio: string;
  };
  statsTrasferta: {
    puntiDaPallePerse: number;
    puntiInArea: number; puntiInAreaR: number; puntiInAreaT: number;
    puntiDaSecondiTiri: number;
    puntiContropiede: number;
    fastBreakPoints: number;
    puntiPanchina: number;
    massimoVantaggio: number; massimoVantaggioScore: string;
    massimoParziale: number; massimoParzialePeriodo: string;
    pointsPerPossession: number;
    tempoInVantaggio: string;
  };
  cambiDiGuida: number;
  parita: number;
};

// ── Componente tabella box score ──────────────────────────────────────────

function BoxScoreTable({ team }: { team: TeamBoxScore }) {
  const pct = (r: number, t: number) => t === 0 ? "—" : `${Math.round(r * 100 / t)}%`;

  return (
    <div>
      <div className="flex items-center justify-between mb-2">
        <h5 className="font-display text-lg uppercase text-white tracking-wide">{team.nome}</h5>
        {team.allenatore && (
          <span className="font-sans text-xs text-zinc-500">All.: {team.allenatore}</span>
        )}
      </div>
      <div className="overflow-x-auto">
        <table className="w-full text-xs min-w-[800px]">
          <thead>
            <tr className="border-b-2 border-zinc-700 text-zinc-500 font-display uppercase tracking-widest">
              <th className="text-left py-2 pr-2 w-6">#</th>
              <th className="text-left py-2 pr-4 min-w-[130px]">Nome</th>
              <th className="text-center py-2 px-2">MIN</th>
              <th className="text-center py-2 px-2 text-brand-orange">PTI</th>
              <th className="text-center py-2 px-2">2PT</th>
              <th className="text-center py-2 px-2">3PT</th>
              <th className="text-center py-2 px-2">TL</th>
              <th className="text-center py-2 px-2">RO</th>
              <th className="text-center py-2 px-2">RD</th>
              <th className="text-center py-2 px-2">RT</th>
              <th className="text-center py-2 px-2">AS</th>
              <th className="text-center py-2 px-2">PP</th>
              <th className="text-center py-2 px-2">PR</th>
              <th className="text-center py-2 px-2">FF</th>
              <th className="text-center py-2 px-2">FS</th>
              <th className="text-center py-2 px-2">+/-</th>
              <th className="text-center py-2 px-2">VAL</th>
            </tr>
          </thead>
          <tbody>
            {team.giocatori.map((p) => (
              <tr key={p.numero} className="border-b border-zinc-800/40 hover:bg-zinc-800/20 transition-colors">
                <td className="py-2 pr-2 font-mono text-zinc-500">{p.numero}{p.isCapitano ? "C" : ""}{p.isQuintetto ? "*" : ""}</td>
                <td className="py-2 pr-4 font-sans font-bold text-zinc-300">{p.nome}</td>
                <td className="py-2 px-2 text-center font-mono text-zinc-500">{p.minuti}</td>
                <td className="py-2 px-2 text-center font-mono font-bold text-brand-orange">{p.punti}</td>
                <td className="py-2 px-2 text-center font-mono text-zinc-400">{p.p2R}/{p.p2T}<span className="text-zinc-600 text-[10px] ml-1">{pct(p.p2R, p.p2T)}</span></td>
                <td className="py-2 px-2 text-center font-mono text-zinc-400">{p.p3R}/{p.p3T}<span className="text-zinc-600 text-[10px] ml-1">{pct(p.p3R, p.p3T)}</span></td>
                <td className="py-2 px-2 text-center font-mono text-zinc-400">{p.tlR}/{p.tlT}<span className="text-zinc-600 text-[10px] ml-1">{pct(p.tlR, p.tlT)}</span></td>
                <td className="py-2 px-2 text-center font-mono text-zinc-500">{p.rebOff}</td>
                <td className="py-2 px-2 text-center font-mono text-zinc-500">{p.rebDef}</td>
                <td className="py-2 px-2 text-center font-mono text-zinc-400 font-bold">{p.rebTot}</td>
                <td className="py-2 px-2 text-center font-mono text-zinc-400">{p.ast}</td>
                <td className="py-2 px-2 text-center font-mono text-zinc-500">{p.palPerse}</td>
                <td className="py-2 px-2 text-center font-mono text-zinc-500">{p.palRecup}</td>
                <td className="py-2 px-2 text-center font-mono text-zinc-500">{p.falliF}</td>
                <td className="py-2 px-2 text-center font-mono text-zinc-500">{p.falliS}</td>
                <td className={`py-2 px-2 text-center font-mono font-bold ${p.plusMinus > 0 ? "text-green-500" : p.plusMinus < 0 ? "text-red-500" : "text-zinc-600"}`}>
                  {p.plusMinus > 0 ? `+${p.plusMinus}` : p.plusMinus}
                </td>
                <td className="py-2 px-2 text-center font-mono text-zinc-400">{p.valutazione}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

type Group = {
  key: string;
  label: string;
  accentClass: string;
  borderClass: string;
  bgClass: string;
  shadowClass: string;
  teams: string[];
  matches: Match[];
};

const defaultGroups: Group[] = [
  {
    key: "A",
    label: "Girone A",
    accentClass: "text-brand-blue",
    borderClass: "border-brand-blue",
    bgClass: "bg-brand-blue/10",
    shadowClass: "shadow-[6px_6px_0_var(--color-brand-blue)]",
    teams: ["Saluta Andonio Spurs", "Boston Lopez", "Miami Spritz", "Golden State Worriers"],
    matches: [
      { id: "ga1", round: "Girone A — G1", date: "05 Ago 18:00", status: "COMPLETA", team1: { name: "Saluta Andonio Spurs", score: 71 }, team2: { name: "Boston Lopez", score: 60 }, details: { mvp: "Marco Rossi (22 pts)", summary: "Partenza solida per gli Spurs, Boston rimane in gara fino al terzo quarto prima del break decisivo." } },
      { id: "ga2", round: "Girone A — G1", date: "05 Ago 19:30", status: "COMPLETA", team1: { name: "Miami Spritz", score: 78 }, team2: { name: "Golden State Worriers", score: 65 }, details: { mvp: "Carlo Neri (19 pts)", summary: "Miami prende subito il controllo, Worriers mai realmente in partita." } },
      { id: "ga3", round: "Girone A — G2", date: "07 Ago 18:00", status: "COMPLETA", team1: { name: "Saluta Andonio Spurs", score: 85 }, team2: { name: "Miami Spritz", score: 78 }, details: { mvp: "Luca Bianchi (18 pts, 10 reb)", summary: "Gli Spurs si confermano al top del girone con una prova di forza." } },
      { id: "ga4", round: "Girone A — G2", date: "07 Ago 20:00", status: "COMPLETA", team1: { name: "Boston Lopez", score: 70 }, team2: { name: "Golden State Worriers", score: 68 }, details: { mvp: "Filippo Conti (17 pts)", summary: "Boston vince di misura in un match tiratissimo deciso negli ultimi 30 secondi." } },
      { id: "ga5", round: "Girone A — G3", date: "10 Ago 18:00", status: "IN PROGRAMMA", team1: { name: "Saluta Andonio Spurs", score: 0 }, team2: { name: "Golden State Worriers", score: 0 }, details: { mvp: "TBD", summary: "Match in programma." } },
      { id: "ga6", round: "Girone A — G3", date: "10 Ago 19:30", status: "IN PROGRAMMA", team1: { name: "Boston Lopez", score: 0 }, team2: { name: "Miami Spritz", score: 0 }, details: { mvp: "TBD", summary: "Match in programma." } },
    ],
  },
  {
    key: "B",
    label: "Girone B",
    accentClass: "text-brand-orange",
    borderClass: "border-brand-orange",
    bgClass: "bg-brand-orange/10",
    shadowClass: "shadow-[6px_6px_0_var(--color-brand-orange)]",
    teams: ["Minnesode Timbermilf", "Denver McNuggets", "Atlanta Robba", "Chicago Poolls"],
    matches: [
      { id: "gb1", round: "Girone B — G1", date: "05 Ago 18:30", status: "COMPLETA", team1: { name: "Minnesode Timbermilf", score: 85 }, team2: { name: "Denver McNuggets", score: 55 }, details: { mvp: "Simone Neri (15 pts, 12 ast)", summary: "Dominio totale di Minnesode in transizione, McNuggets travolti dai contropiedi." } },
      { id: "gb2", round: "Girone B — G1", date: "05 Ago 20:00", status: "COMPLETA", team1: { name: "Atlanta Robba", score: 81 }, team2: { name: "Chicago Poolls", score: 60 }, details: { mvp: "Leo Verdi (24 pts)", summary: "Atlanta Robba parte forte, Chicago non riesce mai a rientrare nel match." } },
      { id: "gb3", round: "Girone B — G2", date: "07 Ago 18:30", status: "COMPLETA", team1: { name: "Minnesode Timbermilf", score: 78 }, team2: { name: "Atlanta Robba", score: 81 }, details: { mvp: "Leo Verdi (21 pts, game winner)", summary: "Sfida per il primo posto del girone. Verdi chiude i conti con un lay-up a 3 secondi dalla sirena." } },
      { id: "gb4", round: "Girone B — G2", date: "07 Ago 20:30", status: "COMPLETA", team1: { name: "Denver McNuggets", score: 65 }, team2: { name: "Chicago Poolls", score: 72 }, details: { mvp: "Davide Mori (16 pts)", summary: "Chicago si aggiudica uno scontro salvezza con una difesa solida nel finale." } },
      { id: "gb5", round: "Girone B — G3", date: "10 Ago 18:30", status: "IN PROGRAMMA", team1: { name: "Minnesode Timbermilf", score: 0 }, team2: { name: "Chicago Poolls", score: 0 }, details: { mvp: "TBD", summary: "Match in programma." } },
      { id: "gb6", round: "Girone B — G3", date: "10 Ago 20:00", status: "IN PROGRAMMA", team1: { name: "Denver McNuggets", score: 0 }, team2: { name: "Atlanta Robba", score: 0 }, details: { mvp: "TBD", summary: "Match in programma." } },
    ],
  },
];

const defaultBracket = {
  semis: [
    { id: "sf1", round: "Semifinale 1", date: "12 Ago 18:00", status: "IN PROGRAMMA" as const, team1: { name: "1° Girone A", score: 0 }, team2: { name: "2° Girone B", score: 0 }, details: { mvp: "TBD", summary: "Semifinale in programma." } },
    { id: "sf2", round: "Semifinale 2", date: "12 Ago 20:30", status: "IN PROGRAMMA" as const, team1: { name: "1° Girone B", score: 0 }, team2: { name: "2° Girone A", score: 0 }, details: { mvp: "TBD", summary: "Semifinale in programma." } },
  ],
  third: { id: "third", round: "Finale 3°-4° Posto", date: "14 Ago 18:30", status: "IN PROGRAMMA" as const, team1: { name: "TBD", score: 0 }, team2: { name: "TBD", score: 0 }, details: { mvp: "TBD", summary: "Finale per il terzo posto." } },
  final: { id: "final", round: "Finale", date: "14 Ago 21:00", status: "IN PROGRAMMA" as const, team1: { name: "TBD", score: 0 }, team2: { name: "TBD", score: 0 }, details: { mvp: "TBD", summary: "La grande finale." } },
};

// — Classifica —

function computeStandings(teams: string[], matches: Match[]) {
  const s: Record<string, { g: number; v: number; p: number; pt: number; pf: number; ps: number }> = {};
  teams.forEach(t => { s[t] = { g: 0, v: 0, p: 0, pt: 0, pf: 0, ps: 0 }; });

  for (const m of matches) {
    if (m.status !== "COMPLETA") continue;
    s[m.team1.name].g++;  s[m.team2.name].g++;
    s[m.team1.name].pf += m.team1.score; s[m.team1.name].ps += m.team2.score;
    s[m.team2.name].pf += m.team2.score; s[m.team2.name].ps += m.team1.score;
    if (m.team1.score > m.team2.score) {
      s[m.team1.name].v++;  s[m.team1.name].pt += 2;
      s[m.team2.name].p++;  s[m.team2.name].pt += 1;
    } else if (m.team2.score > m.team1.score) {
      s[m.team2.name].v++;  s[m.team2.name].pt += 2;
      s[m.team1.name].p++;  s[m.team1.name].pt += 1;
    }
    // punteggi identici ignorati (non deve succedere nel basket)
  }

  return teams
    .map(t => ({ name: t, ...s[t] }))
    .sort((a, b) => b.pt - a.pt || (b.pf - b.ps) - (a.pf - a.ps));
}

function StandingsTable({ group }: { group: Group }) {
  const rows = computeStandings(group.teams, group.matches);

  return (
    <table className="w-full text-sm">
      <thead>
        <tr className={`border-b-2 ${group.borderClass} border-opacity-30`}>
          <th className="text-left pb-3 pr-3 font-display text-xs uppercase tracking-widest text-zinc-500 w-8">#</th>
          <th className="text-left pb-3 font-display text-xs uppercase tracking-widest text-zinc-500">Squadra</th>
          <th className="text-center pb-3 px-3 font-display text-xs uppercase tracking-widest text-zinc-500">G</th>
          <th className="text-center pb-3 px-3 font-display text-xs uppercase tracking-widest text-zinc-500">V</th>
          <th className="text-center pb-3 px-3 font-display text-xs uppercase tracking-widest text-zinc-500">P</th>
          <th className="text-center pb-3 px-3 font-display text-xs uppercase tracking-widest text-brand-orange">PT</th>
          <th className="text-center pb-3 pl-3 font-display text-xs uppercase tracking-widest text-zinc-500">+/-</th>
        </tr>
      </thead>
      <tbody>
        {rows.map((row, i) => {
          const qualifies = i < 2;
          const diff = row.pf - row.ps;
          return (
            <tr
              key={row.name}
              className={`border-b border-zinc-800/50 last:border-0 ${qualifies ? "" : "opacity-60"}`}
            >
              <td className="py-3 pr-3">
                <div className={`w-6 h-6 flex items-center justify-center font-display text-xs font-bold
                  ${i === 0 ? `${group.bgClass} ${group.accentClass} border ${group.borderClass}` : "text-zinc-600"}`}>
                  {i + 1}
                </div>
              </td>
              <td className="py-3">
                <div className="flex items-center gap-2">
                  {qualifies && (
                    <span className={`w-1 h-4 ${group.borderClass} bg-current ${group.accentClass} opacity-60 shrink-0`} />
                  )}
                  <span className={`font-sans font-bold text-sm uppercase truncate ${i === 0 ? "text-white" : "text-zinc-300"}`}>
                    {row.name}
                  </span>
                  {qualifies && (
                    <span className={`font-display text-[9px] uppercase tracking-widest ${group.accentClass} border ${group.borderClass} px-1 shrink-0 opacity-70`}>
                      Q
                    </span>
                  )}
                </div>
              </td>
              <td className="py-3 text-center font-mono text-xs text-zinc-400 px-3">{row.g}</td>
              <td className="py-3 text-center font-mono text-xs text-zinc-400 px-3">{row.v}</td>
              <td className="py-3 text-center font-mono text-xs text-zinc-400 px-3">{row.p}</td>
              <td className={`py-3 text-center font-mono font-bold text-sm px-3 ${i === 0 ? group.accentClass : "text-brand-orange/70"}`}>{row.pt}</td>
              <td className={`py-3 text-center font-mono text-xs pl-3 font-bold ${row.g === 0 ? "text-zinc-600" : diff > 0 ? "text-green-500" : diff < 0 ? "text-red-500" : "text-zinc-400"}`}>
                {row.g === 0 ? "—" : `${diff > 0 ? "+" : ""}${diff}`}
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}

// — Match row orizzontale (girone) —

function GroupMatchRow({ match, onClick }: { match: Match; onClick: () => void }) {
  const isLive = match.status === "LIVE";
  const isPending = match.status === "IN PROGRAMMA";
  const t1Wins = !isPending && match.team1.score > match.team2.score;
  const t2Wins = !isPending && match.team2.score > match.team1.score;

  return (
    <div
      onClick={onClick}
      className={`flex items-center gap-3 px-4 py-3 border-b border-zinc-800/60 last:border-0 hover:bg-zinc-800/30 cursor-pointer transition-colors group
        ${isLive ? "bg-brand-orange/5" : ""}`}
    >
      {/* Data */}
      <span className="font-mono text-[10px] text-zinc-600 w-20 shrink-0">{match.date}</span>

      {/* Team 1 */}
      <span className={`font-sans font-bold text-xs uppercase flex-1 text-right truncate transition-colors
        ${t1Wins ? "text-white" : isPending ? "text-zinc-400" : "text-zinc-500"}`}>
        {match.team1.name}
      </span>

      {/* Score */}
      <div className={`flex items-center gap-1 shrink-0 px-3 py-1 border
        ${isLive ? "border-brand-orange bg-brand-orange/10" : "border-zinc-800 bg-zinc-950"}`}>
        <span className={`font-mono text-base font-bold w-7 text-right ${t1Wins ? "text-brand-orange" : isPending ? "text-zinc-600" : "text-zinc-500"}`}>
          {isPending ? "—" : match.team1.score}
        </span>
        <span className="text-zinc-700 text-xs mx-0.5">:</span>
        <span className={`font-mono text-base font-bold w-7 ${t2Wins ? "text-brand-orange" : isPending ? "text-zinc-600" : "text-zinc-500"}`}>
          {isPending ? "—" : match.team2.score}
        </span>
      </div>

      {/* Team 2 */}
      <span className={`font-sans font-bold text-xs uppercase flex-1 truncate transition-colors
        ${t2Wins ? "text-white" : isPending ? "text-zinc-400" : "text-zinc-500"}`}>
        {match.team2.name}
      </span>

      {/* Status */}
      <div className="w-16 shrink-0 text-right">
        {isLive && <span className="font-display text-[10px] uppercase text-brand-orange animate-pulse">● LIVE</span>}
        {match.status === "COMPLETA" && <span className="font-display text-[10px] uppercase text-zinc-600">✓</span>}
        {isPending && <ChevronRight className="w-3 h-3 text-zinc-700 inline opacity-0 group-hover:opacity-100 transition-opacity" />}
      </div>
    </div>
  );
}

// — Match card (playoff) —

function PlayoffCard({ match, isFinal = false, onClick }: { match: Match; isFinal?: boolean; onClick?: () => void }) {
  const isLive = match.status === "LIVE";
  const isPending = match.status === "IN PROGRAMMA";
  const borderColor = isFinal ? "border-brand-yellow" : isLive ? "border-brand-orange" : isPending ? "border-zinc-700" : "border-zinc-800";

  return (
    <div
      onClick={onClick}
      className={`border-[3px] ${borderColor} bg-zinc-950 cursor-pointer hover:border-white transition-all group relative
        shadow-[4px_4px_0_rgba(0,0,0,0.4)] hover:-translate-y-0.5 flex flex-col`}
    >
      {isLive && (
        <div className="absolute -top-3 -right-3 bg-brand-orange text-brand-bg font-display px-2 py-0.5 text-xs uppercase rotate-3 z-10 animate-pulse">
          LIVE
        </div>
      )}
      <div className="flex justify-between items-center px-4 py-2 border-b border-zinc-800 bg-zinc-900">
        <span className="text-xs font-display text-zinc-400 uppercase tracking-widest">{match.round}</span>
        <span className="text-xs font-sans text-zinc-500">{match.date}</span>
      </div>
      <div className="p-4 flex flex-col gap-3">
        <div className={`flex justify-between items-center ${!isPending && match.team1.score > match.team2.score ? "text-white" : "text-zinc-500"}`}>
          <span className="font-sans font-[900] tracking-tight text-sm uppercase truncate max-w-[150px]">{match.team1.name}</span>
          <span className="font-mono text-2xl font-bold">{isPending ? "—" : match.team1.score}</span>
        </div>
        <div className={`flex justify-between items-center ${!isPending && match.team2.score > match.team1.score ? "text-white" : "text-zinc-500"}`}>
          <span className="font-sans font-[900] tracking-tight text-sm uppercase truncate max-w-[150px]">{match.team2.name}</span>
          <span className="font-mono text-2xl font-bold">{isPending ? "—" : match.team2.score}</span>
        </div>
      </div>
    </div>
  );
}

// — Pagina —

const API = import.meta.env.VITE_API_URL ?? "";

export function Matches() {
  const [groups,        setGroups]        = useState<Group[]>(defaultGroups);
  const [bracketMatches, setBracket]      = useState(defaultBracket);
  const [selectedMatch, setSelectedMatch] = useState<Match | null>(null);
  const [matchDetail,   setMatchDetail]   = useState<MatchDetail | null>(null);
  const [loadingDetail, setLoadingDetail] = useState(false);

  // Fetch squadre + partite in parallelo dal backend
  useEffect(() => {
    Promise.all([
      fetch(`${API}/api/squadre`).then(r => r.ok ? r.json() as Promise<TeamApi[]> : null),
      fetch(`${API}/api/partite`).then(r => r.ok ? r.json() as Promise<Match[]>   : null),
    ])
    .then(([squadre, partite]) => {
      if (!squadre && !partite) return; // backend non disponibile

      setGroups(defaultGroups.map(g => ({
        ...g,
        // Nomi squadre dal backend, fallback a quelli statici
        teams: squadre
          ? squadre.filter(s => s.girone === g.key).map(s => s.nome)
          : g.teams,
        // Partite del girone dal backend (con date e punteggi reali)
        matches: partite
          ? partite.filter(m => m.round.includes(`Girone ${g.key}`))
          : g.matches,
      })));

      // Aggiorna bracket con partite playoff reali
      if (partite) {
        const bracket = partite.filter(m =>
          m.round.includes("Semifinale") || m.round.includes("Finale")
        );
        if (bracket.length > 0) {
          setBracket({
            semis: bracket.filter(m => m.round.includes("Semifinale")) as typeof defaultBracket.semis,
            third: (bracket.find(m => m.round.includes("3°")) ?? defaultBracket.third) as typeof defaultBracket.third,
            final: (bracket.find(m => m.round === "Finale") ?? defaultBracket.final) as typeof defaultBracket.final,
          });
        }
      }
    })
    .catch(() => {}); // errore di rete → restano i dati statici
  }, []);

  const handleSelectMatch = async (match: Match) => {
    setSelectedMatch(match);
    setMatchDetail(null);
    if (match.status !== "COMPLETA") return;
    setLoadingDetail(true);
    try {
      const res = await fetch(`${API}/api/partite/${match.id}`);
      if (res.ok) setMatchDetail(await res.json());
    } catch {
      // backend non ancora disponibile — mostra il summary base
    } finally {
      setLoadingDetail(false);
    }
  };

  // Bracket con semis/terzo posto auto-populate dai risultati reali
  const displayBracket = useMemo(() => {
    // Sostituisce solo nomi placeholder — se l'API ha già i nomi reali li mantiene
    const resolve = (name: string, real: string) =>
      (name.startsWith("1°") || name.startsWith("2°") || name === "TBD") ? real : name;

    let result = { ...bracketMatches, semis: [...bracketMatches.semis] };

    // 1. Quando il girone è completo → popola i nomi nelle semis dai qualificati
    const allGroupComplete = groups.every(g =>
      g.matches.length > 0 && g.matches.every(m => m.status === "COMPLETA")
    );
    if (allGroupComplete) {
      const qualifiers: Record<string, { first: string; second: string }> = {};
      for (const g of groups) {
        const standings = computeStandings(g.teams, g.matches);
        qualifiers[g.key] = {
          first:  standings[0]?.name ?? `1° Girone ${g.key}`,
          second: standings[1]?.name ?? `2° Girone ${g.key}`,
        };
      }
      const gA = qualifiers["A"];
      const gB = qualifiers["B"];
      if (gA && gB) {
        result.semis = [
          { ...result.semis[0],
            team1: { ...result.semis[0].team1, name: resolve(result.semis[0].team1.name, gA.first) },
            team2: { ...result.semis[0].team2, name: resolve(result.semis[0].team2.name, gB.second) },
          },
          { ...result.semis[1],
            team1: { ...result.semis[1].team1, name: resolve(result.semis[1].team1.name, gB.first) },
            team2: { ...result.semis[1].team2, name: resolve(result.semis[1].team2.name, gA.second) },
          },
        ];
      }
    }

    // 2. Quando le semis sono complete → popola la finale 3°-4° con i perdenti
    const semisComplete = bracketMatches.semis.every(s => s.status === "COMPLETA");
    if (semisComplete) {
      const loser = (s: typeof bracketMatches.semis[0]) =>
        s.team1.score < s.team2.score ? s.team1.name : s.team2.name;
      result.third = {
        ...result.third,
        team1: { ...result.third.team1, name: resolve(result.third.team1.name, loser(bracketMatches.semis[0])) },
        team2: { ...result.third.team2, name: resolve(result.third.team2.name, loser(bracketMatches.semis[1])) },
      };
    }

    return result;
  }, [groups, bracketMatches]);

  // Tutte le partite piattate e raggruppate per data
  const allMatches = [
    ...groups.flatMap(g => g.matches),
    ...displayBracket.semis,
    displayBracket.third,
    displayBracket.final,
  ];

  const MESI: Record<string, number> = { Gen:1,Feb:2,Mar:3,Apr:4,Mag:5,Giu:6,Lug:7,Ago:8,Set:9,Ott:10,Nov:11,Dic:12 };
  const parseDayKey = (day: string) => {
    const [d, m] = day.split(" ");
    return (MESI[m] ?? 0) * 100 + parseInt(d, 10);
  };

  const calendarByDay = allMatches.reduce<Record<string, Match[]>>((acc, m) => {
    const parts = m.date.trim().split(/\s+/);
    const day = parts.slice(0, 2).join(" ");
    if (!acc[day]) acc[day] = [];
    acc[day].push(m);
    return acc;
  }, {});

  // Ordina i giorni cronologicamente
  const calendarEntries = Object.entries(calendarByDay)
    .sort(([a], [b]) => parseDayKey(a) - parseDayKey(b));

  return (
    <div className="pt-32 pb-20 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}>

        {/* Header */}
        <div className="text-center mb-16">
          <h1 className="font-display text-[80px] md:text-[120px] text-brand-orange uppercase leading-[0.8] mb-8 tracking-[-4px]">
            Match
          </h1>
          <p className="text-xl font-sans text-zinc-400 max-w-2xl mx-auto">
            Dal girone all'italiana fino alla pazzesca finale dei playoff. Ripercorri ogni canestro.
          </p>
        </div>

        {/* — CALENDARIO — */}
        <div className="mb-24">
          <h2 className="font-display text-4xl md:text-5xl uppercase flex items-center gap-4 border-t-[6px] border-zinc-800 pt-6 pb-10 text-white">
            <Calendar className="w-10 h-10 text-brand-orange" /> Calendario
          </h2>

          <div className="space-y-6">
            {calendarEntries.map(([day, dayMatches]) => (
              <div key={day} className="border-[3px] border-zinc-800 bg-zinc-900 overflow-hidden">

                {/* Header giorno */}
                <div className="px-5 py-3 bg-zinc-950 border-b-2 border-zinc-800 flex items-center justify-between">
                  <span className="font-display text-xl uppercase tracking-widest text-white">{day}</span>
                  <span className="font-display text-[10px] uppercase tracking-widest text-zinc-600">
                    {dayMatches.filter(m => m.status === "COMPLETA").length}/{dayMatches.length} giocate
                  </span>
                </div>

                {/* Partite del giorno */}
                {dayMatches.map(match => (
                  <div
                    key={match.id}
                    onClick={() => handleSelectMatch(match)}
                    className={`flex items-center gap-3 px-4 py-3 border-b border-zinc-800/60 last:border-0
                      hover:bg-zinc-800/30 cursor-pointer transition-colors group
                      ${match.status === "LIVE" ? "bg-brand-orange/5" : ""}`}
                  >
                    {/* Orario */}
                    <div className="w-24 shrink-0">
                      <span className="font-mono text-[10px] text-zinc-600 block">
                        {match.date.split(" ").slice(2).join(" ")}
                      </span>
                      <span className="font-display text-[9px] uppercase tracking-widest text-zinc-700">
                        {match.round}
                      </span>
                    </div>

                    {/* Team 1 */}
                    <span className={`font-sans font-bold text-xs uppercase flex-1 text-right truncate
                      ${match.status !== "IN PROGRAMMA" && match.team1.score > match.team2.score ? "text-white" : "text-zinc-400"}`}>
                      {match.team1.name}
                    </span>

                    {/* Score */}
                    <div className={`flex items-center gap-1 shrink-0 px-3 py-1 border
                      ${match.status === "LIVE" ? "border-brand-orange bg-brand-orange/10" : "border-zinc-800 bg-zinc-950"}`}>
                      <span className={`font-mono text-base font-bold w-7 text-right
                        ${match.status !== "IN PROGRAMMA" && match.team1.score > match.team2.score ? "text-brand-orange" : "text-zinc-600"}`}>
                        {match.status === "IN PROGRAMMA" ? "—" : match.team1.score}
                      </span>
                      <span className="text-zinc-700 text-xs mx-0.5">:</span>
                      <span className={`font-mono text-base font-bold w-7
                        ${match.status !== "IN PROGRAMMA" && match.team2.score > match.team1.score ? "text-brand-orange" : "text-zinc-600"}`}>
                        {match.status === "IN PROGRAMMA" ? "—" : match.team2.score}
                      </span>
                    </div>

                    {/* Team 2 */}
                    <span className={`font-sans font-bold text-xs uppercase flex-1 truncate
                      ${match.status !== "IN PROGRAMMA" && match.team2.score > match.team1.score ? "text-white" : "text-zinc-400"}`}>
                      {match.team2.name}
                    </span>

                    {/* Status badge */}
                    <div className="w-16 shrink-0 text-right">
                      {match.status === "LIVE" && (
                        <span className="font-display text-[10px] uppercase text-brand-orange animate-pulse">● LIVE</span>
                      )}
                      {match.status === "COMPLETA" && (
                        <span className="font-display text-[10px] uppercase text-zinc-600">✓</span>
                      )}
                      {match.status === "IN PROGRAMMA" && (
                        <ChevronRight className="w-3 h-3 text-zinc-700 inline opacity-0 group-hover:opacity-100 transition-opacity" />
                      )}
                    </div>
                  </div>
                ))}
              </div>
            ))}
          </div>
        </div>

        {/* — FASE A GIRONI — */}
        <div className="mb-24">
          <h2 className="font-display text-4xl md:text-5xl uppercase flex items-center gap-4 border-t-[6px] border-zinc-800 pt-6 pb-10 text-white">
            <Calendar className="w-10 h-10 text-brand-blue" /> Fase a Gironi
          </h2>

          {/* Classifiche */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
            {groups.map((group) => (
              <div key={group.key} className={`border-[3px] ${group.borderClass} bg-zinc-900 ${group.shadowClass} overflow-hidden`}>
                <div className={`${group.bgClass} px-5 py-3 border-b-2 ${group.borderClass} border-opacity-40 flex items-center justify-between`}>
                  <h3 className={`font-display text-xl uppercase tracking-widest ${group.accentClass}`}>{group.label}</h3>
                  <span className="font-display text-[10px] uppercase tracking-[0.2em] text-zinc-500">Classifica</span>
                </div>
                <div className="px-5 py-4">
                  <StandingsTable group={group} />
                </div>
                <div className={`px-5 py-2 border-t border-zinc-800 flex items-center gap-1.5`}>
                  <span className={`w-1 h-3 ${group.accentClass} bg-current`} />
                  <span className="font-display text-[10px] uppercase tracking-widest text-zinc-500">Le prime 2 accedono ai playoff</span>
                </div>
              </div>
            ))}
          </div>

          {/* Partite */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {groups.map((group) => (
              <div key={group.key} className="border-[3px] border-zinc-800 bg-zinc-900 overflow-hidden">
                <div className="px-4 py-3 border-b-2 border-zinc-800 bg-zinc-950 flex items-center justify-between">
                  <h3 className={`font-display text-sm uppercase tracking-widest ${group.accentClass}`}>{group.label} — Partite</h3>
                  <span className="font-display text-[10px] uppercase tracking-widest text-zinc-600">
                    {group.matches.filter(m => m.status === "COMPLETA").length}/{group.matches.length} giocate
                  </span>
                </div>
                {group.matches.map((match) => (
                  <GroupMatchRow key={match.id} match={match} onClick={() => handleSelectMatch(match)} />
                ))}
              </div>
            ))}
          </div>
        </div>

        {/* — PLAYOFF — */}
        <div>
          <h2 className="font-display text-4xl md:text-5xl uppercase flex items-center gap-4 border-t-[6px] border-zinc-800 pt-6 pb-10 text-white">
            <Swords className="w-10 h-10 text-brand-orange" /> Playoff Bracket
          </h2>

          <div className="overflow-x-auto pb-6 cursor-grab active:cursor-grabbing">
            <div className="min-w-[900px] flex bg-zinc-900/40 p-8 md:p-12 border-[4px] border-zinc-800 shadow-inner">

              <div className="flex flex-col justify-around w-1/3 pr-8 gap-16 relative z-10">
                {displayBracket.semis.map((match) => (
                  <div key={match.id} className="relative">
                    <PlayoffCard match={match} onClick={() => handleSelectMatch(match)} />
                    <div className="absolute top-1/2 -right-8 w-8 h-[3px] bg-zinc-700" />
                  </div>
                ))}
              </div>

              <div className="w-0 relative z-0">
                <div className="absolute top-[25%] bottom-[25%] left-0 w-[3px] bg-zinc-700" />
                <div className="absolute top-1/2 left-0 w-8 h-[3px] bg-brand-orange" />
              </div>

              <div className="flex flex-col justify-center w-1/3 px-8 relative z-10">
                <div className="relative">
                  <PlayoffCard match={displayBracket.final} isFinal onClick={() => handleSelectMatch(displayBracket.final)} />
                  <div className="absolute top-1/2 -right-8 w-8 h-[3px] bg-brand-yellow/50" />
                </div>
              </div>

              <div className="flex flex-col justify-center w-1/3 pl-8 z-10">
                {(() => {
                  const finale = displayBracket.final;
                  const campione = finale.status === "COMPLETA"
                    ? (finale.team1.score > finale.team2.score ? finale.team1.name : finale.team2.name)
                    : null;
                  return (
                    <div className={`border-[4px] bg-zinc-950 p-6 text-center rotate-2 ${campione ? "border-brand-yellow shadow-[12px_12px_0_var(--color-brand-yellow)]" : "border-brand-yellow/40 shadow-[12px_12px_0_rgba(0,0,0,0.4)]"}`}>
                      <Trophy className={`w-16 h-16 mx-auto mb-4 ${campione ? "text-brand-yellow" : "text-brand-yellow/30"}`} />
                      <span className="font-display text-2xl text-zinc-400 uppercase tracking-widest block">Campione</span>
                      <span className={`font-display text-2xl uppercase tracking-widest block mt-2 leading-tight ${campione ? "text-brand-yellow" : "text-white opacity-20"}`}>
                        {campione ?? "???"}
                      </span>
                    </div>
                  );
                })()}
              </div>

            </div>
          </div>

          {/* Finale 3°-4° Posto */}
          <div className="mt-6 border-[3px] border-zinc-700 bg-zinc-900 overflow-hidden">
            <div className="px-5 py-3 bg-zinc-950 border-b-2 border-zinc-700 flex items-center justify-between">
              <span className="font-display text-sm uppercase tracking-widest text-zinc-400">Finale 3°-4° Posto</span>
              <span className="font-display text-[10px] uppercase tracking-widest text-zinc-600">{displayBracket.third.date}</span>
            </div>
            <div
              onClick={() => handleSelectMatch(displayBracket.third)}
              className="flex items-center gap-4 px-6 py-4 cursor-pointer hover:bg-zinc-800/30 transition-colors"
            >
              <span className={`font-sans font-bold text-sm uppercase flex-1 text-right truncate
                ${displayBracket.third.status === "COMPLETA" && displayBracket.third.team1.score > displayBracket.third.team2.score ? "text-white" : "text-zinc-400"}`}>
                {displayBracket.third.team1.name}
              </span>
              <div className="flex items-center gap-2 shrink-0">
                {displayBracket.third.status === "COMPLETA" ? (
                  <>
                    <span className="font-display text-2xl text-white w-10 text-right">{displayBracket.third.team1.score}</span>
                    <span className="font-display text-zinc-600 text-lg">:</span>
                    <span className="font-display text-2xl text-white w-10 text-left">{displayBracket.third.team2.score}</span>
                  </>
                ) : (
                  <span className="font-display text-zinc-600 text-base px-2">VS</span>
                )}
              </div>
              <span className={`font-sans font-bold text-sm uppercase flex-1 truncate
                ${displayBracket.third.status === "COMPLETA" && displayBracket.third.team2.score > displayBracket.third.team1.score ? "text-white" : "text-zinc-400"}`}>
                {displayBracket.third.team2.name}
              </span>
              {displayBracket.third.status === "COMPLETA" && (
                <span className="font-display text-[9px] uppercase tracking-widest text-zinc-600 shrink-0">Tabellino →</span>
              )}
            </div>
          </div>
        </div>

      </motion.div>

      {/* — MODAL — */}
      <AnimatePresence>
        {selectedMatch && (
          <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
            <motion.div
              initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
              onClick={() => setSelectedMatch(null)}
              className="absolute inset-0 bg-black/90 backdrop-blur-sm cursor-pointer"
            />
            <motion.div
              initial={{ opacity: 0, scale: 0.95, y: 20 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.95, y: 20 }}
              className="relative w-full max-w-5xl bg-zinc-950 border-[4px] border-brand-orange shadow-[16px_16px_0_var(--color-brand-blue)] z-10 flex flex-col max-h-[90vh]"
            >
              <div className="flex justify-between items-center p-6 border-b-[3px] border-zinc-800 bg-zinc-900">
                <h3 className="font-display text-2xl uppercase text-white tracking-wide">{selectedMatch.round}</h3>
                <button onClick={() => setSelectedMatch(null)} className="text-zinc-400 hover:text-white border-2 border-transparent hover:border-brand-orange p-1 transition-colors">
                  <X size={28} />
                </button>
              </div>

              <div className="p-6 overflow-y-auto">
                {selectedMatch.status === "IN PROGRAMMA" ? (
                  <div className="text-center py-10">
                    <p className="font-display text-3xl uppercase text-zinc-500 mb-2">Match in programma</p>
                    <p className="font-sans text-zinc-600">{selectedMatch.date}</p>
                  </div>
                ) : (
                  <>
                    {/* Punteggio finale */}
                    <div className="flex justify-center items-center gap-4 mb-6">
                      <div className="text-center flex-1">
                        <div className="font-sans font-black text-base md:text-xl uppercase mb-2 text-zinc-300 leading-tight">{selectedMatch.team1.name}</div>
                        <div className={`font-mono text-6xl md:text-8xl font-bold ${selectedMatch.team1.score >= selectedMatch.team2.score ? "text-brand-orange" : "text-zinc-600"}`}>
                          {selectedMatch.team1.score}
                        </div>
                      </div>
                      <div className="font-display text-3xl text-zinc-800">VS</div>
                      <div className="text-center flex-1">
                        <div className="font-sans font-black text-base md:text-xl uppercase mb-2 text-zinc-300 leading-tight">{selectedMatch.team2.name}</div>
                        <div className={`font-mono text-6xl md:text-8xl font-bold ${selectedMatch.team2.score >= selectedMatch.team1.score ? "text-white" : "text-zinc-600"}`}>
                          {selectedMatch.team2.score}
                        </div>
                      </div>
                    </div>

                    {/* Loading detail */}
                    {loadingDetail && (
                      <p className="font-display text-zinc-500 uppercase tracking-widest text-sm text-center animate-pulse py-4">
                        Caricamento tabellino...
                      </p>
                    )}

                    {/* Box score completo (da API) */}
                    {matchDetail && !loadingDetail && (
                      <>
                        {/* Info partita */}
                        <div className="grid grid-cols-3 gap-2 mb-6 text-center">
                          <div className="bg-zinc-900 border border-zinc-800 p-3">
                            <p className="font-display text-[10px] uppercase tracking-widest text-zinc-500 mb-1">Gara</p>
                            <p className="font-mono text-white font-bold">#{matchDetail.nGara}</p>
                          </div>
                          <div className="bg-zinc-900 border border-zinc-800 p-3">
                            <p className="font-display text-[10px] uppercase tracking-widest text-zinc-500 mb-1">Spettatori</p>
                            <p className="font-mono text-white font-bold">{matchDetail.spettatori}</p>
                          </div>
                          <div className="bg-zinc-900 border border-zinc-800 p-3">
                            <p className="font-display text-[10px] uppercase tracking-widest text-zinc-500 mb-1">Durata</p>
                            <p className="font-mono text-white font-bold">{matchDetail.durata}</p>
                          </div>
                        </div>

                        {/* Punteggi per quarto */}
                        <div className="mb-6 overflow-x-auto">
                          <table className="w-full text-xs text-center border border-zinc-800">
                            <thead>
                              <tr className="bg-zinc-900 border-b border-zinc-700">
                                <th className="text-left px-3 py-2 font-display uppercase tracking-widest text-zinc-500 w-32">Squadra</th>
                                {matchDetail.quartiCasa.map((_, i) => (
                                  <th key={i} className="px-3 py-2 font-display uppercase tracking-widest text-zinc-500">Q{i+1}</th>
                                ))}
                                <th className="px-3 py-2 font-display uppercase tracking-widest text-brand-orange">TOT</th>
                              </tr>
                            </thead>
                            <tbody>
                              <tr className="border-b border-zinc-800">
                                <td className="text-left px-3 py-2 font-sans font-bold text-zinc-300 truncate">{matchDetail.squadraCasa.nome}</td>
                                {matchDetail.quartiCasa.map((q, i) => <td key={i} className="px-3 py-2 font-mono text-zinc-400">{q}</td>)}
                                <td className="px-3 py-2 font-mono font-bold text-brand-orange">{matchDetail.totaleCasa}</td>
                              </tr>
                              <tr>
                                <td className="text-left px-3 py-2 font-sans font-bold text-zinc-300 truncate">{matchDetail.squadraTrasferta.nome}</td>
                                {matchDetail.quartiTrasferta.map((q, i) => <td key={i} className="px-3 py-2 font-mono text-zinc-400">{q}</td>)}
                                <td className="px-3 py-2 font-mono font-bold text-white">{matchDetail.totaleTrasferta}</td>
                              </tr>
                            </tbody>
                          </table>
                        </div>

                        {/* Tabella giocatori */}
                        <div className="space-y-6 mb-6">
                          <BoxScoreTable team={matchDetail.squadraCasa} />
                          <BoxScoreTable team={matchDetail.squadraTrasferta} />
                        </div>

                        {/* Stats di squadra a confronto */}
                        <div className="border border-zinc-800 overflow-hidden mb-6">
                          <div className="bg-zinc-900 px-4 py-2 border-b border-zinc-800">
                            <h5 className="font-display text-sm uppercase tracking-widest text-zinc-400">Stats di Squadra</h5>
                          </div>
                          {[
                            ["Punti da Palle Perse", matchDetail.statsCasa.puntiDaPallePerse, matchDetail.statsTrasferta.puntiDaPallePerse],
                            ["Punti in Area", matchDetail.statsCasa.puntiInArea, matchDetail.statsTrasferta.puntiInArea],
                            ["Punti Contropiede", matchDetail.statsCasa.puntiContropiede, matchDetail.statsTrasferta.puntiContropiede],
                            ["Punti Panchina", matchDetail.statsCasa.puntiPanchina, matchDetail.statsTrasferta.puntiPanchina],
                            ["Points per Possession", matchDetail.statsCasa.pointsPerPossession, matchDetail.statsTrasferta.pointsPerPossession],
                            ["Tempo in Vantaggio", matchDetail.statsCasa.tempoInVantaggio, matchDetail.statsTrasferta.tempoInVantaggio],
                          ].map(([label, casa, trasf]) => (
                            <div key={String(label)} className="flex items-center border-b border-zinc-800/50 last:border-0 px-4 py-2">
                              <span className="font-mono text-sm font-bold text-brand-orange w-16 text-center">{casa}</span>
                              <span className="flex-1 text-center font-display text-[10px] uppercase tracking-widest text-zinc-500">{label}</span>
                              <span className="font-mono text-sm font-bold text-white w-16 text-center">{trasf}</span>
                            </div>
                          ))}
                        </div>

                        {/* Arbitri */}
                        <p className="font-sans text-xs text-zinc-600 text-center">
                          Arbitri: {matchDetail.arbitri.join(" · ")}
                        </p>
                      </>
                    )}

                    {/* Fallback se backend non disponibile */}
                    {!matchDetail && !loadingDetail && (
                      <>
                        <div className="bg-zinc-900 border-[3px] border-zinc-700 p-6 mb-4">
                          <h4 className="font-display uppercase text-brand-yellow mb-3 text-xl">Match Recap</h4>
                          <p className="font-sans text-zinc-300 text-base leading-relaxed">{selectedMatch.details.summary}</p>
                        </div>
                        <div className="bg-brand-blue/10 border-[3px] border-brand-blue p-5 flex gap-5 items-center">
                          <div className="bg-brand-blue p-3 shrink-0 hidden sm:block">
                            <Flame className="w-8 h-8 text-brand-bg" />
                          </div>
                          <div>
                            <h4 className="font-display uppercase text-brand-blue mb-1 text-xl">MVP / Key Player</h4>
                            <p className="font-sans text-white text-lg font-bold">{selectedMatch.details.mvp}</p>
                          </div>
                        </div>
                      </>
                    )}
                  </>
                )}
              </div>
            </motion.div>
          </div>
        )}
      </AnimatePresence>
    </div>
  );
}
