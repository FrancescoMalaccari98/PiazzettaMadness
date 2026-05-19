import React, { useState } from "react";
import { motion, AnimatePresence } from "motion/react";
import { Trophy, Flame, ChevronRight, X, Swords, Calendar } from "lucide-react";

export function Matches() {
  const [selectedMatch, setSelectedMatch] = useState<any>(null);

  const groupMatches = [
    {
      id: "g1",
      round: "Girone Unico - Giornata 1",
      date: "05 Ago, 18:00",
      status: "COMPLETA",
      team1: { name: "Saluta Andonio Spurs", score: 71 },
      team2: { name: "Boston Lopez", score: 60 },
      details: {
        mvp: "Marco Rossi (22 pts)",
        summary: "Partenza solida per i Spurs, Boston Lopez rimangono in gara fino al terzo quarto prima del break decisivo."
      }
    },
    {
      id: "g2",
      round: "Girone Unico - Giornata 1",
      date: "05 Ago, 19:30",
      status: "COMPLETA",
      team1: { name: "Minnesode Timbermilf", score: 85 },
      team2: { name: "Denver McNuggets", score: 55 },
      details: {
        mvp: "Simone Neri (15 pts, 12 ast)",
        summary: "Dominio totale di Minnesode in transizione, McNuggets travolti dai contropiedi."
      }
    },
    {
      id: "g3",
      round: "Girone Unico - Giornata 2",
      date: "07 Ago, 20:00",
      status: "COMPLETA",
      team1: { name: "Miami Spritz", score: 78 },
      team2: { name: "Atlanta Robba", score: 81 },
      details: {
        mvp: "Leo Verdi (24 pts, game winner)",
        summary: "Sfida all'ultimo respiro. Leo Verdi segna in penetrazione con fallo a 4 secondi dalla fine, ribaltando il match."
      }
    },
    {
      id: "g4",
      round: "Girone Unico - Giornata 3",
      date: "09 Ago, 18:30",
      status: "COMPLETA",
      team1: { name: "Saluta Andonio Spurs", score: 90 },
      team2: { name: "Minnesode Timbermilf", score: 88 },
      details: {
        mvp: "Luca Bianchi (18 pts, 10 reb)",
        summary: "Anticipo di finale pazzesco. Spurs vincono allo scadere dell'overtime in uno scontro fisico pesantissimo e spettacolare."
      }
    }
  ];

  const bracketMatches = {
    semis: [
      {
        id: "sf1",
        round: "Semifinale 1",
        date: "12 Ago, 18:00",
        status: "COMPLETA",
        team1: { name: "Saluta Andonio Spurs", score: 82 },
        team2: { name: "Miami Spritz", score: 74 },
        details: {
          mvp: "Marco Rossi (28 pts, 8 reb)",
          summary: "Andonio Spurs dominano l'ultimo quarto con un parziale di 15-4 che ha spezzato le gambe ai Miami Spritz. Impressionante tenuta fisica."
        }
      },
      {
        id: "sf2",
        round: "Semifinale 2",
        date: "12 Ago, 20:30",
        status: "COMPLETA",
        team1: { name: "Minnesode Timbermilf", score: 89 },
        team2: { name: "Atlanta Robba", score: 88 },
        details: {
          mvp: "Alessio Bianchi (32 pts, game winner)",
          summary: "Il match più bello del torneo. Buzzer beater pazzesco da 8 metri di Bianchi allo scadere che manda in delirio la folla."
        }
      }
    ],
    final: {
      id: "final",
      round: "Finale",
      date: "14 Ago, 21:00",
      status: "LIVE",
      team1: { name: "Saluta Andonio Spurs", score: 45 },
      team2: { name: "Minnesode Timbermilf", score: 42 },
      details: {
        mvp: "TBD",
        summary: "Partita accesissima all'intervallo. Rossi e Bianchi si stanno rispondendo colpo su colpo. Nessuna esclusione di colpi in area."
      }
    }
  };

  const MatchCard = ({ match, isFinal = false, onClick }: { match: any, isFinal?: boolean, onClick?: () => void, key?: any }) => {
    const isLive = match.status === "LIVE";
    const borderColor = isFinal ? "border-brand-yellow" : (isLive ? "border-brand-orange" : "border-zinc-800");
    const hoverColor = isFinal ? "hover:border-brand-yellow" : "hover:border-white";
    
    return (
      <div 
        onClick={onClick}
        className={`border-[3px] ${borderColor} bg-zinc-950 cursor-pointer ${hoverColor} transition-colors group relative shadow-[6px_6px_0_rgba(0,0,0,0.4)] hover:-translate-y-1 hover:translate-x-1 flex flex-col h-full`}
      >
        {isLive && (
          <div className="absolute -top-3 -right-3 bg-brand-orange text-brand-bg font-display px-2 py-0.5 text-sm uppercase rotate-3 z-10 animate-pulse">
            LIVE
          </div>
        )}
        <div className="flex justify-between items-center px-4 py-2 border-b border-zinc-800 bg-zinc-900 border-[1px]">
          <span className="text-xs font-display text-zinc-400 uppercase tracking-widest">{match.round}</span>
          <span className="text-xs font-sans text-zinc-500">{match.date}</span>
        </div>
        <div className="p-4 flex flex-col justify-center gap-3 flex-grow">
          <div className={`flex justify-between items-center ${match.team1.score >= match.team2.score ? 'text-white' : 'text-zinc-500'}`}>
            <span className="font-sans font-[900] tracking-tight text-lg uppercase truncate max-w-[140px]">{match.team1.name}</span>
            <span className="font-mono text-2xl font-bold">{match.team1.score}</span>
          </div>
          <div className={`flex justify-between items-center ${match.team2.score >= match.team1.score ? 'text-white' : 'text-zinc-500'}`}>
            <span className="font-sans font-[900] tracking-tight text-lg uppercase truncate max-w-[140px]">{match.team2.name}</span>
            <span className="font-mono text-2xl font-bold">{match.team2.score}</span>
          </div>
        </div>
      </div>
    );
  };

  return (
    <div className="pt-32 pb-20 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <div className="text-center mb-16">
          <h1 className="font-display text-[80px] md:text-[120px] text-brand-orange uppercase leading-[0.8] mb-8 tracking-[-4px]">
            Match
          </h1>
          <p className="text-xl font-sans text-zinc-400 max-w-2xl mx-auto">
            Dal girone all'italiana fino alla pazzesca finale dei playoff. Ripercorri ogni canestro.
          </p>
        </div>

        {/* Girone all'italiana */}
        <div className="mb-24">
          <h2 className="font-display text-4xl md:text-5xl uppercase flex items-center gap-4 border-t-[6px] border-zinc-800 pt-6 pb-12 text-white">
            <Calendar className="w-10 h-10 text-brand-blue" /> Fase a Gironi
          </h2>
          
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            {groupMatches.map(match => (
              <MatchCard key={match.id} match={match} onClick={() => setSelectedMatch(match)} />
            ))}
          </div>
        </div>

        {/* Tournament Bracket Section */}
        <div>
          <h2 className="font-display text-4xl md:text-5xl uppercase flex items-center gap-4 border-t-[6px] border-zinc-800 pt-6 pb-12 text-white">
            <Swords className="w-10 h-10 text-brand-orange" /> Playoff Bracket
          </h2>
          
          <div className="overflow-x-auto pb-12 cursor-grab active:cursor-grabbing hide-scrollbar">
            <div className="min-w-[900px] flex bg-zinc-900/40 p-8 md:p-12 border-[4px] border-zinc-800 relative shadow-inner">
              {/* Column 1: Semis */}
              <div className="flex flex-col justify-around w-1/3 pr-8 gap-16 relative z-10">
                {bracketMatches.semis.map((match) => (
                  <div key={match.id} className="relative">
                     <MatchCard match={match} onClick={() => setSelectedMatch(match)} />
                     {/* Connector Out */}
                     <div className="absolute top-1/2 -right-8 w-8 h-[3px] bg-zinc-700"></div>
                  </div>
                ))}
              </div>

              {/* Column 2: Connector Vertical */}
              <div className="w-0 relative z-0">
                {/* Height is roughly the distance between the two middle points of semis */}
                <div className="absolute top-[25%] bottom-[25%] left-0 w-[3px] bg-zinc-700"></div>
                {/* Horizontal line to final */}
                <div className="absolute top-1/2 left-0 w-8 h-[3px] bg-brand-orange"></div>
              </div>

              {/* Column 3: Final */}
              <div className="flex flex-col justify-center w-1/3 px-8 relative z-10">
                <div className="relative">
                  <MatchCard match={bracketMatches.final} isFinal onClick={() => setSelectedMatch(bracketMatches.final)} />
                  <div className="absolute top-1/2 -right-8 w-8 h-[3px] bg-brand-yellow/50"></div>
                </div>
              </div>

              {/* Column 4: Champion */}
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

      {/* Match Details Modal */}
      <AnimatePresence>
        {selectedMatch && (
          <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
            <motion.div 
              initial={{ opacity: 0 }} 
              animate={{ opacity: 1 }} 
              exit={{ opacity: 0 }}
              onClick={() => setSelectedMatch(null)}
              className="absolute inset-0 bg-black/90 backdrop-blur-sm cursor-pointer"
            ></motion.div>
            <motion.div
              initial={{ opacity: 0, scale: 0.95, y: 20 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.95, y: 20 }}
              className="relative w-full max-w-2xl bg-zinc-950 border-[4px] border-brand-orange shadow-[16px_16px_0_var(--color-brand-blue)] z-10 flex flex-col max-h-[90vh]"
            >
              <div className="flex justify-between items-center p-6 border-b-[3px] border-zinc-800 bg-zinc-900">
                <h3 className="font-display text-3xl uppercase text-white tracking-wide">Dettagli - {selectedMatch.round}</h3>
                <button onClick={() => setSelectedMatch(null)} className="text-zinc-400 hover:text-white transition-colors border-2 border-transparent hover:border-brand-orange p-1 rounded-sm">
                  <X size={32} />
                </button>
              </div>
              
              <div className="p-6 md:p-10 overflow-y-auto hide-scrollbar">
                 <div className="flex justify-center items-center gap-4 md:gap-8 mb-10">
                    <div className="text-center flex-1">
                      <div className="font-sans font-black text-xl md:text-3xl uppercase mb-2 text-zinc-300">{selectedMatch.team1.name}</div>
                      <div className={`font-mono text-6xl md:text-8xl ${selectedMatch.team1.score >= selectedMatch.team2.score ? 'text-brand-orange' : 'text-zinc-500'}`}>{selectedMatch.team1.score}</div>
                    </div>
                    <div className="font-display text-3xl md:text-5xl text-zinc-700">VS</div>
                    <div className="text-center flex-1">
                      <div className="font-sans font-black text-xl md:text-3xl uppercase mb-2 text-zinc-300">{selectedMatch.team2.name}</div>
                      <div className={`font-mono text-6xl md:text-8xl ${selectedMatch.team2.score >= selectedMatch.team1.score ? 'text-white' : 'text-zinc-500'}`}>{selectedMatch.team2.score}</div>
                    </div>
                 </div>
                 
                 <div className="bg-zinc-900 border-[3px] border-zinc-700 p-6 mb-6">
                   <h4 className="font-display uppercase text-brand-yellow mb-3 text-2xl">Match Recap</h4>
                   <p className="font-sans text-zinc-300 text-lg md:text-xl leading-relaxed">{selectedMatch.details.summary}</p>
                 </div>
                 
                 <div className="bg-brand-blue/10 border-[3px] border-brand-blue p-6 flex flex-col sm:flex-row gap-6 items-start sm:items-center">
                   <div className="bg-brand-blue p-3 shrink-0 hidden sm:block">
                     <Flame className="w-10 h-10 text-brand-bg md:w-12 md:h-12" />
                   </div>
                   <div>
                     <h4 className="font-display uppercase text-brand-blue mb-1 text-2xl">MVP / Key Player</h4>
                     <p className="font-sans text-white text-xl md:text-2xl font-bold">{selectedMatch.details.mvp}</p>
                   </div>
                 </div>
              </div>
            </motion.div>
          </div>
        )}
      </AnimatePresence>
    </div>
  );
}
