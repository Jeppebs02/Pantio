using PantioClassLibrary.DTO;
using PantioClassLibrary.Enums;

namespace PantioClassLibrary.Interfaces.Services;

public interface IOpenFoodFactsService
{
    Task<OffProductData?> GetByEanAsync(string ean, CancellationToken ct = default);
    Task ContributeQuantityAsync(string ean, decimal quantity, QuantityUnit? unit, CancellationToken ct = default);
    Task ContributeNewProductAsync(string ean, string productName, decimal? quantity, QuantityUnit? unit, CancellationToken ct = default);
    Task ContributeNutritionImageAsync(string ean, Stream imageStream, string fileName, string contentType, CancellationToken ct = default);
}
