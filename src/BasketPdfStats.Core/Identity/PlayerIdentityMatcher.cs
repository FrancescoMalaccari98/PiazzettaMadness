using BasketPdfStats.Core.Database;

namespace BasketPdfStats.Core.Identity;

/// <summary>
/// Abbina una riga OCR (numero di maglia + nome) a un giocatore del roster DB.
/// Regola: jersey e nome devono entrambi essere compatibili. Non scrive nulla se il
/// match è incerto — restituisce solo un esito che la pipeline interpreterà.
/// </summary>
public sealed class PlayerIdentityMatcher
{
    private readonly double _certainThreshold;
    private readonly double _probableThreshold;

    /// <param name="certainThreshold">Distanza nome massima per un match certo (default 0.15).</param>
    /// <param name="probableThreshold">Distanza nome massima per un match probabile (default 0.30).</param>
    public PlayerIdentityMatcher(double certainThreshold = 0.15, double probableThreshold = 0.30)
    {
        if (certainThreshold > probableThreshold)
        {
            throw new ArgumentException("certainThreshold non può superare probableThreshold.");
        }

        _certainThreshold = certainThreshold;
        _probableThreshold = probableThreshold;
    }

    public PlayerMatchResult Match(string? ocrJersey, string? ocrName, IReadOnlyList<OcrRosterPlayer> roster)
    {
        ArgumentNullException.ThrowIfNull(roster);

        var jersey = ParseJersey(ocrJersey);
        if (jersey is null)
        {
            return new PlayerMatchResult
            {
                Kind = PlayerMatchKind.Unmatched,
                NameDistance = 1.0,
                OcrName = ocrName ?? string.Empty
            };
        }

        var candidate = roster.FirstOrDefault(player => player.JerseyNumber == jersey.Value);
        if (candidate is null)
        {
            return new PlayerMatchResult
            {
                Kind = PlayerMatchKind.NumberNotFound,
                Jersey = jersey,
                NameDistance = 1.0,
                OcrName = ocrName ?? string.Empty
            };
        }

        var normalizedOcrName = NameNormalizer.Normalize(ocrName);
        if (normalizedOcrName.Length == 0)
        {
            // Nome non leggibile ma jersey univoco nel roster: match probabile (da confermare).
            return new PlayerMatchResult
            {
                Kind = PlayerMatchKind.ProbableMatch,
                Player = candidate,
                Jersey = jersey,
                NameDistance = 1.0,
                NameMissing = true,
                OcrName = ocrName ?? string.Empty
            };
        }

        var distance = BestNameDistance(normalizedOcrName, candidate);
        var kind = distance <= _certainThreshold
            ? PlayerMatchKind.CertainMatch
            : distance <= _probableThreshold
                ? PlayerMatchKind.ProbableMatch
                : PlayerMatchKind.Conflict;

        return new PlayerMatchResult
        {
            Kind = kind,
            Player = candidate,
            Jersey = jersey,
            NameDistance = distance,
            OcrName = ocrName ?? string.Empty
        };
    }

    private static int? ParseJersey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        return digits.Length > 0 && int.TryParse(digits, out var value) ? value : null;
    }

    /// <summary>
    /// Distanza nome minima considerando: variante OCR-corretta, entrambi gli ordini
    /// (nome cognome / cognome nome) e match per sottoinsieme di token (es. solo cognome).
    /// </summary>
    private static double BestNameDistance(string normalizedOcrName, OcrRosterPlayer candidate)
    {
        var first = NameNormalizer.Normalize(candidate.FirstName);
        var last = NameNormalizer.Normalize(candidate.LastName);
        var candidateTokens = NameNormalizer.Tokenize($"{first} {last}");
        if (candidateTokens.Length == 0)
        {
            return 1.0;
        }

        var orderings = new[] { $"{first} {last}".Trim(), $"{last} {first}".Trim() };
        var variants = new[] { normalizedOcrName, NameNormalizer.ApplyOcrSubstitutions(normalizedOcrName) };

        var best = 1.0;
        foreach (var variant in variants)
        {
            foreach (var ordering in orderings)
            {
                best = Math.Min(best, NormalizedLevenshtein(variant, ordering));
            }

            // Match per sottoinsieme di token: ogni token OCR deve avvicinarsi a un token DB.
            var ocrTokens = NameNormalizer.Tokenize(variant);
            if (ocrTokens.Length > 0)
            {
                var tokenDistance = ocrTokens.Max(
                    ocrToken => candidateTokens.Min(
                        candidateToken => NormalizedLevenshtein(ocrToken, candidateToken)));
                best = Math.Min(best, tokenDistance);
            }
        }

        return best;
    }

    private static double NormalizedLevenshtein(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0)
        {
            return 0.0;
        }

        var distance = Levenshtein(a, b);
        return (double)distance / Math.Max(a.Length, b.Length);
    }

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
