using Microsoft.AspNetCore.Mvc;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
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
        // 1. Redis
        var data = await productCacheService.GetAsync(ean, ct);
        if (data is not null) return Ok(data);

        var userId = (Guid)HttpContext.Items["AuthenticatedUserId"]!;

        // 2. DB
        var dbEntry = await productCacheDbRepository.GetByUserAndEanAsync(userId, ean, ct);
        if (dbEntry is not null)
        {
            data = ProductCacheMapper.ToOffProductData(dbEntry);
            await productCacheService.SetAsync(ean, data, ct);
            return Ok(data);
        }

        // 3. OFF
        data = await offService.GetByEanAsync(ean, ct);
        if (data is null) return NotFound();

        var categoryId = (await categoryRepository.GetFirstMatchingTagAsync(data.CategoryTags, ct))?.Id;
        var entry = ProductCacheMapper.ToEntity(userId, ean, data, categoryId);
        await productCacheDbRepository.SaveAsync(entry, ct);
        await productCacheService.SetAsync(ean, data, ct);

        return Ok(data);
    }
}
