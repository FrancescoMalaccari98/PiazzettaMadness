import { Link } from "react-router-dom";
import { motion } from "motion/react";
import { Calendar, MapPin, Trophy, Users, ArrowUpRight, Swords } from "lucide-react";

export function Home() {
  return (
    <div className="w-full">
      {/* Hero Section */}
      <section className="relative min-h-[70vh] md:min-h-[90vh] flex items-center justify-center overflow-hidden pt-20">
        <div className="absolute inset-0 z-0">
          {/* &w=1920: Unsplash ritaglia a 1920px invece di mandare l'originale 5650px (da ~2.5MB a ~200KB).
              fetchPriority="high": segnala al browser che questa è l'immagine più importante (LCP).
              loading="eager": esplicito, evita che un eventuale default "lazy" blocchi l'LCP. */}
          <img
            src="https://images.unsplash.com/photo-1546519638-68e109498ffc?auto=format&fit=crop&q=80&w=1920"
            alt="Street Basketball Action"
            className="w-full h-full object-cover object-center opacity-30 mix-blend-luminosity"
            fetchPriority="high"
            loading="eager"
            referrerPolicy="no-referrer"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-zinc-950 via-zinc-950/60 to-transparent"></div>
          <div className="absolute inset-0 bg-brand-blue/10 mix-blend-overlay"></div>
        </div>

        {/* Mascotte desktop — grande in basso a destra */}
        <div className="absolute right-0 bottom-0 w-[400px] lg:w-[520px] z-10 pointer-events-none select-none hidden md:block">
          <img
            src="/assets/beer.png"
            alt=""
            className="w-full h-auto object-contain drop-shadow-[0_0_40px_rgba(234,99,36,0.25)]"
            style={{ animation: 'mascotFloat 4s ease-in-out infinite' }}
          />
        </div>

        <div className="relative z-10 w-full max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col items-start text-left mt-6 md:mt-20 pb-8 md:pb-0">
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

            {/* Mascotte mobile — affiancata ai bottoni */}
            <div className="md:hidden flex-shrink-0 w-[80px] sm:w-[110px] pointer-events-none select-none">
              <img
                src="/assets/beer.png"
                alt=""
                className="w-full h-auto object-contain"
                style={{ animation: 'mascotFloat 4s ease-in-out infinite' }}
              />
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

      {/* Info Quick Look - Bento Grid Layout */}
      <section className="py-32 bg-brand-bg relative z-10">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 md:grid-cols-12 gap-6">
            
            {/* Block 1: Large Focus */}
            <div className="md:col-span-8 bg-zinc-900 border-[4px] border-brand-blue p-8 md:p-12 relative overflow-hidden group hover:bg-zinc-800 transition-colors">
              <MapPin className="w-12 h-12 text-brand-blue mb-16 md:mb-24 group-hover:scale-110 group-hover:rotate-6 transition-transform" />
              <div className="absolute top-4 right-8 font-display text-[150px] text-zinc-800/30 leading-none pointer-events-none group-hover:text-brand-blue/10 transition-colors">01</div>
              <h3 className="font-display text-4xl md:text-5xl mb-4 text-white uppercase">The Court</h3>
              <p className="font-sans text-zinc-400 text-lg md:text-xl max-w-md">Il tempio del basket di strada. Dove l'asfalto scotta e non ci sono regole scritte, solo rispetto.</p>
            </div>

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
              <div className="absolute -left-6 top-10 bg-brand-yellow text-brand-bg px-8 py-2 font-black text-xl -rotate-6 uppercase z-20 border-2 border-brand-bg">
                STREET CRED
              </div>
              <img 
                src="https://images.unsplash.com/photo-1518481612222-68bbe828def1?auto=format&fit=crop&q=80&w=800"
                alt="Dunk"
                className="w-full h-full object-cover grayscale opacity-80 group-hover:grayscale-0 group-hover:opacity-100 transition-all duration-700 scale-100 group-hover:scale-105"
                referrerPolicy="no-referrer"
              />
              <span className="absolute inset-x-0 bottom-10 flex items-center justify-center text-brand-bg/50 font-black text-7xl md:text-8xl pointer-events-none uppercase tracking-widest z-20 mix-blend-difference">
                MADNESS
              </span>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
