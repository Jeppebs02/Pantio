using Microsoft.Extensions.Logging;
using Moq;
using PantioAPI.Services;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ServiceTests;

public class ExpiryDateServiceTests
{
    private Mock<IExpiryDateRepository> _expiryRepoMock = null!;
    private Mock<IInventoryItemRepository> _itemRepoMock = null!;
    private Mock<IProductCategoryRepository> _categoryRepoMock = null!;
    private Mock<IInventoryItemCacheService> _inventoryItemCacheServiceMock = null!;
    private ExpiryDateService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _expiryRepoMock = new Mock<IExpiryDateRepository>();
        _itemRepoMock = new Mock<IInventoryItemRepository>();
        _categoryRepoMock = new Mock<IProductCategoryRepository>();
        _inventoryItemCacheServiceMock = new Mock<IInventoryItemCacheService>();
        _inventoryItemCacheServiceMock
            .Setup(c => c.InvalidateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _service = new ExpiryDateService(
            _expiryRepoMock.Object,
            _itemRepoMock.Object,
            _categoryRepoMock.Object,
            _inventoryItemCacheServiceMock.Object,
            Mock.Of<ILogger<ExpiryDateService>>());
    }

    private static InventoryItem MakeItem(string? offTag = null, int? categoryId = null) => new()
    {
        Id = Guid.NewGuid(),
        InventoryId = Guid.NewGuid(),
        ProductName = "Milk",
        Quantity = 1m,
        OffTag = offTag,
        CategoryId = categoryId,
        Status = InventoryStatus.Available,
        AddedVia = AddedVia.Manual,
        AddedAt = DateTime.UtcNow.AddDays(-3),
        UpdatedAt = DateTime.UtcNow,
        RowVersion = 0
    };

    private static ExpiryDate MakeExpiryDate(Guid itemId, DateOnly overrideDate) => new()
    {
        Id = Guid.NewGuid(),
        InventoryItemId = itemId,
        EstimatedExpiry = overrideDate,
        IsManualOverride = true,
        OverrideDate = overrideDate
    };

    [Test]
    public async Task SetOverrideAsync_ExistingItem_ReturnsMappedDto()
    {
        #region Arrange
        var item = MakeItem();
        var overrideDate = new DateOnly(2026, 6, 1);
        var expiryEntity = MakeExpiryDate(item.Id, overrideDate);
        _itemRepoMock
            .Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _expiryRepoMock
            .Setup(r => r.SetOverrideAsync(item.Id, overrideDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiryEntity);
        #endregion

        #region Act
        var result = await _service.SetOverrideAsync(item.Id, new UpdateExpiryDateDto(overrideDate));
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsManualOverride, Is.True);
        Assert.That(result.OverrideDate, Is.EqualTo(overrideDate));
        #endregion
    }

    [Test]
    public async Task SetOverrideAsync_NonExistentItem_ReturnsNull()
    {
        #region Arrange
        _itemRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);
        #endregion

        #region Act
        var result = await _service.SetOverrideAsync(Guid.NewGuid(), new UpdateExpiryDateDto(new DateOnly(2026, 6, 1)));
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        _expiryRepoMock.Verify(
            r => r.SetOverrideAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
        #endregion
    }

    [Test]
    public async Task SetOverrideAsync_ItemWithOffTagAndNoCategory_LearnsCategoryFromUserInput()
    {
        #region Arrange
        var item = MakeItem(offTag: "en:whole-milks");
        var overrideDate = DateOnly.FromDateTime(item.AddedAt.AddDays(7));
        var learnedCategory = new ProductCategory { Id = 25, OffTag = "en:whole-milks", DisplayName = "Whole milks", DefaultShelfLifeDays = 7 };
        _itemRepoMock
            .Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _categoryRepoMock
            .Setup(r => r.CreateIfNotExistsAsync("en:whole-milks", 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(learnedCategory);
        _expiryRepoMock
            .Setup(r => r.SetOverrideAsync(item.Id, overrideDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeExpiryDate(item.Id, overrideDate));
        #endregion

        #region Act
        await _service.SetOverrideAsync(item.Id, new UpdateExpiryDateDto(overrideDate));
        #endregion

        #region Assert
        _categoryRepoMock.Verify(
            r => r.CreateIfNotExistsAsync("en:whole-milks", 7, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(item.Category, Is.EqualTo(learnedCategory));
        #endregion
    }

    [Test]
    public async Task SetOverrideAsync_ItemWithOffTagAlreadyHasCategory_DoesNotLearn()
    {
        #region Arrange
        var item = MakeItem(offTag: "en:whole-milks", categoryId: 3);
        var overrideDate = new DateOnly(2026, 6, 1);
        _itemRepoMock
            .Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _expiryRepoMock
            .Setup(r => r.SetOverrideAsync(item.Id, overrideDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeExpiryDate(item.Id, overrideDate));
        #endregion

        #region Act
        await _service.SetOverrideAsync(item.Id, new UpdateExpiryDateDto(overrideDate));
        #endregion

        #region Assert
        _categoryRepoMock.Verify(
            r => r.CreateIfNotExistsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        #endregion
    }

    [Test]
    public async Task SetOverrideAsync_ItemWithNoOffTag_DoesNotLearn()
    {
        #region Arrange
        var item = MakeItem();
        var overrideDate = new DateOnly(2026, 6, 1);
        _itemRepoMock
            .Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _expiryRepoMock
            .Setup(r => r.SetOverrideAsync(item.Id, overrideDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeExpiryDate(item.Id, overrideDate));
        #endregion

        #region Act
        await _service.SetOverrideAsync(item.Id, new UpdateExpiryDateDto(overrideDate));
        #endregion

        #region Assert
        _categoryRepoMock.Verify(
            r => r.CreateIfNotExistsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        #endregion
    }

    [Test]
    public async Task SetOverrideAsync_OverrideDateBeforeAddedAt_DoesNotLearn()
    {
        #region Arrange
        var item = MakeItem(offTag: "en:whole-milks");
        // override date is before AddedAt → shelfLifeDays would be negative
        var overrideDate = DateOnly.FromDateTime(item.AddedAt.AddDays(-1));
        _itemRepoMock
            .Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _expiryRepoMock
            .Setup(r => r.SetOverrideAsync(item.Id, overrideDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeExpiryDate(item.Id, overrideDate));
        #endregion

        #region Act
        await _service.SetOverrideAsync(item.Id, new UpdateExpiryDateDto(overrideDate));
        #endregion

        #region Assert
        _categoryRepoMock.Verify(
            r => r.CreateIfNotExistsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        #endregion
    }
}
