namespace PantioClassLibrary.DTO;

public record ShoppingListItemDto(Guid Id, string Name, float? Quantity, string? MeasuringUnit, bool IsChecked);

public record ShoppingListDto(Guid Id, Guid UserId, string Name, DateTime CreatedAt, List<ShoppingListItemDto> Items);
