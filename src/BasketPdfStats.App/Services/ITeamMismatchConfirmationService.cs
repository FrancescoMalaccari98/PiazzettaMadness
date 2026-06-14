namespace BasketPdfStats.App.Services;

public interface ITeamMismatchConfirmationService
{
    bool ShouldContinue(TeamMismatchWarning warning);
}

public sealed record TeamMismatchWarning(
    string PdfHomeTeam,
    string PdfAwayTeam,
    string MatchHomeTeam,
    string MatchAwayTeam);
