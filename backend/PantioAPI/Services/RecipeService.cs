using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioRepository.Mapper;

namespace PantioAPI.Services;

public class RecipeService(
    IRecipeRepository recipeRepository,
    IInventoryItemRepository inventoryItemRepository,
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

        // Delete siblings first so their entries no longer reference inventory items
        if (recipe.SuggestionBatchId.HasValue)
        {
            var siblings = await recipeRepository.GetBySuggestionBatchAsync(recipe.SuggestionBatchId.Value, ct);
            foreach (var sibling in siblings.Where(s => s.Id != recipeId))
            {
                await recipeRepository.DeleteAsync(sibling.Id, ct);
                logger.LogInformation("Deleted unused batch recipe {SiblingId}", sibling.Id);
            }
        }

        // Snapshot which entries had links before clearing them
        var linkedEntries = recipe.Entries
            .Where(e => e.InventoryItemId.HasValue)
            .Select(e => (e.InventoryItemId!.Value, e.Quantity))
            .ToList();

        // Clear links so the recipe becomes a reusable template
        await recipeRepository.ClearInventoryLinksAsync(recipeId, ct);

        foreach (var (itemId, qty) in linkedEntries)
        {
            var item = await inventoryItemRepository.GetByIdAsync(itemId, ct);
            if (item is null) continue;

            var newQty = item.Quantity - qty;
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

        await recipeRepository.SetCompletedAsync(recipeId, ct);
        logger.LogInformation("Recipe {RecipeId} marked as completed", recipeId);

        return true;
    }

    public async Task<RecipeSuggestionDto?> LinkToInventoryAsync(Guid recipeId, Guid inventoryId, CancellationToken ct = default)
    {
        var recipe = await recipeRepository.GetByIdWithEntriesAsync(recipeId, ct);
        if (recipe is null)
        {
            logger.LogWarning("Link requested for non-existent recipe {RecipeId}", recipeId);
            return null;
        }

        var items = (await inventoryItemRepository.GetByInventoryIdAsync(inventoryId, ct)).ToList();

        var links = recipe.Entries.ToDictionary(
            e => e.Id,
            e => RecipeIngredientMatcher.FindBestMatch(e.ProductName, items)?.Id
        );

        await recipeRepository.UpdateEntryLinksAsync(recipeId, links, ct);
        logger.LogInformation("Linked recipe {RecipeId} to inventory {InventoryId}", recipeId, inventoryId);

        var updated = await recipeRepository.GetByIdWithEntriesAsync(recipeId, ct);
        return RecipeSuggestionMapper.ToDto(updated!);
    }
}
