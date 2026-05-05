using PantioClassLibrary.Entities;

namespace PantioClassLibrary.Interfaces.Repository;

public interface IExpiryNotificationRepository
{
    Task CreateRangeAsync(IEnumerable<ExpiryNotification> notifications, CancellationToken ct = default);
}
