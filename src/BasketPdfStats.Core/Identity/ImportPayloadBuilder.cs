using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Core.Identity;

/// <summary>
/// Costruisce l'<see cref="ImportPayload"/> canonico da un <see cref="ProcessingResult"/> e dal
/// contesto DB. Risolve i playerId tramite <see cref="PlayerIdentityMatcher"/> (match certi/probabili)
/// e applica le scelte manuali della revisione (override entityId → playerId). I giocatori non
/// risolti vengono esclusi dal payload (restano nelle voci di revisione).
/// </summary>
public sealed class ImportPayloadBuilder
{
    private readonly PlayerIdentityMatcher _matcher;

    public ImportPayloadBuilder(PlayerIdentityMatcher? matcher = null)
    {
        _matcher = matcher ?? new PlayerIdentityMatcher();
    }

    public ImportPayload Build(
        int matchId,
        ProcessingResult result,
        OcrMatchContext context,
        IReadOnlyDictionary<string, int>? overrides = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(context);

        var payload = new ImportPayload
        {
            MatchId = matchId,
            Stats = result.Stats,
        };

        AddTeam(payload, result, "Home", context.HomeTeam.TeamId);
        AddTeam(payload, result, "Away", context.AwayTeam.TeamId);

        foreach (var ocr in result.Players)
        {
            var side = ocr.Side ?? string.Empty;
            var isAway = string.Equals(side, "Away", StringComparison.OrdinalIgnoreCase);
            var canonicalTeamId = isAway ? context.AwayTeam.TeamId : context.HomeTeam.TeamId;
            var roster = isAway ? context.AwayTeam.Players : context.HomeTeam.Players;

            var playerId = ResolvePlayerId(ocr, roster, overrides);
            if (playerId is null)
            {
                continue; // non risolto: escluso dal payload, resta in IdentityReview
            }

            payload.Players.Add(new ImportPlayer
            {
                EntityId = ocr.EntityId,
                Side = side,
                PlayerId = playerId.Value,
                TeamId = canonicalTeamId,
                Number = ocr.Number,
                Starter = ocr.Starter,
                DidNotPlay = ocr.DidNotPlay,
            });
        }

        return payload;
    }

    private int? ResolvePlayerId(PlayerStats ocr, IReadOnlyList<OcrRosterPlayer> roster, IReadOnlyDictionary<string, int>? overrides)
    {
        if (overrides is not null && !string.IsNullOrEmpty(ocr.EntityId) && overrides.TryGetValue(ocr.EntityId, out var overridden))
        {
            return overridden;
        }

        var match = _matcher.Match(ocr.Number, ocr.FullName, roster);
        return match.Kind is PlayerMatchKind.CertainMatch or PlayerMatchKind.ProbableMatch
            ? match.Player!.PlayerId
            : null;
    }

    private static void AddTeam(ImportPayload payload, ProcessingResult result, string side, int teamId)
    {
        var team = result.Teams.FirstOrDefault(t => string.Equals(t.Side, side, StringComparison.OrdinalIgnoreCase));
        payload.Teams.Add(new ImportTeam
        {
            Side = side,
            TeamId = teamId,
            Name = team?.Name,
            Abbreviation = team?.Abbreviation,
        });
    }
}
