using Microsoft.AspNetCore.Mvc;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Controllers;

[ApiController]
[Route("api/recipes")]
public class RecipeController(IRecipeService service) : ControllerBase
{
    [HttpPost("{recipeId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid recipeId, CancellationToken ct)
    {
        var success = await service.CompleteAsync(recipeId, ct);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPost("{recipeId:guid}/link")]
    public async Task<IActionResult> Link(Guid recipeId, RecipeLinkRequestDto request, CancellationToken ct)
    {
        var result = await service.LinkToInventoryAsync(recipeId, request.InventoryId, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }
}
