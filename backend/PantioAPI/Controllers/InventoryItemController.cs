using Microsoft.AspNetCore.Mvc;
using PantioAPI.Services;
using PantioClassLibrary.DTO;

namespace PantioAPI.Controllers;

[ApiController]
[Route("api/inventories/{inventoryId:guid}/items")]
public class InventoryItemController(IInventoryItemService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(Guid inventoryId, CreateInventoryItemDto dto)
    {
        var item = await service.CreateAsync(inventoryId, dto);
        return CreatedAtAction(nameof(GetAll), new { inventoryId }, item);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid inventoryId)
    {
        var items = await service.GetByInventoryIdAsync(inventoryId);
        return Ok(items);
    }
}
