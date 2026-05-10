using PantioClassLibrary.DTO;
using PantioClassLibrary.Enums;

namespace PantioClassLibrary.Interfaces.Services;

public interface IStoreConnectionService
{
    Task<IReadOnlyCollection<StoreConnectionDto>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<StoreConnectionDto?> LinkAsync(Guid userId, StoreChain chain, CompleteStoreConnectionLinkDto dto, CancellationToken ct = default);
    Task<StoreConnectionDto?> UpdateAutoSyncAsync(Guid userId, Guid connectionId, bool enabled, CancellationToken ct = default);
    Task<int> SyncDueConnectionsAsync(DateTime dueBefore, CancellationToken ct = default);
    Task<StoreConnectionSyncResultDto?> SyncAsync(Guid userId, Guid connectionId, CancellationToken ct = default);
    Task<bool> DisconnectAsync(Guid userId, Guid connectionId, CancellationToken ct = default);
}
