using Microsoft.AspNetCore.Mvc;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductCacheService productCacheService, IOpenFoodFactsService offService) : ControllerBase
{
    [HttpGet("{ean}")]
    public async Task<IActionResult> GetByEan(string ean, CancellationToken ct)
    {
        var data = await productCacheService.GetAsync(ean, ct);
        if (data is null)
        {
            data = await offService.GetByEanAsync(ean, ct);
            if (data is null) return NotFound();
            await productCacheService.SetAsync(ean, data, ct);
        }
        return Ok(data);
    }
}
