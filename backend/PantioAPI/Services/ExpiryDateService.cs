using Microsoft.Extensions.Logging;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioRepository.Mapper;

namespace PantioAPI.Services;

public class ExpiryDateService(
    IExpiryDateRepository expiryDateRepository,
    IInventoryItemRepository inventoryItemRepository,
    IProductCategoryRepository categoryRepository,
    ILogger<ExpiryDateService> logger) : IExpiryDateService
{
    public async Task<ExpiryDateDto?> SetOverrideAsync(Guid inventoryItemId, UpdateExpiryDateDto dto, CancellationToken ct = default)
    {
        var item = await inventoryItemRepository.GetByIdAsync(inventoryItemId, ct);
        if (item is null)
        {
            logger.LogWarning("Expiry override requested for non-existent inventory item {ItemId}", inventoryItemId);
            return null;
        }

        await LearnCategoryAsync(item, dto.OverrideDate, ct);

        // Shared DbContext scope: LearnCategoryAsync staged a new ProductCategory (if any)
        // and set item.Category. SetOverrideAsync's SaveChangesAsync flushes everything atomically.
        var updated = await expiryDateRepository.SetOverrideAsync(inventoryItemId, dto.OverrideDate, ct);
        logger.LogInformation("Expiry override set for inventory item {ItemId}", inventoryItemId);
        return ExpiryDateMapper.ToDto(updated!);
    }

    private async Task LearnCategoryAsync(InventoryItem item, DateOnly overrideDate, CancellationToken ct)
    {
        if (item.OffTag is null || item.CategoryId is not null) return;

        var shelfLifeDays = overrideDate.DayNumber - DateOnly.FromDateTime(item.AddedAt).DayNumber;
        if (shelfLifeDays <= 0) return;

        var category = await categoryRepository.CreateIfNotExistsAsync(item.OffTag, shelfLifeDays, ct);
        item.Category = category; // EF fixes up item.CategoryId when SaveChanges runs
        logger.LogInformation(
            "Learned category {OffTag} with {Days} day shelf life from user input", item.OffTag, shelfLifeDays);
    }
}
