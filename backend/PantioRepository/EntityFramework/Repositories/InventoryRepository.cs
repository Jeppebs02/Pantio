using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;
using PantioRepository.EntityFramework;

namespace PantioRepository.EntityFramework.Repositories;

public class InventoryRepository(PantioDbContext db) : IInventoryRepository
{
    public async Task<Inventory> CreateAsync(Inventory inventory, CancellationToken ct = default)
    {
        db.Inventories.Add(inventory);
        await db.SaveChangesAsync(ct);
        return inventory;
    }

    public async Task<IEnumerable<Inventory>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.Inventories
            .Where(i => i.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task<Inventory?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Inventories.FindAsync([id], ct);
    }

    public async Task<Inventory?> UpdateAsync(Guid id, UpdateInventoryDto dto, CancellationToken ct = default)
    {
        var inventory = await db.Inventories.FindAsync([id], ct);
        if (inventory is null) return null;

        db.Entry(inventory).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;

        inventory.Name = dto.Name;
        inventory.RowVersion = dto.RowVersion + 1;

        await db.SaveChangesAsync(ct);
        return inventory;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var inventory = await db.Inventories.FindAsync([id], ct);
        if (inventory is null) return false;
        db.Inventories.Remove(inventory);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
