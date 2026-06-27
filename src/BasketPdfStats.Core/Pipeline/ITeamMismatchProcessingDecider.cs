namespace BasketPdfStats.Core.Pipeline;

/// <summary>
/// Decisione utente quando, dopo CH1, le squadre lette dal PDF non corrispondono al match
/// selezionato. Permette di interrompere l'elaborazione PRIMA di eseguire i canali successivi
/// (CH2–CH4) anziché completare inutilmente l'OCR di un PDF sbagliato.
/// L'implementazione UI vive nel layer App (la pipeline conosce solo questa interfaccia).
/// </summary>
public interface ITeamMismatchProcessingDecider
{
    /// <returns>
    /// true = continuare l'elaborazione (esegue CH2–CH4); false = interrompere (azione consigliata).
    /// In entrambi i casi l'import resta bloccato: questa scelta riguarda solo se completare l'OCR.
    /// </returns>
    bool ShouldContinueProcessing(TeamMismatchPrompt prompt);
}

/// <summary>Dati per il prompt di mismatch: squadre del match scelto vs squadre lette dal PDF.</summary>
public sealed record TeamMismatchPrompt(
    string SelectedHomeTeam,
    string SelectedAwayTeam,
    string PdfHomeTeam,
    string PdfAwayTeam);
