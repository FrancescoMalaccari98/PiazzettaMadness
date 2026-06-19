using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Identity;

namespace BasketPdfStats.Tests;

public sealed class PlayerIdentityMatcherTests
{
    private static readonly IReadOnlyList<OcrRosterPlayer> Roster =
    [
        new OcrRosterPlayer { PlayerId = 1, TeamId = 1, FirstName = "Mario", LastName = "Rossi", JerseyNumber = 5 },
        new OcrRosterPlayer { PlayerId = 2, TeamId = 1, FirstName = "Luca", LastName = "Bianchi", JerseyNumber = 7 },
    ];

    private readonly PlayerIdentityMatcher _matcher = new();

    [Fact]
    public void Match_certain_when_jersey_and_name_compatible()
    {
        var result = _matcher.Match("5", "Mario Rossi", Roster);

        Assert.Equal(PlayerMatchKind.CertainMatch, result.Kind);
        Assert.Equal(1, result.Player!.PlayerId);
        Assert.Equal(5, result.Jersey);
    }

    [Fact]
    public void Match_certain_when_only_surname_present()
    {
        var result = _matcher.Match("5", "ROSSI", Roster);

        Assert.Equal(PlayerMatchKind.CertainMatch, result.Kind);
        Assert.Equal(1, result.Player!.PlayerId);
    }

    [Fact]
    public void Match_certain_ignores_starter_marker_on_jersey()
    {
        var result = _matcher.Match("5*", "Mario Rossi", Roster);

        Assert.Equal(PlayerMatchKind.CertainMatch, result.Kind);
        Assert.Equal(5, result.Jersey);
    }

    [Fact]
    public void Match_probable_when_name_close_but_not_exact()
    {
        var result = _matcher.Match("5", "Rossini", Roster);

        Assert.Equal(PlayerMatchKind.ProbableMatch, result.Kind);
        Assert.Equal(1, result.Player!.PlayerId);
    }

    [Fact]
    public void Match_conflict_when_jersey_ok_but_name_incompatible()
    {
        var result = _matcher.Match("5", "Bianchi", Roster);

        Assert.Equal(PlayerMatchKind.Conflict, result.Kind);
        Assert.Equal(1, result.Player!.PlayerId); // candidato per quel jersey
    }

    [Fact]
    public void Match_number_not_found_when_jersey_absent_from_roster()
    {
        var result = _matcher.Match("99", "Mario Rossi", Roster);

        Assert.Equal(PlayerMatchKind.NumberNotFound, result.Kind);
        Assert.Null(result.Player);
        Assert.Equal(99, result.Jersey);
    }

    [Fact]
    public void Match_unmatched_when_jersey_not_readable()
    {
        var result = _matcher.Match("--", "Mario Rossi", Roster);

        Assert.Equal(PlayerMatchKind.Unmatched, result.Kind);
        Assert.Null(result.Jersey);
    }

    [Fact]
    public void Match_probable_and_name_missing_when_jersey_found_but_name_empty()
    {
        var result = _matcher.Match("5", "   ", Roster);

        Assert.Equal(PlayerMatchKind.ProbableMatch, result.Kind);
        Assert.True(result.NameMissing);
        Assert.Equal(1, result.Player!.PlayerId);
    }

    [Fact]
    public void Match_certain_via_ocr_substitution()
    {
        // "Mari0 R0ssi" diventa "mario rossi" applicando 0→o.
        var result = _matcher.Match("5", "Mari0 R0ssi", Roster);

        Assert.Equal(PlayerMatchKind.CertainMatch, result.Kind);
    }

    [Fact]
    public void NormalizeOcrName_handles_accents()
    {
        Assert.Equal("niccolo rosi", NameNormalizer.Normalize("Niccolò Rosì"));
    }

    [Fact]
    public void NormalizeOcrName_handles_apostrophes_and_hyphens()
    {
        Assert.Equal("d angelo de luca", NameNormalizer.Normalize("D'Angelo De-Luca"));
    }

    [Fact]
    public void ApplyOcrSubstitutions_maps_common_digit_confusions()
    {
        Assert.Equal("rossi", NameNormalizer.ApplyOcrSubstitutions(NameNormalizer.Normalize("R0ss1")));
    }

    [Fact]
    public void Constructor_rejects_certain_threshold_above_probable()
    {
        Assert.Throws<ArgumentException>(() => new PlayerIdentityMatcher(certainThreshold: 0.5, probableThreshold: 0.3));
    }
}
