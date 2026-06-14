namespace BasketPdfStats.App.Services;

public sealed class WpfTeamMismatchConfirmationService : ITeamMismatchConfirmationService
{
    public bool ShouldContinue(TeamMismatchWarning warning)
    {
        var message =
            "Le squadre lette dal PDF non corrispondono alla partita selezionata." +
            $"{System.Environment.NewLine}{System.Environment.NewLine}" +
            $"PDF: {warning.PdfHomeTeam} vs {warning.PdfAwayTeam}" +
            $"{System.Environment.NewLine}" +
            $"Match Teams: {warning.MatchHomeTeam} vs {warning.MatchAwayTeam}" +
            $"{System.Environment.NewLine}{System.Environment.NewLine}" +
            "Vuoi importare comunque nel DB?";

        return System.Windows.MessageBox.Show(
            message,
            "Squadre non corrispondenti",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes;
    }
}
