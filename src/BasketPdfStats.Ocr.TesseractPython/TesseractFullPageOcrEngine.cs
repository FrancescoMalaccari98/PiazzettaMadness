using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;

namespace BasketPdfStats.Ocr.TesseractPython;

public sealed class TesseractFullPageOcrEngine : IOcrEngine
{
    private readonly TesseractPythonOcrEngine _inner;

    public TesseractFullPageOcrEngine(TesseractPythonOptions options)
        : this(new TesseractPythonOcrEngine(options))
    {
    }

    public TesseractFullPageOcrEngine(TesseractPythonOcrEngine inner)
    {
        _inner = inner;
    }

    public string EngineName => OcrStrategyNames.TesseractFullPage;

    public Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default) =>
        _inner.ProcessAsync(request, cancellationToken);
}
