using BasketPdfStats.App.ViewModels;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;

namespace BasketPdfStats.Tests;

public sealed class ProcessingResultViewModelTests
{
    [Fact]
    public void Result_viewer_player_grid_declares_all_fixed_fiba_columns()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "BasketPdfStats.App", "ResultViewerForm.cs"));
        var headers = new[]
        {
            "N.", "Nome", "Min.", "FG", "FG%", "2P", "2P%", "3P", "3P%", "FT", "FT%",
            "RO", "RD", "RT", "AS", "PP", "PR", "SD", "FF", "FS", "+/-", "Val.", "PTl", "Provider", "Warnings"
        };

        Assert.All(headers, header => Assert.Contains($"\"{header}\"", source, StringComparison.Ordinal));
        Assert.Contains("PlayerColumns", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Player_rows_link_stats_by_entity_id_and_format_fiba_columns()
    {
        var result = Result();
        result.Players.Add(Player("player:Home:jersey:7", "7", "Mario Rossi"));
        result.Players.Add(Player("player:Home:name:other-player", "7", "Other Player"));
        result.Stats.AddRange(
        [
            Stat("player:Home:jersey:7", "fieldGoals.made", 3),
            Stat("player:Home:jersey:7", "fieldGoals.attempted", 8),
            Stat("player:Home:jersey:7", "fieldGoals.percentage", 37.5),
            Stat("player:Home:jersey:7", "twoPoints.made", 2),
            Stat("player:Home:jersey:7", "twoPoints.attempted", 4),
            Stat("player:Home:jersey:7", "twoPoints.percentage", 50),
            Stat("player:Home:jersey:7", "threePoints.made", 1),
            Stat("player:Home:jersey:7", "threePoints.attempted", 4),
            Stat("player:Home:jersey:7", "threePoints.percentage", 25),
            Stat("player:Home:jersey:7", "freeThrows.made", 2),
            Stat("player:Home:jersey:7", "freeThrows.attempted", 2),
            Stat("player:Home:jersey:7", "freeThrows.percentage", 100),
            Stat("player:Home:jersey:7", "points", 9)
        ]);

        var rows = new ProcessingResultViewModel(result).HomePlayers;
        var mario = Assert.Single(rows, row => row.Name == "Mario Rossi");
        var other = Assert.Single(rows, row => row.Name == "Other Player");

        Assert.Equal("3/8", mario.FieldGoals);
        Assert.Equal("37.5", mario.FieldGoalsPercentage);
        Assert.Equal("2/4", mario.TwoPoints);
        Assert.Equal("50", mario.TwoPointsPercentage);
        Assert.Equal("1/4", mario.ThreePoints);
        Assert.Equal("25", mario.ThreePointsPercentage);
        Assert.Equal("2/2", mario.FreeThrows);
        Assert.Equal("100", mario.FreeThrowsPercentage);
        Assert.Equal("9", mario.Points);
        Assert.Equal("-", other.FieldGoals);
        Assert.Equal("-", other.Points);
    }

    [Fact]
    public void Player_row_uses_fixed_missing_value_placeholder()
    {
        var result = Result();
        result.Players.Add(Player("player:Away:jersey:12", "12", "Missing Values"));

        var row = Assert.Single(new ProcessingResultViewModel(result).AwayPlayers);

        Assert.Equal("-", row.Minutes);
        Assert.Equal("-", row.FieldGoals);
        Assert.Equal("-", row.FieldGoalsPercentage);
        Assert.Equal("-", row.ReboundsOffensive);
        Assert.Equal("-", row.Evaluation);
        Assert.Equal("-", row.Provider);
        Assert.Equal("-", row.Warnings);
    }

    [Fact]
    public void Player_provider_summary_reports_single_provider_or_mixed()
    {
        var result = Result();
        result.Players.Add(Player("player:Home:jersey:4", "4", "Mixed Provider"));
        result.Players.Add(Player("player:Away:jersey:5", "5", "Single Provider"));
        result.Stats.Add(Stat("player:Home:jersey:4", "points", 8, OcrStrategyNames.TesseractFullPage));
        result.Stats.Add(Stat("player:Home:jersey:4", "assists", 2, OcrStrategyNames.TesseractLayoutCrops));
        result.Stats.Add(Stat("player:Away:jersey:5", "points", 11, OcrStrategyNames.TesseractLayoutCrops));

        var viewModel = new ProcessingResultViewModel(result);

        Assert.Equal("Mixed", Assert.Single(viewModel.HomePlayers).Provider);
        Assert.Equal(OcrStrategyNames.TesseractLayoutCrops, Assert.Single(viewModel.AwayPlayers).Provider);
    }

    [Fact]
    public void Player_warning_summary_is_compact_and_deduplicated()
    {
        var result = Result();
        result.Players.Add(Player("player:Home:jersey:9", "9", "Warned Player"));
        var stat = Stat("player:Home:jersey:9", "points", 7);
        stat.Warnings.AddRange(
        [
            Warning("ocr.a"),
            Warning("ocr.b"),
            Warning("ocr.c"),
            Warning("ocr.d"),
            Warning("ocr.a")
        ]);
        result.Stats.Add(stat);

        var row = Assert.Single(new ProcessingResultViewModel(result).HomePlayers);

        Assert.Equal("ocr.a, ocr.b, ocr.c (+1)", row.Warnings);
    }

    [Fact]
    public void Team_totals_keep_fixed_rows_when_values_are_missing()
    {
        var result = Result();
        result.Stats.Add(new StatValue
        {
            Scope = StatScope.Team,
            Side = "Home",
            EntityId = "team:Home",
            StatKey = "points",
            FieldId = "team:Home:points",
            Value = 52
        });

        var rows = new ProcessingResultViewModel(result).HomeTeamTotals;

        Assert.Equal(25, rows.Count);
        Assert.Contains(rows, row => row.Statistic == "Punti" && row.Value == "52");
        Assert.Contains(rows, row => row.Statistic == "Tiri dal campo segnati" && row.Value == "-");
    }

    private static ProcessingResult Result() => new()
    {
        ProcessedFile = new ProcessedFile { FileName = "sample.pdf" }
    };

    private static PlayerStats Player(string entityId, string number, string name)
    {
        var side = entityId.Contains(":Away:", StringComparison.OrdinalIgnoreCase) ? "Away" : "Home";
        return new PlayerStats
        {
            EntityId = entityId,
            TeamId = $"team:{side}",
            Side = side,
            Number = number,
            FullName = name
        };
    }

    private static StatValue Stat(string entityId, string statKey, object value, string? provider = null)
    {
        var stat = new StatValue
        {
            Scope = StatScope.Player,
            Side = entityId.Contains(":Away:", StringComparison.OrdinalIgnoreCase) ? "Away" : "Home",
            EntityId = entityId,
            FieldId = $"{entityId}:{statKey}",
            StatKey = statKey,
            Value = value
        };
        if (!string.IsNullOrWhiteSpace(provider))
        {
            stat.Reconciliation = new StatReconciliationMetadata { SelectedProvider = provider };
        }

        return stat;
    }

    private static ValidationWarning Warning(string ruleId) => new() { RuleId = ruleId };

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "BasketPdfStats.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate BasketPdfStats.sln.");
    }
}
