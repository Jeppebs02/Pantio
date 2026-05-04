using Microsoft.Extensions.Logging;
using PantioClassLibrary.Interfaces.Services;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Repository;
using PantioRepository.Mapper;

namespace PantioAPI.Services;

public class InventoryItemService(IInventoryItemRepository repository, ILogger<InventoryItemService> logger) : IInventoryItemService
{
    public async Task<InventoryItemDto> CreateAsync(Guid inventoryId, CreateInventoryItemDto dto, CancellationToken ct = default)
    {
        var entity = InventoryItemMapper.ToEntity(inventoryId, dto);
        var created = await repository.CreateAsync(entity, ct);
        logger.LogInformation("Inventory item {ItemId} created in inventory {InventoryId}", created.Id, inventoryId);
        return InventoryItemMapper.ToDto(created);
    }

    public async Task<IEnumerable<InventoryItemDto>> GetByInventoryIdAsync(Guid inventoryId, CancellationToken ct = default)
    {
        logger.LogDebug("Fetching items for inventory {InventoryId}", inventoryId);
        var items = await repository.GetByInventoryIdAsync(inventoryId, ct);
        return items.Select(InventoryItemMapper.ToDto);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        if (deleted)
            logger.LogInformation("Inventory item {ItemId} deleted", id);
        else
            logger.LogWarning("Delete requested for non-existent inventory item {ItemId}", id);
        return deleted;
    }
}
