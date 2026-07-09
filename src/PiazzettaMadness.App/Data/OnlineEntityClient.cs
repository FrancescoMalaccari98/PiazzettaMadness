using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PiazzettaMadness.App.Data;

public sealed class OnlineEntityClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new FlexibleBooleanJsonConverter(), new NullableDecimalJsonConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly string _entitiesUrl;

    public OnlineEntityClient(OnlineApiOptions options)
    {
        _entitiesUrl = options.GetEntitiesUrl();
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
    }

    public static OnlineEntityClient? TryCreate()
    {
        var options = OnlineApiOptions.Load();
        return options is null ? null : new OnlineEntityClient(options);
    }

    public Task<List<Tournament>> GetTournamentsAsync() => GetListAsync<Tournament>("tournaments");
    public Task<Tournament> CreateTournamentAsync(Tournament tournament) => CreateAsync("tournaments", tournament);
    public Task<Tournament> UpdateTournamentAsync(Tournament tournament) => UpdateAsync("tournaments", tournament.Id, tournament);
    public Task DeleteTournamentAsync(int id) => DeleteAsync("tournaments", id);

    public Task<List<Edition>> GetEditionsAsync() => GetListAsync<Edition>("editions");
    public Task<Edition> CreateEditionAsync(Edition edition) => CreateAsync("editions", edition);
    public Task<Edition> UpdateEditionAsync(Edition edition) => UpdateAsync("editions", edition.Id, edition);
    public Task DeleteEditionAsync(int id) => DeleteAsync("editions", id);

    public Task<List<Court>> GetCourtsAsync() => GetListAsync<Court>("courts");
    public Task<Court> CreateCourtAsync(Court court) => CreateAsync("courts", court);
    public Task<Court> UpdateCourtAsync(Court court) => UpdateAsync("courts", court.Id, court);
    public Task DeleteCourtAsync(int id) => DeleteAsync("courts", id);

    public Task<List<Sponsor>> GetSponsorsAsync() => GetListAsync<Sponsor>("sponsors");
    public Task<Sponsor> CreateSponsorAsync(Sponsor sponsor) => CreateAsync("sponsors", sponsor);
    public Task<Sponsor> UpdateSponsorAsync(Sponsor sponsor) => UpdateAsync("sponsors", sponsor.Id, sponsor);
    public Task DeleteSponsorAsync(int id) => DeleteAsync("sponsors", id);

    public Task<List<MerchandiseItem>> GetMerchandiseItemsAsync() => GetListAsync<MerchandiseItem>("merchandise_items");
    public Task<MerchandiseItem> CreateMerchandiseItemAsync(MerchandiseItem item) => CreateAsync("merchandise_items", item);
    public Task<MerchandiseItem> UpdateMerchandiseItemAsync(MerchandiseItem item) => UpdateAsync("merchandise_items", item.Id, item);
    public Task DeleteMerchandiseItemAsync(int id) => DeleteAsync("merchandise_items", id);

    public Task<List<Team>> GetTeamsAsync() => GetListAsync<Team>("teams");
    public Task<Team> CreateTeamAsync(Team team) => CreateAsync("teams", team);
    public Task<Team> UpdateTeamAsync(Team team) => UpdateAsync("teams", team.Id, team);
    public Task DeleteTeamAsync(int id) => DeleteAsync("teams", id);

    public Task<List<TeamRoster>> GetTeamRostersAsync() => GetListAsync<TeamRoster>("team_rosters");
    public Task<TeamRoster> CreateTeamRosterAsync(TeamRoster roster) => CreateAsync("team_rosters", roster);
    public Task<TeamRoster> UpdateTeamRosterAsync(TeamRoster roster) => UpdateAsync("team_rosters", roster.Id, roster);
    public Task DeleteTeamRosterAsync(int id) => DeleteAsync("team_rosters", id);

    public Task<List<TournamentGroup>> GetTournamentGroupsAsync() => GetListAsync<TournamentGroup>("tournament_groups");
    public Task<TournamentGroup> CreateTournamentGroupAsync(TournamentGroup group) => CreateAsync("tournament_groups", group);
    public Task<TournamentGroup> UpdateTournamentGroupAsync(TournamentGroup group) => UpdateAsync("tournament_groups", group.Id, group);
    public Task DeleteTournamentGroupAsync(int id) => DeleteAsync("tournament_groups", id);

    public Task<List<GroupTeam>> GetGroupTeamsAsync() => GetListAsync<GroupTeam>("group_teams");
    public Task<GroupTeam> CreateGroupTeamAsync(GroupTeam groupTeam) => CreateAsync("group_teams", groupTeam);
    public Task<GroupTeam> UpdateGroupTeamAsync(GroupTeam groupTeam) => UpdateAsync("group_teams", groupTeam.Id, groupTeam);
    public Task DeleteGroupTeamAsync(int id) => DeleteAsync("group_teams", id);

    public Task<List<Match>> GetMatchesAsync() => GetListAsync<Match>("matches");
    public Task<Match> CreateMatchAsync(Match match) => CreateAsync("matches", match);
    public Task<Match> UpdateMatchAsync(Match match) => UpdateAsync("matches", match.Id, match);
    public Task DeleteMatchAsync(int id) => DeleteAsync("matches", id);

    public Task<List<MatchTeam>> GetMatchTeamsAsync() => GetListAsync<MatchTeam>("match_teams");
    public Task<MatchTeam> CreateMatchTeamAsync(MatchTeam matchTeam) => CreateAsync("match_teams", matchTeam);
    public Task<MatchTeam> UpdateMatchTeamAsync(MatchTeam matchTeam) => UpdateAsync("match_teams", matchTeam.Id, matchTeam);
    public Task DeleteMatchTeamAsync(int id) => DeleteAsync("match_teams", id);

    public Task<List<MatchPlayer>> GetMatchPlayersAsync() => GetListAsync<MatchPlayer>("match_players");
    public Task<MatchPlayer> CreateMatchPlayerAsync(MatchPlayer matchPlayer) => CreateAsync("match_players", matchPlayer);
    public Task<MatchPlayer> UpdateMatchPlayerAsync(MatchPlayer matchPlayer) => UpdateAsync("match_players", matchPlayer.Id, matchPlayer);
    public Task DeleteMatchPlayerAsync(int id) => DeleteAsync("match_players", id);

    public async Task<List<MatchPlayer>> InitializeMatchPlayersAsync(int matchId)
    {
        var response = await _httpClient.PostAsync($"{WithAction("initialize_match_players")}&id={matchId}", null).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<List<MatchPlayer>>(JsonOptions).ConfigureAwait(false) ?? [];
    }

    public async Task SyncLiveAsync(
        Match match,
        IReadOnlyCollection<MatchTeam> matchTeams,
        IReadOnlyCollection<MatchPlayer> matchPlayers,
        ScoreboardStateRecord scoreboardState,
        IReadOnlyCollection<MatchEvent> matchEvents)
    {
        var response = await _httpClient.PostAsJsonAsync(
            WithAction("sync_live"),
            new
            {
                Match = match,
                MatchTeams = matchTeams,
                MatchPlayers = matchPlayers,
                ScoreboardState = scoreboardState,
                MatchEvents = matchEvents
            },
            JsonOptions).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
    }

    public Task<List<MatchEvent>> GetMatchEventsAsync() => GetListAsync<MatchEvent>("match_events");
    public Task<MatchEvent> CreateMatchEventAsync(MatchEvent matchEvent) => CreateAsync("match_events", matchEvent);

    public Task<List<ScoreboardStateRecord>> GetScoreboardStatesAsync() => GetListAsync<ScoreboardStateRecord>("scoreboard_states");
    public Task<ScoreboardStateRecord> CreateScoreboardStateAsync(ScoreboardStateRecord state) => CreateAsync("scoreboard_states", state);
    public Task<ScoreboardStateRecord> UpdateScoreboardStateAsync(int onlineId, ScoreboardStateRecord state) =>
        UpdateAsync("scoreboard_states", onlineId, state);

    public Task<List<CompetitionEvent>> GetCompetitionEventsAsync() => GetListAsync<CompetitionEvent>("competition_events");
    public Task<CompetitionEvent> CreateCompetitionEventAsync(CompetitionEvent competitionEvent) => CreateAsync("competition_events", competitionEvent);
    public Task<CompetitionEvent> UpdateCompetitionEventAsync(CompetitionEvent competitionEvent) => UpdateAsync("competition_events", competitionEvent.Id, competitionEvent);
    public Task DeleteCompetitionEventAsync(int id) => DeleteAsync("competition_events", id);

    public Task<List<ThreePointContestEntry>> GetThreePointContestEntriesAsync() => GetListAsync<ThreePointContestEntry>("three_point_contest_entries");
    public Task<ThreePointContestEntry> CreateThreePointContestEntryAsync(ThreePointContestEntry entry) => CreateAsync("three_point_contest_entries", entry);
    public Task<ThreePointContestEntry> UpdateThreePointContestEntryAsync(ThreePointContestEntry entry) => UpdateAsync("three_point_contest_entries", entry.Id, entry);
    public Task DeleteThreePointContestEntryAsync(int id) => DeleteAsync("three_point_contest_entries", id);

    public Task<List<ThreePointContestRound>> GetThreePointContestRoundsAsync() => GetListAsync<ThreePointContestRound>("three_point_contest_rounds");
    public Task<ThreePointContestRound> CreateThreePointContestRoundAsync(ThreePointContestRound round) => CreateAsync("three_point_contest_rounds", round);
    public Task<ThreePointContestRound> UpdateThreePointContestRoundAsync(ThreePointContestRound round) => UpdateAsync("three_point_contest_rounds", round.Id, round);
    public Task DeleteThreePointContestRoundAsync(int id) => DeleteAsync("three_point_contest_rounds", id);

    public Task<List<ThreePointContestShot>> GetThreePointContestShotsAsync() => GetListAsync<ThreePointContestShot>("three_point_contest_shots");

    public async Task<List<ThreePointContestShot>> InitializeThreePointContestShotsAsync(int roundId)
    {
        var response = await _httpClient.PostAsync(WithAction("initialize_contest_shots") + "&id=" + roundId, null).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<List<ThreePointContestShot>>(JsonOptions).ConfigureAwait(false) ?? [];
    }

    public async Task<ContestSyncBundle> SyncThreePointContestAsync(
        CompetitionEvent competitionEvent,
        ThreePointContestEntry entry,
        ThreePointContestRound round,
        IReadOnlyCollection<ThreePointContestShot> shots)
    {
        var response = await _httpClient.PutAsJsonAsync(
            WithAction("sync_contest"),
            new { CompetitionEvent = competitionEvent, Entry = entry, Round = round, Shots = shots },
            JsonOptions).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        return await ReadEntityAsync<ContestSyncBundle>(response).ConfigureAwait(false);
    }

    public Task<List<ForfeitResult>> GetForfeitResultsAsync() => GetListAsync<ForfeitResult>("forfeit_results");
    public Task<ForfeitResult> CreateForfeitResultAsync(ForfeitResult forfeit) => CreateAsync("forfeit_results", forfeit);
    public Task<ForfeitResult> UpdateForfeitResultAsync(ForfeitResult forfeit) => UpdateAsync("forfeit_results", forfeit.Id, forfeit);
    public Task DeleteForfeitResultAsync(int id) => DeleteAsync("forfeit_results", id);

    public Task<List<Standing>> GetStandingsAsync() => GetListAsync<Standing>("standings");
    public Task<Standing> CreateStandingAsync(Standing standing) => CreateAsync("standings", standing);
    public Task<Standing> UpdateStandingAsync(Standing standing) => UpdateAsync("standings", standing.Id, standing);
    public Task DeleteStandingAsync(int id) => DeleteAsync("standings", id);

    public Task<List<Player>> GetPlayersAsync() => GetListAsync<Player>("players");
    public Task<Player> CreatePlayerAsync(Player player) => CreateAsync("players", player);
    public Task<Player> UpdatePlayerAsync(Player player) => UpdateAsync("players", player.Id, player);
    public Task DeletePlayerAsync(int id) => DeleteAsync("players", id);

    public async Task<MatchBundle> SaveMatchBundleAsync(Match match, MatchTeam home, MatchTeam away)
    {
        var response = await _httpClient.PostAsJsonAsync(
            WithAction("save_match"),
            new { Match = match, Home = home, Away = away },
            JsonOptions).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        return await ReadEntityAsync<MatchBundle>(response).ConfigureAwait(false);
    }

    public async Task DeleteMatchBundleAsync(int matchId)
    {
        var response = await _httpClient.DeleteAsync($"{WithAction("delete_match")}&id={matchId}").ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
    }

    public async Task<ForfeitBundle> SaveForfeitBundleAsync(ForfeitResult forfeit, Match match, MatchTeam home, MatchTeam away)
    {
        var response = await _httpClient.PostAsJsonAsync(
            WithAction("save_forfeit"),
            new { Forfeit = forfeit, Match = match, Home = home, Away = away },
            JsonOptions).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        return await ReadEntityAsync<ForfeitBundle>(response).ConfigureAwait(false);
    }

    public async Task<ForfeitBundle> DeleteForfeitBundleAsync(int forfeitId)
    {
        var response = await _httpClient.DeleteAsync($"{WithAction("delete_forfeit")}&id={forfeitId}").ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        return await ReadEntityAsync<ForfeitBundle>(response).ConfigureAwait(false);
    }

    public async Task<List<Standing>> ReplaceStandingsAsync(IReadOnlyCollection<Standing> standings)
    {
        var response = await _httpClient.PutAsJsonAsync(
            WithAction("replace_standings"),
            new { Standings = standings },
            JsonOptions).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<List<Standing>>(JsonOptions).ConfigureAwait(false) ?? [];
    }

    private async Task<List<T>> GetListAsync<T>(string table)
    {
        var response = await _httpClient.GetAsync(WithTable(table)).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<List<T>>(JsonOptions).ConfigureAwait(false) ?? [];
    }

    private async Task<T> CreateAsync<T>(string table, T entity)
    {
        var response = await _httpClient.PostAsJsonAsync(WithTable(table), entity, JsonOptions).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        return await ReadEntityAsync<T>(response).ConfigureAwait(false);
    }

    private async Task<T> UpdateAsync<T>(string table, int id, T entity)
    {
        var response = await _httpClient.PutAsJsonAsync(WithTableAndId(table, id), entity, JsonOptions).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        return await ReadEntityAsync<T>(response).ConfigureAwait(false);
    }

    private async Task DeleteAsync(string table, int id)
    {
        var response = await _httpClient.DeleteAsync(WithTableAndId(table, id)).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
    }

    private string WithTable(string table)
    {
        var separator = _entitiesUrl.Contains('?') ? '&' : '?';
        return $"{_entitiesUrl}{separator}table={Uri.EscapeDataString(table)}";
    }

    private string WithTableAndId(string table, int id) => $"{WithTable(table)}&id={id}";

    private string WithAction(string action)
    {
        var separator = _entitiesUrl.Contains('?') ? '&' : '?';
        return $"{_entitiesUrl}{separator}action={Uri.EscapeDataString(action)}";
    }

    private static async Task<T> ReadEntityAsync<T>(HttpResponseMessage response)
    {
        var entity = await response.Content.ReadFromJsonAsync<T>(JsonOptions).ConfigureAwait(false);
        return entity is null ? throw new InvalidOperationException("Risposta API non valida.") : entity;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw new OnlineApiException((int)response.StatusCode, body);
    }

    public sealed class OnlineApiException(int statusCode, string responseBody)
        : InvalidOperationException(BuildApiErrorMessage(statusCode, responseBody))
    {
        public int StatusCode { get; } = statusCode;
        public string ResponseBody { get; } = responseBody;

        private static string BuildApiErrorMessage(int statusCode, string responseBody)
        {
            if (statusCode == 429)
            {
                return "Troppe richieste al server. Riprova tra qualche secondo.";
            }

            var body = responseBody.Trim();
            if (body.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                body.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            {
                return $"API online non disponibile (HTTP {statusCode}).";
            }

            const int maxLength = 500;
            if (body.Length > maxLength)
            {
                body = body[..maxLength] + "...";
            }

            return $"API CRUD online non riuscita ({statusCode}): {body}";
        }
    }

    public sealed class MatchBundle
    {
        public Match Match { get; set; } = new();
        public MatchTeam Home { get; set; } = new();
        public MatchTeam Away { get; set; } = new();
    }

    public sealed class ContestSyncBundle
    {
        public CompetitionEvent CompetitionEvent { get; set; } = new();
        public ThreePointContestEntry Entry { get; set; } = new();
        public ThreePointContestRound Round { get; set; } = new();
        public List<ThreePointContestShot> Shots { get; set; } = [];
    }

    public sealed class ForfeitBundle
    {
        public ForfeitResult? Forfeit { get; set; }
        public Match Match { get; set; } = new();
        public MatchTeam Home { get; set; } = new();
        public MatchTeam Away { get; set; } = new();
    }
}
