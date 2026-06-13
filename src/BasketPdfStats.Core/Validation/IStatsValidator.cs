using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Core.Validation;

public interface IStatsValidator
{
    void Validate(ProcessingResult result);
}
