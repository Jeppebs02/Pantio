using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;

namespace PantioRepository.EntityFramework.Repositories;

public class RecipeRepository(PantioDbContext db) : IRecipeRepository
{
    public async Task<Recipe> CreateAsync(Recipe recipe, CancellationToken ct = default)
    {
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(ct);
        return recipe;
    }

    public async Task<Recipe?> GetByIdWithEntriesAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Recipes
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IEnumerable<Recipe>> GetBySuggestionBatchAsync(Guid batchId, CancellationToken ct = default)
    {
        return await db.Recipes
            .Where(r => r.SuggestionBatchId == batchId)
            .ToListAsync(ct);
    }

    public async Task<Recipe?> SetCompletedAsync(Guid id, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.FindAsync([id], ct);
        if (recipe is null) return null;

        recipe.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return recipe;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.FindAsync([id], ct);
        if (recipe is null) return false;

        db.Recipes.Remove(recipe);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task ClearInventoryLinksAsync(Guid recipeId, CancellationToken ct = default)
    {
        var entries = await db.RecipeEntries
            .Where(e => e.RecipeId == recipeId)
            .ToListAsync(ct);

        foreach (var entry in entries)
            entry.InventoryItemId = null;

        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateEntryLinksAsync(Guid recipeId, Dictionary<Guid, Guid?> links, CancellationToken ct = default)
    {
        var entries = await db.RecipeEntries
            .Where(e => e.RecipeId == recipeId)
            .ToListAsync(ct);

        foreach (var entry in entries)
        {
            if (links.TryGetValue(entry.Id, out var inventoryItemId))
                entry.InventoryItemId = inventoryItemId;
        }

        await db.SaveChangesAsync(ct);
    }
}
