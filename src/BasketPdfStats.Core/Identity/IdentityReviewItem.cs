namespace BasketPdfStats.Core.Identity;

public enum IdentityReviewReason
{
    /// <summary>Jersey trovato, nome plausibile ma non certo: da confermare.</summary>
    ProbableMatch,

    /// <summary>Jersey trovato ma nome incompatibile: richiede revisione (blocca).</summary>
    Conflict,

    /// <summary>Numero di maglia OCR non presente nel roster DB.</summary>
    NumberNotFound,

    /// <summary>Riga OCR senza numero di maglia leggibile.</summary>
    Unmatched,

    /// <summary>Giocatore del roster DB non trovato nel PDF: richiede revisione (blocca).</summary>
    NotInPdf
}

/// <summary>
/// Voce di revisione per un'associazione OCR↔giocatore DB dubbia o mancante.
/// Prodotta in sola lettura (Fase 7A): non modifica le entità del risultato.
/// </summary>
public sealed class IdentityReviewItem
{
    public string Side { get; set; } = string.Empty; // Home | Away
    public IdentityReviewReason Reason { get; set; }

    /// <summary>EntityId OCR della riga (es. "player:Home:jersey:5"), per propagare la scelta manuale.</summary>
    public string? OcrEntityId { get; set; }

    public string? OcrJersey { get; set; }
    public string? OcrName { get; set; }
    public int? CandidatePlayerId { get; set; }
    public string? CandidateName { get; set; }
    public double ConfidenceScore { get; set; }
}
