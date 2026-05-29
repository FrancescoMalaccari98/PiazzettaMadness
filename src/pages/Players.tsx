import { useState } from "react";
import { Link } from "react-router-dom";
import { motion } from "motion/react";
import { allPlayers, teams } from "../data/stats";

export function Players() {
  const [activeTeam, setActiveTeam] = useState<string>("all");

  const filtered = activeTeam === "all"
    ? allPlayers
    : allPlayers.filter(p => p.team === activeTeam);

  // Raggruppa per squadra se siamo in modalità "all"
  const grouped = activeTeam === "all"
    ? teams.map(team => ({ team, players: allPlayers.filter(p => p.team === team) }))
    : [{ team: activeTeam, players: filtered }];

  return (
    <div className="w-full min-h-screen bg-brand-bg pt-28 pb-24">

      {/* Header */}
      <div className="relative overflow-hidden border-b-[4px] border-zinc-800 mb-12">
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none overflow-hidden">
          <span className="font-display font-black text-[18vw] uppercase text-white/[0.025] whitespace-nowrap tracking-tighter leading-none">
            PLAYERS
          </span>
        </div>
        <motion.div
          initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}
          className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-12 text-center"
        >
          <p className="font-display text-brand-orange uppercase tracking-[0.3em] text-sm mb-3">Piazzetta Madness 2026</p>
          <h1 className="font-display text-[80px] md:text-[120px] uppercase leading-[0.8] tracking-[-4px] text-brand-orange mb-8">
            Players
          </h1>
          <p className="font-sans text-zinc-400 text-lg max-w-xl mx-auto">
            {allPlayers.length} atleti, 8 squadre. Clicca su un giocatore per vedere le sue statistiche.
          </p>
        </motion.div>
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">

        {/* Filtro squadra */}
        <div className="flex flex-wrap gap-2 mb-10">
          <button
            onClick={() => setActiveTeam("all")}
            className={`font-display text-xs uppercase tracking-widest px-4 py-2 border-2 transition-all bg-transparent
              ${activeTeam === "all" ? "border-brand-orange bg-brand-orange/10 text-brand-orange" : "border-zinc-700 text-zinc-500 hover:border-zinc-500 hover:text-zinc-300"}`}
          >
            Tutte
          </button>
          {teams.map(team => (
            <button
              key={team}
              onClick={() => setActiveTeam(team)}
              className={`font-display text-xs uppercase tracking-widest px-4 py-2 border-2 transition-all bg-transparent
                ${activeTeam === team ? "border-brand-orange bg-brand-orange/10 text-brand-orange" : "border-zinc-700 text-zinc-500 hover:border-zinc-500 hover:text-zinc-300"}`}
            >
              {team}
            </button>
          ))}
        </div>

        {/* Griglia giocatori */}
        <div className="space-y-14">
          {grouped.map(({ team, players }) => (
            <div key={team}>
              {/* Team header */}
              <div className="flex items-center gap-4 mb-6">
                <span className="w-2 h-6 bg-brand-orange shrink-0" />
                <h2 className="font-display text-xl md:text-2xl uppercase tracking-wide text-white">{team}</h2>
                <div className="flex-1 h-px bg-zinc-800" />
                <span className="font-display text-xs uppercase tracking-widest text-zinc-600">{players.length} giocatori</span>
              </div>

              <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
                {players.map(player => (
                  <Link
                    key={player.slug}
                    to={`/statistiche/${player.slug}`}
                    className="group border-[3px] border-zinc-800 bg-zinc-900 hover:border-brand-orange transition-all hover:-translate-y-1 hover:shadow-[6px_6px_0_var(--color-brand-orange)] overflow-hidden"
                  >
                    {/* Foto o placeholder */}
                    <div className="aspect-square bg-zinc-800 relative overflow-hidden">
                      {player.photo ? (
                        <img
                          src={player.photo}
                          alt={player.name}
                          className="w-full h-full object-cover grayscale group-hover:grayscale-0 group-hover:scale-105 transition-all duration-500"
                        />
                      ) : (
                        <div className="w-full h-full flex flex-col items-center justify-center bg-zinc-900 group-hover:bg-zinc-800 transition-colors">
                          <span className="font-display text-4xl md:text-5xl text-zinc-700 group-hover:text-zinc-500 transition-colors uppercase select-none">
                            {player.name.split(" ").map(n => n[0]).join("").slice(0, 2)}
                          </span>
                          {player.number && (
                            <span className="font-display text-xs text-zinc-700 group-hover:text-brand-orange transition-colors mt-1">
                              #{player.number}
                            </span>
                          )}
                        </div>
                      )}
                      {/* Badge PPG */}
                      <div className="absolute bottom-0 right-0 bg-brand-orange text-brand-bg px-2 py-0.5 font-mono text-xs font-bold opacity-0 group-hover:opacity-100 transition-opacity">
                        {player.pts} PPG
                      </div>
                    </div>

                    {/* Info */}
                    <div className="p-3">
                      <p className="font-sans font-bold text-sm uppercase text-white leading-tight group-hover:text-brand-orange transition-colors truncate">
                        {player.name}
                      </p>
                      <p className="font-sans text-xs text-zinc-500 truncate mt-0.5">{player.team}</p>
                    </div>
                  </Link>
                ))}
              </div>
            </div>
          ))}
        </div>

      </div>
    </div>
  );
}
