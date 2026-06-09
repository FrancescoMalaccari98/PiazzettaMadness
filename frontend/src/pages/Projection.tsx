import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence, type TargetAndTransition } from 'motion/react';
import { Flame } from 'lucide-react';

export function Projection() {
  const [gameState, setGameState] = useState({
    team1: "Squadra 1",
    team2: "Squadra 2",
    score1: 0,
    score2: 0,
    fouls1: 0,
    fouls2: 0,
    quarter: 1,
    time: 600
  });

  const [animEvent, setAnimEvent] = useState<{team: number, points: number, ts: number} | null>(null);

  useEffect(() => {
    // 1. Initial load from LocalStorage
    const stored = localStorage.getItem('madness_scoreboard');
    if (stored) {
      try {
        setGameState(JSON.parse(stored));
      } catch (e) {}
    }

    let channel: BroadcastChannel | null = null;
    try {
      // 2. Fast cross-tab communication via BroadcastChannel
      channel = new BroadcastChannel('scoreboard_sync');
      channel.onmessage = (event) => {
        if (event.data.type === 'SYNC_STATE') {
          setGameState(event.data.payload);
        } else if (event.data.type === 'SCORE_ANIM') {
          setAnimEvent(event.data.payload);
          setTimeout(() => setAnimEvent(null), 2500);
        }
      };
      // Ask control panel for fresh data
      channel.postMessage({ type: 'REQUEST_SYNC' });
    } catch (e) { }

    // 3. Fallback: LocalStorage event listener
    const handleStorage = (e: StorageEvent) => {
      if (e.key === 'madness_scoreboard' && e.newValue) {
        try {
          setGameState(JSON.parse(e.newValue));
        } catch (err) {}
      }
      if (e.key === 'madness_score_event' && e.newValue) {
        try {
          const data = JSON.parse(e.newValue);
          setAnimEvent(data);
          setTimeout(() => setAnimEvent(null), 2500);
        } catch (err) {}
      }
    };
    window.addEventListener('storage', handleStorage);

    // 4. BRUTEFORCE FALLBACK: Polling LocalStorage (Works everywhere as a catch-all)
    const pollInterval = setInterval(() => {
      const currentStored = localStorage.getItem('madness_scoreboard');
      if (currentStored) {
        try {
          const newState = JSON.parse(currentStored);
          // Check if it's different to avoid unnecessary state updates
          setGameState(prev => {
            if (JSON.stringify(prev) !== currentStored) {
              return newState;
            }
            return prev;
          });
        } catch(err) {}
      }

      // Also check for score events if they happened in the last 500ms
      const eventStored = localStorage.getItem('madness_score_event');
      if (eventStored) {
        try {
          const ev = JSON.parse(eventStored);
          if (ev.ts > Date.now() - 500) {
             setAnimEvent(prev => {
               if (!prev || prev.ts !== ev.ts) {
                 setTimeout(() => setAnimEvent(null), 2500);
                 return ev;
               }
               return prev;
             });
          }
        } catch(err) {}
      }
    }, 200); // Poll every 200ms

    return () => {
      if (channel) channel.close();
      window.removeEventListener('storage', handleStorage);
      clearInterval(pollInterval);
    };
  }, []);

  const formatTime = (seconds: number) => {
    const m = Math.floor(seconds / 60).toString().padStart(2, '0');
    const s = (seconds % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
  };

  return (
    <div className="w-full h-screen bg-black text-white flex flex-col font-sans overflow-hidden">
      {/* Top Bar: Time and Period */}
      <div className="flex justify-between items-center bg-zinc-950 border-b-[8px] border-zinc-900 px-16 py-6 h-[20vh] relative z-20">
        <div className="flex flex-col items-center">
          <span className="font-display font-black text-zinc-500 uppercase text-4xl mb-2 tracking-widest">Periodo</span>
          <span className="font-mono text-7xl font-bold">{gameState.quarter}</span>
        </div>
        
        <div className="absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 flex flex-col items-center">
          <span className={`font-mono font-black text-[10vw] leading-none tracking-tighter ${gameState.time < 60 ? 'text-red-500 animate-pulse' : 'text-brand-yellow'}`}>
            {formatTime(gameState.time)}
          </span>
        </div>

        <div className="flex flex-col items-center justify-center opacity-30 mix-blend-screen w-[200px]">
          <div className="font-display text-4xl uppercase leading-[0.8] border-l-8 border-brand-orange pl-4 text-white">
            PIAZZETTA<br/>
            MADNESS
          </div>
        </div>
      </div>

      {/* Main Score Area */}
      <div className="flex-1 flex w-full relative z-10 bg-black">
        <TeamScore 
          side={1} 
          teamName={gameState.team1} 
          score={gameState.score1} 
          fouls={gameState.fouls1} 
          animEvent={animEvent} 
        />
        <div className="w-[8px] bg-zinc-900 flex-shrink-0 z-20"></div>
        <TeamScore 
          side={2} 
          teamName={gameState.team2} 
          score={gameState.score2} 
          fouls={gameState.fouls2} 
          animEvent={animEvent} 
        />
      </div>
    </div>
  );
}

function TeamScore({ side, teamName, score, fouls, animEvent }: { side: 1 | 2, teamName: string, score: number, fouls: number, animEvent: any }) {
  
  const isAnim = animEvent?.team === side;
  const points = isAnim ? animEvent.points : 0;
  
  // Create different animation properties based on the points scored
  const getAnimProps = (): TargetAndTransition => {
    if (!isAnim) return { scale: 1, rotate: 0, color: side === 1 ? "#ea6324" : "#ffffff", textShadow: "none" };
    
    if (points === 1) {
      return { 
        scale: [1, 1.2, 1.1, 1], 
        color: ["#ffffff", "#f2a71e", side === 1 ? "#ea6324" : "#ffffff"],
        transition: { duration: 0.5 }
      };
    }
    if (points === 2) {
      return { 
        scale: [1, 1.4, 1.1, 1], 
        y: [0, -30, 0],
        rotate: [0, -2, 2, 0],
        color: ["#ffffff", "#ea6324", side === 1 ? "#ea6324" : "#ffffff"],
        transition: { duration: 0.8, type: "spring", bounce: 0.5 }
      };
    }
    if (points === 3) {
      return { 
        scale: [1, 1.8, 1.3, 1], 
        rotate: [0, -5, 5, -5, 5, 0], 
        color: ["#ffffff", "#ff0000", "#f2a71e", side === 1 ? "#ea6324" : "#ffffff"],
        textShadow: ["0px 0px 0px transparent", "0px 0px 80px rgba(234, 99, 36, 1)", "0px 0px 0px transparent"],
        transition: { duration: 1.5, type: "spring", bounce: 0.7 } 
      };
    }
    return { scale: 1 };
  };

  return (
    <div className={`flex-1 flex flex-col items-center justify-between p-12 relative overflow-hidden`}>
      {/* Team Name */}
      <div className="w-full text-center mt-8">
        <h2 
          className="font-display font-black uppercase text-zinc-300 leading-none tracking-tighter"
          style={{ fontSize: teamName.length > 15 ? '5vw' : '7vw' }}
        >
          {teamName}
        </h2>
      </div>
      
      {/* Massive Score Number */}
      <div className="relative flex-1 flex items-center justify-center w-full">
        <motion.div 
          animate={getAnimProps()} 
          className={`leading-none font-mono font-black ${side === 1 ? 'text-brand-orange' : 'text-white'} z-20 relative`}
          style={{ fontSize: '28vw', textShadow: side === 1 ? 'none' : '0px 0px 40px rgba(255,255,255,0.1)' }}
        >
          {score}
        </motion.div>

        {/* Extreme +3 Animation Overlays */}
        <AnimatePresence>
          {isAnim && points === 3 && (
            <>
              <motion.div 
                initial={{ opacity: 0, scale: 0, y: 100 }}
                animate={{ opacity: 0.6, scale: 1, y: -50 }}
                exit={{ opacity: 0, scale: 2 }}
                transition={{ duration: 1.5, ease: "easeOut" }}
                className="absolute inset-0 flex items-center justify-center text-brand-orange pointer-events-none z-10"
              >
                <Flame style={{ width: '40vw', height: '40vw' }} className="animate-pulse" />
              </motion.div>
              <motion.div 
                initial={{ opacity: 0, scale: 4, y: -200 }}
                animate={{ opacity: [0, 1, 0], scale: [4, 1, 1], y: [-200, 0, 0] }}
                transition={{ duration: 1.5, ease: "easeOut" }}
                className="absolute font-display font-black text-brand-yellow z-30 opacity-0 pointer-events-none text-[8vw] drop-shadow-[0_0_20px_#ea6324] rotate-12 right-1/4 top-1/4 uppercase"
              >
                BOOM!
              </motion.div>
            </>
          )}
        </AnimatePresence>
      </div>

      {/* Fouls */}
      <div className="w-full flex flex-col items-center mb-8">
        <span className="font-display uppercase text-zinc-600 text-3xl mb-4 tracking-widest">Falli di Squadra</span>
        <div className="flex gap-4">
          {[1,2,3,4,5].map(f => (
            <div 
              key={f} 
              className={`w-12 h-12 rounded-full border-4 ${
                fouls >= f 
                  ? (fouls >= 5 ? 'bg-red-500 border-red-500 animate-pulse shadow-[0_0_20px_rgba(239,68,68,0.8)]' : 'bg-brand-orange border-brand-orange shadow-[0_0_10px_rgba(234,99,36,0.5)]') 
                  : 'bg-transparent border-zinc-800'
              }`}
            ></div>
          ))}
        </div>
      </div>
    </div>
  )
}
