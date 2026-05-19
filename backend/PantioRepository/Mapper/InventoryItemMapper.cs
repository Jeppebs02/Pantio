using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;

namespace PantioRepository.Mapper;

public static class InventoryItemMapper
{
    public static InventoryItemDto ToDto(InventoryItem item) => new(
        item.Id,
        item.InventoryId,
        item.ProductName,
        item.Quantity,
        item.QuantityUnit?.ToString(),
        item.Ean,
        item.StorageLocation,
        item.Status.ToString(),
        item.AddedVia.ToString(),
        item.AddedAt,
        item.UpdatedAt,
        item.CategoryId,
        item.NutritionFacts is null ? null : new NutritionFactsDto(
            item.NutritionFacts.Id,
            item.NutritionFacts.EnergyKcal100g,
            item.NutritionFacts.Carbohydrates100g,
            item.NutritionFacts.Sugars100g,
            item.NutritionFacts.Fat100g,
            item.NutritionFacts.SaturatedFat100g,
            item.NutritionFacts.Proteins100g,
            item.NutritionFacts.Salt100g,
            item.NutritionFacts.NutritionDataPer
        ),
        item.ExpiryDate is null ? null : ExpiryDateMapper.ToDto(item.ExpiryDate),
        item.RowVersion
    );

    public static InventoryItem ToEntity(Guid inventoryId, CreateInventoryItemDto dto) => new()
    {
        Id = Guid.NewGuid(),
        InventoryId = inventoryId,
        ProductName = dto.ProductName,
        Quantity = dto.Quantity,
        QuantityUnit = dto.QuantityUnit,
        Ean = dto.Ean,
        StorageLocation = dto.StorageLocation,
        CategoryId = dto.CategoryId,
        ReceiptLineId = dto.ReceiptLineId,
        Status = InventoryStatus.Available,
        AddedVia = dto.AddedVia,
        AddedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
