using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Core.Identity;

/// <summary>
/// Raccoglie le voci di revisione confrontando i giocatori OCR del risultato con il roster DB,
/// usando <see cref="PlayerIdentityMatcher"/>. Sola lettura: non modifica il risultato (la
/// risoluzione canonica con scrittura di playerId è prevista in Fase 8).
/// </summary>
public sealed class IdentityReviewBuilder
{
    private readonly PlayerIdentityMatcher _matcher;

    public IdentityReviewBuilder(PlayerIdentityMatcher? matcher = null)
    {
        _matcher = matcher ?? new PlayerIdentityMatcher();
    }

    public List<IdentityReviewItem> Build(ProcessingResult result, OcrMatchContext context)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(context);

        var items = new List<IdentityReviewItem>();
        BuildForSide(result, "Home", context.HomeTeam.Players, items);
        BuildForSide(result, "Away", context.AwayTeam.Players, items);
        return items;
    }

    private void BuildForSide(ProcessingResult result, string side, IReadOnlyList<OcrRosterPlayer> roster, List<IdentityReviewItem> items)
    {
        var matchedPlayerIds = new HashSet<int>();
        var ocrPlayers = result.Players.Where(player => string.Equals(player.Side, side, StringComparison.OrdinalIgnoreCase));

        foreach (var ocr in ocrPlayers)
        {
            var match = _matcher.Match(ocr.Number, ocr.FullName, roster);
            switch (match.Kind)
            {
                case PlayerMatchKind.CertainMatch:
                    matchedPlayerIds.Add(match.Player!.PlayerId);
                    break;

                case PlayerMatchKind.ProbableMatch:
                    matchedPlayerIds.Add(match.Player!.PlayerId);
                    items.Add(ToItem(side, IdentityReviewReason.ProbableMatch, ocr, match));
                    break;

                case PlayerMatchKind.Conflict:
                    // Il giocatore è stato individuato per jersey (il nome è incompatibile):
                    // marcalo come trovato per non segnalarlo anche come NotInPdf.
                    matchedPlayerIds.Add(match.Player!.PlayerId);
                    items.Add(ToItem(side, IdentityReviewReason.Conflict, ocr, match));
                    break;

                case PlayerMatchKind.NumberNotFound:
                    items.Add(ToItem(side, IdentityReviewReason.NumberNotFound, ocr, match));
                    break;

                case PlayerMatchKind.Unmatched:
                    items.Add(ToItem(side, IdentityReviewReason.Unmatched, ocr, match));
                    break;
            }
        }

        // Giocatori del roster DB non trovati nel PDF.
        foreach (var dbPlayer in roster)
        {
            if (!matchedPlayerIds.Contains(dbPlayer.PlayerId))
            {
                items.Add(new IdentityReviewItem
                {
                    Side = side,
                    Reason = IdentityReviewReason.NotInPdf,
                    CandidatePlayerId = dbPlayer.PlayerId,
                    CandidateName = FullName(dbPlayer),
                    ConfidenceScore = 0.0
                });
            }
        }
    }

    private static IdentityReviewItem ToItem(string side, IdentityReviewReason reason, PlayerStats ocr, PlayerMatchResult match) => new()
    {
        Side = side,
        Reason = reason,
        OcrEntityId = ocr.EntityId,
        OcrJersey = ocr.Number,
        OcrName = ocr.FullName,
        CandidatePlayerId = match.Player?.PlayerId,
        CandidateName = match.Player is null ? null : FullName(match.Player),
        ConfidenceScore = match.NameMissing ? 0.0 : Math.Round(1.0 - match.NameDistance, 3)
    };

    private static string FullName(OcrRosterPlayer player) => $"{player.FirstName} {player.LastName}".Trim();
}
