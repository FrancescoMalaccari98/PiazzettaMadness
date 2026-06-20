// Sostituire G-XXXXXXXXXX con il Measurement ID reale da Google Analytics 4
// (es. G-AB12CD34EF) quando disponibile.
const GA_ID = "G-D8D7S7EWWH";

export function initGA(): void {
  if (document.getElementById("ga-script")) return; // già caricato

  const script = document.createElement("script");
  script.id = "ga-script";
  script.src = `https://www.googletagmanager.com/gtag/js?id=${GA_ID}`;
  script.async = true;
  document.head.appendChild(script);

  (window as any).dataLayer = (window as any).dataLayer || [];
  (window as any).gtag = function () {
    // eslint-disable-next-line prefer-rest-params
    (window as any).dataLayer.push(arguments);
  };
  (window as any).gtag("js", new Date());
  // anonymize_ip: oscura l'ultimo ottetto dell'IP — buona pratica GDPR
  (window as any).gtag("config", GA_ID, { anonymize_ip: true });
}
