namespace PantioClassLibrary.DTO;

public record InventoryItemDto(
    Guid Id,
    Guid InventoryId,
    string ProductName,
    decimal Quantity,
    string? QuantityUnit,
    string? Ean,
    string? StorageLocation,
    string Status,
    string AddedVia,
    DateTime AddedAt,
    DateTime UpdatedAt,
    int? CategoryId,
    NutritionFactsDto? NutritionFacts,
    ExpiryDateDto? ExpiryDate,
    int RowVersion
);
