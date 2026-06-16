using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PiazzettaMadness.App.Data;

public sealed class OnlineTournamentClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public OnlineTournamentClient(OnlineApiOptions options)
    {
        _baseUrl = options.BaseUrl;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
    }

    public static OnlineTournamentClient? TryCreate()
    {
        var options = OnlineApiOptions.Load();
        return options is null ? null : new OnlineTournamentClient(options);
    }

    public async Task<List<Tournament>> GetAllAsync()
    {
        var response = await _httpClient.GetAsync(_baseUrl).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);

        var rows = await response.Content.ReadFromJsonAsync<List<TournamentDto>>().ConfigureAwait(false) ?? [];
        return rows.Select(ToEntity).ToList();
    }

    public async Task<Tournament> CreateAsync(Tournament tournament)
    {
        var response = await _httpClient.PostAsJsonAsync(_baseUrl, ToPayload(tournament)).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        return await ReadTournamentAsync(response).ConfigureAwait(false);
    }

    public async Task<Tournament> UpdateAsync(Tournament tournament)
    {
        var response = await _httpClient.PutAsJsonAsync(WithId(tournament.Id), ToPayload(tournament)).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        return await ReadTournamentAsync(response).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id)
    {
        var response = await _httpClient.DeleteAsync(WithId(id)).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
    }

    private string WithId(int id)
    {
        var separator = _baseUrl.Contains('?') ? '&' : '?';
        return $"{_baseUrl}{separator}id={id}";
    }

    private static TournamentPayload ToPayload(Tournament tournament) =>
        new(tournament.Name, tournament.Description);

    private static Tournament ToEntity(TournamentDto row) =>
        new()
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description,
            CreatedAt = row.CreatedAt ?? "",
            UpdatedAt = row.UpdatedAt ?? ""
        };

    private static async Task<Tournament> ReadTournamentAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("Risposta API vuota.");
        }

        using var document = JsonDocument.Parse(body);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            throw new InvalidOperationException("L'API ha restituito una lista invece del torneo creato/modificato. Ricarica su Aruba la versione aggiornata di /api/tournaments.php.");
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"Risposta API non valida: {body}");
        }

        var row = JsonSerializer.Deserialize<TournamentDto>(document.RootElement.GetRawText());
        return row is null ? throw new InvalidOperationException("Risposta API non valida.") : ToEntity(row);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw new InvalidOperationException($"API tornei non riuscita ({(int)response.StatusCode}): {body}");
    }

    private sealed record TournamentPayload(string Name, string? Description);

    private sealed class TournamentDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }
    }
}
