using Microsoft.EntityFrameworkCore;
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
        Quantity = 1f,
        Status = InventoryStatus.Available,
        AddedVia = AddedVia.Manual,
        AddedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Test]
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
