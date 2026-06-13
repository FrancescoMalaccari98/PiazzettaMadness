namespace BasketPdfStats.Infrastructure.FileSystem;

public static class SafeFileMover
{
    public static string UniquePath(string desiredPath)
    {
        if (!File.Exists(desiredPath))
        {
            return desiredPath;
        }

        var directory = Path.GetDirectoryName(desiredPath)!;
        var name = Path.GetFileNameWithoutExtension(desiredPath);
        var extension = Path.GetExtension(desiredPath);
        var index = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(directory, $"{name}_{index}{extension}");
            index++;
        }
        while (File.Exists(candidate));

        return candidate;
    }
}
