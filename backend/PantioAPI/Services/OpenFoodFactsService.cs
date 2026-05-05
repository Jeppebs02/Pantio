using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Services;

public class OpenFoodFactsService(HttpClient http) : IOpenFoodFactsService
{
    public async Task<OffProductData?> GetByEanAsync(string ean, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"api/v2/product/{ean}.json?fields=product_name,categories_tags,nutriments", ct);

            if (!response.IsSuccessStatusCode) return null;

            var apiResponse = await response.Content.ReadFromJsonAsync<OffApiResponse>(ct);
            if (apiResponse?.Status != 1 || apiResponse.Product is null) return null;

            var p = apiResponse.Product;
            return new OffProductData(
                p.ProductName ?? ean,
                p.CategoryTags ?? [],
                p.Nutriments is null ? null : new OffNutritionData(
                    p.Nutriments.EnergyKcal100g,
                    p.Nutriments.Carbohydrates100g,
                    p.Nutriments.Sugars100g,
                    p.Nutriments.Fat100g,
                    p.Nutriments.SaturatedFat100g,
                    p.Nutriments.Proteins100g,
                    p.Nutriments.Salt100g
                )
            );
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private record OffApiResponse(
        [property: JsonPropertyName("status")] int Status,
        [property: JsonPropertyName("product")] OffApiProduct? Product
    );

    private record OffApiProduct(
        [property: JsonPropertyName("product_name")] string? ProductName,
        [property: JsonPropertyName("categories_tags")] List<string>? CategoryTags,
        [property: JsonPropertyName("nutriments")] OffApiNutriments? Nutriments
    );

    private record OffApiNutriments(
        [property: JsonPropertyName("energy-kcal_100g")] float? EnergyKcal100g,
        [property: JsonPropertyName("carbohydrates_100g")] float? Carbohydrates100g,
        [property: JsonPropertyName("sugars_100g")] float? Sugars100g,
        [property: JsonPropertyName("fat_100g")] float? Fat100g,
        [property: JsonPropertyName("saturated-fat_100g")] float? SaturatedFat100g,
        [property: JsonPropertyName("proteins_100g")] float? Proteins100g,
        [property: JsonPropertyName("salt_100g")] float? Salt100g
    );
}
