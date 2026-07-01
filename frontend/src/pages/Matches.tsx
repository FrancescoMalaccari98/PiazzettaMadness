import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { motion, AnimatePresence } from "motion/react";
import { Trophy, Flame, X, Swords, Calendar, ChevronRight } from "lucide-react";

// ── Helper link squadra → pagina Players filtrata su quella squadra ──
const teamLink = (team: string) => `/giocatori?team=${encodeURIComponent(team)}`;

// Esclude i segnaposto del bracket dai link.
const isRealTeam = (name: string) =>
  !!name && name !== "TBD" && !/^\d+°/.test(name) && !name.includes("Girone")
  && !name.startsWith("Perdente") && !name.startsWith("Vincente");

// Sostituisce "TBD" con nomi contestuali basati sulla fase del match.
// semiIndex: 0 = SF1, 1 = SF2.
function resolveMatch(match: Match, semiIndex = 0): Match {
  if (match.phase === "GroupStage") return match;
  const resolve = (name: string, slot: "team1" | "team2"): string => {
    if (name && name !== "TBD") return name;
    switch (match.phase) {
      case "SemiFinal":
        return slot === "team1"
          ? (semiIndex === 0 ? "1° Girone A" : "1° Girone B")
          : (semiIndex === 0 ? "2° Girone B" : "2° Girone A");
      case "ThirdPlaceFinal":
        return slot === "team1" ? "Perdente Semifinale 1" : "Perdente Semifinale 2";
      case "Final":
        return slot === "team1" ? "Vincente Semifinale 1" : "Vincente Semifinale 2";
      default:
        return name || "TBD";
    }
  };
  return {
    ...match,
    team1: { ...match.team1, name: resolve(match.team1.name, "team1") },
    team2: { ...match.team2, name: resolve(match.team2.name, "team2") },
  };
}

// Abbrevia nomi lunghi solo su mobile.
const mobileAbbrev = (name: string) => name.replace(/^Philadelphia(?=\s|$)/, "Phila.");

function DisplayName({ name }: { name: string }) {
  const short = mobileAbbrev(name);
  if (short === name) return <>{name}</>;
  return (
    <>
      <span className="sm:hidden">{short}</span>
      <span className="hidden sm:inline">{name}</span>
    </>
  );
}

// Link cliccabile sul nome squadra. stopPropagation: dentro righe/card
// che hanno un loro onClick (apertura tabellino), il click sul nome
// naviga alla pagina squadra senza aprire anche il modal.
function TeamLink({ name, className = "" }: { name: string; className?: string }) {
  if (!isRealTeam(name)) return <span className={className}><DisplayName name={name} /></span>;
  return (
    <Link
      to={teamLink(name)}
      onClick={e => e.stopPropagation()}
      className={`${className} hover:text-brand-orange transition-colors`}
    >
      <DisplayName name={name} />
    </Link>
  );
}

type TeamScore = { name: string; score: number; color?: string | null };
type Match = {
  id: string;
  round: string;
  phase: string;
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

function BoxScoreTable({ team, getPlayerSlug }: { team: TeamBoxScore; getPlayerSlug: (nome: string) => string | undefined }) {
  const pct = (r: number, t: number) => t === 0 ? "—" : `${Math.round(r * 100 / t)}%`;

  return (
    <div>
      <div className="flex items-center justify-between mb-2">
        <h5 className="font-display text-lg uppercase text-white tracking-wide">
          <TeamLink name={team.nome} />
        </h5>
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
                <td className="py-2 pr-4 font-sans font-bold text-zinc-300">
                  {(() => {
                    const slug = getPlayerSlug(p.nome);
                    return slug
                      ? <Link to={`/statistiche/${slug}`} className="hover:text-brand-orange transition-colors">{p.nome}</Link>
                      : p.nome;
                  })()}
                </td>
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

// defaultGroups: struttura vuota — tutti i dati reali arrivano dall'API.
// teams e matches vengono popolati da /api/squadre e /api/partite.
const defaultGroups: Group[] = [
  {
    key: "A",
    label: "Girone A",
    accentClass: "text-brand-blue",
    borderClass: "border-brand-blue",
    bgClass: "bg-brand-blue/10",
    shadowClass: "shadow-[6px_6px_0_var(--color-brand-blue)]",
    teams: [],
    matches: [],
  },
  {
    key: "B",
    label: "Girone B",
    accentClass: "text-brand-orange",
    borderClass: "border-brand-orange",
    bgClass: "bg-brand-orange/10",
    shadowClass: "shadow-[6px_6px_0_var(--color-brand-orange)]",
    teams: [],
    matches: [],
  },
];

type Bracket = { semis: Match[]; third: Match | null; final: Match | null };

// — Classifica —

function computeStandings(teams: string[], matches: Match[]) {
  const s: Record<string, { g: number; v: number; p: number; pt: number; pf: number; ps: number }> = {};
  teams.forEach(t => { s[t] = { g: 0, v: 0, p: 0, pt: 0, pf: 0, ps: 0 }; });

  for (const m of matches) {
    if (m.status !== "COMPLETA") continue;
    // Inizializza entry se il team non era nella lista iniziale (es. team di altro girone)
    if (!s[m.team1.name]) s[m.team1.name] = { g: 0, v: 0, p: 0, pt: 0, pf: 0, ps: 0 };
    if (!s[m.team2.name]) s[m.team2.name] = { g: 0, v: 0, p: 0, pt: 0, pf: 0, ps: 0 };
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
  }

  return teams
    .map(t => ({ name: t, ...s[t] }))
    .sort((a, b) => b.pt - a.pt || (b.pf - b.ps) - (a.pf - a.ps));
}

function StandingsTable({ group }: { group: Group }) {
  const rows = computeStandings(group.teams, group.matches);

  return (
    <table className="w-full text-sm table-fixed">
      <thead>
        <tr className={`border-b-2 ${group.borderClass} border-opacity-30`}>
          <th className="text-left pb-3 pr-1.5 sm:pr-3 font-display text-xs uppercase tracking-widest text-zinc-500 w-6 sm:w-8">#</th>
          <th className="text-left pb-3 font-display text-xs uppercase tracking-widest text-zinc-500">Squadra</th>
          <th className="text-center pb-3 px-1 sm:px-3 font-display text-xs uppercase tracking-widest text-zinc-500 w-6 sm:w-9">G</th>
          <th className="text-center pb-3 px-1 sm:px-3 font-display text-xs uppercase tracking-widest text-zinc-500 w-6 sm:w-9">V</th>
          <th className="text-center pb-3 px-1 sm:px-3 font-display text-xs uppercase tracking-widest text-zinc-500 w-6 sm:w-9">P</th>
          <th className="text-center pb-3 px-1 sm:px-3 font-display text-xs uppercase tracking-widest text-brand-orange w-7 sm:w-10">PT</th>
          <th className="text-center pb-3 pl-1 sm:pl-3 font-display text-xs uppercase tracking-widest text-zinc-500 w-9 sm:w-12">+/-</th>
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
              <td className="py-3 pr-1 min-w-0">
                <div className="flex items-center gap-1.5 sm:gap-2 min-w-0">
                  {qualifies && (
                    <span className={`w-1 h-4 ${group.borderClass} bg-current ${group.accentClass} opacity-60 shrink-0`} />
                  )}
                  <TeamLink name={row.name} className={`font-sans font-bold text-sm uppercase truncate min-w-0 flex-1 block ${i === 0 ? "text-white" : "text-zinc-300"}`} />
                  {qualifies && (
                    <span className={`font-display text-[9px] uppercase tracking-widest ${group.accentClass} border ${group.borderClass} px-1 shrink-0 opacity-70`}>
                      Q
                    </span>
                  )}
                </div>
              </td>
              <td className="py-3 text-center font-mono text-xs text-zinc-400 px-1.5 sm:px-3">{row.g}</td>
              <td className="py-3 text-center font-mono text-xs text-zinc-400 px-1.5 sm:px-3">{row.v}</td>
              <td className="py-3 text-center font-mono text-xs text-zinc-400 px-1.5 sm:px-3">{row.p}</td>
              <td className={`py-3 text-center font-mono font-bold text-sm px-1.5 sm:px-3 ${i === 0 ? group.accentClass : "text-brand-orange/70"}`}>{row.pt}</td>
              <td className={`py-3 text-center font-mono text-xs pl-1.5 sm:pl-3 font-bold ${row.g === 0 ? "text-zinc-600" : diff > 0 ? "text-green-500" : diff < 0 ? "text-red-500" : "text-zinc-400"}`}>
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

// Riga partita a due righe (una per squadra): i nomi reali dal backend
// restano sempre leggibili anche su mobile in verticale.
function TeamScoreLine({ name, score, win, pending, accent = "text-brand-orange", color }: {
  name: string; score: number; win: boolean; pending: boolean; accent?: string; color?: string | null;
}) {
  return (
    <div className="flex items-center justify-between gap-2">
      <div className="flex items-center gap-1.5 min-w-0">
        {color && <span className="w-[3px] h-4 shrink-0" style={{ backgroundColor: color }} />}
        <TeamLink name={name} className={`font-sans font-bold text-xs sm:text-sm uppercase truncate
          ${win ? "text-white" : pending ? "text-zinc-400" : "text-zinc-500"}`} />
      </div>
      <span className={`font-mono text-base sm:text-lg font-bold shrink-0 tabular-nums w-7 text-right
        ${win ? accent : pending ? "text-zinc-600" : "text-zinc-500"}`}>
        {pending ? "—" : score}
      </span>
    </div>
  );
}

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
      <span className="font-mono text-[10px] text-zinc-600 w-12 sm:w-20 shrink-0">{match.date}</span>

      {/* Squadre impilate con punteggio allineato a destra */}
      <div className="flex-1 min-w-0 flex flex-col gap-1.5 border-l border-zinc-800 pl-3">
        <TeamScoreLine name={match.team1.name} score={match.team1.score} win={t1Wins} pending={isPending} color={match.team1.color} />
        <TeamScoreLine name={match.team2.name} score={match.team2.score} win={t2Wins} pending={isPending} color={match.team2.color} />
      </div>

      {/* Status */}
      <div className="w-5 shrink-0 text-right">
        {isLive && <span className="font-display text-[10px] uppercase text-brand-orange animate-pulse">●</span>}
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
          <div className="flex items-center gap-1.5 min-w-0 max-w-[150px]">
            {match.team1.color && <span className="w-[3px] h-4 shrink-0" style={{ backgroundColor: match.team1.color }} />}
            <TeamLink name={match.team1.name} className="font-sans font-[900] tracking-tight text-sm uppercase truncate block" />
          </div>
          <span className="font-mono text-2xl font-bold">{isPending ? "—" : match.team1.score}</span>
        </div>
        <div className={`flex justify-between items-center ${!isPending && match.team2.score > match.team1.score ? "text-white" : "text-zinc-500"}`}>
          <div className="flex items-center gap-1.5 min-w-0 max-w-[150px]">
            {match.team2.color && <span className="w-[3px] h-4 shrink-0" style={{ backgroundColor: match.team2.color }} />}
            <TeamLink name={match.team2.name} className="font-sans font-[900] tracking-tight text-sm uppercase truncate block" />
          </div>
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
  const [bracketMatches, setBracket]      = useState<Bracket>({ semis: [], third: null, final: null });
  const [selectedMatch, setSelectedMatch] = useState<Match | null>(null);
  const [matchDetail,   setMatchDetail]   = useState<MatchDetail | null>(null);
  const [loadingDetail, setLoadingDetail] = useState(false);
  // Mappa nome giocatore (minuscolo) → slug, per linkare i nomi nei tabellini
  // alla pagina statistiche del giocatore.
  const [playerSlugs, setPlayerSlugs] = useState<Record<string, string>>({});
  const getPlayerSlug = (nome: string) => playerSlugs[nome.trim().toLowerCase()];

  useEffect(() => {
    fetch(`${API}/api-web/giocatori`)
      .then(r => r.ok ? r.json() as Promise<{ name: string; slug: string }[]> : null)
      .then(list => {
        if (!list) return;
        const map: Record<string, string> = {};
        for (const p of list) if (p.name && p.slug) map[p.name.trim().toLowerCase()] = p.slug;
        setPlayerSlugs(map);
      })
      .catch(() => {});
  }, []);

  // Fetch squadre + partite in parallelo dal backend
  useEffect(() => {
    Promise.all([
      fetch(`${API}/api-web/squadre`).then(r => r.ok ? r.json() as Promise<TeamApi[]> : null),
      fetch(`${API}/api-web/partite`).then(r => r.ok ? r.json() as Promise<Match[]>   : null),
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

      // Aggiorna bracket con partite playoff reali dal campo phase
      if (partite) {
        setBracket({
          semis: partite.filter(m => m.phase === "SemiFinal"),
          third: partite.find(m => m.phase === "ThirdPlaceFinal") ?? null,
          final: partite.find(m => m.phase === "Final") ?? null,
        });
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
      const res = await fetch(`${API}/api-web/partite/${match.id}`);
      if (res.ok) setMatchDetail(await res.json());
    } catch {
      // backend non ancora disponibile — mostra il summary base
    } finally {
      setLoadingDetail(false);
    }
  };

  // Tutte le partite piattate e raggruppate per data (playoff con nomi risolti)
  const allMatches = [
    ...groups.flatMap(g => g.matches),
    ...bracketMatches.semis.map((m, i) => resolveMatch(m, i)),
    ...(bracketMatches.third ? [resolveMatch(bracketMatches.third)] : []),
    ...(bracketMatches.final ? [resolveMatch(bracketMatches.final)] : []),
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

  // Ordina i giorni cronologicamente, e le partite all'interno di ogni giorno per orario
  const parseTime = (date: string) => date.trim().split(/\s+/)[2] ?? "00:00";
  const calendarEntries = Object.entries(calendarByDay)
    .sort(([a], [b]) => parseDayKey(a) - parseDayKey(b))
    .map(([day, ms]) => [day, [...ms].sort((a, b) => parseTime(a.date).localeCompare(parseTime(b.date)))] as [string, Match[]]);

  return (
    <div className="pt-32 pb-20">
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}>

        {/* Header */}
        <div className="relative overflow-hidden border-b-[4px] border-zinc-800 mb-16">
          <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none">
            <span className="font-display font-black text-[18vw] uppercase text-white/[0.025] whitespace-nowrap tracking-tighter leading-none">
              MATCH
            </span>
          </div>
          <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center pb-12">
            <h1 className="font-display text-[56px] sm:text-[80px] md:text-[120px] text-brand-orange uppercase leading-[0.8] mb-6 tracking-[-2px] md:tracking-[-4px]">
              Match
            </h1>
            <p className="text-base sm:text-xl font-sans text-zinc-400 max-w-2xl mx-auto">
              Dal girone all'italiana fino alla pazzesca finale dei playoff. Ripercorri ogni canestro.
            </p>
          </div>
        </div>

        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* — CALENDARIO — */}
        <div className="mb-24">
          <h2 className="font-display text-2xl sm:text-4xl md:text-5xl uppercase flex items-center gap-4 border-t-[6px] border-zinc-800 pt-6 pb-10 text-white">
            <Calendar className="w-6 h-6 sm:w-10 sm:h-10 text-brand-orange shrink-0" /> Calendario
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
                {dayMatches.map(match => {
                  const isPending = match.status === "IN PROGRAMMA";
                  const t1Wins = !isPending && match.team1.score > match.team2.score;
                  const t2Wins = !isPending && match.team2.score > match.team1.score;
                  return (
                  <div
                    key={match.id}
                    onClick={() => handleSelectMatch(match)}
                    className={`flex items-center gap-3 px-3 sm:px-4 py-3 border-b border-zinc-800/60 last:border-0
                      hover:bg-zinc-800/30 cursor-pointer transition-colors group
                      ${match.status === "LIVE" ? "bg-brand-orange/5" : ""}`}
                  >
                    {/* Orario + round */}
                    <div className="w-12 sm:w-28 shrink-0">
                      <span className="font-mono text-xs text-zinc-500 block">
                        {match.date.split(" ").slice(2).join(" ")}
                      </span>
                      <span className="font-display text-[8px] uppercase tracking-wider text-zinc-700 hidden sm:block truncate">
                        {match.round}
                      </span>
                    </div>

                    {/* Squadre impilate */}
                    <div className="flex-1 min-w-0 flex flex-col gap-1.5 border-l border-zinc-800 pl-3">
                      <TeamScoreLine name={match.team1.name} score={match.team1.score} win={t1Wins} pending={isPending} color={match.team1.color} />
                      <TeamScoreLine name={match.team2.name} score={match.team2.score} win={t2Wins} pending={isPending} color={match.team2.color} />
                    </div>

                    {/* Status badge */}
                    <div className="w-5 shrink-0 text-right">
                      {match.status === "LIVE" && (
                        <span className="font-display text-[10px] uppercase text-brand-orange animate-pulse">●</span>
                      )}
                      {match.status === "COMPLETA" && (
                        <span className="font-display text-[10px] uppercase text-zinc-600">✓</span>
                      )}
                      {isPending && (
                        <ChevronRight className="w-3 h-3 text-zinc-700 inline opacity-0 group-hover:opacity-100 transition-opacity" />
                      )}
                    </div>
                  </div>
                  );
                })}
              </div>
            ))}
          </div>
        </div>

        {/* — FASE A GIRONI — */}
        <div className="mb-24">
          <h2 className="font-display text-2xl sm:text-4xl md:text-5xl uppercase flex items-center gap-4 border-t-[6px] border-zinc-800 pt-6 pb-10 text-white">
            <Calendar className="w-6 h-6 sm:w-10 sm:h-10 text-brand-blue shrink-0" /> Fase a Gironi
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

        {/* — PLAYOFF — solo se ci sono partite playoff nel DB — */}
        {(bracketMatches.semis.length > 0 || bracketMatches.final !== null) && (
        <div>
          <h2 className="font-display text-2xl sm:text-4xl md:text-5xl uppercase flex items-center gap-4 border-t-[6px] border-zinc-800 pt-6 pb-10 text-white">
            <Swords className="w-6 h-6 sm:w-10 sm:h-10 text-brand-orange shrink-0" /> Playoff Bracket
          </h2>

          <div className="overflow-x-auto pb-6 cursor-grab active:cursor-grabbing">
            <div className="min-w-[900px] flex bg-zinc-900/40 p-8 md:p-12 border-[4px] border-zinc-800 shadow-inner">

              {bracketMatches.semis.length > 0 && (
                <>
                  <div className="flex flex-col justify-around w-1/3 pr-8 gap-16 relative z-10">
                    {bracketMatches.semis.map((match, i) => (
                      <div key={match.id} className="relative">
                        <PlayoffCard match={resolveMatch(match, i)} onClick={() => handleSelectMatch(match)} />
                        <div className="absolute top-1/2 -right-8 w-8 h-[3px] bg-zinc-700" />
                      </div>
                    ))}
                  </div>
                  <div className="w-0 relative z-0">
                    <div className="absolute top-[25%] bottom-[25%] left-0 w-[3px] bg-zinc-700" />
                    <div className="absolute top-1/2 left-0 w-8 h-[3px] bg-brand-orange" />
                  </div>
                </>
              )}

              {bracketMatches.final && (
                <div className="flex flex-col justify-center w-1/3 px-8 relative z-10">
                  <div className="relative">
                    <PlayoffCard match={resolveMatch(bracketMatches.final)} isFinal onClick={() => handleSelectMatch(bracketMatches.final!)} />
                    <div className="absolute top-1/2 -right-8 w-8 h-[3px] bg-brand-yellow/50" />
                  </div>
                </div>
              )}

              <div className="flex flex-col justify-center w-1/3 pl-8 z-10">
                {(() => {
                  const finale = bracketMatches.final;
                  const campione = finale?.status === "COMPLETA"
                    ? (finale.team1.score > finale.team2.score ? finale.team1.name : finale.team2.name)
                    : null;
                  return (
                    <div className={`border-[4px] bg-zinc-950 p-6 text-center rotate-2 ${campione ? "border-brand-yellow shadow-[12px_12px_0_var(--color-brand-yellow)]" : "border-brand-yellow/40 shadow-[12px_12px_0_rgba(0,0,0,0.4)]"}`}>
                      <Trophy className={`w-16 h-16 mx-auto mb-4 ${campione ? "text-brand-yellow" : "text-brand-yellow/30"}`} />
                      <span className="font-display text-2xl text-zinc-400 uppercase tracking-widest block">Campione</span>
                      <span className={`font-display text-2xl uppercase tracking-widest block mt-2 leading-tight ${campione ? "text-brand-yellow" : "text-white opacity-20"}`}>
                        {campione ? <TeamLink name={campione} /> : "???"}
                      </span>
                    </div>
                  );
                })()}
              </div>

            </div>
          </div>
        </div>
        )}
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
              <div className="flex justify-between items-center gap-3 p-4 sm:p-6 border-b-[3px] border-zinc-800 bg-zinc-900">
                <h3 className="font-display text-lg sm:text-2xl uppercase text-white tracking-wide truncate">{selectedMatch.round}</h3>
                <button onClick={() => setSelectedMatch(null)} className="text-zinc-400 hover:text-white border-2 border-transparent hover:border-brand-orange p-1 transition-colors shrink-0">
                  <X size={28} />
                </button>
              </div>

              <div className="p-4 sm:p-6 overflow-y-auto">
                {selectedMatch.status === "IN PROGRAMMA" ? (
                  <div className="text-center py-10">
                    <img src="/assets/logo.png" alt="" className="w-16 h-16 mx-auto mb-6 opacity-30" />
                    <p className="font-display text-3xl uppercase text-zinc-500 mb-2">Match in programma</p>
                    <p className="font-sans text-zinc-600">{selectedMatch.date}</p>
                  </div>
                ) : selectedMatch.status === "LIVE" ? (
                  <div className="text-center py-10">
                    <div className="inline-flex items-center gap-2.5 px-4 py-2 border-2 border-red-500 text-red-500 font-display uppercase tracking-widest text-sm mb-6">
                      <span className="relative flex h-2.5 w-2.5">
                        <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-500 opacity-75" />
                        <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-red-500" />
                      </span>
                      Live
                    </div>
                    <p className="font-display text-3xl uppercase text-white mb-4">Partita in corso</p>
                    <p className="font-sans text-zinc-400 mb-8">Segui il punteggio in tempo reale sulla pagina dedicata.</p>
                    <Link
                      to="/live"
                      onClick={() => setSelectedMatch(null)}
                      className="inline-block bg-brand-orange text-brand-bg font-display uppercase tracking-widest px-8 py-4 text-lg hover:bg-white transition-colors"
                    >
                      Vai alla Diretta
                    </Link>
                  </div>
                ) : (
                  <>
                    {/* Punteggio finale */}
                    <div className="flex justify-center items-center gap-4 mb-6">
                      <div className="text-center flex-1">
                        <div className="font-sans font-black text-base md:text-xl uppercase mb-2 text-zinc-300 leading-tight">
                          <TeamLink name={selectedMatch.team1.name} />
                        </div>
                        <div className={`font-mono text-6xl md:text-8xl font-bold ${selectedMatch.team1.score >= selectedMatch.team2.score ? "text-brand-orange" : "text-zinc-600"}`}>
                          {selectedMatch.team1.score}
                        </div>
                      </div>
                      <div className="font-display text-3xl text-zinc-800">VS</div>
                      <div className="text-center flex-1">
                        <div className="font-sans font-black text-base md:text-xl uppercase mb-2 text-zinc-300 leading-tight">
                          <TeamLink name={selectedMatch.team2.name} />
                        </div>
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
                        {/* MVP della partita — miglior giocatore della squadra vincente per valutazione */}
                        {(() => {
                          const homeWon = matchDetail.totaleCasa > matchDetail.totaleTrasferta;
                          const winningTeam = homeWon ? matchDetail.squadraCasa : matchDetail.squadraTrasferta;
                          const mvp = [...winningTeam.giocatori].sort((a, b) => b.valutazione - a.valutazione)[0];
                          if (!mvp) return null;
                          const mvpSlug = getPlayerSlug(mvp.nome);
                          return (
                            <div className="bg-brand-blue/10 border-[3px] border-brand-blue p-4 mb-6 flex items-center gap-4">
                              <div className="bg-brand-blue p-2.5 shrink-0">
                                <Flame className="w-6 h-6 text-brand-bg" />
                              </div>
                              <div>
                                <p className="font-display text-xs uppercase tracking-widest text-brand-blue mb-1">MVP della Partita</p>
                                <p className="font-sans text-white text-lg font-bold">
                                  {mvpSlug
                                    ? <Link to={`/statistiche/${mvpSlug}`} className="hover:text-brand-orange transition-colors">{mvp.nome}</Link>
                                    : mvp.nome}
                                  <span className="text-zinc-500 text-sm font-normal ml-2">
                                    {mvp.punti} pts · {mvp.valutazione} val · {winningTeam.nome}
                                  </span>
                                </p>
                              </div>
                            </div>
                          );
                        })()}

                        {/* Punteggi per quarto (solo se ci sono dati) */}
                        {matchDetail.quartiCasa.length > 0 && (
                        <div className="mb-6 overflow-x-auto">
                          <table className="w-full text-xs text-center border border-zinc-800">
                            <thead>
                              <tr className="bg-zinc-900 border-b border-zinc-700">
                                <th className="text-left px-3 py-2 font-display uppercase tracking-widest text-zinc-500 w-32">Squadra</th>
                                {matchDetail.quartiCasa.map((_: number, i: number) => (
                                  <th key={i} className="px-3 py-2 font-display uppercase tracking-widest text-zinc-500">Q{i+1}</th>
                                ))}
                                <th className="px-3 py-2 font-display uppercase tracking-widest text-brand-orange">TOT</th>
                              </tr>
                            </thead>
                            <tbody>
                              <tr className="border-b border-zinc-800">
                                <td className="text-left px-3 py-2 font-sans font-bold text-zinc-300 truncate"><TeamLink name={matchDetail.squadraCasa.nome} /></td>
                                {matchDetail.quartiCasa.map((q: number, i: number) => <td key={i} className="px-3 py-2 font-mono text-zinc-400">{q}</td>)}
                                <td className="px-3 py-2 font-mono font-bold text-brand-orange">{matchDetail.totaleCasa}</td>
                              </tr>
                              <tr>
                                <td className="text-left px-3 py-2 font-sans font-bold text-zinc-300 truncate"><TeamLink name={matchDetail.squadraTrasferta.nome} /></td>
                                {matchDetail.quartiTrasferta.map((q: number, i: number) => <td key={i} className="px-3 py-2 font-mono text-zinc-400">{q}</td>)}
                                <td className="px-3 py-2 font-mono font-bold text-white">{matchDetail.totaleTrasferta}</td>
                              </tr>
                            </tbody>
                          </table>
                        </div>
                        )}

                        {/* Tabella giocatori */}
                        <div className="space-y-6 mb-6">
                          <BoxScoreTable team={matchDetail.squadraCasa} getPlayerSlug={getPlayerSlug} />
                          <BoxScoreTable team={matchDetail.squadraTrasferta} getPlayerSlug={getPlayerSlug} />
                        </div>
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
                            <p className="font-sans text-white text-lg font-bold">
                              {(() => {
                                const slug = getPlayerSlug(selectedMatch.details.mvp);
                                return slug
                                  ? <Link to={`/statistiche/${slug}`} className="hover:text-brand-orange transition-colors">{selectedMatch.details.mvp}</Link>
                                  : selectedMatch.details.mvp;
                              })()}
                            </p>
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
