using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioRepository.Mapper;

namespace PantioAPI.Services;

public class RecipeSuggestionService(
    IInventoryItemRepository inventoryItemRepository,
    IRecipeRepository recipeRepository,
    IHttpClientFactory httpClientFactory,
    IOptions<GeminiOptions> geminiOptions,
    ILogger<RecipeSuggestionService> logger
) : IRecipeSuggestionService
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private record GeminiIngredient(string ProductName, float? Quantity, string? Unit);
    private record GeminiRecipe(string Name, string Description, string Instructions,
                                 float? Portions, List<GeminiIngredient> Ingredients);
    private record GeminiResponseBody(List<GeminiRecipe> Recipes);

    public async Task<RecipeSuggestionListDto> GetSuggestionsAsync(
        Guid userId,
        RecipeSuggestionRequestDto request,
        CancellationToken ct = default)
    {
        var idList = request.InventoryItemIds.ToList();

        if (idList.Count == 0)
        {
            logger.LogWarning("Recipe suggestion requested with empty item list for user {UserId}", userId);
            return new RecipeSuggestionListDto([]);
        }

        var items = (await inventoryItemRepository.GetByIdsAsync(idList, ct)).ToList();

        if (items.Count == 0)
        {
            logger.LogWarning("No inventory items found for IDs supplied by user {UserId}", userId);
            return new RecipeSuggestionListDto([]);
        }

        logger.LogDebug("Building Gemini prompt for {Count} inventory items, user {UserId}", items.Count, userId);

        var prompt = BuildPrompt(items);
        logger.LogInformation("Gemini prompt:\n{Prompt}", prompt);
        var rawJson = await CallGeminiAsync(prompt, ct);

        var geminiBody = JsonSerializer.Deserialize<GeminiResponseBody>(rawJson, JsonOpts);

        if (geminiBody?.Recipes is not { Count: > 0 })
        {
            logger.LogWarning("Gemini returned empty or unparseable recipe list for user {UserId}", userId);
            return new RecipeSuggestionListDto([]);
        }

        var batchId = Guid.NewGuid();
        var savedRecipes = new List<Recipe>();

        foreach (var geminiRecipe in geminiBody.Recipes)
        {
            var recipe = new Recipe
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = geminiRecipe.Name,
                Description = geminiRecipe.Description,
                Instructions = geminiRecipe.Instructions,
                Portions = geminiRecipe.Portions ?? 2f,
                SuggestionBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                Entries = geminiRecipe.Ingredients.Select(ing => new RecipeEntry
                {
                    Id = Guid.NewGuid(),
                    ProductName = ing.ProductName,
                    Quantity = ing.Quantity ?? 0,
                    MeasuringUnit = ing.Unit,
                    InventoryItemId = RecipeIngredientMatcher.FindBestMatch(ing.ProductName, items)?.Id
                }).ToList()
            };

            var saved = await recipeRepository.CreateAsync(recipe, ct);
            savedRecipes.Add(saved);
        }

        logger.LogInformation("Saved {Count} recipe suggestions (batch {BatchId}) for user {UserId}",
            savedRecipes.Count, batchId, userId);

        return new RecipeSuggestionListDto(savedRecipes.Select(RecipeSuggestionMapper.ToDto));
    }

    private static string BuildPrompt(List<InventoryItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Du er en hjælpsom opskriftsassistent. Foreslå præcis 3 opskrifter på dansk.");
        sb.AppendLine();
        sb.AppendLine("Regler:");
        sb.AppendLine("- De 3 opskrifter skal være tydeligt forskellige fra hinanden: vælg f.eks. én ret med kød, én vegetarisk og én med pasta/korn - eller én hurtig hverdagsret, én ovnret og én salat/kold ret.");
        sb.AppendLine("- Hver opskrift skal bruge mindst 2 af de tilgængelige ingredienser.");
        sb.AppendLine("- Du må frit tilføje andre ingredienser som opskriften kræver - du er ikke begrænset til kun de tilgængelige varer.");
        sb.AppendLine("- Mængder skal være realistiske.");
        sb.AppendLine("- Instruktioner skal være én samlet tekst med nummererede trin adskilt af linjeskift.");
        sb.AppendLine("- Portioner skal være et tal der angiver antal serveringer.");
        sb.AppendLine();
        sb.AppendLine("Tilgængelige ingredienser:");

        foreach (var item in items)
        {
            var unit = item.QuantityUnit ?? "unit";
            sb.AppendLine($"- {item.ProductName}: {item.Quantity} {unit}");
        }

        sb.AppendLine();
        sb.AppendLine("Svar med KUN gyldig JSON i dette præcise skema - ingen markdown, ingen forklaring:");
        sb.AppendLine("""
{
  "recipes": [
    {
      "name": "string",
      "description": "string",
      "instructions": "string",
      "portions": 2.0,
      "ingredients": [
        {
          "productName": "string",
          "quantity": 1.0,
          "unit": "string or null"
        }
      ]
    }
  ]
}
""");

        return sb.ToString();
    }

    private async Task<string> CallGeminiAsync(string prompt, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Gemini");
        var apiKey = geminiOptions.Value.ApiKey;

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                responseMimeType = "application/json",
                thinkingConfig   = new { thinkingBudget = 0 }
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        logger.LogDebug("Calling Gemini API");
        var response = await client.PostAsJsonAsync(url, requestBody, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Gemini API error {Status}: {Body}", (int)response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0)
            throw new InvalidOperationException("Gemini returned no candidates (prompt may have been blocked).");

        var text = candidates[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Gemini returned an empty text payload.");

        if (doc.RootElement.TryGetProperty("usageMetadata", out var usage))
        {
            var promptTokens    = usage.TryGetProperty("promptTokenCount",      out var p) ? p.GetInt32() : 0;
            var outputTokens    = usage.TryGetProperty("candidatesTokenCount",  out var c) ? c.GetInt32() : 0;
            var thoughtsTokens  = usage.TryGetProperty("thoughtsTokenCount",    out var t) ? t.GetInt32() : 0;
            logger.LogInformation("Gemini usage — prompt: {Prompt}, output: {Output}, thinking: {Thinking} tokens",
                promptTokens, outputTokens, thoughtsTokens);
        }

        logger.LogDebug("Gemini response received, length {Length}", text.Length);
        return text;
    }

}
