using Microsoft.AspNetCore.Mvc;
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
    [HttpGet("{ean}")]
    public async Task<IActionResult> GetByEan(string ean, CancellationToken ct)
    {
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
}
