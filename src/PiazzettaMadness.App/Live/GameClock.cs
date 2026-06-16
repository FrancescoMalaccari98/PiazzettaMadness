using System.Diagnostics;

namespace PiazzettaMadness.App.Live;

public sealed class GameClock
{
    private readonly Stopwatch _stopwatch = new();
    private long _remainingAtStartMs;

    public GameClock(int durationMs)
    {
        RemainingMs = durationMs;
        _remainingAtStartMs = durationMs;
    }

    public bool IsRunning { get; private set; }

    public int RemainingMs { get; private set; }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _remainingAtStartMs = RemainingMs;
        _stopwatch.Restart();
        IsRunning = true;
    }

    public void Pause()
    {
        if (!IsRunning)
        {
            return;
        }

        Update();
        _stopwatch.Stop();
        IsRunning = false;
    }

    public void Reset(int durationMs)
    {
        _stopwatch.Reset();
        IsRunning = false;
        RemainingMs = durationMs;
        _remainingAtStartMs = durationMs;
    }

    public int Update()
    {
        if (!IsRunning)
        {
            return RemainingMs;
        }

        var elapsed = _stopwatch.ElapsedMilliseconds;
        RemainingMs = (int)Math.Max(0, _remainingAtStartMs - elapsed);

        if (RemainingMs == 0)
        {
            IsRunning = false;
            _stopwatch.Stop();
        }

        return RemainingMs;
    }
}
