using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Core.Presentation;

public sealed class ProcessingResultPresentationService
{
    private readonly IProcessingResultPresenter? _presenter;

    public ProcessingResultPresentationService(IProcessingResultPresenter? presenter)
    {
        _presenter = presenter;
    }

    public bool TryPresent(ProcessingResult result, Action<string>? statusSink = null)
    {
        if (!ShouldPresent(result))
        {
            return false;
        }

        statusSink?.Invoke($"Processing completed for {result.ProcessedFile.FileName}");
        if (_presenter is null)
        {
            return false;
        }

        statusSink?.Invoke($"Opening WinForms result viewer for {result.ProcessedFile.FileName}");
        try
        {
            _presenter.Show(result);
            statusSink?.Invoke($"WinForms result viewer shown for {result.ProcessedFile.FileName}");
            return true;
        }
        catch (Exception ex)
        {
            statusSink?.Invoke($"Failed to show result viewer: {ex.Message}");
            return false;
        }
    }

    private static bool ShouldPresent(ProcessingResult result) =>
        result.ProcessedFile.Status is FileProcessingStatus.CompletedValidated
            or FileProcessingStatus.CompletedWithWarnings
            or FileProcessingStatus.CompletedNotValidated;
}
