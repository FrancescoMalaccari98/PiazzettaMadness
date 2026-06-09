import React, { useState, useEffect } from 'react';
import { motion } from 'motion/react';
import { Lock, Plus, Minus, Play, Pause, RotateCcw, AlertTriangle } from 'lucide-react';

const API = import.meta.env.VITE_API_URL ?? "";

// ── Tipo risposta /api/squadre ───────────────────────────────
type TeamApi = { id: number; nome: string; girone: string };

export function Scoreboard() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");

  const handleLogin = (e: React.FormEvent) => {
    e.preventDefault();
    if (password === "madness26") {
      setIsAuthenticated(true);
    } else {
      setError("Password errata. Area riservata agli organizzatori.");
    }
  };

  if (!isAuthenticated) {
    return (
      <div className="pt-32 pb-20 max-w-xl mx-auto px-4 min-h-[80vh] flex flex-col justify-center">
        <label className="bg-zinc-900 border-[4px] border-zinc-800 p-8 shadow-[12px_12px_0_var(--color-brand-blue)] flex flex-col items-center">
          <Lock className="w-16 h-16 text-brand-orange mb-6" />
          <h2 className="font-display text-4xl uppercase text-white mb-2">Area Staff</h2>
          <p className="font-sans text-zinc-400 mb-8 text-center">Inserisci la password per accedere al tabellone segnapunti.</p>

          <form onSubmit={handleLogin} className="w-full flex flex-col gap-4">
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full bg-zinc-950 border border-zinc-700 text-white font-sans p-4 focus:outline-none focus:border-brand-orange text-center text-xl tracking-widest"
              placeholder="PASSWORD"
            />
            {error && <p className="text-red-500 text-sm font-sans text-center">{error}</p>}
            <button
              type="submit"
              className="w-full bg-brand-orange text-brand-bg font-display uppercase text-2xl py-4 hover:bg-white transition-colors"
            >
              Accedi
            </button>
          </form>
        </label>
      </div>
    );
  }

  return <LiveScoreboard />;
}

function LiveScoreboard() {
  // ── Squadre dal DB ──────────────────────────────────────────
  const [teamOptions, setTeamOptions] = useState<string[]>([]);

  useEffect(() => {
    fetch(`${API}/api-web/squadre`)
      .then(r => r.ok ? r.json() as Promise<TeamApi[]> : null)
      .then(data => {
        if (!data || data.length === 0) return;
        const names = data.map(t => t.nome);
        setTeamOptions(names);
        // Dopo aver ricevuto le squadre, valida le selezioni correnti.
        // Se il nome salvato non esiste più in DB, usa il primo disponibile.
        setTeam1(prev => names.includes(prev) ? prev : names[0] ?? "");
        setTeam2(prev => names.includes(prev) ? prev : names[1] ?? names[0] ?? "");
      })
      .catch(() => {});
  }, []);

  // ── Stato tabellone ─────────────────────────────────────────
  const [team1, setTeam1]   = useState("");
  const [team2, setTeam2]   = useState("");
  const [score1, setScore1] = useState(0);
  const [score2, setScore2] = useState(0);
  const [fouls1, setFouls1] = useState(0);
  const [fouls2, setFouls2] = useState(0);
  const [quarter, setQuarter] = useState(1);
  const [time, setTime]     = useState(600);
  const [isRunning, setIsRunning] = useState(false);
  const [isInitialized, setIsInitialized] = useState(false);

  // ── Restore da localStorage ──────────────────────────────────
  useEffect(() => {
    const saved = localStorage.getItem('madness_scoreboard');
    if (saved) {
      try {
        const p = JSON.parse(saved);
        if (p.team1) setTeam1(p.team1);
        if (p.team2) setTeam2(p.team2);
        setScore1(p.score1  ?? 0);
        setScore2(p.score2  ?? 0);
        setFouls1(p.fouls1  ?? 0);
        setFouls2(p.fouls2  ?? 0);
        setQuarter(p.quarter ?? 1);
        setTime(p.time !== undefined ? p.time : 600);
      } catch (_) {}
    }
    setIsInitialized(true);
  }, []);

  // ── Sync su localStorage + BroadcastChannel ─────────────────
  useEffect(() => {
    if (!isInitialized) return;
    const state = { team1, team2, score1, score2, fouls1, fouls2, quarter, time };
    localStorage.setItem('madness_scoreboard', JSON.stringify(state));
    try {
      const ch = new BroadcastChannel('scoreboard_sync');
      ch.postMessage({ type: 'SYNC_STATE', payload: state });
      ch.close();
    } catch (_) {}
  }, [team1, team2, score1, score2, fouls1, fouls2, quarter, time, isInitialized]);

  // Risponde alle richieste di sync dalla schermata Projection
  useEffect(() => {
    if (!isInitialized) return;
    try {
      const ch = new BroadcastChannel('scoreboard_sync');
      ch.onmessage = (event) => {
        if (event.data.type === 'REQUEST_SYNC') {
          const state = { team1, team2, score1, score2, fouls1, fouls2, quarter, time };
          ch.postMessage({ type: 'SYNC_STATE', payload: state });
        }
      };
      return () => ch.close();
    } catch (_) {}
  }, [team1, team2, score1, score2, fouls1, fouls2, quarter, time, isInitialized]);

  // ── Timer ────────────────────────────────────────────────────
  useEffect(() => {
    if (!isRunning || time <= 0) {
      if (time === 0) setIsRunning(false);
      return;
    }
    const id = setInterval(() => setTime(t => t - 1), 1000);
    return () => clearInterval(id);
  }, [isRunning, time]);

  const formatTime = (s: number) =>
    `${Math.floor(s / 60).toString().padStart(2, '0')}:${(s % 60).toString().padStart(2, '0')}`;

  // ── Punteggio con animazione ─────────────────────────────────
  const adjustScore = (team: 1 | 2, amount: number) => {
    if (amount > 0) {
      const ev = { team, points: amount, ts: Date.now() };
      localStorage.setItem('madness_score_event', JSON.stringify(ev));
      try {
        const ch = new BroadcastChannel('scoreboard_sync');
        ch.postMessage({ type: 'SCORE_ANIM', payload: ev });
        ch.close();
      } catch (_) {}
    }
    if (team === 1) setScore1(Math.max(0, score1 + amount));
    else            setScore2(Math.max(0, score2 + amount));
  };

  const adjustFouls = (team: 1 | 2, amount: number) => {
    if (team === 1) setFouls1(Math.max(0, fouls1 + amount));
    else            setFouls2(Math.max(0, fouls2 + amount));
  };

  // Selettore squadra — mostra le opzioni DB; se ancora in caricamento
  // mantiene il valore corrente come unica opzione.
  const renderTeamSelect = (
    value: string,
    onChange: (v: string) => void,
    borderColor: string
  ) => {
    const options = teamOptions.length > 0 ? teamOptions : (value ? [value] : []);
    return (
      <select
        value={value}
        onChange={e => onChange(e.target.value)}
        className={`w-full bg-zinc-900 border-2 ${borderColor} text-white font-sans text-xl md:text-2xl p-4 uppercase font-bold focus:outline-none`}
      >
        {options.length === 0 && (
          <option value="">Caricamento squadre…</option>
        )}
        {options.map(t => <option key={t} value={t}>{t}</option>)}
      </select>
    );
  };

  return (
    <div className="pt-24 pb-12 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 min-h-screen flex flex-col">
      <div className="flex justify-between items-center mb-8 border-b-2 border-zinc-800 pb-4 flex-wrap gap-4">
        <h1 className="font-display text-2xl sm:text-4xl text-white uppercase flex items-center gap-3">
          <AlertTriangle className="text-brand-orange shrink-0" /> Tabellone Live Control
        </h1>
        <div className="flex items-center gap-3 sm:gap-6 flex-wrap">
          <button
            onClick={() => window.open('/projection', 'ScoreboardProjection', 'width=1280,height=720')}
            className="bg-zinc-800 text-white px-4 py-2 hover:bg-zinc-700 font-display uppercase tracking-widest text-sm flex items-center gap-2 border-[2px] border-zinc-600 hover:border-brand-blue transition-colors shadow-[4px_4px_0_var(--color-brand-blue)] hover:shadow-none hover:translate-x-1 hover:translate-y-1"
          >
            Apri Schermo Proiezione
          </button>
          <div className="text-brand-orange font-mono uppercase tracking-widest text-sm animate-pulse flex items-center gap-2">
            <div className="w-3 h-3 bg-brand-orange rounded-full" /> Controlli Staff
          </div>
        </div>
      </div>

      <div className="flex-1 bg-zinc-950 border-[6px] border-zinc-800 p-4 md:p-8 flex flex-col lg:flex-row gap-8 shadow-[16px_16px_0_var(--color-brand-blue)]">

        {/* TEAM 1 */}
        <div className="flex-1 flex flex-col gap-6">
          {renderTeamSelect(team1, setTeam1, 'border-brand-blue focus:border-brand-blue')}

          <div className="bg-black border-4 border-zinc-800 p-4 sm:p-8 pb-20 flex flex-col items-center flex-1 justify-center rounded-sm relative">
            <div className="text-[88px] sm:text-[120px] md:text-[180px] leading-none font-mono text-brand-orange font-black">
              {score1}
            </div>
            <div className="flex gap-1.5 sm:gap-2 absolute bottom-4 left-1/2 -translate-x-1/2">
              <button onClick={() => adjustScore(1, -1)} className="bg-zinc-800 p-2.5 sm:p-3 hover:bg-zinc-700 active:scale-95"><Minus className="w-5 h-5 sm:w-6 sm:h-6 text-white" /></button>
              <button onClick={() => adjustScore(1, 1)} className="bg-brand-blue text-brand-bg font-black px-4 sm:px-6 hover:bg-white hover:text-black active:scale-95 text-lg sm:text-xl">+1</button>
              <button onClick={() => adjustScore(1, 2)} className="bg-brand-blue text-brand-bg font-black px-4 sm:px-6 hover:bg-white hover:text-black active:scale-95 text-lg sm:text-xl">+2</button>
              <button onClick={() => adjustScore(1, 3)} className="bg-brand-blue text-brand-bg font-black px-4 sm:px-6 hover:bg-white hover:text-black active:scale-95 text-lg sm:text-xl">+3</button>
            </div>
          </div>

          <div className="flex justify-between items-center bg-zinc-900 border-2 border-zinc-700 p-4">
            <span className="font-display uppercase text-zinc-400 text-2xl">Falli</span>
            <div className="flex items-center gap-4">
              <button onClick={() => adjustFouls(1, -1)} className="text-zinc-500 hover:text-white p-2"><Minus className="w-6 h-6" /></button>
              <span className={`font-mono text-4xl ${fouls1 >= 5 ? 'text-red-500 animate-pulse' : 'text-zinc-300'}`}>{fouls1}</span>
              <button onClick={() => adjustFouls(1, 1)} className="text-zinc-500 hover:text-white p-2"><Plus className="w-6 h-6" /></button>
            </div>
          </div>
        </div>

        {/* MIDDLE: TEMPO & PERIODO */}
        <div className="w-full lg:w-1/4 flex flex-col gap-6 order-first lg:order-none">
          <div className="bg-zinc-900 border-4 border-zinc-700 p-6 flex flex-col items-center">
            <h3 className="font-display uppercase text-zinc-500 text-xl mb-4">Periodo</h3>
            <div className="flex items-center gap-6 mb-2">
              <button onClick={() => setQuarter(Math.max(1, quarter - 1))} className="text-zinc-500 hover:text-white"><Minus /></button>
              <span className="font-mono text-5xl font-bold text-white">{quarter}</span>
              <button onClick={() => setQuarter(Math.min(4, quarter + 1))} className="text-zinc-500 hover:text-white"><Plus /></button>
            </div>
          </div>

          <div className="bg-black border-4 border-zinc-800 p-6 flex flex-col items-center flex-1">
            <h3 className="font-display uppercase text-zinc-500 text-xl mb-4">Tempo</h3>
            <div className={`font-mono text-6xl md:text-7xl mb-8 ${time < 60 ? 'text-red-500' : 'text-brand-yellow'}`}>
              {formatTime(time)}
            </div>
            <div className="flex gap-4 mb-4">
              <button
                onClick={() => setIsRunning(r => !r)}
                className={`p-4 rounded-full ${isRunning ? 'bg-zinc-800 text-zinc-400' : 'bg-green-600 text-white'} hover:scale-105 transition-transform`}
              >
                {isRunning ? <Pause className="w-8 h-8" /> : <Play className="w-8 h-8 ml-1" />}
              </button>
              <button
                onClick={() => { setIsRunning(false); setTime(600); }}
                className="p-4 rounded-full bg-zinc-800 text-white hover:bg-zinc-700 hover:rotate-180 transition-all duration-300"
              >
                <RotateCcw className="w-8 h-8" />
              </button>
            </div>
            <div className="grid grid-cols-2 gap-2 w-full mt-4">
              <button onClick={() => setTime(t => t + 60)}          className="bg-zinc-900 border border-zinc-700 text-xs font-sans uppercase p-2 hover:bg-zinc-800">+1 Min</button>
              <button onClick={() => setTime(t => Math.max(0, t-60))} className="bg-zinc-900 border border-zinc-700 text-xs font-sans uppercase p-2 hover:bg-zinc-800">-1 Min</button>
              <button onClick={() => setTime(600)}                   className="bg-zinc-900 border border-zinc-700 text-xs font-sans uppercase p-2 hover:bg-zinc-800">10:00</button>
              <button onClick={() => setTime(14)}                    className="bg-zinc-900 border border-zinc-700 text-xs font-sans uppercase p-2 hover:bg-zinc-800 hover:text-red-500">14 sec</button>
            </div>
          </div>

          <button
            onClick={() => { setScore1(0); setScore2(0); setFouls1(0); setFouls2(0); setQuarter(1); setTime(600); setIsRunning(false); }}
            className="bg-brand-orange text-brand-bg font-display uppercase py-4 border-2 border-transparent hover:bg-brand-bg hover:text-brand-orange hover:border-brand-orange transition-colors"
          >
            Reset Match Completo
          </button>
        </div>

        {/* TEAM 2 */}
        <div className="flex-1 flex flex-col gap-6">
          {renderTeamSelect(team2, setTeam2, 'border-zinc-700 focus:border-zinc-400')}

          <div className="bg-black border-4 border-zinc-800 p-4 sm:p-8 pb-20 flex flex-col items-center flex-1 justify-center rounded-sm relative">
            <div className="text-[88px] sm:text-[120px] md:text-[180px] leading-none font-mono text-white font-black">
              {score2}
            </div>
            <div className="flex gap-1.5 sm:gap-2 absolute bottom-4 left-1/2 -translate-x-1/2">
              <button onClick={() => adjustScore(2, -1)} className="bg-zinc-800 p-2.5 sm:p-3 hover:bg-zinc-700 active:scale-95"><Minus className="w-5 h-5 sm:w-6 sm:h-6 text-white" /></button>
              <button onClick={() => adjustScore(2, 1)} className="bg-zinc-300 text-black font-black px-4 sm:px-6 hover:bg-white hover:text-black active:scale-95 text-lg sm:text-xl">+1</button>
              <button onClick={() => adjustScore(2, 2)} className="bg-zinc-300 text-black font-black px-4 sm:px-6 hover:bg-white hover:text-black active:scale-95 text-lg sm:text-xl">+2</button>
              <button onClick={() => adjustScore(2, 3)} className="bg-zinc-300 text-black font-black px-4 sm:px-6 hover:bg-white hover:text-black active:scale-95 text-lg sm:text-xl">+3</button>
            </div>
          </div>

          <div className="flex justify-between items-center bg-zinc-900 border-2 border-zinc-700 p-4">
            <span className="font-display uppercase text-zinc-400 text-2xl">Falli</span>
            <div className="flex items-center gap-4">
              <button onClick={() => adjustFouls(2, -1)} className="text-zinc-500 hover:text-white p-2"><Minus className="w-6 h-6" /></button>
              <span className={`font-mono text-4xl ${fouls2 >= 5 ? 'text-red-500 animate-pulse' : 'text-zinc-300'}`}>{fouls2}</span>
              <button onClick={() => adjustFouls(2, 1)} className="text-zinc-500 hover:text-white p-2"><Plus className="w-6 h-6" /></button>
            </div>
          </div>
        </div>

      </div>
    </div>
  );
}
