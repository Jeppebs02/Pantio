using PantioClassLibrary.DTO;

namespace PantioClassLibrary.Interfaces.Services;

public interface IInventoryItemCacheService
{
    Task<IEnumerable<InventoryItemDto>?> GetAsync(Guid inventoryId, CancellationToken ct = default);
    Task SetAsync(Guid inventoryId, IEnumerable<InventoryItemDto> items, CancellationToken ct = default);
    Task InvalidateAsync(Guid inventoryId, CancellationToken ct = default);
}
