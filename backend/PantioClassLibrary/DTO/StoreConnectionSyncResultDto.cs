using PantioClassLibrary.Enums;

namespace PantioClassLibrary.DTO;

public sealed record StoreConnectionSyncResultDto(
    Guid ConnectionId,
    StoreChain Chain,
    StoreConnectionStatus Status,
    DateTime SyncedAt,
    int ImportedReceiptCount,
    int ProcessedInventoryItemCount
);
