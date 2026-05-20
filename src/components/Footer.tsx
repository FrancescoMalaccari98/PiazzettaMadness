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

const ArrowIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <path d="M7 17 17 7"/><path d="M7 7h10v10"/>
  </svg>
);

export function Footer() {
  return (
    <footer className="relative bg-brand-orange overflow-hidden text-brand-bg">

      {/* Striscia nera in cima — stile divisa sportiva */}
      <div className="h-4 bg-zinc-950 w-full" />

      {/* Testo decorativo di sfondo */}
      <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none overflow-hidden">
        <span className="font-display font-black text-[22vw] uppercase text-brand-bg/[0.04] whitespace-nowrap tracking-tighter leading-none">
          MADNESS
        </span>
      </div>

      <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
        <div className="grid grid-cols-1 md:grid-cols-12 gap-12">

          {/* Colonna brand — più ampia */}
          <div className="md:col-span-5">
            <div className="flex items-center gap-4 mb-6">
              {/* brightness(0) converte il logo bianco in nero sul fondo arancione */}
              <img
                src="/logo.png"
                alt="Piazzetta Madness"
                className="h-16 w-auto"
                style={{ filter: "brightness(0)" }}
              />
              <div className="font-display text-4xl uppercase leading-[0.82] border-l-4 border-brand-bg pl-4 text-brand-bg">
                PIAZZETTA<br />MADNESS
              </div>
            </div>

            <p className="font-sans font-semibold text-brand-bg/75 text-base leading-relaxed mb-8 max-w-sm">
              Il torneo di basket 5vs5 più duro dell'asfalto.<br />
              Raduna la tua crew e conquista la gloria.
            </p>

            {/* CTA social */}
            <div className="flex flex-wrap gap-3">
              <a
                href="https://www.instagram.com/piazzetta_madness/"
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-2 bg-brand-bg text-white px-5 py-3 font-display uppercase text-sm tracking-widest hover:bg-zinc-800 transition-colors shadow-[4px_4px_0_rgba(0,0,0,0.2)]"
              >
                <InstagramIcon />
                @piazzetta_madness
              </a>
              <a
                href="mailto:info@piazzettamadness.it"
                className="inline-flex items-center gap-2 border-[3px] border-brand-bg text-brand-bg px-5 py-3 font-display uppercase text-sm tracking-widest hover:bg-brand-bg hover:text-white transition-colors"
              >
                <MailIcon />
                Scrivici
              </a>
            </div>
          </div>

          {/* Colonna navigazione */}
          <div className="md:col-span-3 md:col-start-7">
            <h4 className="font-display text-xs uppercase tracking-[0.2em] text-brand-bg/50 mb-5 pb-2 border-b-2 border-brand-bg/20">
              Naviga
            </h4>
            <ul className="space-y-3">
              {[
                { label: "Home", to: "/" },
                { label: "Info & Regolamento", to: "/info" },
                { label: "Foto", to: "/foto" },
                { label: "Match & Bracket", to: "/match" },
                { label: "Statistiche", to: "/statistiche" },
                { label: "Staff", to: "/staff" },
                { label: "Player", to: "/giocatori" },
                { label: "Sponsor", to: "/sponsor" },
              ].map(({ label, to }) => (
                <li key={to}>
                  <Link
                    to={to}
                    className="group inline-flex items-center gap-2 font-sans font-bold text-brand-bg/75 text-sm hover:text-brand-bg transition-colors"
                  >
                    <span className="opacity-0 group-hover:opacity-100 transition-opacity -translate-x-1 group-hover:translate-x-0 duration-200">
                      <ArrowIcon />
                    </span>
                    {label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          {/* Colonna legale */}
          <div className="md:col-span-2">
            <h4 className="font-display text-xs uppercase tracking-[0.2em] text-brand-bg/50 mb-5 pb-2 border-b-2 border-brand-bg/20">
              Legale
            </h4>
            <ul className="space-y-3">
              {[
                { label: "Privacy & Cookie Policy", to: "/privacy" },
              ].map(({ label, to }) => (
                <li key={label}>
                  <Link
                    to={to}
                    className="group inline-flex items-center gap-2 font-sans font-bold text-brand-bg/75 text-sm hover:text-brand-bg transition-colors"
                  >
                    <span className="opacity-0 group-hover:opacity-100 transition-opacity -translate-x-1 group-hover:translate-x-0 duration-200">
                      <ArrowIcon />
                    </span>
                    {label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

        </div>
      </div>

      {/* Copyright — stesso sfondo arancio, niente card scura */}
      <div className="relative z-10 border-t-2 border-brand-bg/20 py-5">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col sm:flex-row justify-between items-center gap-2">
          <p className="font-sans font-semibold text-brand-bg/50 text-xs uppercase tracking-widest">
            © {new Date().getFullYear()} Piazzetta Madness — Porto Potenza Picena
          </p>
          <p className="font-sans font-semibold text-brand-bg/35 text-xs uppercase tracking-widest">
            Estate 2026
          </p>
        </div>
      </div>
    </footer>
  );
}
