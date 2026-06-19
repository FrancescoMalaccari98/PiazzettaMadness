namespace BasketPdfStats.Core.Identity;

public enum TeamMatchOutcome
{
    /// <summary>Le squadre OCR corrispondono ai lati DB nell'ordine letto.</summary>
    CorrectOrder,

    /// <summary>Home e Away risultano invertiti rispetto al DB: serve riallineamento.</summary>
    Inverted,

    /// <summary>Nessun accoppiamento supera la soglia: squadre non riconosciute.</summary>
    Mismatch
}

public sealed class TeamMatchResult
{
    public required TeamMatchOutcome Outcome { get; init; }

    /// <summary>Similarità media [0,1] dell'accoppiamento nell'ordine letto.</summary>
    public double CorrectScore { get; init; }

    /// <summary>Similarità media [0,1] dell'accoppiamento invertito.</summary>
    public double InvertedScore { get; init; }
}
