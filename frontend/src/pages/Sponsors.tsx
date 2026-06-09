import { useState, useEffect } from "react";
import { motion } from "motion/react";
import { ExternalLink, Instagram } from "lucide-react";

const API = import.meta.env.VITE_API_URL ?? "";

type Sponsor = {
  id: number;
  nome: string;
  url: string;
  logo: string;
  ig?: string;
};

const defaultSponsors: Sponsor[] = [
  { id: 1,  nome: "Sponsor 1",  url: "#", logo: "" },
  { id: 2,  nome: "Sponsor 2",  url: "#", logo: "" },
  { id: 3,  nome: "Sponsor 3",  url: "#", logo: "" },
  { id: 4,  nome: "Sponsor 4",  url: "#", logo: "" },
  { id: 5,  nome: "Sponsor 5",  url: "#", logo: "" },
  { id: 6,  nome: "Sponsor 6",  url: "#", logo: "" },
  { id: 7,  nome: "Sponsor 7",  url: "#", logo: "" },
  { id: 8,  nome: "Sponsor 8",  url: "#", logo: "" },
  { id: 9,  nome: "Sponsor 9",  url: "#", logo: "" },
  { id: 10, nome: "Sponsor 10", url: "#", logo: "" },
];

function SponsorCard({ sponsor }: { sponsor: Sponsor }) {
  return (
    <div className="group relative flex flex-col border-[3px] border-zinc-700 bg-zinc-900 hover:border-brand-orange hover:bg-zinc-800 transition-all duration-300 hover:-translate-y-1 hover:shadow-[6px_6px_0_var(--color-brand-orange)] overflow-hidden">

      {/* Logo / nome cliccabile */}
      <a
        href={sponsor.url}
        target="_blank"
        rel="noreferrer"
        className="flex items-center justify-center h-36 md:h-44 p-6 flex-1"
      >
        {sponsor.logo ? (
          <img
            src={sponsor.logo}
            alt={sponsor.nome}
            className="max-h-[65%] max-w-[75%] object-contain filter brightness-0 invert opacity-60 group-hover:opacity-100 transition-opacity"
          />
        ) : (
          <span className="font-display text-lg uppercase tracking-wide text-zinc-500 group-hover:text-white transition-colors text-center leading-tight">
            {sponsor.nome}
          </span>
        )}
        <ExternalLink className="absolute top-2 right-2 w-3.5 h-3.5 text-brand-orange opacity-0 group-hover:opacity-100 transition-opacity" />
      </a>

      {/* Footer con nome + instagram se presente */}
      {(sponsor.nome || sponsor.ig) && (
        <div className="border-t border-zinc-800 px-4 py-2 flex items-center justify-between gap-2 bg-zinc-950/60">
          <span className="font-display text-xs uppercase tracking-wide text-zinc-500 group-hover:text-zinc-300 transition-colors truncate">
            {sponsor.nome}
          </span>
          {sponsor.ig && (
            <a
              href={`https://instagram.com/${sponsor.ig}`}
              target="_blank"
              rel="noreferrer"
              onClick={e => e.stopPropagation()}
              className="text-zinc-600 hover:text-brand-orange transition-colors shrink-0"
              title={`@${sponsor.ig}`}
            >
              <Instagram size={14} />
            </a>
          )}
        </div>
      )}
    </div>
  );
}

export function Sponsors() {
  const [sponsors, setSponsors] = useState<Sponsor[]>(defaultSponsors);

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
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
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
      </div>

      {/* CTA */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mt-24">
        <div className="border-[4px] border-brand-orange bg-brand-orange/5 p-10 md:p-14 flex flex-col md:flex-row items-start md:items-center justify-between gap-8">
          <div>
            <h3 className="font-display text-3xl md:text-4xl uppercase text-white mb-2">
              Vuoi diventare sponsor?
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
