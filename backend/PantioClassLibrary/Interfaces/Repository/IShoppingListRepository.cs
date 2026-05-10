using PantioClassLibrary.Entities;

namespace PantioClassLibrary.Interfaces.Repository;

public interface IShoppingListRepository
{
    Task<ShoppingList> CreateAsync(ShoppingList list, CancellationToken ct = default);
    Task<List<ShoppingList>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<ShoppingList?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ShoppingListItem> AddItemAsync(ShoppingListItem item, CancellationToken ct = default);
    Task<ShoppingListItem?> FindItemByNameAsync(Guid listId, string name, CancellationToken ct = default);
    Task UpdateItemAsync(ShoppingListItem item, CancellationToken ct = default);
    Task<bool> DeleteItemAsync(Guid itemId, CancellationToken ct = default);
    Task<ShoppingListItem?> ToggleItemAsync(Guid itemId, CancellationToken ct = default);
}
