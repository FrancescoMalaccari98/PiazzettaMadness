import React, { useState } from "react";
import { motion, AnimatePresence } from "motion/react";
import { Trophy, Medal, Flame, ChevronRight, X, Swords } from "lucide-react";

export function Stats() {
  const standings = [
    { rank: 1, team: "Saluta Andonio Spurs", wins: 7, losses: 0, pts: 480 },
    { rank: 2, team: "Minnesode Timbermilf", wins: 6, losses: 1, pts: 450 },
    { rank: 3, team: "Atlanta Robba", wins: 5, losses: 2, pts: 410 },
    { rank: 4, team: "Miami Spritz", wins: 4, losses: 3, pts: 380 },
    { rank: 5, team: "Boston Lopez", wins: 3, losses: 4, pts: 340 },
    { rank: 6, team: "Denver McNuggets", wins: 2, losses: 5, pts: 290 },
    { rank: 7, team: "Los Aiche Ride", wins: 1, losses: 6, pts: 250 },
    { rank: 8, team: "Philadelphia 70Sexers", wins: 0, losses: 7, pts: 190 },
  ];

  const topScorers = [
    { name: "Marco 'The Reaper' Rossi", team: "Saluta Andonio Spurs", ppg: 24.5 },
    { name: "Alessio Bianchi", team: "Minnesode Timbermilf", ppg: 22.1 },
    { name: "Leo Verdi", team: "Atlanta Robba", ppg: 19.8 },
  ];

  // Dummy rosters to show individual stats
  const rosters: Record<string, { name: string; pts: number; ast: number; reb: number; stl: number }[]> = {
    "Saluta Andonio Spurs": [
      { name: "Marco Rossi", pts: 24.5, ast: 5.1, reb: 4.2, stl: 1.5 },
      { name: "Giovanni Neri", pts: 15.2, ast: 3.0, reb: 8.5, stl: 0.8 },
      { name: "Luca Bianchi", pts: 8.5, ast: 6.2, reb: 2.1, stl: 2.0 },
      { name: "Matteo Verdi", pts: 6.0, ast: 1.5, reb: 10.2, stl: 0.2 },
      { name: "Andrea Gialli", pts: 4.5, ast: 2.0, reb: 3.0, stl: 1.1 },
    ],
    "Minnesode Timbermilf": [
      { name: "Alessio Bianchi", pts: 22.1, ast: 4.5, reb: 5.6, stl: 1.2 },
      { name: "Francesco Rossi", pts: 18.0, ast: 2.1, reb: 4.5, stl: 0.9 },
      { name: "Simone Neri", pts: 10.5, ast: 8.2, reb: 2.0, stl: 2.5 },
      { name: "Davide Verdi", pts: 7.2, ast: 1.1, reb: 9.0, stl: 0.5 },
      { name: "Lorenzo Gialli", pts: 2.0, ast: 0.5, reb: 2.1, stl: 0.1 },
    ],
    "Atlanta Robba": [
      { name: "Leo Verdi", pts: 19.8, ast: 6.5, reb: 3.2, stl: 2.1 },
      { name: "Marco Bianchi", pts: 16.5, ast: 2.0, reb: 5.5, stl: 1.0 },
      { name: "Giovanni Rossi", pts: 12.0, ast: 3.5, reb: 4.0, stl: 0.5 },
      { name: "Luca Neri", pts: 8.5, ast: 1.0, reb: 8.5, stl: 0.8 },
      { name: "Matteo Gialli", pts: 4.0, ast: 0.5, reb: 1.5, stl: 0.2 },
    ]
  };

  const [selectedTeam, setSelectedTeam] = useState<string | null>(null);

  const handleRowClick = (teamName: string) => {
    setSelectedTeam(selectedTeam === teamName ? null : teamName);
  };

  return (
    <div className="pt-32 pb-20 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <div className="text-center mb-16">
          <h1 className="font-display text-[80px] md:text-[120px] text-brand-orange uppercase leading-[0.8] mb-8 tracking-[-4px]">
            Statistiche
          </h1>
          <p className="text-xl font-sans text-zinc-400 max-w-2xl mx-auto">
            I numeri non mentono. La strada sa chi domina. Clicca su una squadra per vedere il roster completo.
          </p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          
          {/* Standings */}
          <div className="lg:col-span-2 space-y-6">
            <h2 className="font-display text-4xl uppercase flex items-center gap-3 border-t-4 border-brand-blue pt-4 pb-4">
              <Trophy className="text-brand-blue" /> Classifica
            </h2>
            <div className="overflow-hidden border border-zinc-800">
              <table className="w-full text-left border-collapse">
                <thead className="bg-zinc-900">
                  <tr className="border-b border-zinc-800 text-zinc-500 font-display text-xl uppercase tracking-wider">
                    <th className="p-4 w-16 text-center">#</th>
                    <th className="p-4">Squadra</th>
                    <th className="p-4 text-center">W</th>
                    <th className="p-4 text-center">L</th>
                    <th className="p-4 text-right">PTS</th>
                    <th className="p-4 w-10"></th>
                  </tr>
                </thead>
                <tbody>
                  {standings.map((team) => (
                    <React.Fragment key={team.team}>
                      <tr 
                        onClick={() => handleRowClick(team.team)}
                        className={`border-b border-zinc-800 hover:bg-zinc-800/50 transition-colors group cursor-pointer ${selectedTeam === team.team ? 'bg-zinc-800/50' : ''}`}
                      >
                        <td className="p-4 font-display text-2xl text-zinc-500 text-center group-hover:text-brand-orange transition-colors">
                          {team.rank}
                        </td>
                        <td className="p-4 font-sans font-bold text-lg">{team.team}</td>
                        <td className="p-4 text-center text-green-500 font-mono text-lg">{team.wins}</td>
                        <td className="p-4 text-center text-red-500 font-mono text-lg">{team.losses}</td>
                        <td className="p-4 text-right font-mono text-lg">{team.pts}</td>
                        <td className="p-4 text-center text-brand-orange">
                          <motion.div animate={{ rotate: selectedTeam === team.team ? 90 : 0 }}>
                            <ChevronRight size={24} />
                          </motion.div>
                        </td>
                      </tr>
                      {/* Roster Details */}
                      <AnimatePresence>
                        {selectedTeam === team.team && (
                          <motion.tr
                            initial={{ opacity: 0, height: 0 }}
                            animate={{ opacity: 1, height: "auto" }}
                            exit={{ opacity: 0, height: 0 }}
                            className="bg-brand-blue/10 border-b border-brand-blue/30"
                          >
                            <td colSpan={6} className="p-6">
                              <div className="flex justify-between items-center mb-4 border-b border-brand-blue/20 pb-2">
                                <h3 className="font-display uppercase text-2xl text-brand-orange tracking-wide">Roster - {team.team}</h3>
                                <button onClick={() => setSelectedTeam(null)} className="text-zinc-400 hover:text-white"><X size={20}/></button>
                              </div>
                              {rosters[team.team] ? (
                                <div className="grid gap-2">
                                  <div className="grid grid-cols-12 text-xs text-zinc-500 font-display uppercase tracking-widest pl-2 pr-2">
                                    <div className="col-span-6">Giocatore</div>
                                    <div className="col-span-1 text-center">PTS</div>
                                    <div className="col-span-1 text-center">AST</div>
                                    <div className="col-span-1 text-center">REB</div>
                                    <div className="col-span-1 text-center">STL</div>
                                  </div>
                                  {rosters[team.team].map(player => (
                                    <div key={player.name} className="grid grid-cols-12 items-center bg-zinc-900/80 p-3 flex-row border border-zinc-800/50 hover:border-brand-blue/50 transition-colors">
                                      <div className="col-span-6 font-bold font-sans">{player.name}</div>
                                      <div className="col-span-1 text-center font-mono text-brand-orange">{player.pts}</div>
                                      <div className="col-span-1 text-center font-mono text-zinc-300">{player.ast}</div>
                                      <div className="col-span-1 text-center font-mono text-zinc-300">{player.reb}</div>
                                      <div className="col-span-1 text-center font-mono text-zinc-300">{player.stl}</div>
                                    </div>
                                  ))}
                                </div>
                              ) : (
                                <p className="text-zinc-400 text-sm italic">Statistiche giocatori non ancora disponibili per questa squadra.</p>
                              )}
                            </td>
                          </motion.tr>
                        )}
                      </AnimatePresence>
                    </React.Fragment>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Highlights Sidebar */}
          <div className="space-y-10">
            <div>
              <h2 className="font-display text-4xl uppercase flex items-center gap-3 border-t-4 border-brand-orange pt-4 mb-6">
                <Flame className="text-brand-orange" /> Top Scorers
              </h2>
              <div className="space-y-4">
                {topScorers.map((player, idx) => (
                  <div key={player.name} className="bg-zinc-900 border border-zinc-800 p-4 flex items-center relative overflow-hidden group">
                    <div className="absolute top-0 right-0 h-full w-2 bg-brand-blue/30 group-hover:bg-brand-blue transition-colors"></div>
                    {idx === 0 && <div className="absolute top-0 right-0 h-full w-2 bg-brand-orange shadow-[0_0_15px_var(--color-brand-orange)]"></div>}
                    
                    <div className="w-10 font-display text-3xl text-zinc-500">{idx + 1}</div>
                    <div className="flex-1">
                      <div className="font-sans font-bold text-lg">{player.name}</div>
                      <div className="font-sans text-sm text-zinc-400">{player.team}</div>
                    </div>
                    <div className="font-display text-3xl text-brand-orange ml-4">
                      {player.ppg}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="bg-brand-yellow/10 border border-brand-yellow/30 p-6 relative overflow-hidden">
              <Medal className="absolute -top-4 -right-4 text-brand-yellow w-32 h-32 opacity-10" />
              <h3 className="font-display text-2xl text-brand-yellow uppercase mb-2 relative z-10">MVP Torneo</h3>
              <p className="font-sans font-bold text-2xl mb-1 relative z-10">Marco Rossi</p>
              <p className="font-sans text-brand-yellow/80 relative z-10">Saluta Andonio Spurs</p>
            </div>
          </div>

        </div>
      </motion.div>
    </div>
  );
}
