import { lazy, Suspense } from "react";
import { Routes, Route, useLocation } from "react-router-dom";
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
const Registration = lazy(() => import("./pages/Registration").then(m => ({ default: m.Registration })));
const Photos = lazy(() => import("./pages/Photos").then(m => ({ default: m.Photos })));
const Stats = lazy(() => import("./pages/Stats").then(m => ({ default: m.Stats })));
const Matches = lazy(() => import("./pages/Matches").then(m => ({ default: m.Matches })));
const Staff = lazy(() => import("./pages/Staff").then(m => ({ default: m.Staff })));
const Scoreboard = lazy(() => import("./pages/Scoreboard").then(m => ({ default: m.Scoreboard })));
const Projection = lazy(() => import("./pages/Projection").then(m => ({ default: m.Projection })));
const Privacy = lazy(() => import("./pages/Privacy").then(m => ({ default: m.Privacy })));
const Sponsors = lazy(() => import("./pages/Sponsors").then(m => ({ default: m.Sponsors })));

export function App() {
  const location = useLocation();
  const isProjection = location.pathname === "/projection";

  return (
    <div className="flex flex-col min-h-screen bg-brand-bg">
      {!isProjection && <ScrollToTop />}
      {!isProjection && <Navigation />}
      <main className="flex-1">
        {/* Fallback trasparente: evita un flash bianco durante il download del chunk di pagina */}
        <Suspense fallback={<div className="min-h-screen bg-brand-bg" />}>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/info" element={<Info />} />
            <Route path="/iscrizioni" element={<Registration />} />
            <Route path="/foto" element={<Photos />} />
            <Route path="/statistiche" element={<Stats />} />
            <Route path="/match" element={<Matches />} />
            <Route path="/staff" element={<Staff />} />
            <Route path="/scoreboard" element={<Scoreboard />} />
            <Route path="/projection" element={<Projection />} />
            <Route path="/privacy" element={<Privacy />} />
            <Route path="/sponsor" element={<Sponsors />} />
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
