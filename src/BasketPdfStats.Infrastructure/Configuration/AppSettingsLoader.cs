using System.Text.Json;
using BasketPdfStats.Infrastructure.Serialization;

namespace BasketPdfStats.Infrastructure.Configuration;

public static class AppSettingsLoader
{
    public static AppSettings Load(string? configPath = null)
    {
        var path = configPath ?? Path.Combine(AppContext.BaseDirectory, "Config", "appsettings.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "Config", "appsettings.json");
        }

        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonDefaults.Options) ?? new AppSettings();
        NormalizeRuntimeRoot(settings, path);
        return settings;
    }

    private static void NormalizeRuntimeRoot(AppSettings settings, string configPath)
    {
        if (Path.IsPathRooted(settings.Runtime.RuntimeRoot))
        {
            settings.Runtime.RuntimeRoot = Path.GetFullPath(settings.Runtime.RuntimeRoot);
            return;
        }

        var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath)) ?? Directory.GetCurrentDirectory();
        var projectRoot = Directory.GetParent(configDirectory)?.FullName ?? configDirectory;
        settings.Runtime.RuntimeRoot = settings.Runtime.RuntimeRoot == "."
            ? projectRoot
            : Path.GetFullPath(Path.Combine(projectRoot, settings.Runtime.RuntimeRoot));
    }
}
