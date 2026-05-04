using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;

namespace PantioRepository.Mapper;

public static class InventoryMapper
{
    public static InventoryDto ToDto(Inventory inventory) => new(
        inventory.Id,
        inventory.UserId,
        inventory.Name,
        inventory.RowVersion
    );

    public static Inventory ToEntity(Guid userId, CreateInventoryDto dto) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = dto.Name
    };
}
