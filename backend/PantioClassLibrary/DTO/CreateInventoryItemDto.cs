using PantioClassLibrary.Enums;

namespace PantioClassLibrary.DTO;

public record CreateInventoryItemDto(
    string ProductName,
    decimal Quantity,
    QuantityUnit? QuantityUnit,
    string Ean,
    string? StorageLocation,
    //TODO: AddedVia should be sent automatically depending on the method.
    AddedVia AddedVia,
    int? CategoryId = null,
    DateOnly? ManualExpiryDate = null,
    Guid? ReceiptLineId = null
);
