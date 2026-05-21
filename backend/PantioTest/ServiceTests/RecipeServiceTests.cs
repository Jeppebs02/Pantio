using Microsoft.Extensions.Logging;
using Moq;
using PantioAPI.Services;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ServiceTests;

public class RecipeServiceTests
{
    private Mock<IRecipeRepository> _recipeRepoMock = null!;
    private Mock<IInventoryItemRepository> _itemRepoMock = null!;
    private RecipeService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _recipeRepoMock = new Mock<IRecipeRepository>();
        _itemRepoMock = new Mock<IInventoryItemRepository>();
        _service = new RecipeService(
            _recipeRepoMock.Object,
            _itemRepoMock.Object,
            Mock.Of<IInventoryItemCacheService>(),
            Mock.Of<ILogger<RecipeService>>());
    }

    private static InventoryItem MakeItem(decimal quantity = 5m) => new()
    {
        Id = Guid.NewGuid(),
        InventoryId = Guid.NewGuid(),
        ProductName = "Milk",
        Quantity = quantity,
        QuantityUnit = QuantityUnit.l,
        Status = InventoryStatus.Available,
        AddedVia = AddedVia.Manual,
        AddedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        RowVersion = 0
    };

    private static Recipe MakeRecipe(Guid? batchId = null, List<RecipeEntry>? entries = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Name = "Pasta",
        Portions = 2f,
        CreatedAt = DateTime.UtcNow,
        SuggestionBatchId = batchId,
        Entries = entries ?? []
    };

    [Test]
    public async Task CompleteAsync_NonExistentRecipe_ReturnsFalse()
    {
        #region Arrange
        _recipeRepoMock
            .Setup(r => r.GetByIdWithEntriesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe?)null);
        #endregion

        #region Act
        var result = await _service.CompleteAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.False);
        #endregion
    }

    [Test]
    public async Task CompleteAsync_ExistingRecipe_ReturnsTrue()
    {
        #region Arrange
        var recipe = MakeRecipe();
        _recipeRepoMock
            .Setup(r => r.GetByIdWithEntriesAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        _recipeRepoMock
            .Setup(r => r.SetCompletedAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        #endregion

        #region Act
        var result = await _service.CompleteAsync(recipe.Id);
        #endregion

        #region Assert
        Assert.That(result, Is.True);
        _recipeRepoMock.Verify(r => r.SetCompletedAsync(recipe.Id, It.IsAny<CancellationToken>()), Times.Once);
        #endregion
    }

    [Test]
    public async Task CompleteAsync_EntryWithInventoryItem_DecrementsQuantity()
    {
        #region Arrange
        var item = MakeItem(quantity: 5m);
        var entry = new RecipeEntry
        {
            Id = Guid.NewGuid(),
            ProductName = item.ProductName,
            Quantity = 2m,
            InventoryItemId = item.Id
        };
        var recipe = MakeRecipe(entries: [entry]);

        _recipeRepoMock
            .Setup(r => r.GetByIdWithEntriesAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        _recipeRepoMock
            .Setup(r => r.SetCompletedAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        _itemRepoMock
            .Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        UpdateInventoryItemDto? capturedDto = null;
        _itemRepoMock
            .Setup(r => r.UpdateAsync(item.Id, It.IsAny<UpdateInventoryItemDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, UpdateInventoryItemDto, CancellationToken>((_, dto, _) => capturedDto = dto)
            .ReturnsAsync(item);
        #endregion

        #region Act
        await _service.CompleteAsync(recipe.Id);
        #endregion

        #region Assert
        Assert.That(capturedDto, Is.Not.Null);
        Assert.That(capturedDto!.Quantity, Is.EqualTo(3m));
        _itemRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }

    [Test]
    public async Task CompleteAsync_EntryExhaustsInventoryItem_DeletesItem()
    {
        #region Arrange
        var item = MakeItem(quantity: 2m);
        var entry = new RecipeEntry
        {
            Id = Guid.NewGuid(),
            ProductName = item.ProductName,
            Quantity = 2m,
            InventoryItemId = item.Id
        };
        var recipe = MakeRecipe(entries: [entry]);

        _recipeRepoMock
            .Setup(r => r.GetByIdWithEntriesAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        _recipeRepoMock
            .Setup(r => r.SetCompletedAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        _itemRepoMock
            .Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _itemRepoMock
            .Setup(r => r.DeleteAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        #endregion

        #region Act
        await _service.CompleteAsync(recipe.Id);
        #endregion

        #region Assert
        _itemRepoMock.Verify(r => r.DeleteAsync(item.Id, It.IsAny<CancellationToken>()), Times.Once);
        _itemRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateInventoryItemDto>(), It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }

    [Test]
    public async Task CompleteAsync_EntryWithNoInventoryItemId_SkipsInventoryUpdate()
    {
        #region Arrange
        var entry = new RecipeEntry
        {
            Id = Guid.NewGuid(),
            ProductName = "Salt",
            Quantity = 1m,
            InventoryItemId = null
        };
        var recipe = MakeRecipe(entries: [entry]);

        _recipeRepoMock
            .Setup(r => r.GetByIdWithEntriesAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        _recipeRepoMock
            .Setup(r => r.SetCompletedAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        #endregion

        #region Act
        await _service.CompleteAsync(recipe.Id);
        #endregion

        #region Assert
        _itemRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }

    [Test]
    public async Task CompleteAsync_RecipeInBatch_DeletesSiblings()
    {
        #region Arrange
        var batchId = Guid.NewGuid();
        var recipe = MakeRecipe(batchId: batchId);
        var sibling1 = MakeRecipe(batchId: batchId);
        var sibling2 = MakeRecipe(batchId: batchId);

        _recipeRepoMock
            .Setup(r => r.GetByIdWithEntriesAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        _recipeRepoMock
            .Setup(r => r.SetCompletedAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        _recipeRepoMock
            .Setup(r => r.GetBySuggestionBatchAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([recipe, sibling1, sibling2]);
        _recipeRepoMock
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        #endregion

        #region Act
        await _service.CompleteAsync(recipe.Id);
        #endregion

        #region Assert
        _recipeRepoMock.Verify(r => r.DeleteAsync(sibling1.Id, It.IsAny<CancellationToken>()), Times.Once);
        _recipeRepoMock.Verify(r => r.DeleteAsync(sibling2.Id, It.IsAny<CancellationToken>()), Times.Once);
        _recipeRepoMock.Verify(r => r.DeleteAsync(recipe.Id, It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }

    [Test]
    public async Task CompleteAsync_RecipeWithNoBatch_DoesNotCallGetBySuggestionBatch()
    {
        #region Arrange
        var recipe = MakeRecipe(batchId: null);

        _recipeRepoMock
            .Setup(r => r.GetByIdWithEntriesAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        _recipeRepoMock
            .Setup(r => r.SetCompletedAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        #endregion

        #region Act
        await _service.CompleteAsync(recipe.Id);
        #endregion

        #region Assert
        _recipeRepoMock.Verify(r => r.GetBySuggestionBatchAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }

    [Test]
    public async Task LinkToInventoryAsync_NonExistentRecipe_ReturnsNull()
    {
        #region Arrange
        _recipeRepoMock
            .Setup(r => r.GetByIdWithEntriesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe?)null);
        #endregion

        #region Act
        var result = await _service.LinkToInventoryAsync(Guid.NewGuid(), Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    public async Task LinkToInventoryAsync_MatchingIngredient_SetsInventoryItemId()
    {
        #region Arrange
        var inventoryItemId = Guid.NewGuid();
        var item = MakeItem();
        item.Id = inventoryItemId;

        var entry = new RecipeEntry { Id = Guid.NewGuid(), ProductName = item.ProductName, Quantity = 1m };
        var recipe = MakeRecipe(entries: [entry]);
        var updatedRecipe = MakeRecipe(entries: [new RecipeEntry
        {
            Id = entry.Id, ProductName = entry.ProductName, Quantity = entry.Quantity,
            InventoryItemId = inventoryItemId
        }]);
        updatedRecipe.Id = recipe.Id;

        _recipeRepoMock
            .SetupSequence(r => r.GetByIdWithEntriesAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe)
            .ReturnsAsync(updatedRecipe);
        _itemRepoMock
            .Setup(r => r.GetByInventoryIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        Dictionary<Guid, Guid?>? capturedLinks = null;
        _recipeRepoMock
            .Setup(r => r.UpdateEntryLinksAsync(recipe.Id, It.IsAny<Dictionary<Guid, Guid?>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Dictionary<Guid, Guid?>, CancellationToken>((_, links, _) => capturedLinks = links)
            .Returns(Task.CompletedTask);
        #endregion

        #region Act
        await _service.LinkToInventoryAsync(recipe.Id, Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(capturedLinks, Is.Not.Null);
        Assert.That(capturedLinks![entry.Id], Is.EqualTo(inventoryItemId));
        #endregion
    }

    [Test]
    public async Task LinkToInventoryAsync_NoMatchingIngredient_LeavesInventoryItemIdNull()
    {
        #region Arrange
        var item = MakeItem();
        var entry = new RecipeEntry { Id = Guid.NewGuid(), ProductName = "SomethingCompletelyDifferent", Quantity = 1m };
        var recipe = MakeRecipe(entries: [entry]);

        _recipeRepoMock
            .SetupSequence(r => r.GetByIdWithEntriesAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe)
            .ReturnsAsync(recipe);
        _itemRepoMock
            .Setup(r => r.GetByInventoryIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        Dictionary<Guid, Guid?>? capturedLinks = null;
        _recipeRepoMock
            .Setup(r => r.UpdateEntryLinksAsync(recipe.Id, It.IsAny<Dictionary<Guid, Guid?>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Dictionary<Guid, Guid?>, CancellationToken>((_, links, _) => capturedLinks = links)
            .Returns(Task.CompletedTask);
        #endregion

        #region Act
        await _service.LinkToInventoryAsync(recipe.Id, Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(capturedLinks, Is.Not.Null);
        Assert.That(capturedLinks![entry.Id], Is.Null);
        #endregion
    }
}
