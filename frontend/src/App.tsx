import { lazy, Suspense, useState } from "react";
import { Routes, Route, useLocation } from "react-router-dom";
import { SplashScreen } from "./components/SplashScreen";
// Navigation, Footer, ScrollToTop e CookieBanner sono sempre visibili: importazione diretta.
import { Navigation } from "./components/Navigation";
import { Footer } from "./components/Footer";
import { ScrollToTop } from "./components/ScrollToTop";
import { CookieBanner } from "./components/CookieBanner";

// Home è importata direttamente: è sempre la prima pagina che l'utente vede,
// quindi deve essere pronta subito senza aspettare un chunk separato.
import { Home } from "./pages/Home";

// Tutte le altre pagine sono lazy: vengono scaricate solo quando l'utente ci naviga,
// riducendo il bundle iniziale di ~2MB (non minificati) / ~200KB in produzione.
const Info = lazy(() => import("./pages/Info").then(m => ({ default: m.Info })));
const Photos = lazy(() => import("./pages/Photos").then(m => ({ default: m.Photos })));
const Stats = lazy(() => import("./pages/Stats").then(m => ({ default: m.Stats })));
const Matches = lazy(() => import("./pages/Matches").then(m => ({ default: m.Matches })));
const Staff = lazy(() => import("./pages/Staff").then(m => ({ default: m.Staff })));
const Scoreboard = lazy(() => import("./pages/Scoreboard").then(m => ({ default: m.Scoreboard })));
const Live = lazy(() => import("./pages/Live").then(m => ({ default: m.Live })));
const Projection = lazy(() => import("./pages/Projection").then(m => ({ default: m.Projection })));
const Privacy = lazy(() => import("./pages/Privacy").then(m => ({ default: m.Privacy })));
const Sponsors = lazy(() => import("./pages/Sponsors").then(m => ({ default: m.Sponsors })));
const PlayerDetail = lazy(() => import("./pages/PlayerDetail").then(m => ({ default: m.PlayerDetail })));
const Players = lazy(() => import("./pages/Players").then(m => ({ default: m.Players })));
const ThreePointContest = lazy(() => import("./pages/ThreePointContest").then(m => ({ default: m.ThreePointContest })));
const Winners = lazy(() => import("./pages/Winners").then(m => ({ default: m.Winners })));

export function App() {
  const location = useLocation();
  const isProjection = location.pathname === "/projection";
  const [showSplash, setShowSplash] = useState(() => {
    if (sessionStorage.getItem("pm-splash-seen")) return false;
    sessionStorage.setItem("pm-splash-seen", "1");
    return true;
  });

  return (
    <div className="flex flex-col min-h-screen bg-brand-bg overflow-x-hidden">
      {showSplash && <SplashScreen onDone={() => setShowSplash(false)} />}
      {!isProjection && <ScrollToTop />}
      {!isProjection && <Navigation />}
      <main className="flex-1">
        {/* Fallback trasparente: evita un flash bianco durante il download del chunk di pagina */}
        <Suspense fallback={<div className="min-h-screen bg-brand-bg" />}>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/info" element={<Info />} />
            <Route path="/foto" element={<Photos />} />
            <Route path="/statistiche" element={<Stats />} />
            <Route path="/statistiche/classifiche" element={<Stats />} />
            <Route path="/statistiche/rosters" element={<Stats />} />
            <Route path="/match" element={<Matches />} />
            <Route path="/match/calendario" element={<Matches />} />
            <Route path="/match/gironi" element={<Matches />} />
            <Route path="/match/bracket" element={<Matches />} />
            <Route path="/staff" element={<Staff />} />
            <Route path="/live" element={<Live />} />
            <Route path="/scoreboard" element={<Scoreboard />} />
            <Route path="/projection" element={<Projection />} />
            <Route path="/privacy" element={<Privacy />} />
            <Route path="/sponsor" element={<Sponsors />} />
            <Route path="/statistiche/:slug" element={<PlayerDetail />} />
            <Route path="/giocatori" element={<Players />} />
            <Route path="/3pt" element={<ThreePointContest />} />
            <Route path="/winners" element={<Winners />} />
            <Route path="*" element={
              <div className="pt-40 pb-20 text-center">
                <p className="font-display text-[80px] md:text-[120px] text-brand-orange uppercase leading-none tracking-[-4px] mb-4">404</p>
                <p className="font-display text-2xl text-zinc-500 uppercase tracking-widest mb-8">Pagina non trovata</p>
                <a href="/" className="font-display text-sm uppercase tracking-widest text-brand-orange hover:underline">← Torna alla home</a>
              </div>
            } />
          </Routes>
        </Suspense>
      </main>
      {!isProjection && <Footer />}
      {/* Il banner cookie non appare nella schermata di proiezione */}
      {!isProjection && <CookieBanner />}
    </div>
  );
}

export default App;
