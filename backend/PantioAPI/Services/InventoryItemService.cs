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
    IInventoryItemCacheService inventoryItemCacheService,
    ILogger<InventoryItemService> logger) : IInventoryItemService
{
    public async Task<InventoryItemDto> CreateAsync(Guid inventoryId, CreateInventoryItemDto dto, CancellationToken ct = default)
    {
        var entity = InventoryItemMapper.ToEntity(inventoryId, dto);

        ProductCategory? category = null;

        if (!string.IsNullOrWhiteSpace(dto.Ean))
        {
            var offData = await GetProductDataAsync(dto.Ean, ct);
            if (offData is not null)
            {
                entity.ProductName = offData.ProductName;
                category = await categoryRepository.GetFirstMatchingTagAsync(offData.CategoryTags, ct);
                if (category is not null)
                    entity.CategoryId = category.Id;
                else if (offData.CategoryTags.Count > 0)
                    entity.OffTag = offData.CategoryTags[0]; // stored for future learning when user sets expiry
                if (offData.Nutrition is not null)
                    entity.NutritionFacts = BuildNutritionFacts(entity.Id, offData.Nutrition);
            }
        }

        // Fallback: manually supplied CategoryId (manual add without EAN, or OFF returned nothing)
        if (category is null && dto.CategoryId.HasValue)
            category = await categoryRepository.GetByIdAsync(dto.CategoryId.Value, ct);

        entity.ExpiryDate = BuildExpiryDate(entity, dto, category);

        var created = await repository.CreateAsync(entity, ct);
        await inventoryItemCacheService.InvalidateAsync(inventoryId, ct);
        logger.LogInformation("Inventory item {ItemId} created in inventory {InventoryId}", created.Id, inventoryId);
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

    private async Task<OffProductData?> GetProductDataAsync(string ean, CancellationToken ct)
    {
        var cached = await productCacheService.GetAsync(ean, ct);
        if (cached is not null) return cached;

        var data = await offService.GetByEanAsync(ean, ct);
        if (data is not null)
            await productCacheService.SetAsync(ean, data, ct);

        return data;
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
