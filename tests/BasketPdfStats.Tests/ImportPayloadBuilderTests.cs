using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Identity;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Tests;

public sealed class ImportPayloadBuilderTests
{
    private readonly ImportPayloadBuilder _builder = new();

    private static OcrMatchContext Context() => new()
    {
        MatchId = 42,
        HomeTeam = new OcrContextTeam
        {
            TeamId = 5,
            Name = "Virtus",
            Players = [new OcrRosterPlayer { PlayerId = 101, TeamId = 5, FirstName = "Mario", LastName = "Rossi", JerseyNumber = 5 }],
        },
        AwayTeam = new OcrContextTeam
        {
            TeamId = 7,
            Name = "Pall",
            Players = [new OcrRosterPlayer { PlayerId = 201, TeamId = 7, FirstName = "Gino", LastName = "Verdi", JerseyNumber = 4 }],
        },
    };

    private static PlayerStats OcrPlayer(string side, string number, string name) =>
        new() { EntityId = $"player:{side}:jersey:{number}", TeamId = $"team:{side}", Side = side, Number = number, FullName = name };

    [Fact]
    public void Build_uses_canonical_ids_for_resolved_players()
    {
        var result = new ProcessingResult
        {
            Teams = [new TeamStats { Side = "Home", Name = "Virtus" }, new TeamStats { Side = "Away", Name = "Pall" }],
            Players = [OcrPlayer("Home", "5", "Mario Rossi"), OcrPlayer("Away", "4", "Gino Verdi")],
        };

        var payload = _builder.Build(42, result, Context());

        Assert.Equal(42, payload.MatchId);
        Assert.Equal(2, payload.Players.Count);

        var home = payload.Players.Single(p => p.Side == "Home");
        Assert.Equal(101, home.PlayerId);
        Assert.Equal(5, home.TeamId);
        Assert.Equal("player:Home:jersey:5", home.EntityId);

        Assert.Contains(payload.Teams, t => t.Side == "Home" && t.TeamId == 5);
        Assert.Contains(payload.Teams, t => t.Side == "Away" && t.TeamId == 7);
    }

    [Fact]
    public void Build_excludes_unresolved_players()
    {
        var result = new ProcessingResult
        {
            Teams = [new TeamStats { Side = "Home", Name = "Virtus" }],
            // jersey 99 non è nel roster → non risolto → escluso
            Players = [OcrPlayer("Home", "99", "Tizio Caio")],
        };

        var payload = _builder.Build(42, result, Context());

        Assert.Empty(payload.Players);
    }

    [Fact]
    public void Build_applies_manual_override_for_conflicting_player()
    {
        var result = new ProcessingResult
        {
            Teams = [new TeamStats { Side = "Home", Name = "Virtus" }],
            // nome incompatibile col jersey 5 → conflitto → di norma escluso
            Players = [OcrPlayer("Home", "5", "Nome Sbagliato")],
        };
        var overrides = new Dictionary<string, int> { ["player:Home:jersey:5"] = 101 };

        var payload = _builder.Build(42, result, Context(), overrides);

        var player = Assert.Single(payload.Players);
        Assert.Equal(101, player.PlayerId);
    }

    [Fact]
    public void Build_keeps_stats_for_php_to_map_by_entity_id()
    {
        var result = new ProcessingResult
        {
            Teams = [new TeamStats { Side = "Home", Name = "Virtus" }],
            Players = [OcrPlayer("Home", "5", "Mario Rossi")],
            Stats = [new StatValue { Scope = StatScope.Player, EntityId = "player:Home:jersey:5", StatKey = "points", Value = 12 }],
        };

        var payload = _builder.Build(42, result, Context());

        Assert.Same(result.Stats, payload.Stats);
    }
}
