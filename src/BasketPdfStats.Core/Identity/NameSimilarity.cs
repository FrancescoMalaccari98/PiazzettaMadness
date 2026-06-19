namespace BasketPdfStats.Core.Identity;

/// <summary>
/// Similarità tra stringhe basata su distanza di Levenshtein normalizzata.
/// Si aspetta input già normalizzati (vedi <see cref="NameNormalizer"/>).
/// </summary>
public static class NameSimilarity
{
    /// <summary>Distanza normalizzata in [0,1]: 0 = identiche, 1 = completamente diverse.</summary>
    public static double NormalizedDistance(string a, string b)
    {
        a ??= string.Empty;
        b ??= string.Empty;
        if (a.Length == 0 && b.Length == 0)
        {
            return 0.0;
        }

        var distance = Levenshtein(a, b);
        return (double)distance / Math.Max(a.Length, b.Length);
    }

    /// <summary>Similarità in [0,1]: 1 = identiche.</summary>
    public static double Similarity(string a, string b) => 1.0 - NormalizedDistance(a, b);

    private static int Levenshtein(string a, string b)
    {
        if (a.Length == 0)
        {
            return b.Length;
        }

        if (b.Length == 0)
        {
            return a.Length;
        }

        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
