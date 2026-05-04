using PantioClassLibrary.Enums;

namespace PantioClassLibrary.DTO;

public record UpdateInventoryItemDto(
    string ProductName,
    float Quantity,
    string? QuantityUnit,
    string? StorageLocation,
    InventoryStatus Status,
    int RowVersion
);
