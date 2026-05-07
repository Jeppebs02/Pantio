using PantioClassLibrary.Entities;

namespace PantioClassLibrary.Enums
{
    public enum StoreConnectionStatus
    {
        Active,
        Disconnected,
        PendingSync
    }
}

namespace PantioClassLibrary.DTO
{
    using PantioClassLibrary.Enums;

    public sealed record CompleteStoreConnectionLinkDto(
        string AuthorizationCode,
        string CodeVerifier,
        string? RedirectUri
    );

    public sealed record StoreConnectionDto(
        Guid Id,
        Guid UserId,
        StoreChain Chain,
        StoreConnectionStatus Status,
        DateTime ConnectedAt,
        DateTime? DisconnectedAt,
        DateTime? LastPolledAt,
        DateTime? TokenExpiresAt
    );

    public sealed record StoreConnectionSyncResultDto(
        Guid ConnectionId,
        StoreChain Chain,
        StoreConnectionStatus Status,
        DateTime SyncedAt,
        int ImportedReceiptCount,
        int ProcessedInventoryItemCount
    );

    public sealed record ReceiptImportCandidateDto(
        string DsgReceiptId,
        string? StoreName,
        string? ReceiptType,
        float SalesTotalDkk,
        float MemberDiscountDkk,
        float OtherDiscountDkk,
        DateTime CreatedAt,
        IReadOnlyCollection<ReceiptLineImportCandidateDto> Lines
    );

    public sealed record ReceiptLineImportCandidateDto(
        string? Ean,
        string? ArticleDescription,
        float SalesPriceDkk,
        float NormalPriceDkk,
        float DiscountDkk,
        string? DiscountsJson,
        float QtyInSalesUnit,
        float TaxAmountDkk,
        string? ItemType
    );
}

namespace PantioClassLibrary.Interfaces.Repository
{
    using PantioClassLibrary.DTO;
    using PantioClassLibrary.Enums;

    public interface IStoreConnectionRepository
    {
        Task<IReadOnlyCollection<StoreConnection>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<StoreConnection?> GetByUserAndChainAsync(Guid userId, StoreChain chain, CancellationToken ct = default);
        Task<StoreConnection?> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default);
        Task<StoreConnection> CreateAsync(StoreConnection connection, CancellationToken ct = default);
        Task<StoreConnection> UpdateAsync(StoreConnection connection, CancellationToken ct = default);
        Task<IReadOnlyCollection<string>> GetExistingReceiptIdsAsync(IEnumerable<string> dsgReceiptIds, CancellationToken ct = default);
        Task<int> ImportReceiptsAsync(Guid userId, Guid connectionId, IReadOnlyCollection<ReceiptImportCandidateDto> receipts, CancellationToken ct = default);
        Task<int> ProcessImportedReceiptLinesToInventoryAsync(Guid userId, Guid connectionId, CancellationToken ct = default);
    }
}

namespace PantioClassLibrary.Interfaces.Services
{
    using PantioClassLibrary.DTO;
    using PantioClassLibrary.Enums;

    public interface IStoreConnectionService
    {
        Task<IReadOnlyCollection<StoreConnectionDto>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<StoreConnectionDto?> LinkAsync(Guid userId, StoreChain chain, CompleteStoreConnectionLinkDto dto, CancellationToken ct = default);
        Task<StoreConnectionSyncResultDto?> SyncAsync(Guid userId, Guid connectionId, CancellationToken ct = default);
        Task<bool> DisconnectAsync(Guid userId, Guid connectionId, CancellationToken ct = default);
    }
}
