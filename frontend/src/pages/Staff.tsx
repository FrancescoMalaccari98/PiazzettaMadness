import { useState, useEffect } from "react";
import { motion } from "motion/react";
import { Users2, Instagram } from "lucide-react";

const API = import.meta.env.VITE_API_URL ?? "";

type StaffMember = {
  id: number;
  nome: string;
  ruolo: string;
  bio: string;
  foto: string;
  ig: string;
};

const defaultStaff: StaffMember[] = [
  { id: 1, nome: "Nicolò Micucci",  ruolo: "Organizzatore / Founder",   bio: "La mente dietro Piazzetta Madness. Appassionato di basket e street culture.", foto: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&q=80&w=800", ig: "nicomicucci" },
  { id: 2, nome: "Luca Verdi",      ruolo: "Direttore Tecnico",          bio: "Responsabile degli arbitri e del corretto svolgimento del torneo.",             foto: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&q=80&w=800", ig: "lucadrv" },
  { id: 3, nome: "Giulia Rossi",    ruolo: "Media & Comunicazione",      bio: "Fotografa ufficiale e content creator per i canali social.",                    foto: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&q=80&w=800", ig: "giuliarossi_ph" },
  { id: 4, nome: "Marco Bianchi",   ruolo: "Logistica",                   bio: "Si assicura che il campo e l'attrezzatura siano sempre perfetti.",              foto: "https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?auto=format&fit=crop&q=80&w=800", ig: "marcobianchi_log" },
];

export function Staff() {
  const [staffMembers, setStaffMembers] = useState<StaffMember[]>(defaultStaff);

  useEffect(() => {
    fetch(`${API}/api/staff`)
      .then(r => r.ok ? r.json() as Promise<StaffMember[]> : null)
      .then(data => { if (data) setStaffMembers(data); })
      .catch(() => {});
  }, []);

  return (
    <div className="pt-32 pb-20 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <div className="text-center mb-16">
          <h1 className="font-display text-[44px] sm:text-[70px] md:text-[120px] text-brand-orange uppercase leading-[0.8] mb-6 tracking-[-2px] md:tracking-[-4px]">
            Lo Staff
          </h1>
          <p className="text-base sm:text-xl font-sans text-zinc-400 max-w-3xl mx-auto">
            Le persone dietro le quinte che rendono possibile il Piazzetta Madness.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
          {staffMembers.map((member, index) => (
            <motion.div
              key={member.id}
              initial={{ opacity: 0, y: 40, skewY: 2 }}
              whileInView={{ opacity: 1, y: 0, skewY: 0 }}
              viewport={{ once: true, margin: "-60px" }}
              transition={{ delay: index * 0.12, duration: 0.5, ease: [0.22, 1, 0.36, 1] }}
              className="bg-zinc-900 border-[3px] border-zinc-800 hover:border-brand-blue group transition-colors overflow-hidden flex flex-col"
            >
              <div className="aspect-square overflow-hidden relative">
                <img
                  src={member.foto}
                  alt={member.nome}
                  className="w-full h-full object-cover grayscale group-hover:grayscale-0 transition-all duration-500 scale-100 group-hover:scale-105"
                  referrerPolicy="no-referrer"
                />
                <div className="absolute inset-0 bg-brand-blue/20 opacity-0 group-hover:opacity-100 transition-opacity mix-blend-multiply" />
                <div className="absolute bottom-0 left-0 w-full bg-gradient-to-t from-zinc-950 to-transparent h-24" />
              </div>
              <div className="p-6 flex-1 flex flex-col relative z-10 -mt-8">
                <h3 className="font-display text-2xl uppercase tracking-wide text-white mb-1">{member.nome}</h3>
                <p className="font-sans font-bold text-brand-yellow text-sm uppercase tracking-wider mb-4">{member.ruolo}</p>
                <p className="font-sans text-zinc-400 text-sm mb-6 flex-1">{member.bio}</p>
                <div className="border-t border-zinc-800 pt-4 flex items-center justify-between mt-auto">
                  <a
                    href={`https://instagram.com/${member.ig}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-zinc-500 hover:text-brand-orange transition-colors flex items-center gap-2 text-sm font-sans uppercase font-bold"
                  >
                    <Instagram size={16} /> @{member.ig}
                  </a>
                </div>
              </div>
            </motion.div>
          ))}
        </div>

        <div className="mt-24 border-[3px] border-brand-orange bg-zinc-900 border-dashed p-10 text-center relative overflow-hidden group">
          <div className="absolute inset-0 bg-[url('https://images.unsplash.com/photo-1546519638-68e109498ffc?auto=format&fit=crop&q=80&w=1920')] bg-cover bg-center opacity-5 group-hover:opacity-10 transition-opacity" />
          <div className="relative z-10">
            <Users2 className="w-16 h-16 text-brand-orange mx-auto mb-6" />
            <h2 className="font-display text-4xl uppercase mb-4">Vuoi unirti a noi?</h2>
            <p className="font-sans text-zinc-400 text-lg mb-8 max-w-2xl mx-auto">
              Siamo sempre alla ricerca di volontari, arbitri e collaboratori per far crescere il torneo. Scrivici!
            </p>
            <a
              href="mailto:info@piazzettamadness.it"
              className="inline-block bg-brand-orange text-brand-bg font-display uppercase tracking-widest px-8 py-4 text-xl hover:bg-white hover:text-brand-bg transition-colors"
            >
              Contattaci
            </a>
          </div>
        </div>
      </motion.div>
    </div>
  );
}
