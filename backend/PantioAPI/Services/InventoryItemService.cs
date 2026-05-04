using PantioClassLibrary.Interfaces.Services;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Repository;
using PantioRepository.Mapper;

namespace PantioAPI.Services;

public class InventoryItemService(IInventoryItemRepository repository) : IInventoryItemService
{
    public async Task<InventoryItemDto> CreateAsync(Guid inventoryId, CreateInventoryItemDto dto, CancellationToken ct = default)
    {
        var entity = InventoryItemMapper.ToEntity(inventoryId, dto);
        var created = await repository.CreateAsync(entity, ct);
        return InventoryItemMapper.ToDto(created);
    }

    public async Task<IEnumerable<InventoryItemDto>> GetByInventoryIdAsync(Guid inventoryId, CancellationToken ct = default)
    {
        var items = await repository.GetByInventoryIdAsync(inventoryId, ct);
        return items.Select(InventoryItemMapper.ToDto);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await repository.DeleteAsync(id, ct);
    }
}
