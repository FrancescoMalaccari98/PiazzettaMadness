using System.Text.Json;

namespace BasketPdfStats.Ocr.TesseractPython;

public sealed class PythonJsonReader
{
    public async Task<JsonDocument> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }
}
