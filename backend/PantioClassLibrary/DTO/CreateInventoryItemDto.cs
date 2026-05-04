using PantioClassLibrary.Enums;

namespace PantioClassLibrary.DTO;

public record CreateInventoryItemDto(
    string ProductName,
    float Quantity,
    string? QuantityUnit,
    string? Ean,
    string? StorageLocation,
    AddedVia AddedVia
);
