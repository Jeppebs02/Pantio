using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;

namespace PantioRepository.Mapper;

public static class RecipeSuggestionMapper
{
    public static RecipeListItemDto ToListItemDto(Recipe recipe) =>
        new(
            recipe.Id,
            recipe.Name,
            recipe.Description,
            recipe.Portions,
            recipe.Entries.Count,
            recipe.Entries.Select(e => e.ProductName),
            recipe.IsSaved
        );

    public static RecipeSuggestionDto ToDto(Recipe recipe) =>
        new(
            recipe.Id,
            recipe.Name,
            recipe.Description ?? string.Empty,
            recipe.Instructions ?? string.Empty,
            recipe.Portions,
            recipe.Entries.Select(e => new RecipeSuggestionIngredientDto(
                e.ProductName,
                e.Quantity,
                e.MeasuringUnit,
                e.InventoryItemId,
                e.InventoryItemId.HasValue
            )),
            recipe.IsSaved
        );
}
