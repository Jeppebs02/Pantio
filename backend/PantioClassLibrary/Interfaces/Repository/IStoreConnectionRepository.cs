using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;

namespace PantioClassLibrary.Interfaces.Repository;

public interface IStoreConnectionRepository
{
    Task<IReadOnlyCollection<StoreConnection>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<StoreConnection?> GetByUserAndChainAsync(Guid userId, StoreChain chain, CancellationToken ct = default);
    Task<StoreConnection?> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<StoreConnection?> UpdateAutoSyncAsync(Guid userId, Guid connectionId, bool enabled, CancellationToken ct = default);
    Task<IReadOnlyCollection<StoreConnection>> GetDueForAutoSyncAsync(DateTime dueBefore, CancellationToken ct = default);
    Task<StoreConnection> CreateAsync(StoreConnection connection, CancellationToken ct = default);
    Task<StoreConnection> UpdateAsync(StoreConnection connection, CancellationToken ct = default);
    Task<IReadOnlyCollection<string>> GetExistingReceiptIdsAsync(IEnumerable<string> dsgReceiptIds, CancellationToken ct = default);
    Task<int> ImportReceiptsAsync(Guid userId, Guid connectionId, IReadOnlyCollection<ReceiptImportCandidateDto> receipts, CancellationToken ct = default);
    Task<int> ProcessImportedReceiptLinesToInventoryAsync(Guid userId, Guid connectionId, CancellationToken ct = default);
}
