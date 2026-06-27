using BasketPdfStats.Core.Database;

namespace BasketPdfStats.Core.Models;

public sealed class OcrProcessingRequest
{
    public string PdfPath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string DocumentHash { get; set; } = string.Empty;
    public string WorkingPath { get; set; } = string.Empty;
    public OcrRunPlan? RunPlan { get; set; }
    public DocumentPreparationResult? DocumentPreparation { get; set; }

    /// <summary>
    /// Contesto canonico della partita (squadre + roster dal DB), se caricato.
    /// I motori OCR possono usarlo come supporto al parsing (es. CH1); il matching
    /// canonico delle identità avviene comunque in C#. Null = comportamento legacy.
    /// </summary>
    public OcrMatchContext? MatchContext { get; set; }

    /// <summary>
    /// Impostato dalla pipeline quando l'utente sceglie di interrompere l'elaborazione perché le
    /// squadre del PDF non corrispondono al match selezionato (decisione dopo CH1). Transitorio:
    /// non fa parte del JSON di output.
    /// </summary>
    public bool TeamMismatchAborted { get; set; }
}
