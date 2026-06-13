namespace BasketPdfStats.Core.Pipeline;

public sealed class AlreadyProcessedPdfDetection
{
    public bool IsAlreadyProcessed { get; init; }
    public string? Evidence { get; init; }
}
