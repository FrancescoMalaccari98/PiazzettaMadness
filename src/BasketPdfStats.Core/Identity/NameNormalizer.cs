using System.Globalization;
using System.Text;

namespace BasketPdfStats.Core.Identity;

/// <summary>
/// Normalizza i nomi per il confronto fuzzy: rimuove accenti, abbassa a minuscolo,
/// converte apostrofi/trattini in spazi, comprime gli spazi. Espone anche una variante
/// con sostituzioni OCR comuni (0→o, 1→i, 5→s, rn→m) da usare solo se migliora il match.
/// Non produce identità canoniche: è solo supporto al confronto.
/// </summary>
public static class NameNormalizer
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var decomposed = raw.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue; // accenti
            }

            if (character is '\'' or '’' or 'ʼ' or '-' or '‐' or '–')
            {
                builder.Append(' '); // apostrofi e trattini → spazio
            }
            else if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else if (char.IsWhiteSpace(character))
            {
                builder.Append(' ');
            }
            // altra punteggiatura: scartata
        }

        return CollapseWhitespace(builder.ToString());
    }

    /// <summary>
    /// Applica sostituzioni OCR comuni su una stringa già normalizzata.
    /// Va usata come variante alternativa: il matcher sceglie la distanza minore.
    /// </summary>
    public static string ApplyOcrSubstitutions(string normalized)
    {
        if (string.IsNullOrEmpty(normalized))
        {
            return normalized;
        }

        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            builder.Append(character switch
            {
                '0' => 'o',
                '1' => 'i',
                '5' => 's',
                _ => character
            });
        }

        return CollapseWhitespace(builder.ToString().Replace("rn", "m"));
    }

    public static string[] Tokenize(string normalized) =>
        string.IsNullOrEmpty(normalized)
            ? []
            : normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static string CollapseWhitespace(string value) =>
        string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
