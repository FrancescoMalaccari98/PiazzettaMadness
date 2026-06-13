namespace BasketPdfStats.Core.Enums;

public enum FileProcessingStatus
{
    Pending,
    Processing,
    CompletedValidated,
    CompletedWithWarnings,
    CompletedNotValidated,
    Failed,
    SkippedDuplicate
}
