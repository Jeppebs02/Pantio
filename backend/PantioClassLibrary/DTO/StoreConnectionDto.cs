using PantioClassLibrary.Enums;

namespace PantioClassLibrary.DTO;

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
