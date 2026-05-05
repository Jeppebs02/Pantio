using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Services;

public class ExpiryCheckService(
    IExpiryDateRepository expiryDateRepository,
    IExpiryNotificationRepository notificationRepository,
    IOptions<ExpiryCheckOptions> options,
    ILogger<ExpiryCheckService> logger) : IExpiryCheckService
{
    public async Task RunCheckAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = today.AddDays(options.Value.NotificationThresholdDays);

        var expiringItems = (await expiryDateRepository.GetExpiringSoonAsync(threshold, ct)).ToList();
        if (expiringItems.Count == 0) return;

        var now = DateTime.UtcNow;
        var notifications = new List<ExpiryNotification>(expiringItems.Count);

        foreach (var expiry in expiringItems)
        {
            var effectiveDate = expiry.OverrideDate ?? expiry.EstimatedExpiry;
            var daysRemaining = effectiveDate.DayNumber - today.DayNumber;

            if (daysRemaining <= 0)
                expiry.InventoryItem.Status = InventoryStatus.Expired;

            notifications.Add(new ExpiryNotification
            {
                Id = Guid.NewGuid(),
                ExpiryDateId = expiry.Id,
                UserId = expiry.InventoryItem.Inventory.UserId,
                DaysBeforeExpiry = daysRemaining,
                Channel = NotificationChannel.InApp,
                SentAt = now,
                Acknowledged = false
            });

            expiry.NotificationSentAt = now;
        }

        // Shared DbContext scope — SaveChanges persists notifications +
        // NotificationSentAt stamps + any Expired status updates atomically.
        await notificationRepository.CreateRangeAsync(notifications, ct);

        logger.LogInformation(
            "Expiry check complete — {Count} notification(s) created", notifications.Count);
    }
}
