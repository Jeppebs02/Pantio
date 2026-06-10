using Microsoft.AspNetCore.Mvc;
using Moq;
using PantioAPI.Controllers;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Exceptions;
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

    private static InventoryDto MakeDto(Guid userId, string name = "Fridge") =>
        new(Guid.NewGuid(), userId, name, RowVersion: 0);

    [Test]
    [Category("LA-02")]
    public async Task Create_ValidDto_Returns201WithCreatedInventory()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateInventoryDto("Fridge");
        var created = MakeDto(userId);
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
    [Category("LA-01")]
    public async Task GetAll_UserWithInventories_ReturnsOkWithInventories()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var inventories = new[] { MakeDto(userId, "Fridge"), MakeDto(userId, "Pantry") };
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
    [Category("LA-01")]
    public async Task GetById_ExistingInventory_ReturnsOkWithInventory()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var inventoryDto = new InventoryDto(id, userId, "Fridge", RowVersion: 0);
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
    [Category("LA-01")]
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
    [Category("LA-03")]
    public async Task Update_ExistingInventory_ReturnsOkWithUpdatedInventory()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var dto = new UpdateInventoryDto("Pantry", RowVersion: 0);
        var updated = new InventoryDto(id, userId, "Pantry", RowVersion: 1);
        _serviceMock
            .Setup(s => s.UpdateAsync(id, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);
        #endregion

        #region Act
        var result = await _controller.Update(userId, id, dto, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(updated));
        #endregion
    }

    [Test]
    [Category("LA-03")]
    public async Task Update_NonExistentInventory_Returns404()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateInventoryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryDto?)null);
        #endregion

        #region Act
        var result = await _controller.Update(Guid.NewGuid(), Guid.NewGuid(),
            new UpdateInventoryDto("X", RowVersion: 0), CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        #endregion
    }

    [Test]
    [Category("IK-04")]
    public async Task Update_ConcurrentModification_Returns409WithDanishMessage()
    {
        #region Arrange
        const string danishMessage = "Inventaret blev ændret af en anden operation. Hent inventaret igen og prøv igen.";
        _serviceMock
            .Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateInventoryDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException(danishMessage));
        #endregion

        #region Act
        var result = await _controller.Update(Guid.NewGuid(), Guid.NewGuid(),
            new UpdateInventoryDto("X", RowVersion: 0), CancellationToken.None);
        #endregion

        #region Assert
        var conflict = result as ConflictObjectResult;
        Assert.That(conflict, Is.Not.Null);
        Assert.That(conflict!.StatusCode, Is.EqualTo(409));
        #endregion
    }

    [Test]
    [Category("LA-02")]
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
    [Category("LA-02")]
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
