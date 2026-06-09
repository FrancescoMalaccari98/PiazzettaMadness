import React, { useState } from "react";
import { motion } from "motion/react";
import { Send, CheckCircle2 } from "lucide-react";

export function Registration() {
  const [submitted, setSubmitted] = useState(false);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitted(true);
    // Here would be actual form submission to a backend
  };

  return (
    <div className="pt-32 pb-20 max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <div className="text-center mb-12">
          <h1 className="font-display text-[80px] md:text-[120px] text-brand-orange uppercase leading-[0.8] mb-8 tracking-[-4px]">
            Iscriviti Ora
          </h1>
          <p className="text-xl font-sans text-zinc-400">
            Compila il modulo per registrare il tuo team. Verrai ricontattato dallo staff per i dettagli.
          </p>
        </div>

        {submitted ? (
          <motion.div 
            initial={{ scale: 0.9, opacity: 0 }} 
            animate={{ scale: 1, opacity: 1 }}
            className="bg-brand-orange text-white p-12 text-center border-4 border-black"
          >
            <CheckCircle2 className="w-24 h-24 mx-auto mb-6 opacity-80" />
            <h2 className="font-display text-5xl uppercase mb-4">Richiesta Inviata!</h2>
            <p className="font-sans text-xl">
              La palla è passata a noi. Ti contatteremo presto per confermare la tua iscrizione e definire i dettagli per la quota.
            </p>
            <button 
              onClick={() => setSubmitted(false)}
              className="mt-8 px-6 py-3 bg-black text-white font-display text-xl uppercase hover:bg-zinc-800 transition-colors"
            >
              Invia un'altra squadra
            </button>
          </motion.div>
        ) : (
          <form onSubmit={handleSubmit} className="bg-zinc-900 border border-zinc-800 p-6 md:p-8 space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-2">
                <label htmlFor="teamName" className="font-display text-xl text-white uppercase tracking-wide">Nome Squadra *</label>
                <input 
                  type="text" 
                  id="teamName" 
                  required
                  className="w-full bg-zinc-950 border border-zinc-700 text-white font-sans p-4 focus:outline-none focus:border-brand-orange transition-colors rounded-none"
                  placeholder="Es. Atlanta Robba"
                />
              </div>
              <div className="space-y-2">
                <label htmlFor="captainName" className="font-display text-xl text-white uppercase tracking-wide">Nome Capitano *</label>
                <input 
                  type="text" 
                  id="captainName" 
                  required
                  className="w-full bg-zinc-950 border border-zinc-700 text-white font-sans p-4 focus:outline-none focus:border-brand-orange transition-colors rounded-none"
                  placeholder="Nome e Cognome"
                />
              </div>
              <div className="space-y-2">
                <label htmlFor="email" className="font-display text-xl text-white uppercase tracking-wide">Email Capitano *</label>
                <input 
                  type="email" 
                  id="email" 
                  required
                  className="w-full bg-zinc-950 border border-zinc-700 text-white font-sans p-4 focus:outline-none focus:border-brand-orange transition-colors rounded-none"
                  placeholder="La tua email"
                />
              </div>
              <div className="space-y-2">
                <label htmlFor="phone" className="font-display text-xl text-white uppercase tracking-wide">Cellulare Capitano *</label>
                <input 
                  type="tel" 
                  id="phone" 
                  required
                  className="w-full bg-zinc-950 border border-zinc-700 text-white font-sans p-4 focus:outline-none focus:border-brand-orange transition-colors rounded-none"
                  placeholder="+39 ..."
                />
              </div>
            </div>

            <div className="space-y-2">
              <label htmlFor="roster" className="font-display text-xl text-white uppercase tracking-wide">Roster Iniziale (Opzionale, max 12)</label>
              <textarea 
                id="roster" 
                rows={4}
                className="w-full bg-zinc-950 border border-zinc-700 text-white font-sans p-4 focus:outline-none focus:border-brand-orange transition-colors rounded-none resize-y"
                placeholder="Elenca i giocatori con cui intendi partecipare (Nome, Cognome)"
              ></textarea>
            </div>

            <div className="space-y-2">
              <label htmlFor="notes" className="font-display text-xl text-white uppercase tracking-wide">Note aggiuntive / Domande</label>
              <textarea 
                id="notes" 
                rows={3}
                className="w-full bg-zinc-950 border border-zinc-700 text-white font-sans p-4 focus:outline-none focus:border-brand-orange transition-colors rounded-none resize-y"
                placeholder="Scrivi qui eventuali richieste o particolarità da segnalare"
              ></textarea>
            </div>

            <div className="pt-6 border-t border-zinc-800">
              <div className="flex items-center gap-3 mb-6">
                <input 
                  type="checkbox" 
                  id="privacy" 
                  required
                  className="w-5 h-5 accent-brand-orange bg-zinc-950 border-zinc-700 rounded-none cursor-pointer"
                />
                <label htmlFor="privacy" className="font-sans text-sm text-zinc-400">
                  Ho letto il <a href="/info" className="text-brand-orange hover:underline">Regolamento</a> e l'<a href="/privacy" className="text-brand-orange hover:underline">Informativa Privacy</a> e accetto il trattamento dei dati personali per scopi organizzativi legati al torneo.
                </label>
              </div>

              <button 
                type="submit"
                className="w-full flex items-center justify-center gap-2 px-8 py-5 bg-brand-orange text-brand-bg font-black font-display text-2xl uppercase tracking-wider hover:bg-white transition-colors"
                >
                Invia Iscrizione <Send className="w-6 h-6" />
              </button>
            </div>
          </form>
        )}
      </motion.div>
    </div>
  );
}
