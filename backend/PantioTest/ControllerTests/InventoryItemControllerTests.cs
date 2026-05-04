using Microsoft.AspNetCore.Mvc;
using Moq;
using PantioAPI.Controllers;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Exceptions;
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
        Guid.NewGuid(), inventoryId, "Milk", 1f, "L", "5701234567890", null,
        "Available", "Manual", DateTime.UtcNow, DateTime.UtcNow,
        null, null, 0
    );

    [Test]
    public async Task Create_ValidDto_Returns201WithCreatedItem()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var dto = new CreateInventoryItemDto("Milk", 1f, "L", "5701234567890", null, AddedVia.Manual);
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
    public async Task Update_ExistingItem_ReturnsOkWithUpdatedItem()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var dto = new UpdateInventoryItemDto("Yoghurt", 2f, "stk", null, InventoryStatus.Low, RowVersion: 0);
        var updatedDto = MakeDto(inventoryId) with { ProductName = "Yoghurt", RowVersion = 1 };
        _serviceMock
            .Setup(s => s.UpdateAsync(id, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);
        #endregion

        #region Act
        var result = await _controller.Update(inventoryId, id, dto, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(updatedDto));
        #endregion
    }

    [Test]
    public async Task Update_NonExistentItem_Returns404()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateInventoryItemDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItemDto?)null);
        #endregion

        #region Act
        var result = await _controller.Update(Guid.NewGuid(), Guid.NewGuid(),
            new UpdateInventoryItemDto("X", 1f, null, null, InventoryStatus.Available, 0),
            CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        #endregion
    }

    [Test]
    public async Task Update_ConcurrentModification_Returns409WithDanishMessage()
    {
        #region Arrange
        const string danishMessage = "Varen blev ændret af en anden operation. Hent varen igen og prøv igen.";
        _serviceMock
            .Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateInventoryItemDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException(danishMessage));
        #endregion

        #region Act
        var result = await _controller.Update(Guid.NewGuid(), Guid.NewGuid(),
            new UpdateInventoryItemDto("X", 1f, null, null, InventoryStatus.Available, 0),
            CancellationToken.None);
        #endregion

        #region Assert
        var conflict = result as ConflictObjectResult;
        Assert.That(conflict, Is.Not.Null);
        Assert.That(conflict!.StatusCode, Is.EqualTo(409));
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
