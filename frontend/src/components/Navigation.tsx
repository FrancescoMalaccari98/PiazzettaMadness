import { useState, useEffect, useRef } from "react";
import { Link, useLocation } from "react-router-dom";
import { ChevronDown } from "lucide-react";
import { cn } from "../lib/utils";

type NavLink = {
  name: string;
  path: string;
  live?: boolean;
  children?: { name: string; path: string }[];
};

const matchSubLinks = [
  { name: "Calendario", path: "/match/calendario" },
  { name: "Fase a Gironi", path: "/match/gironi" },
  { name: "Playoff Bracket", path: "/match/bracket" },
];

const statsSubLinks = [
  { name: "Classifiche Individuali", path: "/statistiche/classifiche" },
  { name: "Rosters per Squadra", path: "/statistiche/rosters" },
];

const leagueSubLinks = [
  { name: "Info", path: "/info" },
  { name: "Rosters", path: "/giocatori" },
  { name: "3PTs", path: "/3pt" },
  { name: "Winners", path: "/winners" },
];

const communitySubLinks = [
  { name: "Pics", path: "/foto" },
  { name: "Crew", path: "/staff" },
];

const navLinks: NavLink[] = [
  { name: "Home", path: "/" },
  { name: "League", path: "/info", children: leagueSubLinks },
  { name: "Matches", path: "/match/calendario", children: matchSubLinks },
  { name: "Live", path: "/live", live: true },
  { name: "Stats", path: "/statistiche", children: statsSubLinks },
  { name: "Community", path: "/foto", children: communitySubLinks },
  { name: "Sponsor", path: "/sponsor" },
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
  const [desktopOpenMenu, setDesktopOpenMenu] = useState<string | null>(null);
  const [mobileOpenMenu, setMobileOpenMenu] = useState<string | null>(null);
  const [scrolled, setScrolled] = useState(false);
  const [visible, setVisible] = useState(true);
  const lastScrollY = useRef(0);
  const location = useLocation();

  const isPathActive = (path: string) => {
    if (path === "/") return location.pathname === "/";
    return location.pathname === path || location.pathname.startsWith(`${path}/`);
  };

  const isLinkActive = (link: NavLink) => {
    if (link.children) {
      return isPathActive(link.path) || link.children.some(child => isPathActive(child.path));
    }
    return isPathActive(link.path);
  };

  useEffect(() => {
    setIsOpen(false);
    setDesktopOpenMenu(null);
    setMobileOpenMenu(null);
  }, [location.pathname, location.search, location.hash]);

  useEffect(() => {
    if (!isOpen) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") setIsOpen(false);
    };

    const handleResize = () => {
      if (window.matchMedia("(min-width: 768px)").matches) {
        setIsOpen(false);
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    window.addEventListener("resize", handleResize);

    return () => {
      window.removeEventListener("keydown", handleKeyDown);
      window.removeEventListener("resize", handleResize);
    };
  }, [isOpen]);

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
        "fixed top-0 left-0 right-0 w-full z-50 transition-transform duration-300 ease-in-out",
        visible ? "translate-y-0" : "-translate-y-full"
      )}
    >
      {/* ── Layer sfondo: opacity transition (composited) ── */}
      <div
        className="absolute inset-0 bg-gradient-to-b from-zinc-950/70 to-transparent transition-opacity duration-300 pointer-events-none"
        style={{ opacity: scrolled ? 0 : 1 }}
      />
      <div
        className="absolute inset-0 bg-zinc-950/95 backdrop-blur-md border-b-2 border-brand-orange/40 shadow-[0_4px_24px_rgba(0,0,0,0.5)] transition-opacity duration-300 pointer-events-none"
        style={{ opacity: scrolled ? 1 : 0 }}
      />

      <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className={cn(
          "flex items-center justify-between transition-[height] duration-300",
          scrolled ? "h-14" : "h-20"
        )}>

          {/* Logo + brand */}
          <Link
            to="/"
            className="flex-shrink-0 flex items-center gap-3 group"
            onClick={() => setIsOpen(false)}
          >
            {/* wrapper gestisce il resize scroll (scale composited) */}
            <div
              className="origin-left transition-transform duration-300"
              style={{ transform: scrolled ? 'scale(0.643)' : 'scale(1)' }}
            >
              <img
                src="/assets/logo.png"
                alt="Piazzetta Madness Logo"
                width={56}
                height={56}
                className="w-auto h-14"
              />
            </div>
            <div
              className="font-display uppercase leading-[0.85] border-l-[3px] border-brand-orange pl-3 text-white text-2xl origin-left transition-transform duration-300"
              style={{ transform: scrolled ? 'scale(0.75)' : 'scale(1)' }}
            >
              PIAZZETTA<br />MADNESS
            </div>
          </Link>

          {/* Desktop links */}
          <div className="hidden md:flex items-center gap-7">
            {navLinks.map((link) => {
              const active = isLinkActive(link);
              const desktopMenuOpen = desktopOpenMenu === link.name;

              if (link.children) {
                return (
                  <div
                    key={link.name}
                    className="relative"
                    onMouseEnter={() => setDesktopOpenMenu(link.name)}
                    onMouseLeave={() => setDesktopOpenMenu(null)}
                  >
                    <button
                      type="button"
                      onClick={() => setDesktopOpenMenu(open => open === link.name ? null : link.name)}
                      className={cn(
                        "relative font-sans text-sm font-extrabold uppercase tracking-wide transition-colors duration-200 group py-1 flex items-center gap-1.5",
                        active ? "text-brand-orange" : "text-zinc-300 hover:text-white"
                      )}
                      aria-expanded={desktopMenuOpen}
                    >
                      {link.name}
                      <ChevronDown className={cn("w-3.5 h-3.5 transition-transform", desktopMenuOpen ? "rotate-180" : "rotate-0")} />
                      <span className={cn(
                        "absolute bottom-0 left-0 h-[2px] bg-brand-orange transition-transform duration-300 origin-left",
                        active ? "scale-x-100" : "scale-x-0 group-hover:scale-x-100"
                      )} style={{ width: '100%' }} />
                    </button>

                    <div
                      className={cn(
                        "absolute left-1/2 top-full w-56 -translate-x-1/2 pt-3 transition-all duration-150",
                        desktopMenuOpen
                          ? "opacity-100 translate-y-0 pointer-events-auto"
                          : "opacity-0 -translate-y-2 pointer-events-none"
                      )}
                    >
                      <div className="border-[3px] border-zinc-800 bg-zinc-950 shadow-[8px_8px_0_var(--color-brand-orange)]">
                        {link.children.map(child => {
                          const childActive = location.pathname === child.path;
                          return (
                            <Link
                              key={child.path}
                              to={child.path}
                              className={cn(
                                "block px-4 py-3 border-b border-zinc-800 last:border-0 font-sans text-sm font-extrabold uppercase tracking-wide transition-colors",
                                childActive
                                  ? "text-brand-orange bg-brand-orange/10"
                                  : "text-zinc-400 hover:text-white hover:bg-zinc-900"
                              )}
                            >
                              {child.name}
                            </Link>
                          );
                        })}
                      </div>
                    </div>
                  </div>
                );
              }

              return (
                <Link
                  key={link.name}
                  to={link.path}
                  className={cn(
                    "relative font-sans text-sm font-extrabold uppercase tracking-wide transition-colors duration-200 group py-1 flex items-center gap-1.5",
                    active
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
                    "absolute bottom-0 left-0 h-[2px] bg-brand-orange transition-transform duration-300 origin-left",
                    active ? "scale-x-100" : "scale-x-0 group-hover:scale-x-100"
                  )} style={{ width: '100%' }} />
                </Link>
              );
            })}

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

      {/* Mobile menu */}
      {isOpen && (
        <div
          className="relative md:hidden w-full bg-zinc-950/98 backdrop-blur-md border-b-2 border-brand-orange/50 overflow-hidden"
        >
          <div className="px-4 py-4 space-y-1">
            {navLinks.map((link) => {
              const active = isLinkActive(link);
              const mobileMenuOpen = mobileOpenMenu === link.name;

              if (link.children) {
                return (
                  <div key={link.name}>
                    <button
                      type="button"
                      onClick={() => setMobileOpenMenu(open => open === link.name ? null : link.name)}
                      className={cn(
                        "w-full flex items-center justify-between gap-3 px-4 py-3 font-display text-lg tracking-wider uppercase transition-colors border-l-4",
                        active
                          ? "text-brand-orange border-brand-orange bg-brand-orange/5"
                          : "text-zinc-300 border-transparent hover:text-white hover:border-zinc-600 hover:bg-zinc-800/40"
                      )}
                      aria-expanded={mobileMenuOpen}
                    >
                      <span>{link.name}</span>
                      <ChevronDown className={cn("w-5 h-5 transition-transform", mobileMenuOpen ? "rotate-180" : "rotate-0")} />
                    </button>
                    {mobileMenuOpen && (
                      <div className="ml-4 mt-1 mb-2 border-l border-zinc-800">
                        {link.children.map(child => (
                          <Link
                            key={child.path}
                            to={child.path}
                            onClick={() => setIsOpen(false)}
                            className={cn(
                              "block px-4 py-3 font-display text-lg tracking-wider uppercase transition-colors",
                              location.pathname === child.path
                                ? "text-brand-orange"
                                : "text-zinc-500 hover:text-white"
                            )}
                          >
                            {child.name}
                          </Link>
                        ))}
                      </div>
                    )}
                  </div>
                );
              }

              return (
                <Link
                  key={link.name}
                  to={link.path}
                  onClick={() => setIsOpen(false)}
                  className={cn(
                    "flex items-center gap-3 px-4 py-3 font-display text-lg tracking-wider uppercase transition-colors border-l-4",
                    active
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
              );
            })}
            <a
              href="https://www.instagram.com/piazzetta_madness/"
              target="_blank"
              rel="noreferrer"
              onClick={() => setIsOpen(false)}
              className="flex items-center gap-3 px-4 py-3 border-l-4 border-transparent text-zinc-400 hover:text-brand-orange transition-colors"
            >
              <InstagramIcon size={18} />
              <span className="font-display text-lg tracking-wider uppercase">Instagram</span>
            </a>
          </div>
        </div>
      )}
    </nav>
  );
}
