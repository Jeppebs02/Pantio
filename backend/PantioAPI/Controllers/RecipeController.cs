using Microsoft.AspNetCore.Mvc;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioRepository.Mapper;

namespace PantioAPI.Controllers;

[ApiController]
public class RecipeController(IRecipeService service, IRecipeRepository recipeRepository) : ControllerBase
{
    [HttpGet("api/users/{userId:guid}/recipes")]
    public async Task<IActionResult> ListRecipes(
        Guid userId,
        [FromQuery] string? search,
        [FromQuery(Name = "ingredient")] IEnumerable<string>? ingredients,
        CancellationToken ct)
    {
        var recipes = await recipeRepository.GetByUserFilteredAsync(userId, search, ingredients, ct);
        return Ok(recipes.Select(RecipeSuggestionMapper.ToListItemDto));
    }

    [HttpGet("api/users/{userId:guid}/recipes/{recipeId:guid}")]
    public async Task<IActionResult> GetById(Guid userId, Guid recipeId, CancellationToken ct)
    {
        var recipe = await recipeRepository.GetByIdWithEntriesAsync(recipeId, ct);
        if (recipe is null) return NotFound();
        return Ok(RecipeSuggestionMapper.ToDto(recipe));
    }

    [HttpPost("api/users/{userId:guid}/recipes/{recipeId:guid}/save")]
    public async Task<IActionResult> ToggleSave(Guid userId, Guid recipeId, CancellationToken ct)
    {
        var isSaved = await recipeRepository.ToggleSavedAsync(recipeId, ct);
        if (isSaved is null) return NotFound();
        return Ok(new { isSaved });
    }

    [HttpPost("api/recipes/{recipeId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid recipeId, CompleteRecipeRequestDto request, CancellationToken ct)
    {
        var success = await service.CompleteAsync(recipeId, request.Portions, ct);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPost("api/recipes/{recipeId:guid}/link")]
    public async Task<IActionResult> Link(Guid recipeId, RecipeLinkRequestDto request, CancellationToken ct)
    {
        var result = await service.LinkToInventoryAsync(recipeId, request.InventoryIds, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }
}
