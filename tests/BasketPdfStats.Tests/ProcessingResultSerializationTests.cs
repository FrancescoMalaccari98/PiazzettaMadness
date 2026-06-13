using System.Text.Json;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Tests;

public sealed class ProcessingResultSerializationTests
{
    [Fact]
    public void ProcessingResult_round_trips_as_json()
    {
        var result = new ProcessingResult
        {
            ProcessedFile = new ProcessedFile
            {
                FileName = "sample.pdf",
                DocumentHash = "sha256:abc",
                Status = FileProcessingStatus.CompletedValidated
            },
            Stats =
            [
                new StatValue
                {
                    Scope = StatScope.Team,
                    EntityId = "MIA",
                    Side = "Home",
                    StatKey = "points",
                    FieldId = "team:MIA:points",
                    Value = 48,
                    Status = StatValidationStatus.Validato,
                    Score = 0.98
                }
            ],
            Players =
            [
                new PlayerStats
                {
                    EntityId = "player:Home:jersey:7",
                    SourceEntityId = "player:MIA:row:4",
                    TeamId = "team:Home",
                    Side = "Home",
                    Number = "7",
                    FullName = "Player One"
                }
            ]
        };

        var json = JsonSerializer.Serialize(result, JsonDefaults.Options);
        var restored = JsonSerializer.Deserialize<ProcessingResult>(json, JsonDefaults.Options);

        Assert.NotNull(restored);
        Assert.Equal("sample.pdf", restored.ProcessedFile.FileName);
        Assert.Equal(FileProcessingStatus.CompletedValidated, restored.ProcessedFile.Status);
        Assert.Single(restored.Stats);
        Assert.Equal("team:MIA:points", restored.Stats[0].FieldId);
        var player = Assert.Single(restored.Players);
        Assert.Equal("player:Home:jersey:7", player.EntityId);
        Assert.Equal(player.EntityId, player.PlayerId);
        Assert.Equal("player:MIA:row:4", player.SourceEntityId);
        Assert.Contains("\"entityId\": \"player:Home:jersey:7\"", json);
        Assert.Contains("\"sourceEntityId\": \"player:MIA:row:4\"", json);
    }
}
