using Microsoft.Extensions.Logging;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioRepository.Mapper;

namespace PantioAPI.Services;

public class InventoryService(IInventoryRepository repository, ILogger<InventoryService> logger) : IInventoryService
{
    public async Task<InventoryDto> CreateAsync(Guid userId, CreateInventoryDto dto, CancellationToken ct = default)
    {
        var entity = InventoryMapper.ToEntity(userId, dto);
        var created = await repository.CreateAsync(entity, ct);
        logger.LogInformation("Inventory {InventoryId} created for user {UserId}", created.Id, userId);
        return InventoryMapper.ToDto(created);
    }

    public async Task<IEnumerable<InventoryDto>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        logger.LogDebug("Fetching inventories for user {UserId}", userId);
        var inventories = await repository.GetByUserIdAsync(userId, ct);
        return inventories.Select(InventoryMapper.ToDto);
    }

    public async Task<InventoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var inventory = await repository.GetByIdAsync(id, ct);
        if (inventory is null)
            logger.LogWarning("Inventory {InventoryId} not found", id);
        return inventory is null ? null : InventoryMapper.ToDto(inventory);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        if (deleted)
            logger.LogInformation("Inventory {InventoryId} deleted", id);
        else
            logger.LogWarning("Delete requested for non-existent inventory {InventoryId}", id);
        return deleted;
    }
}
