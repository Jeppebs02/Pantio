using Microsoft.Extensions.Logging;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioRepository.Mapper;

namespace PantioAPI.Services;

public class StoreConnectionService(
    IStoreConnectionRepository repository,
    INettoAuthClient nettoAuthClient,
    ILogger<StoreConnectionService> logger) : IStoreConnectionService
{
    private static readonly TimeSpan TokenRefreshSkew = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyCollection<StoreConnectionDto>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        logger.LogDebug("Fetching store connections for user {UserId}", userId);
        var connections = await repository.GetByUserIdAsync(userId, ct);
        return connections.Select(StoreConnectionMapper.ToDto).ToArray();
    }

    public async Task<StoreConnectionDto?> LinkAsync(Guid userId, StoreChain chain, CompleteStoreConnectionLinkDto dto, CancellationToken ct = default)
    {
        if (!IsSupportedChain(chain))
            return null;

        var tokenSet = await nettoAuthClient.ExchangeCodeAsync(dto.AuthorizationCode, dto.CodeVerifier, dto.RedirectUri, ct);

        var connection = await repository.GetByUserAndChainAsync(userId, chain, ct);
        if (connection is null)
        {
            connection = await repository.CreateAsync(new StoreConnection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Chain = chain,
                ConnectedAt = DateTime.UtcNow,
                AccessToken = tokenSet.AccessToken,
                RefreshToken = tokenSet.RefreshToken,
                IdToken = tokenSet.IdToken,
                TokenExpiresAt = ResolveTokenExpiry(tokenSet)
            }, ct);

            logger.LogInformation("Store connection {ConnectionId} created for user {UserId} and chain {Chain}", connection.Id, userId, chain);
            return StoreConnectionMapper.ToDto(connection);
        }

        connection.DisconnectedAt = null;
        connection.ConnectedAt = DateTime.UtcNow;
        connection.AccessToken = tokenSet.AccessToken;
        connection.RefreshToken = tokenSet.RefreshToken;
        connection.IdToken = tokenSet.IdToken;
        connection.TokenExpiresAt = ResolveTokenExpiry(tokenSet);

        var updated = await repository.UpdateAsync(connection, ct);
        logger.LogInformation("Store connection {ConnectionId} relinked for user {UserId}", updated.Id, userId);
        return StoreConnectionMapper.ToDto(updated);
    }

    public async Task<StoreConnectionDto?> UpdateAutoSyncAsync(Guid userId, Guid connectionId, bool enabled, CancellationToken ct = default)
    {
        var connection = await repository.UpdateAutoSyncAsync(userId, connectionId, enabled, ct);
        if (connection is null)
            return null;

        logger.LogInformation(
            "Store connection {ConnectionId} auto-sync set to {AutoSyncEnabled} for user {UserId}",
            connectionId,
            enabled,
            userId);
        return StoreConnectionMapper.ToDto(connection);
    }

    public async Task<int> SyncDueConnectionsAsync(DateTime dueBefore, CancellationToken ct = default)
    {
        var dueConnections = await repository.GetDueForAutoSyncAsync(dueBefore, ct);
        var syncedCount = 0;

        foreach (var connection in dueConnections)
        {
            try
            {
                var result = await SyncAsync(connection.UserId, connection.Id, ct);
                if (result is not null)
                    syncedCount++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "Auto-sync failed for store connection {ConnectionId} and user {UserId}",
                    connection.Id,
                    connection.UserId);
            }
        }

        return syncedCount;
    }

    public async Task<StoreConnectionSyncResultDto?> SyncAsync(Guid userId, Guid connectionId, CancellationToken ct = default)
    {
        var connection = await repository.GetByIdAsync(userId, connectionId, ct);
        if (connection is null || connection.DisconnectedAt is not null)
            return null;

        if (NeedsRefresh(connection))
        {
            if (string.IsNullOrWhiteSpace(connection.RefreshToken))
                return null;

            var refreshedTokens = await nettoAuthClient.RefreshAsync(connection.RefreshToken, ct);
            connection.AccessToken = refreshedTokens.AccessToken;
            connection.RefreshToken = refreshedTokens.RefreshToken;
            connection.IdToken = refreshedTokens.IdToken;
            connection.TokenExpiresAt = ResolveTokenExpiry(refreshedTokens);
            logger.LogInformation("Store connection {ConnectionId} tokens refreshed for user {UserId}", connectionId, userId);
        }

        if (string.IsNullOrWhiteSpace(connection.AccessToken) || string.IsNullOrWhiteSpace(connection.IdToken))
            return null;

        var receiptSummaries = await nettoAuthClient.GetReceiptSummariesAsync(connection.AccessToken, connection.IdToken, ct);
        var existingReceiptIds = await repository.GetExistingReceiptIdsAsync(
            receiptSummaries.Select(receipt => receipt.Id),
            ct);
        var existingReceiptIdSet = existingReceiptIds.ToHashSet(StringComparer.Ordinal);
        var receiptsToImport = new List<ReceiptImportCandidateDto>();

        foreach (var summary in receiptSummaries)
        {
            if (existingReceiptIdSet.Contains(summary.Id))
                continue;

            var receiptType = string.IsNullOrWhiteSpace(summary.ReceiptType) ? "merged" : summary.ReceiptType!;
            var detail = await nettoAuthClient.GetReceiptDetailAsync(connection.AccessToken, connection.IdToken, receiptType, summary.Id, ct);

            receiptsToImport.Add(new ReceiptImportCandidateDto(
                summary.Id,
                summary.StoreName,
                summary.ReceiptType,
                summary.SalesTotalDkk,
                summary.MemberDiscountDkk,
                summary.OtherDiscountDkk,
                summary.CreatedAt,
                detail.LineItems.Select(line => new ReceiptLineImportCandidateDto(
                    line.Ean,
                    line.ArticleDescription,
                    line.SalesPriceDkk,
                    line.NormalPriceDkk,
                    line.DiscountDkk,
                    line.DiscountsJson,
                    line.QtyInSalesUnit,
                    line.TaxAmountDkk,
                    line.ItemType
                )).ToArray()
            ));
        }

        var importedReceiptCount = await repository.ImportReceiptsAsync(userId, connection.Id, receiptsToImport, ct);
        var processedInventoryItemCount = await repository.ProcessImportedReceiptLinesToInventoryAsync(userId, connection.Id, ct);
        connection.LastPolledAt = DateTime.UtcNow;
        await repository.UpdateAsync(connection, ct);

        logger.LogInformation(
            "Store connection {ConnectionId} imported {ReceiptCount} receipts and processed {InventoryItemCount} inventory items for user {UserId}",
            connectionId,
            importedReceiptCount,
            processedInventoryItemCount,
            userId);
        return new StoreConnectionSyncResultDto(
            connection.Id,
            connection.Chain,
            StoreConnectionMapper.ToStatus(connection),
            connection.LastPolledAt.Value,
            importedReceiptCount,
            processedInventoryItemCount
        );
    }

    public async Task<bool> DisconnectAsync(Guid userId, Guid connectionId, CancellationToken ct = default)
    {
        var connection = await repository.GetByIdAsync(userId, connectionId, ct);
        if (connection is null) return false;

        connection.DisconnectedAt = DateTime.UtcNow;
        connection.GigyaSessionToken = null;
        connection.AccessToken = null;
        connection.RefreshToken = null;
        connection.IdToken = null;
        connection.TokenExpiresAt = null;

        await repository.UpdateAsync(connection, ct);
        logger.LogInformation("Store connection {ConnectionId} disconnected for user {UserId}", connectionId, userId);
        return true;
    }

    private static bool IsSupportedChain(StoreChain chain) => chain == StoreChain.Netto;

    private static bool NeedsRefresh(StoreConnection connection)
    {
        return connection.TokenExpiresAt.HasValue &&
               connection.TokenExpiresAt.Value <= DateTime.UtcNow.Add(TokenRefreshSkew);
    }

    private static DateTime ResolveTokenExpiry(NettoTokenSet tokenSet)
    {
        var expiresInSeconds = tokenSet.ExpiresInSeconds > 0
            ? tokenSet.ExpiresInSeconds.Value
            : 3000;

        return DateTime.UtcNow.AddSeconds(expiresInSeconds);
    }
}
