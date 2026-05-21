using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PantioAPI.Services;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Exceptions;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ServiceTests;

public class InventoryItemServiceTests
{
    private Mock<IInventoryItemRepository> _repositoryMock = null!;
    private Mock<IProductCategoryRepository> _categoryRepoMock = null!;
    private Mock<IOpenFoodFactsService> _offServiceMock = null!;
    private Mock<IProductCacheService> _cacheServiceMock = null!;
    private Mock<IProductCacheDbRepository> _productCacheDbRepoMock = null!;
    private Mock<IInventoryItemCacheService> _inventoryItemCacheServiceMock = null!;
    private InventoryItemService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IInventoryItemRepository>();
        _categoryRepoMock = new Mock<IProductCategoryRepository>();
        _offServiceMock = new Mock<IOpenFoodFactsService>();
        _cacheServiceMock = new Mock<IProductCacheService>();
        _productCacheDbRepoMock = new Mock<IProductCacheDbRepository>();
        _inventoryItemCacheServiceMock = new Mock<IInventoryItemCacheService>();
        _inventoryItemCacheServiceMock
            .Setup(c => c.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IEnumerable<InventoryItemDto>?>(null));
        _inventoryItemCacheServiceMock
            .Setup(c => c.SetAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<InventoryItemDto>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _inventoryItemCacheServiceMock
            .Setup(c => c.InvalidateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _service = new InventoryItemService(
            _repositoryMock.Object,
            _categoryRepoMock.Object,
            _offServiceMock.Object,
            _cacheServiceMock.Object,
            _productCacheDbRepoMock.Object,
            _inventoryItemCacheServiceMock.Object,
            Mock.Of<ILogger<InventoryItemService>>());
    }

    private static InventoryItem MakeEntity(Guid inventoryId) => new()
    {
        Id = Guid.NewGuid(),
        InventoryId = inventoryId,
        ProductName = "Milk",
        Quantity = 1m,
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
        var userId = Guid.NewGuid();
        var dto = new CreateInventoryItemDto("Milk", 1m, QuantityUnit.l, "5701234567890", null, AddedVia.Manual);
        var entity = MakeEntity(inventoryId);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        #endregion

        #region Act
        var result = await _service.CreateAsync(inventoryId, userId, dto);
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
        var userId = Guid.NewGuid();
        var dto = new CreateInventoryItemDto("Milk", 1m, null, "5701234567890", null, AddedVia.Manual);
        InventoryItem? captured = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryItem, CancellationToken>((item, _) => captured = item)
            .ReturnsAsync((InventoryItem item, CancellationToken _) => item);
        #endregion

        #region Act
        await _service.CreateAsync(inventoryId, userId, dto);
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
        var dto = new UpdateInventoryItemDto("Yoghurt", 2m, null, "Køleskab", InventoryStatus.Low, RowVersion: 0);
        var updated = new InventoryItem
        {
            Id = Guid.NewGuid(),
            InventoryId = inventoryId,
            ProductName = "Yoghurt",
            Quantity = 2m,
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
        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateInventoryItemDto("X", 1m, null, null, InventoryStatus.Available, 0));
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
            _service.UpdateAsync(Guid.NewGuid(), new UpdateInventoryItemDto("X", 1m, null, null, InventoryStatus.Available, 0)));
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
        var result = await _service.DeleteAsync(Guid.NewGuid(), id);
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
        var result = await _service.DeleteAsync(Guid.NewGuid(), Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.False);
        #endregion
    }

    [Test]
    public async Task CreateAsync_EanInRedisCache_DoesNotCallOffApi()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new CreateInventoryItemDto("Milk", 1m, QuantityUnit.l, "5701234567890", null, AddedVia.Manual);
        var cachedData = new OffProductData("Arla Letmælk", ["en:milks", "en:dairy"], null);
        _cacheServiceMock
            .Setup(c => c.GetAsync("5701234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedData);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem item, CancellationToken _) => item);
        #endregion

        #region Act
        await _service.CreateAsync(inventoryId, userId, dto);
        #endregion

        #region Assert
        _offServiceMock.Verify(o => o.GetByEanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }

    [Test]
    public async Task CreateAsync_EanNotInCache_CallsOffApiAndWritesCache()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new CreateInventoryItemDto("Milk", 1m, QuantityUnit.l, "5701234567890", null, AddedVia.Manual);
        var offData = new OffProductData("Arla Letmælk", ["en:milks", "en:dairy"], null);
        _cacheServiceMock
            .Setup(c => c.GetAsync("5701234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync((OffProductData?)null);
        _offServiceMock
            .Setup(o => o.GetByEanAsync("5701234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(offData);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem item, CancellationToken _) => item);
        #endregion

        #region Act
        await _service.CreateAsync(inventoryId, userId, dto);
        #endregion

        #region Assert
        _cacheServiceMock.Verify(
            c => c.SetAsync("5701234567890", offData, It.IsAny<CancellationToken>()), Times.Once);
        #endregion
    }

    [Test]
    public async Task CreateAsync_OffDataWithMatchingCategory_SetsProductNameAndExpiry()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new CreateInventoryItemDto("Unknown", 1m, QuantityUnit.l, "5701234567890", null, AddedVia.Manual);
        var offData = new OffProductData("Arla Letmælk", ["en:milks", "en:dairy"], null);
        var category = new ProductCategory { Id = 3, OffTag = "en:milks", DisplayName = "Mælk", DefaultShelfLifeDays = 7 };
        _cacheServiceMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(offData);
        _categoryRepoMock
            .Setup(r => r.GetFirstMatchingTagAsync(offData.CategoryTags, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        InventoryItem? captured = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryItem, CancellationToken>((item, _) => captured = item)
            .ReturnsAsync((InventoryItem item, CancellationToken _) => item);
        #endregion

        #region Act
        await _service.CreateAsync(inventoryId, userId, dto);
        #endregion

        #region Assert
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.ProductName, Is.EqualTo("Arla Letmælk"));
        Assert.That(captured.CategoryId, Is.EqualTo(3));
        Assert.That(captured.ExpiryDate, Is.Not.Null);
        Assert.That(captured.ExpiryDate!.CategoryDefaultUsedDays, Is.EqualTo(7));
        #endregion
    }

    [Test]
    public async Task CreateAsync_WithCategoryId_AttachesExpiryDateBasedOnShelfLife()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new CreateInventoryItemDto("Milk", 1m, QuantityUnit.l, "5701234567890", null, AddedVia.Manual, CategoryId: 1);
        var category = new ProductCategory { Id = 1, OffTag = "en:dairy", DisplayName = "Dairy", DefaultShelfLifeDays = 7 };
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        InventoryItem? captured = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryItem, CancellationToken>((item, _) => captured = item)
            .ReturnsAsync((InventoryItem item, CancellationToken _) => item);
        #endregion

        #region Act
        await _service.CreateAsync(inventoryId, userId, dto);
        #endregion

        #region Assert
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.ExpiryDate, Is.Not.Null);
        Assert.That(captured.ExpiryDate!.IsManualOverride, Is.False);
        Assert.That(captured.ExpiryDate.CategoryDefaultUsedDays, Is.EqualTo(7));
        Assert.That(captured.ExpiryDate.EstimatedExpiry,
            Is.EqualTo(DateOnly.FromDateTime(captured.AddedAt.AddDays(7))));
        #endregion
    }

    [Test]
    public async Task CreateAsync_WithManualExpiryDate_AttachesManualOverride()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var overrideDate = new DateOnly(2026, 12, 31);
        var dto = new CreateInventoryItemDto("Juice", 1m, QuantityUnit.l, "5701234567890", null, AddedVia.Manual, ManualExpiryDate: overrideDate);
        InventoryItem? captured = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryItem, CancellationToken>((item, _) => captured = item)
            .ReturnsAsync((InventoryItem item, CancellationToken _) => item);
        #endregion

        #region Act
        await _service.CreateAsync(inventoryId, userId, dto);
        #endregion

        #region Assert
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.ExpiryDate, Is.Not.Null);
        Assert.That(captured.ExpiryDate!.IsManualOverride, Is.True);
        Assert.That(captured.ExpiryDate.OverrideDate, Is.EqualTo(overrideDate));
        Assert.That(captured.ExpiryDate.EstimatedExpiry, Is.EqualTo(overrideDate));
        #endregion
    }

    [Test]
    public async Task CreateAsync_NoCategoryAndNoManualDate_ExpiryDateIsNull()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new CreateInventoryItemDto("Unknown Item", 1m, null, "5701234567890", null, AddedVia.Manual);
        InventoryItem? captured = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryItem, CancellationToken>((item, _) => captured = item)
            .ReturnsAsync((InventoryItem item, CancellationToken _) => item);
        #endregion

        #region Act
        await _service.CreateAsync(inventoryId, userId, dto);
        #endregion

        #region Assert
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.ExpiryDate, Is.Null);
        #endregion
    }

    [Test]
    public async Task GetByInventoryIdAsync_CacheHit_SkipsRepository()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var cached = new List<InventoryItemDto>();
        _inventoryItemCacheServiceMock
            .Setup(c => c.GetAsync(inventoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);
        #endregion

        #region Act
        var result = await _service.GetByInventoryIdAsync(inventoryId);
        #endregion

        #region Assert
        _repositoryMock.Verify(r => r.GetByInventoryIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(result, Is.SameAs(cached));
        #endregion
    }

    [Test]
    public async Task GetByInventoryIdAsync_CacheMiss_PopulatesCache()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByInventoryIdAsync(inventoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { MakeEntity(inventoryId) });
        #endregion

        #region Act
        await _service.GetByInventoryIdAsync(inventoryId);
        #endregion

        #region Assert
        _inventoryItemCacheServiceMock.Verify(
            c => c.SetAsync(inventoryId, It.IsAny<IEnumerable<InventoryItemDto>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        #endregion
    }

    [Test]
    public async Task CreateAsync_Always_InvalidatesInventoryCache()
    {
        #region Arrange
        var inventoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new CreateInventoryItemDto("Milk", 1m, null, string.Empty, null, AddedVia.Manual);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem item, CancellationToken _) => item);
        #endregion

        #region Act
        await _service.CreateAsync(inventoryId, userId, dto);
        #endregion

        #region Assert
        _inventoryItemCacheServiceMock.Verify(
            c => c.InvalidateAsync(inventoryId, It.IsAny<CancellationToken>()), Times.Once);
        #endregion
    }

    [Test]
    public async Task UpdateAsync_ExistingItem_InvalidatesInventoryCache()
    {
        #region Arrange
        var itemId = Guid.NewGuid();
        var inventoryId = Guid.NewGuid();
        var dto = new UpdateInventoryItemDto("Milk", 1m, null, null, InventoryStatus.Available, 0);
        _repositoryMock
            .Setup(r => r.UpdateAsync(itemId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItem
            {
                Id = itemId,
                InventoryId = inventoryId,
                ProductName = "Milk",
                Quantity = 1m,
                Status = InventoryStatus.Available,
                AddedVia = AddedVia.Manual,
                AddedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RowVersion = 1
            });
        #endregion

        #region Act
        await _service.UpdateAsync(itemId, dto);
        #endregion

        #region Assert
        _inventoryItemCacheServiceMock.Verify(
            c => c.InvalidateAsync(inventoryId, It.IsAny<CancellationToken>()), Times.Once);
        #endregion
    }

    [Test]
    public async Task DeleteAsync_ExistingItem_InvalidatesInventoryCache()
    {
        #region Arrange
        var itemId = Guid.NewGuid();
        var inventoryId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.DeleteAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        #endregion

        #region Act
        await _service.DeleteAsync(inventoryId, itemId);
        #endregion

        #region Assert
        _inventoryItemCacheServiceMock.Verify(
            c => c.InvalidateAsync(inventoryId, It.IsAny<CancellationToken>()), Times.Once);
        #endregion
    }
}
