using System.IO;
using Microsoft.Data.Sqlite;

namespace PiazzettaMadness.App.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        if (File.Exists(AppPaths.DatabasePath))
        {
            File.Delete(AppPaths.DatabasePath);
        }

        ApplySchema();
        ApplyMigrations();
        LiveSessionStore.Initialize();
    }

    private static void ApplySchema()
    {
        using var db = new AppDbContext();
        db.Database.EnsureCreated();
    }

    private static void SeedDemoData()
    {
        using var connection = new SqliteConnection($"Data Source={AppPaths.DatabasePath}");
        connection.Open();

        using var transaction = connection.BeginTransaction();
        var tournamentId = ExecuteScalarLong(connection, transaction, "INSERT INTO tournaments (name, description) VALUES ('Piazzetta Madness', 'Torneo basket locale'); SELECT last_insert_rowid();");
        var editionId = ExecuteScalarLong(connection, transaction, $"INSERT INTO editions (tournament_id, name, year, status, is_console_active) VALUES ({tournamentId}, 'Piazzetta Madness 2026', 2026, 'Draft', 1); SELECT last_insert_rowid();");
        var courtId = ExecuteScalarLong(connection, transaction, $"INSERT INTO courts (edition_id, name, location) VALUES ({editionId}, 'Piazzetta Verde', 'Porto Potenza Picena'); SELECT last_insert_rowid();");
        var groupAId = ExecuteScalarLong(connection, transaction, $"INSERT INTO tournament_groups (edition_id, name, code, sort_order) VALUES ({editionId}, 'Girone A', 'A', 1); SELECT last_insert_rowid();");
        var groupBId = ExecuteScalarLong(connection, transaction, $"INSERT INTO tournament_groups (edition_id, name, code, sort_order) VALUES ({editionId}, 'Girone B', 'B', 2); SELECT last_insert_rowid();");

        var teams = new[]
        {
            ("A1", "#e63946"), ("A2", "#f77f00"), ("A3", "#2a9d8f"), ("A4", "#457b9d"),
            ("B1", "#7b2cbf"), ("B2", "#06d6a0"), ("B3", "#ef476f"), ("B4", "#118ab2")
        };

        var teamIds = new List<long>();
        for (var i = 0; i < teams.Length; i++)
        {
            var team = teams[i];
            var teamId = ExecuteScalarLong(
                connection,
                transaction,
                "INSERT INTO teams (edition_id, name, short_name, primary_color, secondary_color) " +
                $"VALUES ({editionId}, 'Team {team.Item1}', '{team.Item1}', '{team.Item2}', '#111827'); SELECT last_insert_rowid();");
            teamIds.Add(teamId);

            var groupId = i < 4 ? groupAId : groupBId;
            Execute(connection, transaction, $"INSERT INTO group_teams (group_id, team_id, seed_label) VALUES ({groupId}, {teamId}, '{team.Item1}');");
        }

        var matchId = ExecuteScalarLong(connection, transaction, $"""
            INSERT INTO matches
                (edition_id, group_id, court_id, name, phase, round, status, max_score, max_score_enabled)
            VALUES
                ({editionId}, {groupAId}, {courtId}, 'A2 vs A4', 'GroupStage', 'Giornata 1', 'Ready', 45, 1);
            SELECT last_insert_rowid();
            """);
        Execute(connection, transaction, $"INSERT INTO match_teams (match_id, team_id, side) VALUES ({matchId}, {teamIds[1]}, 'Home'), ({matchId}, {teamIds[3]}, 'Away');");
        Execute(connection, transaction, $"""
            INSERT INTO scoreboard_states
                (match_id, current_period, current_period_type, game_clock_ms_remaining, shot_clock_ms_remaining)
            VALUES
                ({matchId}, 1, 'Regular', 720000, 24000);
            """);
        Execute(connection, transaction, $"INSERT INTO competition_events (edition_id, event_type, name, status) VALUES ({editionId}, 'ThreePointContest', '3 Point Contest', 'Scheduled');");

        transaction.Commit();
    }

    private static void EnsureDemoData()
    {
        using var connection = new SqliteConnection($"Data Source={AppPaths.DatabasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM tournaments;";
        var count = Convert.ToInt64(command.ExecuteScalar());

        if (count == 0)
        {
            SeedDemoData();
        }
    }

    private static void EnsureRichDemoData()
    {
        using var connection = new SqliteConnection($"Data Source={AppPaths.DatabasePath}");
        connection.Open();

        using var transaction = connection.BeginTransaction();
        var editionId = ExecuteScalarNullableLong(connection, transaction, "SELECT id FROM editions ORDER BY year DESC, id DESC LIMIT 1;");
        if (editionId is null)
        {
            transaction.Commit();
            return;
        }

        var courtId = GetOrCreateCourt(connection, transaction, editionId.Value, "Campo Centrale", "Piazzetta");
        var groupAId = GetOrCreateGroup(connection, transaction, editionId.Value, "Girone A", "A", 1);
        var groupBId = GetOrCreateGroup(connection, transaction, editionId.Value, "Girone B", "B", 2);

        var demoTeams = new[]
        {
            ("Piazzetta Hawks", "HAW", "#e63946", "#111827", groupAId, "HAW"),
            ("Porto Bulls", "BUL", "#f77f00", "#111827", groupAId, "BUL"),
            ("Monte Lakers", "LAK", "#6d28d9", "#fbbf24", groupAId, "LAK"),
            ("Adriatica Sharks", "SHA", "#0284c7", "#e0f2fe", groupAId, "SHA"),
            ("Centro Raptors", "RAP", "#16a34a", "#052e16", groupBId, "RAP"),
            ("Piazza Celtics", "CEL", "#059669", "#f8fafc", groupBId, "CEL"),
            ("Marina Suns", "SUN", "#facc15", "#7c2d12", groupBId, "SUN"),
            ("Collina Kings", "KIN", "#7c3aed", "#f8fafc", groupBId, "KIN")
        };

        var teamIds = new Dictionary<string, long>();
        foreach (var team in demoTeams)
        {
            var teamId = GetOrCreateTeam(connection, transaction, editionId.Value, team.Item1, team.Item2, team.Item3, team.Item4);
            teamIds[team.Item2] = teamId;
            EnsureGroupTeam(connection, transaction, team.Item5, teamId, team.Item6);
        }

        var firstNames = new[]
        {
            "Luca", "Marco", "Andrea", "Matteo", "Davide", "Simone", "Alessandro", "Francesco",
            "Gabriele", "Filippo", "Riccardo", "Tommaso", "Nicolò", "Edoardo", "Michele", "Stefano"
        };
        var lastNames = new[]
        {
            "Rossi", "Bianchi", "Marchetti", "Ferri", "Conti", "Romagnoli", "Serafini", "Moretti",
            "Gentili", "Mancini", "De Santis", "Giuliani", "Silvestri", "Carletti", "Monti", "Lombardi"
        };
        var roles = new[] { "Playmaker", "Guardia", "Ala", "Ala forte", "Centro", "Guardia", "Ala" };
        var rosterByTeam = new Dictionary<string, List<long>>();

        for (var teamIndex = 0; teamIndex < demoTeams.Length; teamIndex++)
        {
            var team = demoTeams[teamIndex];
            var roster = new List<long>();
            var teamId = teamIds[team.Item2];

            for (var playerIndex = 0; playerIndex < 7; playerIndex++)
            {
                var jerseyNumber = 4 + playerIndex + (teamIndex % 3 * 10);
                var firstName = firstNames[(teamIndex * 2 + playerIndex) % firstNames.Length];
                var lastName = lastNames[(teamIndex * 3 + playerIndex) % lastNames.Length];
                var email = $"{team.Item2.ToLowerInvariant()}.{jerseyNumber}@demo.piazzetta.local";
                var playerId = GetOrCreatePlayer(
                    connection,
                    transaction,
                    firstName,
                    lastName,
                    playerIndex == 0 ? $"{team.Item2} Captain" : null,
                    $"DMO{teamIndex + 1:00}{playerIndex + 1:00}PMADNESS",
                    $"Via del Basket {teamIndex + 1}, {playerIndex + 4}",
                    $"+39 333 10{teamIndex:00}{playerIndex:00}",
                    email,
                    $"199{playerIndex % 8}-{(playerIndex % 9) + 1:00}-{(teamIndex + playerIndex + 10):00}");

                EnsureRoster(connection, transaction, teamId, playerId, jerseyNumber, roles[playerIndex], playerIndex == 0);
                roster.Add(playerId);
            }

            rosterByTeam[team.Item2] = roster;
        }

        var match1Id = GetOrCreateMatch(connection, transaction, editionId.Value, groupAId, courtId, "Piazzetta Hawks vs Porto Bulls", "GroupStage", "Giornata 1", "Finished");
        EnsureMatchTeams(connection, transaction, match1Id, teamIds["HAW"], teamIds["BUL"], 45, 39, teamIds["HAW"]);
        EnsureMatchPlayers(connection, transaction, match1Id, teamIds["HAW"], rosterByTeam["HAW"]);
        EnsureMatchPlayers(connection, transaction, match1Id, teamIds["BUL"], rosterByTeam["BUL"]);

        var match2Id = GetOrCreateMatch(connection, transaction, editionId.Value, groupAId, courtId, "Monte Lakers vs Adriatica Sharks", "GroupStage", "Giornata 1", "Ready");
        EnsureMatchTeams(connection, transaction, match2Id, teamIds["LAK"], teamIds["SHA"], 0, 0, null);
        EnsureMatchPlayers(connection, transaction, match2Id, teamIds["LAK"], rosterByTeam["LAK"]);
        EnsureMatchPlayers(connection, transaction, match2Id, teamIds["SHA"], rosterByTeam["SHA"]);
        EnsureScoreboardState(connection, transaction, match2Id, 0, 0);

        var match3Id = GetOrCreateMatch(connection, transaction, editionId.Value, groupBId, courtId, "Centro Raptors vs Piazza Celtics", "GroupStage", "Giornata 1", "Finished");
        EnsureMatchTeams(connection, transaction, match3Id, teamIds["RAP"], teamIds["CEL"], 37, 41, teamIds["CEL"]);
        EnsureMatchPlayers(connection, transaction, match3Id, teamIds["RAP"], rosterByTeam["RAP"]);
        EnsureMatchPlayers(connection, transaction, match3Id, teamIds["CEL"], rosterByTeam["CEL"]);

        var match4Id = GetOrCreateMatch(connection, transaction, editionId.Value, groupBId, courtId, "Marina Suns vs Collina Kings", "GroupStage", "Giornata 1", "Scheduled");
        EnsureMatchTeams(connection, transaction, match4Id, teamIds["SUN"], teamIds["KIN"], 0, 0, null);
        EnsureMatchPlayers(connection, transaction, match4Id, teamIds["SUN"], rosterByTeam["SUN"]);
        EnsureMatchPlayers(connection, transaction, match4Id, teamIds["KIN"], rosterByTeam["KIN"]);

        var semifinalId = GetOrCreateMatch(connection, transaction, editionId.Value, null, courtId, "Semifinale 1", "SemiFinal", "Final Four", "Scheduled");
        EnsureMatchTeams(connection, transaction, semifinalId, teamIds["HAW"], teamIds["CEL"], 0, 0, null);

        var finalId = GetOrCreateMatch(connection, transaction, editionId.Value, null, courtId, "Finale Piazzetta Madness", "Final", "Final Four", "Scheduled");
        EnsureMatchTeams(connection, transaction, finalId, teamIds["BUL"], teamIds["RAP"], 0, 0, null);

        EnsureScoreEvent(connection, transaction, match1Id, teamIds["HAW"], rosterByTeam["HAW"][0], 3, "Tripla in transizione");
        EnsureScoreEvent(connection, transaction, match1Id, teamIds["BUL"], rosterByTeam["BUL"][1], 2, "Canestro dal pitturato");
        EnsureScoreEvent(connection, transaction, match3Id, teamIds["CEL"], rosterByTeam["CEL"][0], 3, "Tripla decisiva");

        var contestId = GetOrCreateCompetitionEvent(connection, transaction, editionId.Value, "3 Point Contest", "Scheduled");
        var seed = 1;
        foreach (var team in demoTeams)
        {
            var entryId = EnsureThreePointEntry(connection, transaction, contestId, teamIds[team.Item2], rosterByTeam[team.Item2][0], seed++);
            EnsureThreePointRound(connection, transaction, entryId, 1, "Qualification", 3 + seed % 4, 4, 2 + seed % 3, 5, 3);
        }

        transaction.Commit();
    }

    private static long GetOrCreateCourt(SqliteConnection connection, SqliteTransaction transaction, long editionId, string name, string location)
    {
        var existingId = ExecuteScalarNullableLong(
            connection,
            transaction,
            "SELECT id FROM courts WHERE edition_id = $editionId AND name = $name LIMIT 1;",
            ("$editionId", editionId),
            ("$name", name));
        if (existingId is not null)
        {
            return existingId.Value;
        }

        return ExecuteScalarLong(
            connection,
            transaction,
            "INSERT INTO courts (edition_id, name, location) VALUES ($editionId, $name, $location); SELECT last_insert_rowid();",
            ("$editionId", editionId),
            ("$name", name),
            ("$location", location));
    }

    private static long GetOrCreateGroup(SqliteConnection connection, SqliteTransaction transaction, long editionId, string name, string code, int sortOrder)
    {
        var existingId = ExecuteScalarNullableLong(
            connection,
            transaction,
            "SELECT id FROM tournament_groups WHERE edition_id = $editionId AND code = $code LIMIT 1;",
            ("$editionId", editionId),
            ("$code", code));
        if (existingId is not null)
        {
            return existingId.Value;
        }

        return ExecuteScalarLong(
            connection,
            transaction,
            "INSERT INTO tournament_groups (edition_id, name, code, sort_order) VALUES ($editionId, $name, $code, $sortOrder); SELECT last_insert_rowid();",
            ("$editionId", editionId),
            ("$name", name),
            ("$code", code),
            ("$sortOrder", sortOrder));
    }

    private static long GetOrCreateTeam(SqliteConnection connection, SqliteTransaction transaction, long editionId, string name, string shortName, string primaryColor, string secondaryColor)
    {
        var existingId = ExecuteScalarNullableLong(
            connection,
            transaction,
            "SELECT id FROM teams WHERE edition_id = $editionId AND name = $name LIMIT 1;",
            ("$editionId", editionId),
            ("$name", name));
        if (existingId is not null)
        {
            Execute(
                connection,
                transaction,
                "UPDATE teams SET short_name = $shortName, primary_color = $primaryColor, secondary_color = $secondaryColor, updated_at = CURRENT_TIMESTAMP WHERE id = $id;",
                ("$id", existingId.Value),
                ("$shortName", shortName),
                ("$primaryColor", primaryColor),
                ("$secondaryColor", secondaryColor));
            return existingId.Value;
        }

        return ExecuteScalarLong(
            connection,
            transaction,
            "INSERT INTO teams (edition_id, name, short_name, primary_color, secondary_color) VALUES ($editionId, $name, $shortName, $primaryColor, $secondaryColor); SELECT last_insert_rowid();",
            ("$editionId", editionId),
            ("$name", name),
            ("$shortName", shortName),
            ("$primaryColor", primaryColor),
            ("$secondaryColor", secondaryColor));
    }

    private static void EnsureGroupTeam(SqliteConnection connection, SqliteTransaction transaction, long groupId, long teamId, string seedLabel)
    {
        if (ExecuteScalarNullableLong(connection, transaction, "SELECT id FROM group_teams WHERE group_id = $groupId AND team_id = $teamId LIMIT 1;", ("$groupId", groupId), ("$teamId", teamId)) is not null)
        {
            return;
        }

        Execute(
            connection,
            transaction,
            "INSERT INTO group_teams (group_id, team_id, seed_label) VALUES ($groupId, $teamId, $seedLabel);",
            ("$groupId", groupId),
            ("$teamId", teamId),
            ("$seedLabel", seedLabel));
    }

    private static long GetOrCreatePlayer(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string firstName,
        string lastName,
        string? nickname,
        string fiscalCode,
        string address,
        string phoneNumber,
        string email,
        string birthDate)
    {
        var existingId = ExecuteScalarNullableLong(connection, transaction, "SELECT id FROM players WHERE email = $email LIMIT 1;", ("$email", email));
        if (existingId is not null)
        {
            Execute(
                connection,
                transaction,
                """
                UPDATE players
                SET first_name = $firstName,
                    last_name = $lastName,
                    nickname = $nickname,
                    fiscal_code = $fiscalCode,
                    address = $address,
                    phone_number = $phoneNumber,
                    birth_date = $birthDate,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = $id;
                """,
                ("$id", existingId.Value),
                ("$firstName", firstName),
                ("$lastName", lastName),
                ("$nickname", nickname),
                ("$fiscalCode", fiscalCode),
                ("$address", address),
                ("$phoneNumber", phoneNumber),
                ("$birthDate", birthDate));
            return existingId.Value;
        }

        return ExecuteScalarLong(
            connection,
            transaction,
            """
            INSERT INTO players
                (first_name, last_name, nickname, fiscal_code, address, phone_number, email, birth_date)
            VALUES
                ($firstName, $lastName, $nickname, $fiscalCode, $address, $phoneNumber, $email, $birthDate);
            SELECT last_insert_rowid();
            """,
            ("$firstName", firstName),
            ("$lastName", lastName),
            ("$nickname", nickname),
            ("$fiscalCode", fiscalCode),
            ("$address", address),
            ("$phoneNumber", phoneNumber),
            ("$email", email),
            ("$birthDate", birthDate));
    }

    private static void EnsureRoster(SqliteConnection connection, SqliteTransaction transaction, long teamId, long playerId, int jerseyNumber, string role, bool isCaptain)
    {
        var existingId = ExecuteScalarNullableLong(connection, transaction, "SELECT id FROM team_rosters WHERE team_id = $teamId AND player_id = $playerId LIMIT 1;", ("$teamId", teamId), ("$playerId", playerId));
        if (existingId is not null)
        {
            Execute(
                connection,
                transaction,
                "UPDATE team_rosters SET jersey_number = $jerseyNumber, role = $role, is_captain = $isCaptain, is_active = 1, updated_at = CURRENT_TIMESTAMP WHERE id = $id;",
                ("$id", existingId.Value),
                ("$jerseyNumber", jerseyNumber),
                ("$role", role),
                ("$isCaptain", isCaptain ? 1 : 0));
            return;
        }

        Execute(
            connection,
            transaction,
            "INSERT INTO team_rosters (team_id, player_id, jersey_number, role, is_captain, is_active) VALUES ($teamId, $playerId, $jerseyNumber, $role, $isCaptain, 1);",
            ("$teamId", teamId),
            ("$playerId", playerId),
            ("$jerseyNumber", jerseyNumber),
            ("$role", role),
            ("$isCaptain", isCaptain ? 1 : 0));
    }

    private static long GetOrCreateMatch(SqliteConnection connection, SqliteTransaction transaction, long editionId, long? groupId, long courtId, string name, string phase, string round, string status)
    {
        var existingId = ExecuteScalarNullableLong(connection, transaction, "SELECT id FROM matches WHERE edition_id = $editionId AND name = $name LIMIT 1;", ("$editionId", editionId), ("$name", name));
        if (existingId is not null)
        {
            Execute(
                connection,
                transaction,
                "UPDATE matches SET group_id = $groupId, court_id = $courtId, phase = $phase, round = $round, status = $status, updated_at = CURRENT_TIMESTAMP WHERE id = $id;",
                ("$id", existingId.Value),
                ("$groupId", groupId),
                ("$courtId", courtId),
                ("$phase", phase),
                ("$round", round),
                ("$status", status));
            return existingId.Value;
        }

        return ExecuteScalarLong(
            connection,
            transaction,
            """
            INSERT INTO matches
                (edition_id, group_id, court_id, name, phase, round, status, max_score, max_score_enabled)
            VALUES
                ($editionId, $groupId, $courtId, $name, $phase, $round, $status, 45, 1);
            SELECT last_insert_rowid();
            """,
            ("$editionId", editionId),
            ("$groupId", groupId),
            ("$courtId", courtId),
            ("$name", name),
            ("$phase", phase),
            ("$round", round),
            ("$status", status));
    }

    private static void EnsureMatchTeams(SqliteConnection connection, SqliteTransaction transaction, long matchId, long homeTeamId, long awayTeamId, int homeScore, int awayScore, long? winnerTeamId)
    {
        EnsureMatchTeam(connection, transaction, matchId, homeTeamId, "Home", homeScore, winnerTeamId == homeTeamId);
        EnsureMatchTeam(connection, transaction, matchId, awayTeamId, "Away", awayScore, winnerTeamId == awayTeamId);

        Execute(
            connection,
            transaction,
            "UPDATE matches SET winner_team_id = $winnerTeamId, win_reason = CASE WHEN $winnerTeamId IS NULL THEN NULL ELSE 'Regular' END WHERE id = $matchId;",
            ("$matchId", matchId),
            ("$winnerTeamId", winnerTeamId));
    }

    private static void EnsureMatchTeam(SqliteConnection connection, SqliteTransaction transaction, long matchId, long teamId, string side, int score, bool isWinner)
    {
        var existingId = ExecuteScalarNullableLong(connection, transaction, "SELECT id FROM match_teams WHERE match_id = $matchId AND side = $side LIMIT 1;", ("$matchId", matchId), ("$side", side));
        if (existingId is not null)
        {
            Execute(
                connection,
                transaction,
                "UPDATE match_teams SET team_id = $teamId, score = $score, is_winner = $isWinner WHERE id = $id;",
                ("$id", existingId.Value),
                ("$teamId", teamId),
                ("$score", score),
                ("$isWinner", isWinner ? 1 : 0));
            return;
        }

        Execute(
            connection,
            transaction,
            "INSERT INTO match_teams (match_id, team_id, side, score, is_winner) VALUES ($matchId, $teamId, $side, $score, $isWinner);",
            ("$matchId", matchId),
            ("$teamId", teamId),
            ("$side", side),
            ("$score", score),
            ("$isWinner", isWinner ? 1 : 0));
    }

    private static void EnsureMatchPlayers(SqliteConnection connection, SqliteTransaction transaction, long matchId, long teamId, IReadOnlyList<long> playerIds)
    {
        for (var i = 0; i < playerIds.Count; i++)
        {
            var playerId = playerIds[i];
            if (ExecuteScalarNullableLong(connection, transaction, "SELECT id FROM match_players WHERE match_id = $matchId AND player_id = $playerId LIMIT 1;", ("$matchId", matchId), ("$playerId", playerId)) is not null)
            {
                continue;
            }

            var jerseyNumber = ExecuteScalarNullableLong(connection, transaction, "SELECT jersey_number FROM team_rosters WHERE team_id = $teamId AND player_id = $playerId LIMIT 1;", ("$teamId", teamId), ("$playerId", playerId));
            Execute(
                connection,
                transaction,
                """
                INSERT INTO match_players
                    (match_id, team_id, player_id, jersey_number, is_starting_five, is_on_court, points, personal_fouls)
                VALUES
                    ($matchId, $teamId, $playerId, $jerseyNumber, $isStartingFive, $isOnCourt, $points, $personalFouls);
                """,
                ("$matchId", matchId),
                ("$teamId", teamId),
                ("$playerId", playerId),
                ("$jerseyNumber", jerseyNumber),
                ("$isStartingFive", i < 5 ? 1 : 0),
                ("$isOnCourt", i < 5 ? 1 : 0),
                ("$points", i < 5 ? 4 + i : i),
                ("$personalFouls", i % 3));
        }
    }

    private static void EnsureScoreboardState(SqliteConnection connection, SqliteTransaction transaction, long matchId, int homeScore, int awayScore)
    {
        if (ExecuteScalarNullableLong(connection, transaction, "SELECT id FROM scoreboard_states WHERE match_id = $matchId LIMIT 1;", ("$matchId", matchId)) is not null)
        {
            Execute(
                connection,
                transaction,
                "UPDATE scoreboard_states SET home_score = $homeScore, away_score = $awayScore, game_clock_ms_remaining = 720000, shot_clock_ms_remaining = 24000, last_updated_at = CURRENT_TIMESTAMP WHERE match_id = $matchId;",
                ("$matchId", matchId),
                ("$homeScore", homeScore),
                ("$awayScore", awayScore));
            return;
        }

        Execute(
            connection,
            transaction,
            "INSERT INTO scoreboard_states (match_id, current_period, current_period_type, game_clock_ms_remaining, shot_clock_ms_remaining, home_score, away_score) VALUES ($matchId, 1, 'Regular', 720000, 24000, $homeScore, $awayScore);",
            ("$matchId", matchId),
            ("$homeScore", homeScore),
            ("$awayScore", awayScore));
    }

    private static void EnsureScoreEvent(SqliteConnection connection, SqliteTransaction transaction, long matchId, long teamId, long playerId, int points, string description)
    {
        if (ExecuteScalarNullableLong(connection, transaction, "SELECT id FROM match_events WHERE match_id = $matchId AND team_id = $teamId AND player_id = $playerId AND event_type = 'Score' AND points = $points LIMIT 1;", ("$matchId", matchId), ("$teamId", teamId), ("$playerId", playerId), ("$points", points)) is not null)
        {
            return;
        }

        Execute(
            connection,
            transaction,
            "INSERT INTO match_events (match_id, period, period_type, game_clock_ms_remaining, shot_clock_ms_remaining, team_id, player_id, event_type, points, description) VALUES ($matchId, 1, 'Regular', 480000, 18000, $teamId, $playerId, 'Score', $points, $description);",
            ("$matchId", matchId),
            ("$teamId", teamId),
            ("$playerId", playerId),
            ("$points", points),
            ("$description", description));
    }

    private static long GetOrCreateCompetitionEvent(SqliteConnection connection, SqliteTransaction transaction, long editionId, string name, string status)
    {
        var existingId = ExecuteScalarNullableLong(connection, transaction, "SELECT id FROM competition_events WHERE edition_id = $editionId AND event_type = 'ThreePointContest' AND name = $name LIMIT 1;", ("$editionId", editionId), ("$name", name));
        if (existingId is not null)
        {
            return existingId.Value;
        }

        return ExecuteScalarLong(
            connection,
            transaction,
            "INSERT INTO competition_events (edition_id, event_type, name, status) VALUES ($editionId, 'ThreePointContest', $name, $status); SELECT last_insert_rowid();",
            ("$editionId", editionId),
            ("$name", name),
            ("$status", status));
    }

    private static long EnsureThreePointEntry(SqliteConnection connection, SqliteTransaction transaction, long eventId, long teamId, long playerId, int seedOrder)
    {
        var existingId = ExecuteScalarNullableLong(connection, transaction, "SELECT id FROM three_point_contest_entries WHERE competition_event_id = $eventId AND team_id = $teamId LIMIT 1;", ("$eventId", eventId), ("$teamId", teamId));
        if (existingId is not null)
        {
            Execute(connection, transaction, "UPDATE three_point_contest_entries SET player_id = $playerId, seed_order = $seedOrder WHERE id = $id;", ("$id", existingId.Value), ("$playerId", playerId), ("$seedOrder", seedOrder));
            return existingId.Value;
        }

        return ExecuteScalarLong(
            connection,
            transaction,
            "INSERT INTO three_point_contest_entries (competition_event_id, team_id, player_id, seed_order) VALUES ($eventId, $teamId, $playerId, $seedOrder); SELECT last_insert_rowid();",
            ("$eventId", eventId),
            ("$teamId", teamId),
            ("$playerId", playerId),
            ("$seedOrder", seedOrder));
    }

    private static void EnsureThreePointRound(SqliteConnection connection, SqliteTransaction transaction, long entryId, int roundNumber, string roundType, int station1, int station2, int station3, int station4, int station5)
    {
        var total = station1 + station2 + station3 + station4 + station5;
        if (ExecuteScalarNullableLong(connection, transaction, "SELECT id FROM three_point_contest_rounds WHERE entry_id = $entryId AND round_number = $roundNumber AND round_type = $roundType LIMIT 1;", ("$entryId", entryId), ("$roundNumber", roundNumber), ("$roundType", roundType)) is null)
        {
            Execute(
                connection,
                transaction,
                "INSERT INTO three_point_contest_rounds (entry_id, round_number, round_type, station1_score, station2_score, station3_score, station4_score, station5_score, total_score) VALUES ($entryId, $roundNumber, $roundType, $station1, $station2, $station3, $station4, $station5, $total);",
                ("$entryId", entryId),
                ("$roundNumber", roundNumber),
                ("$roundType", roundType),
                ("$station1", station1),
                ("$station2", station2),
                ("$station3", station3),
                ("$station4", station4),
                ("$station5", station5),
                ("$total", total));
        }

        Execute(connection, transaction, "UPDATE three_point_contest_entries SET total_score = (SELECT COALESCE(SUM(total_score), 0) FROM three_point_contest_rounds WHERE entry_id = $entryId) WHERE id = $entryId;", ("$entryId", entryId));
    }

    private static void ApplyMigrations()
    {
        using var connection = new SqliteConnection($"Data Source={AppPaths.DatabasePath}");
        connection.Open();

        AddColumnIfMissing(connection, "players", "fiscal_code", "TEXT NULL");
        AddColumnIfMissing(connection, "players", "address", "TEXT NULL");
        AddColumnIfMissing(connection, "players", "phone_number", "TEXT NULL");
        AddColumnIfMissing(connection, "players", "email", "TEXT NULL");
        AddColumnIfMissing(connection, "editions", "is_console_active", "INTEGER NOT NULL DEFAULT 0 CHECK (is_console_active IN (0, 1))");
        CreateUniqueIndexIfMissing(connection, "ux_editions_console_active", "CREATE UNIQUE INDEX ux_editions_console_active ON editions(is_console_active) WHERE is_console_active = 1;");
        CreateSponsorsTableIfMissing(connection);
        CreateMerchandiseItemsTableIfMissing(connection);
    }

    private static void CreateUniqueIndexIfMissing(SqliteConnection connection, string indexName, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql.Replace("CREATE UNIQUE INDEX ", "CREATE UNIQUE INDEX IF NOT EXISTS ");
        command.ExecuteNonQuery();
    }

    private static void CreateSponsorsTableIfMissing(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS sponsors (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                description TEXT NULL,
                image_path TEXT NULL,
                is_active INTEGER NOT NULL DEFAULT 1 CHECK (is_active IN (0, 1)),
                sort_order INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """;
        command.ExecuteNonQuery();
    }


    private static void CreateMerchandiseItemsTableIfMissing(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS merchandise_items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                description TEXT NULL,
                price REAL NULL,
                image_path TEXT NULL,
                is_active INTEGER NOT NULL DEFAULT 1 CHECK (is_active IN (0, 1)),
                sort_order INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """;
        command.ExecuteNonQuery();
    }
    private static void AddColumnIfMissing(SqliteConnection connection, string tableName, string columnName, string definition)
    {
        using var check = connection.CreateCommand();
        check.CommandText = $"PRAGMA table_info({tableName});";

        using var reader = check.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};";
        alter.ExecuteNonQuery();
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        AddParameters(command, parameters);
        command.ExecuteNonQuery();
    }

    private static long ExecuteScalarLong(SqliteConnection connection, SqliteTransaction transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        AddParameters(command, parameters);
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static long? ExecuteScalarNullableLong(SqliteConnection connection, SqliteTransaction transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        AddParameters(command, parameters);
        var value = command.ExecuteScalar();
        return value is null or DBNull ? null : Convert.ToInt64(value);
    }

    private static void AddParameters(SqliteCommand command, params (string Name, object? Value)[] parameters)
    {
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
        }
    }
}
