using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioRepository.EntityFramework;
using PantioRepository.EntityFramework.Repositories;

namespace PantioTest.RepositoryTests;

public class InventoryItemRepositoryTests
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

    private static InventoryItem MakeItem(Guid inventoryId, string productName = "Milk") => new()
    {
        Id = Guid.NewGuid(),
        InventoryId = inventoryId,
        ProductName = productName,
        Quantity = 1m,
        Status = InventoryStatus.Available,
        AddedVia = AddedVia.Manual,
        AddedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        RowVersion = 0
    };

    [Test]
    [Category("LA-02")]
    public async Task CreateAsync_ValidItem_PersistsToDatabase()
    {
        #region Arrange
        var item = MakeItem(Guid.NewGuid());
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new InventoryItemRepository(db).CreateAsync(item);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            Assert.That(await db.InventoryItems.FindAsync(item.Id), Is.Not.Null);
        }
        #endregion
    }

    [Test]
    [Category("LA-02")]
    public async Task CreateAsync_ValidItem_ReturnsEntityWithSameId()
    {
        #region Arrange
        var item = MakeItem(Guid.NewGuid());
        #endregion

        #region Act
        await using var db = CreateContext();
        var result = await new InventoryItemRepository(db).CreateAsync(item);
        #endregion

        #region Assert
        Assert.That(result.Id, Is.EqualTo(item.Id));
        Assert.That(result.ProductName, Is.EqualTo(item.ProductName));
        #endregion
    }

    [Test]
    [Category("LA-01")]
    public async Task GetByInventoryIdAsync_InventoryWithItems_ReturnsOnlyMatchingItems()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var otherInventoryId = Guid.NewGuid();
        await using (var db = CreateContext())
        {
            db.InventoryItems.AddRange(
                MakeItem(inventoryId, "Milk"),
                MakeItem(inventoryId, "Eggs"),
                MakeItem(otherInventoryId, "Bread")
            );
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        IEnumerable<InventoryItem> result;
        await using (var db = CreateContext())
        {
            result = await new InventoryItemRepository(db).GetByInventoryIdAsync(inventoryId);
        }
        #endregion

        #region Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(i => i.InventoryId == inventoryId), Is.True);
        #endregion
    }

    [Test]
    [Category("LA-01")]
    public async Task GetByInventoryIdAsync_EmptyInventory_ReturnsEmptyCollection()
    {
        #region Arrange
        await using var db = CreateContext();
        #endregion

        #region Act
        var result = await new InventoryItemRepository(db).GetByInventoryIdAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.Empty);
        #endregion
    }

    [Test]
    [Category("LA-01")]
    public async Task GetByIdAsync_ExistingItem_ReturnsCorrectItem()
    {
        #region Arrange
        var item = MakeItem(Guid.NewGuid());
        await using (var db = CreateContext())
        {
            db.InventoryItems.Add(item);
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        InventoryItem? result;
        await using (var db = CreateContext())
        {
            result = await new InventoryItemRepository(db).GetByIdAsync(item.Id);
        }
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(item.Id));
        Assert.That(result.ProductName, Is.EqualTo(item.ProductName));
        #endregion
    }

    [Test]
    [Category("LA-01")]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        #region Arrange
        await using var db = CreateContext();
        #endregion

        #region Act
        var result = await new InventoryItemRepository(db).GetByIdAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    [Category("LA-03")]
    public async Task UpdateAsync_ExistingItem_PersistsChanges()
    {
        #region Arrange
        var item = MakeItem(Guid.NewGuid());
        await using (var db = CreateContext())
        {
            db.InventoryItems.Add(item);
            await db.SaveChangesAsync();
        }
        var dto = new UpdateInventoryItemDto("Yoghurt", 3m, null, "Køleskab", InventoryStatus.Low, RowVersion: 0);
        #endregion

        #region Act
        InventoryItem? result;
        await using (var db = CreateContext())
        {
            result = await new InventoryItemRepository(db).UpdateAsync(item.Id, dto);
        }
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ProductName, Is.EqualTo("Yoghurt"));
        Assert.That(result.Quantity, Is.EqualTo(3m));
        Assert.That(result.Status, Is.EqualTo(InventoryStatus.Low));
        Assert.That(result.RowVersion, Is.EqualTo(1));
        await using (var db = CreateContext())
        {
            var persisted = await db.InventoryItems.FindAsync(item.Id);
            Assert.That(persisted!.ProductName, Is.EqualTo("Yoghurt"));
        }
        #endregion
    }

    [Test]
    [Category("LA-03")]
    public async Task UpdateAsync_NonExistentId_ReturnsNull()
    {
        #region Arrange
        await using var db = CreateContext();
        var dto = new UpdateInventoryItemDto("X", 1m, null, null, InventoryStatus.Available, RowVersion: 0);
        #endregion

        #region Act
        var result = await new InventoryItemRepository(db).UpdateAsync(Guid.NewGuid(), dto);
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    [Category("IK-04")]
    public async Task UpdateAsync_StaleRowVersion_ThrowsDbUpdateConcurrencyException()
    {
        #region Arrange
        var item = MakeItem(Guid.NewGuid());
        await using (var db = CreateContext())
        {
            db.InventoryItems.Add(item);
            await db.SaveChangesAsync();
        }

        // Simulate concurrent write: bump RowVersion to 1
        await using (var db = CreateContext())
        {
            var concurrent = await db.InventoryItems.FindAsync(item.Id);
            concurrent!.RowVersion = 1;
            await db.SaveChangesAsync();
        }

        var dto = new UpdateInventoryItemDto("Stale", 1m, null, null, InventoryStatus.Available, RowVersion: 0);
        #endregion

        #region Act & Assert
        await using (var db = CreateContext())
        {
            Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                new InventoryItemRepository(db).UpdateAsync(item.Id, dto));
        }
        #endregion
    }

    [Test]
    [Category("LA-02")]
    public async Task DeleteAsync_ExistingItem_ReturnsTrueAndRemovesFromDatabase()
    {
        #region Arrange
        var item = MakeItem(Guid.NewGuid());
        await using (var db = CreateContext())
        {
            db.InventoryItems.Add(item);
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        bool deleted;
        await using (var db = CreateContext())
        {
            deleted = await new InventoryItemRepository(db).DeleteAsync(item.Id);
        }
        #endregion

        #region Assert
        Assert.That(deleted, Is.True);
        await using (var db = CreateContext())
        {
            Assert.That(await db.InventoryItems.FindAsync(item.Id), Is.Null);
        }
        #endregion
    }

    [Test]
    [Category("LA-02")]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        #region Arrange
        await using var db = CreateContext();
        #endregion

        #region Act
        var result = await new InventoryItemRepository(db).DeleteAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.False);
        #endregion
    }
}
