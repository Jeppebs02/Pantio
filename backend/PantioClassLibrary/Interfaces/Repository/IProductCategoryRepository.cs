using PantioClassLibrary.Entities;

namespace PantioClassLibrary.Interfaces.Repository;

public interface IProductCategoryRepository
{
    Task<ProductCategory?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductCategory?> GetFirstMatchingTagAsync(IEnumerable<string> offTags, CancellationToken ct = default);
    Task<ProductCategory> CreateIfNotExistsAsync(string offTag, int shelfLifeDays, CancellationToken ct = default);
}
