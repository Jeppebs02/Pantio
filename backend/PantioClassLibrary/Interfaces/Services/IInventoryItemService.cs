using PantioClassLibrary.DTO;

namespace PantioClassLibrary.Interfaces.Services;

public interface IInventoryItemService
{
    Task<InventoryItemDto> CreateAsync(Guid inventoryId, CreateInventoryItemDto dto, CancellationToken ct = default);
    Task<IEnumerable<InventoryItemDto>> GetByInventoryIdAsync(Guid inventoryId, CancellationToken ct = default);
    Task<InventoryItemDto?> UpdateAsync(Guid id, UpdateInventoryItemDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid inventoryId, Guid id, CancellationToken ct = default);
}
