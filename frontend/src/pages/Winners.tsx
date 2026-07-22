import { useEffect, useState } from "react";
import { motion } from "motion/react";
import { Target, Trophy } from "lucide-react";
import { api, type ApiWinnerEdition } from "../lib/api";

function WinnerImage({
  src,
  alt,
  initials,
  accent = "orange",
}: {
  src: string | null;
  alt: string;
  initials: string;
  accent?: "orange" | "yellow";
}) {
  const [failed, setFailed] = useState(false);
  const showImage = src && !failed;
  const accentClass = accent === "yellow" ? "text-brand-yellow" : "text-brand-orange";

  return (
    <div className="aspect-[4/3] bg-zinc-950 border-b-[3px] border-zinc-800 relative overflow-hidden">
      {showImage ? (
        <img
          src={src}
          alt={alt}
          className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-105"
          onError={() => setFailed(true)}
        />
      ) : (
        <div className="w-full h-full flex items-center justify-center bg-zinc-900">
          <span className={`font-display text-5xl sm:text-6xl uppercase select-none ${accentClass}/60`}>
            {initials}
          </span>
        </div>
      )}
      <div className="absolute inset-0 bg-gradient-to-t from-zinc-950 via-transparent to-transparent" />
    </div>
  );
}

function EditionBlock({ item, index }: { item: ApiWinnerEdition; index: number }) {
  const championName = item.champion.team_name ?? "TBD";
  const threePointName = item.threePoint?.player_name ?? "TBD";
  const finalMatch = item.champion.final_match;
  const finalScore = finalMatch && finalMatch.home_score !== null && finalMatch.away_score !== null
    ? `${finalMatch.home_score}-${finalMatch.away_score}`
    : "Finale";
  const threePointScore = item.threePoint
    ? item.threePoint.tiebreak_score > 0
      ? `${item.threePoint.final_score} + ${item.threePoint.tiebreak_score}`
      : String(item.threePoint.final_score)
    : "TBD";

  return (
    <motion.section
      initial={{ opacity: 0, y: 28 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-50px" }}
      transition={{ delay: index * 0.08, duration: 0.4, ease: [0.22, 1, 0.36, 1] }}
      className="h-full flex flex-col gap-4"
    >
      <div className="flex items-center gap-3">
        <div className="w-[4px] h-9 bg-brand-orange shrink-0" />
        <div className="min-w-0">
          <p className="font-display text-[10px] uppercase tracking-widest text-zinc-600 truncate">{item.edition.name}</p>
          <h2 className="font-display text-3xl sm:text-4xl uppercase text-white leading-none">{item.edition.year}</h2>
        </div>
        <div className="flex-1 h-[3px] bg-zinc-800" />
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-5 flex-1 items-stretch">
        <motion.article
          initial={{ opacity: 0, y: 18 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-50px" }}
          transition={{ delay: index * 0.08 + 0.04, duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
          className="group h-full border-[3px] border-brand-orange bg-zinc-900 overflow-hidden shadow-[5px_5px_0_var(--color-brand-blue)] flex flex-col"
        >
          <WinnerImage
            src={item.champion.photo}
            alt={championName}
            initials={championName === "TBD" ? "TBD" : championName.slice(0, 1)}
          />
          <div className="p-4 flex-1 flex flex-col">
            <div className="flex items-center justify-between gap-3 mb-3">
              <div className="flex items-center gap-2 min-w-0">
                <Trophy className="w-4 h-4 text-brand-yellow shrink-0" />
                <p className="font-display text-[10px] uppercase tracking-widest text-brand-yellow truncate">Campioni</p>
              </div>
            </div>
            <h3 className="font-display text-2xl uppercase text-white leading-[0.9] mb-3 line-clamp-2">
              {championName}
            </h3>
            <p className="font-sans text-zinc-400 text-xs leading-relaxed mb-4 line-clamp-3">
              {item.champion.description}
            </p>
            <div className="border-t border-zinc-800 pt-3 mt-auto">
              <div>
                <p className="font-display text-[10px] uppercase tracking-widest text-zinc-600">Finale</p>
                <p className="font-mono text-lg font-black text-brand-orange">{finalScore}</p>
              </div>
            </div>
          </div>
        </motion.article>

        <motion.article
          initial={{ opacity: 0, y: 18 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-50px" }}
          transition={{ delay: index * 0.08 + 0.08, duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
          className="group h-full border-[3px] border-brand-yellow bg-zinc-900 overflow-hidden flex flex-col"
        >
          <WinnerImage
            src={item.threePoint?.photo ?? null}
            alt={threePointName}
            initials={threePointName === "TBD" ? "3PT" : threePointName.split(" ").map(part => part[0]).join("").slice(0, 2)}
            accent="yellow"
          />
          <div className="p-4 flex-1 flex flex-col">
            <div className="flex items-center justify-between gap-3 mb-3">
              <div className="flex items-center gap-2 min-w-0">
                <Target className="w-4 h-4 text-brand-orange shrink-0" />
                <p className="font-display text-[10px] uppercase tracking-widest text-brand-orange truncate">3 Point Contest</p>
              </div>
            </div>
            <h3 className="font-display text-2xl uppercase text-white leading-[0.9] mb-2 line-clamp-2">
              {threePointName}
            </h3>
            {item.threePoint?.team_name && (
              <p className="font-sans font-bold text-[10px] uppercase tracking-wider text-zinc-500 mb-3 truncate">
                {item.threePoint.team_name}
                {item.threePoint.jersey_number !== null ? ` · #${item.threePoint.jersey_number}` : ""}
              </p>
            )}
            <p className="font-sans text-zinc-400 text-xs leading-relaxed mb-4 line-clamp-3">
              {item.threePoint?.description ?? "Vincitore del 3 Point Contest in aggiornamento."}
            </p>
            <div className="border-t border-zinc-800 pt-3 mt-auto">
              <p className="font-display text-[10px] uppercase tracking-widest text-zinc-600">Punteggio finale</p>
              <p className="font-mono text-xl font-black text-brand-yellow">{threePointScore}</p>
            </div>
          </div>
        </motion.article>
      </div>
    </motion.section>
  );
}

export function Winners() {
  const [items, setItems] = useState<ApiWinnerEdition[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getWinners()
      .then(data => { if (data) setItems(data.editions); })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="pt-32 pb-20 min-h-screen">
      <div className="relative overflow-hidden border-b-[4px] border-zinc-800 mb-14">
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none">
          <span className="font-display font-black text-[14vw] uppercase text-white/[0.025] whitespace-nowrap tracking-tighter leading-none">
            WINNERS
          </span>
        </div>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center pb-12"
        >
          <p className="font-display text-brand-orange uppercase tracking-[0.3em] text-sm mb-3">Hall of Fame</p>
          <h1 className="font-display text-[44px] sm:text-[76px] md:text-[120px] text-brand-orange uppercase leading-[0.8] mb-6 tracking-[-2px] md:tracking-[-4px]">
            Winners
          </h1>
          <p className="text-sm sm:text-xl font-sans text-zinc-400 max-w-[260px] sm:max-w-2xl mx-auto leading-relaxed">
            L'albo d'oro del Piazzetta Madness: campioni del torneo e mani piu' calde dall'arco.
          </p>
        </motion.div>
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {loading ? (
          <div className="text-center py-20">
            <div className="inline-block w-12 h-12 border-4 border-zinc-700 border-t-brand-orange rounded-full animate-spin" />
          </div>
        ) : items.length === 0 ? (
          <div className="border-[4px] border-zinc-800 bg-zinc-900 p-10 sm:p-14 text-center">
            <Trophy className="w-10 h-10 text-brand-orange mx-auto mb-5" />
            <h2 className="font-display text-3xl sm:text-4xl uppercase text-white mb-3">Albo in costruzione</h2>
            <p className="font-sans text-zinc-400 max-w-xl mx-auto">
              Quando un'edizione sara' conclusa, i vincitori compariranno automaticamente qui.
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 lg:gap-6">
            {items.map((item, index) => (
              <EditionBlock key={item.edition.id} item={item} index={index} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
