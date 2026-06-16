using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace PiazzettaMadness.App.Data;

public static class LiveSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS live_session (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                match_id INTEGER NOT NULL,
                package_json TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public static void Save(AppDbContext db, int matchId)
    {
        var match = db.Matches.FirstOrDefault(x => x.Id == matchId);
        if (match is null)
        {
            return;
        }

        var edition = db.Editions.FirstOrDefault(x => x.Id == match.EditionId);
        var teamSides = db.MatchTeams.Where(x => x.MatchId == matchId).ToList();
        var teamIds = teamSides.Select(x => x.TeamId).ToHashSet();
        var matchPlayers = db.MatchPlayers.Where(x => x.MatchId == matchId).ToList();
        var playerIds = matchPlayers.Select(x => x.PlayerId).ToHashSet();

        var package = new LivePackage
        {
            Tournament = edition is null ? null : db.Tournaments.FirstOrDefault(x => x.Id == edition.TournamentId),
            Edition = edition,
            Court = match.CourtId is null ? null : db.Courts.FirstOrDefault(x => x.Id == match.CourtId),
            Group = match.GroupId is null ? null : db.TournamentGroups.FirstOrDefault(x => x.Id == match.GroupId),
            Match = match,
            Teams = db.Teams.Where(x => teamIds.Contains(x.Id)).ToList(),
            Players = db.Players.Where(x => playerIds.Contains(x.Id)).ToList(),
            Rosters = db.TeamRosters.Where(x => teamIds.Contains(x.TeamId) && playerIds.Contains(x.PlayerId)).ToList(),
            MatchTeams = teamSides,
            MatchPlayers = matchPlayers,
            MatchEvents = db.MatchEvents.Where(x => x.MatchId == matchId).ToList(),
            ScoreboardState = db.ScoreboardStates.FirstOrDefault(x => x.MatchId == matchId)
        };

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO live_session (id, match_id, package_json, updated_at)
            VALUES (1, $matchId, $json, $updatedAt)
            ON CONFLICT(id) DO UPDATE SET
                match_id = excluded.match_id,
                package_json = excluded.package_json,
                updated_at = excluded.updated_at;
            """;
        command.Parameters.AddWithValue("$matchId", matchId);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(package, JsonOptions));
        command.Parameters.AddWithValue("$updatedAt", DateTime.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    public static int? Restore(AppDbContext db)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT package_json FROM live_session WHERE id = 1;";
        var json = command.ExecuteScalar() as string;
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        LivePackage? package;
        try
        {
            package = JsonSerializer.Deserialize<LivePackage>(json, JsonOptions);
        }
        catch (JsonException)
        {
            Clear();
            return null;
        }

        if (package?.Match is null)
        {
            Clear();
            return null;
        }

        AddIfMissing(db.Tournaments, package.Tournament);
        AddIfMissing(db.Editions, package.Edition);
        AddIfMissing(db.Courts, package.Court);
        AddIfMissing(db.TournamentGroups, package.Group);
        AddRangeIfMissing(db.Teams, package.Teams);
        AddRangeIfMissing(db.Players, package.Players);
        AddRangeIfMissing(db.TeamRosters, package.Rosters);
        AddIfMissing(db.Matches, package.Match);
        AddRangeIfMissing(db.MatchTeams, package.MatchTeams);
        AddRangeIfMissing(db.MatchPlayers, package.MatchPlayers);
        AddRangeIfMissing(db.MatchEvents, package.MatchEvents);
        AddIfMissing(db.ScoreboardStates, package.ScoreboardState);
        try
        {
            db.SaveChanges();
            return package.Match.Id;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            Clear();
            return null;
        }
    }

    public static void Clear()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM live_session WHERE id = 1;";
        command.ExecuteNonQuery();
    }

    private static SqliteConnection Open()
    {
        var connection = new SqliteConnection($"Data Source={AppPaths.LiveDatabasePath}");
        connection.Open();
        return connection;
    }

    private static void AddIfMissing<T>(Microsoft.EntityFrameworkCore.DbSet<T> set, T? row) where T : class
    {
        if (row is not null)
        {
            set.Add(row);
        }
    }

    private static void AddRangeIfMissing<T>(Microsoft.EntityFrameworkCore.DbSet<T> set, IEnumerable<T> rows) where T : class =>
        set.AddRange(rows);

    private sealed class LivePackage
    {
        public Tournament? Tournament { get; set; }
        public Edition? Edition { get; set; }
        public Court? Court { get; set; }
        public TournamentGroup? Group { get; set; }
        public Match? Match { get; set; }
        public List<Team> Teams { get; set; } = [];
        public List<Player> Players { get; set; } = [];
        public List<TeamRoster> Rosters { get; set; } = [];
        public List<MatchTeam> MatchTeams { get; set; } = [];
        public List<MatchPlayer> MatchPlayers { get; set; } = [];
        public List<MatchEvent> MatchEvents { get; set; } = [];
        public ScoreboardStateRecord? ScoreboardState { get; set; }
    }
}
