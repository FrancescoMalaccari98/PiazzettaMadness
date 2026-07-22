import { useState, useEffect, useMemo } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { motion } from "motion/react";
import { allPlayers as defaultPlayers, type Player } from "../data/stats";
import { EditionSelector } from "../components/EditionSelector";
import { api, type ApiEdition } from "../lib/api";

const API = import.meta.env.VITE_API_URL ?? "";

export function Players() {
  const [players, setPlayers] = useState<Player[]>(defaultPlayers);
  const [editions, setEditions] = useState<ApiEdition[]>([]);
  const [editionsReady, setEditionsReady] = useState(false);
  const [selectedEditionId, setSelectedEditionId] = useState<number | null>(null);
  // Filtro squadra pre-impostato dal parametro URL ?team= (link dalla pagina Stats)
  const [searchParams] = useSearchParams();
  const teamParam = searchParams.get("team");
  const [activeTeam, setActiveTeam] = useState<string>(teamParam ?? "all");

  useEffect(() => {
    let cancelled = false;

    api.getEditions()
      .then(list => {
        if (cancelled) return;
        setEditions(list);
        setSelectedEditionId(list.find(edition => edition.is_default)?.id ?? list[0]?.id ?? null);
      })
      .catch(() => {})
      .finally(() => {
        if (!cancelled) setEditionsReady(true);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!editionsReady) return;

    const controller = new AbortController();
    const editionQuery = selectedEditionId ? `?edition_id=${selectedEditionId}` : "";

    fetch(`${API}/api-web/giocatori${editionQuery}`, { signal: controller.signal })
      .then(r => r.ok ? r.json() as Promise<Player[]> : null)
      .then(data => { if (data) setPlayers(data); })
      .catch(error => {
        if (error?.name !== "AbortError") {
          // Il fallback statico resta visibile se il backend non risponde.
        }
      });

    return () => controller.abort();
  }, [editionsReady, selectedEditionId]);

  const selectedEdition = editions.find(edition => edition.id === selectedEditionId) ?? editions.find(edition => edition.is_default);
  const teams = useMemo(() => [...new Set(players.map(p => p.team))], [players]);
  const teamColorMap = useMemo(() => new Map(players.map(p => [p.team, p.teamColor || "#ea6324"])), [players]);

  // Se si arriva con un nuovo ?team= o si cambia edizione, mantiene un filtro valido.
  useEffect(() => {
    if (teamParam && teams.includes(teamParam)) {
      setActiveTeam(teamParam);
      return;
    }

    if (activeTeam !== "all" && !teams.includes(activeTeam)) {
      setActiveTeam("all");
    }
  }, [activeTeam, teamParam, teams]);

  const grouped = activeTeam === "all"
    ? teams.map(team => ({ team, players: players.filter(p => p.team === team) }))
    : [{ team: activeTeam, players: players.filter(p => p.team === activeTeam) }];

  return (
    <div className="w-full min-h-screen bg-brand-bg pt-28 pb-24">

      {/* Header */}
      <div className="relative overflow-hidden border-b-[4px] border-zinc-800 mb-12">
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none overflow-hidden">
          <span className="font-display font-black text-[18vw] uppercase text-white/[0.025] whitespace-nowrap tracking-tighter leading-none">
            ROSTERS
          </span>
        </div>
        <motion.div
          initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}
          className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-12 text-center"
        >
          <p className="font-display text-brand-orange uppercase tracking-[0.3em] text-sm mb-3">
            {selectedEdition ? `Piazzetta Madness ${selectedEdition.year}` : "Piazzetta Madness"}
          </p>
          <h1 className="font-display text-[56px] sm:text-[80px] md:text-[120px] uppercase leading-[0.8] tracking-[-2px] md:tracking-[-4px] text-brand-orange mb-6">
            Rosters
          </h1>
          <p className="font-sans text-zinc-400 text-sm sm:text-lg max-w-[260px] sm:max-w-xl mx-auto leading-relaxed">
            {players.length} atleti, {teams.length} squadre. Clicca su un giocatore per vedere le sue statistiche.
          </p>
        </motion.div>
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <EditionSelector
          editions={editions}
          selectedEditionId={selectedEditionId}
          onChange={setSelectedEditionId}
          className="mb-8 justify-end"
        />

        {/* Filtro squadra */}
        <div className="flex flex-wrap gap-2 mb-10">
          <button
            onClick={() => setActiveTeam("all")}
            className={`font-display text-xs uppercase tracking-widest px-4 py-2 border-2 transition-all bg-transparent
              ${activeTeam === "all" ? "border-brand-orange bg-brand-orange/10 text-brand-orange" : "border-zinc-700 text-zinc-500 hover:border-zinc-500 hover:text-zinc-300"}`}
          >
            Tutte
          </button>
          {teams.map(team => {
            const color = teamColorMap.get(team) ?? "#ea6324";
            const active = activeTeam === team;
            return (
              <button
                key={team}
                onClick={() => setActiveTeam(team)}
                className={`font-display text-xs uppercase tracking-widest px-4 py-2 border-2 transition-all ${active ? "text-white" : "text-zinc-500"}`}
                style={active
                  ? { borderColor: color, backgroundColor: `${color}26` }
                  : { borderColor: "#3f3f46", backgroundColor: "transparent" }}
              >
                {team}
              </button>
            );
          })}
        </div>

        {/* Griglia giocatori */}
        <div className="space-y-14">
          {grouped.map(({ team, players: roster }) => {
            const teamColor = roster[0]?.teamColor || "#ea6324"; // fallback brand-orange
            return (
            <div key={team}>
              <div className="flex items-center gap-4 mb-6">
                <span className="w-2 h-6 shrink-0" style={{ backgroundColor: teamColor }} />
                <h2 className="font-display text-xl md:text-2xl uppercase tracking-wide text-white">{team}</h2>
                <div className="flex-1 h-px bg-zinc-800" />
                <span className="font-display text-xs uppercase tracking-widest text-zinc-600">{roster.length} giocatori</span>
              </div>

              <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
                {roster.map((player, i) => (
                  <motion.div
                    key={player.slug}
                    initial={{ opacity: 0, y: 30 }}
                    whileInView={{ opacity: 1, y: 0 }}
                    viewport={{ once: true, margin: "-30px" }}
                    transition={{ delay: (i % 5) * 0.08, duration: 0.4, ease: [0.22, 1, 0.36, 1] }}
                  >
                  <Link
                    to={`/statistiche/${player.slug}`}
                    className="block group border-[3px] border-zinc-800 bg-zinc-900 transition-all hover:-translate-y-1 overflow-hidden"
                    onMouseEnter={e => {
                      e.currentTarget.style.borderColor = teamColor;
                      e.currentTarget.style.boxShadow = `6px 6px 0 ${teamColor}`;
                    }}
                    onMouseLeave={e => {
                      e.currentTarget.style.borderColor = "";
                      e.currentTarget.style.boxShadow = "";
                    }}
                  >
                    {/* Foto o placeholder */}
                    <div className="aspect-square bg-zinc-800 relative overflow-hidden">
                      {player.photo ? (
                        <img
                          src={player.photo}
                          alt={player.name}
                          className="w-full h-full object-cover group-hover:scale-105 transition-all duration-500"
                        />
                      ) : (
                        <div className="w-full h-full flex flex-col items-center justify-center bg-zinc-900 group-hover:bg-zinc-800 transition-colors">
                          <span className="font-display text-4xl md:text-5xl text-zinc-700 group-hover:text-zinc-500 transition-colors uppercase select-none">
                            {player.name.split(" ").filter(Boolean).map(n => n[0]).join("").slice(0, 2)}
                          </span>
                          {player.number && (
                            <span className="font-display text-xs text-zinc-700 group-hover:text-zinc-400 transition-colors mt-1">
                              #{player.number}
                            </span>
                          )}
                        </div>
                      )}
                    </div>

                    {/* Info */}
                    <div className="p-3">
                      <div className="flex items-center gap-1.5 min-w-0">
                        <span className="w-2 h-2 shrink-0 rounded-sm" style={{ backgroundColor: teamColor }} />
                        <p className="font-sans font-bold text-sm uppercase text-white leading-tight truncate">
                          {player.name}
                        </p>
                      </div>
                      <p className="font-sans text-xs text-zinc-500 truncate mt-0.5 mb-3">{player.team}</p>

                      {/* Stats */}
                      <div className="grid grid-cols-4 gap-1 border-t border-zinc-800 pt-2">
                        {[
                          { label: "PTI", val: player.pts, accent: "text-brand-orange" },
                          { label: "ASS", val: player.ast, accent: "text-zinc-300" },
                          { label: "RIM", val: player.reb, accent: "text-zinc-300" },
                          { label: "VAL", val: player.val, accent: "text-brand-yellow" },
                        ].map(s => (
                          <div key={s.label} className="text-center">
                            <div className={`font-mono text-sm font-bold ${s.accent}`}>{s.val}</div>
                            <div className="font-display text-[8px] uppercase tracking-widest text-zinc-600">{s.label}</div>
                          </div>
                        ))}
                      </div>
                    </div>
                  </Link>
                  </motion.div>
                ))}
              </div>
            </div>
            );
          })}
        </div>

      </div>
    </div>
  );
}
