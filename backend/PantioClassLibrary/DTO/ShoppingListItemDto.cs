namespace PantioClassLibrary.DTO;

public record ShoppingListItemDto(Guid Id, string Name, decimal? Quantity, string? MeasuringUnit, bool IsChecked);
