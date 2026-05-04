using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;
using PantioRepository.EntityFramework;

namespace PantioRepository.EntityFramework.Repositories;

public class InventoryItemRepository(PantioDbContext db) : IInventoryItemRepository
{
    public async Task<InventoryItem> CreateAsync(InventoryItem item, CancellationToken ct = default)
    {
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<IEnumerable<InventoryItem>> GetByInventoryIdAsync(Guid inventoryId, CancellationToken ct = default)
    {
        return await db.InventoryItems
            .Where(i => i.InventoryId == inventoryId)
            .ToListAsync(ct);
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.InventoryItems.FindAsync([id], ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await db.InventoryItems.FindAsync([id], ct);
        if (item is null) return false;
        db.InventoryItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
