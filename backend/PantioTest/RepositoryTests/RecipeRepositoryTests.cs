using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioRepository.EntityFramework;
using PantioRepository.EntityFramework.Repositories;

namespace PantioTest.RepositoryTests;

public class RecipeRepositoryTests
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

    private static Recipe MakeRecipe(Guid? batchId = null, List<RecipeEntry>? entries = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Name = "Pasta Bolognese",
        Description = "Classic Italian",
        Instructions = "1. Cook pasta\n2. Make sauce",
        Portions = 4f,
        SuggestionBatchId = batchId,
        CreatedAt = DateTime.UtcNow,
        Entries = entries ?? []
    };

    private static RecipeEntry MakeEntry(string productName = "Pasta") => new()
    {
        Id = Guid.NewGuid(),
        ProductName = productName,
        Quantity = 200m,
        MeasuringUnit = "g"
    };

    [Test]
    public async Task CreateAsync_ValidRecipe_PersistsToDatabase()
    {
        #region Arrange
        var recipe = MakeRecipe();
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new RecipeRepository(db).CreateAsync(recipe);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            Assert.That(await db.Recipes.FindAsync(recipe.Id), Is.Not.Null);
        }
        #endregion
    }

    [Test]
    public async Task CreateAsync_RecipeWithEntries_PersistsEntriesToDatabase()
    {
        #region Arrange
        var recipe = MakeRecipe(entries: [MakeEntry("Pasta"), MakeEntry("Tomato")]);
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new RecipeRepository(db).CreateAsync(recipe);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            var entries = db.RecipeEntries.Where(e => e.RecipeId == recipe.Id).ToList();
            Assert.That(entries.Count, Is.EqualTo(2));
        }
        #endregion
    }

    [Test]
    public async Task GetByIdWithEntriesAsync_ExistingRecipe_ReturnsRecipeWithEntries()
    {
        #region Arrange
        var recipe = MakeRecipe(entries: [MakeEntry("Pasta")]);
        await using (var db = CreateContext())
        {
            await new RecipeRepository(db).CreateAsync(recipe);
        }
        #endregion

        #region Act
        Recipe? result;
        await using (var db = CreateContext())
        {
            result = await new RecipeRepository(db).GetByIdWithEntriesAsync(recipe.Id);
        }
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(recipe.Id));
        Assert.That(result.Entries.Count, Is.EqualTo(1));
        Assert.That(result.Entries.First().ProductName, Is.EqualTo("Pasta"));
        #endregion
    }

    [Test]
    public async Task GetByIdWithEntriesAsync_NonExistentId_ReturnsNull()
    {
        #region Arrange
        await using var db = CreateContext();
        #endregion

        #region Act
        var result = await new RecipeRepository(db).GetByIdWithEntriesAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    public async Task GetBySuggestionBatchAsync_ThreeRecipesInBatch_ReturnsAllThree()
    {
        #region Arrange
        var batchId = Guid.NewGuid();
        var recipes = new[] { MakeRecipe(batchId), MakeRecipe(batchId), MakeRecipe(batchId) };
        var otherRecipe = MakeRecipe(batchId: Guid.NewGuid());

        await using (var db = CreateContext())
        {
            db.Recipes.AddRange(recipes);
            db.Recipes.Add(otherRecipe);
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        IEnumerable<Recipe> result;
        await using (var db = CreateContext())
        {
            result = await new RecipeRepository(db).GetBySuggestionBatchAsync(batchId);
        }
        #endregion

        #region Assert
        Assert.That(result.Count(), Is.EqualTo(3));
        Assert.That(result.All(r => r.SuggestionBatchId == batchId), Is.True);
        #endregion
    }

    [Test]
    public async Task SetCompletedAsync_ExistingRecipe_SetsCompletedAt()
    {
        #region Arrange
        var recipe = MakeRecipe();
        await using (var db = CreateContext())
        {
            db.Recipes.Add(recipe);
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new RecipeRepository(db).SetCompletedAsync(recipe.Id);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            var persisted = await db.Recipes.FindAsync(recipe.Id);
            Assert.That(persisted!.CompletedAt, Is.Not.Null);
        }
        #endregion
    }

    [Test]
    public async Task SetCompletedAsync_NonExistentId_ReturnsNull()
    {
        #region Arrange
        await using var db = CreateContext();
        #endregion

        #region Act
        var result = await new RecipeRepository(db).SetCompletedAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    public async Task DeleteAsync_ExistingRecipe_ReturnsTrueAndRemovesFromDatabase()
    {
        #region Arrange
        var recipe = MakeRecipe();
        await using (var db = CreateContext())
        {
            db.Recipes.Add(recipe);
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        bool deleted;
        await using (var db = CreateContext())
        {
            deleted = await new RecipeRepository(db).DeleteAsync(recipe.Id);
        }
        #endregion

        #region Assert
        Assert.That(deleted, Is.True);
        await using (var db = CreateContext())
        {
            Assert.That(await db.Recipes.FindAsync(recipe.Id), Is.Null);
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
        var result = await new RecipeRepository(db).DeleteAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.False);
        #endregion
    }

    [Test]
    public async Task ClearInventoryLinksAsync_NullsAllInventoryItemIds()
    {
        #region Arrange
        var inventoryItemId = Guid.NewGuid();
        var entry1 = MakeEntry("Pasta");
        entry1.InventoryItemId = inventoryItemId;
        var entry2 = MakeEntry("Tomato");
        entry2.InventoryItemId = inventoryItemId;
        var recipe = MakeRecipe(entries: [entry1, entry2]);

        await using (var db = CreateContext())
        {
            await new RecipeRepository(db).CreateAsync(recipe);
        }
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new RecipeRepository(db).ClearInventoryLinksAsync(recipe.Id);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            var entries = db.RecipeEntries.Where(e => e.RecipeId == recipe.Id).ToList();
            Assert.That(entries.All(e => e.InventoryItemId == null), Is.True);
        }
        #endregion
    }

    [Test]
    public async Task UpdateEntryLinksAsync_SetsLinksForMatchingEntries()
    {
        #region Arrange
        var entry = MakeEntry("Pasta");
        var recipe = MakeRecipe(entries: [entry]);
        var inventoryItemId = Guid.NewGuid();

        await using (var db = CreateContext())
        {
            await new RecipeRepository(db).CreateAsync(recipe);
        }
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            var links = new Dictionary<Guid, Guid?> { [entry.Id] = inventoryItemId };
            await new RecipeRepository(db).UpdateEntryLinksAsync(recipe.Id, links);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            var persisted = db.RecipeEntries.First(e => e.Id == entry.Id);
            Assert.That(persisted.InventoryItemId, Is.EqualTo(inventoryItemId));
        }
        #endregion
    }

    [Test]
    public async Task UpdateEntryLinksAsync_DoesNotAffectOtherRecipes()
    {
        #region Arrange
        var entry = MakeEntry("Pasta");
        var otherEntry = MakeEntry("Rice");
        var recipe = MakeRecipe(entries: [entry]);
        var otherRecipe = MakeRecipe(entries: [otherEntry]);
        var inventoryItemId = Guid.NewGuid();

        await using (var db = CreateContext())
        {
            await new RecipeRepository(db).CreateAsync(recipe);
            await new RecipeRepository(db).CreateAsync(otherRecipe);
        }
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            var links = new Dictionary<Guid, Guid?> { [entry.Id] = inventoryItemId };
            await new RecipeRepository(db).UpdateEntryLinksAsync(recipe.Id, links);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            var other = db.RecipeEntries.First(e => e.Id == otherEntry.Id);
            Assert.That(other.InventoryItemId, Is.Null);
        }
        #endregion
    }
}
