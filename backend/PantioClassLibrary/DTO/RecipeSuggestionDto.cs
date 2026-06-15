namespace PantioClassLibrary.DTO;

public record RecipeSuggestionDto(
    Guid Id,
    string Name,
    string Description,
    string Instructions,
    float Portions,
    IEnumerable<RecipeSuggestionIngredientDto> Ingredients,
    bool IsSaved
);
