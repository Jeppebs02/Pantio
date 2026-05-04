using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces;
using PantioRepository.Mapper;

namespace PantioAPI.Services;

public class InventoryItemService(IInventoryItemRepository repository) : IInventoryItemService
{
    public async Task<InventoryItemDto> CreateAsync(Guid inventoryId, CreateInventoryItemDto dto)
    {
        var entity = InventoryItemMapper.ToEntity(inventoryId, dto);
        var created = await repository.CreateAsync(entity);
        return InventoryItemMapper.ToDto(created);
    }

    public async Task<IEnumerable<InventoryItemDto>> GetByInventoryIdAsync(Guid inventoryId)
    {
        var items = await repository.GetByInventoryIdAsync(inventoryId);
        return items.Select(InventoryItemMapper.ToDto);
    }
}
