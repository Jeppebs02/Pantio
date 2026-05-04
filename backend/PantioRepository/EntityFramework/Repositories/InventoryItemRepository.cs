using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces;
using PantioRepository.EntityFramework;

namespace PantioRepository.EntityFramework.Repositories;

public class InventoryItemRepository(PantioDbContext db) : IInventoryItemRepository
{
    public async Task<InventoryItem> CreateAsync(InventoryItem item)
    {
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    public async Task<IEnumerable<InventoryItem>> GetByInventoryIdAsync(Guid inventoryId)
    {
        return await db.InventoryItems
            .Where(i => i.InventoryId == inventoryId)
            .ToListAsync();
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id)
    {
        return await db.InventoryItems.FindAsync(id);
    }
}
