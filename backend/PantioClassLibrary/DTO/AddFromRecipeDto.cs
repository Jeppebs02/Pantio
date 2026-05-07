namespace PantioClassLibrary.DTO;

public record AddFromRecipeDto(Guid RecipeId, Guid InventoryId, string? Name);
