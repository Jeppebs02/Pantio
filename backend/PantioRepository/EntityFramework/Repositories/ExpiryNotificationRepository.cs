using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;
using PantioRepository.EntityFramework;

namespace PantioRepository.EntityFramework.Repositories;

public class ExpiryNotificationRepository(PantioDbContext db) : IExpiryNotificationRepository
{
    public async Task CreateRangeAsync(IEnumerable<ExpiryNotification> notifications, CancellationToken ct = default)
    {
        db.ExpiryNotifications.AddRange(notifications);
        await db.SaveChangesAsync(ct);
    }
}
