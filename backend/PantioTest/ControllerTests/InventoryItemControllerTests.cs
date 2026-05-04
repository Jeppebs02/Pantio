using Microsoft.AspNetCore.Mvc;
using Moq;
using PantioAPI.Controllers;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ControllerTests;

public class InventoryItemControllerTests
{
    private Mock<IInventoryItemService> _serviceMock = null!;
    private InventoryItemController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IInventoryItemService>();
        _controller = new InventoryItemController(_serviceMock.Object);
    }

    private static InventoryItemDto MakeDto(Guid inventoryId) => new(
        Guid.NewGuid(), inventoryId, "Milk", 1f, "L", null, null,
        "Available", "Manual", DateTime.UtcNow, DateTime.UtcNow
    );

    [Test]
    public async Task Create_ValidDto_Returns201WithCreatedItem()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var dto = new CreateInventoryItemDto("Milk", 1f, "L", null, null, AddedVia.Manual);
        var itemDto = MakeDto(inventoryId);
        _serviceMock
            .Setup(s => s.CreateAsync(inventoryId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemDto);
        #endregion

        #region Act
        var result = await _controller.Create(inventoryId, dto, CancellationToken.None);
        #endregion

        #region Assert
        var created = result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.StatusCode, Is.EqualTo(201));
        Assert.That(created.Value, Is.EqualTo(itemDto));
        #endregion
    }

    [Test]
    public async Task GetAll_InventoryWithItems_ReturnsOkWithItems()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var items = new[] { MakeDto(inventoryId), MakeDto(inventoryId) };
        _serviceMock
            .Setup(s => s.GetByInventoryIdAsync(inventoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        #endregion

        #region Act
        var result = await _controller.GetAll(inventoryId, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(items));
        #endregion
    }

    [Test]
    public async Task GetAll_EmptyInventory_ReturnsOkWithEmptyCollection()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.GetByInventoryIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        #endregion

        #region Act
        var result = await _controller.GetAll(Guid.NewGuid(), CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        #endregion
    }

    [Test]
    public async Task Delete_ExistingItem_Returns204()
    {
        #region Arrange
        var id = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        #endregion

        #region Act
        var result = await _controller.Delete(Guid.NewGuid(), id, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
        #endregion
    }

    [Test]
    public async Task Delete_NonExistentItem_Returns404()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        #endregion

        #region Act
        var result = await _controller.Delete(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        #endregion
    }
}
