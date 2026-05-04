using PantioClassLibrary.Enums;

namespace PantioClassLibrary.DTO;

public record CreateInventoryItemDto(
    string ProductName,
    float Quantity,
    string? QuantityUnit,
    string Ean,
    string? StorageLocation,
    //TODO: AddedVia should be sent automatically depending on the method. 
    AddedVia AddedVia
);
