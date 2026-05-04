using PantioClassLibrary.DTO;

namespace PantioClassLibrary.Interfaces.Services;

public interface IInventoryItemService
{
    Task<InventoryItemDto> CreateAsync(Guid inventoryId, CreateInventoryItemDto dto);
    Task<IEnumerable<InventoryItemDto>> GetByInventoryIdAsync(Guid inventoryId);
}
