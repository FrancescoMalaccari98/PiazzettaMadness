import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { motion } from "motion/react";
import { Calendar, MapPin, Trophy, Users, ArrowUpRight, Swords } from "lucide-react";

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

export function Home() {
  const [kickoffState, setKickoffState] = useState<{ target: Date; show: boolean } | null>(null);

  useEffect(() => {
    fetch(`${API}/api-web/partite`)
      .then(r => r.ok ? r.json() : null)
      .then((matches: { date: string }[] | null) => {
        if (!matches?.length) return;
        const first = matches.find(m => m.date);
        if (first) setKickoffState(getKickoffState(first.date));
      })
      .catch(() => {});
  }, []);

  const fallbackTarget = (() => {
    const now = new Date();
    const y = now.getFullYear();
    const t = new Date(y, 6, 10, 20, 45, 0); // 10 Lug — stessa data della prima partita
    return t > now ? t : new Date(y + 1, 6, 10, 20, 45, 0);
  })();
  const countdown = useCountdown(kickoffState?.target ?? fallbackTarget);
  return (
    <div className="w-full">
      {/* Hero Section */}
      <section className="relative min-h-[70vh] md:min-h-[90vh] flex items-center justify-center overflow-hidden pt-20">
        <div className="absolute inset-0 z-0">
          <img
            src="/assets/campetto.jpeg"
            alt="Street Basketball Court"
            className="w-full h-full object-cover object-center opacity-40 grayscale"
            fetchPriority="high"
            loading="eager"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-zinc-950 via-zinc-950/60 to-transparent"></div>
          <div className="absolute inset-0 bg-brand-blue/10 mix-blend-overlay"></div>
        </div>


        <div className="relative z-10 w-full max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col items-start text-left pb-8 md:pb-0">
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
              <span className="block text-5xl sm:text-6xl md:text-[100px] lg:text-[120px] text-white">PRONTI PER LA</span>
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
                to="/match"
                className="px-6 sm:px-8 py-4 border-[3px] border-white text-white font-display text-base sm:text-xl uppercase tracking-wider hover:bg-white hover:text-black transition-colors w-fit shadow-[6px_6px_0_var(--color-brand-orange)] sm:shadow-[8px_8px_0_var(--color-brand-orange)] hover:shadow-[4px_4px_0_var(--color-brand-orange)] transform hover:translate-x-1 hover:translate-y-1"
              >
                Vedi i Match
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

      {/* Countdown */}
      {(kickoffState === null || kickoffState.show) && !countdown.over && (
        <section className="py-16 md:py-24 bg-brand-bg border-b-[4px] border-zinc-800">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
            <p className="font-display text-xs uppercase tracking-[0.35em] text-zinc-500 mb-8">La Madness inizia tra</p>
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
      <section className="py-32 bg-brand-bg relative z-10">
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
                <h3 className="font-display text-4xl md:text-5xl mb-3 text-white uppercase">The Court</h3>
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

            {/* Block 2: High Contrast Accent */}
            <div className="md:col-span-4 bg-brand-orange border-[4px] border-brand-orange p-8 md:p-12 flex flex-col justify-between group cursor-crosshair">
              <div className="flex justify-between items-start mb-16">
                <Calendar className="w-12 h-12 text-brand-bg group-hover:rotate-12 transition-transform" />
                <ArrowUpRight className="w-10 h-10 text-brand-bg opacity-50 group-hover:opacity-100 transition-opacity" />
              </div>
              <div>
                <h3 className="font-display text-4xl mb-2 text-brand-bg uppercase">Summer '26</h3>
                <p className="font-sans font-bold text-brand-bg/80 text-lg">Le date ufficiali verranno svelate a breve. Preparati.</p>
              </div>
            </div>

            {/* Block 3: Detail Info */}
            <div className="md:col-span-5 bg-zinc-900 border-[4px] border-brand-yellow p-8 md:p-12 relative overflow-hidden group">
              <Users className="w-12 h-12 text-brand-yellow mb-12 group-hover:scale-110 transition-transform" />
              <h3 className="font-display text-3xl mb-4 text-white uppercase">5 VS 5 Format</h3>
              <p className="font-sans text-zinc-400 text-lg">Match senza esclusione di colpi. Rotazioni veloci, fisicità al limite. Forma il tuo quintetto migliore.</p>
            </div>

            {/* Block 4: Prize */}
            <div className="md:col-span-7 bg-zinc-900 border-[4px] border-zinc-700 p-8 md:p-12 relative overflow-hidden group hover:border-white transition-colors">
              <div className="absolute top-0 right-0 w-1/3 h-full bg-zinc-800/30 border-l-[4px] border-zinc-700 group-hover:border-white transition-colors skew-x-12 translate-x-10"></div>
              <Trophy className="w-12 h-12 text-white mb-12 group-hover:scale-110 transition-transform relative z-10" />
              <h3 className="font-display text-3xl mb-4 text-white uppercase relative z-10">Gloria Eterna</h3>
              <p className="font-sans text-zinc-400 text-lg relative z-10 md:w-3/4">Montepremi in palio e il diritto di vantarsi come i veri Re della Piazzetta fino alla successiva edizione del torneo.</p>
            </div>

          </div>
        </div>
      </section>

      {/* Tournament Live Preview Section */}
      <section className="py-24 border-y-[6px] border-zinc-800 bg-zinc-950 relative overflow-hidden">
        <div className="absolute inset-0 bg-brand-blue/5"></div>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 relative z-10">
          <div className="flex flex-col md:flex-row justify-between items-start md:items-end mb-12 gap-6">
            <div>
              <h2 className="font-display text-3xl sm:text-5xl md:text-7xl uppercase flex items-center gap-3 sm:gap-4 text-white mb-4">
                <Swords className="w-8 h-8 sm:w-12 sm:h-12 text-brand-orange shrink-0" /> Formato Torneo
              </h2>
              <p className="font-sans text-base sm:text-xl text-zinc-400 max-w-2xl">Dal girone all'italiana (tutti contro tutti) fino all'eliminazione diretta. Scopri le date e i super-match della Piazzetta Madness.</p>
            </div>
            <Link 
              to="/match" 
              className="bg-zinc-900 text-white border-[3px] border-zinc-700 px-6 py-3 font-display uppercase tracking-widest hover:border-brand-orange hover:bg-brand-orange hover:text-black transition-colors shrink-0"
            >
              Vedi Tabellone Completo
            </Link>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
            {/* Box 1: Girone Preview */}
            <div className="border-[4px] border-brand-blue bg-zinc-900/50 p-8 hover:-translate-y-2 transition-transform shadow-[8px_8px_0_var(--color-brand-blue)] flex flex-col justify-between">
              <div>
                <Calendar className="text-brand-blue w-10 h-10 mb-6" />
                <h3 className="font-display text-3xl uppercase text-white mb-2">1. Fase a Gironi</h3>
                <p className="font-sans text-zinc-400 mb-8">Formato all'italiana: 6 squadre, un unico girone. Chi sopravvive accede alla fase finale. Ogni fischio è decisivo.</p>
              </div>
              <div className="border-t border-zinc-800 pt-6 mt-auto">
                <div className="flex justify-between items-center text-sm font-mono text-zinc-500 mb-2">
                  <span>Match Clou:</span>
                  <span>14 Ago</span>
                </div>
                <div className="flex items-center justify-between gap-2 font-sans font-bold text-sm sm:text-lg md:text-xl uppercase">
                  <span className="flex-1 text-left leading-tight">Saluta Andonio Spurs</span>
                  <span className="text-brand-orange shrink-0 px-1">VS</span>
                  <span className="flex-1 text-right leading-tight">Miami Spritz</span>
                </div>
              </div>
            </div>

            {/* Box 2: Playoff Preview */}
            <div className="border-[4px] border-brand-orange bg-zinc-900/50 p-8 hover:-translate-y-2 transition-transform shadow-[8px_8px_0_var(--color-brand-orange)] flex flex-col justify-between">
              <div>
                <Trophy className="text-brand-orange w-10 h-10 mb-6" />
                <h3 className="font-display text-3xl uppercase text-white mb-2">2. Playoff Bracket</h3>
                <p className="font-sans text-zinc-400 mb-8">Le migliori 4 si sfidano in semifinali e finale secca. Il tabellone si infiamma, niente seconde possibilità.</p>
              </div>
              <div className="flex items-center gap-4 mt-auto">
                {/* Mini bracket illustration */}
                <div className="flex-1 space-y-4">
                  <div className="h-10 border-[2px] border-zinc-700 bg-zinc-950 flex items-center justify-center text-xs font-display text-zinc-500 uppercase">Semifinale</div>
                  <div className="h-10 border-[2px] border-zinc-700 bg-zinc-950 flex items-center justify-center text-xs font-display text-zinc-500 uppercase">Semifinale</div>
                </div>
                <div className="w-8 border-t-2 border-r-2 border-b-2 border-zinc-700 h-14 translate-x-2"></div>
                <div className="flex-1">
                  <div className="h-12 border-[2px] border-brand-yellow font-display uppercase tracking-widest text-brand-yellow bg-zinc-950 flex items-center justify-center">Finale</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>
      
      {/* Visual Section */}
      <section className="py-32 relative overflow-hidden bg-brand-bg">
        {/* Background massive typography */}
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 font-display text-[15vw] text-zinc-900/40 whitespace-nowrap pointer-events-none z-0 tracking-tighter">
          NO FOULS
        </div>

        <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col lg:flex-row items-center gap-16 text-white">
          <div className="flex-1 pr-0 lg:pr-10">
            <h2 className="font-display text-5xl sm:text-6xl md:text-8xl font-black uppercase leading-[0.85] mb-8">
              Non è solo <br/>
              <span className="text-stroke-active text-transparent">un gioco</span>
            </h2>
            <div className="w-24 h-2 bg-brand-orange mb-8 transform -rotate-2"></div>
            <p className="font-sans font-light text-lg md:text-xl max-w-lg mb-10 text-zinc-300">
              Piazzetta Madness è rispetto, competizione e sudore. Se non hai grinta, questa non è la tua casa. Raduna la tua crew e preparati alla battaglia.
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
             <div className="aspect-[4/5] md:aspect-square bg-zinc-900 border-[8px] border-brand-orange relative group overflow-hidden shadow-[20px_20px_0_var(--color-brand-blue)] rotate-2 hover:rotate-0 transition-transform">
              <div className="absolute -left-6 top-4 bg-brand-yellow text-brand-bg px-8 py-2 font-black text-xl -rotate-6 uppercase z-20 border-2 border-brand-bg">
                STREET CRED
              </div>
              <img
                src="/assets/beer.png"
                alt="Mascotte Piazzetta Madness"
                className="w-full h-full object-contain p-8 scale-100 group-hover:scale-110 transition-all duration-700"
              />
              <span className="absolute inset-x-0 bottom-10 flex items-center justify-center text-brand-bg/50 font-black text-4xl sm:text-5xl md:text-6xl pointer-events-none uppercase tracking-widest z-20 mix-blend-difference">
                MADNESS
              </span>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
