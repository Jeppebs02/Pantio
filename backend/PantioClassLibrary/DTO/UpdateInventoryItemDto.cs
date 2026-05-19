using PantioClassLibrary.Enums;

namespace PantioClassLibrary.DTO;

public record UpdateInventoryItemDto(
    string ProductName,
    decimal Quantity,
    QuantityUnit? QuantityUnit,
    string? StorageLocation,
    InventoryStatus Status,
    int RowVersion
);
