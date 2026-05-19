using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioRepository.EntityFramework;
using PantioRepository.EntityFramework.Repositories;

namespace PantioTest.RepositoryTests;

public class ShoppingListRepositoryTests
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

    private static ShoppingList MakeList(Guid? userId = null, List<ShoppingListItem>? items = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId ?? Guid.NewGuid(),
        Name = "Test List",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Items = items ?? []
    };

    private static ShoppingListItem MakeItem(string name = "Milk") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Quantity = 1m,
        MeasuringUnit = "L",
        IsChecked = false
    };

    [Test]
    public async Task CreateAsync_PersistsList()
    {
        #region Arrange
        var list = MakeList();
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new ShoppingListRepository(db).CreateAsync(list);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            Assert.That(await db.ShoppingLists.FindAsync(list.Id), Is.Not.Null);
        }
        #endregion
    }

    [Test]
    public async Task CreateAsync_WithItems_PersistsItems()
    {
        #region Arrange
        var list = MakeList(items: [MakeItem("Milk"), MakeItem("Eggs")]);
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new ShoppingListRepository(db).CreateAsync(list);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            var items = db.ShoppingListItems.Where(i => i.ShoppingListId == list.Id).ToList();
            Assert.That(items.Count, Is.EqualTo(2));
        }
        #endregion
    }

    [Test]
    public async Task GetByUserAsync_ReturnsOnlyUserLists()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var list1 = MakeList(userId);
        var list2 = MakeList(userId);
        var otherList = MakeList();

        await using (var db = CreateContext())
        {
            db.ShoppingLists.AddRange(list1, list2, otherList);
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        List<ShoppingList> result;
        await using (var db = CreateContext())
        {
            result = await new ShoppingListRepository(db).GetByUserAsync(userId);
        }
        #endregion

        #region Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(l => l.UserId == userId), Is.True);
        #endregion
    }

    [Test]
    public async Task GetByIdWithItemsAsync_IncludesItems()
    {
        #region Arrange
        var list = MakeList(items: [MakeItem("Milk")]);
        await using (var db = CreateContext())
        {
            await new ShoppingListRepository(db).CreateAsync(list);
        }
        #endregion

        #region Act
        ShoppingList? result;
        await using (var db = CreateContext())
        {
            result = await new ShoppingListRepository(db).GetByIdWithItemsAsync(list.Id);
        }
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items.Count, Is.EqualTo(1));
        Assert.That(result.Items.First().Name, Is.EqualTo("Milk"));
        #endregion
    }

    [Test]
    public async Task GetByIdWithItemsAsync_NonExistentId_ReturnsNull()
    {
        #region Arrange
        await using var db = CreateContext();
        #endregion

        #region Act
        var result = await new ShoppingListRepository(db).GetByIdWithItemsAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    public async Task DeleteAsync_ExistingList_ReturnsTrueAndRemoves()
    {
        #region Arrange
        var list = MakeList();
        await using (var db = CreateContext())
        {
            db.ShoppingLists.Add(list);
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        bool deleted;
        await using (var db = CreateContext())
        {
            deleted = await new ShoppingListRepository(db).DeleteAsync(list.Id);
        }
        #endregion

        #region Assert
        Assert.That(deleted, Is.True);
        await using (var db = CreateContext())
        {
            Assert.That(await db.ShoppingLists.FindAsync(list.Id), Is.Null);
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
        var result = await new ShoppingListRepository(db).DeleteAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.False);
        #endregion
    }

    [Test]
    public async Task AddItemAsync_PersistsItem()
    {
        #region Arrange
        var list = MakeList();
        await using (var db = CreateContext())
        {
            db.ShoppingLists.Add(list);
            await db.SaveChangesAsync();
        }

        var item = new ShoppingListItem
        {
            Id = Guid.NewGuid(),
            ShoppingListId = list.Id,
            Name = "Butter",
            Quantity = 200m,
            MeasuringUnit = "g",
            IsChecked = false
        };
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new ShoppingListRepository(db).AddItemAsync(item);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            Assert.That(await db.ShoppingListItems.FindAsync(item.Id), Is.Not.Null);
        }
        #endregion
    }

    [Test]
    public async Task FindItemByNameAsync_ExistingItem_ReturnsIt()
    {
        #region Arrange
        var item = MakeItem("Milk");
        var list = MakeList(items: [item]);
        await using (var db = CreateContext())
        {
            await new ShoppingListRepository(db).CreateAsync(list);
        }
        #endregion

        #region Act
        ShoppingListItem? result;
        await using (var db = CreateContext())
        {
            result = await new ShoppingListRepository(db).FindItemByNameAsync(list.Id, "Milk");
        }
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(item.Id));
        #endregion
    }

    [Test]
    public async Task FindItemByNameAsync_CaseInsensitive_ReturnsIt()
    {
        #region Arrange
        var item = MakeItem("Milk");
        var list = MakeList(items: [item]);
        await using (var db = CreateContext())
        {
            await new ShoppingListRepository(db).CreateAsync(list);
        }
        #endregion

        #region Act
        ShoppingListItem? result;
        await using (var db = CreateContext())
        {
            result = await new ShoppingListRepository(db).FindItemByNameAsync(list.Id, "milk");
        }
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(item.Id));
        #endregion
    }

    [Test]
    public async Task FindItemByNameAsync_DifferentList_ReturnsNull()
    {
        #region Arrange
        var item = MakeItem("Milk");
        var list = MakeList(items: [item]);
        await using (var db = CreateContext())
        {
            await new ShoppingListRepository(db).CreateAsync(list);
        }
        #endregion

        #region Act
        ShoppingListItem? result;
        await using (var db = CreateContext())
        {
            result = await new ShoppingListRepository(db).FindItemByNameAsync(Guid.NewGuid(), "Milk");
        }
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    public async Task UpdateItemAsync_PersistsChanges()
    {
        #region Arrange
        var item = MakeItem("Milk");
        var list = MakeList(items: [item]);
        await using (var db = CreateContext())
        {
            await new ShoppingListRepository(db).CreateAsync(list);
        }
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            var toUpdate = await db.ShoppingListItems.FindAsync(item.Id);
            toUpdate!.Quantity = 5m;
            await new ShoppingListRepository(db).UpdateItemAsync(toUpdate);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            var persisted = await db.ShoppingListItems.FindAsync(item.Id);
            Assert.That(persisted!.Quantity, Is.EqualTo(5f));
        }
        #endregion
    }

    [Test]
    public async Task DeleteItemAsync_RemovesItem()
    {
        #region Arrange
        var item = MakeItem();
        var list = MakeList(items: [item]);
        await using (var db = CreateContext())
        {
            await new ShoppingListRepository(db).CreateAsync(list);
        }
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new ShoppingListRepository(db).DeleteItemAsync(item.Id);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            Assert.That(await db.ShoppingListItems.FindAsync(item.Id), Is.Null);
        }
        #endregion
    }

    [Test]
    public async Task DeleteItemAsync_NonExistentId_ReturnsFalse()
    {
        #region Arrange
        await using var db = CreateContext();
        #endregion

        #region Act
        var result = await new ShoppingListRepository(db).DeleteItemAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.False);
        #endregion
    }

    [Test]
    public async Task ToggleItemAsync_FlipsIsChecked()
    {
        #region Arrange
        var item = MakeItem();
        var list = MakeList(items: [item]);
        await using (var db = CreateContext())
        {
            await new ShoppingListRepository(db).CreateAsync(list);
        }
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new ShoppingListRepository(db).ToggleItemAsync(item.Id);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            var persisted = await db.ShoppingListItems.FindAsync(item.Id);
            Assert.That(persisted!.IsChecked, Is.True);
        }
        #endregion
    }

    [Test]
    public async Task ToggleItemAsync_CalledTwice_RestoresOriginalState()
    {
        #region Arrange
        var item = MakeItem();
        var list = MakeList(items: [item]);
        await using (var db = CreateContext())
        {
            await new ShoppingListRepository(db).CreateAsync(list);
        }
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            var repo = new ShoppingListRepository(db);
            await repo.ToggleItemAsync(item.Id);
            await repo.ToggleItemAsync(item.Id);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            var persisted = await db.ShoppingListItems.FindAsync(item.Id);
            Assert.That(persisted!.IsChecked, Is.False);
        }
        #endregion
    }

    [Test]
    public async Task ToggleItemAsync_NonExistentId_ReturnsNull()
    {
        #region Arrange
        await using var db = CreateContext();
        #endregion

        #region Act
        var result = await new ShoppingListRepository(db).ToggleItemAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }
}
