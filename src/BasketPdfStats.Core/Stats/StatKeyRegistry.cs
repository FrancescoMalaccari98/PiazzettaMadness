using BasketPdfStats.Core.Enums;

namespace BasketPdfStats.Core.Stats;

public static class StatKeyRegistry
{
    private static readonly IReadOnlyDictionary<string, StatKeyDefinition> Definitions = BuildDefinitions();

    public static IReadOnlyCollection<StatKeyDefinition> All => Definitions.Values.ToArray();

    public static bool Contains(string statKey) => Definitions.ContainsKey(statKey);

    public static StatKeyDefinition Get(string statKey)
    {
        if (Definitions.TryGetValue(statKey, out var definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Unknown stat key '{statKey}'.");
    }

    public static bool TryGet(string statKey, out StatKeyDefinition? definition) => Definitions.TryGetValue(statKey, out definition);

    private static IReadOnlyDictionary<string, StatKeyDefinition> BuildDefinitions()
    {
        var teamPlayer = new[] { StatScope.Team, StatScope.Player };
        var intStats = new (string Key, string Display, int Min, int Max)[]
        {
            ("fieldGoals.made", "Tiri dal campo segnati", 0, 120),
            ("fieldGoals.attempted", "Tiri dal campo tentati", 0, 160),
            ("twoPoints.made", "Tiri da 2 segnati", 0, 120),
            ("twoPoints.attempted", "Tiri da 2 tentati", 0, 160),
            ("threePoints.made", "Tiri da 3 segnati", 0, 80),
            ("threePoints.attempted", "Tiri da 3 tentati", 0, 100),
            ("freeThrows.made", "Tiri liberi segnati", 0, 80),
            ("freeThrows.attempted", "Tiri liberi tentati", 0, 100),
            ("rebounds.offensive", "Rimbalzi offensivi", 0, 80),
            ("rebounds.defensive", "Rimbalzi difensivi", 0, 100),
            ("rebounds.total", "Rimbalzi totali", 0, 160),
            ("assists", "Assist", 0, 80),
            ("turnovers", "Palle perse", 0, 80),
            ("steals", "Recuperi", 0, 80),
            ("blocks", "Stoppate", 0, 50),
            ("fouls.committed", "Falli commessi", 0, 80),
            ("fouls.drawn", "Falli subiti", 0, 80),
            ("efficiency", "Valutazione", -100, 250),
            ("evaluation", "Valutazione", -100, 250),
            ("minutes", "Minuti giocati in secondi", 0, 20000),
        };

        var list = new List<StatKeyDefinition>();
        list.Add(new StatKeyDefinition("points", "Punti", "number", null, true, [StatScope.Result, StatScope.Team, StatScope.Player, StatScope.Comparative], 0, 250));
        list.AddRange(intStats.Select(x => new StatKeyDefinition(x.Key, x.Display, "number", null, true, teamPlayer, x.Min, x.Max)));
        list.Add(new StatKeyDefinition("fieldGoals.percentage", "Percentuale tiri dal campo", "percentage", "%", true, teamPlayer, 0, 100));
        list.Add(new StatKeyDefinition("twoPoints.percentage", "Percentuale tiri da 2", "percentage", "%", true, teamPlayer, 0, 100));
        list.Add(new StatKeyDefinition("threePoints.percentage", "Percentuale tiri da 3", "percentage", "%", true, teamPlayer, 0, 100));
        list.Add(new StatKeyDefinition("freeThrows.percentage", "Percentuale tiri liberi", "percentage", "%", true, teamPlayer, 0, 100));
        list.Add(new StatKeyDefinition("plusMinus", "Plus/minus", "number", null, true, teamPlayer, -250, 250));
        list.Add(new StatKeyDefinition("period.points", "Punti periodo", "number", null, true, [StatScope.Result], 0, 250));
        list.Add(new StatKeyDefinition("comparative.pointsInThePaint", "Punti in area", "number", null, true, [StatScope.Comparative], 0, 250));
        list.Add(new StatKeyDefinition("comparative.pointsFromTurnovers", "Punti da palle perse", "number", null, true, [StatScope.Comparative], 0, 250));
        list.Add(new StatKeyDefinition("comparative.secondChancePoints", "Punti da secondi tiri", "number", null, true, [StatScope.Comparative], 0, 250));
        list.Add(new StatKeyDefinition("comparative.fastBreakPoints", "Punti in contropiede", "number", null, true, [StatScope.Comparative], 0, 250));
        list.Add(new StatKeyDefinition("comparative.fastBreakPointsFromTurnovers", "Punti in contropiede da palle perse", "number", null, true, [StatScope.Comparative], 0, 250));
        list.Add(new StatKeyDefinition("comparative.benchPoints", "Punti panchina", "number", null, true, [StatScope.Comparative], 0, 250));
        list.Add(new StatKeyDefinition("comparative.largestLead", "Massimo vantaggio", "string", null, true, [StatScope.Comparative]));
        list.Add(new StatKeyDefinition("comparative.biggestRun", "Massimo parziale", "string", null, true, [StatScope.Comparative]));
        list.Add(new StatKeyDefinition("comparative.pointsPerPossession", "Punti per possesso", "number", null, true, [StatScope.Comparative], 0, 5));
        list.Add(new StatKeyDefinition("comparative.leadChanges", "Cambi guida", "number", null, true, [StatScope.Comparative], 0, 100));
        list.Add(new StatKeyDefinition("comparative.timesTied", "Parita", "number", null, true, [StatScope.Comparative], 0, 100));
        list.Add(new StatKeyDefinition("comparative.timeInLead", "Tempo in vantaggio", "string", null, true, [StatScope.Comparative]));

        return list.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
    }
}
