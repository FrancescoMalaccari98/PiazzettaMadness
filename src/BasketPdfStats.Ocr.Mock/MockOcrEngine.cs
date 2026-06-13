using System.Diagnostics;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;

namespace BasketPdfStats.Ocr.Mock;

public sealed class MockOcrEngine : IOcrEngine
{
    public string EngineName => "MockOcr";

    public Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();

        var result = new ProcessingResult
        {
            Game = new GameStats
            {
                GameId = $"game:{ShortHash(request.DocumentHash)}",
                Competition = "Piazzetta Madness",
                Venue = "Piazzetta Verde",
                Date = "2026-05-29",
                Time = "21:00",
                Duration = "00:30",
                HomeTeamId = "MIA",
                AwayTeamId = "PHI",
                FinalScore = "48-26",
                Periods =
                [
                    new PeriodScore { Period = "Q1", Home = 29, Away = 17 },
                    new PeriodScore { Period = "Q2", Home = 19, Away = 9 }
                ]
            },
            Teams =
            [
                new TeamStats { TeamId = "MIA", Side = "Home", Name = "Miami Spritz", Abbreviation = "MIA" },
                new TeamStats { TeamId = "PHI", Side = "Away", Name = "Philadelphia 70Sexers", Abbreviation = "PHI" }
            ],
            Players =
            [
                new PlayerStats { PlayerId = "player:MIA:7", TeamId = "MIA", Side = "Home", Number = "7", FullName = "Raoul PIERGENTILI" },
                new PlayerStats { PlayerId = "player:PHI:11", TeamId = "PHI", Side = "Away", Number = "11", FullName = "Alessandro BARBIERI" }
            ]
        };

        result.Stats.AddRange(CreateResultStats());
        result.Stats.AddRange(CreateTeamStats("MIA", "Home", new TeamLine(48, 19, 42, 45.2, 13, 23, 56.5, 6, 19, 31.6, 4, 13, 30.8, 8, 21, 29, 3, 8, 7, 1, 7, 10, 12, 51, "20:00")));
        result.Stats.AddRange(CreateTeamStats("PHI", "Away", new TeamLine(26, 11, 41, 26.8, 8, 17, 47.1, 3, 24, 12.5, 1, 2, 50.0, 4, 20, 24, 1, 11, 4, 0, 10, 7, -12, 24, "20:00")));
        result.Stats.Add(CreateStat(StatScope.Player, "player:MIA:7", "Home", "points", "player:MIA:7:points", 10, "10", "playerTable.home.row7.points", 0.92));
        result.Stats.Add(CreateStat(StatScope.Player, "player:MIA:7", "Home", "fieldGoals.made", "player:MIA:7:fieldGoals.made", 5, "5/10", "playerTable.home.row7.fieldGoals", 0.88));
        result.Stats.Add(CreateStat(StatScope.Player, "player:PHI:11", "Away", "points", "player:PHI:11:points", 6, "6", "playerTable.away.row11.points", 0.91));
        result.Stats.Add(CreateStat(StatScope.Comparative, "game:mock", null, "points", "comparative.home.pointsFromTurnovers", 11, "11", "comparative.left.pointsFromTurnovers.home", 0.9));

        stopwatch.Stop();
        result.OcrRuns.Add(new OcrRunResult
        {
            Engine = EngineName,
            Status = OcrRunStatus.Success,
            DurationMs = stopwatch.ElapsedMilliseconds
        });
        result.OcrRuns.Add(new OcrRunResult { Engine = OcrStrategyNames.TesseractFullPage, Status = OcrRunStatus.Skipped });
        result.OcrRuns.Add(new OcrRunResult { Engine = "AdobePdfServices", Status = OcrRunStatus.NotConfigured });
        result.Validation.Status = FileProcessingStatus.CompletedValidated;

        return Task.FromResult(result);
    }

    private static IEnumerable<StatValue> CreateResultStats()
    {
        yield return CreateStat(StatScope.Result, "game:mock", "Home", "points", "result.home.points", 48, "48", "header.score.home", 0.98);
        yield return CreateStat(StatScope.Result, "game:mock", "Away", "points", "result.away.points", 26, "26", "header.score.away", 0.98);
    }

    private static IEnumerable<StatValue> CreateTeamStats(string teamId, string side, TeamLine line)
    {
        yield return CreateStat(StatScope.Team, teamId, side, "points", $"team:{teamId}:points", line.Points, line.Points.ToString(), $"playerTable.{side.ToLowerInvariant()}.total.points", 0.98);
        yield return CreateStat(StatScope.Team, teamId, side, "fieldGoals.made", $"team:{teamId}:fieldGoals.made", line.FieldGoalsMade, $"{line.FieldGoalsMade}/{line.FieldGoalsAttempted}", $"playerTable.{side.ToLowerInvariant()}.total.fieldGoals", 0.95);
        yield return CreateStat(StatScope.Team, teamId, side, "fieldGoals.attempted", $"team:{teamId}:fieldGoals.attempted", line.FieldGoalsAttempted, $"{line.FieldGoalsMade}/{line.FieldGoalsAttempted}", $"playerTable.{side.ToLowerInvariant()}.total.fieldGoals", 0.95);
        yield return CreateStat(StatScope.Team, teamId, side, "fieldGoals.percentage", $"team:{teamId}:fieldGoals.percentage", line.FieldGoalsPct, $"{line.FieldGoalsPct:0.0}", $"playerTable.{side.ToLowerInvariant()}.total.fieldGoalsPct", 0.93);
        yield return CreateStat(StatScope.Team, teamId, side, "twoPoints.made", $"team:{teamId}:twoPoints.made", line.TwoMade, $"{line.TwoMade}/{line.TwoAttempted}", $"playerTable.{side.ToLowerInvariant()}.total.twoPoints", 0.94);
        yield return CreateStat(StatScope.Team, teamId, side, "twoPoints.attempted", $"team:{teamId}:twoPoints.attempted", line.TwoAttempted, $"{line.TwoMade}/{line.TwoAttempted}", $"playerTable.{side.ToLowerInvariant()}.total.twoPoints", 0.94);
        yield return CreateStat(StatScope.Team, teamId, side, "twoPoints.percentage", $"team:{teamId}:twoPoints.percentage", line.TwoPct, $"{line.TwoPct:0.0}", $"playerTable.{side.ToLowerInvariant()}.total.twoPointsPct", 0.92);
        yield return CreateStat(StatScope.Team, teamId, side, "threePoints.made", $"team:{teamId}:threePoints.made", line.ThreeMade, $"{line.ThreeMade}/{line.ThreeAttempted}", $"playerTable.{side.ToLowerInvariant()}.total.threePoints", 0.94);
        yield return CreateStat(StatScope.Team, teamId, side, "threePoints.attempted", $"team:{teamId}:threePoints.attempted", line.ThreeAttempted, $"{line.ThreeMade}/{line.ThreeAttempted}", $"playerTable.{side.ToLowerInvariant()}.total.threePoints", 0.94);
        yield return CreateStat(StatScope.Team, teamId, side, "threePoints.percentage", $"team:{teamId}:threePoints.percentage", line.ThreePct, $"{line.ThreePct:0.0}", $"playerTable.{side.ToLowerInvariant()}.total.threePointsPct", 0.92);
        yield return CreateStat(StatScope.Team, teamId, side, "freeThrows.made", $"team:{teamId}:freeThrows.made", line.FreeThrowsMade, $"{line.FreeThrowsMade}/{line.FreeThrowsAttempted}", $"playerTable.{side.ToLowerInvariant()}.total.freeThrows", 0.93);
        yield return CreateStat(StatScope.Team, teamId, side, "freeThrows.attempted", $"team:{teamId}:freeThrows.attempted", line.FreeThrowsAttempted, $"{line.FreeThrowsMade}/{line.FreeThrowsAttempted}", $"playerTable.{side.ToLowerInvariant()}.total.freeThrows", 0.93);
        yield return CreateStat(StatScope.Team, teamId, side, "freeThrows.percentage", $"team:{teamId}:freeThrows.percentage", line.FreeThrowsPct, $"{line.FreeThrowsPct:0.0}", $"playerTable.{side.ToLowerInvariant()}.total.freeThrowsPct", 0.91);
        yield return CreateStat(StatScope.Team, teamId, side, "rebounds.offensive", $"team:{teamId}:rebounds.offensive", line.OffensiveRebounds, line.OffensiveRebounds.ToString(), $"playerTable.{side.ToLowerInvariant()}.total.reboundsOffensive", 0.9);
        yield return CreateStat(StatScope.Team, teamId, side, "rebounds.defensive", $"team:{teamId}:rebounds.defensive", line.DefensiveRebounds, line.DefensiveRebounds.ToString(), $"playerTable.{side.ToLowerInvariant()}.total.reboundsDefensive", 0.9);
        yield return CreateStat(StatScope.Team, teamId, side, "rebounds.total", $"team:{teamId}:rebounds.total", line.TotalRebounds, line.TotalRebounds.ToString(), $"playerTable.{side.ToLowerInvariant()}.total.reboundsTotal", 0.9);
        yield return CreateStat(StatScope.Team, teamId, side, "assists", $"team:{teamId}:assists", line.Assists, line.Assists.ToString(), $"playerTable.{side.ToLowerInvariant()}.total.assists", 0.9);
        yield return CreateStat(StatScope.Team, teamId, side, "turnovers", $"team:{teamId}:turnovers", line.Turnovers, line.Turnovers.ToString(), $"playerTable.{side.ToLowerInvariant()}.total.turnovers", 0.9);
        yield return CreateStat(StatScope.Team, teamId, side, "steals", $"team:{teamId}:steals", line.Steals, line.Steals.ToString(), $"playerTable.{side.ToLowerInvariant()}.total.steals", 0.9);
        yield return CreateStat(StatScope.Team, teamId, side, "blocks", $"team:{teamId}:blocks", line.Blocks, line.Blocks.ToString(), $"playerTable.{side.ToLowerInvariant()}.total.blocks", 0.85);
        yield return CreateStat(StatScope.Team, teamId, side, "fouls.committed", $"team:{teamId}:fouls.committed", line.FoulsCommitted, line.FoulsCommitted.ToString(), $"playerTable.{side.ToLowerInvariant()}.total.foulsCommitted", 0.9);
        yield return CreateStat(StatScope.Team, teamId, side, "fouls.drawn", $"team:{teamId}:fouls.drawn", line.FoulsDrawn, line.FoulsDrawn.ToString(), $"playerTable.{side.ToLowerInvariant()}.total.foulsDrawn", 0.9);
        yield return CreateStat(StatScope.Team, teamId, side, "plusMinus", $"team:{teamId}:plusMinus", line.PlusMinus, line.PlusMinus.ToString(), $"playerTable.{side.ToLowerInvariant()}.total.plusMinus", 0.86);
        yield return CreateStat(StatScope.Team, teamId, side, "efficiency", $"team:{teamId}:efficiency", line.Efficiency, line.Efficiency.ToString(), $"playerTable.{side.ToLowerInvariant()}.total.efficiency", 0.9);
        yield return CreateStat(StatScope.Team, teamId, side, "minutes", $"team:{teamId}:minutes", line.Minutes, line.Minutes, $"playerTable.{side.ToLowerInvariant()}.total.minutes", 0.88);
    }

    private static StatValue CreateStat(StatScope scope, string entityId, string? side, string statKey, string fieldId, object? value, string raw, string zoneId, double score)
    {
        return new StatValue
        {
            Scope = scope,
            EntityId = entityId,
            Side = side,
            StatKey = statKey,
            FieldId = fieldId,
            Value = value,
            Status = StatValidationStatus.Validato,
            Score = score,
            Candidates =
            [
                new OcrCandidate
                {
                    Engine = "MockOcr",
                    FieldId = fieldId,
                    StatKey = statKey,
                    ZoneId = zoneId,
                    Raw = raw,
                    Normalized = value,
                    Confidence = score,
                    Status = OcrRunStatus.Success
                }
            ]
        };
    }

    private static string ShortHash(string documentHash)
    {
        var normalized = documentHash.Replace("sha256:", string.Empty, StringComparison.OrdinalIgnoreCase);
        return normalized.Length <= 12 ? normalized : normalized[..12];
    }

    private sealed record TeamLine(
        int Points,
        int FieldGoalsMade,
        int FieldGoalsAttempted,
        double FieldGoalsPct,
        int TwoMade,
        int TwoAttempted,
        double TwoPct,
        int ThreeMade,
        int ThreeAttempted,
        double ThreePct,
        int FreeThrowsMade,
        int FreeThrowsAttempted,
        double FreeThrowsPct,
        int OffensiveRebounds,
        int DefensiveRebounds,
        int TotalRebounds,
        int Assists,
        int Turnovers,
        int Steals,
        int Blocks,
        int FoulsCommitted,
        int FoulsDrawn,
        int PlusMinus,
        int Efficiency,
        string Minutes);
}
