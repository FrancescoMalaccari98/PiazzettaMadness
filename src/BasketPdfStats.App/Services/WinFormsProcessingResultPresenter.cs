using System.Threading;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Presentation;
using Forms = System.Windows.Forms;

namespace BasketPdfStats.App.Services;

public sealed class WinFormsProcessingResultPresenter : IProcessingResultPresenter
{
    private static readonly TimeSpan ViewerStartupTimeout = TimeSpan.FromSeconds(10);

    public void Show(ProcessingResult result)
    {
        Exception? startupException = null;
        using var shown = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            try
            {
                var form = new ResultViewerForm(result);
                form.Shown += (_, _) => shown.Set();
                Forms.Application.Run(form);
            }
            catch (Exception ex)
            {
                startupException = ex;
                shown.Set();
            }
        })
        {
            Name = $"Result viewer: {result.ProcessedFile.FileName}",
            IsBackground = false
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!shown.Wait(ViewerStartupTimeout))
        {
            throw new TimeoutException($"Timed out while opening result viewer for {result.ProcessedFile.FileName}.");
        }

        if (startupException is not null)
        {
            throw new InvalidOperationException($"Could not open result viewer for {result.ProcessedFile.FileName}.", startupException);
        }
    }
}
