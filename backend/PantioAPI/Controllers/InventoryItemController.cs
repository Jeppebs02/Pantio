using Microsoft.AspNetCore.Mvc;
using PantioClassLibrary.Interfaces.Services;
using PantioClassLibrary.DTO;

namespace PantioAPI.Controllers;

[ApiController]
[Route("api/inventories/{inventoryId:guid}/items")]
public class InventoryItemController(IInventoryItemService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(Guid inventoryId, CreateInventoryItemDto dto, CancellationToken ct)
    {
        var item = await service.CreateAsync(inventoryId, dto, ct);
        return CreatedAtAction(nameof(GetAll), new { inventoryId }, item);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid inventoryId, CancellationToken ct)
    {
        var items = await service.GetByInventoryIdAsync(inventoryId, ct);
        return Ok(items);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid inventoryId, Guid id, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
