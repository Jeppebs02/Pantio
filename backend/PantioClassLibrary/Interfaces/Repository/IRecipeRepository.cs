using PantioClassLibrary.Entities;

namespace PantioClassLibrary.Interfaces.Repository;

public interface IRecipeRepository
{
    Task<Recipe> CreateAsync(Recipe recipe, CancellationToken ct = default);
    Task<Recipe?> GetByIdWithEntriesAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Recipe>> GetBySuggestionBatchAsync(Guid batchId, CancellationToken ct = default);
    Task<Recipe?> SetCompletedAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task ClearInventoryLinksAsync(Guid recipeId, CancellationToken ct = default);
    Task UpdateEntryLinksAsync(Guid recipeId, Dictionary<Guid, Guid?> links, CancellationToken ct = default);
    Task<IEnumerable<Recipe>> GetByUserFilteredAsync(Guid userId, string? search, IEnumerable<string>? ingredientNames, CancellationToken ct = default);
    Task<bool?> ToggleSavedAsync(Guid recipeId, CancellationToken ct = default);
}
