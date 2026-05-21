using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioClassLibrary.Utilities;

namespace PantioAPI.Services;

public class ShoppingListService(
    IShoppingListRepository shoppingListRepository,
    IRecipeRepository recipeRepository,
    IInventoryItemRepository inventoryItemRepository,
    ILogger<ShoppingListService> logger
) : IShoppingListService
{
    public async Task<ShoppingListDto> CreateAsync(Guid userId, CreateShoppingListDto dto, CancellationToken ct = default)
    {
        var list = new ShoppingList
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await shoppingListRepository.CreateAsync(list, ct);
        logger.LogInformation("Created shopping list {ListId} for user {UserId}", list.Id, userId);
        return ToDto(list);
    }

    public async Task<List<ShoppingListDto>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var lists = await shoppingListRepository.GetByUserAsync(userId, ct);
        return lists.Select(ToDto).ToList();
    }

    public async Task<ShoppingListDto?> GetByIdAsync(Guid listId, CancellationToken ct = default)
    {
        var list = await shoppingListRepository.GetByIdWithItemsAsync(listId, ct);
        return list is null ? null : ToDto(list);
    }

    public async Task<bool> DeleteAsync(Guid listId, CancellationToken ct = default)
    {
        return await shoppingListRepository.DeleteAsync(listId, ct);
    }

    public async Task<ShoppingListItemDto> AddItemAsync(Guid listId, AddShoppingListItemDto dto, CancellationToken ct = default)
    {
        var existing = await shoppingListRepository.FindItemByNameAsync(listId, dto.Name, ct);
        if (existing is not null)
        {
            QuantityUnit existingUnit = default, newUnit = default;
            bool bothHaveUnits = existing.MeasuringUnit is not null && dto.MeasuringUnit is not null
                && Enum.TryParse(existing.MeasuringUnit, ignoreCase: true, out existingUnit)
                && Enum.TryParse(dto.MeasuringUnit, ignoreCase: true, out newUnit);

            if (bothHaveUnits && !QuantityUnitConverter.AreSameCategory(existingUnit, newUnit))
            {
                // Incompatible unit categories (e.g. ml vs g) — fall through to create a new item
            }
            else
            {
                var addQty = dto.Quantity ?? 0;
                if (bothHaveUnits)
                    addQty = QuantityUnitConverter.Convert(addQty, newUnit, existingUnit);

                existing.Quantity = (existing.Quantity ?? 0) + addQty;
                await shoppingListRepository.UpdateItemAsync(existing, ct);
                return ToItemDto(existing);
            }
        }

        var item = new ShoppingListItem
        {
            Id = Guid.NewGuid(),
            ShoppingListId = listId,
            Name = dto.Name,
            Quantity = dto.Quantity,
            MeasuringUnit = dto.MeasuringUnit,
            IsChecked = false
        };

        await shoppingListRepository.AddItemAsync(item, ct);
        return ToItemDto(item);
    }

    public async Task<bool> DeleteItemAsync(Guid listId, Guid itemId, CancellationToken ct = default)
    {
        return await shoppingListRepository.DeleteItemAsync(itemId, ct);
    }

    public async Task<ShoppingListItemDto?> ToggleItemAsync(Guid listId, Guid itemId, CancellationToken ct = default)
    {
        var item = await shoppingListRepository.ToggleItemAsync(itemId, ct);
        return item is null ? null : ToItemDto(item);
    }

    public async Task<ShoppingListDto?> CreateFromRecipeAsync(Guid userId, AddFromRecipeDto dto, CancellationToken ct = default)
    {
        var recipe = await recipeRepository.GetByIdWithEntriesAsync(dto.RecipeId, ct);
        if (recipe is null)
        {
            logger.LogWarning("CreateFromRecipe requested for non-existent recipe {RecipeId}", dto.RecipeId);
            return null;
        }

        var inventoryItems = (await inventoryItemRepository.GetByInventoryIdAsync(dto.InventoryId, ct)).ToList();

        var missingEntries = recipe.Entries
            .Where(e => RecipeIngredientMatcher.FindBestMatch(e.ProductName, inventoryItems) is null)
            .ToList();

        var list = new ShoppingList
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name ?? recipe.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = missingEntries.Select(e => new ShoppingListItem
            {
                Id = Guid.NewGuid(),
                Name = e.ProductName,
                Quantity = e.Quantity,
                MeasuringUnit = e.MeasuringUnit,
                IsChecked = false
            }).ToList()
        };

        await shoppingListRepository.CreateAsync(list, ct);
        logger.LogInformation(
            "Created shopping list {ListId} from recipe {RecipeId} with {Count} missing items",
            list.Id, dto.RecipeId, list.Items.Count);

        return ToDto(list);
    }

    private static ShoppingListDto ToDto(ShoppingList list) =>
        new(list.Id, list.UserId, list.Name, list.CreatedAt,
            list.Items.Select(ToItemDto).ToList());

    private static ShoppingListItemDto ToItemDto(ShoppingListItem item) =>
        new(item.Id, item.Name, item.Quantity, item.MeasuringUnit, item.IsChecked);
}
