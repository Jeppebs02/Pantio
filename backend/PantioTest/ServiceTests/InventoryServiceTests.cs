using Microsoft.Extensions.Logging;
using Moq;
using PantioAPI.Services;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;

namespace PantioTest.ServiceTests;

public class InventoryServiceTests
{
    private Mock<IInventoryRepository> _repositoryMock = null!;
    private InventoryService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IInventoryRepository>();
        _service = new InventoryService(_repositoryMock.Object, Mock.Of<ILogger<InventoryService>>());
    }

    [Test]
    public async Task CreateAsync_ValidInput_ReturnsCorrectDto()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateInventoryDto("Fridge");
        var entity = new Inventory { Id = Guid.NewGuid(), UserId = userId, Name = "Fridge" };
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Inventory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        #endregion

        #region Act
        var result = await _service.CreateAsync(userId, dto);
        #endregion

        #region Assert
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.Name, Is.EqualTo("Fridge"));
        #endregion
    }

    [Test]
    public async Task CreateAsync_ValidInput_CallsRepositoryWithCorrectUserIdAndName()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateInventoryDto("Pantry");
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Inventory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory inv, CancellationToken _) => inv);
        #endregion

        #region Act
        await _service.CreateAsync(userId, dto);
        #endregion

        #region Assert
        _repositoryMock.Verify(
            r => r.CreateAsync(
                It.Is<Inventory>(i => i.UserId == userId && i.Name == "Pantry"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        #endregion
    }

    [Test]
    public async Task GetByUserIdAsync_UserWithMultipleInventories_ReturnsMappedDtos()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var inventories = new List<Inventory>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Name = "Fridge" },
            new() { Id = Guid.NewGuid(), UserId = userId, Name = "Pantry" }
        };
        _repositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventories);
        #endregion

        #region Act
        var result = await _service.GetByUserIdAsync(userId);
        #endregion

        #region Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.Select(i => i.Name), Is.EquivalentTo(new[] { "Fridge", "Pantry" }));
        #endregion
    }

    [Test]
    public async Task GetByIdAsync_ExistingInventory_ReturnsDto()
    {
        #region Arrange
        var id = Guid.NewGuid();
        var inventory = new Inventory { Id = id, UserId = Guid.NewGuid(), Name = "Fridge" };
        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);
        #endregion

        #region Act
        var result = await _service.GetByIdAsync(id);
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(id));
        Assert.That(result.Name, Is.EqualTo("Fridge"));
        #endregion
    }

    [Test]
    public async Task GetByIdAsync_NonExistentInventory_ReturnsNull()
    {
        #region Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory?)null);
        #endregion

        #region Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    public async Task DeleteAsync_ExistingInventory_ReturnsTrue()
    {
        #region Arrange
        var id = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        #endregion

        #region Act
        var result = await _service.DeleteAsync(id);
        #endregion

        #region Assert
        Assert.That(result, Is.True);
        #endregion
    }

    [Test]
    public async Task DeleteAsync_NonExistentInventory_ReturnsFalse()
    {
        #region Arrange
        _repositoryMock
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
}
