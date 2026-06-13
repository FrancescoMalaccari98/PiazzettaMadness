namespace BasketPdfStats.Core.Normalization;

public static class CanonicalStatKeyMapper
{
    private static readonly IReadOnlyDictionary<string, string[]> Aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["tiri dal campo r/t"] = ["fieldGoals.made", "fieldGoals.attempted"],
        ["tiri dal campo %"] = ["fieldGoals.percentage"],
        ["2 punti r/t"] = ["twoPoints.made", "twoPoints.attempted"],
        ["2 punti %"] = ["twoPoints.percentage"],
        ["3 punti r/t"] = ["threePoints.made", "threePoints.attempted"],
        ["3 punti %"] = ["threePoints.percentage"],
        ["tiri liberi r/t"] = ["freeThrows.made", "freeThrows.attempted"],
        ["tiri liberi %"] = ["freeThrows.percentage"],
        ["ro"] = ["rebounds.offensive"],
        ["rd"] = ["rebounds.defensive"],
        ["rt"] = ["rebounds.total"],
        ["as"] = ["assists"],
        ["pp"] = ["turnovers"],
        ["pr"] = ["steals"],
        ["sd"] = ["blocks"],
        ["ff"] = ["fouls.committed"],
        ["fs"] = ["fouls.drawn"],
        ["+/-"] = ["plusMinus"],
        ["val."] = ["evaluation"],
        ["ptl"] = ["points"],
        ["efficiency"] = ["evaluation"]
    };

    public static string Map(string statKey)
    {
        return MapAll(statKey)[0];
    }

    public static IReadOnlyList<string> MapAll(string statKey)
    {
        return Aliases.TryGetValue(statKey.Trim(), out var canonical) ? canonical : [statKey];
    }
}
