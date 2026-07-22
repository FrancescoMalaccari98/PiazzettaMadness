import { useState, useEffect } from "react";
import { motion } from "motion/react";
import { ExternalLink } from "lucide-react";

const API = import.meta.env.VITE_API_URL ?? "";

type Sponsor = {
  id: number;
  nome: string;
  logo: string;
  sito: string;
  ig: string;
};

const IgIcon = ({ size = 14 }: { size?: number }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="2" y="2" width="20" height="20" rx="5" ry="5"/>
    <path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z"/>
    <line x1="17.5" y1="6.5" x2="17.51" y2="6.5"/>
  </svg>
);

const CARD_COLORS = [
  { border: "border-brand-orange", shadow: "shadow-[6px_6px_0_var(--color-brand-orange)]", text: "text-brand-orange" },
  { border: "border-brand-yellow", shadow: "shadow-[6px_6px_0_var(--color-brand-yellow)]", text: "text-brand-yellow" },
  { border: "border-brand-blue",   shadow: "shadow-[6px_6px_0_var(--color-brand-blue)]",   text: "text-brand-blue" },
  { border: "border-zinc-400",     shadow: "shadow-[6px_6px_0_rgba(161,161,170,0.5)]",     text: "text-zinc-300" },
] as const;

function SponsorCard({ sponsor, index }: { sponsor: Sponsor; index: number }) {
  const color = CARD_COLORS[index % CARD_COLORS.length];
  const hasSito = !!sponsor.sito;
  const hasIg = !!sponsor.ig;
  const hasLinks = hasSito || hasIg;
  const igUrl = sponsor.ig.startsWith("http") ? sponsor.ig : `https://instagram.com/${sponsor.ig}`;
  const clickHref = hasSito ? sponsor.sito : (hasIg ? igUrl : "");
  const hasClick = hasSito || hasIg;

  const Wrapper = hasClick ? "a" : "div";
  const wrapperProps = hasClick ? {
    href: clickHref,
    target: "_blank" as const,
    rel: "noreferrer",
  } : {};

  return (
    <div className={`group flex flex-col bg-zinc-900 border-[4px] ${color.border} ${color.shadow} overflow-hidden`}>

      {/* Logo */}
      <Wrapper
        {...wrapperProps}
        className="flex items-center justify-center h-44 sm:h-56 p-6 relative cursor-pointer bg-[#d1d1d1]"
      >
        <img
          src={sponsor.logo}
          alt={sponsor.nome}
          className="max-h-full max-w-full object-contain group-hover:scale-105 transition-transform duration-300"
          loading="lazy"
        />
        {hasClick && (
          <ExternalLink className={`absolute top-3 right-3 w-4 h-4 ${color.text} opacity-0 group-hover:opacity-100 transition-opacity`} />
        )}
      </Wrapper>

      {/* Footer: nome + link */}
      <div className="px-4 py-4 bg-zinc-950/80 flex items-center justify-between gap-3 min-w-0">
        {hasClick ? (
          <a href={clickHref} target="_blank" rel="noreferrer" className={`font-display text-lg xl:text-xl uppercase leading-tight ${color.text} min-w-0 truncate hover:underline`}>
            {sponsor.nome}
          </a>
        ) : (
          <p className={`font-display text-lg xl:text-xl uppercase leading-tight ${color.text} min-w-0 truncate`}>
            {sponsor.nome}
          </p>
        )}

        {hasLinks && (
          <div className="flex items-center gap-3 shrink-0">
            {hasSito && (
              <a
                href={sponsor.sito}
                target="_blank"
                rel="noreferrer"
                onClick={e => e.stopPropagation()}
                className="text-zinc-400 hover:text-white transition-colors"
                title="Sito ufficiale"
              >
                <ExternalLink className="w-5 h-5" />
              </a>
            )}
            {hasIg && (
              <a
                href={igUrl}
                target="_blank"
                rel="noreferrer"
                onClick={e => e.stopPropagation()}
                className="text-zinc-400 hover:text-white transition-colors"
                title="Instagram"
              >
                <IgIcon size={20} />
              </a>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export function Sponsors() {
  const [sponsors, setSponsors] = useState<Sponsor[]>([]);

  useEffect(() => {
    fetch(`${API}/api-web/sponsor`)
      .then(r => r.ok ? r.json() as Promise<Sponsor[]> : null)
      .then(data => { if (data) setSponsors(data); })
      .catch(() => {});
  }, []);

  return (
    <div className="w-full min-h-screen bg-brand-bg pt-28 pb-24">

      {/* Header */}
      <div className="relative overflow-hidden border-b-[4px] border-zinc-800 pb-16 mb-20">
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none overflow-hidden">
          <span className="font-display font-black text-[18vw] uppercase text-white/[0.025] whitespace-nowrap tracking-tighter leading-none">
            SPONSOR
          </span>
        </div>
        <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <p className="font-display text-brand-orange uppercase tracking-[0.3em] text-sm mb-4">
            Piazzetta Madness 2026
          </p>
          <h1 className="font-display text-[44px] sm:text-[70px] md:text-[120px] uppercase leading-[0.8] tracking-[-2px] md:tracking-[-4px] text-white mb-6">
            I Nostri<br />
            <span className="text-brand-orange">Sponsor</span>
          </h1>
          <p className="font-sans text-zinc-400 text-sm sm:text-lg max-w-[260px] sm:max-w-xl mx-auto leading-relaxed">
            Senza di loro Piazzetta Madness non esisterebbe.<br />Scegli chi crede nel campetto.
          </p>
        </div>
      </div>

      {/* Griglia sponsor */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {sponsors.length === 0 ? (
          <div className="text-center py-16">
            <img src="/assets/logo.png" alt="" className="w-16 h-16 mx-auto mb-6 opacity-30" />
            <p className="font-display text-2xl uppercase text-zinc-600">Sponsor in arrivo</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
            {sponsors.map((sponsor, i) => (
              <motion.div
                key={sponsor.id}
                initial={{ opacity: 0, scale: 0.9 }}
                whileInView={{ opacity: 1, scale: 1 }}
                viewport={{ once: true, margin: "-40px" }}
                transition={{ delay: (i % 5) * 0.07, duration: 0.4, ease: "easeOut" }}
              >
                <SponsorCard sponsor={sponsor} index={i} />
              </motion.div>
            ))}
          </div>
        )}
      </div>

      {/* CTA */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mt-24">
        <div className="border-[4px] border-brand-orange bg-brand-orange/5 p-10 md:p-14 flex flex-col md:flex-row items-start md:items-center justify-between gap-8">
          <div>
            <h3 className="font-display text-3xl md:text-4xl uppercase text-white mb-2">
              Vuoi collaborare con noi?
            </h3>
            <p className="font-sans text-zinc-400 text-base max-w-md">
              Contattaci per scoprire i pacchetti disponibili e portare il tuo brand in campo.
            </p>
          </div>
          <a
            href="mailto:info@piazzettamadness.it"
            className="shrink-0 bg-brand-orange text-brand-bg px-8 py-4 font-display text-xl uppercase tracking-widest hover:bg-white transition-colors shadow-[6px_6px_0_rgba(0,0,0,0.3)]"
          >
            Contattaci
          </a>
        </div>
      </div>

    </div>
  );
}
