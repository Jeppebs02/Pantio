using PantioClassLibrary.Enums;

namespace PantioClassLibrary.DTO;

public record ContributeQuantityDto(decimal Quantity, QuantityUnit? QuantityUnit);
