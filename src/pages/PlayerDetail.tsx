import { useParams, Link, useNavigate } from "react-router-dom";
import { motion } from "motion/react";
import { ArrowLeft, Flame, Target, Shield, Zap, Trophy, Star } from "lucide-react";
import { allPlayers } from "../data/stats";

const statCards = [
  { key: "pts" as const, label: "Punti",    unit: "PPG", Icon: Flame,  accent: "text-brand-orange", border: "border-brand-orange", bg: "bg-brand-orange/10", shadow: "shadow-[6px_6px_0_var(--color-brand-orange)]" },
  { key: "ast" as const, label: "Assist",   unit: "APG", Icon: Target, accent: "text-brand-blue",   border: "border-brand-blue",   bg: "bg-brand-blue/10",   shadow: "shadow-[6px_6px_0_var(--color-brand-blue)]" },
  { key: "reb" as const, label: "Rimbalzi", unit: "RPG", Icon: Shield, accent: "text-brand-yellow", border: "border-brand-yellow", bg: "bg-brand-yellow/10", shadow: "shadow-[6px_6px_0_var(--color-brand-yellow)]" },
  { key: "stl" as const, label: "Recuperi", unit: "SPG", Icon: Zap,   accent: "text-green-400",    border: "border-green-500",    bg: "bg-green-500/10",    shadow: "shadow-[6px_6px_0_rgba(34,197,94,0.4)]" },
];

export function PlayerDetail() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const player = allPlayers.find(p => p.slug === slug);

  if (!player) {
    return (
      <div className="pt-40 pb-20 text-center">
        <p className="font-display text-4xl text-zinc-500 uppercase">Giocatore non trovato</p>
        <Link to="/statistiche" className="mt-8 inline-flex items-center gap-2 text-brand-orange font-display uppercase tracking-widest hover:underline">
          <ArrowLeft className="w-4 h-4" /> Torna alle statistiche
        </Link>
      </div>
    );
  }

  const playedMatches = player.matchLog.filter(m => m.result !== "-");
  const bestGame = [...playedMatches].sort((a, b) => b.pts - a.pts)[0];

  // Ranking del giocatore nelle varie stat tra tutti
  const rank = (key: "pts" | "ast" | "reb" | "stl") => {
    const sorted = [...allPlayers].sort((a, b) => b[key] - a[key]);
    return sorted.findIndex(p => p.slug === player.slug) + 1;
  };

  return (
    <div className="w-full min-h-screen bg-brand-bg pt-24 pb-24">

      {/* Back link */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mb-8">
        <button
          onClick={() => navigate(-1)}
          className="inline-flex items-center gap-2 font-display text-sm uppercase tracking-widest text-zinc-500 hover:text-brand-orange transition-colors bg-transparent"
        >
          <ArrowLeft className="w-4 h-4" />
          Indietro
        </button>
      </div>

      {/* Header giocatore */}
      <div className="relative border-b-[4px] border-zinc-800 mb-12 overflow-hidden">
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none overflow-hidden">
          <span className="font-display font-black text-[18vw] uppercase text-white/[0.025] whitespace-nowrap tracking-tighter leading-none">
            {player.name.split(" ")[1] ?? player.name}
          </span>
        </div>

        <motion.div
          initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}
          className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-12"
        >
          {player.number && (
            <span className="font-display text-[100px] md:text-[160px] leading-none text-white/[0.06] font-black absolute top-0 right-8 pointer-events-none select-none">
              #{player.number}
            </span>
          )}

          <div className="flex items-end gap-8 flex-wrap">
            {/* Foto o placeholder iniziali */}
            <div className="shrink-0">
              {player.photo ? (
                <img
                  src={player.photo}
                  alt={player.name}
                  className="w-32 h-32 md:w-44 md:h-44 object-cover border-[4px] border-brand-orange shadow-[8px_8px_0_var(--color-brand-blue)] grayscale hover:grayscale-0 transition-all duration-500"
                />
              ) : (
                <div className="w-32 h-32 md:w-44 md:h-44 border-[4px] border-zinc-700 bg-zinc-900 flex items-center justify-center shadow-[8px_8px_0_rgba(0,0,0,0.4)]">
                  <span className="font-display text-5xl md:text-7xl text-zinc-600 uppercase select-none">
                    {player.name.split(" ").map(n => n[0]).join("").slice(0, 2)}
                  </span>
                </div>
              )}
            </div>

            {/* Info */}
            <div>
              <p className="font-display text-brand-orange uppercase tracking-[0.3em] text-sm mb-3">{player.team}</p>
              <h1 className="font-display text-[80px] md:text-[120px] uppercase leading-[0.8] tracking-[-4px] text-white mb-8">
                {player.name}
              </h1>
              <div className="flex items-center gap-4 flex-wrap">
                <span className="font-display text-xs uppercase tracking-[0.2em] text-zinc-500 border border-zinc-700 px-3 py-1">
                  {playedMatches.length} partite giocate
                </span>
                {rank("pts") <= 3 && (
                  <span className="font-display text-xs uppercase tracking-[0.2em] text-brand-yellow border border-brand-yellow px-3 py-1 flex items-center gap-1">
                    <Star className="w-3 h-3" /> Top {rank("pts")} scorer
                  </span>
                )}
              </div>
            </div>
          </div>
        </motion.div>
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-12">

        {/* Medie stagione */}
        <section>
          <h2 className="font-display text-xl uppercase tracking-widest text-zinc-500 mb-5">Medie stagionali</h2>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {statCards.map(card => (
              <div key={card.key} className={`border-[3px] ${card.border} bg-zinc-900 ${card.shadow} overflow-hidden`}>
                <div className={`${card.bg} border-b-2 ${card.border} border-opacity-40 px-4 py-2 flex items-center justify-between`}>
                  <span className={`font-display text-xs uppercase tracking-widest ${card.accent}`}>{card.label}</span>
                  <card.Icon className={`w-4 h-4 ${card.accent}`} />
                </div>
                <div className="p-4">
                  <div className={`font-mono text-4xl font-bold ${card.accent} mb-1`}>{player[card.key]}</div>
                  <div className="font-display text-xs uppercase tracking-widest text-zinc-600">{card.unit} · #{rank(card.key)} nel torneo</div>
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* Best game */}
        {bestGame && (
          <section>
            <h2 className="font-display text-xl uppercase tracking-widest text-zinc-500 mb-5 flex items-center gap-2">
              <Trophy className="w-5 h-5 text-brand-yellow" /> Miglior partita
            </h2>
            <div className="border-[3px] border-brand-yellow bg-zinc-900 shadow-[6px_6px_0_var(--color-brand-yellow)] p-6 md:p-8">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-6">
                <div>
                  <p className="font-display text-xs uppercase tracking-[0.3em] text-zinc-500 mb-1">{bestGame.date} · vs {bestGame.opponent}</p>
                  <p className="font-display text-4xl md:text-5xl text-white uppercase">{player.name}</p>
                </div>
                <div className="grid grid-cols-4 gap-3">
                  {[
                    { label: "PTS", val: bestGame.pts, accent: "text-brand-orange" },
                    { label: "AST", val: bestGame.ast, accent: "text-brand-blue" },
                    { label: "REB", val: bestGame.reb, accent: "text-brand-yellow" },
                    { label: "STL", val: bestGame.stl, accent: "text-green-400" },
                  ].map(s => (
                    <div key={s.label} className="border-2 border-zinc-700 bg-zinc-950 p-3 text-center min-w-[60px]">
                      <div className={`font-mono text-2xl font-bold ${s.accent}`}>{s.val}</div>
                      <div className="font-display text-[10px] uppercase tracking-widest text-zinc-500">{s.label}</div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </section>
        )}

        {/* Log partite */}
        <section>
          <h2 className="font-display text-xl uppercase tracking-widest text-zinc-500 mb-5">Statistiche per partita</h2>
          <div className="border-[3px] border-zinc-800 bg-zinc-900 overflow-hidden">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-zinc-950 border-b-2 border-zinc-800">
                  <th className="text-left px-5 py-3 font-display text-xs uppercase tracking-widest text-zinc-500">Data</th>
                  <th className="text-left px-5 py-3 font-display text-xs uppercase tracking-widest text-zinc-500">Avversario</th>
                  <th className="text-center px-4 py-3 font-display text-xs uppercase tracking-widest text-zinc-500">Ris.</th>
                  <th className="text-center px-4 py-3 font-display text-xs uppercase tracking-widest text-brand-orange">PTS</th>
                  <th className="text-center px-4 py-3 font-display text-xs uppercase tracking-widest text-brand-blue">AST</th>
                  <th className="text-center px-4 py-3 font-display text-xs uppercase tracking-widest text-brand-yellow">REB</th>
                  <th className="text-center px-4 py-3 font-display text-xs uppercase tracking-widest text-green-400">STL</th>
                </tr>
              </thead>
              <tbody>
                {player.matchLog.map((log, i) => {
                  const isPending = log.result === "-";
                  const isBest = bestGame && log.matchId === bestGame.matchId;
                  return (
                    <tr
                      key={log.matchId}
                      className={`border-b border-zinc-800/50 last:border-0 transition-colors
                        ${isBest ? "bg-brand-yellow/5" : i % 2 === 0 ? "bg-zinc-900" : "bg-zinc-950/50"}
                        ${isPending ? "opacity-40" : "hover:bg-zinc-800/40"}`}
                    >
                      <td className="px-5 py-3 font-mono text-xs text-zinc-500">{log.date}</td>
                      <td className="px-5 py-3 font-sans font-bold text-sm uppercase text-zinc-300">
                        {isBest && <span className="text-brand-yellow mr-2 text-xs">★</span>}
                        {log.opponent}
                      </td>
                      <td className="px-4 py-3 text-center">
                        {isPending
                          ? <span className="font-display text-xs text-zinc-600 uppercase tracking-widest">—</span>
                          : <span className={`font-display text-xs font-bold uppercase px-2 py-0.5 border ${log.result === "V" ? "text-green-400 border-green-500/30 bg-green-500/10" : "text-red-400 border-red-500/30 bg-red-500/10"}`}>
                              {log.result}
                            </span>
                        }
                      </td>
                      <td className={`px-4 py-3 text-center font-mono font-bold ${isPending ? "text-zinc-700" : "text-brand-orange"}`}>
                        {isPending ? "—" : log.pts}
                      </td>
                      <td className={`px-4 py-3 text-center font-mono ${isPending ? "text-zinc-700" : "text-zinc-400"}`}>
                        {isPending ? "—" : log.ast}
                      </td>
                      <td className={`px-4 py-3 text-center font-mono ${isPending ? "text-zinc-700" : "text-zinc-400"}`}>
                        {isPending ? "—" : log.reb}
                      </td>
                      <td className={`px-4 py-3 text-center font-mono ${isPending ? "text-zinc-700" : "text-zinc-400"}`}>
                        {isPending ? "—" : log.stl}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </section>

      </div>
    </div>
  );
}
