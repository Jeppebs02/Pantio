namespace PantioClassLibrary.DTO;

public record InventoryDto(
    Guid Id,
    Guid UserId,
    string Name,
    int RowVersion
);
