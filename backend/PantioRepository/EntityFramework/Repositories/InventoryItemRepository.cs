using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.DTO;
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

    public async Task<InventoryItem?> UpdateAsync(Guid id, UpdateInventoryItemDto dto, CancellationToken ct = default)
    {
        var item = await db.InventoryItems.FindAsync([id], ct);
        if (item is null) return null;

        // Tell EF what the client believes the current row_version is.
        // If a concurrent write has already bumped it, SaveChanges throws DbUpdateConcurrencyException.
        db.Entry(item).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;

        item.ProductName = dto.ProductName;
        item.Quantity = dto.Quantity;
        item.QuantityUnit = dto.QuantityUnit;
        item.StorageLocation = dto.StorageLocation;
        item.Status = dto.Status;
        item.RowVersion = dto.RowVersion + 1;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return item;
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
