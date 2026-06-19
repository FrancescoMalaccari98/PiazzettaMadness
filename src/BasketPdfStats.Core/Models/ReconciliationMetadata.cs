namespace BasketPdfStats.Core.Models;

public sealed class ReconciliationMetadata
{
    public bool Enabled { get; set; }
    public List<string> Providers { get; set; } = [];
    public string Strategy { get; set; } = string.Empty;

    /// <summary>
    /// True se è stata rilevata e corretta un'inversione Home/Away rispetto al DB (Fase 6).
    /// </summary>
    public bool SideInversionApplied { get; set; }
}

public sealed class StatReconciliationMetadata
{
    public string SelectedProvider { get; set; } = string.Empty;
    public List<string> AgreedProviders { get; set; } = [];
    public Dictionary<string, object?> ProviderValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
