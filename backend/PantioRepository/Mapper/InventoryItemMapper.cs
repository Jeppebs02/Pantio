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
        item.QuantityUnit,
        item.Ean,
        item.StorageLocation,
        item.Status.ToString(),
        item.AddedVia.ToString(),
        item.AddedAt,
        item.UpdatedAt
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
        Status = InventoryStatus.Available,
        AddedVia = dto.AddedVia,
        AddedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
