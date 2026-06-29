import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { motion } from "motion/react";
import { Target, Trophy } from "lucide-react";

function playerSlug(fullName: string, jerseyNumber: number | null = null): string {
  const base = fullName
    .toLowerCase()
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/\s+/g, "-")
    .replace(/[^a-z0-9-]/g, "");
  return jerseyNumber !== null ? `${base}-${jerseyNumber}` : base;
}

const API = import.meta.env.VITE_API_URL ?? "";

type Round = {
  round_number: number;
  round_type: "Qualification" | "Final" | "TieBreak";
  stations: number[];
  total: number;
};

type Entry = {
  player: string;
  team: string;
  team_short: string | null;
  seed_order: number | null;
  total_score: number;
  final_position: number | null;
  jersey_number: number | null;
  team_color: string | null;
  rounds: Round[];
};

type ContestData = {
  status: "Scheduled" | "Live" | "Completed" | "Cancelled" | "none";
  event_name?: string;
  scheduled_at?: string;
  entries: Entry[];
  message?: string;
};

const ROUND_LABELS: Record<string, string> = {
  Qualification: "Qualificazione",
  Final: "Finale",
  TieBreak: "Spareggio",
};

const POSITION_STYLES: Record<number, { badge: string; border: string; text: string }> = {
  1: { badge: "bg-brand-yellow text-brand-bg", border: "border-brand-yellow", text: "text-brand-yellow" },
  2: { badge: "bg-zinc-400 text-brand-bg", border: "border-zinc-400", text: "text-zinc-300" },
  3: { badge: "bg-amber-700 text-white", border: "border-amber-700", text: "text-amber-600" },
};

const MAX_STATION = 6;

const QUAL_TYPES = ["Qualification"];
const FINAL_TYPES = ["Final", "TieBreak"];

function roundTotal(entry: Entry, types: string[]): number {
  return entry.rounds
    .filter(r => types.includes(r.round_type))
    .reduce((s, r) => s + r.total, 0);
}

function StationBars({ round }: { round: Round }) {
  return (
    <div className="flex items-end gap-2">
      {round.stations.map((score, si) => (
        <div key={si} className="flex-1 max-w-[80px]">
          <div className="h-1.5 bg-zinc-800 rounded-full overflow-hidden mb-1">
            <div
              className={`h-full rounded-full transition-all ${
                score >= 5 ? "bg-brand-yellow"
                : score >= 3 ? "bg-brand-orange"
                : "bg-zinc-600"
              }`}
              style={{ width: `${(score / MAX_STATION) * 100}%` }}
            />
          </div>
          <div className="flex items-center justify-between">
            <span className="font-display text-[9px] uppercase text-zinc-600">S{si + 1}</span>
            <span className={`font-mono text-xs font-bold ${
              score >= 5 ? "text-brand-yellow"
              : score >= 3 ? "text-brand-orange"
              : "text-zinc-500"
            }`}>
              {score}
            </span>
          </div>
        </div>
      ))}
      <div className="border-l border-zinc-700 pl-3 ml-1 shrink-0">
        <span className="font-mono text-lg font-black text-white">{round.total}</span>
        <span className="font-display text-[9px] uppercase text-zinc-600 block">TOT</span>
      </div>
    </div>
  );
}

function PlayerRow({ entry, pos, isCompleted, showRoundTypes }: {
  entry: Entry;
  pos: number;
  isCompleted: boolean;
  showRoundTypes: string[];
}) {
  const style = POSITION_STYLES[pos];
  const visibleRounds = entry.rounds.filter(r => showRoundTypes.includes(r.round_type));
  const sectionTotal = visibleRounds.reduce((s, r) => s + r.total, 0);

  return (
    <div className={`border-b border-zinc-800/60 last:border-0 ${pos <= 3 && isCompleted ? "bg-zinc-800/10" : ""}`}>
      {/* Info giocatore */}
      <div className="flex items-center gap-2 sm:gap-4 px-3 sm:px-5 py-3 sm:py-4">
        <span
          className="w-7 h-7 sm:w-8 sm:h-8 flex items-center justify-center font-display text-xs sm:text-sm font-bold shrink-0 text-white"
          style={{ backgroundColor: entry.team_color ?? "#3f3f46" }}
        >
          {pos}
        </span>

        <div className="min-w-0 flex-1">
          <Link
            to={`/statistiche/${playerSlug(entry.player, entry.jersey_number)}`}
            className={`font-sans font-bold text-xs sm:text-base uppercase truncate block hover:text-brand-orange transition-colors ${
              style ? style.text : "text-zinc-300"
            }`}
          >
            {entry.player}
          </Link>
          <p className="font-sans text-[10px] sm:text-xs text-zinc-500 truncate">{entry.team}</p>
        </div>

        <span className={`font-mono text-lg sm:text-2xl font-black tabular-nums shrink-0 ${
          style ? style.text : "text-zinc-400"
        }`}>
          {sectionTotal}
        </span>
      </div>

      {/* Dettaglio stazioni — solo round della sezione corrente */}
      {visibleRounds.length > 0 && (
        <div className="px-3 sm:px-5 pb-3 sm:pb-4 pt-0 space-y-2 sm:space-y-3">
          {visibleRounds.map(round => (
            <div key={round.round_number}>
              <p className="font-display text-[9px] sm:text-[10px] uppercase tracking-widest text-zinc-500 mb-1.5 sm:mb-2">
                {ROUND_LABELS[round.round_type] ?? round.round_type}
              </p>
              <StationBars round={round} />
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export function ThreePointContest() {
  const [data, setData] = useState<ContestData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch(`${API}/api-web/three-point-contest`)
      .then(r => r.ok ? r.json() as Promise<ContestData> : null)
      .then(d => { if (d) setData(d); })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const hasEntries = data && data.entries.length > 0;
  const isCompleted = data?.status === "Completed";
  const isLive = data?.status === "Live";

  // Separa qualificazione da finalisti.
  // I finalisti restano anche in qualEntries con il solo punteggio di qualificazione.
  const qualEntries  = data?.entries.filter(e => e.rounds.some(r => r.round_type === "Qualification")) ?? [];
  const finalEntries = data?.entries.filter(e => e.rounds.some(r => r.round_type === "Final" || r.round_type === "TieBreak")) ?? [];
  // Ordina i finalisti: per final_position se settata, altrimenti per punteggio Final DESC
  const sortedFinalEntries = [...finalEntries].sort((a, b) => {
    if (a.final_position !== null && b.final_position !== null) {
      return a.final_position - b.final_position;
    }
    return roundTotal(b, FINAL_TYPES) - roundTotal(a, FINAL_TYPES);
  });
  const hasFinals = finalEntries.length > 0;
  // Podio automatico: ≥3 finalisti hanno un punteggio Final > 0
  const showPodium = sortedFinalEntries.filter(e => roundTotal(e, FINAL_TYPES) > 0).length >= 3;

  return (
    <div className="pt-32 pb-20 min-h-screen">
      {/* Header */}
      <div className="relative overflow-hidden border-b-[4px] border-zinc-800 mb-12">
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none">
          <span className="font-display font-black text-[14vw] uppercase text-white/[0.025] whitespace-nowrap tracking-tighter leading-none">
            3PT
          </span>
        </div>
        <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center pb-12">
          <h1 className="font-display text-[40px] sm:text-[65px] md:text-[110px] text-brand-orange uppercase leading-[0.8] mb-6 tracking-[-2px] md:tracking-[-4px]">
            3 Point<br />Contest
          </h1>
          <p className="text-base sm:text-xl font-sans text-zinc-400 max-w-2xl mx-auto">
            5 postazioni, 25 palloni, 90 secondi. Chi ha la mano più calda della Piazzetta?
          </p>
        </div>
      </div>

      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">

        {loading ? (
          <div className="text-center py-20">
            <div className="inline-block w-12 h-12 border-4 border-zinc-700 border-t-brand-orange rounded-full animate-spin" />
          </div>
        ) : !hasEntries ? (
          <EmptyState message={data?.message} scheduledAt={data?.scheduled_at} />
        ) : (
          <div className="space-y-10">

            {/* Status badge */}
            {(isLive || isCompleted) && (
              <div className="flex justify-center">
                <div className={`inline-flex items-center gap-2.5 px-4 py-2 border-2 font-display uppercase tracking-widest text-sm ${
                  isLive ? "border-red-500 text-red-500" : "border-brand-yellow text-brand-yellow"
                }`}>
                  {isLive && (
                    <span className="relative flex h-2.5 w-2.5">
                      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-500 opacity-75" />
                      <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-red-500" />
                    </span>
                  )}
                  {isLive ? "In corso" : "Completato"}
                </div>
              </div>
            )}

            {/* Podio (automatico quando ≥3 finalisti hanno punteggio > 0) */}
            {showPodium && (
              <Podium entries={sortedFinalEntries} />
            )}

            {/* FINALE — separata dalla qualificazione */}
            {hasFinals && (
              <section>
                <h2 className="font-display text-xl sm:text-2xl uppercase tracking-widest text-brand-yellow mb-6 flex items-center gap-3">
                  <Trophy className="w-5 h-5 text-brand-yellow" /> Finale — Top 3
                </h2>
                <div className="border-[3px] border-brand-yellow bg-zinc-900 overflow-hidden">
                  <div className="hidden sm:grid grid-cols-[auto_1fr_auto_auto] gap-4 items-center px-5 py-3 bg-zinc-950 border-b-2 border-brand-yellow/30 font-display text-xs uppercase tracking-widest text-zinc-500">
                    <span className="w-8 text-center">#</span>
                    <span>Giocatore</span>
                    <span className="w-20 text-center">Squadra</span>
                    <span className="w-12 text-center">Punti</span>
                  </div>
                  {sortedFinalEntries
                    .map((entry, i) => (
                      <PlayerRow key={entry.player} entry={entry} pos={entry.final_position ?? i + 1} isCompleted={isCompleted} showRoundTypes={FINAL_TYPES} />
                    ))}
                </div>
              </section>
            )}

            {/* QUALIFICAZIONE */}
            <section>
              <h2 className="font-display text-xl sm:text-2xl uppercase tracking-widest text-zinc-500 mb-6 flex items-center gap-3">
                <Target className="w-5 h-5 text-brand-orange" /> {hasFinals ? "Qualificazione" : "Classifica"}
              </h2>
              <div className="border-[3px] border-zinc-800 bg-zinc-900 overflow-hidden">
                <div className="hidden sm:grid grid-cols-[auto_1fr_auto_auto] gap-4 items-center px-5 py-3 bg-zinc-950 border-b-2 border-zinc-800 font-display text-xs uppercase tracking-widest text-zinc-500">
                  <span className="w-8 text-center">#</span>
                  <span>Giocatore</span>
                  <span className="w-20 text-center">Squadra</span>
                  <span className="w-12 text-center">Punti</span>
                </div>
                {(hasFinals ? qualEntries : data.entries)
                  .sort((a, b) => roundTotal(b, QUAL_TYPES) - roundTotal(a, QUAL_TYPES))
                  .map((entry, i) => (
                    <PlayerRow key={entry.player} entry={entry} pos={i + 1} isCompleted={isCompleted} showRoundTypes={QUAL_TYPES} />
                  ))}
              </div>
            </section>

            {/* Regole rapide */}
            <div>
              <h3 className="font-display text-lg sm:text-2xl uppercase tracking-widest text-zinc-500 mb-6 flex items-center gap-3">
                <Target className="w-5 h-5 text-brand-orange" /> Come funziona
              </h3>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
                <div className="bg-zinc-900 border-[4px] border-brand-orange shadow-[6px_6px_0_var(--color-brand-orange)] p-6 sm:p-8 flex flex-col items-center text-center">
                  <p className="font-display text-6xl sm:text-7xl text-brand-orange font-black leading-none mb-4">5</p>
                  <p className="font-sans text-zinc-400 text-sm">Postazioni dietro la linea dei 3 punti</p>
                </div>
                <div className="bg-zinc-900 border-[4px] border-brand-yellow shadow-[6px_6px_0_var(--color-brand-yellow)] p-6 sm:p-8 flex flex-col items-center text-center">
                  <p className="font-display text-6xl sm:text-7xl text-brand-yellow font-black leading-none mb-4">25</p>
                  <p className="font-sans text-zinc-400 text-sm">Palloni totali (4 da 1pt + 1 money ball da 2pt)</p>
                </div>
                <div className="bg-zinc-900 border-[4px] border-zinc-400 shadow-[6px_6px_0_rgba(161,161,170,0.5)] p-6 sm:p-8 flex flex-col items-center text-center">
                  <p className="font-display text-6xl sm:text-7xl text-white font-black leading-none mb-4">90"</p>
                  <p className="font-sans text-zinc-400 text-sm">Secondi per completare il percorso</p>
                </div>
                <div className="bg-zinc-900 border-[4px] border-brand-blue shadow-[6px_6px_0_var(--color-brand-blue)] p-6 sm:p-8 flex flex-col items-center text-center">
                  <p className="font-display text-6xl sm:text-7xl text-brand-blue font-black leading-none mb-4">3</p>
                  <p className="font-sans text-zinc-400 text-sm">I migliori vanno in finale. Parità: spareggio da 3 postazioni</p>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

// ── Podio top 3 ─────────────────────────────────────────────
// entries deve essere già ordinato per punteggio Final DESC (1°, 2°, 3°)
function Podium({ entries }: { entries: Entry[] }) {
  const top3 = entries.slice(0, 3);
  if (top3.length < 3) return null;

  // 2°-sinistra, 1°-centro, 3°-destra
  const config = [
    {
      entry: top3[1], pos: 2,
      blockH: "h-20 sm:h-28 md:h-36",
      borderColor: "border-zinc-400",
      bgColor: "bg-zinc-800/60",
      numColor: "text-zinc-400",
      nameColor: "text-white",
      scoreColor: "text-zinc-300",
      zClass: "-mr-[3px]",
    },
    {
      entry: top3[0], pos: 1,
      blockH: "h-32 sm:h-44 md:h-56",
      borderColor: "border-brand-yellow",
      bgColor: "bg-brand-yellow/10",
      numColor: "text-brand-yellow",
      nameColor: "text-brand-yellow",
      scoreColor: "text-brand-yellow",
      zClass: "relative z-10",
    },
    {
      entry: top3[2], pos: 3,
      blockH: "h-14 sm:h-20 md:h-28",
      borderColor: "border-amber-700",
      bgColor: "bg-amber-900/20",
      numColor: "text-amber-700",
      nameColor: "text-amber-400",
      scoreColor: "text-amber-500",
      zClass: "-ml-[3px]",
    },
  ];

  return (
    <div className="mb-10">
      {/* Colonne: info + blocco — items-end allinea i blocchi in basso,
          così i nomi si posizionano naturalmente sopra il proprio blocco */}
      <div className="flex items-end justify-center">
        {config.map(({ entry, pos, blockH, borderColor, bgColor, numColor, nameColor, scoreColor, zClass }, i) => {
          const score = roundTotal(entry, FINAL_TYPES);
          return (
            <motion.div
              key={entry.player}
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: i * 0.12, duration: 0.5, ease: [0.22, 1, 0.36, 1] }}
              className={`flex flex-col items-center flex-1 ${zClass}`}
            >
              {/* Info giocatore — subito sopra il blocco */}
              <div className="text-center px-1 pb-2 sm:pb-3 w-full">
                <p className={`font-display text-[11px] sm:text-base md:text-xl font-black uppercase leading-tight break-words ${nameColor}`}>
                  {entry.player}
                </p>
                <p className="font-sans text-[9px] sm:text-[10px] uppercase tracking-wide text-zinc-500 mt-0.5">
                  {entry.team}
                </p>
                <p className={`font-display text-lg sm:text-3xl md:text-4xl font-black ${scoreColor} mt-0.5 sm:mt-1 leading-none`}>
                  {score}
                  <span className="font-sans text-[10px] sm:text-xs text-zinc-500 ml-0.5 sm:ml-1 font-normal">pts</span>
                </p>
              </div>

              {/* Blocco podio */}
              <motion.div
                initial={{ scaleY: 0 }}
                animate={{ scaleY: 1 }}
                style={{ originY: 1 }}
                transition={{ delay: i * 0.12 + 0.15, duration: 0.4, ease: [0.22, 1, 0.36, 1] }}
                className={`w-full ${blockH} ${bgColor} border-[3px] border-b-0 ${borderColor} flex items-center justify-center`}
              >
                <span className={`font-display text-4xl sm:text-6xl md:text-7xl font-black ${numColor} opacity-40 select-none`}>
                  {pos}
                </span>
              </motion.div>
            </motion.div>
          );
        })}
      </div>
      {/* Base */}
      <div className="h-[3px] bg-zinc-700" />
    </div>
  );
}

// ── Stato vuoto ─────────────────────────────────────────────
function EmptyState({ message, scheduledAt }: { message?: string; scheduledAt?: string }) {
  return (
    <div className="border-[3px] border-dashed border-zinc-700 bg-zinc-900/50 p-10 sm:p-16 text-center max-w-2xl mx-auto">
      <img src="/assets/logo.png" alt="" className="w-16 h-16 mx-auto mb-6 opacity-30" />
      <h2 className="font-display text-3xl sm:text-4xl uppercase text-white mb-3">
        3 Point Contest
      </h2>
      <p className="font-sans text-zinc-400 text-base sm:text-lg mb-2">
        {message ?? "La gara del tiro da 3 punti non è ancora iniziata."}
      </p>
      {scheduledAt && (
        <p className="font-mono text-brand-orange text-lg mt-4">{scheduledAt}</p>
      )}
      <p className="font-sans text-zinc-500 text-sm mt-6">
        Sabato 11 Luglio — durante la fase finale del torneo.
      </p>
    </div>
  );
}
