import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { initGA } from "../lib/analytics";

const CONSENT_KEY = "pm_cookie_consent";

export type ConsentStatus = "accepted" | "necessary_only" | null;

export function getConsentStatus(): ConsentStatus {
  return localStorage.getItem(CONSENT_KEY) as ConsentStatus;
}

export function CookieBanner() {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const consent = localStorage.getItem(CONSENT_KEY);
    if (!consent) {
      setVisible(true);
    } else if (consent === "accepted") {
      // Utente di ritorno che aveva già accettato: init GA subito
      initGA();
    }
  }, []);

  const handleAccept = () => {
    localStorage.setItem(CONSENT_KEY, "accepted");
    initGA();
    setVisible(false);
  };

  const handleNecessaryOnly = () => {
    localStorage.setItem(CONSENT_KEY, "necessary_only");
    setVisible(false);
  };

  if (!visible) return null;

  return (
    <div
      role="dialog"
      aria-label="Consenso cookie"
      aria-modal="false"
      className="fixed bottom-0 left-0 right-0 z-[200] bg-zinc-950 border-t-4 border-brand-orange shadow-[0_-8px_30px_rgba(0,0,0,0.6)]"
    >
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-5 flex flex-col md:flex-row items-start md:items-center gap-5">

        <div className="flex-1 min-w-0">
          <p className="font-display text-sm uppercase tracking-widest text-brand-orange mb-1">
            Informativa Cookie
          </p>
          <p className="font-sans text-sm text-zinc-300 leading-relaxed">
            Questo sito usa cookie tecnici necessari e, con il tuo consenso, cookie analitici
            (Google Analytics) per misurare gli accessi. I tuoi dati sono trattati da{" "}
            <strong className="text-white">Francesco Emiliani</strong> nel rispetto del GDPR (Reg. UE 2016/679).{" "}
            <Link
              to="/privacy"
              className="text-brand-orange underline underline-offset-2 hover:text-white transition-colors"
            >
              Leggi l'informativa completa
            </Link>
          </p>
        </div>

        <div className="flex items-center gap-3 flex-shrink-0 w-full md:w-auto">
          <button
            onClick={handleNecessaryOnly}
            className="flex-1 md:flex-none px-5 py-3 border-2 border-zinc-600 text-zinc-300 font-display uppercase text-xs tracking-widest hover:border-white hover:text-white transition-colors"
          >
            Solo necessari
          </button>
          <button
            onClick={handleAccept}
            className="flex-1 md:flex-none px-6 py-3 bg-brand-orange text-brand-bg font-display uppercase text-xs tracking-widest hover:bg-white transition-colors"
          >
            Accetta tutti
          </button>
        </div>
      </div>
    </div>
  );
}
