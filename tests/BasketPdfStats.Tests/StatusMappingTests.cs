using System.Text.Json;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Tests;

public sealed class StatusMappingTests
{
    [Fact]
    public void File_status_serializes_as_string()
    {
        var json = JsonSerializer.Serialize(FileProcessingStatus.CompletedWithWarnings, JsonDefaults.Options);

        Assert.Equal("\"CompletedWithWarnings\"", json);
    }

    [Fact]
    public void Ocr_status_serializes_as_string()
    {
        var json = JsonSerializer.Serialize(OcrRunStatus.NotConfigured, JsonDefaults.Options);

        Assert.Equal("\"NotConfigured\"", json);
    }

    [Fact]
    public void Stat_validation_status_serializes_as_string()
    {
        var json = JsonSerializer.Serialize(StatValidationStatus.Validato, JsonDefaults.Options);

        Assert.Equal("\"Validato\"", json);
    }
}
