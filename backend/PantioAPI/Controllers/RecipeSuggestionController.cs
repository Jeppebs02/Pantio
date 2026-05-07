using Microsoft.AspNetCore.Mvc;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/recipe-suggestions")]
public class RecipeSuggestionController(IRecipeSuggestionService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GetSuggestions(
        Guid userId,
        RecipeSuggestionRequestDto request,
        CancellationToken ct)
    {
        if (request.InventoryItemIds is null || !request.InventoryItemIds.Any())
            return BadRequest(new { message = "At least one inventory item ID must be provided." });

        var result = await service.GetSuggestionsAsync(userId, request, ct);
        return Ok(result);
    }
}
