using Microsoft.AspNetCore.Mvc;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/shopping-lists")]
public class ShoppingListController(IShoppingListService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid userId, CancellationToken ct)
    {
        var lists = await service.GetAllAsync(userId, ct);
        return Ok(lists);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid userId, CreateShoppingListDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Name is required." });

        var result = await service.CreateAsync(userId, dto, ct);
        return CreatedAtAction(nameof(GetById), new { userId, listId = result.Id }, result);
    }

    [HttpPost("from-recipe")]
    public async Task<IActionResult> CreateFromRecipe(Guid userId, [FromBody] AddFromRecipeDto dto, CancellationToken ct)
    {
        var result = await service.CreateFromRecipeAsync(userId, dto, ct);
        if (result is null) return NotFound();
        return CreatedAtAction(nameof(GetById), new { userId, listId = result.Id }, result);
    }

    [HttpGet("{listId:guid}")]
    public async Task<IActionResult> GetById(Guid userId, Guid listId, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(listId, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{listId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, Guid listId, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(listId, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{listId:guid}/items")]
    public async Task<IActionResult> AddItem(Guid userId, Guid listId, AddShoppingListItemDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Name is required." });

        var result = await service.AddItemAsync(listId, dto, ct);
        return Created(string.Empty, result);
    }

    [HttpDelete("{listId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid userId, Guid listId, Guid itemId, CancellationToken ct)
    {
        var deleted = await service.DeleteItemAsync(listId, itemId, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPatch("{listId:guid}/items/{itemId:guid}/toggle")]
    public async Task<IActionResult> ToggleItem(Guid userId, Guid listId, Guid itemId, CancellationToken ct)
    {
        var result = await service.ToggleItemAsync(listId, itemId, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }
}
