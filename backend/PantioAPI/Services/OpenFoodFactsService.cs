using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PantioAPI.Options;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Services;
using PantioClassLibrary.Utilities;

namespace PantioAPI.Services;

public class OpenFoodFactsService(
    HttpClient http,
    IOptions<OpenFoodFactsOptions> options,
    ILogger<OpenFoodFactsService> logger) : IOpenFoodFactsService
{
    public async Task<OffProductData?> GetByEanAsync(string ean, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"api/v2/product/{ean}.json?fields=product_name,categories_tags,nutriments,quantity", ct);

            logger.LogInformation("OFF lookup for {Ean}: HTTP {Status}", ean, (int)response.StatusCode);

            if (!response.IsSuccessStatusCode) return null;

            var apiResponse = await response.Content.ReadFromJsonAsync<OffApiResponse>(ct);

            logger.LogInformation("OFF response status={OffStatus} hasProduct={HasProduct}", apiResponse?.Status, apiResponse?.Product is not null);

            if (apiResponse?.Status != 1 || apiResponse.Product is null) return null;

            var p = apiResponse.Product;
            var (parsedQty, parsedUnit) = OffQuantityParser.Parse(p.Quantity);
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
                ),
                Quantity: parsedQty,
                QuantityUnit: parsedUnit
            );
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "OFF HTTP request failed for EAN {Ean}", ean);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in OFF lookup for EAN {Ean}", ean);
            return null;
        }
    }

    public async Task ContributeQuantityAsync(string ean, decimal quantity, QuantityUnit? unit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ContributorUserId)) return;

        var quantityStr = unit.HasValue
            ? $"{quantity.ToString(CultureInfo.InvariantCulture)} {unit.Value}"
            : quantity.ToString(CultureInfo.InvariantCulture);

        var fields = new Dictionary<string, string>
        {
            ["code"] = ean,
            ["user_id"] = options.Value.ContributorUserId,
            ["password"] = options.Value.ContributorPassword,
            ["quantity"] = quantityStr,
            ["comment"] = "Added via Pantio app"
        };

        try
        {
            var response = await http.PostAsync("cgi/product_jqm2.pl", new FormUrlEncodedContent(fields), ct);
            logger.LogInformation("OFF quantity contribution for {Ean}: HTTP {Status}", ean, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OFF quantity contribution failed for EAN {Ean}", ean);
        }
    }

    public async Task ContributeNewProductAsync(string ean, string productName, decimal? quantity, QuantityUnit? unit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ContributorUserId)) return;

        var fields = new Dictionary<string, string>
        {
            ["code"] = ean,
            ["user_id"] = options.Value.ContributorUserId,
            ["password"] = options.Value.ContributorPassword,
            ["product_name"] = productName,
            ["lang"] = "da",
            ["comment"] = "New product added via Pantio app"
        };

        if (quantity.HasValue && unit.HasValue)
            fields["quantity"] = $"{quantity.Value.ToString(CultureInfo.InvariantCulture)} {unit.Value}";
        else if (quantity.HasValue)
            fields["quantity"] = quantity.Value.ToString(CultureInfo.InvariantCulture);

        try
        {
            var response = await http.PostAsync("cgi/product_jqm2.pl", new FormUrlEncodedContent(fields), ct);
            logger.LogInformation("OFF new product contribution for {Ean}: HTTP {Status}", ean, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OFF new product contribution failed for EAN {Ean}", ean);
        }
    }

    public async Task ContributeNutritionImageAsync(string ean, Stream imageStream, string fileName, string contentType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ContributorUserId)) return;

        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(ean), "code");
            form.Add(new StringContent(options.Value.ContributorUserId), "user_id");
            form.Add(new StringContent(options.Value.ContributorPassword), "password");
            form.Add(new StringContent("nutrition"), "imagefield");
            form.Add(new StringContent("Added via Pantio app"), "comment");

            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(imageContent, "imgupload_nutrition", fileName);

            var response = await http.PostAsync("cgi/product_image_upload.pl", form, ct);
            logger.LogInformation("OFF nutrition image contribution for {Ean}: HTTP {Status}", ean, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OFF nutrition image contribution failed for EAN {Ean}", ean);
        }
    }

    private record OffApiResponse(
        [property: JsonPropertyName("status")] int Status,
        [property: JsonPropertyName("product")] OffApiProduct? Product
    );

    private record OffApiProduct(
        [property: JsonPropertyName("product_name")] string? ProductName,
        [property: JsonPropertyName("categories_tags")] List<string>? CategoryTags,
        [property: JsonPropertyName("nutriments")] OffApiNutriments? Nutriments,
        [property: JsonPropertyName("quantity")] string? Quantity
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
