using BasketPdfStats.Core.Enums;

namespace BasketPdfStats.Core.Stats;

public sealed record StatKeyDefinition(
    string Key,
    string DisplayName,
    string ValueType,
    string? Unit,
    bool Nullable,
    IReadOnlyCollection<StatScope> Scopes,
    int? MinValue = null,
    int? MaxValue = null);
