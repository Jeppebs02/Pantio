namespace PantioClassLibrary.DTO;

public record OffProductData(
    string ProductName,
    IReadOnlyList<string> CategoryTags,
    OffNutritionData? Nutrition,
    string? CategoryName = null,
    int? DefaultShelfLifeDays = null
);

public record OffNutritionData(
    float? EnergyKcal100g,
    float? Carbohydrates100g,
    float? Sugars100g,
    float? Fat100g,
    float? SaturatedFat100g,
    float? Proteins100g,
    float? Salt100g
);
