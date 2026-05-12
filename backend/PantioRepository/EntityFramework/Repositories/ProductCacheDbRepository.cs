using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;
using PantioRepository.EntityFramework;

namespace PantioRepository.EntityFramework.Repositories;

public class ProductCacheDbRepository(PantioDbContext db) : IProductCacheDbRepository
{
    public async Task<ProductCache?> GetByUserAndEanAsync(Guid userId, string ean, CancellationToken ct = default)
    {
        return await db.ProductCache
            .Include(p => p.NutritionFacts)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Ean == ean, ct);
    }

    public async Task SaveAsync(ProductCache entry, CancellationToken ct = default)
    {
        var existing = await db.ProductCache
            .Include(p => p.NutritionFacts)
            .FirstOrDefaultAsync(p => p.UserId == entry.UserId && p.Ean == entry.Ean, ct);

        if (existing is null)
        {
            db.ProductCache.Add(entry);
            await db.SaveChangesAsync(ct);
            return;
        }

        existing.ProductName = entry.ProductName;
        existing.CategoryId = entry.CategoryId;
        existing.CachedAt = entry.CachedAt;

        if (entry.NutritionFacts is not null)
        {
            if (existing.NutritionFacts is null)
            {
                entry.NutritionFacts.Id = Guid.NewGuid();
                entry.NutritionFacts.ProductCacheId = existing.Id;
                db.NutritionFacts.Add(entry.NutritionFacts);
            }
            else
            {
                var n = existing.NutritionFacts;
                var src = entry.NutritionFacts;
                n.EnergyKcal100g = src.EnergyKcal100g;
                n.Carbohydrates100g = src.Carbohydrates100g;
                n.Sugars100g = src.Sugars100g;
                n.Fat100g = src.Fat100g;
                n.SaturatedFat100g = src.SaturatedFat100g;
                n.Proteins100g = src.Proteins100g;
                n.Salt100g = src.Salt100g;
                n.CachedAt = entry.NutritionFacts.CachedAt;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
