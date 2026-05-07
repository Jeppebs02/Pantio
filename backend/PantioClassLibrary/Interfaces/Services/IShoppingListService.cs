using PantioClassLibrary.DTO;

namespace PantioClassLibrary.Interfaces.Services;

public interface IShoppingListService
{
    Task<ShoppingListDto> CreateAsync(Guid userId, CreateShoppingListDto dto, CancellationToken ct = default);
    Task<List<ShoppingListDto>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<ShoppingListDto?> GetByIdAsync(Guid listId, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid listId, CancellationToken ct = default);
    Task<ShoppingListItemDto> AddItemAsync(Guid listId, AddShoppingListItemDto dto, CancellationToken ct = default);
    Task<bool> DeleteItemAsync(Guid listId, Guid itemId, CancellationToken ct = default);
    Task<ShoppingListItemDto?> ToggleItemAsync(Guid listId, Guid itemId, CancellationToken ct = default);
    Task<ShoppingListDto?> CreateFromRecipeAsync(Guid userId, AddFromRecipeDto dto, CancellationToken ct = default);
}
