namespace BasketPdfStats.Core.Models;

public sealed class TeamStats
{
    public string TeamId { get; set; } = string.Empty;
    public string? Side { get; set; }
    public string? Name { get; set; }
    public string? Abbreviation { get; set; }
    public string? Coach { get; set; }
    public string? AssistantCoach { get; set; }
}
