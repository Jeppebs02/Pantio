using PantioClassLibrary.Entities;

namespace PantioClassLibrary.Interfaces;

public interface IInventoryItemRepository
{
    Task<InventoryItem> CreateAsync(InventoryItem item);
    Task<IEnumerable<InventoryItem>> GetByInventoryIdAsync(Guid inventoryId);
    Task<InventoryItem?> GetByIdAsync(Guid id);
}
