import { useState, useEffect } from "react";
import { motion } from "motion/react";
import { Target, Trophy } from "lucide-react";

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

function StationBars({ round }: { round: Round }) {
  return (
    <div className="flex items-center gap-1">
      {round.stations.map((score, si) => (
        <div key={si} className={`w-8 h-8 flex items-center justify-center font-mono text-xs font-bold border border-zinc-700 ${
          score >= 5 ? "bg-brand-yellow/20 text-brand-yellow border-brand-yellow/40"
          : score >= 3 ? "bg-brand-orange/10 text-brand-orange border-brand-orange/30"
          : "bg-zinc-900 text-zinc-500"
        }`}>
          {score}
        </div>
      ))}
      <div className="font-mono text-base font-black text-white ml-2 w-8 text-center">
        {round.total}
      </div>
    </div>
  );
}

function PlayerRow({ entry, pos, isCompleted }: { entry: Entry; pos: number; isCompleted: boolean }) {
  const style = POSITION_STYLES[pos];

  return (
    <div className={`border-b border-zinc-800/60 last:border-0 ${pos <= 3 && isCompleted ? "bg-zinc-800/10" : ""}`}>
      {/* Info giocatore */}
      <div className="flex items-center gap-2 sm:gap-4 px-3 sm:px-5 py-3 sm:py-4">
        <span className={`w-7 h-7 sm:w-8 sm:h-8 flex items-center justify-center font-display text-xs sm:text-sm font-bold shrink-0 ${
          style ? style.badge : "text-zinc-600"
        }`}>
          {pos}
        </span>

        <div className="min-w-0 flex-1">
          <p className={`font-sans font-bold text-xs sm:text-base uppercase truncate ${
            style ? style.text : "text-zinc-300"
          }`}>
            {entry.player}
          </p>
          <p className="font-sans text-[10px] sm:text-xs text-zinc-500 truncate">{entry.team}</p>
        </div>

        <span className={`font-mono text-lg sm:text-2xl font-black tabular-nums shrink-0 ${
          style ? style.text : "text-zinc-400"
        }`}>
          {entry.total_score}
        </span>
      </div>

      {/* Dettaglio stazioni — sempre visibile */}
      {entry.rounds.length > 0 && (
        <div className="px-3 sm:px-5 pb-3 sm:pb-4 pt-0 space-y-2 sm:space-y-3">
          {entry.rounds.map(round => (
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

  // Separa qualificazione da finalisti
  const qualEntries = data?.entries.filter(e => !e.rounds.some(r => r.round_type === "Final" || r.round_type === "TieBreak")) ?? [];
  const finalEntries = data?.entries.filter(e => e.rounds.some(r => r.round_type === "Final" || r.round_type === "TieBreak")) ?? [];
  const hasFinals = finalEntries.length > 0;

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

            {/* Podio (solo se completato con posizioni) */}
            {isCompleted && data.entries.some(e => e.final_position !== null) && (
              <Podium entries={data.entries} />
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
                  {finalEntries
                    .sort((a, b) => (a.final_position ?? 99) - (b.final_position ?? 99))
                    .map((entry, i) => (
                      <PlayerRow key={entry.player} entry={entry} pos={entry.final_position ?? i + 1} isCompleted={isCompleted} />
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
                  .sort((a, b) => b.total_score - a.total_score)
                  .map((entry, i) => (
                    <PlayerRow key={entry.player} entry={entry} pos={entry.final_position ?? i + 1} isCompleted={isCompleted} />
                  ))}
              </div>
            </section>

            {/* Regole rapide */}
            <div className="border-[3px] border-dashed border-zinc-700 bg-zinc-900/50 p-6 sm:p-8">
              <h3 className="font-display text-lg uppercase text-zinc-400 mb-4 flex items-center gap-2">
                <Target className="w-4 h-4" /> Come funziona
              </h3>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 text-sm font-sans text-zinc-400">
                <div className="bg-zinc-950 border border-zinc-800 p-4 text-center">
                  <p className="font-mono text-3xl text-brand-orange font-black mb-1">5</p>
                  <p>Postazioni dietro la linea dei 3 punti</p>
                </div>
                <div className="bg-zinc-950 border border-zinc-800 p-4 text-center">
                  <p className="font-mono text-3xl text-brand-yellow font-black mb-1">25</p>
                  <p>Palloni totali (4 da 1pt + 1 money ball da 2pt)</p>
                </div>
                <div className="bg-zinc-950 border border-zinc-800 p-4 text-center">
                  <p className="font-mono text-3xl text-white font-black mb-1">90"</p>
                  <p>Secondi per completare il percorso</p>
                </div>
                <div className="bg-zinc-950 border border-zinc-800 p-4 text-center">
                  <p className="font-mono text-3xl text-brand-blue font-black mb-1">3</p>
                  <p>I migliori vanno in finale. Parità: spareggio da 3 postazioni</p>
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
function Podium({ entries }: { entries: Entry[] }) {
  const top3 = entries.filter(e => e.final_position !== null && e.final_position <= 3)
    .sort((a, b) => a.final_position! - b.final_position!);

  if (top3.length === 0) return null;

  const podiumOrder = [top3[1], top3[0], top3[2]].filter(Boolean);
  const heights = ["h-28 sm:h-36", "h-36 sm:h-48", "h-20 sm:h-28"];

  return (
    <div className="flex items-end justify-center gap-2 sm:gap-5 mb-8">
      {podiumOrder.map((entry, i) => {
        if (!entry) return null;
        const pos = entry.final_position!;
        const style = POSITION_STYLES[pos]!;
        return (
          <motion.div
            key={entry.player}
            initial={{ opacity: 0, y: 40 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: i * 0.15, duration: 0.5, ease: [0.22, 1, 0.36, 1] }}
            className="flex flex-col items-center w-1/3 max-w-[200px]"
          >
            <p className={`font-sans font-bold text-[10px] sm:text-sm uppercase text-center mb-1 sm:mb-2 leading-tight break-words ${style.text}`}>
              {entry.player}
            </p>
            <p className="font-sans text-[9px] sm:text-[10px] text-zinc-500 uppercase mb-2 sm:mb-3 truncate max-w-full">{entry.team}</p>
            <div className={`w-full ${heights[i]} border-[2px] sm:border-[3px] ${style.border} bg-zinc-900 flex flex-col items-center justify-center relative`}>
              <span className={`font-display text-2xl sm:text-5xl font-black ${style.text}`}>
                {entry.total_score}
              </span>
              <span className="font-display text-[9px] sm:text-xs uppercase tracking-widest text-zinc-500">pts</span>
              <div className={`absolute -top-3 sm:-top-4 w-6 h-6 sm:w-8 sm:h-8 ${style.badge} flex items-center justify-center font-display text-xs sm:text-sm font-black`}>
                {pos}
              </div>
            </div>
          </motion.div>
        );
      })}
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
