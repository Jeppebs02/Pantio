using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioRepository.EntityFramework;
using PantioRepository.EntityFramework.Repositories;

namespace PantioTest.RepositoryTests;

public class ExpiryDateRepositoryTests
{
    private DbContextOptions<PantioDbContext> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _options = new DbContextOptionsBuilder<PantioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private PantioDbContext CreateContext() => new(_options);

    private static InventoryItem MakeItem(Guid inventoryId) => new()
    {
        Id = Guid.NewGuid(),
        InventoryId = inventoryId,
        ProductName = "Milk",
        Quantity = 1f,
        Status = InventoryStatus.Available,
        AddedVia = AddedVia.Manual,
        AddedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        RowVersion = 0
    };

    [Test]
    public async Task SetOverrideAsync_ItemWithExistingExpiryDate_UpdatesOverride()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var item = MakeItem(inventoryId);
        var existingExpiry = new ExpiryDate
        {
            Id = Guid.NewGuid(),
            InventoryItemId = item.Id,
            EstimatedExpiry = new DateOnly(2026, 6, 1),
            IsManualOverride = false,
            CategoryDefaultUsedDays = 7
        };
        await using (var db = CreateContext())
        {
            db.InventoryItems.Add(item);
            db.ExpiryDates.Add(existingExpiry);
            await db.SaveChangesAsync();
        }
        var overrideDate = new DateOnly(2026, 5, 15);
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new ExpiryDateRepository(db).SetOverrideAsync(item.Id, overrideDate);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            var updated = await db.ExpiryDates.FirstAsync(e => e.InventoryItemId == item.Id);
            Assert.That(updated.IsManualOverride, Is.True);
            Assert.That(updated.OverrideDate, Is.EqualTo(overrideDate));
        }
        #endregion
    }

    [Test]
    public async Task SetOverrideAsync_ItemWithNoExpiryDate_CreatesNewOverride()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var item = MakeItem(inventoryId);
        await using (var db = CreateContext())
        {
            db.InventoryItems.Add(item);
            await db.SaveChangesAsync();
        }
        var overrideDate = new DateOnly(2026, 8, 20);
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new ExpiryDateRepository(db).SetOverrideAsync(item.Id, overrideDate);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            var created = await db.ExpiryDates.FirstOrDefaultAsync(e => e.InventoryItemId == item.Id);
            Assert.That(created, Is.Not.Null);
            Assert.That(created!.IsManualOverride, Is.True);
            Assert.That(created.OverrideDate, Is.EqualTo(overrideDate));
            Assert.That(created.EstimatedExpiry, Is.EqualTo(overrideDate));
        }
        #endregion
    }

    [Test]
    public async Task SetOverrideAsync_NonExistentItem_ReturnsNull()
    {
        #region Arrange
        var nonExistentItemId = Guid.NewGuid();
        #endregion

        #region Act
        ExpiryDate? result;
        await using (var db = CreateContext())
        {
            result = await new ExpiryDateRepository(db).SetOverrideAsync(nonExistentItemId, new DateOnly(2026, 6, 1));
        }
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }
}
