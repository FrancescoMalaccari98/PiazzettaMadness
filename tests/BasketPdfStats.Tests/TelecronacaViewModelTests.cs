using BasketPdfStats.App.ViewModels;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Identity;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Tests;

/// <summary>
/// Vista "Risultati Telecronaca": popolamento da dati finali, separazione squadre, ordinamento,
/// N.E. in fondo, top player, dettaglio, review/mismatch.
/// </summary>
public sealed class TelecronacaViewModelTests
{
    [Fact]
    public void Populates_both_teams_and_separates_players()
    {
        var vm = new TelecronacaViewModel(SampleResult(), Option());

        Assert.True(vm.HasPlayers);
        Assert.Equal("Aurora Lynx", vm.HomeTeam);
        Assert.Equal("Nebula Bears", vm.AwayTeam);
        Assert.Equal(3, vm.HomePlayers.Count);
        Assert.Single(vm.AwayPlayers);
        Assert.All(vm.HomePlayers, p => Assert.DoesNotContain("Bears", p.DisplayName));
    }

    [Fact]
    public void Default_order_is_jersey_ascending_with_ne_last()
    {
        var vm = new TelecronacaViewModel(SampleResult(), Option());

        // #4 Rossi, #7 Bianchi giocano; #12 Verdi è N.E. → in fondo.
        Assert.Equal(["4", "7", "12"], vm.HomePlayers.Select(p => p.Number).ToArray());
        Assert.True(vm.HomePlayers[^1].DidNotPlay);
        Assert.Equal("N.E.", vm.HomePlayers[^1].NeLabel);
    }

    [Fact]
    public void Missing_values_render_as_dash()
    {
        var vm = new TelecronacaViewModel(SampleResult(), Option());
        var ne = vm.HomePlayers.Single(p => p.Number == "12");

        Assert.Equal("-", ne.Points);
        Assert.Equal("-", ne.Rebounds);
        Assert.Equal("-", ne.Two);
    }

    [Fact]
    public void Compact_columns_format_shooting_with_percentage()
    {
        var vm = new TelecronacaViewModel(SampleResult(), Option());
        var rossi = vm.HomePlayers.Single(p => p.Number == "4");

        Assert.Equal("18", rossi.Points);
        Assert.Equal("3/5 60%", rossi.Two);
        Assert.Equal("2/6 33%", rossi.Three);
        Assert.Equal("6", rossi.Rebounds);
    }

    [Fact]
    public void Highlights_compute_top_scorer_and_best_evaluation()
    {
        var vm = new TelecronacaViewModel(SampleResult(), Option());

        Assert.Contains(vm.Highlights, h => h.StartsWith("Top scorer:", StringComparison.Ordinal) && h.Contains("Mario Rossi") && h.Contains("18 PTS"));
        Assert.Contains(vm.Highlights, h => h.StartsWith("Miglior valutazione:", StringComparison.Ordinal) && h.Contains("Mario Rossi") && h.Contains("22 VAL"));
    }

    [Fact]
    public void Selected_player_detail_exposes_full_stats()
    {
        var vm = new TelecronacaViewModel(SampleResult(), Option());
        var rossi = vm.HomePlayers.Single(p => p.Number == "4");

        vm.SelectedHomePlayer = rossi;

        Assert.True(vm.HasSelectedPlayer);
        Assert.Same(rossi, vm.SelectedPlayer);
        Assert.Equal("3/5 (60%)", rossi.TwoDetail);
        Assert.Equal("22", rossi.Evaluation);
        Assert.Equal("2", rossi.ReboundsOffensive);
        Assert.Equal("4", rossi.ReboundsDefensive);
    }

    [Fact]
    public void Selecting_away_clears_home_selection()
    {
        var vm = new TelecronacaViewModel(SampleResult(), Option());
        vm.SelectedHomePlayer = vm.HomePlayers[0];
        vm.SelectedAwayPlayer = vm.AwayPlayers[0];

        Assert.Null(vm.SelectedHomePlayer);
        Assert.Same(vm.AwayPlayers[0], vm.SelectedPlayer);
    }

    [Fact]
    public void Review_required_is_exposed_in_status()
    {
        var result = SampleResult();
        result.ProcessedFile.Status = FileProcessingStatus.CompletedWithReviewRequired;
        result.IdentityReview = [new IdentityReviewItem { Side = "Home", Reason = IdentityReviewReason.Conflict }];

        var vm = new TelecronacaViewModel(result, Option());

        Assert.True(vm.ReviewRequired);
        Assert.Equal("Review", vm.StatusKind);
        Assert.Equal("Revisione richiesta", vm.StatusText);
    }

    [Fact]
    public void Mismatch_hides_player_tables_and_marks_not_importable()
    {
        var vm = new TelecronacaViewModel(SampleResult(), Option(),
            mismatchMessage: "Il PDF selezionato non corrisponde al match scelto.", importable: false);

        Assert.False(vm.HasPlayers);
        Assert.Empty(vm.HomePlayers);
        Assert.Empty(vm.AwayPlayers);
        Assert.Equal("Error", vm.StatusKind);
        Assert.False(vm.Importable);
        Assert.Equal("PDF non corrispondente al match selezionato", vm.StatusText);
    }

    private static OcrMatchOption Option() => new()
    {
        MatchId = 63,
        HomeTeam = new OcrMatchTeam { Name = "Aurora Lynx" },
        AwayTeam = new OcrMatchTeam { Name = "Nebula Bears" },
        ScheduledStartAt = "2026-07-08 20:30:00",
        Phase = "GroupStage",
        Round = "Giornata 1",
    };

    private static ProcessingResult SampleResult()
    {
        var result = new ProcessingResult
        {
            ProcessedFile = new ProcessedFile { FileName = "x.pdf", Status = FileProcessingStatus.CompletedWithWarnings },
            Teams =
            [
                new TeamStats { Side = "Home", Name = "Aurora Lynx" },
                new TeamStats { Side = "Away", Name = "Nebula Bears" },
            ],
            Players =
            [
                Player("player:Home:jersey:7", "Home", "7", "Luca Bianchi"),
                Player("player:Home:jersey:4", "Home", "4", "Mario Rossi"),
                Player("player:Home:jersey:12", "Home", "12", "Marco Verdi", didNotPlay: true),
                Player("player:Away:jersey:6", "Away", "6", "Samir Velori"),
            ],
        };

        // Stat punteggio finale (Result scope).
        result.Stats.Add(ResultStat("Home", 45));
        result.Stats.Add(ResultStat("Away", 38));

        // Mario Rossi #4: 18 pt, 2PT 3/5, 3PT 2/6, reb 2/4/6, val 22.
        AddPlayer(result, "player:Home:jersey:4", "Home",
            ("points", 18), ("twoPoints.made", 3), ("twoPoints.attempted", 5),
            ("threePoints.made", 2), ("threePoints.attempted", 6),
            ("rebounds.offensive", 2), ("rebounds.defensive", 4), ("rebounds.total", 6),
            ("assists", 5), ("steals", 1), ("fouls.committed", 2), ("evaluation", 22));

        // Luca Bianchi #7: 8 pt, val 10.
        AddPlayer(result, "player:Home:jersey:7", "Home",
            ("points", 8), ("rebounds.total", 3), ("assists", 2), ("evaluation", 10));

        // Samir Velori #6 (away): 11 pt.
        AddPlayer(result, "player:Away:jersey:6", "Away",
            ("points", 11), ("rebounds.total", 5), ("evaluation", 12));

        // #12 Verdi: nessuna statistica (N.E.).
        return result;
    }

    private static PlayerStats Player(string entityId, string side, string number, string name, bool didNotPlay = false) =>
        new() { EntityId = entityId, Side = side, Number = number, FullName = name, DidNotPlay = didNotPlay };

    private static StatValue ResultStat(string side, int points) => new()
    {
        Scope = StatScope.Result,
        Side = side,
        EntityId = $"team:{side}",
        StatKey = "points",
        FieldId = $"result:{side}:points",
        Value = points,
    };

    private static void AddPlayer(ProcessingResult result, string entityId, string side, params (string Key, int Value)[] stats)
    {
        foreach (var s in stats)
        {
            result.Stats.Add(new StatValue
            {
                Scope = StatScope.Player,
                Side = side,
                EntityId = entityId,
                StatKey = s.Key,
                FieldId = $"{entityId}:{s.Key}",
                Value = s.Value,
            });
        }
    }
}
