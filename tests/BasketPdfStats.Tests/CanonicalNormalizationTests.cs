using BasketPdfStats.Core.Normalization;

namespace BasketPdfStats.Tests;

public sealed class CanonicalNormalizationTests
{
    [Theory]
    [InlineData("Home", "team:Home")]
    [InlineData("Away", "team:Away")]
    public void Team_keys_depend_only_on_side(string side, string expected)
    {
        Assert.Equal(expected, CanonicalEntityKeyBuilder.Team(side));
    }

    [Theory]
    [InlineData("*7", "player:Home:jersey:7", "7", true)]
    [InlineData(" 7 ", "player:Home:jersey:7", "7", false)]
    public void Player_keys_use_clean_jersey_and_preserve_starter(string jersey, string expectedId, string expectedJersey, bool expectedStarter)
    {
        var identity = CanonicalEntityKeyBuilder.Player("Home", jersey, "Player Name", 9);

        Assert.Equal(expectedId, identity.EntityId);
        Assert.Equal(expectedJersey, identity.Jersey);
        Assert.Equal(expectedStarter, identity.Starter);
    }

    [Fact]
    public void Player_key_falls_back_to_normalized_name()
    {
        Assert.Equal("player:Away:name:raoul-piergentili", CanonicalEntityKeyBuilder.Player("Away", null, "  Raoul  PIERGENTILI! ", 4).EntityId);
    }

    [Fact]
    public void Player_key_falls_back_to_zero_padded_row()
    {
        Assert.Equal("player:Away:row:04", CanonicalEntityKeyBuilder.Player("Away", null, null, 4).EntityId);
    }

    [Fact]
    public void Special_rows_are_recognized()
    {
        Assert.True(CanonicalEntityKeyBuilder.IsSpecialPlayerRow("Squadra/Allenatore", null));
        Assert.True(CanonicalEntityKeyBuilder.IsSpecialPlayerRow(null, "Totali"));
    }

    [Theory]
    [InlineData("Tiri dal campo R/T", "fieldGoals.made", "fieldGoals.attempted")]
    [InlineData("2 Punti R/T", "twoPoints.made", "twoPoints.attempted")]
    [InlineData("3 Punti R/T", "threePoints.made", "threePoints.attempted")]
    [InlineData("Tiri Liberi R/T", "freeThrows.made", "freeThrows.attempted")]
    public void Ratio_headers_map_to_made_and_attempted(string header, string made, string attempted)
    {
        Assert.Equal(new[] { made, attempted }, CanonicalStatKeyMapper.MapAll(header));
    }

    [Theory]
    [InlineData("Tiri dal campo %", "fieldGoals.percentage")]
    [InlineData("RO", "rebounds.offensive")]
    [InlineData("RD", "rebounds.defensive")]
    [InlineData("RT", "rebounds.total")]
    [InlineData("AS", "assists")]
    [InlineData("PP", "turnovers")]
    [InlineData("PR", "steals")]
    [InlineData("SD", "blocks")]
    [InlineData("FF", "fouls.committed")]
    [InlineData("FS", "fouls.drawn")]
    [InlineData("+/-", "plusMinus")]
    [InlineData("Val.", "evaluation")]
    [InlineData("PTl", "points")]
    [InlineData("efficiency", "evaluation")]
    public void Scalar_headers_map_to_canonical_stat_key(string header, string expected)
    {
        Assert.Equal(expected, CanonicalStatKeyMapper.Map(header));
    }
}
