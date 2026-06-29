import { useState, useEffect } from "react";
import { motion } from "motion/react";
import { Users2 } from "lucide-react";

const API = import.meta.env.VITE_API_URL ?? "";

type StaffMember = {
  id: number;
  nome: string;
  ruolo: string;
  categoria: string;
  bio: string;
  foto: string;
  ig: string;
};

const CATEGORIE: { key: string; label: string }[] = [
  { key: "founders",      label: "Founders" },
  { key: "social",        label: "Social" },
  { key: "it",            label: "IT" },
  { key: "collaboratori", label: "Collaboratori" },
];


const defaultStaff: StaffMember[] = [
  { id: 1, nome: "Nicolò Micucci",  ruolo: "Organizzatore / Founder",   categoria: "founders",      bio: "La mente dietro Piazzetta Madness.", foto: "", ig: "nicomicucci" },
  { id: 2, nome: "Luca Verdi",      ruolo: "Direttore Tecnico",          categoria: "it",            bio: "Responsabile degli arbitri e del corretto svolgimento.", foto: "", ig: "lucadrv" },
  { id: 3, nome: "Giulia Rossi",    ruolo: "Media & Comunicazione",      categoria: "social",        bio: "Fotografa ufficiale e content creator.", foto: "", ig: "giuliarossi_ph" },
  { id: 4, nome: "Marco Bianchi",   ruolo: "Logistica",                  categoria: "collaboratori", bio: "Si assicura che il campo sia sempre perfetto.", foto: "", ig: "marcobianchi_log" },
];

function StaffCard({ member, index }: { member: StaffMember; index: number }) {
  const parts = member.nome.trim().split(" ");
  const lastName = parts.pop() ?? "";
  const firstName = parts.join(" ");

  return (
    <motion.div
      initial={{ opacity: 0, y: 40, skewY: 2 }}
      whileInView={{ opacity: 1, y: 0, skewY: 0 }}
      viewport={{ once: true, margin: "-60px" }}
      transition={{ delay: index * 0.1, duration: 0.5, ease: [0.22, 1, 0.36, 1] }}
      className="bg-zinc-900 border-[3px] border-zinc-800 hover:border-brand-blue group transition-all duration-300 overflow-hidden flex flex-col"
    >
      {/* barra arancio in cima, diventa blu all'hover */}
      <div className="h-[4px] bg-brand-orange group-hover:bg-brand-blue transition-colors duration-300 shrink-0" />

      {/* foto o placeholder iniziali */}
      <div className="aspect-square overflow-hidden relative">
        {member.foto ? (
          <img
            src={member.foto}
            alt={member.nome}
            className="w-full h-full object-cover grayscale group-hover:grayscale-0 transition-all duration-500 scale-100 group-hover:scale-105"
            referrerPolicy="no-referrer"
          />
        ) : (
          <div className="w-full h-full bg-zinc-800 flex items-center justify-center">
            <span className="font-display text-7xl text-zinc-600 uppercase select-none">
              {member.nome.charAt(0)}
            </span>
          </div>
        )}
        <div className="absolute inset-0 bg-brand-blue/20 opacity-0 group-hover:opacity-100 transition-opacity mix-blend-multiply" />
        <div className="absolute bottom-0 left-0 w-full bg-gradient-to-t from-zinc-950 to-transparent h-24" />
      </div>

      {/* contenuto card */}
      <div className="p-6 flex-1 flex flex-col relative z-10 -mt-8">
        <h3 className="font-display text-2xl uppercase tracking-wide text-white leading-tight mb-1">
          {firstName && <span className="block">{firstName}</span>}
          <span className="block">{lastName}</span>
        </h3>
        <p className="font-sans font-bold text-brand-yellow text-xs uppercase tracking-wider mb-4 line-clamp-2">
          {member.ruolo}
        </p>
        <p className="font-sans text-zinc-400 text-sm mb-6 flex-1">{member.bio}</p>
        <div className="border-t border-zinc-800 pt-4 mt-auto">
          {member.ig ? (
            <a
              href={`https://instagram.com/${member.ig}`}
              target="_blank"
              rel="noopener noreferrer"
              className="text-zinc-500 hover:text-brand-orange transition-colors flex items-center gap-2 text-sm font-sans uppercase font-bold"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <rect x="2" y="2" width="20" height="20" rx="5" ry="5"/>
                <path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z"/>
                <line x1="17.5" y1="6.5" x2="17.51" y2="6.5"/>
              </svg>
              @{member.ig}
            </a>
          ) : null}
        </div>
      </div>
    </motion.div>
  );
}

export function Staff() {
  const [staffMembers, setStaffMembers] = useState<StaffMember[]>(defaultStaff);

  useEffect(() => {
    fetch(`${API}/api-web/staff`)
      .then(r => r.ok ? r.json() as Promise<StaffMember[]> : null)
      .then(data => { if (data) setStaffMembers(data); })
      .catch(() => {});
  }, []);

  const grouped = CATEGORIE.map(cat => ({
    ...cat,
    members: staffMembers.filter(m => m.categoria === cat.key),
  })).filter(g => g.members.length > 0);

  return (
    <div className="pt-32 pb-20">
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
      >
        {/* hero header */}
        <div className="relative overflow-hidden border-b-[4px] border-zinc-800 mb-16">
          <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none">
            <span className="font-display font-black text-[18vw] uppercase text-white/[0.025] whitespace-nowrap tracking-tighter leading-none">
              STAFF
            </span>
          </div>
          <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center pb-12">
            <h1 className="font-display text-[44px] sm:text-[70px] md:text-[120px] text-brand-orange uppercase leading-[0.8] mb-6 tracking-[-2px] md:tracking-[-4px]">
              Lo Staff
            </h1>
            <p className="text-base sm:text-xl font-sans text-zinc-400 max-w-3xl mx-auto">
              Le persone dietro le quinte che rendono possibile il Piazzetta Madness.
            </p>
          </div>
        </div>

        {/* sezioni per categoria */}
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-20">
          {grouped.map(group => (
            <section key={group.key}>
              {/* intestazione sezione */}
              <div className="flex items-center gap-4 mb-10">
                <div className="w-[4px] h-9 bg-brand-orange shrink-0" />
                <h2 className="font-display text-3xl sm:text-4xl uppercase text-white tracking-wide">
                  {group.label}
                </h2>
                <div className="flex-1 h-[3px] bg-zinc-800" />
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 sm:gap-8">
                {group.members.map((member, i) => (
                  <StaffCard key={member.id} member={member} index={i} />
                ))}
              </div>
            </section>
          ))}
        </div>

        {/* CTA join */}
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mt-24">
          <div className="border-[3px] border-brand-orange bg-zinc-900 border-dashed p-10 text-center relative overflow-hidden group">
            <div className="relative z-10">
              <Users2 className="w-16 h-16 text-brand-orange mx-auto mb-6" />
              <h2 className="font-display text-4xl uppercase mb-4">Vuoi unirti a noi?</h2>
              <p className="font-sans text-zinc-400 text-lg mb-8 max-w-2xl mx-auto">
                Siamo sempre alla ricerca di volontari, arbitri e collaboratori per far crescere il torneo. Contattaci!
              </p>
              <a
                href="mailto:info@piazzettamadness.it"
                className="inline-block bg-brand-orange text-brand-bg font-display uppercase tracking-widest px-8 py-4 text-xl hover:bg-white hover:text-brand-bg transition-colors"
              >
                Contattaci
              </a>
            </div>
          </div>
        </div>
      </motion.div>
    </div>
  );
}
