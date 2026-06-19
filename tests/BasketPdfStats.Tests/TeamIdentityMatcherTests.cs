using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Identity;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Tests;

public sealed class TeamIdentityMatcherTests
{
    private readonly TeamIdentityMatcher _matcher = new();

    private static OcrMatchContext Context(string home, string away) => new()
    {
        MatchId = 1,
        HomeTeam = new OcrContextTeam { TeamId = 1, Name = home },
        AwayTeam = new OcrContextTeam { TeamId = 2, Name = away },
    };

    [Fact]
    public void Detects_correct_side()
    {
        var result = _matcher.Match("Virtus", "Pallacanestro", Context("Virtus", "Pallacanestro"));

        Assert.Equal(TeamMatchOutcome.CorrectOrder, result.Outcome);
    }

    [Fact]
    public void Detects_side_inversion()
    {
        var result = _matcher.Match("Pallacanestro", "Virtus", Context("Virtus", "Pallacanestro"));

        Assert.Equal(TeamMatchOutcome.Inverted, result.Outcome);
    }

    [Fact]
    public void Detects_mismatch_when_no_team_recognized()
    {
        var result = _matcher.Match("Lakers", "Celtics", Context("Virtus", "Pallacanestro"));

        Assert.Equal(TeamMatchOutcome.Mismatch, result.Outcome);
    }

    [Fact]
    public void Tolerates_minor_ocr_noise_in_correct_order()
    {
        var result = _matcher.Match("Virtuss", "Pallacanestr0", Context("Virtus", "Pallacanestro"));

        Assert.Equal(TeamMatchOutcome.CorrectOrder, result.Outcome);
    }
}

public sealed class SideInversionTests
{
    [Fact]
    public void Apply_swaps_sides_entity_ids_score_and_periods()
    {
        var result = new ProcessingResult
        {
            Teams =
            [
                new TeamStats { Side = "Home", TeamId = "team:Home", Name = "Pall" },
                new TeamStats { Side = "Away", TeamId = "team:Away", Name = "Virtus" },
            ],
            Players =
            [
                new PlayerStats { EntityId = "player:Home:jersey:5", TeamId = "team:Home", Side = "Home" },
                new PlayerStats { EntityId = "player:Away:jersey:7", TeamId = "team:Away", Side = "Away" },
            ],
            Stats =
            [
                new StatValue { Scope = StatScope.Team, EntityId = "team:Home", Side = "Home", StatKey = "points", Value = 52 },
            ],
            Game = new GameStats
            {
                HomeTeamId = "team:Home",
                AwayTeamId = "team:Away",
                FinalScore = "52-44",
                Periods = [new PeriodScore { Period = "Q1", Home = 12, Away = 10 }],
            },
        };

        SideInversion.Apply(result);

        var home = result.Teams.Single(t => t.Side == "Home");
        Assert.Equal("Virtus", home.Name);
        Assert.Equal("team:Home", home.TeamId);

        var player = result.Players.Single(p => p.Side == "Home");
        Assert.Equal("player:Home:jersey:7", player.EntityId);
        Assert.Equal("team:Home", player.TeamId);

        var stat = result.Stats.Single();
        Assert.Equal("Away", stat.Side);
        Assert.Equal("team:Away", stat.EntityId);

        Assert.Equal("44-52", result.Game.FinalScore);
        // Slot labels invariati: lo slot Home ora contiene la squadra corretta.
        Assert.Equal("team:Home", result.Game.HomeTeamId);
        var q1 = result.Game.Periods.Single();
        Assert.Equal(10, q1.Home);
        Assert.Equal(12, q1.Away);
    }
}
