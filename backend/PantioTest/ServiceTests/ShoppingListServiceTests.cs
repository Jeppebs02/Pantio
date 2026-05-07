using Microsoft.Extensions.Logging;
using Moq;
using PantioAPI.Services;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Repository;

namespace PantioTest.ServiceTests;

public class ShoppingListServiceTests
{
    private Mock<IShoppingListRepository> _listRepoMock = null!;
    private Mock<IRecipeRepository> _recipeRepoMock = null!;
    private Mock<IInventoryItemRepository> _itemRepoMock = null!;
    private ShoppingListService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _listRepoMock = new Mock<IShoppingListRepository>();
        _recipeRepoMock = new Mock<IRecipeRepository>();
        _itemRepoMock = new Mock<IInventoryItemRepository>();
        _service = new ShoppingListService(
            _listRepoMock.Object,
            _recipeRepoMock.Object,
            _itemRepoMock.Object,
            Mock.Of<ILogger<ShoppingListService>>());
    }

    private static ShoppingList MakeList(Guid userId, List<ShoppingListItem>? items = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = "Test List",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Items = items ?? []
    };

    private static InventoryItem MakeInventoryItem(string name) => new()
    {
        Id = Guid.NewGuid(),
        InventoryId = Guid.NewGuid(),
        ProductName = name,
        Quantity = 1f,
        Status = InventoryStatus.Available,
        AddedVia = AddedVia.Manual,
        AddedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        RowVersion = 0
    };

    private static Recipe MakeRecipe(List<RecipeEntry> entries) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Name = "Pasta",
        Portions = 2f,
        CreatedAt = DateTime.UtcNow,
        Entries = entries
    };

    [Test]
    public async Task CreateAsync_CreatesAndReturnsDto()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateShoppingListDto("Uge 20");
        _listRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<ShoppingList>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingList l, CancellationToken _) => l);
        #endregion

        #region Act
        var result = await _service.CreateAsync(userId, dto);
        #endregion

        #region Assert
        Assert.That(result.Name, Is.EqualTo("Uge 20"));
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.Items, Is.Empty);
        #endregion
    }

    [Test]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var lists = new List<ShoppingList> { MakeList(userId), MakeList(userId) };
        _listRepoMock
            .Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lists);
        #endregion

        #region Act
        var result = await _service.GetAllAsync(userId);
        #endregion

        #region Assert
        Assert.That(result.Count, Is.EqualTo(2));
        #endregion
    }

    [Test]
    public async Task DeleteAsync_NonExistentList_ReturnsFalse()
    {
        #region Arrange
        _listRepoMock
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        #endregion

        #region Act
        var result = await _service.DeleteAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.False);
        #endregion
    }

    [Test]
    public async Task CreateFromRecipeAsync_NonExistentRecipe_ReturnsNull()
    {
        #region Arrange
        _recipeRepoMock
            .Setup(r => r.GetByIdWithEntriesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe?)null);
        #endregion

        #region Act
        var result = await _service.CreateFromRecipeAsync(Guid.NewGuid(), new AddFromRecipeDto(Guid.NewGuid(), Guid.NewGuid(), null));
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    public async Task CreateFromRecipeAsync_AddsOnlyMissingIngredients()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var milkItem = MakeInventoryItem("Milk");
        var entries = new List<RecipeEntry>
        {
            new() { Id = Guid.NewGuid(), ProductName = "Milk", Quantity = 1f, MeasuringUnit = "L" },
            new() { Id = Guid.NewGuid(), ProductName = "Eggs", Quantity = 3f, MeasuringUnit = "stk" }
        };
        var recipe = MakeRecipe(entries);

        _recipeRepoMock
            .Setup(r => r.GetByIdWithEntriesAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        _itemRepoMock
            .Setup(r => r.GetByInventoryIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([milkItem]);

        ShoppingList? captured = null;
        _listRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<ShoppingList>(), It.IsAny<CancellationToken>()))
            .Callback<ShoppingList, CancellationToken>((l, _) => captured = l)
            .ReturnsAsync((ShoppingList l, CancellationToken _) => l);
        #endregion

        #region Act
        var result = await _service.CreateFromRecipeAsync(userId, new AddFromRecipeDto(recipe.Id, Guid.NewGuid(), null));
        #endregion

        #region Assert
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Items.Count, Is.EqualTo(1));
        Assert.That(captured.Items.First().Name, Is.EqualTo("Eggs"));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items.Count, Is.EqualTo(1));
        #endregion
    }

    [Test]
    public async Task CreateFromRecipeAsync_AllInInventory_CreatesEmptyList()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var milkItem = MakeInventoryItem("Milk");
        var entries = new List<RecipeEntry>
        {
            new() { Id = Guid.NewGuid(), ProductName = "Milk", Quantity = 1f }
        };
        var recipe = MakeRecipe(entries);

        _recipeRepoMock
            .Setup(r => r.GetByIdWithEntriesAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        _itemRepoMock
            .Setup(r => r.GetByInventoryIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([milkItem]);
        _listRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<ShoppingList>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingList l, CancellationToken _) => l);
        #endregion

        #region Act
        var result = await _service.CreateFromRecipeAsync(userId, new AddFromRecipeDto(recipe.Id, Guid.NewGuid(), null));
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items, Is.Empty);
        #endregion
    }

    [Test]
    public async Task CreateFromRecipeAsync_UsesProvidedNameOverRecipeName()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var recipe = MakeRecipe([]);

        _recipeRepoMock
            .Setup(r => r.GetByIdWithEntriesAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        _itemRepoMock
            .Setup(r => r.GetByInventoryIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _listRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<ShoppingList>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingList l, CancellationToken _) => l);
        #endregion

        #region Act
        var result = await _service.CreateFromRecipeAsync(userId, new AddFromRecipeDto(recipe.Id, Guid.NewGuid(), "Min indkøbsliste"));
        #endregion

        #region Assert
        Assert.That(result!.Name, Is.EqualTo("Min indkøbsliste"));
        #endregion
    }

    [Test]
    public async Task CreateFromRecipeAsync_NoCustomName_UsesRecipeName()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var recipe = MakeRecipe([]);

        _recipeRepoMock
            .Setup(r => r.GetByIdWithEntriesAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        _itemRepoMock
            .Setup(r => r.GetByInventoryIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _listRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<ShoppingList>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingList l, CancellationToken _) => l);
        #endregion

        #region Act
        var result = await _service.CreateFromRecipeAsync(userId, new AddFromRecipeDto(recipe.Id, Guid.NewGuid(), null));
        #endregion

        #region Assert
        Assert.That(result!.Name, Is.EqualTo(recipe.Name));
        #endregion
    }
}
