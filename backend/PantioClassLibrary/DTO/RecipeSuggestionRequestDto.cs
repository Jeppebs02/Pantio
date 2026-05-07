namespace PantioClassLibrary.DTO;

public record RecipeSuggestionRequestDto(IEnumerable<Guid> InventoryItemIds);
