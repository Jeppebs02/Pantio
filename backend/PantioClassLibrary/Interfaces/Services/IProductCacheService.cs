using PantioClassLibrary.DTO;

namespace PantioClassLibrary.Interfaces.Services;

public interface IProductCacheService
{
    Task<OffProductData?> GetAsync(string ean, CancellationToken ct = default);
    Task SetAsync(string ean, OffProductData data, CancellationToken ct = default);
}
