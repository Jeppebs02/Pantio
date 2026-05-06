using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Controllers
{
    [ApiController]
    [Route("api/users/{userId:guid}/store-connections")]
    public class StoreConnectionController(IStoreConnectionService service) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(Guid userId, CancellationToken ct)
        {
            var connections = await service.GetByUserIdAsync(userId, ct);
            return Ok(connections);
        }

        [HttpPost("{chain}")]
        public async Task<IActionResult> Link(Guid userId, StoreChain chain, [FromBody] CompleteStoreConnectionLinkDto dto, CancellationToken ct)
        {
            var connection = await service.LinkAsync(userId, chain, dto, ct);
            if (connection is null)
                return BadRequest(new { message = "Store chain is not supported yet." });

            return CreatedAtAction(nameof(GetAll), new { userId }, connection);
        }

        [HttpPost("{connectionId:guid}/sync")]
        public async Task<IActionResult> Sync(Guid userId, Guid connectionId, CancellationToken ct)
        {
            var result = await service.SyncAsync(userId, connectionId, ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpDelete("{connectionId:guid}")]
        public async Task<IActionResult> Disconnect(Guid userId, Guid connectionId, CancellationToken ct)
        {
            var disconnected = await service.DisconnectAsync(userId, connectionId, ct);
            return disconnected ? NoContent() : NotFound();
        }
    }
}

namespace PantioAPI.Services
{
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
            return connections.Select(MapToDto).ToArray();
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
                return MapToDto(connection);
            }

            connection.DisconnectedAt = null;
            connection.ConnectedAt = DateTime.UtcNow;
            connection.AccessToken = tokenSet.AccessToken;
            connection.RefreshToken = tokenSet.RefreshToken;
            connection.IdToken = tokenSet.IdToken;
            connection.TokenExpiresAt = ResolveTokenExpiry(tokenSet);

            var updated = await repository.UpdateAsync(connection, ct);
            logger.LogInformation("Store connection {ConnectionId} relinked for user {UserId}", updated.Id, userId);
            return MapToDto(updated);
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
                MapStatus(connection),
                connection.LastPolledAt.Value,
                importedReceiptCount,
                processedInventoryItemCount
            );
        }

        public async Task<bool> DisconnectAsync(Guid userId, Guid connectionId, CancellationToken ct = default)
        {
            var connection = await repository.GetByIdAsync(userId, connectionId, ct);
            if (connection is null)
                return false;

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

        private static StoreConnectionDto MapToDto(StoreConnection connection)
        {
            return new StoreConnectionDto(
                connection.Id,
                connection.UserId,
                connection.Chain,
                MapStatus(connection),
                connection.ConnectedAt,
                connection.DisconnectedAt,
                connection.LastPolledAt,
                connection.TokenExpiresAt
            );
        }

        private static StoreConnectionStatus MapStatus(StoreConnection connection)
        {
            if (connection.DisconnectedAt is not null)
                return StoreConnectionStatus.Disconnected;

            return connection.LastPolledAt is null
                ? StoreConnectionStatus.PendingSync
                : StoreConnectionStatus.Active;
        }

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
}
