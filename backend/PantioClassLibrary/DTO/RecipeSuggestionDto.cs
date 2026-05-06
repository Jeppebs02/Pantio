namespace PantioClassLibrary.DTO;

public record RecipeSuggestionIngredientDto(
    string ProductName,
    float Quantity,
    string? MeasuringUnit,
    Guid? InventoryItemId,
    bool InInventory
);

public record RecipeSuggestionDto(
    Guid Id,
    string Name,
    string Description,
    string Instructions,
    float Portions,
    IEnumerable<RecipeSuggestionIngredientDto> Ingredients
);

public record RecipeSuggestionListDto(IEnumerable<RecipeSuggestionDto> Suggestions);
