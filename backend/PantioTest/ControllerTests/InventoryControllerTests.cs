using Microsoft.AspNetCore.Mvc;
using Moq;
using PantioAPI.Controllers;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ControllerTests;

public class InventoryControllerTests
{
    private Mock<IInventoryService> _serviceMock = null!;
    private InventoryController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IInventoryService>();
        _controller = new InventoryController(_serviceMock.Object);
    }

    [Test]
    public async Task Create_ValidDto_Returns201WithCreatedInventory()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateInventoryDto("Fridge");
        var created = new InventoryDto(Guid.NewGuid(), userId, "Fridge");
        _serviceMock
            .Setup(s => s.CreateAsync(userId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);
        #endregion

        #region Act
        var result = await _controller.Create(userId, dto, CancellationToken.None);
        #endregion

        #region Assert
        var createdResult = result as CreatedAtActionResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.StatusCode, Is.EqualTo(201));
        Assert.That(createdResult.Value, Is.EqualTo(created));
        #endregion
    }

    [Test]
    public async Task GetAll_UserWithInventories_ReturnsOkWithInventories()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var inventories = new[]
        {
            new InventoryDto(Guid.NewGuid(), userId, "Fridge"),
            new InventoryDto(Guid.NewGuid(), userId, "Pantry")
        };
        _serviceMock
            .Setup(s => s.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventories);
        #endregion

        #region Act
        var result = await _controller.GetAll(userId, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(inventories));
        #endregion
    }

    [Test]
    public async Task GetById_ExistingInventory_ReturnsOkWithInventory()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var inventoryDto = new InventoryDto(id, userId, "Fridge");
        _serviceMock
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryDto);
        #endregion

        #region Act
        var result = await _controller.GetById(userId, id, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(inventoryDto));
        #endregion
    }

    [Test]
    public async Task GetById_NonExistentInventory_Returns404()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryDto?)null);
        #endregion

        #region Act
        var result = await _controller.GetById(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        #endregion
    }

    [Test]
    public async Task Delete_ExistingInventory_Returns204()
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
    public async Task Delete_NonExistentInventory_Returns404()
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
