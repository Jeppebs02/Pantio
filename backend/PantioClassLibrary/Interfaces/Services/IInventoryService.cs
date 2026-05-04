using PantioClassLibrary.DTO;

namespace PantioClassLibrary.Interfaces.Services;

public interface IInventoryService
{
    Task<InventoryDto> CreateAsync(Guid userId, CreateInventoryDto dto, CancellationToken ct = default);
    Task<IEnumerable<InventoryDto>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<InventoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<InventoryDto?> UpdateAsync(Guid id, UpdateInventoryDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
