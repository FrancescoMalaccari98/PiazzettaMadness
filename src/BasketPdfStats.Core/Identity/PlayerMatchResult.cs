using BasketPdfStats.Core.Database;

namespace BasketPdfStats.Core.Identity;

public enum PlayerMatchKind
{
    /// <summary>Jersey trovato e nome compatibile (distanza ≤ soglia certa).</summary>
    CertainMatch,

    /// <summary>Jersey trovato e nome plausibile (distanza ≤ soglia probabile), o nome non leggibile.</summary>
    ProbableMatch,

    /// <summary>Jersey trovato ma nome incompatibile (distanza oltre soglia): richiede revisione.</summary>
    Conflict,

    /// <summary>Nessun giocatore del roster ha quel numero di maglia.</summary>
    NumberNotFound,

    /// <summary>Riga OCR senza numero di maglia leggibile.</summary>
    Unmatched
}

/// <summary>
/// Esito dell'abbinamento di una riga OCR a un giocatore del roster DB.
/// Non scrive dati: rappresenta solo la decisione del matcher.
/// </summary>
public sealed class PlayerMatchResult
{
    public required PlayerMatchKind Kind { get; init; }

    /// <summary>Giocatore DB abbinato o candidato (null per NumberNotFound/Unmatched).</summary>
    public OcrRosterPlayer? Player { get; init; }

    /// <summary>Numero di maglia letto dall'OCR (null se non leggibile).</summary>
    public int? Jersey { get; init; }

    /// <summary>Distanza nome normalizzata [0..1]; 1 quando non confrontabile.</summary>
    public double NameDistance { get; init; }

    /// <summary>True se il nome OCR non era leggibile (match basato sul solo jersey univoco).</summary>
    public bool NameMissing { get; init; }

    public string OcrName { get; init; } = string.Empty;
}
