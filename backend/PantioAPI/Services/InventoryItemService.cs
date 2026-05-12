using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Exceptions;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioRepository.Mapper;

namespace PantioAPI.Services;

public class InventoryItemService(
    IInventoryItemRepository repository,
    IProductCategoryRepository categoryRepository,
    IOpenFoodFactsService offService,
    IProductCacheService productCacheService,
    IProductCacheDbRepository productCacheDbRepository,
    IInventoryItemCacheService inventoryItemCacheService,
    ILogger<InventoryItemService> logger) : IInventoryItemService
{
    public async Task<InventoryItemDto> CreateAsync(Guid inventoryId, Guid userId, CreateInventoryItemDto dto, CancellationToken ct = default)
    {
        var entity = InventoryItemMapper.ToEntity(inventoryId, dto);

        ProductCategory? category = null;
        OffProductData? offData = null;

        if (!string.IsNullOrWhiteSpace(dto.Ean))
        {
            offData = await GetProductDataAsync(userId, dto.Ean, ct);
            if (offData is not null)
            {
                entity.ProductName = offData.ProductName;
                category = await categoryRepository.GetFirstMatchingTagAsync(offData.CategoryTags, ct);
                if (category is not null)
                    entity.CategoryId = category.Id;
                else if (offData.CategoryTags.Count > 0)
                    entity.OffTag = offData.CategoryTags[0];
                if (offData.Nutrition is not null)
                    entity.NutritionFacts = BuildNutritionFacts(entity.Id, offData.Nutrition);
            }
        }

        // Fallback: manually supplied CategoryId
        if (category is null && dto.CategoryId.HasValue)
            category = await categoryRepository.GetByIdAsync(dto.CategoryId.Value, ct);

        entity.ExpiryDate = BuildExpiryDate(entity, dto, category);

        // Learn category from user's manual expiry date when OFF returned tags but DB had no match.
        // CreateIfNotExistsAsync intentionally omits SaveChanges — repository.CreateAsync flushes atomically.
        if (category is null && entity.OffTag is not null && dto.ManualExpiryDate.HasValue)
        {
            var shelfLifeDays = dto.ManualExpiryDate.Value.DayNumber - DateOnly.FromDateTime(entity.AddedAt).DayNumber;
            if (shelfLifeDays > 0)
            {
                var learnedCategory = await categoryRepository.CreateIfNotExistsAsync(entity.OffTag, shelfLifeDays, ct);
                entity.Category = learnedCategory; // EF fixes up CategoryId on SaveChanges
                logger.LogInformation(
                    "Learned category {OffTag} with {Days}-day shelf life from manual entry on create",
                    entity.OffTag, shelfLifeDays);
            }
        }

        var created = await repository.CreateAsync(entity, ct);
        await inventoryItemCacheService.InvalidateAsync(inventoryId, ct);
        logger.LogInformation("Inventory item {ItemId} created in inventory {InventoryId}", created.Id, inventoryId);

        // OFF returned nothing — persist the user's manual entry so future lookups find it
        if (!string.IsNullOrWhiteSpace(dto.Ean) && offData is null)
        {
            var manualEntry = ProductCacheMapper.ToManualEntity(userId, dto.Ean, dto.ProductName, category?.Id);
            await productCacheDbRepository.SaveAsync(manualEntry, ct);
            logger.LogInformation("Manual product entry saved for EAN {Ean}", dto.Ean);
        }

        return InventoryItemMapper.ToDto(created);
    }

    public async Task<IEnumerable<InventoryItemDto>> GetByInventoryIdAsync(Guid inventoryId, CancellationToken ct = default)
    {
        var cached = await inventoryItemCacheService.GetAsync(inventoryId, ct);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for inventory {InventoryId}", inventoryId);
            return cached;
        }

        logger.LogDebug("Cache miss — fetching items for inventory {InventoryId} from database", inventoryId);
        var items = await repository.GetByInventoryIdAsync(inventoryId, ct);
        var dtos = items.Select(InventoryItemMapper.ToDto).ToList();
        await inventoryItemCacheService.SetAsync(inventoryId, dtos, ct);
        return dtos;
    }

    public async Task<InventoryItemDto?> UpdateAsync(Guid id, UpdateInventoryItemDto dto, CancellationToken ct = default)
    {
        try
        {
            var updated = await repository.UpdateAsync(id, dto, ct);
            if (updated is null)
            {
                logger.LogWarning("Update requested for non-existent inventory item {ItemId}", id);
                return null;
            }
            await inventoryItemCacheService.InvalidateAsync(updated.InventoryId, ct);
            logger.LogInformation("Inventory item {ItemId} updated", id);
            return InventoryItemMapper.ToDto(updated);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict on inventory item {ItemId}", id);
            throw new ConcurrencyConflictException("Varen blev ændret af en anden operation. Hent varen igen og prøv igen.");
        }
    }

    public async Task<bool> DeleteAsync(Guid inventoryId, Guid id, CancellationToken ct = default)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        if (deleted)
        {
            await inventoryItemCacheService.InvalidateAsync(inventoryId, ct);
            logger.LogInformation("Inventory item {ItemId} deleted", id);
        }
        else
            logger.LogWarning("Delete requested for non-existent inventory item {ItemId}", id);
        return deleted;
    }

    private async Task<OffProductData?> GetProductDataAsync(Guid userId, string ean, CancellationToken ct)
    {
        // 1. Redis
        var cached = await productCacheService.GetAsync(ean, ct);
        if (cached is not null) return cached;

        // 2. DB
        var dbEntry = await productCacheDbRepository.GetByUserAndEanAsync(userId, ean, ct);
        if (dbEntry is not null)
        {
            var dbData = ProductCacheMapper.ToOffProductData(dbEntry);
            await productCacheService.SetAsync(ean, dbData, ct);
            return dbData;
        }

        // 3. OFF
        var offData = await offService.GetByEanAsync(ean, ct);
        if (offData is null) return null;

        var categoryId = (await categoryRepository.GetFirstMatchingTagAsync(offData.CategoryTags, ct))?.Id;
        var entry = ProductCacheMapper.ToEntity(userId, ean, offData, categoryId);
        await productCacheDbRepository.SaveAsync(entry, ct);
        await productCacheService.SetAsync(ean, offData, ct);

        return offData;
    }

    private static ExpiryDate? BuildExpiryDate(InventoryItem entity, CreateInventoryItemDto dto, ProductCategory? category)
    {
        if (dto.ManualExpiryDate.HasValue)
            return new ExpiryDate
            {
                Id = Guid.NewGuid(),
                InventoryItemId = entity.Id,
                EstimatedExpiry = dto.ManualExpiryDate.Value,
                IsManualOverride = true,
                OverrideDate = dto.ManualExpiryDate.Value
            };

        if (category is not null)
            return new ExpiryDate
            {
                Id = Guid.NewGuid(),
                InventoryItemId = entity.Id,
                EstimatedExpiry = DateOnly.FromDateTime(entity.AddedAt.AddDays(category.DefaultShelfLifeDays)),
                IsManualOverride = false,
                CategoryDefaultUsedDays = category.DefaultShelfLifeDays
            };

        return null;
    }

    private static NutritionFacts BuildNutritionFacts(Guid inventoryItemId, OffNutritionData nutrition) => new()
    {
        Id = Guid.NewGuid(),
        InventoryItemId = inventoryItemId,
        EnergyKcal100g = nutrition.EnergyKcal100g,
        Carbohydrates100g = nutrition.Carbohydrates100g,
        Sugars100g = nutrition.Sugars100g,
        Fat100g = nutrition.Fat100g,
        SaturatedFat100g = nutrition.SaturatedFat100g,
        Proteins100g = nutrition.Proteins100g,
        Salt100g = nutrition.Salt100g,
        NutritionDataPer = "100g",
        CachedAt = DateTime.UtcNow
    };
}
