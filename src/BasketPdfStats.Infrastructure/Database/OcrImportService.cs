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
    private readonly string _matchLookupDate;

    public OcrImportService(OcrApiOptions options)
    {
        _baseUrl = options.BaseUrl.TrimEnd('/');
        _matchLookupDate = options.MatchLookupDate.Trim();
        _http = new HttpClient();
        if (!string.IsNullOrWhiteSpace(options.Token))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.Token);
        }
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<OcrMatchLookupResult> GetTodayMatchesAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/matches/today";
        if (!string.IsNullOrWhiteSpace(_matchLookupDate))
        {
            url = $"{url}?date={Uri.EscapeDataString(_matchLookupDate)}";
        }

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
        catch (TaskCanceledException)
        {
            return new OcrMatchLookupResult { Success = false, ErrorMessage = "Timeout della richiesta" };
        }
        catch (Exception ex)
        {
            return new OcrMatchLookupResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<OcrImportResult> ImportAsync(
        int matchId,
        ProcessingResult result,
        bool allowTeamMismatch = false,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/import/{matchId}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(result, options: JsonDefaults.Options)
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
