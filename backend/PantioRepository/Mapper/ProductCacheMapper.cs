using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;

namespace PantioRepository.Mapper;

public static class ProductCacheMapper
{
    public static OffProductData ToOffProductData(ProductCache cache) =>
        new(
            cache.ProductName,
            cache.Category?.OffTag is string tag ? [tag] : [],
            cache.NutritionFacts is null ? null : new OffNutritionData(
                cache.NutritionFacts.EnergyKcal100g,
                cache.NutritionFacts.Carbohydrates100g,
                cache.NutritionFacts.Sugars100g,
                cache.NutritionFacts.Fat100g,
                cache.NutritionFacts.SaturatedFat100g,
                cache.NutritionFacts.Proteins100g,
                cache.NutritionFacts.Salt100g
            ),
            cache.Category?.DisplayName,
            cache.Category?.DefaultShelfLifeDays
        );

    public static ProductCache ToEntity(Guid userId, string ean, OffProductData data, int? categoryId)
    {
        var entry = new ProductCache
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Ean = ean,
            ProductName = data.ProductName,
            CategoryId = categoryId,
            CachedAt = DateTime.UtcNow
        };

        if (data.Nutrition is not null)
            entry.NutritionFacts = BuildNutritionFacts(entry.Id, data.Nutrition);

        return entry;
    }

    public static ProductCache ToManualEntity(Guid userId, string ean, string productName, int? categoryId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Ean = ean,
            ProductName = productName,
            CategoryId = categoryId,
            CachedAt = DateTime.UtcNow
        };

    private static NutritionFacts BuildNutritionFacts(Guid productCacheId, OffNutritionData nutrition) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProductCacheId = productCacheId,
            EnergyKcal100g = nutrition.EnergyKcal100g,
            Carbohydrates100g = nutrition.Carbohydrates100g,
            Sugars100g = nutrition.Sugars100g,
            Fat100g = nutrition.Fat100g,
            SaturatedFat100g = nutrition.SaturatedFat100g,
            Proteins100g = nutrition.Proteins100g,
            Salt100g = nutrition.Salt100g,
            NutritionDataPer = "100g",
            CachedAt = DateTime.UtcNow
        };
}
