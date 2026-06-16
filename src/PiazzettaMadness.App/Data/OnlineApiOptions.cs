using System.IO;
using System.Text.Json;

namespace PiazzettaMadness.App.Data;

public sealed class OnlineApiOptions
{
    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";

    public string GetEntitiesUrl()
    {
        var uri = new Uri(BaseUrl, UriKind.Absolute);
        var path = uri.AbsolutePath;
        var slashIndex = path.LastIndexOf('/');
        var entitiesPath = slashIndex >= 0
            ? string.Concat(path.AsSpan(0, slashIndex + 1), "entities.php")
            : "/api/entities.php";

        return new UriBuilder(uri)
        {
            Path = entitiesPath,
            Query = ""
        }.Uri.ToString();
    }

    public static OnlineApiOptions? Load()
    {
        foreach (var path in GetCandidatePaths())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var json = File.ReadAllText(path);
            var options = JsonSerializer.Deserialize<OnlineApiOptions>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!string.IsNullOrWhiteSpace(options?.BaseUrl) && !string.IsNullOrWhiteSpace(options.ApiKey))
            {
                return options;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidatePaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "online-api.local.json");
        yield return Path.Combine(Environment.CurrentDirectory, "online-api.local.json");
    }
}
