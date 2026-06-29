import { motion } from "motion/react";
import { ShieldCheck, Cookie, User, Clock, Mail, FileText, AlertTriangle } from "lucide-react";

const Section = ({ icon: Icon, title, children }: { icon: any; title: string; children: React.ReactNode }) => (
  <section className="bg-zinc-900 border border-zinc-800 p-6 sm:p-8 relative overflow-hidden">
    <div className="flex items-start gap-3 sm:gap-4 mb-6 border-t-4 border-brand-orange pt-6">
      <Icon className="w-7 h-7 sm:w-8 sm:h-8 text-brand-orange flex-shrink-0 mt-1" />
      <h2 className="font-display text-2xl sm:text-3xl uppercase text-white leading-tight">{title}</h2>
    </div>
    <div className="font-sans text-zinc-300 space-y-4 leading-relaxed text-base">
      {children}
    </div>
  </section>
);

export function Privacy() {
  const lastUpdate = "Maggio 2026";

  return (
    <div className="pt-32 pb-20 max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}>

        <div className="mb-12">
          <h1 className="font-display text-[44px] sm:text-[70px] md:text-[120px] text-brand-orange uppercase leading-[0.85] mb-6 tracking-[-2px] md:tracking-[-4px]">
            Privacy &amp;<br/>Cookie Policy
          </h1>
          <p className="font-sans text-zinc-400 text-lg">
            Informativa ai sensi dell'art. 13 del Regolamento UE 2016/679 (GDPR)
            e del D.Lgs. 196/2003 come modificato dal D.Lgs. 101/2018.
          </p>
          <p className="font-sans text-zinc-500 text-sm mt-2">
            Ultimo aggiornamento: {lastUpdate}
          </p>
        </div>

        {/* Avviso minorenni */}
        <div className="border-[3px] border-brand-yellow bg-brand-yellow/10 p-6 mb-10 flex gap-4 items-start">
          <AlertTriangle className="w-8 h-8 text-brand-yellow flex-shrink-0 mt-0.5" />
          <div>
            <h3 className="font-display text-xl uppercase text-brand-yellow mb-2">
              Nota sui partecipanti minorenni
            </h3>
            <p className="font-sans text-zinc-300 text-sm leading-relaxed">
              Il trattamento dei dati personali di partecipanti di età inferiore ai 18 anni avviene
              esclusivamente previo consenso scritto del genitore o tutore legale, acquisito in formato
              cartaceo prima dell'inizio del torneo. I dati dei minorenni non sono raccolti tramite
              questo sito web. La firma della liberatoria cartacea è condizione necessaria per la
              partecipazione al torneo.
            </p>
          </div>
        </div>

        <div className="space-y-6">

          <Section icon={User} title="1. Titolare del Trattamento">
            <p>
              Il Titolare del Trattamento dei dati personali è:
            </p>
            <div className="bg-zinc-950 border border-zinc-700 p-4 font-mono text-xs sm:text-sm space-y-1 break-words">
              <p><strong className="text-white">Francesco Emiliani</strong></p>
              <p>Residenza: Via Vespucci 12, Porto Potenza Picena (MC)</p>
              <p>C.F.: MLNFNC98L22H211Z</p>
              <p>Email: <a href="mailto:f.emiliani@piazzettamadness.it" className="text-brand-orange hover:underline break-all">f.emiliani@piazzettamadness.it</a></p>
            </div>
            <p className="text-zinc-400 text-sm">
              Per esercitare i tuoi diritti o per qualsiasi domanda relativa al trattamento dei tuoi
              dati, puoi contattarci all'indirizzo email sopra indicato.
            </p>
          </Section>

          <Section icon={FileText} title="2. Dati raccolti e finalità">
            <p>
              Raccogliamo e trattiamo i dati personali esclusivamente per le finalità connesse
              all'organizzazione e alla gestione del torneo <strong className="text-white">Piazzetta Madness</strong>.
            </p>

            <div className="space-y-4">
              <div className="border-l-4 border-brand-blue pl-4">
                <h4 className="font-display text-white uppercase text-lg mb-2">Modulo di iscrizione squadre</h4>
                <p>I dati raccolti tramite il form di iscrizione sono:</p>
                <ul className="list-disc pl-5 space-y-1 mt-2 text-zinc-400">
                  <li>Nome e cognome del capitano</li>
                  <li>Indirizzo email del capitano</li>
                  <li>Numero di telefono del capitano</li>
                  <li>Nome della squadra</li>
                  <li>Elenco opzionale dei giocatori (nomi e cognomi)</li>
                </ul>
                <p className="mt-2 text-sm text-zinc-500">
                  <strong>Base giuridica:</strong> esecuzione del contratto/accordo di partecipazione (art. 6, par. 1, lett. b GDPR),
                  adempimento di obblighi organizzativi e di sicurezza (lett. c) e consenso dell'interessato per la pubblicazione di immagini (lett. a).
                </p>
              </div>

              <div className="border-l-4 border-brand-orange pl-4">
                <h4 className="font-display text-white uppercase text-lg mb-2">Moduli cartacei individuali</h4>
                <p>
                  Per ogni partecipante al torneo vengono raccolti, tramite moduli cartacei firmati, i seguenti dati:
                </p>
                <ul className="list-disc pl-5 space-y-1 mt-2 text-zinc-400">
                  <li>Nome e cognome</li>
                  <li>Data di nascita</li>
                  <li>Codice fiscale (esclusivamente per finalità organizzative e assicurative)</li>
                  <li>Squadra di appartenenza</li>
                </ul>
                <p className="mt-2 text-zinc-400">
                  Per i partecipanti minorenni, i medesimi dati sono raccolti tramite liberatoria firmata dal genitore o tutore legale,
                  unitamente ai dati identificativi del genitore/tutore e copia del documento d'identità.
                </p>
                <p className="mt-2 text-sm text-zinc-500">
                  <strong>Base giuridica:</strong> esecuzione del contratto/accordo di partecipazione (art. 6, par. 1, lett. b GDPR)
                  e adempimento di obblighi organizzativi e di sicurezza (lett. c).
                  Data di nascita, codice fiscale e documenti personali non saranno pubblicati online.
                </p>
              </div>

              <div className="border-l-4 border-zinc-600 pl-4">
                <h4 className="font-display text-white uppercase text-lg mb-2">Dati di navigazione</h4>
                <p>
                  I sistemi informatici acquisiscono automaticamente alcuni dati tecnici (indirizzo IP,
                  tipo di browser, sistema operativo, pagine visitate) necessari al funzionamento del sito.
                  Questi dati non sono associati a utenti identificabili e vengono eliminati dopo 30 giorni.
                </p>
                <p className="mt-2 text-sm text-zinc-500">
                  <strong>Base giuridica:</strong> legittimo interesse del Titolare (art. 6, par. 1, lett. f GDPR).
                </p>
              </div>

              <div className="border-l-4 border-brand-blue pl-4">
                <h4 className="font-display text-white uppercase text-lg mb-2">Statistiche di accesso (Google Analytics)</h4>
                <p>
                  Solo previo tuo consenso, utilizziamo Google Analytics 4 per raccogliere dati
                  statistici anonimi sugli accessi al sito (numero di visitatori, pagine più visitate,
                  picchi di traffico, provenienza geografica aggregata, tipo di dispositivo).
                  L'IP viene anonimizzato prima di qualsiasi elaborazione.
                </p>
                <p className="mt-2 text-sm text-zinc-500">
                  <strong>Base giuridica:</strong> consenso dell'interessato (art. 6, par. 1, lett. a GDPR).
                  Puoi revocare il consenso in qualsiasi momento cancellando i dati del browser o
                  contattandoci via email.
                </p>
              </div>

              <div className="border-l-4 border-brand-blue pl-4">
                <h4 className="font-display text-white uppercase text-lg mb-2">Mappa interattiva (Google Maps)</h4>
                <p>
                  Solo previo tuo consenso, incorporiamo una mappa Google Maps per mostrare la
                  posizione del campo di gioco. Il caricamento della mappa comporta l'invio del tuo
                  indirizzo IP a Google LLC e l'impostazione di cookie tecnici da parte di Google.
                  Se non presti consenso, la mappa non viene caricata.
                </p>
                <p className="mt-2 text-sm text-zinc-500">
                  <strong>Base giuridica:</strong> consenso dell'interessato (art. 6, par. 1, lett. a GDPR).
                </p>
              </div>

              <div className="border-l-4 border-zinc-600 pl-4">
                <h4 className="font-display text-white uppercase text-lg mb-2">Immagini e foto — Partecipanti</h4>
                <p>
                  Le fotografie scattate durante il torneo che ritraggono i partecipanti possono essere
                  pubblicate sui canali social e sul sito dell'evento. Il consenso alla pubblicazione
                  delle immagini viene raccolto tramite apposita liberatoria cartacea firmata prima
                  dell'inizio del torneo. Per i minorenni è richiesta la firma del genitore o tutore.
                </p>
                <p className="mt-2 text-sm text-zinc-500">
                  <strong>Base giuridica:</strong> consenso esplicito dell'interessato (art. 6, par. 1, lett. a GDPR).
                </p>
              </div>

              <div className="border-l-4 border-zinc-600 pl-4">
                <h4 className="font-display text-white uppercase text-lg mb-2">Immagini e foto — Pubblico e spettatori</h4>
                <p>
                  Il torneo si svolge in uno spazio pubblico aperto (piazzetta). Durante le riprese
                  fotografiche e video a fini documentali dell'evento, è possibile che vengano
                  inquadrate persone presenti come spettatori o di passaggio, in modo non intenzionale
                  e non come soggetti principali dello scatto.
                </p>
                <p className="mt-2">
                  Tali immagini possono essere pubblicate sui canali social e sul sito dell'evento
                  esclusivamente per finalità di cronaca e documentazione del torneo, senza scopo
                  commerciale e senza che le persone riprese siano identificate o poste in primo piano.
                </p>
                <p className="mt-2 text-sm text-zinc-500">
                  <strong>Base giuridica:</strong> legittimo interesse del Titolare (art. 6, par. 1, lett. f GDPR)
                  e finalità di cronaca ai sensi dell'art. 137 del D.Lgs. 196/2003 (Codice Privacy),
                  in conformità con le indicazioni del Garante per la protezione dei dati personali
                  relative agli eventi sportivi amatoriali pubblici.
                  Qualora tu ritenga di essere stato ripreso in modo lesivo della tua riservatezza,
                  puoi richiedere la rimozione dell'immagine scrivendo a{" "}
                  <a href="mailto:f.emiliani@piazzettamadness.it" className="text-brand-orange hover:underline">
                    f.emiliani@piazzettamadness.it
                  </a>.
                </p>
              </div>
            </div>
          </Section>

          <Section icon={Clock} title="3. Periodo di conservazione">
            <p>
              I dati personali raccolti tramite il modulo di iscrizione vengono conservati per il tempo
              strettamente necessario alle finalità per cui sono stati raccolti:
            </p>
            <ul className="list-disc pl-5 space-y-2 text-zinc-400">
              <li>
                <strong className="text-white">Dati di iscrizione</strong>: fino a 12 mesi dalla
                conclusione dell'edizione del torneo a cui si riferiscono, salvo obblighi di legge
                che richiedano una conservazione più lunga.
              </li>
              <li>
                <strong className="text-white">Immagini e video</strong>: per tutta la durata del
                progetto Piazzetta Madness, salvo richiesta di cancellazione da parte dell'interessato.
              </li>
              <li>
                <strong className="text-white">Liberatorie cartacee minorenni</strong>: conservate
                fisicamente per tutta la durata del torneo e distrutte entro 30 giorni dalla sua
                conclusione, salvo contenziosi in corso.
              </li>
            </ul>
          </Section>

          <Section icon={User} title="4. Destinatari dei dati">
            <p>
              I tuoi dati personali non sono ceduti a terzi per finalità commerciali o di marketing.
              Possono essere comunicati esclusivamente a:
            </p>
            <ul className="list-disc pl-5 space-y-2 text-zinc-400">
              <li>Componenti dello staff organizzativo del torneo, vincolati da obbligo di riservatezza.</li>
              <li>
                Fornitori di servizi tecnici necessari alla gestione del sito web
                (hosting, server), designati come Responsabili del Trattamento ai sensi dell'art. 28 GDPR.
              </li>
              <li>
                <strong className="text-white">Google LLC</strong> — esclusivamente se hai prestato consenso,
                per Google Analytics (statistiche) e Google Maps (mappa interattiva). Google agisce come
                Responsabile del Trattamento ai sensi dell'art. 28 GDPR. I dati possono essere elaborati
                su server situati negli USA, nel rispetto delle garanzie previste dagli artt. 45-46 GDPR
                (Standard Contractual Clauses).
              </li>
              <li>
                Autorità competenti, esclusivamente nei casi previsti dalla legge.
              </li>
            </ul>
            <p>
              Ad eccezione di Google Analytics (solo con consenso), i dati non vengono trasferiti
              al di fuori dell'Unione Europea.
            </p>
          </Section>

          <Section icon={ShieldCheck} title="5. Diritti dell'interessato">
            <p>
              Ai sensi degli artt. 15-22 del GDPR, hai il diritto di:
            </p>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {[
                { title: "Accesso", desc: "Ottenere conferma se stiamo trattando tuoi dati e riceverne copia." },
                { title: "Rettifica", desc: "Correggere dati inesatti o incompleti che ti riguardano." },
                { title: "Cancellazione", desc: 'Richiedere la cancellazione ("diritto all\'oblio") nei casi previsti.' },
                { title: "Limitazione", desc: "Limitare il trattamento in determinate circostanze." },
                { title: "Portabilità", desc: "Ricevere i tuoi dati in formato strutturato e leggibile da macchina." },
                { title: "Opposizione", desc: "Opporti al trattamento basato su legittimo interesse." },
                { title: "Revoca consenso", desc: "Revocare in qualsiasi momento il consenso precedentemente prestato." },
                { title: "Reclamo", desc: "Proporre reclamo al Garante per la Protezione dei Dati Personali." },
              ].map((right) => (
                <div key={right.title} className="bg-zinc-950 border border-zinc-800 p-4">
                  <h4 className="font-display text-brand-orange uppercase text-sm mb-1">{right.title}</h4>
                  <p className="text-zinc-400 text-sm">{right.desc}</p>
                </div>
              ))}
            </div>
            <p>
              Per esercitare i tuoi diritti scrivi a:{" "}
              <a href="mailto:f.emiliani@piazzettamadness.it" className="text-brand-orange hover:underline">
                f.emiliani@piazzettamadness.it
              </a>.
              Risponderemo entro 30 giorni dalla ricezione della richiesta.
            </p>
            <p className="text-zinc-400 text-sm">
              Puoi proporre reclamo al Garante per la Protezione dei Dati Personali (
              <span className="text-zinc-300">www.garanteprivacy.it</span>
              ), Piazza Venezia 11, 00187 Roma.
            </p>
          </Section>

          <Section icon={Cookie} title="6. Cookie e tecnologie simili">
            <p>
              Questo sito utilizza <strong className="text-white">cookie tecnici necessari</strong> e,
              solo previo consenso, <strong className="text-white">cookie analitici</strong> di Google Analytics
              e cookie di terze parti di Google Maps per la mappa interattiva.
              Non utilizziamo cookie di profilazione o di marketing.
            </p>

            <h4 className="font-display text-white uppercase text-sm tracking-widest mt-2">Cookie tecnici (sempre attivi)</h4>
            <div className="overflow-hidden border border-zinc-700">
              <table className="w-full text-sm text-left">
                <thead className="bg-zinc-800">
                  <tr>
                    <th className="p-3 font-display uppercase text-zinc-300 text-xs tracking-widest">Nome</th>
                    <th className="p-3 font-display uppercase text-zinc-300 text-xs tracking-widest">Tipo</th>
                    <th className="p-3 font-display uppercase text-zinc-300 text-xs tracking-widest">Finalita'</th>
                    <th className="p-3 font-display uppercase text-zinc-300 text-xs tracking-widest">Durata</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-zinc-800">
                  <tr className="bg-zinc-950">
                    <td className="p-3 font-mono text-brand-orange">pm_cookie_consent</td>
                    <td className="p-3 text-zinc-400">localStorage</td>
                    <td className="p-3 text-zinc-400">Memorizza la scelta dell'utente sul banner cookie</td>
                    <td className="p-3 text-zinc-400">Persistente</td>
                  </tr>
                  <tr className="bg-zinc-950/50">
                    <td className="p-3 font-mono text-brand-orange">madness_scoreboard</td>
                    <td className="p-3 text-zinc-400">localStorage</td>
                    <td className="p-3 text-zinc-400">Sincronizzazione tabellone live tra finestre (solo staff)</td>
                    <td className="p-3 text-zinc-400">Sessione</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <h4 className="font-display text-white uppercase text-sm tracking-widest mt-4">Cookie di terze parti (solo con consenso)</h4>
            <div className="overflow-hidden border border-zinc-700">
              <table className="w-full text-sm text-left">
                <thead className="bg-zinc-800">
                  <tr>
                    <th className="p-3 font-display uppercase text-zinc-300 text-xs tracking-widest">Nome</th>
                    <th className="p-3 font-display uppercase text-zinc-300 text-xs tracking-widest">Terza parte</th>
                    <th className="p-3 font-display uppercase text-zinc-300 text-xs tracking-widest">Finalita'</th>
                    <th className="p-3 font-display uppercase text-zinc-300 text-xs tracking-widest">Durata</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-zinc-800">
                  <tr className="bg-zinc-950">
                    <td className="p-3 font-mono text-brand-blue">_ga</td>
                    <td className="p-3 text-zinc-400">Google Analytics</td>
                    <td className="p-3 text-zinc-400">Distingue gli utenti unici (ID anonimo)</td>
                    <td className="p-3 text-zinc-400">2 anni</td>
                  </tr>
                  <tr className="bg-zinc-950/50">
                    <td className="p-3 font-mono text-brand-blue">_ga_*</td>
                    <td className="p-3 text-zinc-400">Google Analytics</td>
                    <td className="p-3 text-zinc-400">Mantiene lo stato della sessione</td>
                    <td className="p-3 text-zinc-400">2 anni</td>
                  </tr>
                  <tr className="bg-zinc-950">
                    <td className="p-3 font-mono text-brand-blue">NID, CONSENT</td>
                    <td className="p-3 text-zinc-400">Google Maps</td>
                    <td className="p-3 text-zinc-400">Funzionamento mappa interattiva e preferenze</td>
                    <td className="p-3 text-zinc-400">6 mesi – 2 anni</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <p>
              I cookie tecnici non richiedono consenso ai sensi delle Linee Guida del Garante del 10 giugno 2021.
              I cookie analitici di Google Analytics vengono attivati solo dopo il tuo consenso esplicito.
            </p>
            <p className="text-zinc-400 text-sm">
              Puoi revocare il consenso in qualsiasi momento cancellando i dati del browser:
              Impostazioni → Privacy → Cancella dati di navigazione.
            </p>
          </Section>

          <Section icon={Mail} title="7. Modifiche alla presente informativa">
            <p>
              Il Titolare si riserva il diritto di modificare la presente informativa in qualsiasi momento,
              in particolare in seguito a variazioni normative o all'introduzione di nuovi servizi
              (es. analytics, sistema di iscrizioni online con raccolta documentale).
            </p>
            <p>
              Le modifiche saranno pubblicate su questa pagina con aggiornamento della data in calce.
              Per trattamenti sostanzialmente diversi da quelli descritti, sarà richiesto nuovo consenso.
            </p>
            <p className="text-zinc-500 text-sm">
              Versione corrente: {lastUpdate} — Prima versione pubblicata.
            </p>
          </Section>

        </div>
      </motion.div>
    </div>
  );
}
