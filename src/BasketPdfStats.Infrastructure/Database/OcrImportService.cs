using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Infrastructure.Configuration;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Infrastructure.Database;

public sealed class OcrImportService : IOcrImportService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public OcrImportService(OcrApiOptions options)
    {
        _baseUrl = options.BaseUrl.TrimEnd('/');
        _http = new HttpClient();
        if (!string.IsNullOrWhiteSpace(options.Token))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.Token);
        }
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    // Costruttore testabile: HttpClient iniettato (es. con un handler fake nei test).
    public OcrImportService(HttpClient httpClient, string baseUrl)
    {
        _http = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<OcrMatchLookupResult> GetMatchesForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var dateText = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var url = $"{_baseUrl}/matches/today?date={Uri.EscapeDataString(dateText)}";

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = TryParseError(body) ?? $"HTTP {(int)response.StatusCode}";
                return new OcrMatchLookupResult { Success = false, ErrorMessage = errorMsg };
            }

            var parsed = JsonSerializer.Deserialize<OcrMatchLookupResult>(body, JsonDefaults.Options);
            if (parsed is null)
            {
                return new OcrMatchLookupResult { Success = false, ErrorMessage = "Risposta non parsabile" };
            }

            parsed.Matches ??= [];
            if (!parsed.Success)
            {
                parsed.ErrorMessage ??= "Lookup partite fallito lato server";
            }

            return parsed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellazione volontaria del chiamante (cambio rapido data): propaga, nessun errore.
            throw;
        }
        catch (OperationCanceledException)
        {
            // Timeout interno di HttpClient: il token del chiamante non è stato cancellato.
            return new OcrMatchLookupResult { Success = false, ErrorMessage = "Timeout della richiesta" };
        }
        catch (Exception ex)
        {
            return new OcrMatchLookupResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<OcrMatchContextResult> GetMatchContextAsync(int matchId, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/matches/{matchId}/context";

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = TryParseError(body) ?? $"HTTP {(int)response.StatusCode}";
                return new OcrMatchContextResult { Success = false, ErrorMessage = errorMsg };
            }

            var context = JsonSerializer.Deserialize<OcrMatchContext>(body, JsonDefaults.Options);
            if (context is null)
            {
                return new OcrMatchContextResult { Success = false, ErrorMessage = "Risposta non parsabile" };
            }

            // Difesa lato client: un roster vuoto non deve mai entrare nel flusso.
            if (context.HomeTeam.Players.Count == 0 || context.AwayTeam.Players.Count == 0)
            {
                return new OcrMatchContextResult { Success = false, ErrorMessage = "Roster incompleto per la partita." };
            }

            return new OcrMatchContextResult { Success = true, Context = context };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return new OcrMatchContextResult { Success = false, ErrorMessage = "Timeout della richiesta" };
        }
        catch (Exception ex)
        {
            return new OcrMatchContextResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<OcrImportResult> ImportAsync(
        int matchId,
        ImportPayload payload,
        bool allowTeamMismatch = false,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/import/{matchId}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload, options: JsonDefaults.Options)
            };
            if (allowTeamMismatch)
            {
                request.Headers.TryAddWithoutValidation("X-Allow-Team-Mismatch", "1");
            }

            var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = TryParseError(body) ?? $"HTTP {(int)response.StatusCode}";
                return new OcrImportResult { Success = false, MatchId = matchId, ErrorMessage = errorMsg };
            }

            var parsed = JsonSerializer.Deserialize<PhpImportResponse>(body, JsonDefaults.Options);
            if (parsed is null)
            {
                return new OcrImportResult { Success = false, MatchId = matchId, ErrorMessage = "Risposta non parsabile" };
            }

            return new OcrImportResult
            {
                Success         = parsed.Success,
                MatchId         = parsed.MatchId,
                ImportedPlayers = parsed.ImportedPlayers,
                Warnings        = parsed.Warnings ?? [],
                ErrorMessage    = parsed.Success ? null : "Import fallito lato server",
            };
        }
        catch (TaskCanceledException)
        {
            return new OcrImportResult { Success = false, MatchId = matchId, ErrorMessage = "Timeout della richiesta" };
        }
        catch (Exception ex)
        {
            return new OcrImportResult { Success = false, MatchId = matchId, ErrorMessage = ex.Message };
        }
    }

    private static string? TryParseError(string body)
    {
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err))
                return err.GetString();
        }
        catch { }
        return null;
    }

    private sealed class PhpImportResponse
    {
        public bool Success { get; set; }
        public int MatchId { get; set; }
        public int ImportedPlayers { get; set; }
        public List<string>? Warnings { get; set; }
    }
}
