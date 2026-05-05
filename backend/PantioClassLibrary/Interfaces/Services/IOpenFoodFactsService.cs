using PantioClassLibrary.DTO;

namespace PantioClassLibrary.Interfaces.Services;

public interface IOpenFoodFactsService
{
    Task<OffProductData?> GetByEanAsync(string ean, CancellationToken ct = default);
}
