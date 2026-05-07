using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;

namespace PantioRepository.EntityFramework.Repositories;

public class ShoppingListRepository(PantioDbContext db) : IShoppingListRepository
{
    public async Task<ShoppingList> CreateAsync(ShoppingList list, CancellationToken ct = default)
    {
        db.ShoppingLists.Add(list);
        await db.SaveChangesAsync(ct);
        return list;
    }

    public async Task<List<ShoppingList>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.ShoppingLists
            .Include(l => l.Items)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ShoppingList?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
    {
        return await db.ShoppingLists
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var list = await db.ShoppingLists.FindAsync([id], ct);
        if (list is null) return false;

        db.ShoppingLists.Remove(list);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ShoppingListItem> AddItemAsync(ShoppingListItem item, CancellationToken ct = default)
    {
        db.ShoppingListItems.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<bool> DeleteItemAsync(Guid itemId, CancellationToken ct = default)
    {
        var item = await db.ShoppingListItems.FindAsync([itemId], ct);
        if (item is null) return false;

        db.ShoppingListItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ShoppingListItem?> ToggleItemAsync(Guid itemId, CancellationToken ct = default)
    {
        var item = await db.ShoppingListItems.FindAsync([itemId], ct);
        if (item is null) return null;

        item.IsChecked = !item.IsChecked;
        await db.SaveChangesAsync(ct);
        return item;
    }
}
