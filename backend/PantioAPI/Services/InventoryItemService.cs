using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Exceptions;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioRepository.Mapper;

namespace PantioAPI.Services;

public class InventoryItemService(IInventoryItemRepository repository, ILogger<InventoryItemService> logger) : IInventoryItemService
{
    public async Task<InventoryItemDto> CreateAsync(Guid inventoryId, CreateInventoryItemDto dto, CancellationToken ct = default)
    {
        var entity = InventoryItemMapper.ToEntity(inventoryId, dto);
        // TODO: Fill missing inventoryitem data with OpenFoodFactsService, this service determines wether to use cache or OFF api.
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

    public async Task<InventoryItemDto?> UpdateAsync(Guid id, UpdateInventoryItemDto dto, CancellationToken ct = default)
    {
        try
        {
            var updated = await repository.UpdateAsync(id, dto, ct);
            if (updated is null)
            {
                logger.LogWarning("Update requested for non-existent inventory item {ItemId}", id);
                return null;
            }
            logger.LogInformation("Inventory item {ItemId} updated", id);
            return InventoryItemMapper.ToDto(updated);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict on inventory item {ItemId}", id);
            throw new ConcurrencyConflictException("Varen blev ændret af en anden operation. Hent varen igen og prøv igen.");
        }
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
