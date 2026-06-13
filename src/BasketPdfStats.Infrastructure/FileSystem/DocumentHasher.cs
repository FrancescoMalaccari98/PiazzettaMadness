using System.Security.Cryptography;

namespace BasketPdfStats.Infrastructure.FileSystem;

public static class DocumentHasher
{
    public static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ShortHash(string documentHash)
    {
        var normalized = documentHash.Replace("sha256:", string.Empty, StringComparison.OrdinalIgnoreCase);
        return normalized.Length <= 12 ? normalized : normalized[..12];
    }
}
