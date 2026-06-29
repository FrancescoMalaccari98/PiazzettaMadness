import { useState, useEffect, useRef } from "react";
import { Link } from "react-router-dom";
import { motion } from "motion/react";

function playerSlug(fullName: string, jerseyNumber: number | string | null = null): string {
  const base = fullName
    .toLowerCase()
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/\s+/g, "-")
    .replace(/[^a-z0-9-]/g, "");
  return jerseyNumber !== null ? `${base}-${jerseyNumber}` : base;
}

const API = import.meta.env.VITE_API_URL ?? "";

// ── Tipi risposta /api-web/live ──────────────────────────────
type LivePlayer = {
  player_id: number;
  name: string;
  jersey_number: number | string | null;
  points: number;
  fouls: number;
};

type LiveTeam = {
  id: number;
  name: string;
  short_name: string | null;
  score: number;
  players: LivePlayer[];
};

type LiveData = {
  has_live: boolean;
  source: "live" | "ocr" | "none";
  status: "Live" | "Paused" | "Finished" | "none";
  period?: number;
  clock?: string;
  clock_seconds?: number;
  clock_running?: boolean;
  home_score?: number;
  away_score?: number;
  home_team?: LiveTeam;
  away_team?: LiveTeam;
  message?: string;
};

// Polling: 5s quando c'è una diretta, 15s quando non c'è nessuna
// partita live (per accorgersi quando ne inizia una senza martellare il server).
const POLL_LIVE_MS = 5000;
const POLL_IDLE_MS = 15000;

const FOUL_LIMIT = 5; // personal_foul_limit di default nel db

const PERIOD_LABELS: Record<number, string> = {
  1: "Tempo 1",
  2: "Tempo 2",
  3: "Overtime",
  4: "Intervallo",
  5: "Riscaldamento",
};

// Classi statiche per accento squadra (Tailwind non rileva classi dinamiche).
const ACCENTS = {
  blue:   { bar: "bg-brand-blue",   text: "text-brand-blue",   border: "border-brand-blue" },
  orange: { bar: "bg-brand-orange", text: "text-brand-orange", border: "border-brand-orange" },
} as const;
type Accent = keyof typeof ACCENTS;

function formatClock(totalSec: number): string {
  const s = Math.max(0, Math.floor(totalSec));
  const m = Math.floor(s / 60);
  const sec = s % 60;
  return `${m}:${sec.toString().padStart(2, "0")}`;
}

export function Live() {
  const [data, setData] = useState<LiveData | null>(null);
  const [loading, setLoading] = useState(true);
  const [clockSeconds, setClockSeconds] = useState(0);
  const [clockRunning, setClockRunning] = useState(false);
  const [clockPeriod, setClockPeriod] = useState(1);
  const lastServerClock = useRef(-1);

  // Polling ogni 5s — aggiorna tutto (punteggio, giocatori, falli).
  // Per il clock: aggiorna solo se il server manda un valore diverso dal precedente.
  // Se uguale, il tabellone non ha scritto → il countdown locale continua.
  useEffect(() => {
    let active = true;
    let timer: ReturnType<typeof setTimeout>;

    const poll = async () => {
      let d: LiveData = { has_live: false, source: "none", status: "none" };
      try {
        const r = await fetch(`${API}/api-web/live`);
        if (r.ok) d = await r.json();
      } catch {}
      if (!active) return;
      setData(d);
      setLoading(false);
      if (d.has_live && d.clock_seconds !== undefined) {
        const serverClock = d.clock_seconds;
        if (serverClock !== lastServerClock.current) {
          setClockSeconds(serverClock);
          lastServerClock.current = serverClock;
        }
        setClockRunning(!!d.clock_running);
        setClockPeriod(d.period ?? 1);
      } else {
        setClockRunning(false);
      }
      timer = setTimeout(poll, d.has_live ? POLL_LIVE_MS : POLL_IDLE_MS);
    };

    poll();
    return () => { active = false; clearTimeout(timer); };
  }, []);

  // Countdown locale: scorre 1s/1s tra un polling e l'altro.
  // Si ferma se il cronometro non gira.
  useEffect(() => {
    if (!clockRunning) return;
    const id = setInterval(() => setClockSeconds(c => Math.max(0, c - 1)), 1000);
    return () => clearInterval(id);
  }, [clockRunning]);

  const isLive = data?.has_live === true;

  return (
    <div className="pt-32 pb-20 min-h-screen">
      {/* Header */}
      <div className="relative overflow-hidden border-b-[4px] border-zinc-800 mb-12">
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none">
          <span className="font-display font-black text-[18vw] uppercase text-white/[0.025] whitespace-nowrap tracking-tighter leading-none">
            LIVE
          </span>
        </div>
        <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center pb-12">
          <h1 className="font-display text-[44px] sm:text-[70px] md:text-[120px] text-brand-orange uppercase leading-[0.8] mb-6 tracking-[-2px] md:tracking-[-4px]">
            Diretta
          </h1>
          <p className="text-base sm:text-xl font-sans text-zinc-400 max-w-3xl mx-auto">
            Punteggio e statistiche in tempo reale dal campo.
          </p>
        </div>
      </div>

      <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
        {loading && !data ? (
          <LoadingState />
        ) : isLive && data?.home_team && data?.away_team ? (
          <LiveBoard
            data={data}
            clock={formatClock(clockSeconds)}
            running={clockRunning}
            period={clockPeriod}
          />
        ) : (
          <EmptyState message={data?.message} />
        )}
      </div>
    </div>
  );
}

// ── Tabellone live ───────────────────────────────────────────
function LiveBoard({ data, clock, running, period }: { data: LiveData; clock: string; running: boolean; period?: number }) {
  const home = data.home_team!;
  const away = data.away_team!;
  const homeScore = data.home_score ?? home.score ?? 0;
  const awayScore = data.away_score ?? away.score ?? 0;
  const homeLead = homeScore > awayScore;
  const awayLead = awayScore > homeScore;
  const paused = data.status === "Paused";

  return (
    <div className="space-y-6">
      {/* Badge stato */}
      <div className="flex justify-center">
        <div className={`inline-flex items-center gap-2.5 px-4 py-2 border-2 font-display uppercase tracking-widest text-sm ${
          paused
            ? "border-brand-yellow text-brand-yellow"
            : "border-red-500 text-red-500"
        }`}>
          <span className="relative flex h-2.5 w-2.5">
            {!paused && <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-500 opacity-75" />}
            <span className={`relative inline-flex rounded-full h-2.5 w-2.5 ${paused ? "bg-brand-yellow" : "bg-red-500"}`} />
          </span>
          {paused ? "In pausa" : "Live"}
        </div>
      </div>

      {/* Scoreboard centrale stile NBA */}
      <div className="bg-zinc-950 border-[5px] border-zinc-800 shadow-[12px_12px_0_var(--color-brand-blue)]">
        <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-2 sm:gap-4 p-4 sm:p-8">

          {/* HOME */}
          <TeamScore team={home} score={homeScore} leading={homeLead} accent="blue" align="left" />

          {/* CENTRO: periodo + tempo */}
          <div className="flex flex-col items-center justify-center px-1 sm:px-4 min-w-[80px] sm:min-w-[140px]">
            <div className="font-display uppercase text-zinc-500 text-[10px] sm:text-sm tracking-widest mb-1">
              {PERIOD_LABELS[period ?? data.period ?? 0] ?? "—"}
            </div>
            <div className={`font-mono font-black tabular-nums text-2xl sm:text-5xl leading-none ${
              running ? "text-brand-yellow" : "text-zinc-400"
            }`}>
              {clock}
            </div>
          </div>

          {/* AWAY */}
          <TeamScore team={away} score={awayScore} leading={awayLead} accent="orange" align="right" />
        </div>
      </div>

      {/* Roster giocatori — affiancati e a specchio, punti al centro.
          max-w + mx-auto: la coppia di tabelle resta centrata sotto il tabellone
          invece di spalmarsi su tutta la larghezza. */}
      <div className="grid grid-cols-2 gap-3 sm:gap-5 max-w-4xl mx-auto">
        <PlayerList team={home} accent="blue" />
        <PlayerList team={away} accent="orange" mirrored />
      </div>
    </div>
  );
}

function TeamScore({ team, score, leading, accent, align }: {
  team: LiveTeam; score: number; leading: boolean; accent: Accent; align: "left" | "right";
}) {
  const a = ACCENTS[accent];
  return (
    <div className={`flex flex-col ${align === "right" ? "items-end text-right" : "items-start text-left"}`}>
      <div className={`h-1 w-12 sm:w-20 mb-2 ${a.bar}`} />
      <Link to={`/giocatori?team=${encodeURIComponent(team.name)}`} className="font-display uppercase text-white text-base sm:text-3xl leading-[0.95] break-words hover:text-brand-orange transition-colors">
        {team.name}
      </Link>
      {team.short_name && (
        <div className="font-mono text-zinc-600 text-xs sm:text-sm mt-0.5">{team.short_name}</div>
      )}
      <motion.div
        key={score}
        initial={{ scale: 1.25 }}
        animate={{ scale: 1 }}
        transition={{ type: "spring", stiffness: 380, damping: 18 }}
        className={`font-mono font-black tabular-nums leading-none mt-2 text-[64px] sm:text-[110px] md:text-[130px] ${
          leading ? a.text : "text-white"
        }`}
      >
        {score}
      </motion.div>
    </div>
  );
}

function PlayerList({ team, accent, mirrored = false }: { team: LiveTeam; accent: Accent; mirrored?: boolean }) {
  const a = ACCENTS[accent];
  const players = [...team.players].sort((x, y) => y.points - x.points);
  // L'ordine base è [nome, falli, punti]. Con flex-row-reverse diventa
  // [punti, falli, nome]: così i punti delle due squadre si toccano al centro.
  const rowDir = mirrored ? "flex-row-reverse" : "";
  const nameAlign = mirrored ? "text-right" : "text-left";
  // Padding ridotto sul lato esterno (sinistra a sx, destra a dx) e normale
  // sul lato centrale, così "Players" e i nomi si attaccano al bordo esterno.
  const padX = mirrored ? "pr-2 pl-3" : "pl-2 pr-3";

  return (
    <div className="bg-zinc-900 border-[3px] border-zinc-800">
      {/* Intestazione */}
      <div className={`flex items-center gap-2 ${padX} py-3 border-b-[3px] ${a.border} ${rowDir}`}>
        <h3 className={`font-display uppercase text-white text-base sm:text-xl tracking-wide flex-1 min-w-0 ${nameAlign}`}>
          Players
        </h3>
        <span className="font-mono text-zinc-500 text-[10px] uppercase w-5 text-center shrink-0">F</span>
        <span className="font-mono text-zinc-500 text-[10px] uppercase w-6 text-center shrink-0">PT</span>
      </div>

      {players.length === 0 ? (
        <p className="px-3 py-6 text-zinc-600 font-sans text-xs text-center">
          Formazione non disponibile
        </p>
      ) : (
        <ul className="divide-y divide-zinc-800">
          {players.map(p => {
            const fouledOut = p.fouls >= FOUL_LIMIT;
            return (
              <li key={p.player_id} className={`flex items-center gap-2 ${padX} py-2 ${rowDir}`}>
                <Link to={`/statistiche/${playerSlug(p.name, p.jersey_number)}`} className={`font-sans text-zinc-200 text-xs sm:text-sm flex-1 min-w-0 break-words leading-tight hover:text-brand-orange transition-colors ${nameAlign}`}>
                  {(() => {
                    const parts = p.name.trim().split(" ");
                    const first = parts.shift() ?? "";
                    const rest = parts.join(" ");
                    return (
                      <>
                        <span className="block">{first}</span>
                        {rest && <span className="block">{rest}</span>}
                      </>
                    );
                  })()}
                </Link>
                <span className={`font-mono tabular-nums text-sm w-5 text-center shrink-0 ${
                  fouledOut ? "text-red-500 font-bold" : "text-zinc-500"
                }`}>
                  {p.fouls}
                </span>
                <span className="font-mono tabular-nums text-white text-sm sm:text-base font-bold w-6 text-center shrink-0">
                  {p.points}
                </span>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}

// ── Stati vuoti ──────────────────────────────────────────────
function EmptyState({ message }: { message?: string }) {
  return (
    <div className="border-[3px] border-dashed border-zinc-700 bg-zinc-900/50 p-10 sm:p-16 text-center max-w-2xl mx-auto">
      <div className="flex justify-center mb-6">
        <img src="/assets/logo.png" alt="" className="w-16 h-16 opacity-30" />
      </div>
      <h2 className="font-display text-3xl sm:text-4xl uppercase text-white mb-3">
        Nessuna diretta in corso
      </h2>
      <p className="font-sans text-zinc-400 text-base sm:text-lg mb-8">
        {message ?? "Al momento non ci sono partite in diretta."} Torna durante le giornate del torneo per seguire il punteggio in tempo reale.
      </p>
      <a
        href="/match"
        className="inline-block bg-brand-orange text-brand-bg font-display uppercase tracking-widest px-8 py-4 text-lg hover:bg-white transition-colors"
      >
        Vedi il calendario
      </a>
    </div>
  );
}

function LoadingState() {
  return (
    <div className="text-center py-20">
      <div className="inline-block w-12 h-12 border-4 border-zinc-700 border-t-brand-orange rounded-full animate-spin" />
      <p className="font-display uppercase text-zinc-500 tracking-widest mt-6">Caricamento diretta…</p>
    </div>
  );
}
