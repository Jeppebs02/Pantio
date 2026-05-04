using PantioClassLibrary.Entities;

namespace PantioClassLibrary.Interfaces.Repository;

public interface IInventoryItemRepository
{
    Task<InventoryItem> CreateAsync(InventoryItem item, CancellationToken ct = default);
    Task<IEnumerable<InventoryItem>> GetByInventoryIdAsync(Guid inventoryId, CancellationToken ct = default);
    Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
