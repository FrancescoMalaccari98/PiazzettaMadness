using BasketPdfStats.Infrastructure.Configuration;

namespace BasketPdfStats.Infrastructure.FileSystem;

public static class RuntimeFolderInitializer
{
    public static void EnsureCreated(RuntimeOptions options)
    {
        Directory.CreateDirectory(options.WorkingPath);
        Directory.CreateDirectory(options.OutputJsonPath);
        Directory.CreateDirectory(options.LogsPath);
    }
}
