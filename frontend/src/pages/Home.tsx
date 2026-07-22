import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { motion } from "motion/react";
import { Calendar, MapPin, Trophy, BarChart2, ArrowUpRight, ChevronLeft, ChevronRight } from "lucide-react";

const MAPS_URL = "https://maps.app.goo.gl/5kgGKRw5Hm3LJHbn9";

const API = import.meta.env.VITE_API_URL ?? "";

const IT_MONTHS_MAP: Record<string, number> = {
  Gen:1,Feb:2,Mar:3,Apr:4,Mag:5,Giu:6,Lug:7,Ago:8,Set:9,Ott:10,Nov:11,Dic:12
};

// Parsa "10 Lug 20:45" e calcola target + visibilità:
// - Prima del kickoff → mostra countdown a quest'anno
// - Entro 30 giorni dal kickoff → nascondi (torneo in corso)
// - Dopo 30 giorni → mostra countdown all'anno prossimo
function getKickoffState(dateStr: string): { target: Date; show: boolean } | null {
  const [day, mon, time] = dateStr.split(" ");
  const month = IT_MONTHS_MAP[mon];
  if (!month || !time) return null;
  const [hour, minute] = time.split(":").map(Number);
  const now = new Date();
  const year = now.getFullYear();
  const thisYear = new Date(year, month - 1, parseInt(day), hour, minute, 0);
  const hideUntil = new Date(thisYear.getTime() + 30 * 86400000);
  if (now < thisYear) return { target: thisYear, show: true };
  if (now < hideUntil) return { target: thisYear, show: false };
  return { target: new Date(year + 1, month - 1, parseInt(day), hour, minute, 0), show: true };
}

function useCountdown(target: Date) {
  const calc = () => {
    const diff = target.getTime() - Date.now();
    if (diff <= 0) return { days: 0, hours: 0, minutes: 0, seconds: 0, over: true };
    return {
      days:    Math.floor(diff / 86400000),
      hours:   Math.floor((diff % 86400000) / 3600000),
      minutes: Math.floor((diff % 3600000)  / 60000),
      seconds: Math.floor((diff % 60000)    / 1000),
      over: false,
    };
  };
  const [time, setTime] = useState(calc);
  useEffect(() => {
    const id = setInterval(() => setTime(calc()), 1000);
    return () => clearInterval(id);
  }, []);
  return time;
}

function CountdownUnit({ value, label, pulse = false }: { value: number; label: string; pulse?: boolean }) {
  return (
    <div className="flex flex-col items-center gap-1 sm:gap-3">
      <div className={`bg-zinc-900 border-[2px] sm:border-[3px] border-brand-orange px-3 sm:px-8 md:px-10 py-2.5 sm:py-5 md:py-7 flex items-center justify-center shadow-[3px_3px_0_var(--color-brand-blue)] sm:shadow-[6px_6px_0_var(--color-brand-blue)] ${pulse ? "animate-pulse" : ""}`}>
        <span className="font-display text-[28px] sm:text-6xl md:text-8xl text-white tabular-nums leading-none tracking-[-1px] sm:tracking-[-2px]">
          {String(value).padStart(2, "0")}
        </span>
      </div>
      <span className="font-display text-[8px] sm:text-[10px] uppercase tracking-[0.2em] sm:tracking-[0.3em] text-brand-orange">{label}</span>
    </div>
  );
}

type StatLeader = { name: string; team: string; value: number };
type HomeStats = { pts: StatLeader | null; ast: StatLeader | null; reb: StatLeader | null };

type HomeMatch = {
  id: string;
  round: string;
  phase?: string;
  date: string;
  status: "COMPLETA" | "LIVE" | "IN PROGRAMMA";
  team1: { name: string; score: number; color?: string | null };
  team2: { name: string; score: number; color?: string | null };
};

// — Match Center: prossima partita (o quella live) in evidenza —

function MatchCenterCard({ match }: { match: HomeMatch }) {
  const isLive = match.status === "LIVE";
  return (
    <div className="border-[3px] border-brand-orange bg-zinc-900 shadow-brand-lg relative">
      {isLive && (
        <div className="absolute top-4 right-4 z-10 flex items-center gap-2 bg-brand-orange text-brand-bg font-display px-3 py-1 text-xs uppercase tracking-widest -rotate-2">
          <span className="relative flex h-2 w-2">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-brand-bg opacity-75" />
            <span className="relative inline-flex rounded-full h-2 w-2 bg-brand-bg" />
          </span>
          Live
        </div>
      )}
      <div className="flex items-center justify-between px-5 sm:px-8 py-3 border-b-2 border-white/10">
        <span className="font-display text-xs sm:text-sm uppercase tracking-[0.2em] text-brand-orange">{match.round}</span>
        <span className="font-mono text-xs text-ink-dim">{match.date}</span>
      </div>
      <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-3 sm:gap-6 px-5 sm:px-10 py-8 sm:py-10">
        <div className="flex flex-col items-center gap-3 text-center min-w-0">
          <span className="w-8 h-1 rounded-full" style={{ backgroundColor: match.team1.color ?? "var(--color-ink-dim)" }} />
          <span className="font-display text-base sm:text-2xl uppercase tracking-wide leading-tight text-white truncate max-w-full">{match.team1.name}</span>
          {isLive && <span className="font-display text-4xl sm:text-6xl text-brand-orange tabular-nums leading-none">{match.team1.score}</span>}
        </div>
        <div className="font-display text-sm sm:text-lg text-brand-yellow border-2 border-brand-yellow rounded-full w-10 h-10 sm:w-14 sm:h-14 flex items-center justify-center shrink-0 bg-brand-bg">VS</div>
        <div className="flex flex-col items-center gap-3 text-center min-w-0">
          <span className="w-8 h-1 rounded-full" style={{ backgroundColor: match.team2.color ?? "var(--color-ink-dim)" }} />
          <span className="font-display text-base sm:text-2xl uppercase tracking-wide leading-tight text-white truncate max-w-full">{match.team2.name}</span>
          {isLive && <span className="font-display text-4xl sm:text-6xl text-brand-orange tabular-nums leading-none">{match.team2.score}</span>}
        </div>
      </div>
      <div className="flex items-center justify-between gap-4 px-5 sm:px-8 py-4 border-t-2 border-white/10">
        <Link to={`/match/calendario`} className="font-display text-xs sm:text-sm uppercase tracking-widest text-ink-dim hover:text-white transition-colors">
          Scheda completa →
        </Link>
        {isLive && (
          <Link
            to="/live"
            className="bg-brand-orange text-brand-bg font-display px-4 sm:px-6 py-2.5 uppercase text-xs sm:text-sm tracking-widest hover:bg-white transition-colors shadow-brand-sm"
          >
            Segui live
          </Link>
        )}
      </div>
    </div>
  );
}

// — Tournament Pulse: numeri reali dell'edizione, derivati dalle partite già caricate —

function PulseStat({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col items-center text-center gap-1.5 px-2">
      <span className="font-display text-3xl sm:text-5xl text-white tabular-nums leading-none">{value}</span>
      <span className="font-display text-[9px] sm:text-[10px] uppercase tracking-[0.22em] text-brand-orange">{label}</span>
    </div>
  );
}

export function Home() {
  const [kickoffState, setKickoffState] = useState<{ target: Date; show: boolean } | null>(null);
  const [allMatches, setAllMatches] = useState<HomeMatch[]>([]);
  const [homeStats, setHomeStats] = useState<HomeStats | null>(null);
  const [galleryPhotos, setGalleryPhotos] = useState<string[]>([]);
  const [galleryGroup, setGalleryGroup] = useState(0);

  useEffect(() => {
    fetch(`${API}/api-web/partite`)
      .then(r => r.ok ? r.json() : null)
      .then((matches: HomeMatch[] | null) => {
        if (!matches?.length) return;
        setAllMatches(matches);
        const first = matches.find(m => m.date);
        if (first) setKickoffState(getKickoffState(first.date));
      })
      .catch(() => {});

    fetch(`${API}/api-web/statistiche`)
      .then(r => r.ok ? r.json() : null)
      .then((d: any) => {
        const ps: any[] = d?.players ?? [];
        if (!ps.length) return;
        const top = (key: string): StatLeader | null => {
          const p = [...ps].sort((a, b) => b[key] - a[key])[0];
          return p ? { name: p.name, team: p.team, value: p[key] } : null;
        };
        setHomeStats({ pts: top("pts"), ast: top("ast"), reb: top("reb") });
      })
      .catch(() => {});

    fetch(`${API}/api-web/merchandise`)
      .then(r => r.ok ? r.json() as Promise<string[]> : null)
      .then(data => {
        if (!data?.length) return;
        const shuffled = [...data].sort(() => Math.random() - 0.5);
        // Precarica tutte le foto in background: sono pesanti (foto originali,
        // non ottimizzate), senza preload la rotazione ogni 5s mostra un flash
        // nero mentre il browser scarica il gruppo successivo.
        shuffled.forEach(url => { const img = new Image(); img.src = url; });
        setGalleryPhotos(shuffled);
        setGalleryGroup(Math.floor(Math.random() * Math.ceil(shuffled.length / 4)));
      })
      .catch(() => {});
  }, []);

  // Prossima partita da giocare (o la prima live)
  const liveMatch = allMatches.find(m => m.status === "LIVE");
  const nextMatch = liveMatch ?? allMatches.find(m => m.status === "IN PROGRAMMA");

  // Tournament Pulse — numeri reali, derivati dalle partite già caricate (nessuna nuova chiamata API)
  const playedMatches = allMatches.filter(m => m.status === "COMPLETA");
  const teamsCount = new Set(allMatches.flatMap(m => [m.team1.name, m.team2.name])).size;
  const totalPoints = playedMatches.reduce((sum, m) => sum + m.team1.score + m.team2.score, 0);

  // Playoff: semifinali e finale
  const semis = allMatches.filter(m => m.round.includes("Semifinale") || m.round.includes("Final Four"));
  const sf1 = semis[0];
  const sf2 = semis[1];
  const finale = allMatches.find(m => m.round === "Finale" || m.round.includes("Finale 1"));

  const galleryGroupSize = 4;
  const galleryTotalGroups = Math.max(1, Math.ceil(galleryPhotos.length / galleryGroupSize));
  const gallerySlice = galleryPhotos.slice(galleryGroup * galleryGroupSize, (galleryGroup + 1) * galleryGroupSize);
  const galleryNext = () => setGalleryGroup(g => (g + 1) % galleryTotalGroups);
  const galleryPrev = () => setGalleryGroup(g => (g - 1 + galleryTotalGroups) % galleryTotalGroups);


  const fallbackTarget = (() => {
    const now = new Date();
    const y = now.getFullYear();
    const t = new Date(y, 6, 8, 21, 0, 0); // 8 Lug 21:00
    return t > now ? t : new Date(y + 1, 6, 8, 21, 0, 0);
  })();
  const countdown = useCountdown(kickoffState?.target ?? fallbackTarget);
  return (
    <div className="w-full">
      {/* Hero Section */}
      <section className="relative min-h-[70vh] md:min-h-[90vh] flex items-center justify-center overflow-hidden pt-20">
        <div className="absolute inset-0 z-0">
          <img
            src="/assets/campetto.webp"
            alt="Street Basketball Court"
            className="w-full h-full object-cover object-center opacity-40 grayscale"
            fetchPriority="high"
            loading="eager"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-zinc-950 via-zinc-950/60 to-transparent"></div>
          <div className="absolute inset-0 bg-brand-blue/10 mix-blend-overlay"></div>
        </div>


        <div className="relative z-10 w-full max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col items-start text-left pb-8 md:pb-0">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.2, duration: 0.5 }}
            className="flex flex-wrap items-center gap-x-4 gap-y-1.5 mb-5 font-mono text-[11px] sm:text-xs uppercase tracking-[0.15em] text-ink-dim"
          >
            <span className="flex items-center gap-1.5">
              <MapPin className="w-3.5 h-3.5 text-brand-orange" /> Porto Potenza Picena
            </span>
            <span className="w-1 h-1 rounded-full bg-zinc-700" aria-hidden="true" />
            <span>Estate 2026</span>
            <span className="w-1 h-1 rounded-full bg-zinc-700" aria-hidden="true" />
            <span className={liveMatch ? "text-brand-orange" : "text-ink-dim"}>
              {liveMatch ? "Torneo in corso" : nextMatch ? "Torneo in avvicinamento" : "Prossima edizione"}
            </span>
          </motion.div>

          <motion.p
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.4, duration: 0.6 }}
            className="text-lg md:text-xl font-sans font-light text-zinc-400 max-w-md mb-8"
          >
            Il torneo di basket 5vs5 più duro dell'asfalto. Raduna la tua squadra, scendi in campo e conquista la gloria.
          </motion.p>

          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6 }}
          >
            <h1 className="font-display leading-[0.85] mb-6 tracking-[-2px] md:tracking-[-4px]">
              <span className="block text-5xl sm:text-6xl md:text-[100px] lg:text-[120px] text-white">PRONTI PER IL</span>
              <span className="block text-5xl sm:text-6xl md:text-[100px] lg:text-[120px] text-brand-orange">MADNESS?</span>
            </h1>
          </motion.div>

          <motion.div
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ delay: 0.6, duration: 0.4 }}
            className="flex items-end gap-4 w-full mt-2"
          >
            {/* Bottoni */}
            <div className="flex flex-col sm:flex-row gap-4">
              <Link
                to="/info"
                className="bg-brand-orange text-brand-bg px-5 py-4 sm:py-5 font-black uppercase text-base sm:text-xl -rotate-2 w-fit hover:-rotate-1 transition-transform border-[3px] border-transparent hover:border-white shadow-[6px_6px_0_var(--color-brand-blue)] sm:shadow-[8px_8px_0_var(--color-brand-blue)] hover:shadow-[4px_4px_0_var(--color-brand-blue)]"
              >
                Scopri il Torneo
              </Link>
              <Link
                to={liveMatch ? "/live" : "/match/calendario"}
                className="px-6 sm:px-8 py-4 border-[3px] border-white text-white font-display text-base sm:text-xl uppercase tracking-wider hover:bg-white hover:text-black transition-colors w-fit shadow-[6px_6px_0_var(--color-brand-orange)] sm:shadow-[8px_8px_0_var(--color-brand-orange)] hover:shadow-[4px_4px_0_var(--color-brand-orange)] transform hover:translate-x-1 hover:translate-y-1 flex items-center gap-2.5"
              >
                {liveMatch && (
                  <span className="relative flex h-2.5 w-2.5">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-500 opacity-75" />
                    <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-red-500" />
                  </span>
                )}
                {liveMatch ? "Vedi il match live" : "Vedi i Match"}
              </Link>
            </div>

          </motion.div>
        </div>
      </section>

      {/* Marquee Ticker — CSS puro per evitare blocchi sul main thread mobile */}
      <div className="overflow-hidden whitespace-nowrap border-y-[4px] border-brand-blue bg-brand-orange py-3 md:py-4 -rotate-2 scale-105 relative z-20 md:-mt-8 shadow-[0_10px_30px_rgba(0,0,0,0.5)]">
        <div className="marquee-track inline-block text-brand-bg font-display text-3xl md:text-5xl uppercase tracking-widest font-black">
          PIAZZETTA MADNESS // STREET BASKETBALL // NO EXCUSES // PLAY HARD // PIAZZETTA MADNESS // STREET BASKETBALL // NO EXCUSES // PLAY HARD // PIAZZETTA MADNESS // STREET BASKETBALL // NO EXCUSES // PLAY HARD //&nbsp;&nbsp;PIAZZETTA MADNESS // STREET BASKETBALL // NO EXCUSES // PLAY HARD // PIAZZETTA MADNESS // STREET BASKETBALL // NO EXCUSES // PLAY HARD // PIAZZETTA MADNESS // STREET BASKETBALL // NO EXCUSES // PLAY HARD //&nbsp;
        </div>
      </div>

      {/* Match Center — prossima partita o live */}
      {nextMatch && (
        <section className="py-12 md:py-16 bg-brand-bg">
          <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
            <p className="font-display text-xs uppercase tracking-[0.35em] text-ink-dim mb-6 text-center">
              {liveMatch ? "In corso ora" : "Prossima partita"}
            </p>
            <MatchCenterCard match={nextMatch} />
          </div>
        </section>
      )}

      {/* Tournament Pulse — stato dell'edizione in numeri reali */}
      {allMatches.length > 0 && (
        <section className="py-10 md:py-14 bg-zinc-900/60 border-y-2 border-white/10">
          <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex flex-wrap items-center justify-around gap-y-6 gap-x-4">
              <PulseStat label="Squadre" value={String(teamsCount)} />
              <PulseStat label="Partite Giocate" value={String(playedMatches.length)} />
              <PulseStat label="Punti Segnati" value={String(totalPoints)} />
              {homeStats?.pts && (
                <div className="flex flex-col items-center text-center gap-1.5 px-2 max-w-[160px]">
                  <span className="font-display text-3xl sm:text-5xl text-brand-orange tabular-nums leading-none">{homeStats.pts.value}</span>
                  <span className="font-display text-[9px] sm:text-[10px] uppercase tracking-[0.22em] text-ink-dim truncate w-full">
                    Top Scorer — {homeStats.pts.name}
                  </span>
                </div>
              )}
            </div>
          </div>
        </section>
      )}

      {/* Countdown */}
      {(kickoffState === null || kickoffState.show) && !countdown.over && (
        <section className="py-16 md:py-24 bg-brand-bg border-b-[4px] border-zinc-800">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
            <p className="font-display text-xs uppercase tracking-[0.35em] text-zinc-500 mb-8">Il Madness inizia tra</p>
            <div className="flex items-start justify-center gap-1 sm:gap-4">
              <CountdownUnit value={countdown.days}    label="Giorni"  />
              <div className="w-px bg-zinc-800 self-stretch mt-1 mb-6 sm:mt-2 sm:mb-8 mx-0.5 sm:mx-2" />
              <CountdownUnit value={countdown.hours}   label="Ore"     />
              <div className="w-px bg-zinc-800 self-stretch mt-1 mb-6 sm:mt-2 sm:mb-8 mx-0.5 sm:mx-2" />
              <CountdownUnit value={countdown.minutes} label="Minuti"  />
              <div className="w-px bg-zinc-800 self-stretch mt-1 mb-6 sm:mt-2 sm:mb-8 mx-0.5 sm:mx-2" />
              <CountdownUnit value={countdown.seconds} label="Secondi" pulse />
            </div>
            <p className="font-mono text-xs text-zinc-700 mt-8 tracking-widest">
              ESTATE 2026 — PORTO POTENZA PICENA
            </p>
          </div>
        </section>
      )}

      {/* Info Quick Look - Bento Grid Layout */}
      <section className="py-12 md:py-16 bg-brand-bg relative z-10">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 md:grid-cols-12 gap-6">
            
            {/* Block 1: The Court */}
            <a
              href={MAPS_URL}
              target="_blank"
              rel="noopener noreferrer"
              className="md:col-span-8 bg-zinc-900 border-[4px] border-brand-blue p-8 md:p-12 relative overflow-hidden group hover:bg-zinc-800 transition-colors flex flex-col justify-between gap-8"
            >
              <div className="relative">
                <MapPin className="w-12 h-12 text-brand-blue mb-6 group-hover:scale-110 group-hover:rotate-6 transition-transform" />
                <div className="absolute top-0 right-0 pointer-events-none opacity-10 group-hover:opacity-20 transition-opacity">
                  <img src="/logo.png" alt="" className="w-32 h-32 object-contain" />
                </div>
                <h2 className="font-display text-4xl md:text-5xl mb-3 text-white uppercase">The Court</h2>
                <p className="font-sans text-zinc-400 text-lg md:text-xl max-w-md">Il tempio del basket di strada. Dove l'asfalto scotta e non ci sono regole scritte, solo rispetto.</p>
              </div>

              <div className="flex flex-col sm:flex-row items-start sm:items-end justify-between gap-6">
                <div className="space-y-1">
                  <p className="font-mono text-[10px] uppercase tracking-[0.3em] text-zinc-600">Indirizzo</p>
                  <p className="font-mono text-sm text-zinc-300">Via Marche, Porto Potenza Picena (MC)</p>
                  <p className="font-mono text-[11px] text-zinc-600 tracking-widest">43°21'23"N  13°41'49"E</p>
                </div>
                <div className="flex items-center gap-3 bg-brand-blue text-white px-5 py-3 font-display uppercase text-sm tracking-widest group-hover:bg-white group-hover:text-black transition-colors shrink-0 border-[2px] border-transparent group-hover:border-brand-blue shadow-[4px_4px_0_rgba(0,0,0,0.3)]">
                  Apri su Google Maps
                  <ArrowUpRight className="w-4 h-4 group-hover:translate-x-1 group-hover:-translate-y-1 transition-transform" />
                </div>
              </div>
            </a>

            {/* Block 2: Stats leader */}
            <Link to="/statistiche" className="md:col-span-4 bg-zinc-900 border-[4px] border-brand-orange p-8 flex flex-col justify-between group hover:-translate-y-1 transition-transform shadow-[8px_8px_0_var(--color-brand-orange)]">
              <div className="flex justify-between items-start mb-4">
                <BarChart2 className="w-10 h-10 text-brand-orange group-hover:scale-110 transition-transform" />
                <ArrowUpRight className="w-8 h-8 text-brand-orange opacity-40 group-hover:opacity-100 transition-opacity" />
              </div>

              <div className="flex-1 flex flex-col justify-between">
                <h3 className="font-display text-2xl sm:text-3xl uppercase text-white mb-4">Statistiche</h3>

                {homeStats ? (
                  <div className="space-y-3">
                    {([
                      { label: "PTS", stat: homeStats.pts },
                      { label: "AST", stat: homeStats.ast },
                      { label: "REB", stat: homeStats.reb },
                    ] as const).map(({ label, stat }) => stat && (
                      <div key={label} className="flex items-center gap-3 border-b border-zinc-800 pb-3 last:border-0 last:pb-0">
                        <span className="font-display text-[10px] uppercase tracking-widest text-brand-orange w-7 shrink-0">{label}</span>
                        <span className="font-sans text-sm text-zinc-300 truncate flex-1 leading-tight">{stat.name}</span>
                        <span className="font-mono text-base font-black text-white tabular-nums shrink-0">{stat.value}</span>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="flex flex-col gap-2">
                    <p className="font-sans text-zinc-500 text-sm leading-relaxed">Statistiche disponibili dopo le prime partite.</p>
                    <p className="font-display text-xs uppercase tracking-widest text-brand-orange mt-2">Vedi la pagina →</p>
                  </div>
                )}
              </div>
            </Link>

            {/* Block 3: Fase a Gironi */}
            <Link to="/match/gironi" className="md:col-span-5 bg-zinc-900/50 border-[4px] border-brand-blue p-8 flex flex-col justify-between group hover:-translate-y-1 transition-transform shadow-[8px_8px_0_var(--color-brand-blue)]">
              <div>
                <Calendar className="text-brand-blue w-10 h-10 mb-6" />
                <h3 className="font-display text-2xl sm:text-3xl uppercase text-white mb-2">1. Fase a Gironi</h3>
                <p className="font-sans text-zinc-400">8 squadre, 2 gironi da 4. Le prime 2 di ogni girone passano ai playoff.</p>
              </div>
              <div className="border-t border-zinc-800 pt-4 mt-6 flex items-center justify-between">
                <span className="font-mono text-xs text-zinc-500">{playedMatches.length} partite giocate</span>
                <span className="font-display text-xs uppercase tracking-widest text-brand-blue">Classifica →</span>
              </div>
            </Link>

            {/* Block 4: Playoff Bracket con squadre reali */}
            <Link to="/match/bracket" className="md:col-span-7 bg-zinc-900/50 border-[4px] border-brand-orange p-8 flex flex-col justify-between group hover:-translate-y-1 transition-transform shadow-[8px_8px_0_var(--color-brand-orange)]">
              <div>
                <Trophy className="text-brand-orange w-10 h-10 mb-6" />
                <h3 className="font-display text-2xl sm:text-3xl uppercase text-white mb-2">2. Playoff Bracket</h3>
                <p className="font-sans text-zinc-400 mb-6">Eliminazione diretta fino al campione.</p>
              </div>
              <div className="flex items-center gap-3 mt-auto">
                <div className="flex-1 min-w-0 space-y-2">
                  <div className="h-8 border-[2px] border-zinc-700 bg-zinc-950 flex items-center justify-center px-2 overflow-hidden">
                    <span className="text-[9px] font-display uppercase truncate block text-zinc-500">
                      {sf1?.status === "COMPLETA"
                        ? (sf1.team1.score > sf1.team2.score ? sf1.team1.name : sf1.team2.name)
                        : "Semifinale 1"}
                    </span>
                  </div>
                  <div className="h-8 border-[2px] border-zinc-700 bg-zinc-950 flex items-center justify-center px-2 overflow-hidden">
                    <span className="text-[9px] font-display uppercase truncate block text-zinc-500">
                      {sf2?.status === "COMPLETA"
                        ? (sf2.team1.score > sf2.team2.score ? sf2.team1.name : sf2.team2.name)
                        : "Semifinale 2"}
                    </span>
                  </div>
                </div>
                <div className="w-5 border-t-2 border-r-2 border-b-2 border-zinc-700 h-10 shrink-0"></div>
                <div className="flex-1 min-w-0">
                  <div className="h-9 border-[2px] border-brand-yellow bg-zinc-950 flex items-center justify-center px-2 overflow-hidden">
                    <span className="text-[10px] font-display uppercase tracking-widest truncate block text-brand-yellow">
                      {finale?.status === "COMPLETA"
                        ? (finale.team1.score > finale.team2.score ? finale.team1.name : finale.team2.name)
                        : "Finale"}
                    </span>
                  </div>
                </div>
              </div>
            </Link>

          </div>
        </div>
      </section>

      
      {/* Visual Section */}
      <section className="py-12 md:py-16 relative overflow-hidden bg-brand-bg">
        {/* Background massive typography */}
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 font-display text-[15vw] text-zinc-900/40 whitespace-nowrap pointer-events-none z-0 tracking-tighter">
          NO FOULS
        </div>

        <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col lg:flex-row items-center gap-16 text-white">
          <div className="flex-1 pr-0 lg:pr-10">
            <h2 className="font-display text-5xl sm:text-6xl md:text-8xl font-black uppercase leading-[0.85] mb-8">
              Non e' solo <br/>
              <span className="text-brand-orange">un gioco</span>
            </h2>
            <div className="w-24 h-2 bg-brand-orange mb-8 transform -rotate-2"></div>
            <p className="font-sans font-light text-lg md:text-xl max-w-lg mb-10 text-zinc-300">
              Piazzetta Madness è rispetto, competizione e sudore.<br />Se non hai grinta, questa non è la tua casa.<br />Raduna la tua crew e preparati alla battaglia.
            </p>
            <Link 
              to="/foto" 
              className="group inline-flex items-center gap-4 bg-white text-black border-[3px] border-white font-display text-xl uppercase tracking-wider px-8 py-4 hover:bg-brand-orange hover:text-brand-bg hover:border-brand-orange transition-all shadow-[8px_8px_0_var(--color-brand-blue)] hover:shadow-[4px_4px_0_var(--color-brand-blue)]"
            >
              Guarda la Gallery
              <ArrowUpRight className="w-6 h-6 group-hover:translate-x-1 group-hover:-translate-y-1 transition-transform" />
            </Link>
          </div>
          
          <div className="flex-1 w-full relative mt-12 lg:mt-0">
            {galleryPhotos.length > 0 ? (
              <div>
                <div className={`grid gap-1.5 border-[6px] border-brand-orange shadow-[16px_16px_0_var(--color-brand-blue)] ${gallerySlice.length === 1 ? "grid-cols-1" : "grid-cols-2"}`}>
                  {gallerySlice.map((url, i) => (
                    <div key={url + i} className="aspect-square overflow-hidden bg-zinc-900">
                      <img
                        src={url}
                        alt={`Gallery ${galleryGroup * galleryGroupSize + i + 1}`}
                        className="w-full h-full object-cover"
                        loading="eager"
                        referrerPolicy="no-referrer"
                      />
                    </div>
                  ))}
                </div>

                {galleryTotalGroups > 1 && (
                  <div className="flex items-center justify-between mt-4">
                    <button
                      onClick={galleryPrev}
                      aria-label="Foto precedenti"
                      className="p-2.5 border-[3px] border-zinc-700 text-zinc-400 hover:border-brand-orange hover:text-brand-orange transition-colors"
                    >
                      <ChevronLeft className="w-5 h-5" />
                    </button>
                    <span className="font-mono text-xs text-zinc-600 tracking-[0.2em]">
                      {galleryGroup + 1} / {galleryTotalGroups}
                    </span>
                    <button
                      onClick={galleryNext}
                      aria-label="Foto successive"
                      className="p-2.5 border-[3px] border-zinc-700 text-zinc-400 hover:border-brand-orange hover:text-brand-orange transition-colors"
                    >
                      <ChevronRight className="w-5 h-5" />
                    </button>
                  </div>
                )}
              </div>
            ) : (
              <div className="aspect-[4/5] md:aspect-square bg-zinc-900 border-[8px] border-brand-orange relative group overflow-hidden shadow-[20px_20px_0_var(--color-brand-blue)] rotate-2 hover:rotate-0 transition-transform">
                <div className="absolute -left-6 top-4 bg-brand-yellow text-brand-bg px-8 py-2 font-black text-xl -rotate-6 uppercase z-20 border-2 border-brand-bg">
                  STREET CRED
                </div>
                <img
                  src="/assets/beer.webp"
                  alt="Mascotte Piazzetta Madness"
                  className="w-full h-full object-contain p-8 scale-100 group-hover:scale-110 transition-all duration-700"
                />
                <span className="absolute inset-x-0 bottom-10 flex items-center justify-center text-brand-bg/50 font-black text-4xl sm:text-5xl md:text-6xl pointer-events-none uppercase tracking-widest z-20 mix-blend-difference">
                  MADNESS
                </span>
              </div>
            )}
          </div>
        </div>
      </section>
    </div>
  );
}
