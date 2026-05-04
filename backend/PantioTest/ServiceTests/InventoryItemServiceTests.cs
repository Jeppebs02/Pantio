using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PantioAPI.Services;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Exceptions;
using PantioClassLibrary.Interfaces.Repository;

namespace PantioTest.ServiceTests;

public class InventoryItemServiceTests
{
    private Mock<IInventoryItemRepository> _repositoryMock = null!;
    private InventoryItemService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IInventoryItemRepository>();
        _service = new InventoryItemService(_repositoryMock.Object, Mock.Of<ILogger<InventoryItemService>>());
    }

    private static InventoryItem MakeEntity(Guid inventoryId) => new()
    {
        Id = Guid.NewGuid(),
        InventoryId = inventoryId,
        ProductName = "Milk",
        Quantity = 1f,
        Status = InventoryStatus.Available,
        AddedVia = AddedVia.Manual,
        AddedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        RowVersion = 0
    };

    [Test]
    public async Task CreateAsync_ValidInput_ReturnsCorrectDto()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var dto = new CreateInventoryItemDto("Milk", 1f, "L", "5701234567890", null, AddedVia.Manual);
        var entity = MakeEntity(inventoryId);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        #endregion

        #region Act
        var result = await _service.CreateAsync(inventoryId, dto);
        #endregion

        #region Assert
        Assert.That(result.InventoryId, Is.EqualTo(inventoryId));
        Assert.That(result.ProductName, Is.EqualTo("Milk"));
        #endregion
    }

    [Test]
    public async Task CreateAsync_ValidInput_SetsStatusToAvailable()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var dto = new CreateInventoryItemDto("Milk", 1f, null, "5701234567890", null, AddedVia.Manual);
        InventoryItem? captured = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryItem, CancellationToken>((item, _) => captured = item)
            .ReturnsAsync((InventoryItem item, CancellationToken _) => item);
        #endregion

        #region Act
        await _service.CreateAsync(inventoryId, dto);
        #endregion

        #region Assert
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Status, Is.EqualTo(InventoryStatus.Available));
        #endregion
    }

    [Test]
    public async Task GetByInventoryIdAsync_InventoryWithItems_ReturnsMappedDtos()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var items = new[] { MakeEntity(inventoryId), MakeEntity(inventoryId) };
        _repositoryMock
            .Setup(r => r.GetByInventoryIdAsync(inventoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        #endregion

        #region Act
        var result = await _service.GetByInventoryIdAsync(inventoryId);
        #endregion

        #region Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(i => i.InventoryId == inventoryId), Is.True);
        #endregion
    }

    [Test]
    public async Task GetByInventoryIdAsync_EmptyInventory_ReturnsEmptyCollection()
    {
        #region Arrange
        _repositoryMock
            .Setup(r => r.GetByInventoryIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        #endregion

        #region Act
        var result = await _service.GetByInventoryIdAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.Empty);
        #endregion
    }

    [Test]
    public async Task UpdateAsync_ExistingItem_ReturnsMappedDto()
    {
        #region Arrange
        var id = Guid.NewGuid();
        var inventoryId = Guid.NewGuid();
        var dto = new UpdateInventoryItemDto("Yoghurt", 2f, "stk", "Køleskab", InventoryStatus.Low, RowVersion: 0);
        var updated = new InventoryItem
        {
            Id = Guid.NewGuid(),
            InventoryId = inventoryId,
            ProductName = "Yoghurt",
            Quantity = 2f,
            Status = InventoryStatus.Low,
            AddedVia = AddedVia.Manual,
            AddedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = 1
        };
        _repositoryMock
            .Setup(r => r.UpdateAsync(id, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);
        #endregion

        #region Act
        var result = await _service.UpdateAsync(id, dto);
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ProductName, Is.EqualTo("Yoghurt"));
        Assert.That(result.RowVersion, Is.EqualTo(1));
        #endregion
    }

    [Test]
    public async Task UpdateAsync_NonExistentItem_ReturnsNull()
    {
        #region Arrange
        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateInventoryItemDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);
        #endregion

        #region Act
        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateInventoryItemDto("X", 1f, null, null, InventoryStatus.Available, 0));
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    public void UpdateAsync_ConcurrentModification_ThrowsConcurrencyConflictException()
    {
        #region Arrange
        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateInventoryItemDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        #endregion

        #region Act & Assert
        Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            _service.UpdateAsync(Guid.NewGuid(), new UpdateInventoryItemDto("X", 1f, null, null, InventoryStatus.Available, 0)));
        #endregion
    }

    [Test]
    public async Task DeleteAsync_ExistingItem_ReturnsTrue()
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
    public async Task DeleteAsync_NonExistentItem_ReturnsFalse()
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
