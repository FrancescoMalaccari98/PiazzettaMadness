using BasketPdfStats.Core.Stats;

namespace BasketPdfStats.Tests;

public sealed class StatKeyRegistryTests
{
    [Theory]
    [InlineData("points")]
    [InlineData("fieldGoals.made")]
    [InlineData("fieldGoals.attempted")]
    [InlineData("fieldGoals.percentage")]
    [InlineData("twoPoints.made")]
    [InlineData("twoPoints.attempted")]
    [InlineData("twoPoints.percentage")]
    [InlineData("threePoints.made")]
    [InlineData("threePoints.attempted")]
    [InlineData("threePoints.percentage")]
    [InlineData("freeThrows.made")]
    [InlineData("freeThrows.attempted")]
    [InlineData("freeThrows.percentage")]
    [InlineData("rebounds.offensive")]
    [InlineData("rebounds.defensive")]
    [InlineData("rebounds.total")]
    [InlineData("assists")]
    [InlineData("turnovers")]
    [InlineData("steals")]
    [InlineData("blocks")]
    [InlineData("fouls.committed")]
    [InlineData("fouls.drawn")]
    [InlineData("plusMinus")]
    [InlineData("efficiency")]
    [InlineData("evaluation")]
    [InlineData("minutes")]
    [InlineData("period.points")]
    [InlineData("comparative.pointsInThePaint")]
    [InlineData("comparative.pointsFromTurnovers")]
    [InlineData("comparative.secondChancePoints")]
    [InlineData("comparative.fastBreakPoints")]
    [InlineData("comparative.fastBreakPointsFromTurnovers")]
    [InlineData("comparative.benchPoints")]
    [InlineData("comparative.largestLead")]
    [InlineData("comparative.biggestRun")]
    [InlineData("comparative.pointsPerPossession")]
    [InlineData("comparative.leadChanges")]
    [InlineData("comparative.timesTied")]
    [InlineData("comparative.timeInLead")]
    public void Contains_required_stat_keys(string statKey)
    {
        Assert.True(StatKeyRegistry.Contains(statKey));
    }
}
