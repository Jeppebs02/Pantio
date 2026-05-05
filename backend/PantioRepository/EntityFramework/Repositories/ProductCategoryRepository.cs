using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;
using PantioRepository.EntityFramework;

namespace PantioRepository.EntityFramework.Repositories;

public class ProductCategoryRepository(PantioDbContext db) : IProductCategoryRepository
{
    public async Task<ProductCategory?> GetByIdAsync(int id, CancellationToken ct = default)
        => await db.ProductCategories.FindAsync([id], ct);

    public async Task<ProductCategory?> GetFirstMatchingTagAsync(IEnumerable<string> offTags, CancellationToken ct = default)
    {
        var tagList = offTags.ToList();
        var matches = await db.ProductCategories
            .Where(c => tagList.Contains(c.OffTag))
            .ToListAsync(ct);

        // Preserve OFF's ordering — most specific tag first
        return tagList
            .Select(tag => matches.FirstOrDefault(c => c.OffTag == tag))
            .FirstOrDefault(c => c is not null);
    }

    public async Task<ProductCategory> CreateIfNotExistsAsync(string offTag, int shelfLifeDays, CancellationToken ct = default)
    {
        var existing = await db.ProductCategories.FirstOrDefaultAsync(c => c.OffTag == offTag, ct);
        if (existing is not null) return existing;

        var category = new ProductCategory
        {
            OffTag = offTag,
            DisplayName = TagToDisplayName(offTag),
            DefaultShelfLifeDays = shelfLifeDays
        };
        db.ProductCategories.Add(category);
        // Intentionally no SaveChangesAsync — caller's SaveChanges flushes this
        // together with the ExpiryDate and InventoryItem FK update in one transaction.
        return category;
    }

    private static string TagToDisplayName(string offTag)
    {
        var segment = offTag.Contains(':') ? offTag[(offTag.LastIndexOf(':') + 1)..] : offTag;
        return char.ToUpper(segment[0]) + segment[1..].Replace('-', ' ');
    }
}
