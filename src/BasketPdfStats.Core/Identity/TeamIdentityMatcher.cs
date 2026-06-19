using BasketPdfStats.Core.Database;

namespace BasketPdfStats.Core.Identity;

/// <summary>
/// Confronta i nomi squadra letti dall'OCR (header del PDF, da CH1) con le squadre
/// canoniche del DB e stabilisce se l'ordine è corretto, invertito o non riconosciuto.
/// Esegue dopo CH1, non prima dell'OCR.
/// </summary>
public sealed class TeamIdentityMatcher
{
    private readonly double _matchThreshold;

    /// <param name="matchThreshold">Similarità media minima per accettare un accoppiamento (default 0.5).</param>
    public TeamIdentityMatcher(double matchThreshold = 0.5)
    {
        _matchThreshold = matchThreshold;
    }

    public TeamMatchResult Match(string? homeOcrName, string? awayOcrName, OcrMatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var homeOcr = NameNormalizer.Normalize(homeOcrName);
        var awayOcr = NameNormalizer.Normalize(awayOcrName);
        var homeDb = NameNormalizer.Normalize(context.HomeTeam.Name);
        var awayDb = NameNormalizer.Normalize(context.AwayTeam.Name);

        var correctScore = (NameSimilarity.Similarity(homeOcr, homeDb) + NameSimilarity.Similarity(awayOcr, awayDb)) / 2.0;
        var invertedScore = (NameSimilarity.Similarity(homeOcr, awayDb) + NameSimilarity.Similarity(awayOcr, homeDb)) / 2.0;

        TeamMatchOutcome outcome;
        if (Math.Max(correctScore, invertedScore) < _matchThreshold)
        {
            outcome = TeamMatchOutcome.Mismatch;
        }
        else
        {
            outcome = invertedScore > correctScore ? TeamMatchOutcome.Inverted : TeamMatchOutcome.CorrectOrder;
        }

        return new TeamMatchResult
        {
            Outcome = outcome,
            CorrectScore = correctScore,
            InvertedScore = invertedScore
        };
    }
}
