using PantioClassLibrary.DTO;

namespace PantioClassLibrary.Interfaces.Services;

public interface IRecipeService
{
    Task<bool> CompleteAsync(Guid recipeId, float requestedPortions, CancellationToken ct = default);
    Task<RecipeSuggestionDto?> LinkToInventoryAsync(Guid recipeId, IEnumerable<Guid> inventoryIds, CancellationToken ct = default);
}
