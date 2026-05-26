using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioClassLibrary.Utilities;
using PantioRepository.Mapper;

namespace PantioAPI.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(
    IProductCacheService productCacheService,
    IProductCacheDbRepository productCacheDbRepository,
    IOpenFoodFactsService offService,
    IProductCategoryRepository categoryRepository) : ControllerBase
{
    private static bool IsValidEan13(string ean) =>
        Regex.IsMatch(ean, @"^\d{13}$");

    [HttpGet("{ean}")]
    public async Task<IActionResult> GetByEan(string ean, CancellationToken ct)
    {
        if (!IsValidEan13(ean)) return BadRequest("EAN must be exactly 13 digits.");
        var userId = (Guid)HttpContext.Items["AuthenticatedUserId"]!;

        // 1. Redis
        var data = await productCacheService.GetAsync(ean, ct);
        if (data is not null)
        {
            // Enrich with category if missing (old cache entry pre-dates this field)
            if (data.CategoryName is null)
            {
                var cachedEntry = await productCacheDbRepository.GetByUserAndEanAsync(userId, ean, ct);
                if (cachedEntry?.Category is not null)
                {
                    data = data with
                    {
                        CategoryName = cachedEntry.Category.DisplayName,
                        DefaultShelfLifeDays = cachedEntry.Category.DefaultShelfLifeDays
                    };
                    await productCacheService.SetAsync(ean, data, ct);
                }
            }
            return Ok(OffQuantityParser.NormalizeData(data));
        }

        // 2. DB
        var dbEntry = await productCacheDbRepository.GetByUserAndEanAsync(userId, ean, ct);
        if (dbEntry is not null)
        {
            data = OffQuantityParser.NormalizeData(ProductCacheMapper.ToOffProductData(dbEntry));
            await productCacheService.SetAsync(ean, data, ct);
            return Ok(data);
        }

        // 3. OFF
        data = await offService.GetByEanAsync(ean, ct);
        if (data is null) return NotFound();

        var category = await categoryRepository.GetFirstMatchingTagAsync(data.CategoryTags, ct);
        var entry = ProductCacheMapper.ToEntity(userId, ean, data, category?.Id);
        if (category is not null)
            data = data with { CategoryName = category.DisplayName, DefaultShelfLifeDays = category.DefaultShelfLifeDays };
        await productCacheDbRepository.SaveAsync(entry, ct);
        await productCacheService.SetAsync(ean, data, ct);

        return Ok(data);
    }

    [HttpPost("{ean}/contribute/quantity")]
    public async Task<IActionResult> ContributeQuantity(string ean, [FromBody] ContributeQuantityDto dto, CancellationToken ct)
    {
        if (!IsValidEan13(ean)) return BadRequest("EAN must be exactly 13 digits.");
        var userId = (Guid)HttpContext.Items["AuthenticatedUserId"]!;

        await offService.ContributeQuantityAsync(ean, dto.Quantity, dto.QuantityUnit, ct);

        var cached = await productCacheService.GetAsync(ean, ct);
        if (cached is not null)
        {
            var updated = cached with { Quantity = dto.Quantity, QuantityUnit = dto.QuantityUnit };
            await productCacheService.SetAsync(ean, updated, ct);
        }

        var dbEntry = await productCacheDbRepository.GetByUserAndEanAsync(userId, ean, ct);
        if (dbEntry is not null)
        {
            dbEntry.Quantity = dto.Quantity.ToString(CultureInfo.InvariantCulture);
            dbEntry.QuantityUnit = dto.QuantityUnit?.ToString();
            await productCacheDbRepository.SaveAsync(dbEntry, ct);
        }

        return NoContent();
    }

    [HttpPost("{ean}/contribute/new-product")]
    public async Task<IActionResult> ContributeNewProduct(string ean, [FromBody] ContributeNewProductDto dto, CancellationToken ct)
    {
        if (!IsValidEan13(ean)) return BadRequest("EAN must be exactly 13 digits.");
        await offService.ContributeNewProductAsync(ean, dto.ProductName, dto.Quantity, dto.QuantityUnit, ct);
        return NoContent();
    }

    [HttpPost("{ean}/contribute/nutrition-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ContributeNutritionImage(string ean, IFormFile image, CancellationToken ct)
    {
        if (!IsValidEan13(ean)) return BadRequest("EAN must be exactly 13 digits.");
        if (image is null || image.Length == 0) return BadRequest("Image required.");

        await using var stream = image.OpenReadStream();
        await offService.ContributeNutritionImageAsync(ean, stream, image.FileName, image.ContentType, ct);

        return NoContent();
    }
}
