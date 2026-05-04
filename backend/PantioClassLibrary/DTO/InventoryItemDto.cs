namespace PantioClassLibrary.DTO;

public record InventoryItemDto(
    Guid Id,
    Guid InventoryId,
    string ProductName,
    float Quantity,
    string? QuantityUnit,
    string? Ean,
    string? StorageLocation,
    string Status,
    string AddedVia,
    DateTime AddedAt,
    DateTime UpdatedAt
);
