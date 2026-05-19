import { ExternalLink } from "lucide-react";

type Sponsor = {
  id: number;
  name: string;
  url: string;
  logo?: string;
};

type Tier = {
  key: string;
  label: string;
  description: string;
  borderClass: string;
  accentClass: string;
  bgClass: string;
  sponsors: Sponsor[];
};

const tiers: Tier[] = [
  {
    key: "main",
    label: "Main Partner",
    description: "I partner principali che rendono possibile tutto questo.",
    borderClass: "border-brand-orange",
    accentClass: "text-brand-orange",
    bgClass: "bg-brand-orange/10",
    sponsors: [
      { id: 1, name: "Sponsor Principale 1", url: "#" },
      { id: 2, name: "Sponsor Principale 2", url: "#" },
    ],
  },
  {
    key: "gold",
    label: "Gold Sponsor",
    description: "Partner fondamentali del torneo.",
    borderClass: "border-brand-yellow",
    accentClass: "text-brand-yellow",
    bgClass: "bg-brand-yellow/5",
    sponsors: [
      { id: 3, name: "Gold Sponsor 1", url: "#" },
      { id: 4, name: "Gold Sponsor 2", url: "#" },
      { id: 5, name: "Gold Sponsor 3", url: "#" },
      { id: 6, name: "Gold Sponsor 4", url: "#" },
      { id: 7, name: "Gold Sponsor 5", url: "#" },
    ],
  },
  {
    key: "silver",
    label: "Silver Sponsor",
    description: "Il supporto che fa la differenza.",
    borderClass: "border-brand-blue",
    accentClass: "text-brand-blue",
    bgClass: "bg-brand-blue/5",
    sponsors: [
      { id: 8,  name: "Silver Sponsor 1",  url: "#" },
      { id: 9,  name: "Silver Sponsor 2",  url: "#" },
      { id: 10, name: "Silver Sponsor 3",  url: "#" },
      { id: 11, name: "Silver Sponsor 4",  url: "#" },
      { id: 12, name: "Silver Sponsor 5",  url: "#" },
      { id: 13, name: "Silver Sponsor 6",  url: "#" },
      { id: 14, name: "Silver Sponsor 7",  url: "#" },
      { id: 15, name: "Silver Sponsor 8",  url: "#" },
    ],
  },
  {
    key: "supporter",
    label: "Supporter",
    description: "Grazie a chi ci ha dato una mano.",
    borderClass: "border-zinc-700",
    accentClass: "text-zinc-400",
    bgClass: "bg-zinc-900/50",
    sponsors: [
      { id: 16, name: "Supporter 1",  url: "#" },
      { id: 17, name: "Supporter 2",  url: "#" },
      { id: 18, name: "Supporter 3",  url: "#" },
      { id: 19, name: "Supporter 4",  url: "#" },
      { id: 20, name: "Supporter 5",  url: "#" },
      { id: 21, name: "Supporter 6",  url: "#" },
      { id: 22, name: "Supporter 7",  url: "#" },
      { id: 23, name: "Supporter 8",  url: "#" },
      { id: 24, name: "Supporter 9",  url: "#" },
      { id: 25, name: "Supporter 10", url: "#" },
      { id: 26, name: "Supporter 11", url: "#" },
      { id: 27, name: "Supporter 12", url: "#" },
      { id: 28, name: "Supporter 13", url: "#" },
      { id: 29, name: "Supporter 14", url: "#" },
      { id: 30, name: "Supporter 15", url: "#" },
    ],
  },
];

const gridCols: Record<string, string> = {
  main:      "grid-cols-1 sm:grid-cols-2",
  gold:      "grid-cols-2 sm:grid-cols-3 lg:grid-cols-5",
  silver:    "grid-cols-2 sm:grid-cols-3 lg:grid-cols-4",
  supporter: "grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5",
};

const cardHeight: Record<string, string> = {
  main:      "h-44 md:h-56",
  gold:      "h-36 md:h-44",
  silver:    "h-32 md:h-36",
  supporter: "h-24 md:h-28",
};

const logoSize: Record<string, string> = {
  main:      "text-2xl md:text-3xl",
  gold:      "text-base md:text-xl",
  silver:    "text-sm md:text-base",
  supporter: "text-xs md:text-sm",
};

function SponsorCard({ sponsor, tier }: { sponsor: Sponsor; tier: Tier }) {
  return (
    <a
      href={sponsor.url}
      target="_blank"
      rel="noreferrer"
      className={`
        group relative flex flex-col items-center justify-center
        ${cardHeight[tier.key]} border-[3px] ${tier.borderClass}
        ${tier.bgClass} hover:bg-zinc-800 transition-all duration-300
        hover:-translate-y-1 hover:shadow-[6px_6px_0_rgba(0,0,0,0.4)]
        overflow-hidden p-4
      `}
    >
      {/* Numero decorativo di sfondo */}
      <span className="absolute top-1 right-2 font-display text-[40px] leading-none text-white/[0.03] pointer-events-none select-none">
        {String(sponsor.id).padStart(2, "0")}
      </span>

      {/* Logo o placeholder */}
      {sponsor.logo ? (
        <img
          src={sponsor.logo}
          alt={sponsor.name}
          className="max-h-[60%] max-w-[80%] object-contain filter brightness-0 invert opacity-70 group-hover:opacity-100 transition-opacity"
        />
      ) : (
        <div className="flex flex-col items-center gap-2 text-center">
          {/* Rettangolo placeholder logo */}
          <div className={`w-16 h-8 ${tier.bgClass} border-2 ${tier.borderClass} opacity-40 group-hover:opacity-70 transition-opacity`} />
          <span className={`font-display uppercase tracking-wide ${logoSize[tier.key]} text-zinc-400 group-hover:text-white transition-colors leading-tight`}>
            {sponsor.name}
          </span>
        </div>
      )}

      {/* Link icon */}
      <ExternalLink
        className={`absolute bottom-2 right-2 w-3.5 h-3.5 ${tier.accentClass} opacity-0 group-hover:opacity-100 transition-opacity`}
      />
    </a>
  );
}

export function Sponsors() {
  return (
    <div className="w-full min-h-screen bg-brand-bg pt-28 pb-24">

      {/* Header */}
      <div className="relative overflow-hidden border-b-[4px] border-zinc-800 pb-16 mb-20">
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none overflow-hidden">
          <span className="font-display font-black text-[18vw] uppercase text-white/[0.025] whitespace-nowrap tracking-tighter leading-none">
            SPONSOR
          </span>
        </div>
        <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <p className="font-display text-brand-orange uppercase tracking-[0.3em] text-sm mb-4">
            Piazzetta Madness 2026
          </p>
          <h1 className="font-display text-6xl md:text-8xl lg:text-[110px] uppercase leading-[0.85] tracking-[-3px] text-white mb-6">
            I Nostri<br />
            <span className="text-brand-orange">Sponsor</span>
          </h1>
          <p className="font-sans text-zinc-400 text-lg max-w-xl">
            Senza di loro la Piazzetta Madness non esisterebbe. Supporta chi supporta noi.
          </p>
        </div>
      </div>

      {/* Tier sections */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-20">
        {tiers.map((tier) => (
          <section key={tier.key}>

            {/* Tier header */}
            <div className="flex items-center gap-6 mb-8">
              <div className={`h-1 w-10 ${tier.borderClass} bg-current ${tier.accentClass}`} />
              <div>
                <h2 className={`font-display text-3xl md:text-4xl uppercase tracking-wide ${tier.accentClass}`}>
                  {tier.label}
                </h2>
                <p className="font-sans text-zinc-500 text-sm mt-1">{tier.description}</p>
              </div>
              <div className={`flex-1 h-[2px] ${tier.bgClass} border-t-2 ${tier.borderClass} opacity-30`} />
            </div>

            {/* Cards grid */}
            <div className={`grid ${gridCols[tier.key]} gap-4`}>
              {tier.sponsors.map((sponsor) => (
                <SponsorCard key={sponsor.id} sponsor={sponsor} tier={tier} />
              ))}
            </div>

          </section>
        ))}
      </div>

      {/* CTA footer interno */}
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
