namespace PantioClassLibrary.DTO;

public record RecipeSuggestionIngredientDto(
    string ProductName,
    decimal Quantity,
    string? MeasuringUnit,
    Guid? InventoryItemId,
    bool InInventory
);
