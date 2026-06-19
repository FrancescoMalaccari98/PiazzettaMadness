using System.Text.Json;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Identity;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Tests;

public sealed class IdentityReviewBuilderTests
{
    private readonly IdentityReviewBuilder _builder = new();

    private static OcrMatchContext Context() => new()
    {
        MatchId = 1,
        HomeTeam = new OcrContextTeam
        {
            TeamId = 1,
            Name = "Home",
            Players =
            [
                new OcrRosterPlayer { PlayerId = 1, TeamId = 1, FirstName = "Mario", LastName = "Rossi", JerseyNumber = 5 },
                new OcrRosterPlayer { PlayerId = 2, TeamId = 1, FirstName = "Luca", LastName = "Bianchi", JerseyNumber = 7 },
            ],
        },
        AwayTeam = new OcrContextTeam { TeamId = 2, Name = "Away", Players = [] },
    };

    private static PlayerStats OcrPlayer(string number, string name) =>
        new() { EntityId = $"player:Home:jersey:{number}", TeamId = "team:Home", Side = "Home", Number = number, FullName = name };

    [Fact]
    public void Certain_matches_produce_no_review_items()
    {
        var result = new ProcessingResult { Players = [OcrPlayer("5", "Mario Rossi"), OcrPlayer("7", "Luca Bianchi")] };

        var items = _builder.Build(result, Context());

        Assert.Empty(items);
    }

    [Fact]
    public void Conflict_produces_conflict_review_item()
    {
        var result = new ProcessingResult { Players = [OcrPlayer("5", "Mario Rossi"), OcrPlayer("7", "Giovanni Sconosciuto")] };

        var items = _builder.Build(result, Context());

        var conflict = Assert.Single(items, i => i.Reason == IdentityReviewReason.Conflict);
        Assert.Equal("7", conflict.OcrJersey);
        Assert.Equal(2, conflict.CandidatePlayerId);
        // Il giocatore in conflitto non viene anche segnalato come NotInPdf.
        Assert.DoesNotContain(items, i => i.Reason == IdentityReviewReason.NotInPdf && i.CandidatePlayerId == 2);
    }

    [Fact]
    public void Db_player_missing_from_pdf_produces_not_in_pdf_item()
    {
        var result = new ProcessingResult { Players = [OcrPlayer("5", "Mario Rossi")] };

        var items = _builder.Build(result, Context());

        var notInPdf = Assert.Single(items, i => i.Reason == IdentityReviewReason.NotInPdf);
        Assert.Equal(2, notInPdf.CandidatePlayerId);
        Assert.Equal("Luca Bianchi", notInPdf.CandidateName);
    }

    [Fact]
    public void Number_not_found_produces_review_item_without_candidate()
    {
        var result = new ProcessingResult { Players = [OcrPlayer("5", "Mario Rossi"), OcrPlayer("99", "Tizio Caio")] };

        var items = _builder.Build(result, Context());

        var notFound = Assert.Single(items, i => i.Reason == IdentityReviewReason.NumberNotFound);
        Assert.Null(notFound.CandidatePlayerId);
    }

    [Fact]
    public void IdentityReviewItem_serializes_reason_as_string()
    {
        var item = new IdentityReviewItem { Side = "Home", Reason = IdentityReviewReason.Conflict, OcrJersey = "7" };

        var json = JsonSerializer.Serialize(item, JsonDefaults.Options);
        var restored = JsonSerializer.Deserialize<IdentityReviewItem>(json, JsonDefaults.Options);

        Assert.Contains("\"reason\": \"Conflict\"", json);
        Assert.NotNull(restored);
        Assert.Equal(IdentityReviewReason.Conflict, restored!.Reason);
    }
}
