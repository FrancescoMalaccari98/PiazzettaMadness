using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.App.Services;

/// <summary>Correzione manuale di un singolo campo statistico (fieldId univoco → nuovo valore).</summary>
public sealed record StatEdit(string FieldId, object? NewValue);

/// <summary>
/// Risultato della revisione manuale: correzioni statistiche, override identità e
/// eventuale override del punteggio finale.
/// </summary>
public sealed record ManualEditSet(
    IReadOnlyList<StatEdit> StatEdits,
    IReadOnlyDictionary<string, int> IdentityOverrides,
    string? FinalScoreOverride = null);

/// <summary>
/// Servizio che mostra la UI di revisione manuale completa (statistiche + identità).
/// Ritorna il set di correzioni se l'utente ha confermato, null se ha annullato.
/// </summary>
public interface IManualReviewService
{
    ManualEditSet? ReviewAndEdit(ProcessingResult result, OcrMatchContext? context);
}
