using Microsoft.AspNetCore.Mvc;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Exceptions;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/inventories")]
public class InventoryController(IInventoryService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(Guid userId, CreateInventoryDto dto, CancellationToken ct)
    {
        var inventory = await service.CreateAsync(userId, dto, ct);
        return CreatedAtAction(nameof(GetAll), new { userId }, inventory);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid userId, CancellationToken ct)
    {
        var inventories = await service.GetByUserIdAsync(userId, ct);
        return Ok(inventories);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid userId, Guid id, CancellationToken ct)
    {
        var inventory = await service.GetByIdAsync(id, ct);
        return inventory is null ? NotFound() : Ok(inventory);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid userId, Guid id, UpdateInventoryDto dto, CancellationToken ct)
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
    public async Task<IActionResult> Delete(Guid userId, Guid id, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
