using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Core.Database;

public interface IOcrImportService
{
    Task<OcrImportResult> ImportAsync(int matchId, ProcessingResult result, CancellationToken cancellationToken = default);
}

public sealed class OcrImportResult
{
    public bool Success { get; set; }
    public int MatchId { get; set; }
    public int ImportedPlayers { get; set; }
    public List<string> Warnings { get; set; } = [];
    public string? ErrorMessage { get; set; }
}
