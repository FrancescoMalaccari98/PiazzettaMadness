namespace BasketPdfStats.Core.Enums;

public enum FileProcessingStatus
{
    Pending,
    Processing,
    CompletedValidated,
    CompletedWithWarnings,
    CompletedNotValidated,
    CompletedWithReviewRequired,
    Failed,
    SkippedDuplicate
}
