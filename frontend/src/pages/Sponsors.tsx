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

function SponsorCard({ sponsor }: { sponsor: Sponsor }) {
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
    <div className="group flex flex-col border-[3px] border-zinc-700 bg-zinc-900 hover:border-brand-orange transition-all duration-300 hover:-translate-y-1 hover:shadow-[8px_8px_0_var(--color-brand-orange)] overflow-hidden">

      {/* Logo */}
      <Wrapper
        {...wrapperProps}
        className="flex items-center justify-center h-40 sm:h-48 p-6 relative cursor-pointer bg-zinc-400"
      >
        <img
          src={sponsor.logo}
          alt={sponsor.nome}
          className="max-h-full max-w-full object-contain group-hover:scale-105 transition-transform duration-300"
          loading="lazy"
        />
        {hasClick && (
          <ExternalLink className="absolute top-3 right-3 w-4 h-4 text-brand-orange opacity-0 group-hover:opacity-100 transition-opacity" />
        )}
      </Wrapper>

      {/* Footer: nome + link */}
      <div className="border-t-[3px] border-zinc-800 px-4 py-3 bg-zinc-950/80">
        <p className="font-display text-sm uppercase tracking-wide text-brand-orange truncate mb-1">
          {sponsor.nome}
        </p>

        {hasLinks && (
          <div className="flex items-center gap-4">
            {hasSito && (
              <a
                href={sponsor.sito}
                target="_blank"
                rel="noreferrer"
                onClick={e => e.stopPropagation()}
                className="font-sans text-[10px] sm:text-xs uppercase tracking-wider text-zinc-400 hover:text-white transition-colors flex items-center gap-1.5"
              >
                <ExternalLink className="w-3 h-3" /> Sito ufficiale
              </a>
            )}
            {hasIg && (
              <a
                href={sponsor.ig.startsWith("http") ? sponsor.ig : `https://instagram.com/${sponsor.ig}`}
                target="_blank"
                rel="noreferrer"
                onClick={e => e.stopPropagation()}
                className="text-zinc-400 hover:text-white transition-colors flex items-center gap-1.5"
              >
                <IgIcon size={13} />
                <span className="font-sans text-[10px] sm:text-xs">Instagram</span>
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
          <p className="font-sans text-zinc-400 text-base sm:text-lg max-w-xl mx-auto">
            Senza di loro la Piazzetta Madness non esisterebbe. Supporta chi supporta noi.
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
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
            {sponsors.map((sponsor, i) => (
              <motion.div
                key={sponsor.id}
                initial={{ opacity: 0, scale: 0.9 }}
                whileInView={{ opacity: 1, scale: 1 }}
                viewport={{ once: true, margin: "-40px" }}
                transition={{ delay: (i % 5) * 0.07, duration: 0.4, ease: "easeOut" }}
              >
                <SponsorCard sponsor={sponsor} />
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
