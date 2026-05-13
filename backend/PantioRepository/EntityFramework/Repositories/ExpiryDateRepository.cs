using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;
using PantioRepository.EntityFramework;

namespace PantioRepository.EntityFramework.Repositories;

public class ExpiryDateRepository(PantioDbContext db) : IExpiryDateRepository
{
    public async Task<ExpiryDate?> SetOverrideAsync(Guid inventoryItemId, DateOnly overrideDate, CancellationToken ct = default)
    {
        var expiry = await db.ExpiryDates
            .FirstOrDefaultAsync(e => e.InventoryItemId == inventoryItemId, ct);

        if (expiry is null)
        {
            var itemExists = await db.InventoryItems.AnyAsync(i => i.Id == inventoryItemId, ct);
            if (!itemExists) return null;

            expiry = new ExpiryDate
            {
                Id = Guid.NewGuid(),
                InventoryItemId = inventoryItemId,
                EstimatedExpiry = overrideDate,
                IsManualOverride = true,
                OverrideDate = overrideDate
            };
            db.ExpiryDates.Add(expiry);
        }
        else
        {
            expiry.IsManualOverride = true;
            expiry.OverrideDate = overrideDate;
        }

        await db.SaveChangesAsync(ct);
        return expiry;
    }

    public async Task<IEnumerable<ExpiryDate>> GetExpiringSoonAsync(DateOnly threshold, CancellationToken ct = default)
    {
        return await db.ExpiryDates
            .Where(e => e.NotificationSentAt == null)
            .Where(e => (e.OverrideDate ?? e.EstimatedExpiry) <= threshold)
            .Include(e => e.InventoryItem)
                .ThenInclude(i => i.Inventory)
                    .ThenInclude(inv => inv.User)
            .ToListAsync(ct);
    }
}
