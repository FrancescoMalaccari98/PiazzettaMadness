using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace BasketPdfStats.App.Services;

public sealed class WpfFilePicker : IFilePicker
{
    public string? PickPdf()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona PDF tabellino",
            Filter = "PDF (*.pdf)|*.pdf",
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
