using PantioClassLibrary.Enums;

namespace PantioClassLibrary.DTO;

public record ContributeNewProductDto(string ProductName, decimal? Quantity, QuantityUnit? QuantityUnit);
