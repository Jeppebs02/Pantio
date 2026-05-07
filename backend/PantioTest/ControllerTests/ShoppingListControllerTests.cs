using Microsoft.AspNetCore.Mvc;
using Moq;
using PantioAPI.Controllers;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ControllerTests;

public class ShoppingListControllerTests
{
    private Mock<IShoppingListService> _serviceMock = null!;
    private ShoppingListController _controller = null!;
    private readonly Guid _userId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IShoppingListService>();
        _controller = new ShoppingListController(_serviceMock.Object);
    }

    private static ShoppingListDto MakeListDto(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Guid.NewGuid(), "Test List", DateTime.UtcNow, []);

    private static ShoppingListItemDto MakeItemDto() =>
        new(Guid.NewGuid(), "Milk", 1f, "L", false);

    [Test]
    public async Task GetAll_ReturnsOkWithLists()
    {
        #region Arrange
        var lists = new List<ShoppingListDto> { MakeListDto(), MakeListDto() };
        _serviceMock
            .Setup(s => s.GetAllAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lists);
        #endregion

        #region Act
        var result = await _controller.GetAll(_userId, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(lists));
        #endregion
    }

    [Test]
    public async Task Create_ValidDto_Returns201WithDto()
    {
        #region Arrange
        var listId = Guid.NewGuid();
        var dto = new CreateShoppingListDto("Uge 20");
        var created = MakeListDto(listId);
        _serviceMock
            .Setup(s => s.CreateAsync(_userId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);
        #endregion

        #region Act
        var result = await _controller.Create(_userId, dto, CancellationToken.None);
        #endregion

        #region Assert
        var created201 = result as CreatedAtActionResult;
        Assert.That(created201, Is.Not.Null);
        Assert.That(created201!.Value, Is.EqualTo(created));
        #endregion
    }

    [Test]
    public async Task Create_EmptyName_Returns400()
    {
        #region Arrange
        var dto = new CreateShoppingListDto("");
        #endregion

        #region Act
        var result = await _controller.Create(_userId, dto, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        #endregion
    }

    [Test]
    public async Task CreateFromRecipe_ExistingRecipe_Returns201WithDto()
    {
        #region Arrange
        var listId = Guid.NewGuid();
        var dto = new AddFromRecipeDto(Guid.NewGuid(), Guid.NewGuid(), null);
        var created = MakeListDto(listId);
        _serviceMock
            .Setup(s => s.CreateFromRecipeAsync(_userId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);
        #endregion

        #region Act
        var result = await _controller.CreateFromRecipe(_userId, dto, CancellationToken.None);
        #endregion

        #region Assert
        var created201 = result as CreatedAtActionResult;
        Assert.That(created201, Is.Not.Null);
        Assert.That(created201!.Value, Is.EqualTo(created));
        #endregion
    }

    [Test]
    public async Task CreateFromRecipe_NonExistentRecipe_Returns404()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.CreateFromRecipeAsync(_userId, It.IsAny<AddFromRecipeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingListDto?)null);
        #endregion

        #region Act
        var result = await _controller.CreateFromRecipe(_userId, new AddFromRecipeDto(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        #endregion
    }

    [Test]
    public async Task GetById_ExistingList_Returns200()
    {
        #region Arrange
        var listId = Guid.NewGuid();
        var dto = MakeListDto(listId);
        _serviceMock
            .Setup(s => s.GetByIdAsync(listId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        #endregion

        #region Act
        var result = await _controller.GetById(_userId, listId, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(dto));
        #endregion
    }

    [Test]
    public async Task GetById_NonExistentList_Returns404()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingListDto?)null);
        #endregion

        #region Act
        var result = await _controller.GetById(_userId, Guid.NewGuid(), CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        #endregion
    }

    [Test]
    public async Task Delete_ExistingList_Returns204()
    {
        #region Arrange
        var listId = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.DeleteAsync(listId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        #endregion

        #region Act
        var result = await _controller.Delete(_userId, listId, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
        #endregion
    }

    [Test]
    public async Task Delete_NonExistentList_Returns404()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        #endregion

        #region Act
        var result = await _controller.Delete(_userId, Guid.NewGuid(), CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        #endregion
    }

    [Test]
    public async Task AddItem_ValidDto_Returns201()
    {
        #region Arrange
        var listId = Guid.NewGuid();
        var dto = new AddShoppingListItemDto("Milk", 1f, "L");
        var itemDto = MakeItemDto();
        _serviceMock
            .Setup(s => s.AddItemAsync(listId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemDto);
        #endregion

        #region Act
        var result = await _controller.AddItem(_userId, listId, dto, CancellationToken.None);
        #endregion

        #region Assert
        var created = result as CreatedResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.Value, Is.EqualTo(itemDto));
        #endregion
    }

    [Test]
    public async Task AddItem_EmptyName_Returns400()
    {
        #region Arrange
        var dto = new AddShoppingListItemDto("", null, null);
        #endregion

        #region Act
        var result = await _controller.AddItem(_userId, Guid.NewGuid(), dto, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        #endregion
    }

    [Test]
    public async Task DeleteItem_ExistingItem_Returns204()
    {
        #region Arrange
        var listId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.DeleteItemAsync(listId, itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        #endregion

        #region Act
        var result = await _controller.DeleteItem(_userId, listId, itemId, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
        #endregion
    }

    [Test]
    public async Task DeleteItem_NonExistentItem_Returns404()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.DeleteItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        #endregion

        #region Act
        var result = await _controller.DeleteItem(_userId, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        #endregion
    }

    [Test]
    public async Task ToggleItem_ExistingItem_Returns200WithDto()
    {
        #region Arrange
        var listId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var itemDto = MakeItemDto();
        _serviceMock
            .Setup(s => s.ToggleItemAsync(listId, itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemDto);
        #endregion

        #region Act
        var result = await _controller.ToggleItem(_userId, listId, itemId, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(itemDto));
        #endregion
    }

    [Test]
    public async Task ToggleItem_NonExistentItem_Returns404()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.ToggleItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingListItemDto?)null);
        #endregion

        #region Act
        var result = await _controller.ToggleItem(_userId, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        #endregion
    }
}
