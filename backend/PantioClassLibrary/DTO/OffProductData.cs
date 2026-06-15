using PantioClassLibrary.Enums;

namespace PantioClassLibrary.DTO;

public record OffProductData(
    string ProductName,
    IReadOnlyList<string> CategoryTags,
    OffNutritionData? Nutrition,
    string? CategoryName = null,
    int? DefaultShelfLifeDays = null,
    decimal? Quantity = null,
    QuantityUnit? QuantityUnit = null
);
