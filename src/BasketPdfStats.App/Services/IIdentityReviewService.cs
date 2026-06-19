using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Identity;

namespace BasketPdfStats.App.Services;

public interface IIdentityReviewService
{
    /// <summary>
    /// Mostra la UI di revisione manuale delle identità. Ritorna la mappa delle scelte
    /// (entityId OCR → playerId canonico) se l'utente ha risolto i conflitti obbligatori e
    /// confermato; null se ha annullato.
    /// </summary>
    IReadOnlyDictionary<string, int>? ReviewAndConfirm(IReadOnlyList<IdentityReviewItem> items, OcrMatchContext context);
}
