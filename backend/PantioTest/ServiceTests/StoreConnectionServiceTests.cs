using Microsoft.Extensions.Logging;
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
    private Mock<ILogger<StoreConnectionService>> _loggerMock = null!;
    private IStoreConnectionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IStoreConnectionRepository>();
        _nettoAuthClientMock = new Mock<INettoAuthClient>();
        _loggerMock = new Mock<ILogger<StoreConnectionService>>();
        _service = new StoreConnectionService(_repositoryMock.Object, _nettoAuthClientMock.Object, _loggerMock.Object);
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
        _repositoryMock
            .Setup(repository => repository.ImportReceiptsAsync(userId, connectionId, It.IsAny<IReadOnlyCollection<ReceiptImportCandidateDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repositoryMock
            .Setup(repository => repository.ProcessImportedReceiptLinesToInventoryAsync(userId, connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
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
        _repositoryMock.Verify(
            repository => repository.ProcessImportedReceiptLinesToInventoryAsync(userId, connectionId, It.IsAny<CancellationToken>()),
            Times.Once);
        #endregion
    }
}
