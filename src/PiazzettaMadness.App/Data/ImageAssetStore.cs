using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace PiazzettaMadness.App.Data;

public static class ImageAssetStore
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".bmp"
    };

    private static readonly Regex UnsafeFileChars = new(@"[^a-zA-Z0-9._-]+", RegexOptions.Compiled);

    public static string Import(string sourcePath, string category)
    {
        if (!File.Exists(sourcePath) || !ImageExtensions.Contains(Path.GetExtension(sourcePath)))
        {
            throw new InvalidOperationException("Il file selezionato non e una immagine valida.");
        }

        var relativeDirectory = Path.Combine("assets", "images", SanitizeSegment(category));
        var targetDirectory = Path.Combine(AppPaths.AppDataDirectory, relativeDirectory);
        Directory.CreateDirectory(targetDirectory);

        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var fileName = $"{SanitizeSegment(baseName)}-{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
        var targetPath = Path.Combine(targetDirectory, fileName);
        File.Copy(sourcePath, targetPath, overwrite: false);

        return ToStoredPath(Path.Combine(relativeDirectory, fileName));
    }

    public static bool IsValidOptionalImagePath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return true;
        }

        var resolvedPath = ResolvePath(storedPath);
        return resolvedPath is not null
            && File.Exists(resolvedPath)
            && ImageExtensions.Contains(Path.GetExtension(resolvedPath));
    }

    public static string? ResolvePath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        return Path.IsPathRooted(storedPath)
            ? storedPath
            : Path.Combine(AppPaths.AppDataDirectory, storedPath.Replace('/', Path.DirectorySeparatorChar));
    }

    public static BitmapImage? CreateImageSource(string? storedPath)
    {
        var resolvedPath = ResolvePath(storedPath);
        if (resolvedPath is null || !IsValidOptionalImagePath(storedPath))
        {
            return null;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(resolvedPath);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private static string SanitizeSegment(string value)
    {
        var sanitized = UnsafeFileChars.Replace(value.Trim(), "-").Trim('-', '.');
        return string.IsNullOrWhiteSpace(sanitized) ? "image" : sanitized.ToLowerInvariant();
    }

    private static string ToStoredPath(string path) => path.Replace(Path.DirectorySeparatorChar, '/');
}
