namespace PantioClassLibrary.DTO;

public record RecipeListItemDto(
    Guid Id,
    string Name,
    string? Description,
    float Portions,
    int IngredientCount,
    IEnumerable<string> IngredientNames,
    bool IsSaved
);
