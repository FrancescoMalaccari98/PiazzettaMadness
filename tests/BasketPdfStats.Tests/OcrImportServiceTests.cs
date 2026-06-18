using System.Net;
using System.Text;
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
