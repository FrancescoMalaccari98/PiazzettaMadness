namespace BasketPdfStats.Ocr.TesseractPython;

public sealed class PythonProcessResult
{
    public bool Started { get; init; }
    public bool TimedOut { get; init; }
    public int? ExitCode { get; init; }
    public string Stdout { get; init; } = string.Empty;
    public string Stderr { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
}
