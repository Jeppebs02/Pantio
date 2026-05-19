using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PantioAPI;
using Moq;
using PantioAPI.Services;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ServiceTests;

public class StoreConnectionServiceTests
{
    private Mock<IStoreConnectionRepository> _repositoryMock = null!;
    private Mock<INettoAuthClient> _nettoAuthClientMock = null!;
    private Mock<IInventoryItemService> _inventoryItemServiceMock = null!;
    private Mock<IInventoryRepository> _inventoryRepositoryMock = null!;
    private Mock<ILogger<StoreConnectionService>> _loggerMock = null!;
    private IStoreConnectionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IStoreConnectionRepository>();
        _nettoAuthClientMock = new Mock<INettoAuthClient>();
        _inventoryItemServiceMock = new Mock<IInventoryItemService>();
        _inventoryRepositoryMock = new Mock<IInventoryRepository>();
        _loggerMock = new Mock<ILogger<StoreConnectionService>>();
        _service = new StoreConnectionService(
            _repositoryMock.Object,
            _nettoAuthClientMock.Object,
            _inventoryItemServiceMock.Object,
            _inventoryRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task SyncAsync_ImportsOnlyMissingReceiptsAndReturnsImportedCount()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var connection = new StoreConnection
        {
            Id = connectionId,
            UserId = userId,
            Chain = StoreChain.Netto,
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            IdToken = "id-token",
            ConnectedAt = DateTime.UtcNow.AddDays(-1),
            TokenExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var receiptSummaries = new[]
        {
            new NettoReceiptSummary("existing-receipt", "Netto A", "merged", 10f, 1f, 0f, DateTime.UtcNow.AddDays(-2)),
            new NettoReceiptSummary("new-receipt", "Netto B", "full", 25f, 0f, 2f, DateTime.UtcNow.AddDays(-1))
        };

        _repositoryMock
            .Setup(repository => repository.GetByIdAsync(userId, connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);
        _nettoAuthClientMock
            .Setup(client => client.GetReceiptSummariesAsync(connection.AccessToken!, connection.IdToken!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiptSummaries);
        _repositoryMock
            .Setup(repository => repository.GetExistingReceiptIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "existing-receipt" });
        _nettoAuthClientMock
            .Setup(client => client.GetReceiptDetailAsync(connection.AccessToken!, connection.IdToken!, "full", "new-receipt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NettoReceiptDetail([
                new NettoReceiptLine("5701234567890", "Milk", 25f, 25f, 0f, null, 1f, 5f, "01")
            ]));
        var targetInventory = new Inventory { Id = Guid.NewGuid(), UserId = userId, Name = "Pantry" };
        var receiptLine = new ReceiptLine
        {
            Id = Guid.NewGuid(),
            Ean = "5701234567890",
            ArticleDescription = "Milk",
            QtyInSalesUnit = 1f,
            ItemType = "01",
            ProcessedToInventory = false,
            Receipt = new Receipt { UserId = userId, StoreConnectionId = connectionId, CreatedAt = DateTime.UtcNow }
        };
        var createdItem = new InventoryItemDto(Guid.NewGuid(), targetInventory.Id, "Milk", 1m, null, "5701234567890", null, "Available", "Receipt", DateTime.UtcNow, DateTime.UtcNow, null, null, null, 0);

        _repositoryMock
            .Setup(repository => repository.ImportReceiptsAsync(userId, connectionId, It.IsAny<IReadOnlyCollection<ReceiptImportCandidateDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _inventoryRepositoryMock
            .Setup(repo => repo.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([targetInventory]);
        _repositoryMock
            .Setup(repository => repository.GetUnprocessedReceiptLinesAsync(userId, connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([receiptLine]);
        _inventoryItemServiceMock
            .Setup(service => service.CreateAsync(targetInventory.Id, userId, It.IsAny<CreateInventoryItemDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdItem);
        _repositoryMock
            .Setup(repository => repository.MarkReceiptLinesProcessedAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(repository => repository.UpdateAsync(connection, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);
        #endregion

        #region Act
        var result = await _service.SyncAsync(userId, connectionId, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ImportedReceiptCount, Is.EqualTo(1));
        Assert.That(result.ProcessedInventoryItemCount, Is.EqualTo(1));
        _nettoAuthClientMock.Verify(
            client => client.GetReceiptDetailAsync(connection.AccessToken!, connection.IdToken!, "full", "new-receipt", It.IsAny<CancellationToken>()),
            Times.Once);
        _repositoryMock.Verify(
            repository => repository.ImportReceiptsAsync(
                userId,
                connectionId,
                It.Is<IReadOnlyCollection<ReceiptImportCandidateDto>>(receipts =>
                    receipts.Count == 1 &&
                    receipts.Single().DsgReceiptId == "new-receipt" &&
                    receipts.Single().Lines.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _inventoryItemServiceMock.Verify(
            service => service.CreateAsync(targetInventory.Id, userId, It.IsAny<CreateInventoryItemDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
        #endregion
    }

    [Test]
    public async Task UpdateAutoSyncAsync_ExistingConnection_ReturnsUpdatedDto()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var connection = new StoreConnection
        {
            Id = connectionId,
            UserId = userId,
            Chain = StoreChain.Netto,
            AutoSyncEnabled = true,
            ConnectedAt = DateTime.UtcNow
        };
        _repositoryMock
            .Setup(repository => repository.UpdateAutoSyncAsync(userId, connectionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);
        #endregion

        #region Act
        var result = await _service.UpdateAutoSyncAsync(userId, connectionId, true, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.AutoSyncEnabled, Is.True);
        Assert.That(result.Id, Is.EqualTo(connectionId));
        #endregion
    }

    [Test]
    public async Task UpdateAutoSyncAsync_MissingConnection_ReturnsNull()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        _repositoryMock
            .Setup(repository => repository.UpdateAutoSyncAsync(userId, connectionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreConnection?)null);
        #endregion

        #region Act
        var result = await _service.UpdateAutoSyncAsync(userId, connectionId, true, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    public async Task SyncDueConnectionsAsync_ConnectionFailure_ContinuesWithRemainingConnections()
    {
        #region Arrange
        var failingConnection = new StoreConnection
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Chain = StoreChain.Netto,
            AutoSyncEnabled = true,
            ConnectedAt = DateTime.UtcNow.AddDays(-2)
        };
        var successfulConnection = new StoreConnection
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Chain = StoreChain.Netto,
            AutoSyncEnabled = true,
            AccessToken = "access-token",
            IdToken = "id-token",
            ConnectedAt = DateTime.UtcNow.AddDays(-2),
            TokenExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        var dueBefore = DateTime.UtcNow.AddHours(-6);

        _repositoryMock
            .Setup(repository => repository.GetDueForAutoSyncAsync(dueBefore, It.IsAny<CancellationToken>()))
            .ReturnsAsync([failingConnection, successfulConnection]);
        _repositoryMock
            .Setup(repository => repository.GetByIdAsync(failingConnection.UserId, failingConnection.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sync failed"));
        _repositoryMock
            .Setup(repository => repository.GetByIdAsync(successfulConnection.UserId, successfulConnection.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successfulConnection);
        _nettoAuthClientMock
            .Setup(client => client.GetReceiptSummariesAsync(successfulConnection.AccessToken!, successfulConnection.IdToken!, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repositoryMock
            .Setup(repository => repository.GetExistingReceiptIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repositoryMock
            .Setup(repository => repository.ImportReceiptsAsync(successfulConnection.UserId, successfulConnection.Id, It.IsAny<IReadOnlyCollection<ReceiptImportCandidateDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _inventoryRepositoryMock
            .Setup(repo => repo.GetByUserIdAsync(successfulConnection.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repositoryMock
            .Setup(repository => repository.GetUnprocessedReceiptLinesAsync(successfulConnection.UserId, successfulConnection.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repositoryMock
            .Setup(repository => repository.UpdateAsync(successfulConnection, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successfulConnection);
        #endregion

        #region Act
        var result = await _service.SyncDueConnectionsAsync(dueBefore, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.EqualTo(1));
        _repositoryMock.Verify(
            repository => repository.GetByIdAsync(successfulConnection.UserId, successfulConnection.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        #endregion
    }

    [Test]
    public async Task AutoSyncBackgroundService_RunOnce_InvokesDueSync()
    {
        #region Arrange
        var storeConnectionServiceMock = new Mock<IStoreConnectionService>();
        storeConnectionServiceMock
            .Setup(service => service.SyncDueConnectionsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        var services = new ServiceCollection()
            .AddScoped(_ => storeConnectionServiceMock.Object)
            .BuildServiceProvider();
        var backgroundService = new StoreConnectionAutoSyncBackgroundService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new StoreConnectionAutoSyncOptions { IntervalHours = 6 }),
            Mock.Of<ILogger<StoreConnectionAutoSyncBackgroundService>>());
        #endregion

        #region Act
        var result = await backgroundService.RunOnceAsync(CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.EqualTo(2));
        storeConnectionServiceMock.Verify(
            service => service.SyncDueConnectionsAsync(It.Is<DateTime>(dueBefore => dueBefore <= DateTime.UtcNow.AddHours(-6)), It.IsAny<CancellationToken>()),
            Times.Once);
        #endregion
    }

    [Test]
    public async Task AutoSyncBackgroundService_RunOnce_ServiceFailure_ReturnsZero()
    {
        #region Arrange
        var storeConnectionServiceMock = new Mock<IStoreConnectionService>();
        storeConnectionServiceMock
            .Setup(service => service.SyncDueConnectionsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("batch failed"));
        var services = new ServiceCollection()
            .AddScoped(_ => storeConnectionServiceMock.Object)
            .BuildServiceProvider();
        var backgroundService = new StoreConnectionAutoSyncBackgroundService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new StoreConnectionAutoSyncOptions { IntervalHours = 6 }),
            Mock.Of<ILogger<StoreConnectionAutoSyncBackgroundService>>());
        #endregion

        #region Act
        var result = await backgroundService.RunOnceAsync(CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.EqualTo(0));
        #endregion
    }
}
