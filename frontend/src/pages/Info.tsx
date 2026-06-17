import { motion } from "motion/react";
import { BadgeInfo, CalendarDays, Users, Timer, Trophy, Shield, Target, AlertTriangle, MessageSquare } from "lucide-react";

function Section({ icon: Icon, title, children }: { icon: React.ElementType; title: string; children: React.ReactNode }) {
  return (
    <section className="bg-zinc-900 border border-zinc-800 p-6 sm:p-8 relative overflow-hidden">
      <div className="absolute -top-4 -right-4 opacity-[0.03] pointer-events-none">
        <Icon className="w-24 h-24 sm:w-32 sm:h-32" />
      </div>
      <h2 className="font-display text-2xl sm:text-3xl mb-6 flex items-center gap-3 relative z-10 text-brand-orange border-t-4 border-brand-orange pt-4">
        {title}
      </h2>
      <div className="font-sans text-zinc-300 space-y-4 relative z-10 text-[15px] leading-relaxed">
        {children}
      </div>
    </section>
  );
}

export function Info() {
  return (
    <div className="pt-32 pb-20">
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}>

        {/* Header */}
        <div className="relative overflow-hidden border-b-[4px] border-zinc-800 mb-12">
          <div className="absolute inset-0 flex items-center justify-center pointer-events-none select-none">
            <span className="font-display font-black text-[18vw] uppercase text-white/[0.025] whitespace-nowrap tracking-tighter leading-none">
              RULES
            </span>
          </div>
          <div className="relative z-10 max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center pb-12">
            <h1 className="font-display text-[44px] sm:text-[80px] md:text-[120px] text-brand-orange uppercase leading-[0.8] mb-6 tracking-[-2px] md:tracking-[-4px]">
              Info & Rules
            </h1>
            <p className="text-base sm:text-xl font-sans text-zinc-400">
              Regolamento Ufficiale — Piazzetta Madness 2026
            </p>
          </div>
        </div>

        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 space-y-10">

          {/* ── INFORMAZIONI GENERALI ── */}
          <Section icon={CalendarDays} title="Informazioni Generali">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="bg-zinc-950 border border-zinc-800 p-4">
                <p className="font-display text-xs uppercase tracking-widest text-zinc-500 mb-1">Date</p>
                <p className="text-white font-bold">8, 9, 10 e 11 Luglio 2026</p>
              </div>
              <div className="bg-zinc-950 border border-zinc-800 p-4">
                <p className="font-display text-xs uppercase tracking-widest text-zinc-500 mb-1">Luogo</p>
                <p className="text-white font-bold">Piazzetta Verde, Porto Potenza Picena (MC)</p>
              </div>
            </div>
            <p><strong className="text-white">Formula:</strong> 8 squadre divise in 2 gironi da 4. Le prime due di ogni girone passano in semifinale.</p>
            <p><strong className="text-white">Accesso al campo:</strong> solo per giocatori e organizzatori. Il pubblico è il benvenuto, ma fuori dal rettangolo di gioco.</p>
            <p><strong className="text-white">Puntualità:</strong> ogni squadra deve presentarsi almeno <span className="text-brand-orange font-bold">15 minuti prima</span> della propria partita.</p>
          </Section>

          {/* ── ISCRIZIONE ── */}
          <Section icon={Users} title="Iscrizione">
            <div className="bg-zinc-950 border border-brand-orange/30 p-4 mb-2">
              <p className="font-display text-xl text-brand-orange">Costo: €200 a squadra</p>
            </div>
            <p><strong className="text-white">Per iscriversi servono:</strong></p>
            <ul className="list-disc pl-5 space-y-1 text-zinc-400">
              <li>Modulo firmato da tutta la squadra</li>
              <li>Documento d'identità di ogni giocatore</li>
              <li>Modulo Privacy firmato da ogni partecipante</li>
            </ul>
            <p className="text-red-400"><strong>Rinuncia dopo il 21 giugno:</strong> perdita della quota + mora di €400.</p>
            <p>Il roster è considerato <strong className="text-white">definitivo dal 5 Luglio</strong>: non saranno ammessi cambi o nuove iscrizioni di giocatori dopo tale data.</p>
          </Section>

          {/* ── FORMULA DEL TORNEO ── */}
          <Section icon={BadgeInfo} title="Formula del Torneo">
            <div className="border-l-4 border-brand-blue pl-4 mb-4">
              <h4 className="font-display text-white uppercase text-lg mb-2">Fase a Gironi — Mer 8, Gio 9 e Ven 10 Luglio</h4>
              <p>Ogni squadra gioca <strong className="text-white">3 partite</strong>, suddivise tra mercoledì, giovedì e venerdì.</p>
              <p>Il calendario è già stabilito; i gironi saranno estratti casualmente.</p>
            </div>
            <p><strong className="text-white">Criteri in caso di parità:</strong></p>
            <ol className="list-decimal pl-5 space-y-1 text-zinc-400">
              <li>Scontro diretto</li>
              <li>Differenza canestri</li>
              <li>Punti fatti</li>
              <li>Sorteggio</li>
            </ol>
            <div className="border-l-4 border-brand-orange pl-4 mt-4">
              <h4 className="font-display text-white uppercase text-lg mb-2">Fase Finale — Sabato 11 Luglio</h4>
              <div className="overflow-x-auto">
                <table className="w-full text-sm border border-zinc-800 mt-2">
                  <thead>
                    <tr className="bg-zinc-950 text-zinc-500 font-display text-xs uppercase tracking-widest">
                      <th className="text-left px-3 py-2">Evento</th>
                      <th className="text-center px-3 py-2">Inizio</th>
                      <th className="text-center px-3 py-2">Fine</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-zinc-800">
                    <tr><td className="px-3 py-2 text-zinc-300">Semifinale 1 (1ª A vs 2ª B)</td><td className="px-3 py-2 text-center text-zinc-400">20:00</td><td className="px-3 py-2 text-center text-zinc-400">20:45</td></tr>
                    <tr><td className="px-3 py-2 text-zinc-300">Semifinale 2 (1ª B vs 2ª A)</td><td className="px-3 py-2 text-center text-zinc-400">20:45</td><td className="px-3 py-2 text-center text-zinc-400">21:30</td></tr>
                    <tr><td className="px-3 py-2 text-brand-yellow">3 Point Contest</td><td className="px-3 py-2 text-center text-zinc-400">21:30</td><td className="px-3 py-2 text-center text-zinc-400">22:15</td></tr>
                    <tr><td className="px-3 py-2 text-zinc-300">Finale 3°-4° posto</td><td className="px-3 py-2 text-center text-zinc-400">22:15</td><td className="px-3 py-2 text-center text-zinc-400">23:00</td></tr>
                    <tr><td className="px-3 py-2 text-brand-orange font-bold">Finale 1°-2° posto</td><td className="px-3 py-2 text-center text-zinc-400">23:00</td><td className="px-3 py-2 text-center text-zinc-400">23:45</td></tr>
                    <tr><td className="px-3 py-2 text-zinc-300">Premiazione</td><td className="px-3 py-2 text-center text-zinc-400">23:45</td><td className="px-3 py-2 text-center text-zinc-400">00:15</td></tr>
                  </tbody>
                </table>
              </div>
            </div>
          </Section>

          {/* ── REGOLE DI GIOCO ── */}
          <Section icon={Timer} title="Regole di Gioco">
            <p><strong className="text-white">Tempi di gioco:</strong> 2 tempi da <span className="text-brand-orange font-bold">12 minuti</span> a tempo continuato. Il cronometro si ferma solo per tiri liberi, palla a due, palla che esce dalla rete o espulsione. Negli <strong className="text-white">ultimi 90 secondi</strong> dell'ultimo quarto il tempo si ferma ad ogni interruzione.</p>
            <p className="text-zinc-400 text-sm border-l-2 border-zinc-700 pl-3">Per le finali 3°/4° e 1°/2° posto: tempi da 15 minuti, cronometro fermo negli ultimi 120 secondi. Comportamenti palesemente mirati a perdere tempo saranno sanzionati con fallo tecnico.</p>

            <p><strong className="text-white">Punteggio massimo:</strong> la partita finisce se una squadra raggiunge <span className="text-brand-orange font-bold">51 punti</span>. Questa regola <strong className="text-red-400">non si applica</strong> nelle fasi finali (semifinali e finali).</p>

            <p><strong className="text-white">Supplementari:</strong> in caso di parità, supplementari da 2 minuti fino a determinare una vincente.</p>

            <p><strong className="text-white">Intervallo:</strong> 2 minuti tra primo e secondo tempo.</p>

            <p><strong className="text-white">Time-out:</strong> 2 per squadra (uno per tempo, 30 secondi), richiedibili da giocatori in campo o dalla panchina, solo in possesso.</p>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-2">
              <div className="bg-zinc-950 border border-zinc-800 p-3">
                <p className="font-display text-xs uppercase tracking-widest text-red-400 mb-1">Falli personali</p>
                <p className="text-white font-bold">Al 4° fallo il giocatore è espulso</p>
              </div>
              <div className="bg-zinc-950 border border-zinc-800 p-3">
                <p className="font-display text-xs uppercase tracking-widest text-brand-yellow mb-1">Bonus squadra</p>
                <p className="text-white font-bold">Dal 5° fallo: 2 tiri liberi</p>
              </div>
            </div>

            <p><strong className="text-white">Palla a due:</strong> tradizionale. Ogni gara inizia con palla a due. In caso di palla contesa, si effettua sempre una vera palla a due tra i giocatori coinvolti, anche dalla linea del tiro libero come in NBA (niente possesso alternato).</p>

            <p><strong className="text-white">Infrazioni:</strong> regole FIBA (3, 5, 8, 24 secondi).</p>

            <p><strong className="text-white">Valore canestri:</strong> 2 punti (entro arco), 3 punti (fuori arco), 1 punto (tiri liberi).</p>

            <p><strong className="text-white">Riscaldamento:</strong> 5 minuti.</p>

            <p><strong className="text-white">Cambi:</strong> liberi a palla ferma, davanti al tavolo e con autorizzazione dell'arbitro.</p>

            <p><strong className="text-white">Numero minimo:</strong> almeno <span className="text-brand-orange font-bold">5 giocatori in campo</span>. Se per espulsioni o infortuni si scende sotto, la partita viene interrotta e assegnata a tavolino (20-0).</p>
          </Section>

          {/* ── ARBITRAGGIO E DISCIPLINA ── */}
          <Section icon={Shield} title="Arbitraggio e Disciplina">
            <p><strong className="text-white">Arbitri:</strong> tutte le partite saranno dirette da ufficiali designati dall'organizzazione.</p>
            <p><strong className="text-white">Falli tecnici e antisportivi:</strong> sanzionati secondo regolamento FIP. In caso di espulsione grave, l'organizzazione valuterà sospensioni per le gare successive.</p>
            <p><strong className="text-white">Panchina:</strong> soggetta al regolamento tecnico; eventuali infrazioni potranno comportare sanzioni.</p>
            <p><strong className="text-white">Pubblico:</strong> l'organizzazione si riserva il diritto di allontanare chiunque tenga comportamenti offensivi, provocatori o violenti. In caso di episodi gravi, la partita potrà essere interrotta.</p>
          </Section>

          {/* ── DIVISE ── */}
          <Section icon={Users} title="Divise">
            <p>Ogni squadra riceve un <strong className="text-white">completino ufficiale</strong> (maglia e pantaloncini).</p>
            <p>Le squadre giocano sempre con lo stesso completino.</p>
          </Section>

          {/* ── 3 POINT CONTEST ── */}
          <Section icon={Target} title="3 Point Contest">
            <p>Ogni squadra può iscrivere <strong className="text-white">uno e un solo giocatore</strong> regolarmente registrato nel roster. Il nome va comunicato entro mercoledì 8 luglio.</p>
            <p><strong className="text-white">Regole:</strong></p>
            <ul className="list-disc pl-5 space-y-1 text-zinc-400">
              <li>5 postazioni fisse dietro la linea dei 3 punti</li>
              <li>Ogni postazione: 4 palloni da 1 punto + 1 "money ball" da 2 punti</li>
              <li>Tempo limite: <strong className="text-white">90 secondi</strong></li>
              <li>Tiri esclusivamente dietro la linea (piede sulla linea = non valido)</li>
              <li>Palloni tirati uno alla volta, non si può tornare indietro</li>
            </ul>
            <p><strong className="text-white">Finale:</strong> i 3 migliori punteggi accedono a una finale secca. In caso di parità: mini-sfida da 3 postazioni.</p>
          </Section>

          {/* ── PREMI ── */}
          <Section icon={Trophy} title="Premi">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="bg-zinc-950 border border-brand-yellow/30 p-4 flex items-center gap-3">
                <span className="text-2xl">🥇</span>
                <div><p className="text-white font-bold">1ª classificata</p><p className="text-zinc-500 text-sm">Trofeo + premi</p></div>
              </div>
              <div className="bg-zinc-950 border border-zinc-700 p-4 flex items-center gap-3">
                <span className="text-2xl">🥈</span>
                <div><p className="text-white font-bold">2ª classificata</p><p className="text-zinc-500 text-sm">Trofeo + premi</p></div>
              </div>
              <div className="bg-zinc-950 border border-zinc-700 p-4 flex items-center gap-3">
                <span className="text-2xl">🥉</span>
                <div><p className="text-white font-bold">3ª classificata</p><p className="text-zinc-500 text-sm">Trofeo + premi</p></div>
              </div>
              <div className="bg-zinc-950 border border-brand-orange/30 p-4 flex items-center gap-3">
                <span className="text-2xl">🏀</span>
                <div><p className="text-white font-bold">MVP del Torneo</p><p className="text-zinc-500 text-sm">Trofeo + premio</p></div>
              </div>
              <div className="bg-zinc-950 border border-brand-blue/30 p-4 flex items-center gap-3">
                <span className="text-2xl">🛡️</span>
                <div><p className="text-white font-bold">Best Defensive Player</p><p className="text-zinc-500 text-sm">Trofeo + premio</p></div>
              </div>
              <div className="bg-zinc-950 border border-brand-yellow/30 p-4 flex items-center gap-3">
                <span className="text-2xl">🎯</span>
                <div><p className="text-white font-bold">3 Point Contest</p><p className="text-zinc-500 text-sm">Trofeo + premio</p></div>
              </div>
              <div className="bg-zinc-950 border border-zinc-700 p-4 flex items-center gap-3 sm:col-span-2">
                <span className="text-2xl">🏀</span>
                <div><p className="text-white font-bold">Ultima classificata</p><p className="text-zinc-500 text-sm">Buono da €15 da CS Sport — esclusivamente per scarpe da basket</p></div>
              </div>
            </div>
          </Section>

          {/* ── COMUNICAZIONI ── */}
          <Section icon={MessageSquare} title="Comunicazioni">
            <p><strong className="text-white">Calendario e risultati:</strong> pubblicati sul gruppo WhatsApp dei capitani e sul sito ufficiale del torneo.</p>
            <p><strong className="text-white">Referente di squadra:</strong> il <span className="text-brand-orange">capitano</span>, unico autorizzato a comunicare con l'organizzazione e responsabile della condotta della squadra.</p>
            <p>Le comunicazioni inviate nel gruppo WhatsApp dei capitani sono considerate <strong className="text-white">ufficiali a tutti gli effetti</strong>.</p>
          </Section>

          {/* ── SITUAZIONI STRAORDINARIE ── */}
          <Section icon={AlertTriangle} title="Situazioni Straordinarie">
            <div className="space-y-4">
              <div className="border-l-4 border-zinc-600 pl-4">
                <h4 className="font-display text-white uppercase text-base mb-1">Infortuni</h4>
                <p className="text-zinc-400">Il gioco prosegue regolarmente, fermandosi solo per permettere l'assistenza.</p>
              </div>
              <div className="border-l-4 border-brand-blue pl-4">
                <h4 className="font-display text-white uppercase text-base mb-1">Pioggia o maltempo</h4>
                <p className="text-zinc-400">L'organizzazione valuterà la situazione. Si potrà utilizzare un campo coperto alternativo (se disponibile) o posticipare la gara.</p>
              </div>
              <div className="border-l-4 border-brand-orange pl-4">
                <h4 className="font-display text-white uppercase text-base mb-1">Interruzioni</h4>
                <p className="text-zinc-400">Se breve, si riprende dal punteggio interrotto. Altrimenti, l'organizzazione decide se recuperare o annullare.</p>
              </div>
              <div className="border-l-4 border-red-500 pl-4">
                <h4 className="font-display text-white uppercase text-base mb-1">Espulsioni e abbandono</h4>
                <p className="text-zinc-400">In caso di tripla espulsione, rissa o abbandono: sconfitta automatica 20-0, possibile esclusione dal torneo, sospensione dei giocatori coinvolti per le partite successive.</p>
              </div>
            </div>
          </Section>

        </div>
      </motion.div>
    </div>
  );
}
