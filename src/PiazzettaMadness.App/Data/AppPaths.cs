using System.IO;

namespace PiazzettaMadness.App.Data;

public static class AppPaths
{
    public static string AppDataDirectory
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PiazzettaMadness");

            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string DatabasePath { get; } = Path.Combine(
        Path.GetTempPath(),
        $"piazzetta-madness-cache-{Environment.ProcessId}.db");

    public static string LiveDatabasePath => Path.Combine(AppDataDirectory, "piazzetta-madness-live.db");

    public static string SchemaPath => Path.Combine(AppContext.BaseDirectory, "Data", "sqlite-schema-v1.sql");

    public static string ScoreboardIndexPath => Path.Combine(AppContext.BaseDirectory, "Scoreboard", "index.html");
}
