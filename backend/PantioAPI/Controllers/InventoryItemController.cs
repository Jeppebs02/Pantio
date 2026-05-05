using Microsoft.AspNetCore.Mvc;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Exceptions;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Controllers;

[ApiController]
[Route("api/inventories/{inventoryId:guid}/items")]
public class InventoryItemController(IInventoryItemService service, IExpiryDateService expiryDateService) : ControllerBase
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

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid inventoryId, Guid id, UpdateInventoryItemDto dto, CancellationToken ct)
    {
        try
        {
            var updated = await service.UpdateAsync(id, dto, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid inventoryId, Guid id, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPatch("{id:guid}/expiry")]
    public async Task<IActionResult> SetExpiryOverride(Guid inventoryId, Guid id, UpdateExpiryDateDto dto, CancellationToken ct)
    {
        var result = await expiryDateService.SetOverrideAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
