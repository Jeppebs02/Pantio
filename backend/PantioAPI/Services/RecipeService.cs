using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioClassLibrary.Utilities;
using PantioRepository.Mapper;

namespace PantioAPI.Services;

public class RecipeService(
    IRecipeRepository recipeRepository,
    IInventoryItemRepository inventoryItemRepository,
    IInventoryItemCacheService inventoryItemCacheService,
    ILogger<RecipeService> logger
) : IRecipeService
{
    public async Task<bool> CompleteAsync(Guid recipeId, CancellationToken ct = default)
    {
        var recipe = await recipeRepository.GetByIdWithEntriesAsync(recipeId, ct);
        if (recipe is null)
        {
            logger.LogWarning("Complete requested for non-existent recipe {RecipeId}", recipeId);
            return false;
        }

        // Snapshot which entries had links before clearing them
        var linkedEntries = recipe.Entries
            .Where(e => e.InventoryItemId.HasValue)
            .Select(e => (e.InventoryItemId!.Value, e.Quantity, e.MeasuringUnit))
            .ToList();

        // Clear links so the recipe becomes a reusable template
        await recipeRepository.ClearInventoryLinksAsync(recipeId, ct);

        var affectedInventoryIds = new HashSet<Guid>();
        foreach (var (itemId, qty, entryUnit) in linkedEntries)
        {
            var item = await inventoryItemRepository.GetByIdAsync(itemId, ct);
            if (item is null) continue;

            affectedInventoryIds.Add(item.InventoryId);

            var effectiveQty = qty;
            if (item.QuantityUnit.HasValue && entryUnit is not null
                && Enum.TryParse<QuantityUnit>(entryUnit, ignoreCase: true, out var parsedUnit)
                && QuantityUnitConverter.AreSameCategory(item.QuantityUnit.Value, parsedUnit))
            {
                effectiveQty = QuantityUnitConverter.Convert(qty, parsedUnit, item.QuantityUnit.Value);
            }

            var newQty = item.Quantity - effectiveQty;

            if (newQty <= 0)
            {
                await inventoryItemRepository.DeleteAsync(item.Id, ct);
                logger.LogInformation("Deleted inventory item {ItemId} after recipe completion", item.Id);
            }
            else
            {
                await inventoryItemRepository.UpdateAsync(item.Id, new UpdateInventoryItemDto(
                    item.ProductName, newQty, item.QuantityUnit, item.StorageLocation,
                    item.Status, item.RowVersion), ct);
                logger.LogInformation("Decremented inventory item {ItemId} to {NewQty}", item.Id, newQty);
            }
        }

        foreach (var inventoryId in affectedInventoryIds)
            await inventoryItemCacheService.InvalidateAsync(inventoryId, ct);

        await recipeRepository.SetCompletedAsync(recipeId, ct);
        logger.LogInformation("Recipe {RecipeId} marked as completed", recipeId);

        return true;
    }

    public async Task<RecipeSuggestionDto?> LinkToInventoryAsync(Guid recipeId, IEnumerable<Guid> inventoryIds, CancellationToken ct = default)
    {
        var recipe = await recipeRepository.GetByIdWithEntriesAsync(recipeId, ct);
        if (recipe is null)
        {
            logger.LogWarning("Link requested for non-existent recipe {RecipeId}", recipeId);
            return null;
        }

        var allItems = new List<PantioClassLibrary.Entities.InventoryItem>();
        foreach (var inventoryId in inventoryIds)
        {
            var inventoryItems = await inventoryItemRepository.GetByInventoryIdAsync(inventoryId, ct);
            allItems.AddRange(inventoryItems);
        }

        var links = recipe.Entries.ToDictionary(
            e => e.Id,
            e => RecipeIngredientMatcher.FindBestMatch(e.ProductName, allItems)?.Id
        );

        await recipeRepository.UpdateEntryLinksAsync(recipeId, links, ct);
        logger.LogInformation("Linked recipe {RecipeId} to inventories [{InventoryIds}]", recipeId, string.Join(", ", inventoryIds));

        var updated = await recipeRepository.GetByIdWithEntriesAsync(recipeId, ct);
        return RecipeSuggestionMapper.ToDto(updated!);
    }
}
