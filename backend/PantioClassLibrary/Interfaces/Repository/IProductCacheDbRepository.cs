using PantioClassLibrary.Entities;

namespace PantioClassLibrary.Interfaces.Repository;

public interface IProductCacheDbRepository
{
    Task<ProductCache?> GetByUserAndEanAsync(Guid userId, string ean, CancellationToken ct = default);
    Task SaveAsync(ProductCache entry, CancellationToken ct = default);
}
