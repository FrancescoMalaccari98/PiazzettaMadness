import { Info as InfoIcon, Target, ShieldAlert, BadgeInfo } from "lucide-react";
import { motion } from "motion/react";

export function Info() {
  return (
    <div className="pt-32 pb-20 max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <h1 className="font-display text-[80px] md:text-[120px] text-brand-orange uppercase leading-[0.8] mb-8 tracking-[-4px]">
          Info & Rules
        </h1>
        <p className="text-xl font-sans text-zinc-400 mb-12">
          Le regole della strada. Leggi attentamente o non scendere in campo.
        </p>

        <div className="space-y-12">
          
          <section className="bg-zinc-900 border border-zinc-800 p-8 relative overflow-hidden">
            <div className="absolute top-0 right-0 p-8 opacity-10">
              <BadgeInfo className="w-48 h-48" />
            </div>
            <h2 className="font-display text-4xl mb-6 flex items-center gap-3 relative z-10 text-brand-orange border-t-4 border-brand-orange pt-4">
              Formato Torneo
            </h2>
            <div className="font-sans text-zinc-300 space-y-4 relative z-10">
              <p>
                <strong>Struttura:</strong> Il torneo è un 5vs5 full court. Prima fase a gironi, seguita da fase ad eliminazione diretta.
              </p>
              <p>
                <strong>Durata Partite:</strong> 4 quarti da 10 minuti (con tempo sporco), ultimi 2 minuti dell'ultimo quarto tempo effettivo.
              </p>
              <p>
                <strong>Timeout:</strong> 2 per squadra nel primo tempo, 3 nel secondo tempo. 1 timeout da 30 secondi e gli altri da 1 minuto.
              </p>
            </div>
          </section>

          <section className="bg-zinc-900 border border-zinc-800 p-8 relative overflow-hidden">
            <div className="absolute top-0 right-0 p-8 opacity-10">
              <Target className="w-48 h-48" />
            </div>
            <h2 className="font-display text-4xl mb-6 flex items-center gap-3 relative z-10 text-brand-orange border-t-4 border-brand-orange pt-4">
              Requisiti Iscrizione
            </h2>
            <div className="font-sans text-zinc-300 space-y-4 relative z-10">
              <ul className="list-disc pl-5 space-y-2">
                <li>Roster minimo di 7 giocatori, massimo 12 giocatori per squadra.</li>
                <li>Età minima 16 anni compiuti (con liberatoria genitori per minorenni).</li>
                <li>Quota di iscrizione: indicata nella pagina <a href="/iscrizioni" className="text-brand-orange hover:underline">Iscrizioni</a>.</li>
                <li>Certificato medico non agonistico o agonistico in corso di validità obbligatorio per ogni partecipante.</li>
              </ul>
            </div>
          </section>

          <section className="bg-zinc-900 border border-zinc-800 p-8 relative overflow-hidden">
            <div className="absolute top-0 right-0 p-8 opacity-10">
              <ShieldAlert className="w-48 h-48" />
            </div>
            <h2 className="font-display text-4xl mb-6 flex items-center gap-3 relative z-10 text-brand-orange border-t-4 border-brand-orange pt-4">
              Comportamento
            </h2>
            <div className="font-sans text-zinc-300 space-y-4 relative z-10">
              <p>
                <strong>Fair Play:</strong> Il trash talking fa parte del gioco, ma insulti razziali, minacce e risse comporteranno l'espulsione immediata dell'intera squadra dal torneo senza alcun rimborso.
              </p>
              <p>
                <strong>Arbitraggio:</strong> Le decisioni degli arbitri sono insindacabili. Solo il capitano della squadra può chiedere delucidazioni in modo pulito e rispettoso.
              </p>
            </div>
          </section>

        </div>
      </motion.div>
    </div>
  );
}
