using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioRepository.EntityFramework;
using PantioRepository.EntityFramework.Repositories;

namespace PantioTest.RepositoryTests;

public class InventoryRepositoryTests
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

    private static Inventory MakeInventory(Guid userId, string name = "Fridge") =>
        new() { Id = Guid.NewGuid(), UserId = userId, Name = name, RowVersion = 0 };

    [Test]
    public async Task CreateAsync_ValidInventory_PersistsToDatabase()
    {
        #region Arrange
        var inventory = MakeInventory(Guid.NewGuid());
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new InventoryRepository(db).CreateAsync(inventory);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            Assert.That(await db.Inventories.FindAsync(inventory.Id), Is.Not.Null);
        }
        #endregion
    }

    [Test]
    public async Task CreateAsync_ValidInventory_ReturnsEntityWithSameId()
    {
        #region Arrange
        var inventory = MakeInventory(Guid.NewGuid());
        #endregion

        #region Act
        await using var db = CreateContext();
        var result = await new InventoryRepository(db).CreateAsync(inventory);
        #endregion

        #region Assert
        Assert.That(result.Id, Is.EqualTo(inventory.Id));
        Assert.That(result.Name, Is.EqualTo(inventory.Name));
        #endregion
    }

    [Test]
    public async Task GetByUserIdAsync_UserWithMultipleInventories_ReturnsOnlyUsersInventories()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await using (var db = CreateContext())
        {
            db.Inventories.AddRange(
                MakeInventory(userId, "Fridge"),
                MakeInventory(userId, "Pantry"),
                MakeInventory(otherUserId, "Other")
            );
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        IEnumerable<Inventory> result;
        await using (var db = CreateContext())
        {
            result = await new InventoryRepository(db).GetByUserIdAsync(userId);
        }
        #endregion

        #region Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(i => i.UserId == userId), Is.True);
        #endregion
    }

    [Test]
    public async Task GetByUserIdAsync_UserWithNoInventories_ReturnsEmptyCollection()
    {
        #region Arrange
        await using var db = CreateContext();
        #endregion

        #region Act
        var result = await new InventoryRepository(db).GetByUserIdAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.Empty);
        #endregion
    }

    [Test]
    public async Task GetByIdAsync_ExistingInventory_ReturnsCorrectInventory()
    {
        #region Arrange
        var inventory = MakeInventory(Guid.NewGuid());
        await using (var db = CreateContext())
        {
            db.Inventories.Add(inventory);
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        Inventory? result;
        await using (var db = CreateContext())
        {
            result = await new InventoryRepository(db).GetByIdAsync(inventory.Id);
        }
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(inventory.Id));
        Assert.That(result.Name, Is.EqualTo(inventory.Name));
        #endregion
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        #region Arrange
        await using var db = CreateContext();
        #endregion

        #region Act
        var result = await new InventoryRepository(db).GetByIdAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    public async Task UpdateAsync_ExistingInventory_PersistsChanges()
    {
        #region Arrange
        var inventory = MakeInventory(Guid.NewGuid());
        await using (var db = CreateContext())
        {
            db.Inventories.Add(inventory);
            await db.SaveChangesAsync();
        }
        var dto = new UpdateInventoryDto("Pantry", RowVersion: 0);
        #endregion

        #region Act
        Inventory? result;
        await using (var db = CreateContext())
        {
            result = await new InventoryRepository(db).UpdateAsync(inventory.Id, dto);
        }
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Pantry"));
        Assert.That(result.RowVersion, Is.EqualTo(1));
        await using (var db = CreateContext())
        {
            var persisted = await db.Inventories.FindAsync(inventory.Id);
            Assert.That(persisted!.Name, Is.EqualTo("Pantry"));
        }
        #endregion
    }

    [Test]
    public async Task UpdateAsync_NonExistentId_ReturnsNull()
    {
        #region Arrange
        await using var db = CreateContext();
        var dto = new UpdateInventoryDto("X", RowVersion: 0);
        #endregion

        #region Act
        var result = await new InventoryRepository(db).UpdateAsync(Guid.NewGuid(), dto);
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    public async Task UpdateAsync_StaleRowVersion_ThrowsDbUpdateConcurrencyException()
    {
        #region Arrange
        var inventory = MakeInventory(Guid.NewGuid());
        await using (var db = CreateContext())
        {
            db.Inventories.Add(inventory);
            await db.SaveChangesAsync();
        }

        // Simulate concurrent write: bump RowVersion to 1
        await using (var db = CreateContext())
        {
            var concurrent = await db.Inventories.FindAsync(inventory.Id);
            concurrent!.RowVersion = 1;
            await db.SaveChangesAsync();
        }

        var dto = new UpdateInventoryDto("Stale", RowVersion: 0);
        #endregion

        #region Act & Assert
        await using (var db = CreateContext())
        {
            Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                new InventoryRepository(db).UpdateAsync(inventory.Id, dto));
        }
        #endregion
    }

    [Test]
    public async Task DeleteAsync_ExistingInventory_ReturnsTrueAndRemovesFromDatabase()
    {
        #region Arrange
        bool deleted;
        var inventory = MakeInventory(Guid.NewGuid());
        await using (var db = CreateContext())
        {
            db.Inventories.Add(inventory);
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            deleted = await new InventoryRepository(db).DeleteAsync(inventory.Id);
        }
        #endregion

        #region Assert
        Assert.That(deleted, Is.True);
        await using (var db = CreateContext())
        {
            Assert.That(await db.Inventories.FindAsync(inventory.Id), Is.Null);
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
        var result = await new InventoryRepository(db).DeleteAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.False);
        #endregion
    }
}
