using BasketPdfStats.Core.Pipeline;

namespace BasketPdfStats.App.Services;

/// <summary>
/// Popup WPF mostrata dalla pipeline (dopo CH1) quando le squadre del PDF non corrispondono al match
/// selezionato. L'utente sceglie se interrompere l'elaborazione (consigliato) o completarla comunque.
/// In entrambi i casi l'import resta bloccato a valle.
/// </summary>
public sealed class WpfTeamMismatchProcessingDecider : ITeamMismatchProcessingDecider
{
    public bool ShouldContinueProcessing(TeamMismatchPrompt prompt)
    {
        var nl = System.Environment.NewLine;
        var message =
            "Il PDF selezionato non corrisponde al match scelto." + nl + nl +
            $"Match selezionato: {prompt.SelectedHomeTeam} vs {prompt.SelectedAwayTeam}" + nl +
            $"PDF rilevato: {prompt.PdfHomeTeam} vs {prompt.PdfAwayTeam}" + nl + nl +
            "Vuoi INTERROMPERE l'elaborazione? (consigliato)" + nl +
            "• Sì = interrompi ora (non esegue gli altri canali OCR)." + nl +
            "• No = completa comunque l'OCR (l'import resta comunque bloccato).";

        // La pipeline può chiamare da un thread diverso da quello UI: marshalla sul Dispatcher.
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        System.Windows.MessageBoxResult Show() => System.Windows.MessageBox.Show(
            message,
            "PDF non corrispondente al match",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning,
            System.Windows.MessageBoxResult.Yes); // default consigliato: interrompi

        var result = dispatcher is null || dispatcher.CheckAccess()
            ? Show()
            : dispatcher.Invoke(Show);

        // Sì (interrompi) → non continuare; No (continua) → continua.
        return result == System.Windows.MessageBoxResult.No;
    }
}
