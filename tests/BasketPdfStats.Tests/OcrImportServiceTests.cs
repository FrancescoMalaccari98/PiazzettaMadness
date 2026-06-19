using System.Net;
using System.Text;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Infrastructure.Database;

namespace BasketPdfStats.Tests;

public sealed class OcrImportServiceTests
{
    private const string BaseUrl = "https://example.test/api-ocr";

    [Fact]
    public async Task GetMatchesForDateAsync_queries_endpoint_with_iso_date()
    {
        string? requestedUri = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri?.ToString();
            return JsonResponse(HttpStatusCode.OK, """{"success":true,"matches":[]}""");
        });
        var service = new OcrImportService(new HttpClient(handler), BaseUrl);

        await service.GetMatchesForDateAsync(new DateOnly(2026, 7, 8));

        Assert.Equal("https://example.test/api-ocr/matches/today?date=2026-07-08", requestedUri);
    }

    [Fact]
    public async Task GetMatchesForDateAsync_returns_success_with_empty_list()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.OK, """{"success":true,"matches":[]}"""));
        var service = new OcrImportService(new HttpClient(handler), BaseUrl);

        var result = await service.GetMatchesForDateAsync(new DateOnly(2026, 7, 8));

        Assert.True(result.Success);
        Assert.Empty(result.Matches);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task GetMatchesForDateAsync_reports_api_error_on_http_failure()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.InternalServerError, """{"error":"Internal database error"}"""));
        var service = new OcrImportService(new HttpClient(handler), BaseUrl);

        var result = await service.GetMatchesForDateAsync(new DateOnly(2026, 7, 8));

        Assert.False(result.Success);
        Assert.Equal("Internal database error", result.ErrorMessage);
    }

    [Fact]
    public async Task GetMatchesForDateAsync_reports_timeout_when_http_client_cancels_without_caller_token()
    {
        // Simula il timeout interno di HttpClient: TaskCanceledException senza token del chiamante cancellato.
        var handler = new StubHttpMessageHandler(_ => throw new TaskCanceledException());
        var service = new OcrImportService(new HttpClient(handler), BaseUrl);

        var result = await service.GetMatchesForDateAsync(new DateOnly(2026, 7, 8));

        Assert.False(result.Success);
        Assert.Equal("Timeout della richiesta", result.ErrorMessage);
    }

    [Fact]
    public async Task GetMatchesForDateAsync_propagates_cancellation_when_caller_token_cancelled()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.OK, """{"success":true,"matches":[]}"""));
        var service = new OcrImportService(new HttpClient(handler), BaseUrl);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetMatchesForDateAsync(new DateOnly(2026, 7, 8), cts.Token));
    }

    [Fact]
    public async Task GetMatchContextAsync_returns_full_roster_for_known_match()
    {
        string? requestedUri = null;
        const string json = """
        {
          "matchId": 42,
          "homeTeam": { "teamId": 5, "name": "Virtus", "players": [
            { "playerId": 101, "teamId": 5, "firstName": "Mario", "lastName": "Rossi", "jerseyNumber": 3 } ] },
          "awayTeam": { "teamId": 7, "name": "Pall", "players": [
            { "playerId": 201, "teamId": 7, "firstName": "Gino", "lastName": "Verdi", "jerseyNumber": 4 } ] }
        }
        """;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri?.ToString();
            return JsonResponse(HttpStatusCode.OK, json);
        });
        var service = new OcrImportService(new HttpClient(handler), BaseUrl);

        var result = await service.GetMatchContextAsync(42);

        Assert.Equal("https://example.test/api-ocr/matches/42/context", requestedUri);
        Assert.True(result.Success);
        Assert.NotNull(result.Context);
        Assert.Equal(42, result.Context!.MatchId);
        Assert.Equal(5, result.Context.HomeTeam.TeamId);
        Assert.Equal("Rossi", Assert.Single(result.Context.HomeTeam.Players).LastName);
        Assert.Equal(4, Assert.Single(result.Context.AwayTeam.Players).JerseyNumber);
    }

    [Fact]
    public async Task GetMatchContextAsync_reports_error_on_404()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.NotFound, """{"error":"Match not found: 99"}"""));
        var service = new OcrImportService(new HttpClient(handler), BaseUrl);

        var result = await service.GetMatchContextAsync(99);

        Assert.False(result.Success);
        Assert.Null(result.Context);
        Assert.Equal("Match not found: 99", result.ErrorMessage);
    }

    [Fact]
    public async Task GetMatchContextAsync_reports_error_on_empty_roster()
    {
        const string json = """
        {
          "matchId": 42,
          "homeTeam": { "teamId": 5, "name": "Virtus", "players": [] },
          "awayTeam": { "teamId": 7, "name": "Pall", "players": [] }
        }
        """;
        var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, json));
        var service = new OcrImportService(new HttpClient(handler), BaseUrl);

        var result = await service.GetMatchContextAsync(42);

        Assert.False(result.Success);
        Assert.Null(result.Context);
        Assert.Contains("Roster incompleto", result.ErrorMessage!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportAsync_posts_payload_and_returns_success()
    {
        string? requestedUri = null;
        string? sentBody = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri?.ToString();
            sentBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse(HttpStatusCode.OK, """{"success":true,"matchId":42,"importedPlayers":1,"warnings":[]}""");
        });
        var service = new OcrImportService(new HttpClient(handler), BaseUrl);
        var payload = new ImportPayload
        {
            MatchId = 42,
            Players = [new ImportPlayer { EntityId = "player:Home:jersey:5", Side = "Home", PlayerId = 101, TeamId = 5, Number = "5" }],
        };

        var result = await service.ImportAsync(42, payload);

        Assert.Equal("https://example.test/api-ocr/import/42", requestedUri);
        Assert.Contains("\"playerId\": 101", sentBody!, StringComparison.Ordinal);
        Assert.True(result.Success);
        Assert.Equal(1, result.ImportedPlayers);
    }

    [Fact]
    public async Task ImportAsync_reports_error_on_422_invalid_player_ids()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse((HttpStatusCode)422, """{"error":"Invalid canonical player IDs for this match."}"""));
        var service = new OcrImportService(new HttpClient(handler), BaseUrl);

        var result = await service.ImportAsync(42, new ImportPayload { MatchId = 42 });

        Assert.False(result.Success);
        Assert.Equal("Invalid canonical player IDs for this match.", result.ErrorMessage);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) =>
        new(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_responder(request));
        }
    }
}
