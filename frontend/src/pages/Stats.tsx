import { useState, useEffect, useRef } from "react";
import { Link } from "react-router-dom";
import { motion, AnimatePresence, useInView, animate as motionAnimate } from "motion/react";
import { Trophy, Flame, Medal, ChevronDown, Star, Target, Shield, Zap, BarChart2, TrendingUp } from "lucide-react";
import { defaultStatsData, type StatsData, type Player } from "../data/stats";

const API = import.meta.env.VITE_API_URL ?? "";

function AnimatedNumber({ value, className }: { value: number; className?: string }) {
  const ref = useRef<HTMLSpanElement>(null);
  const isInView = useInView(ref, { once: true });
  const [display, setDisplay] = useState(0);

  useEffect(() => {
    if (!isInView) return;
    const controls = motionAnimate(0, value, {
      duration: 1.0,
      ease: "easeOut",
      onUpdate: (v) => setDisplay(Math.round(v * 10) / 10),
    });
    return controls.stop;
  }, [isInView, value]);

  return <span ref={ref} className={className}>{display}</span>;
}

const statCategories = [
  { key: "pts"       as const, label: "Punti",       unit: "PPG", icon: Flame,      accent: "text-brand-orange", border: "border-brand-orange", bg: "bg-brand-orange/10", bar: "bg-brand-orange" },
  { key: "ast"       as const, label: "Assist",      unit: "APG", icon: Target,     accent: "text-brand-blue",   border: "border-brand-blue",   bg: "bg-brand-blue/10",   bar: "bg-brand-blue" },
  { key: "reb"       as const, label: "Rimbalzi",    unit: "RPG", icon: Shield,     accent: "text-brand-yellow", border: "border-brand-yellow", bg: "bg-brand-yellow/10", bar: "bg-brand-yellow" },
  { key: "stl"       as const, label: "Recuperi",    unit: "SPG", icon: Zap,        accent: "text-green-400",    border: "border-green-500",    bg: "bg-green-500/10",    bar: "bg-green-400" },
  { key: "sd"        as const, label: "Stoppate",    unit: "BPG", icon: Shield,     accent: "text-purple-400",   border: "border-purple-500",   bg: "bg-purple-500/10",   bar: "bg-purple-400" },
  { key: "val"       as const, label: "Valutazione", unit: "VAL", icon: Star,       accent: "text-brand-yellow", border: "border-brand-yellow", bg: "bg-brand-yellow/10", bar: "bg-brand-yellow" },
  { key: "plusMinus" as const, label: "+/-",         unit: "Media/G", icon: TrendingUp, accent: "text-cyan-400",     border: "border-cyan-500",     bg: "bg-cyan-500/10",     bar: "bg-cyan-400" },
];

type StatKey = typeof statCategories[number]["key"];

export function Stats() {
  const [data, setData] = useState<StatsData>(defaultStatsData);
  const [activeTab, setActiveTab] = useState<StatKey>("pts");
  const [openTeam, setOpenTeam] = useState<string | null>(null);

  useEffect(() => {
<<<<<<< HEAD
    fetch(`${API}/api-web/statistiche`)
=======
    fetch(`${API}/api/statistiche`)
>>>>>>> 8c935b5209820221f529d117fe84c8a3fdce6e97
      .then(r => r.ok ? r.json() as Promise<StatsData> : null)
      .then(d => { if (d) setData(d); })
      .catch(() => {});
  }, []);

  const { players: allPlayers, matchMvps, tournamentMvpSlug, teamStats } = data;

  const topLeader = (key: StatKey, n = 5) =>
    [...allPlayers].sort((a, b) => b[key] - a[key]).slice(0, n);

  const hasPlayers = allPlayers.length > 0;
  const tournamentMvp = hasPlayers ? (allPlayers.find(p => p.slug === tournamentMvpSlug) ?? allPlayers[0]) : null;
  const topScorer  = hasPlayers ? topLeader("pts", 1)[0] : null;
  const topAssist  = hasPlayers ? topLeader("ast", 1)[0] : null;
  const topReb     = hasPlayers ? topLeader("reb", 1)[0] : null;
  const topVal     = hasPlayers ? topLeader("val", 1)[0] : null;

  const avg = (val: number, g: number) => g > 0 ? (val / g).toFixed(1) : "—";

  const activeCategory = statCategories.find(c => c.key === activeTab)!;
  const leaderboard = topLeader(activeTab, 10);

  const teams = [...new Set(allPlayers.map(p => p.team))];

  return (
    <div className="w-full pt-28 pb-24 bg-brand-bg">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">

        {/* Header */}
        <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} className="mb-16">
          <div className="relative overflow-hidden pb-12 border-b-[4px] border-zinc-800">
            <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none overflow-hidden">
              <span className="font-display font-black text-[15vw] uppercase text-white/[0.025] whitespace-nowrap tracking-tighter leading-none">
                STATS
              </span>
            </div>
            <div className="relative z-10 text-center">
              <p className="font-display text-brand-orange uppercase tracking-[0.3em] text-sm mb-3">Piazzetta Madness 2026</p>
              <h1 className="font-display text-[40px] sm:text-[65px] md:text-[120px] uppercase leading-[0.85] tracking-[-1px] md:tracking-[-4px] text-white mb-6">
                Statistiche<br /><span className="text-brand-orange">Giocatori</span>
              </h1>
              <p className="font-sans text-zinc-400 text-base sm:text-lg max-w-xl mx-auto">
                I numeri non mentono. Ogni canestro, ogni assist, ogni rimbalzo strappato.
              </p>
            </div>
          </div>
        </motion.div>

        {/* — MVP TORNEO — */}
        {tournamentMvp && (
        <section className="mb-16">
          <h2 className="font-display text-lg sm:text-2xl uppercase tracking-widest text-zinc-500 mb-6 flex items-center gap-3">
            <Trophy className="w-5 h-5 text-brand-yellow" /> MVP del Torneo
          </h2>
          <div className="border-[4px] border-brand-yellow bg-zinc-900 shadow-[8px_8px_0_var(--color-brand-yellow)] overflow-hidden">
            <div className="grid grid-cols-1 md:grid-cols-3">
              <div className="bg-brand-yellow/10 flex flex-col items-center justify-center p-6 md:p-10 border-r-0 md:border-r-4 border-brand-yellow/30 border-b-4 md:border-b-0">
                <Medal className="w-10 h-10 md:w-16 md:h-16 text-brand-yellow mb-2 md:mb-3 opacity-80" />
                <span className="font-display text-[56px] md:text-[80px] leading-none text-brand-yellow font-black">MVP</span>
              </div>
              <div className="col-span-2 p-6 md:p-10 flex flex-col justify-center">
                <span className="font-display text-xs uppercase tracking-[0.3em] text-zinc-500 mb-2">Miglior giocatore del torneo</span>
                <h3 className="font-display text-2xl sm:text-4xl md:text-6xl uppercase text-white leading-tight mb-2">{tournamentMvp.name}</h3>
                <p className="font-sans font-bold text-brand-yellow text-base mb-4">{tournamentMvp.team}</p>
                <div className="grid grid-cols-4 gap-2 sm:gap-4">
                  {[
                    { label: "PPG", val: tournamentMvp.pts },
                    { label: "APG", val: tournamentMvp.ast },
                    { label: "RPG", val: tournamentMvp.reb },
                    { label: "VAL", val: tournamentMvp.val },
                  ].map(s => (
                    <div key={s.label} className="border-2 border-zinc-700 bg-zinc-950 p-3 text-center">
                      <AnimatedNumber value={s.val} className="font-mono text-2xl font-bold text-white block" />
                      <div className="font-display text-xs uppercase tracking-widest text-zinc-500">{s.label}</div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </section>
        )}

        {/* — LEADERS — */}
        {hasPlayers && topScorer && topAssist && topReb && topVal && (
        <motion.section
          className="mb-16"
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-60px" }}
          transition={{ duration: 0.5, ease: [0.22, 1, 0.36, 1] }}
        >
          <h2 className="font-display text-lg sm:text-2xl uppercase tracking-widest text-zinc-500 mb-6 flex items-center gap-3">
            <Star className="w-5 h-5 text-brand-orange" /> Leaders di Categoria
          </h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            {[
              { label: "Top Scorer",     unit: "PPG", player: topScorer, val: topScorer.pts,  accent: "text-brand-orange", border: "border-brand-orange", bg: "bg-brand-orange/10", shadow: "shadow-[6px_6px_0_var(--color-brand-orange)]", Icon: Flame },
              { label: "Top Assist",     unit: "APG", player: topAssist, val: topAssist.ast,  accent: "text-brand-blue",   border: "border-brand-blue",   bg: "bg-brand-blue/10",   shadow: "shadow-[6px_6px_0_var(--color-brand-blue)]",   Icon: Target },
              { label: "Top Rimbalzi",   unit: "RPG", player: topReb,    val: topReb.reb,     accent: "text-brand-yellow", border: "border-brand-yellow", bg: "bg-brand-yellow/10", shadow: "shadow-[6px_6px_0_var(--color-brand-yellow)]", Icon: Shield },
              { label: "Top Valutazione",unit: "VAL", player: topVal,    val: topVal.val,     accent: "text-cyan-400",     border: "border-cyan-500",     bg: "bg-cyan-500/10",     shadow: "shadow-[6px_6px_0_rgba(6,182,212,0.4)]",      Icon: BarChart2 },
            ].map(card => (
              <div key={card.label} className={`border-[3px] ${card.border} bg-zinc-900 ${card.shadow} overflow-hidden`}>
                <div className={`${card.bg} border-b-2 ${card.border} border-opacity-40 px-5 py-3 flex items-center justify-between`}>
                  <span className={`font-display text-sm uppercase tracking-widest ${card.accent}`}>{card.label}</span>
                  <card.Icon className={`w-5 h-5 ${card.accent}`} />
                </div>
                <div className="p-5">
                  <AnimatedNumber value={card.val} className={`font-mono text-5xl font-bold ${card.accent} mb-2 block`} />
                  <div className="font-display text-xs uppercase tracking-widest text-zinc-500 mb-3">{card.unit}</div>
                  <div className="font-sans font-bold text-white text-lg uppercase leading-tight">{card.player.name}</div>
                  <div className="font-sans text-zinc-500 text-sm">{card.player.team}</div>
                </div>
              </div>
            ))}
          </div>
        </motion.section>
        )}

        {/* — MVP PER PARTITA — */}
        <section className="mb-16">
          <h2 className="font-display text-lg sm:text-2xl uppercase tracking-widest text-zinc-500 mb-6 flex items-center gap-3">
            <Flame className="w-5 h-5 text-brand-orange" /> MVP per Partita
          </h2>
          <div className="border-[3px] border-zinc-800 bg-zinc-900 overflow-hidden">
            {matchMvps.map((mvp, i) => (
              <div key={i} className="flex items-center gap-4 px-5 py-4 border-b border-zinc-800 last:border-0 hover:bg-zinc-800/40 transition-colors">
                <div className="font-mono text-xs text-zinc-600 w-14 shrink-0">{mvp.date}</div>
                <div className="flex-1 min-w-0">
                  <div className="font-sans text-xs text-zinc-500 uppercase tracking-wide truncate">{mvp.match}</div>
                </div>
                <div className="flex items-center gap-3 shrink-0">
                  <div className="text-right hidden sm:block">
                    <div className="font-sans font-bold text-white text-sm uppercase">{mvp.player}</div>
                    <div className="font-sans text-zinc-500 text-xs">{mvp.team}</div>
                  </div>
                  <div className="bg-brand-orange/10 border border-brand-orange/30 px-3 py-1">
                    <span className="font-mono text-xs text-brand-orange font-bold">{mvp.stat}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* — CLASSIFICHE INDIVIDUALI — */}
        <section className="mb-16">
          <h2 className="font-display text-lg sm:text-2xl uppercase tracking-widest text-zinc-500 mb-6 flex items-center gap-3">
            <Trophy className="w-5 h-5 text-brand-blue" /> Classifiche Individuali
          </h2>

          {/* Tab selector */}
          <div className="flex gap-2 mb-6 flex-wrap">
            {statCategories.map(cat => (
              <button
                key={cat.key}
                onClick={() => setActiveTab(cat.key)}
                className={`flex items-center gap-1.5 px-3 sm:px-5 py-2 font-display text-[10px] sm:text-sm uppercase tracking-widest border-[2px] transition-all
                  ${activeTab === cat.key
                    ? `${cat.border} ${cat.bg} ${cat.accent}`
                    : "border-zinc-700 text-zinc-500 hover:border-zinc-500 hover:text-zinc-300"
                  }`}
              >
                <cat.icon className="w-4 h-4" />
                {cat.label}
              </button>
            ))}
          </div>

          {/* Leaderboard */}
          <div className={`border-[3px] ${activeCategory.border} bg-zinc-900 overflow-hidden`}>
            <div className={`${activeCategory.bg} border-b-2 ${activeCategory.border} border-opacity-40 px-5 py-3 flex items-center justify-between`}>
              <span className={`font-display text-sm uppercase tracking-widest ${activeCategory.accent}`}>
                Top 10 — {activeCategory.label}
              </span>
              <span className={`font-display text-xs uppercase tracking-widest ${activeCategory.accent} opacity-60`}>
                {activeCategory.unit}
              </span>
            </div>
            <AnimatePresence mode="wait">
            <motion.div
              key={activeTab}
              initial={{ opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -8 }}
              transition={{ duration: 0.25, ease: "easeOut" }}
            >
            {leaderboard.map((player, i) => {
              const val = player[activeTab];
              const max = Math.max(...leaderboard.map(p => Math.abs(p[activeTab])));
              const pct = max > 0 ? Math.round((Math.abs(val) / max) * 100) : 0;
              return (
                <div key={player.name} className="flex items-center gap-4 px-5 py-3.5 border-b border-zinc-800/60 last:border-0">
                  <span className={`font-display text-xl w-7 shrink-0 ${i === 0 ? activeCategory.accent : "text-zinc-600"}`}>
                    {i + 1}
                  </span>
                  <div className="flex-1 min-w-0">
                    <div className="flex flex-col sm:flex-row sm:items-center gap-0.5 sm:gap-3 mb-1.5">
                      <Link to={`/statistiche/${player.slug}`} className={`font-sans font-bold text-xs sm:text-sm uppercase hover:text-brand-orange transition-colors truncate ${i === 0 ? "text-white" : "text-zinc-300"}`}>
                        {player.name}
                      </Link>
                      <span className="font-sans text-xs text-zinc-600 truncate hidden sm:block">{player.team}</span>
                    </div>
                    <div className="h-1 bg-zinc-800 w-full">
                      <div
                        className={`h-full ${activeCategory.bar} transition-all`}
                        style={{ width: `${pct}%`, opacity: i === 0 ? 1 : 0.4 + (0.6 * pct / 100) }}
                      />
                    </div>
                  </div>
                  <span className={`font-mono text-xl font-bold shrink-0 ${i === 0 ? activeCategory.accent : "text-zinc-400"}`}>
                    {val > 0 && activeTab === "plusMinus" ? `+${val}` : val}
                  </span>
                </div>
              );
            })}
            </motion.div>
            </AnimatePresence>
          </div>
        </section>

        {/* — STATISTICHE DI SQUADRA (FIBA) — */}
        <section className="mb-16">
          <h2 className="font-display text-lg sm:text-2xl uppercase tracking-widest text-zinc-500 mb-6 flex items-center gap-3">
            <BarChart2 className="w-5 h-5 text-brand-blue" /> Statistiche di Squadra
          </h2>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[700px] border-[3px] border-zinc-800 bg-zinc-900">
              <thead>
                <tr className="bg-zinc-950 border-b-2 border-zinc-800">
                  <th className="text-left px-5 py-3 font-display text-xs uppercase tracking-widest text-zinc-400">Squadra</th>
                  <th className="text-center px-4 py-3 font-display text-xs uppercase tracking-widest text-brand-orange">Area/G</th>
                  <th className="text-center px-4 py-3 font-display text-xs uppercase tracking-widest text-brand-blue">Panchina/G</th>
                  <th className="text-center px-4 py-3 font-display text-xs uppercase tracking-widest text-brand-yellow">Contropiede/G</th>
                  <th className="text-center px-4 py-3 font-display text-xs uppercase tracking-widest text-green-400">P.Perse/G</th>
                  <th className="text-center px-4 py-3 font-display text-xs uppercase tracking-widest text-cyan-400">PPP</th>
                  <th className="text-center px-4 py-3 font-display text-xs uppercase tracking-widest text-purple-400">Max Vantaggio</th>
                </tr>
              </thead>
              <tbody>
                {[...teamStats]
                  .sort((a, b) => b.pointsPerPossession - a.pointsPerPossession)
                  .map((ts, i) => (
                    <tr key={ts.squadra} className={`border-b border-zinc-800/50 ${i === 0 ? "bg-brand-orange/5" : "hover:bg-zinc-800/30"} transition-colors`}>
                      <td className="px-5 py-3">
                        {i === 0 && <span className="text-brand-orange mr-2">★</span>}
                        <span className="font-sans font-bold text-sm uppercase text-zinc-300">{ts.squadra}</span>
                      </td>
                      <td className="text-center px-4 py-3 font-mono font-bold text-brand-orange">{avg(ts.puntiInArea, ts.partiteGiocate)}</td>
                      <td className="text-center px-4 py-3 font-mono text-zinc-400">{avg(ts.puntiPanchina, ts.partiteGiocate)}</td>
                      <td className="text-center px-4 py-3 font-mono text-zinc-400">{avg(ts.puntiContropiede, ts.partiteGiocate)}</td>
                      <td className="text-center px-4 py-3 font-mono text-zinc-400">{avg(ts.puntiDaPallePerse, ts.partiteGiocate)}</td>
                      <td className="text-center px-4 py-3 font-mono text-cyan-400 font-bold">{ts.pointsPerPossession.toFixed(2)}</td>
                      <td className="text-center px-4 py-3 font-mono text-zinc-400">{ts.massimoVantaggio}</td>
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
          <p className="font-display text-[10px] uppercase tracking-widest text-zinc-600 mt-2">
            PPP = Points Per Possession · ordinate per efficienza offensiva
          </p>
        </section>

        {/* — ROSTER PER SQUADRA — */}
        <section>
          <h2 className="font-display text-lg sm:text-2xl uppercase tracking-widest text-zinc-500 mb-6 flex items-center gap-3">
            <Shield className="w-5 h-5 text-brand-yellow" /> Roster per Squadra
          </h2>
          <div className="space-y-3">
            {teams.map(team => {
              const roster = allPlayers.filter((p: Player) => p.team === team).sort((a, b) => b.pts - a.pts);
              const isOpen = openTeam === team;
              return (
                <div key={team} className={`border-[3px] transition-colors ${isOpen ? "border-zinc-600" : "border-zinc-800"} bg-zinc-900 overflow-hidden`}>
                  <button
                    onClick={() => setOpenTeam(isOpen ? null : team)}
                    className="w-full flex items-center justify-between px-5 py-4 hover:bg-zinc-800/40 transition-colors group"
                  >
                    <div className="flex items-center gap-4">
                      <span className="font-display text-lg uppercase tracking-wide text-white group-hover:text-brand-orange transition-colors">{team}</span>
                      <span className="font-display text-xs uppercase tracking-widest text-zinc-600">{roster.length} giocatori</span>
                    </div>
                    <motion.div animate={{ rotate: isOpen ? 180 : 0 }} transition={{ duration: 0.2 }}>
                      <ChevronDown className={`w-5 h-5 ${isOpen ? "text-brand-orange" : "text-zinc-600"}`} />
                    </motion.div>
                  </button>

                  <AnimatePresence initial={false}>
                    {isOpen && (
                      <motion.div
                        initial={{ height: 0, opacity: 0 }}
                        animate={{ height: "auto", opacity: 1 }}
                        exit={{ height: 0, opacity: 0 }}
                        transition={{ duration: 0.2 }}
                        className="overflow-hidden"
                      >
                        <div className="border-t-2 border-zinc-800 overflow-x-auto">
                          <table className="w-full min-w-[640px] text-sm">
                            <thead>
                              <tr className="bg-zinc-950">
                                <th className="text-left px-5 py-3 font-display text-xs uppercase tracking-widest text-zinc-500">Giocatore</th>
                                <th className="text-center px-3 py-3 font-display text-xs uppercase tracking-widest text-brand-orange">PTI</th>
                                <th className="text-center px-3 py-3 font-display text-xs uppercase tracking-widest text-brand-blue">ASS</th>
                                <th className="text-center px-3 py-3 font-display text-xs uppercase tracking-widest text-brand-yellow">RIM</th>
                                <th className="text-center px-3 py-3 font-display text-xs uppercase tracking-widest text-green-400">REC</th>
                                <th className="text-center px-3 py-3 font-display text-xs uppercase tracking-widest text-purple-400">STO</th>
                                <th className="text-center px-3 py-3 font-display text-xs uppercase tracking-widest text-zinc-500">2PT%</th>
                                <th className="text-center px-3 py-3 font-display text-xs uppercase tracking-widest text-zinc-500">3PT%</th>
                                <th className="text-center px-3 py-3 font-display text-xs uppercase tracking-widest text-zinc-500">TL%</th>
                                <th className="text-center px-3 py-3 font-display text-xs uppercase tracking-widest text-cyan-400">VAL</th>
                                <th className="text-center px-3 py-3 font-display text-xs uppercase tracking-widest text-zinc-500">+/-</th>
                              </tr>
                            </thead>
                            <tbody>
                              {roster.map((player: Player, i: number) => (
                                <tr key={player.name} className={`border-t border-zinc-800/50 ${i === 0 ? "bg-brand-orange/5" : "hover:bg-zinc-800/30"} transition-colors`}>
                                  <td className="px-5 py-3">
                                    {i === 0 && <span className="text-brand-orange mr-2">★</span>}
                                    <Link to={`/statistiche/${player.slug}`} className="font-sans font-bold text-sm uppercase text-zinc-300 hover:text-brand-orange transition-colors">
                                      {player.name}
                                    </Link>
                                  </td>
                                  <td className="text-center px-3 py-3 font-mono font-bold text-brand-orange">{player.pts}</td>
                                  <td className="text-center px-3 py-3 font-mono text-zinc-400">{player.ast}</td>
                                  <td className="text-center px-3 py-3 font-mono text-zinc-400">{player.reb}</td>
                                  <td className="text-center px-3 py-3 font-mono text-zinc-400">{player.stl}</td>
                                  <td className="text-center px-3 py-3 font-mono text-zinc-400">{player.sd}</td>
                                  <td className="text-center px-3 py-3 font-mono text-zinc-500">{player.p2pct}%</td>
                                  <td className="text-center px-3 py-3 font-mono text-zinc-500">{player.p3pct > 0 ? `${player.p3pct}%` : "—"}</td>
                                  <td className="text-center px-3 py-3 font-mono text-zinc-500">{player.tlpct}%</td>
                                  <td className="text-center px-3 py-3 font-mono font-bold text-cyan-400">{player.val}</td>
                                  <td className={`text-center px-3 py-3 font-mono font-bold ${player.plusMinus >= 0 ? "text-green-400" : "text-red-400"}`}>
                                    {player.plusMinus >= 0 ? `+${player.plusMinus}` : player.plusMinus}
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      </motion.div>
                    )}
                  </AnimatePresence>
                </div>
              );
            })}
          </div>
        </section>

      </div>
    </div>
  );
}
