namespace BasketPdfStats.Core.Database;

/// <summary>
/// Contesto canonico della partita caricato dal DB (via PHP REST) prima dell'OCR.
/// Il DB è la fonte di verità per identità di partita, squadre e giocatori.
/// L'OCR estrae statistiche e le associa a queste entità; non le crea.
/// Contratto endpoint: GET /api-ocr/matches/{matchId}/context.
/// </summary>
public sealed class OcrMatchContext
{
    public int MatchId { get; set; }
    public OcrContextTeam HomeTeam { get; set; } = new();
    public OcrContextTeam AwayTeam { get; set; } = new();
}

public sealed class OcrContextTeam
{
    public int TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<OcrRosterPlayer> Players { get; set; } = [];
}

public sealed class OcrRosterPlayer
{
    public int PlayerId { get; set; }
    public int TeamId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int JerseyNumber { get; set; }
}

public sealed class OcrMatchContextResult
{
    public bool Success { get; set; }
    public OcrMatchContext? Context { get; set; }
    public string? ErrorMessage { get; set; }
}
