using System.Text.RegularExpressions;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Core.Identity;

/// <summary>
/// Riallinea un <see cref="ProcessingResult"/> quando l'OCR ha invertito Home/Away
/// rispetto al DB. Scambia i lati e i token "Home"/"Away" negli entity ID di squadre,
/// giocatori e statistiche, e inverte punteggio finale e parziali. I campi slot di
/// <see cref="GameStats"/> (HomeTeamId/AwayTeamId) restano invariati: indicano lo slot,
/// non la squadra, ed è la squadra a spostarsi nello slot corretto.
/// </summary>
public static class SideInversion
{
    public static void Apply(ProcessingResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        foreach (var team in result.Teams)
        {
            team.Side = SwapSide(team.Side);
            team.TeamId = SwapId(team.TeamId);
        }

        foreach (var player in result.Players)
        {
            player.Side = SwapSide(player.Side);
            player.TeamId = SwapId(player.TeamId);
            player.EntityId = SwapId(player.EntityId);
        }

        foreach (var stat in result.Stats)
        {
            stat.Side = SwapSide(stat.Side);
            stat.EntityId = SwapId(stat.EntityId);
        }

        SwapFinalScore(result.Game);
        foreach (var period in result.Game.Periods)
        {
            (period.Home, period.Away) = (period.Away, period.Home);
        }
    }

    private static string? SwapSide(string? side) => side switch
    {
        "Home" => "Away",
        "Away" => "Home",
        _ => side
    };

    private static string SwapId(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return id;
        }

        var parts = id.Split(':');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "Home")
            {
                parts[i] = "Away";
            }
            else if (parts[i] == "Away")
            {
                parts[i] = "Home";
            }
        }

        return string.Join(':', parts);
    }

    private static void SwapFinalScore(GameStats game)
    {
        if (string.IsNullOrWhiteSpace(game.FinalScore))
        {
            return;
        }

        var match = Regex.Match(game.FinalScore, @"^\s*(\d+)\s*-\s*(\d+)\s*$");
        if (match.Success)
        {
            game.FinalScore = $"{match.Groups[2].Value}-{match.Groups[1].Value}";
        }
    }
}
