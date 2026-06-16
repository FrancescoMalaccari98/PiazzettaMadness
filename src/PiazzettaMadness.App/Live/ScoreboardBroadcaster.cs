using System.Text.Json;
using Microsoft.Web.WebView2.Wpf;

namespace PiazzettaMadness.App.Live;

public sealed class ScoreboardBroadcaster
{
    private readonly List<ScoreboardTarget> _targets = [];
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public event EventHandler? TargetsChanged;

    public void Register(int id, string name, WebView2 target)
    {
        if (_targets.All(x => x.Id != id))
        {
            _targets.Add(new ScoreboardTarget(id, name, target));
            TargetsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Unregister(int id)
    {
        var removed = _targets.RemoveAll(x => x.Id == id);
        if (removed > 0)
        {
            TargetsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public IReadOnlyList<ScoreboardDisplayInfo> GetDisplays()
    {
        return _targets
            .Select(x => new ScoreboardDisplayInfo(x.Id, x.Name))
            .ToList();
    }

    public Task BroadcastAsync(ScoreboardState state)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = "scoreboardState",
            sentAtUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            state
        }, _jsonOptions);

        foreach (var target in _targets.ToArray())
        {
            if (target.WebView.CoreWebView2 is null)
            {
                continue;
            }

            target.WebView.CoreWebView2.PostWebMessageAsJson(payload);
        }

        return Task.CompletedTask;
    }

    public Task BroadcastContestAsync(ThreePointContestDisplayState state)
    {
        return PostAsync(new
        {
            type = "contestState",
            state
        });
    }

    public Task ShowScoreboardAsync()
    {
        return PostAsync(new
        {
            type = "displayMode",
            mode = "scoreboard"
        });
    }

    public Task ShowSponsorsAsync(IReadOnlyList<SponsorSlide> sponsors, int intervalMs)
    {
        return PostAsync(new
        {
            type = "displayMode",
            mode = "sponsors",
            sponsors,
            sponsorIntervalMs = intervalMs
        });
    }

    public Task ShowPlayerStatsAsync()
    {
        return PostAsync(new
        {
            type = "displayMode",
            mode = "playerStats"
        });
    }

    public Task ShowContestAsync()
    {
        return PostAsync(new
        {
            type = "displayMode",
            mode = "contest"
        });
    }

    public Task ShowThreePointCelebrationAsync(string playerName, int? jerseyNumber, string teamName, string teamColor)
    {
        return PostAsync(new
        {
            type = "threePointCelebration",
            playerName,
            jerseyNumber,
            teamName,
            teamColor
        });
    }

    public Task ShowPlayerCelebrationAsync(
        string celebration,
        string playerName,
        int? jerseyNumber,
        string teamName,
        string teamColor)
    {
        return PostAsync(new
        {
            type = "playerCelebration",
            celebration,
            playerName,
            jerseyNumber,
            teamName,
            teamColor
        });
    }

    private Task PostAsync(object message)
    {
        var payload = JsonSerializer.Serialize(message, _jsonOptions);

        foreach (var target in _targets.ToArray())
        {
            if (target.WebView.CoreWebView2 is null)
            {
                continue;
            }

            target.WebView.CoreWebView2.PostWebMessageAsJson(payload);
        }

        return Task.CompletedTask;
    }

    private sealed record ScoreboardTarget(int Id, string Name, WebView2 WebView);
}

public sealed record ScoreboardDisplayInfo(int Id, string Name);
