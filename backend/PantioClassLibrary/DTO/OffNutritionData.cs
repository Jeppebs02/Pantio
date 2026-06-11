namespace PantioClassLibrary.DTO;

public record OffNutritionData(
    float? EnergyKcal100g,
    float? Carbohydrates100g,
    float? Sugars100g,
    float? Fat100g,
    float? SaturatedFat100g,
    float? Proteins100g,
    float? Salt100g
);
