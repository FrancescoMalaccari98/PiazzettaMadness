import { Link } from "react-router-dom";

const InstagramIcon = () => (
  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <rect width="20" height="20" x="2" y="2" rx="5" ry="5"/>
    <path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z"/>
    <line x1="17.5" x2="17.51" y1="6.5" y2="6.5"/>
  </svg>
);

const MailIcon = () => (
  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <rect width="20" height="16" x="2" y="4" rx="2"/>
    <path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7"/>
  </svg>
);

export function Footer() {

  return (
    <footer className="relative bg-brand-orange overflow-hidden text-brand-bg">

      {/* Striscia nera in cima */}
      <div className="h-4 bg-zinc-950 w-full" />

      {/* Testo decorativo di sfondo */}
      <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none overflow-hidden">
        <span className="font-display font-black text-[22vw] uppercase text-brand-bg/[0.04] whitespace-nowrap tracking-tighter leading-none">
          MADNESS
        </span>
      </div>

      <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 md:py-14">
        <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-8">

          {/* Brand + descrizione */}
          <div>
            <div className="flex items-center gap-3 mb-4">
              <img
                src="/assets/logo.png"
                alt="Piazzetta Madness"
                className="h-10 md:h-14 w-auto"
                style={{ filter: "brightness(0)" }}
              />
              <div className="font-display text-2xl md:text-3xl uppercase leading-[0.82] border-l-4 border-brand-bg pl-3 md:pl-4 text-brand-bg">
                PIAZZETTA<br />MADNESS
              </div>
            </div>
            <p className="font-sans font-semibold text-brand-bg/75 text-sm md:text-base leading-relaxed max-w-sm">
              Il torneo di basket 5vs5 più duro dell'asfalto.<br />
              Raduna la tua crew e conquista la gloria.
            </p>
          </div>

          {/* Bottoni CTA */}
          <div className="flex flex-wrap gap-2 md:gap-3 shrink-0">
            <a
              href="https://www.instagram.com/piazzetta_madness/"
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-2 bg-brand-bg text-white px-4 py-2.5 md:px-5 md:py-3 font-display uppercase text-xs md:text-sm tracking-widest hover:bg-zinc-800 transition-colors shadow-[4px_4px_0_rgba(0,0,0,0.2)]"
            >
              <InstagramIcon />
              @piazzetta_madness
            </a>
            <a
              href="mailto:info@piazzettamadness.it"
              className="inline-flex items-center gap-2 border-[3px] border-brand-bg text-brand-bg px-4 py-2.5 md:px-5 md:py-3 font-display uppercase text-xs md:text-sm tracking-widest hover:bg-brand-bg hover:text-white transition-colors"
            >
              <MailIcon />
              Contattaci
            </a>
          </div>

        </div>
      </div>

      {/* Copyright */}
      <div className="relative z-10 border-t-2 border-brand-bg/20 py-5">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col sm:flex-row justify-between items-center gap-2">
          <p className="font-sans font-semibold text-brand-bg/50 text-xs uppercase tracking-widest">
            <span className="hidden sm:inline">© {new Date().getFullYear()} Piazzetta Madness — Porto Potenza Picena</span>
            <span className="sm:hidden">© {new Date().getFullYear()} Piazzetta Madness</span>
          </p>
          <div className="flex items-center gap-4">
            <Link
              to="/privacy"
              className="font-sans font-semibold text-brand-bg/50 text-xs uppercase tracking-widest hover:text-brand-bg transition-colors"
            >
              Privacy & Cookie Policy
            </Link>
            <button
              onClick={() => { localStorage.removeItem("pm_cookie_consent"); window.location.reload(); }}
              className="font-sans font-semibold text-brand-bg/50 text-xs uppercase tracking-widest hover:text-brand-bg transition-colors bg-transparent"
            >
              Gestisci Cookie
            </button>
          </div>
        </div>
      </div>

      {/* ── SEZIONI RIMOSSE — commentate per futura re-abilitazione ──

      Colonna navigazione:
      <div className="col-span-1 md:col-span-3 md:col-start-7">
        <h4>Naviga</h4>
        <ul>Home / Info / Foto / Match / Statistiche / Staff / Player / Sponsor</ul>
      </div>

      Colonna legale (ora solo Privacy nel copyright bar):
      <div className="col-span-1 md:col-span-2">
        <h4>Legale</h4>
        <ul>Privacy & Cookie Policy / Gestisci Cookie</ul>
      </div>

      ── */}

    </footer>
  );
}
