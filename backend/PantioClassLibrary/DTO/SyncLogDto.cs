namespace PantioClassLibrary.DTO;

public sealed record SyncLogDto(
    Guid Id,
    DateTime SyncedAt,
    string Status,
    int ImportedReceiptCount,
    int ProcessedInventoryCount
);
