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
    IInventoryItemService inventoryItemService,
    IInventoryRepository inventoryRepository,
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
        if (!IsSupportedChain(chain)) return null;

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

        // Reset sync state on reconnect so the receipt picker is shown again
        connection.DisconnectedAt = null;
        connection.ConnectedAt = DateTime.UtcNow;
        connection.LastPolledAt = null;
        connection.ImportHorizon = null;
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
        if (connection is null) return null;

        logger.LogInformation(
            "Store connection {ConnectionId} auto-sync set to {AutoSyncEnabled} for user {UserId}", connectionId, enabled, userId);
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
                if (result is not null) syncedCount++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "Auto-sync failed for store connection {ConnectionId} and user {UserId}", connection.Id, connection.UserId);
            }
        }

        return syncedCount;
    }

    public async Task<StoreConnectionSyncResultDto?> SyncAsync(Guid userId, Guid connectionId, CancellationToken ct = default)
    {
        var connection = await repository.GetByIdAsync(userId, connectionId, ct);
        if (connection is null || connection.DisconnectedAt is not null) return null;

        if (!await EnsureFreshTokensAsync(connection, ct)) return null;

        if (string.IsNullOrWhiteSpace(connection.AccessToken) || string.IsNullOrWhiteSpace(connection.IdToken)) return null;

        int importedReceiptCount;
        int processedInventoryItemCount;

        try
        {
            var receiptSummaries = await nettoAuthClient.GetReceiptSummariesAsync(connection.AccessToken, connection.IdToken, ct);

            var existingReceiptIds = await repository.GetExistingReceiptIdsAsync(
                receiptSummaries.Select(r => r.Id), ct);
            var existingReceiptIdSet = existingReceiptIds.ToHashSet(StringComparer.Ordinal);
            var receiptsToImport = new List<ReceiptImportCandidateDto>();

            foreach (var summary in receiptSummaries)
            {
                if (existingReceiptIdSet.Contains(summary.Id)) continue;

                var receiptType = string.IsNullOrWhiteSpace(summary.ReceiptType) ? "merged" : summary.ReceiptType!;
                var detail = await nettoAuthClient.GetReceiptDetailAsync(connection.AccessToken, connection.IdToken, receiptType, summary.Id, ct);

                receiptsToImport.Add(BuildImportCandidate(summary, detail));
            }

            importedReceiptCount = await repository.ImportReceiptsAsync(userId, connection.Id, receiptsToImport, ct);
            processedInventoryItemCount = await ProcessReceiptLinesToInventoryAsync(userId, connection.Id, ct);
            connection.LastPolledAt = DateTime.UtcNow;
            await repository.UpdateAsync(connection, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await repository.SaveSyncLogAsync(new SyncLog
            {
                Id = Guid.NewGuid(),
                StoreConnectionId = connectionId,
                SyncedAt = DateTime.UtcNow,
                Status = "Failed",
                ErrorMessage = ex.Message
            }, ct);
            throw;
        }

        logger.LogInformation(
            "Store connection {ConnectionId} imported {ReceiptCount} receipts and processed {InventoryItemCount} inventory items for user {UserId}",
            connectionId, importedReceiptCount, processedInventoryItemCount, userId);

        await repository.SaveSyncLogAsync(new SyncLog
        {
            Id = Guid.NewGuid(),
            StoreConnectionId = connectionId,
            SyncedAt = connection.LastPolledAt!.Value,
            Status = "Success",
            ImportedReceiptCount = importedReceiptCount,
            ProcessedInventoryCount = processedInventoryItemCount
        }, ct);

        return new StoreConnectionSyncResultDto(
            connection.Id,
            connection.Chain,
            StoreConnectionMapper.ToStatus(connection),
            connection.LastPolledAt.Value,
            importedReceiptCount,
            processedInventoryItemCount
        );
    }

    public async Task<IReadOnlyCollection<PendingReceiptDto>?> GetPendingReceiptsAsync(Guid userId, Guid connectionId, CancellationToken ct = default)
    {
        var connection = await repository.GetByIdAsync(userId, connectionId, ct);
        if (connection is null || connection.DisconnectedAt is not null) return null;

        if (!await EnsureFreshTokensAsync(connection, ct)) return null;

        if (string.IsNullOrWhiteSpace(connection.AccessToken) || string.IsNullOrWhiteSpace(connection.IdToken)) return null;

        var summaries = await nettoAuthClient.GetReceiptSummariesAsync(connection.AccessToken, connection.IdToken, ct);

        return summaries
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new PendingReceiptDto(s.Id, s.StoreName, s.ReceiptType, s.SalesTotalDkk, s.CreatedAt))
            .ToArray();
    }

    public async Task<StoreConnectionSyncResultDto?> ImportSelectedAsync(Guid userId, Guid connectionId, ImportSelectedReceiptsDto dto, CancellationToken ct = default)
    {
        var inventories = await inventoryRepository.GetByUserIdAsync(userId, ct);
        if (!inventories.Any())
            throw new InvalidOperationException("Du skal oprette et lager, inden du importerer kvitteringer.");

        var connection = await repository.GetByIdAsync(userId, connectionId, ct);
        if (connection is null || connection.DisconnectedAt is not null) return null;

        if (!await EnsureFreshTokensAsync(connection, ct)) return null;

        if (string.IsNullOrWhiteSpace(connection.AccessToken) || string.IsNullOrWhiteSpace(connection.IdToken)) return null;

        var selectedIdSet = dto.SelectedDsgReceiptIds.ToHashSet(StringComparer.Ordinal);
        var summaries = await nettoAuthClient.GetReceiptSummariesAsync(connection.AccessToken, connection.IdToken, ct);

        var existingReceiptIds = await repository.GetExistingReceiptIdsAsync(summaries.Select(s => s.Id), ct);
        var existingReceiptIdSet = existingReceiptIds.ToHashSet(StringComparer.Ordinal);
        var receiptsToImport = new List<ReceiptImportCandidateDto>();

        foreach (var summary in summaries)
        {
            if (existingReceiptIdSet.Contains(summary.Id)) continue;

            if (selectedIdSet.Contains(summary.Id))
            {
                var receiptType = string.IsNullOrWhiteSpace(summary.ReceiptType) ? "merged" : summary.ReceiptType!;
                var detail = await nettoAuthClient.GetReceiptDetailAsync(connection.AccessToken, connection.IdToken, receiptType, summary.Id, ct);
                receiptsToImport.Add(BuildImportCandidate(summary, detail));
            }
            else
            {
                // Stub: record as seen without lines — permanently excluded from future syncs via deduplication
                receiptsToImport.Add(new ReceiptImportCandidateDto(summary.Id, summary.StoreName, summary.ReceiptType, summary.SalesTotalDkk, summary.MemberDiscountDkk, summary.OtherDiscountDkk, summary.CreatedAt, []));
            }
        }

        await repository.ImportReceiptsAsync(userId, connectionId, receiptsToImport, ct);
        var importedReceiptCount = receiptsToImport.Count(r => selectedIdSet.Contains(r.DsgReceiptId));
        var processedInventoryItemCount = importedReceiptCount > 0
            ? await ProcessReceiptLinesToInventoryAsync(userId, connectionId, ct)
            : 0;

        connection.LastPolledAt = DateTime.UtcNow;
        await repository.UpdateAsync(connection, ct);

        await repository.SaveSyncLogAsync(new SyncLog
        {
            Id = Guid.NewGuid(),
            StoreConnectionId = connectionId,
            SyncedAt = connection.LastPolledAt.Value,
            Status = "Success",
            ImportedReceiptCount = importedReceiptCount,
            ProcessedInventoryCount = processedInventoryItemCount
        }, ct);

        logger.LogInformation(
            "Store connection {ConnectionId} initial import: {ReceiptCount} receipts, {ItemCount} items",
            connectionId, importedReceiptCount, processedInventoryItemCount);

        return new StoreConnectionSyncResultDto(
            connection.Id,
            connection.Chain,
            StoreConnectionMapper.ToStatus(connection),
            connection.LastPolledAt.Value,
            importedReceiptCount,
            processedInventoryItemCount
        );
    }

    public async Task<IReadOnlyCollection<SyncLogDto>> GetSyncHistoryAsync(Guid userId, Guid connectionId, CancellationToken ct = default)
    {
        var connection = await repository.GetByIdAsync(userId, connectionId, ct);
        if (connection is null) return [];

        var logs = await repository.GetSyncLogsAsync(connectionId, ct);
        return logs.Select(l => new SyncLogDto(l.Id, l.SyncedAt, l.Status, l.ImportedReceiptCount, l.ProcessedInventoryCount)).ToArray();
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

    private async Task<bool> EnsureFreshTokensAsync(StoreConnection connection, CancellationToken ct)
    {
        if (!NeedsRefresh(connection)) return true;
        if (string.IsNullOrWhiteSpace(connection.RefreshToken)) return false;

        var refreshedTokens = await nettoAuthClient.RefreshAsync(connection.RefreshToken, ct);
        connection.AccessToken = refreshedTokens.AccessToken;
        connection.RefreshToken = refreshedTokens.RefreshToken;
        connection.IdToken = refreshedTokens.IdToken;
        connection.TokenExpiresAt = ResolveTokenExpiry(refreshedTokens);
        await repository.UpdateAsync(connection, ct);

        logger.LogInformation("Store connection {ConnectionId} tokens refreshed", connection.Id);
        return true;
    }

    private async Task<int> ProcessReceiptLinesToInventoryAsync(Guid userId, Guid connectionId, CancellationToken ct)
    {
        var inventories = await inventoryRepository.GetByUserIdAsync(userId, ct);
        var targetInventory = inventories.OrderBy(i => i.Name).ThenBy(i => i.Id).FirstOrDefault();
        if (targetInventory is null) return 0;

        var lines = await repository.GetUnprocessedReceiptLinesAsync(userId, connectionId, ct);
        if (lines.Count == 0) return 0;

        var processedCount = 0;
        var processedLineIds = new List<Guid>();

        foreach (var line in lines)
        {
            var qty = Math.Max(1, (int)Math.Round(line.QtyInSalesUnit));
            var dto = new CreateInventoryItemDto(
                ProductName: string.IsNullOrWhiteSpace(line.ArticleDescription) ? "Imported item" : line.ArticleDescription,
                Quantity: 1,
                QuantityUnit: null,
                Ean: line.Ean ?? string.Empty,
                StorageLocation: null,
                AddedVia: AddedVia.Receipt,
                ReceiptLineId: line.Id
            );

            for (var i = 0; i < qty; i++)
            {
                try
                {
                    await inventoryItemService.CreateAsync(targetInventory.Id, userId, dto, ct);
                    processedCount++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Failed to create inventory item for receipt line {LineId}", line.Id);
                }
            }

            processedLineIds.Add(line.Id);
        }

        if (processedLineIds.Count > 0)
            await repository.MarkReceiptLinesProcessedAsync(processedLineIds, ct);

        return processedCount;
    }

    private static ReceiptImportCandidateDto BuildImportCandidate(NettoReceiptSummary summary, NettoReceiptDetail detail)
    {
        return new ReceiptImportCandidateDto(
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
        );
    }

    private static bool IsSupportedChain(StoreChain chain) => chain == StoreChain.Netto;

    private static bool NeedsRefresh(StoreConnection connection)
    {
        return connection.TokenExpiresAt.HasValue && connection.TokenExpiresAt.Value <= DateTime.UtcNow.Add(TokenRefreshSkew);
    }

    private static DateTime ResolveTokenExpiry(NettoTokenSet tokenSet)
    {
        var expiresInSeconds = tokenSet.ExpiresInSeconds > 0
            ? tokenSet.ExpiresInSeconds.Value
            : 3000;

        return DateTime.UtcNow.AddSeconds(expiresInSeconds);
    }
}
