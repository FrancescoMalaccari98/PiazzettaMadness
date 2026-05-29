import { useState } from "react";
import { motion, AnimatePresence } from "motion/react";
import { Trophy, Flame, X, Swords, Calendar, ChevronRight } from "lucide-react";

type TeamScore = { name: string; score: number };
type Match = {
  id: string;
  round: string;
  date: string;
  status: "COMPLETA" | "LIVE" | "IN PROGRAMMA";
  team1: TeamScore;
  team2: TeamScore;
  details: { mvp: string; summary: string };
};

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

const groups: Group[] = [
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

const bracketMatches = {
  semis: [
    { id: "sf1", round: "Semifinale 1", date: "12 Ago 18:00", status: "IN PROGRAMMA" as const, team1: { name: "1° Girone A", score: 0 }, team2: { name: "2° Girone B", score: 0 }, details: { mvp: "TBD", summary: "Semifinale in programma." } },
    { id: "sf2", round: "Semifinale 2", date: "12 Ago 20:30", status: "IN PROGRAMMA" as const, team1: { name: "1° Girone B", score: 0 }, team2: { name: "2° Girone A", score: 0 }, details: { mvp: "TBD", summary: "Semifinale in programma." } },
  ],
  final: { id: "final", round: "Finale", date: "14 Ago 21:00", status: "IN PROGRAMMA" as const, team1: { name: "TBD", score: 0 }, team2: { name: "TBD", score: 0 }, details: { mvp: "TBD", summary: "La grande finale." } },
};

// — Classifica —

function computeStandings(teams: string[], matches: Match[]) {
  const s: Record<string, { g: number; v: number; p: number; pf: number; ps: number }> = {};
  teams.forEach(t => { s[t] = { g: 0, v: 0, p: 0, pf: 0, ps: 0 }; });

  for (const m of matches) {
    if (m.status !== "COMPLETA") continue;
    s[m.team1.name].g++;  s[m.team2.name].g++;
    s[m.team1.name].pf += m.team1.score; s[m.team1.name].ps += m.team2.score;
    s[m.team2.name].pf += m.team2.score; s[m.team2.name].ps += m.team1.score;
    if (m.team1.score > m.team2.score) { s[m.team1.name].v++; s[m.team2.name].p++; }
    else { s[m.team2.name].v++; s[m.team1.name].p++; }
  }

  return teams
    .map(t => ({ name: t, ...s[t] }))
    .sort((a, b) => b.v - a.v || (b.pf - b.ps) - (a.pf - a.ps));
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
              <td className={`py-3 text-center font-mono font-bold text-sm px-3 ${i === 0 ? group.accentClass : "text-zinc-300"}`}>{row.v}</td>
              <td className="py-3 text-center font-mono text-xs text-zinc-400 px-3">{row.p}</td>
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
        <div className={`flex justify-between items-center ${!isPending && match.team1.score >= match.team2.score ? "text-white" : "text-zinc-500"}`}>
          <span className="font-sans font-[900] tracking-tight text-sm uppercase truncate max-w-[150px]">{match.team1.name}</span>
          <span className="font-mono text-2xl font-bold">{isPending ? "—" : match.team1.score}</span>
        </div>
        <div className={`flex justify-between items-center ${!isPending && match.team2.score >= match.team1.score ? "text-white" : "text-zinc-500"}`}>
          <span className="font-sans font-[900] tracking-tight text-sm uppercase truncate max-w-[150px]">{match.team2.name}</span>
          <span className="font-mono text-2xl font-bold">{isPending ? "—" : match.team2.score}</span>
        </div>
      </div>
    </div>
  );
}

// — Pagina —

export function Matches() {
  const [selectedMatch, setSelectedMatch] = useState<Match | null>(null);

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
                  <GroupMatchRow key={match.id} match={match} onClick={() => setSelectedMatch(match)} />
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
                {bracketMatches.semis.map((match) => (
                  <div key={match.id} className="relative">
                    <PlayoffCard match={match} onClick={() => setSelectedMatch(match)} />
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
                  <PlayoffCard match={bracketMatches.final} isFinal onClick={() => setSelectedMatch(bracketMatches.final)} />
                  <div className="absolute top-1/2 -right-8 w-8 h-[3px] bg-brand-yellow/50" />
                </div>
              </div>

              <div className="flex flex-col justify-center w-1/3 pl-8 z-10">
                <div className="border-[4px] border-brand-yellow bg-zinc-950 p-6 text-center shadow-[12px_12px_0_var(--color-brand-yellow)] rotate-2">
                  <Trophy className="w-16 h-16 text-brand-yellow mx-auto mb-4" />
                  <span className="font-display text-2xl text-zinc-400 uppercase tracking-widest block">Campione</span>
                  <span className="font-display text-3xl text-white uppercase tracking-widest opacity-50 block mt-2">???</span>
                </div>
              </div>

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
              className="relative w-full max-w-2xl bg-zinc-950 border-[4px] border-brand-orange shadow-[16px_16px_0_var(--color-brand-blue)] z-10 flex flex-col max-h-[90vh]"
            >
              <div className="flex justify-between items-center p-6 border-b-[3px] border-zinc-800 bg-zinc-900">
                <h3 className="font-display text-2xl uppercase text-white tracking-wide">{selectedMatch.round}</h3>
                <button onClick={() => setSelectedMatch(null)} className="text-zinc-400 hover:text-white border-2 border-transparent hover:border-brand-orange p-1 transition-colors">
                  <X size={28} />
                </button>
              </div>

              <div className="p-6 md:p-10 overflow-y-auto">
                {selectedMatch.status === "IN PROGRAMMA" ? (
                  <div className="text-center py-10">
                    <p className="font-display text-3xl uppercase text-zinc-500 mb-2">Match in programma</p>
                    <p className="font-sans text-zinc-600">{selectedMatch.date}</p>
                  </div>
                ) : (
                  <>
                    <div className="flex justify-center items-center gap-4 md:gap-8 mb-10">
                      <div className="text-center flex-1">
                        <div className="font-sans font-black text-lg md:text-2xl uppercase mb-3 text-zinc-300 leading-tight">{selectedMatch.team1.name}</div>
                        <div className={`font-mono text-6xl md:text-8xl font-bold ${selectedMatch.team1.score >= selectedMatch.team2.score ? "text-brand-orange" : "text-zinc-600"}`}>
                          {selectedMatch.team1.score}
                        </div>
                      </div>
                      <div className="font-display text-3xl md:text-5xl text-zinc-800">VS</div>
                      <div className="text-center flex-1">
                        <div className="font-sans font-black text-lg md:text-2xl uppercase mb-3 text-zinc-300 leading-tight">{selectedMatch.team2.name}</div>
                        <div className={`font-mono text-6xl md:text-8xl font-bold ${selectedMatch.team2.score >= selectedMatch.team1.score ? "text-white" : "text-zinc-600"}`}>
                          {selectedMatch.team2.score}
                        </div>
                      </div>
                    </div>

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
              </div>
            </motion.div>
          </div>
        )}
      </AnimatePresence>
    </div>
  );
}
