import { useState, useEffect } from "react";
import { motion, AnimatePresence } from "motion/react";

export function SplashScreen({ onDone }: { onDone: () => void }) {
  const [phase, setPhase] = useState(0);
  const [exiting, setExiting] = useState(false);

  const skip = () => { setExiting(true); setTimeout(onDone, 350); };

  useEffect(() => {
    const t = [
      setTimeout(() => setPhase(1), 120),   // striscia arancione
      setTimeout(() => setPhase(2), 480),   // character bg + PIAZZETTA
      setTimeout(() => setPhase(3), 720),   // MADNESS
      setTimeout(() => setPhase(4), 1050),  // sottotitolo
      setTimeout(() => setPhase(5), 3000),  // shake
      setTimeout(() => setPhase(6), 3500),  // flash uscita
    ];
    const exit = setTimeout(() => { setExiting(true); setTimeout(onDone, 450); }, 3950);
    return () => { t.forEach(clearTimeout); clearTimeout(exit); };
  }, [onDone]);

  return (
    <AnimatePresence>
      {!exiting && (
        <motion.div
          exit={{ opacity: 0 }}
          transition={{ duration: 0.45 }}
          className="fixed inset-0 z-[9999] overflow-hidden select-none flex items-center justify-center"
        >
          {/* ── SFONDO CAMPETTO ── */}
          <img
            src="/assets/campetto.webp"
            alt=""
            className="absolute inset-0 w-full h-full object-cover grayscale"
            style={{ filter: "grayscale(1) brightness(0.25)" }}
            draggable={false}
          />

          {/* ── 1. STRISCIA ARANCIONE CHE SPAZZA ── */}
          <motion.div
            className="absolute inset-x-0 z-30 pointer-events-none"
            style={{ height: "6px", background: "#ea6324", top: 0 }}
            initial={{ scaleX: 0, y: 0 }}
            animate={phase >= 1
              ? { scaleX: 1, y: "100vh" }
              : { scaleX: 0, y: 0 }
            }
            transition={{ duration: 0.32, ease: [0.4, 0, 0.2, 1] }}
          />

          {/* ── 2. FADE-IN SFONDO ── */}
          <motion.div
            className="absolute inset-0 pointer-events-none z-0"
            initial={{ opacity: 0 }}
            animate={{ opacity: phase >= 2 ? 1 : 0 }}
            transition={{ duration: 0.6 }}
          >
          </motion.div>

          {/* ── LOGO — top left come brand stamp ── */}
          <motion.div
            className="absolute top-5 sm:top-7 left-5 sm:left-8 z-20 pointer-events-none flex items-center gap-2 sm:gap-3"
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: phase >= 1 ? 1 : 0, x: phase >= 1 ? 0 : -20 }}
            transition={{ duration: 0.4 }}
          >
            <img src="/assets/logo.png" alt="Piazzetta Madness" className="w-9 sm:w-14 h-auto" draggable={false} />
            <div className="hidden sm:flex flex-col leading-none">
              <span className="font-display text-white text-[10px] sm:text-xs uppercase tracking-[0.2em]">Piazzetta</span>
              <span className="font-display text-brand-orange text-[10px] sm:text-xs uppercase tracking-[0.2em]">Madness</span>
            </div>
          </motion.div>

          {/* ── Contenuto testo ── */}
          <div className="relative z-10 flex flex-col items-center text-center w-full px-3 sm:px-6">

            {/* PIAZZETTA — entra dall'alto */}
            <div className="overflow-hidden w-full">
              <motion.p
                className="font-display uppercase text-white leading-none w-full"
                style={{
                  fontSize: "clamp(48px, 13vw, 160px)",
                  letterSpacing: "clamp(-1px, -0.03em, -4px)",
                }}
                initial={{ y: "-110%" }}
                animate={{ y: phase >= 2 ? "0%" : "-110%" }}
                transition={{ type: "spring", stiffness: 320, damping: 30 }}
              >
                PIAZZETTA
              </motion.p>
            </div>

            {/* MADNESS — entra dal basso */}
            <div className="overflow-hidden w-full" style={{ marginTop: "clamp(-6px, -1vw, -16px)" }}>
              <motion.p
                className="font-display uppercase leading-none w-full"
                style={{
                  fontSize: "clamp(60px, 17vw, 210px)",
                  letterSpacing: "clamp(-2px, -0.04em, -6px)",
                  color: "#ea6324",
                  textShadow: phase >= 3
                    ? "0 0 80px rgba(234,99,36,0.45), 0 0 28px rgba(234,99,36,0.25)"
                    : "none",
                }}
                initial={{ y: "110%" }}
                animate={{ y: phase >= 3 ? "0%" : "110%" }}
                transition={{ type: "spring", stiffness: 300, damping: 26 }}
              >
                MADNESS
              </motion.p>
            </div>

            {/* Linea + sottotitolo */}
            <motion.div
              className="flex items-center gap-3 sm:gap-4 mt-4 sm:mt-6 w-full justify-center"
              initial={{ opacity: 0, y: 14 }}
              animate={{ opacity: phase >= 4 ? 1 : 0, y: phase >= 4 ? 0 : 14 }}
              transition={{ duration: 0.4 }}
            >
              <span className="block h-px flex-1 max-w-[60px] sm:max-w-[80px] bg-zinc-700" />
              <span className="font-sans text-[10px] sm:text-xs uppercase tracking-[0.25em] sm:tracking-[0.35em] text-zinc-500 whitespace-nowrap">
                Summer '26 &mdash; Street Basketball
              </span>
              <span className="block h-px flex-1 max-w-[60px] sm:max-w-[80px] bg-zinc-700" />
            </motion.div>
          </div>

          {/* ── 5. SHAKE al momento del flash ── */}
          <AnimatePresence>
            {phase >= 5 && (
              <motion.div
                key="shake"
                className="absolute inset-0 z-20 pointer-events-none"
                animate={{ x: [0, -6, 6, -4, 4, -2, 2, 0] }}
                transition={{ duration: 0.35, ease: "easeInOut" }}
              />
            )}
          </AnimatePresence>

          {/* ── 6. FLASH ARANCIONE USCITA ── */}
          <motion.div
            className="absolute inset-0 bg-brand-orange z-40 pointer-events-none"
            animate={{ opacity: phase >= 6 ? 1 : 0 }}
            transition={{ duration: 0.3 }}
          />

          {/* Skip — touch-friendly su mobile */}
          <button
            onClick={skip}
            className="absolute bottom-6 right-5 sm:bottom-8 sm:right-8 font-sans text-[11px] uppercase tracking-widest text-zinc-600 hover:text-zinc-300 transition-colors z-50 p-2"
          >
            Skip →
          </button>

        </motion.div>
      )}
    </AnimatePresence>
  );
}
