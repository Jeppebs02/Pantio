using PantioClassLibrary.Entities;

namespace PantioClassLibrary.Interfaces.Repository;

public interface IInventoryRepository
{
    Task<Inventory> CreateAsync(Inventory inventory, CancellationToken ct = default);
    Task<IEnumerable<Inventory>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Inventory?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
