using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PantioAPI.Options;
using PantioAPI.Services;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ServiceTests;

public class ExpiryCheckServiceTests
{
    private Mock<IExpiryDateRepository> _expiryRepoMock = null!;
    private Mock<IExpiryNotificationRepository> _notificationRepoMock = null!;
    private ExpiryCheckService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _expiryRepoMock = new Mock<IExpiryDateRepository>();
        _notificationRepoMock = new Mock<IExpiryNotificationRepository>();
        var options = Options.Create(new ExpiryCheckOptions { NotificationThresholdDays = 3, IntervalHours = 4.8 });
        _service = new ExpiryCheckService(
            _expiryRepoMock.Object,
            _notificationRepoMock.Object,
            Mock.Of<IFcmService>(),
            options,
            Mock.Of<ILogger<ExpiryCheckService>>());
    }

    private static ExpiryDate MakeExpiryDate(DateOnly effectiveDate, bool isManualOverride = false) => new()
    {
        Id = Guid.NewGuid(),
        EstimatedExpiry = isManualOverride ? effectiveDate.AddDays(10) : effectiveDate,
        IsManualOverride = isManualOverride,
        OverrideDate = isManualOverride ? effectiveDate : null,
        InventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductName = "Milk",
            Quantity = 1m,
            Status = InventoryStatus.Available,
            AddedVia = AddedVia.Manual,
            AddedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Inventory = new Inventory
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Name = "Fridge"
            }
        }
    };

    [Test]
    [Category("UD-02")]
    public async Task RunCheckAsync_ItemsExpiringSoon_CreatesNotifications()
    {
        #region Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expiring = new[] { MakeExpiryDate(today.AddDays(2)), MakeExpiryDate(today.AddDays(1)) };
        _expiryRepoMock
            .Setup(r => r.GetExpiringSoonAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiring);
        IEnumerable<ExpiryNotification>? captured = null;
        _notificationRepoMock
            .Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<ExpiryNotification>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ExpiryNotification>, CancellationToken>((n, _) => captured = n)
            .Returns(Task.CompletedTask);
        #endregion

        #region Act
        await _service.RunCheckAsync();
        #endregion

        #region Assert
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Count(), Is.EqualTo(2));
        Assert.That(captured.All(n => n.Channel == NotificationChannel.InApp), Is.True);
        Assert.That(captured.All(n => !n.Acknowledged), Is.True);
        #endregion
    }

    [Test]
    [Category("UD-03")]
    public async Task RunCheckAsync_AlreadyExpiredItem_SetsStatusToExpired()
    {
        #region Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expired = MakeExpiryDate(today.AddDays(-2));
        _expiryRepoMock
            .Setup(r => r.GetExpiringSoonAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([expired]);
        _notificationRepoMock
            .Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<ExpiryNotification>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        #endregion

        #region Act
        await _service.RunCheckAsync();
        #endregion

        #region Assert
        Assert.That(expired.InventoryItem.Status, Is.EqualTo(InventoryStatus.Expired));
        #endregion
    }

    [Test]
    [Category("UD-02")]
    public async Task RunCheckAsync_ExpiringSoonItem_StampsNotificationSentAt()
    {
        #region Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expiry = MakeExpiryDate(today.AddDays(1));
        _expiryRepoMock
            .Setup(r => r.GetExpiringSoonAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([expiry]);
        _notificationRepoMock
            .Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<ExpiryNotification>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        #endregion

        #region Act
        await _service.RunCheckAsync();
        #endregion

        #region Assert
        Assert.That(expiry.NotificationSentAt, Is.Not.Null);
        #endregion
    }

    [Test]
    [Category("UD-03")]
    public async Task RunCheckAsync_ManualOverrideDate_UsesOverrideDateForDaysCalculation()
    {
        #region Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overrideDate = today.AddDays(1);
        var expiry = MakeExpiryDate(overrideDate, isManualOverride: true);
        _expiryRepoMock
            .Setup(r => r.GetExpiringSoonAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([expiry]);
        IEnumerable<ExpiryNotification>? captured = null;
        _notificationRepoMock
            .Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<ExpiryNotification>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ExpiryNotification>, CancellationToken>((n, _) => captured = n)
            .Returns(Task.CompletedTask);
        #endregion

        #region Act
        await _service.RunCheckAsync();
        #endregion

        #region Assert
        Assert.That(captured!.Single().DaysBeforeExpiry, Is.EqualTo(1));
        #endregion
    }

    [Test]
    [Category("UD-02")]
    public async Task RunCheckAsync_NoExpiringItems_DoesNotCallNotificationRepository()
    {
        #region Arrange
        _expiryRepoMock
            .Setup(r => r.GetExpiringSoonAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        #endregion

        #region Act
        await _service.RunCheckAsync();
        #endregion

        #region Assert
        _notificationRepoMock.Verify(
            r => r.CreateRangeAsync(It.IsAny<IEnumerable<ExpiryNotification>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        #endregion
    }
}
