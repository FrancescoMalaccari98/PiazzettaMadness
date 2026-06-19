namespace BasketPdfStats.Core.Models;

/// <summary>
/// Payload canonico inviato a POST /api-ocr/import (Fase 8). Realizza il "CanonicalGameResult":
/// porta gli ID canonici DB (matchId, teamId, playerId) già risolti da C#. Il PHP verifica gli ID
/// e salva, senza rifare fuzzy matching. Le statistiche restano nella forma flat per entityId OCR,
/// così la logica di scrittura PHP esistente resta invariata; ogni giocatore porta il proprio
/// playerId canonico e l'entityId OCR per collegare le statistiche.
/// </summary>
public sealed class ImportPayload
{
    public int MatchId { get; set; }
    public List<ImportTeam> Teams { get; set; } = [];
    public List<ImportPlayer> Players { get; set; } = [];
    public List<StatValue> Stats { get; set; } = [];
}

public sealed class ImportTeam
{
    public string Side { get; set; } = string.Empty; // Home | Away
    public int TeamId { get; set; }                    // ID canonico DB
    public string? Name { get; set; }
    public string? Abbreviation { get; set; }
}

public sealed class ImportPlayer
{
    public string EntityId { get; set; } = string.Empty; // entityId OCR (collega le statistiche)
    public string Side { get; set; } = string.Empty;
    public int PlayerId { get; set; }                    // ID canonico DB risolto da C#
    public int TeamId { get; set; }                      // ID canonico DB
    public string? Number { get; set; }
    public bool Starter { get; set; }
    public bool DidNotPlay { get; set; }
}
