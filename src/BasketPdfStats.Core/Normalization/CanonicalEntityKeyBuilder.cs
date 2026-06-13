using System.Globalization;
using System.Text;

namespace BasketPdfStats.Core.Normalization;

public static class CanonicalEntityKeyBuilder
{
    public static string Team(string? side) => $"team:{NormalizeSide(side)}";

    public static CanonicalPlayerIdentity Player(string? side, string? rawJersey, string? name, int rowIndex)
    {
        var normalizedSide = NormalizeSide(side);
        if (TryParseJersey(rawJersey, out var jersey, out var starter))
        {
            return new CanonicalPlayerIdentity($"player:{normalizedSide}:jersey:{jersey}", jersey, starter);
        }

        var normalizedName = NormalizeName(name);
        return normalizedName.Length > 0
            ? new CanonicalPlayerIdentity($"player:{normalizedSide}:name:{normalizedName}", null, false)
            : new CanonicalPlayerIdentity($"player:{normalizedSide}:row:{Math.Max(0, rowIndex):D2}", null, false);
    }

    public static bool TryParseJersey(string? rawJersey, out string jersey, out bool starter)
    {
        var value = rawJersey?.Trim() ?? string.Empty;
        starter = value.StartsWith('*');
        var digits = starter ? value[1..].Trim() : value;
        if (digits.Length == 0 ||
            !digits.All(char.IsDigit) ||
            !int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            jersey = string.Empty;
            starter = false;
            return false;
        }

        jersey = number.ToString(CultureInfo.InvariantCulture);
        return true;
    }

    public static string NormalizeName(string? name)
    {
        var builder = new StringBuilder();
        var pendingSeparator = false;
        foreach (var character in (name ?? string.Empty).Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(character);
                pendingSeparator = false;
            }
            else
            {
                pendingSeparator = true;
            }
        }

        return builder.ToString();
    }

    public static bool IsSpecialPlayerRow(string? jersey, string? name)
    {
        var value = $"{jersey} {name}";
        return value.Contains("Squadra", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("Allenatore", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("Totali", StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeSide(string? side)
    {
        return side?.Trim().ToLowerInvariant() switch
        {
            "home" => "Home",
            "away" => "Away",
            _ => string.IsNullOrWhiteSpace(side) ? "Unknown" : side.Trim()
        };
    }
}

public sealed record CanonicalPlayerIdentity(string EntityId, string? Jersey, bool Starter);
