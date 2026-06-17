import { useState, useEffect } from "react";
import { motion, AnimatePresence } from "motion/react";
import { Target, Trophy, ChevronDown } from "lucide-react";

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

const MAX_STATION = 6; // 4×1pt + 1×2pt money ball

export function ThreePointContest() {
  const [data, setData] = useState<ContestData | null>(null);
  const [loading, setLoading] = useState(true);
  const [expandedPlayer, setExpandedPlayer] = useState<string | null>(null);

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

            {/* Classifica completa */}
            <section>
              <h2 className="font-display text-xl sm:text-2xl uppercase tracking-widest text-zinc-500 mb-6 flex items-center gap-3">
                <Target className="w-5 h-5 text-brand-orange" /> Classifica
              </h2>

              <div className="border-[3px] border-zinc-800 bg-zinc-900 overflow-hidden">
                {/* Header tabella */}
                <div className="hidden sm:grid grid-cols-[auto_1fr_auto_auto_auto] gap-4 items-center px-5 py-3 bg-zinc-950 border-b-2 border-zinc-800 font-display text-xs uppercase tracking-widest text-zinc-500">
                  <span className="w-8 text-center">#</span>
                  <span>Giocatore</span>
                  <span className="w-28 text-center">Squadra</span>
                  <span className="w-16 text-center">Punti</span>
                  <span className="w-8" />
                </div>

                {data.entries.map((entry, i) => {
                  const pos = entry.final_position ?? i + 1;
                  const style = POSITION_STYLES[pos];
                  const isExpanded = expandedPlayer === entry.player;

                  return (
                    <div key={entry.player} className="border-b border-zinc-800/60 last:border-0">
                      <button
                        onClick={() => setExpandedPlayer(isExpanded ? null : entry.player)}
                        className={`w-full grid grid-cols-[auto_1fr_auto_auto] sm:grid-cols-[auto_1fr_auto_auto_auto] gap-3 sm:gap-4 items-center px-4 sm:px-5 py-4 hover:bg-zinc-800/30 transition-colors text-left ${
                          pos <= 3 && isCompleted ? "bg-zinc-800/10" : ""
                        }`}
                      >
                        {/* Posizione */}
                        <span className={`w-8 h-8 flex items-center justify-center font-display text-sm font-bold shrink-0 ${
                          style ? `${style.badge}` : "text-zinc-600"
                        }`}>
                          {pos}
                        </span>

                        {/* Nome + squadra (mobile: impilati) */}
                        <div className="min-w-0">
                          <p className={`font-sans font-bold text-sm sm:text-base uppercase truncate ${
                            style ? style.text : "text-zinc-300"
                          }`}>
                            {entry.player}
                          </p>
                          <p className="font-sans text-xs text-zinc-500 truncate sm:hidden">{entry.team}</p>
                        </div>

                        {/* Squadra (desktop) */}
                        <span className="hidden sm:block w-28 text-center font-sans text-xs text-zinc-500 uppercase truncate">
                          {entry.team_short ?? entry.team}
                        </span>

                        {/* Punteggio */}
                        <span className={`font-mono text-xl sm:text-2xl font-black tabular-nums w-16 text-center ${
                          style ? style.text : "text-zinc-400"
                        }`}>
                          {entry.total_score}
                        </span>

                        {/* Expand */}
                        {entry.rounds.length > 0 && (
                          <motion.span
                            animate={{ rotate: isExpanded ? 180 : 0 }}
                            transition={{ duration: 0.2 }}
                            className="hidden sm:block w-8 text-center"
                          >
                            <ChevronDown className={`w-5 h-5 ${isExpanded ? "text-brand-orange" : "text-zinc-600"}`} />
                          </motion.span>
                        )}
                      </button>

                      {/* Dettaglio round espanso */}
                      <AnimatePresence initial={false}>
                        {isExpanded && entry.rounds.length > 0 && (
                          <motion.div
                            initial={{ height: 0, opacity: 0 }}
                            animate={{ height: "auto", opacity: 1 }}
                            exit={{ height: 0, opacity: 0 }}
                            transition={{ duration: 0.2 }}
                            className="overflow-hidden"
                          >
                            <div className="px-5 pb-4 pt-1 space-y-3 border-t border-zinc-800">
                              {entry.rounds.map(round => (
                                <div key={round.round_number}>
                                  <p className="font-display text-xs uppercase tracking-widest text-zinc-500 mb-2">
                                    {ROUND_LABELS[round.round_type] ?? round.round_type}
                                  </p>
                                  <div className="flex items-center gap-2">
                                    {round.stations.map((score, si) => (
                                      <div key={si} className="flex-1 max-w-[80px]">
                                        <div className="h-1.5 bg-zinc-800 rounded-full overflow-hidden mb-1">
                                          <div
                                            className={`h-full rounded-full ${score >= 5 ? "bg-brand-yellow" : score >= 3 ? "bg-brand-orange" : "bg-zinc-600"}`}
                                            style={{ width: `${(score / MAX_STATION) * 100}%` }}
                                          />
                                        </div>
                                        <div className="flex items-center justify-between">
                                          <span className="font-display text-[9px] uppercase text-zinc-600">S{si + 1}</span>
                                          <span className={`font-mono text-xs font-bold ${
                                            score >= 5 ? "text-brand-yellow" : score >= 3 ? "text-brand-orange" : "text-zinc-500"
                                          }`}>
                                            {score}
                                          </span>
                                        </div>
                                      </div>
                                    ))}
                                    <div className="border-l border-zinc-700 pl-3 ml-1">
                                      <span className="font-mono text-lg font-black text-white">{round.total}</span>
                                      <span className="font-display text-[9px] uppercase text-zinc-600 block">TOT</span>
                                    </div>
                                  </div>
                                </div>
                              ))}
                            </div>
                          </motion.div>
                        )}
                      </AnimatePresence>
                    </div>
                  );
                })}
              </div>
            </section>

            {/* Regole rapide */}
            <div className="border-[3px] border-dashed border-zinc-700 bg-zinc-900/50 p-6 sm:p-8">
              <h3 className="font-display text-lg uppercase text-zinc-400 mb-4 flex items-center gap-2">
                <Target className="w-4 h-4" /> Come funziona
              </h3>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-sm font-sans text-zinc-400">
                <div className="bg-zinc-950 border border-zinc-800 p-4 text-center">
                  <p className="font-mono text-3xl text-brand-orange font-black mb-1">5</p>
                  <p>Postazioni dietro la linea dei 3 punti</p>
                </div>
                <div className="bg-zinc-950 border border-zinc-800 p-4 text-center">
                  <p className="font-mono text-3xl text-brand-yellow font-black mb-1">25</p>
                  <p>Palloni totali (4 normali + 1 money ball per postazione)</p>
                </div>
                <div className="bg-zinc-950 border border-zinc-800 p-4 text-center">
                  <p className="font-mono text-3xl text-white font-black mb-1">90"</p>
                  <p>Secondi per completare il percorso</p>
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
    <div className="flex items-end justify-center gap-3 sm:gap-5 mb-8">
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
            <p className={`font-sans font-bold text-xs sm:text-sm uppercase text-center mb-2 leading-tight ${style.text}`}>
              {entry.player}
            </p>
            <p className="font-sans text-[10px] text-zinc-500 uppercase mb-3 truncate">{entry.team}</p>
            <div className={`w-full ${heights[i]} border-[3px] ${style.border} bg-zinc-900 flex flex-col items-center justify-center relative`}>
              <span className={`font-display text-4xl sm:text-5xl font-black ${style.text}`}>
                {entry.total_score}
              </span>
              <span className="font-display text-xs uppercase tracking-widest text-zinc-500">pts</span>
              <div className={`absolute -top-4 w-8 h-8 ${style.badge} flex items-center justify-center font-display text-sm font-black`}>
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
