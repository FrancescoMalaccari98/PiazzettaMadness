import { useState, useEffect, useRef } from "react";
import { Link, useLocation } from "react-router-dom";
import { cn } from "../lib/utils";

// SVG inline: lucide-react e motion fuori dal critical path (vedi commento originale)
const navLinks: { name: string; path: string; live?: boolean }[] = [
  { name: "Home", path: "/" },
  { name: "Info", path: "/info" },
  { name: "Foto", path: "/foto" },
  { name: "Match", path: "/match" },
  { name: "Stat.", path: "/statistiche" },
  { name: "Staff", path: "/staff" },
  { name: "Player", path: "/giocatori" },
  { name: "Sponsor", path: "/sponsor" },
  { name: "Live", path: "/scoreboard", live: true },
];

const MenuIcon = () => (
  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <line x1="4" x2="20" y1="12" y2="12"/><line x1="4" x2="20" y1="6" y2="6"/><line x1="4" x2="20" y1="18" y2="18"/>
  </svg>
);

const XIcon = () => (
  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <path d="M18 6 6 18"/><path d="m6 6 12 12"/>
  </svg>
);

const InstagramIcon = ({ size = 24 }: { size?: number }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <rect width="20" height="20" x="2" y="2" rx="5" ry="5"/>
    <path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z"/>
    <line x1="17.5" x2="17.51" y1="6.5" y2="6.5"/>
  </svg>
);

export function Navigation() {
  const [isOpen, setIsOpen] = useState(false);
  const [scrolled, setScrolled] = useState(false);
  const [visible, setVisible] = useState(true);
  const lastScrollY = useRef(0);
  const location = useLocation();

  // Chiude il menu mobile al cambio di route
  useEffect(() => {
    setIsOpen(false);
  }, [location.pathname]);

  // Nasconde la navbar sullo scroll verso il basso, la mostra verso l'alto
  useEffect(() => {
    const handleScroll = () => {
      const currentY = window.scrollY;
      setScrolled(currentY > 30);

      if (currentY < 50 || currentY < lastScrollY.current) {
        setVisible(true);
      } else if (currentY > lastScrollY.current && currentY > 120) {
        setVisible(false);
        setIsOpen(false);
      }
      lastScrollY.current = currentY;
    };

    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => window.removeEventListener("scroll", handleScroll);
  }, []);

  return (
    <nav
      className={cn(
        "fixed top-0 left-0 right-0 w-full z-50 transition-all duration-300 ease-in-out",
        scrolled
          ? "bg-zinc-950/95 backdrop-blur-md border-b-2 border-brand-orange/40 shadow-[0_4px_24px_rgba(0,0,0,0.5)]"
          : "bg-gradient-to-b from-zinc-950/70 to-transparent",
        visible ? "translate-y-0" : "-translate-y-full"
      )}
    >
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className={cn(
          "flex items-center justify-between transition-all duration-300",
          scrolled ? "h-14" : "h-20"
        )}>

          {/* Logo + brand */}
          <Link
            to="/"
            className="flex-shrink-0 flex items-center gap-3 group"
            onClick={() => setIsOpen(false)}
          >
            <img
              src="/assets/logo.png"
              alt="Piazzetta Madness Logo"
              className={cn(
                "w-auto transition-all duration-300",
                scrolled ? "h-9" : "h-14"
              )}
              style={{ animation: 'logoPulse 8s linear infinite' }}
            />
            <div className={cn(
              "font-display uppercase leading-[0.85] border-l-[3px] border-brand-orange pl-3 text-white transition-all duration-300",
              scrolled ? "text-lg" : "text-2xl"
            )}>
              PIAZZETTA<br />MADNESS
            </div>
          </Link>

          {/* Desktop links */}
          <div className="hidden md:flex items-center gap-7">
            {navLinks.map((link) => (
              <Link
                key={link.name}
                to={link.path}
                className={cn(
                  "relative font-sans text-sm font-extrabold uppercase tracking-wide transition-colors duration-200 group py-1 flex items-center gap-1.5",
                  location.pathname === link.path
                    ? "text-brand-orange"
                    : "text-zinc-300 hover:text-white"
                )}
              >
                {link.live && (
                  <span className="relative flex h-2 w-2">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-500 opacity-75" />
                    <span className="relative inline-flex rounded-full h-2 w-2 bg-red-500" />
                  </span>
                )}
                {link.name}
                <span className={cn(
                  "absolute bottom-0 left-0 h-[2px] bg-brand-orange transition-all duration-300",
                  location.pathname === link.path
                    ? "w-full"
                    : "w-0 group-hover:w-full"
                )} />
              </Link>
            ))}

            <a
              href="https://www.instagram.com/piazzetta_madness/"
              target="_blank"
              rel="noreferrer"
              className="ml-2 flex items-center gap-2 text-zinc-300 hover:text-brand-orange transition-colors border border-zinc-700 hover:border-brand-orange bg-zinc-900/50 hover:bg-zinc-800/60 px-4 py-2"
            >
              <InstagramIcon size={15} />
              <span className="text-xs font-display uppercase tracking-widest">Instagram</span>
            </a>
          </div>

          {/* Mobile button */}
          <button
            onClick={() => setIsOpen(!isOpen)}
            aria-expanded={isOpen}
            aria-label="Apri menu"
            className="md:hidden p-2 text-zinc-300 hover:text-white hover:bg-zinc-800/60 rounded-sm transition-colors"
          >
            {isOpen ? <XIcon /> : <MenuIcon />}
          </button>
        </div>
      </div>

      {/* Mobile menu — CSS transition, no motion */}
      <div
        aria-hidden={!isOpen}
        className={cn(
          "md:hidden absolute w-full bg-zinc-950/98 backdrop-blur-md border-b-2 border-brand-orange/50",
          "transition-all duration-200 overflow-hidden",
          isOpen ? "opacity-100 translate-y-0" : "opacity-0 -translate-y-3 pointer-events-none"
        )}
      >
        <div className="px-4 py-4 space-y-1">
          {navLinks.map((link) => (
            <Link
              key={link.name}
              to={link.path}
              onClick={() => setIsOpen(false)}
              className={cn(
                "flex items-center gap-3 px-4 py-3 font-display text-lg tracking-wider uppercase transition-all border-l-4",
                location.pathname === link.path
                  ? "text-brand-orange border-brand-orange bg-brand-orange/5"
                  : "text-zinc-300 border-transparent hover:text-white hover:border-zinc-600 hover:bg-zinc-800/40"
              )}
            >
              {link.live && (
                <span className="relative flex h-2 w-2">
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-500 opacity-75" />
                  <span className="relative inline-flex rounded-full h-2 w-2 bg-red-500" />
                </span>
              )}
              {link.name}
            </Link>
          ))}
          <a
            href="https://www.instagram.com/piazzetta_madness/"
            target="_blank"
            rel="noreferrer"
            className="flex items-center gap-3 px-4 py-3 border-l-4 border-transparent text-zinc-400 hover:text-brand-orange transition-colors"
          >
            <InstagramIcon size={18} />
            <span className="font-display text-lg tracking-wider uppercase">Instagram</span>
          </a>
        </div>
      </div>
    </nav>
  );
}
