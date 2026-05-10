using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;

namespace PantioRepository.Mapper;

public static class StoreConnectionMapper
{
    public static StoreConnectionDto ToDto(StoreConnection connection) => new(
        connection.Id,
        connection.UserId,
        connection.Chain,
        ToStatus(connection),
        connection.ConnectedAt,
        connection.DisconnectedAt,
        connection.LastPolledAt,
        connection.TokenExpiresAt
    );

    public static StoreConnectionStatus ToStatus(StoreConnection connection)
    {
        if (connection.DisconnectedAt is not null)
            return StoreConnectionStatus.Disconnected;

        return connection.LastPolledAt is null
            ? StoreConnectionStatus.PendingSync
            : StoreConnectionStatus.Active;
    }
}
