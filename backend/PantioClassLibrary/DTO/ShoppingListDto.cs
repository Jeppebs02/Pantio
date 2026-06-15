namespace PantioClassLibrary.DTO;

public record ShoppingListDto(Guid Id, Guid UserId, string Name, DateTime CreatedAt, List<ShoppingListItemDto> Items);
