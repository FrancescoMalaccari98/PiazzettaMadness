namespace BasketPdfStats.Core.Models;

public sealed class PlayerStats
{
    private string _entityId = string.Empty;

    public string EntityId
    {
        get => _entityId;
        set => _entityId = value ?? string.Empty;
    }

    public string PlayerId
    {
        get => _entityId;
        set => _entityId = value ?? string.Empty;
    }

    public string? SourceEntityId { get; set; }
    public string TeamId { get; set; } = string.Empty;
    public string? Side { get; set; }
    public string? Number { get; set; }
    public string? FullName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Starter { get; set; }
    public bool Captain { get; set; }
    public bool DidNotPlay { get; set; }
}
