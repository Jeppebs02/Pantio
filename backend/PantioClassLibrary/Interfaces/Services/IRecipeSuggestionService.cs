using PantioClassLibrary.DTO;

namespace PantioClassLibrary.Interfaces.Services;

public interface IRecipeSuggestionService
{
    Task<RecipeSuggestionListDto> GetSuggestionsAsync(
        Guid userId,
        RecipeSuggestionRequestDto request,
        CancellationToken ct = default);
}
