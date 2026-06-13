using BasketPdfStats.Ocr.TesseractPython;

namespace BasketPdfStats.Tests;

public sealed class PythonProcessRunnerTests
{
    [Fact]
    public async Task Captures_stderr_and_exit_code()
    {
        var runner = new PythonProcessRunner();

        var result = await runner.RunAsync(
            "cmd.exe",
            "/c \"echo boom 1>&2 & exit /b 7\"",
            Directory.GetCurrentDirectory(),
            new Dictionary<string, string?>(),
            TimeSpan.FromSeconds(5));

        Assert.True(result.Started);
        Assert.Equal(7, result.ExitCode);
        Assert.Contains("boom", result.Stderr);
    }

    [Fact]
    public async Task Returns_timeout_result()
    {
        var runner = new PythonProcessRunner();

        var result = await runner.RunAsync(
            "cmd.exe",
            "/c \"ping 127.0.0.1 -n 6 > nul\"",
            Directory.GetCurrentDirectory(),
            new Dictionary<string, string?>(),
            TimeSpan.FromMilliseconds(100));

        Assert.True(result.Started);
        Assert.True(result.TimedOut);
    }
}
