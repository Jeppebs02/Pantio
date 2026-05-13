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
    IFcmService fcmService,
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

            var fcmToken = expiry.InventoryItem.Inventory.User?.FcmToken;
            var channel = NotificationChannel.InApp;

            if (fcmToken is not null)
            {
                var productName = expiry.InventoryItem.ProductName;
                var body = daysRemaining <= 0
                    ? $"{productName} er udløbet"
                    : daysRemaining == 1
                        ? $"{productName} udløber i morgen"
                        : $"{productName} udløber om {daysRemaining} dage";

                try
                {
                    await fcmService.SendAsync(fcmToken, "Pantio", body, ct);
                    channel = NotificationChannel.Push;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "FCM send failed for item {ItemId}", expiry.InventoryItemId);
                }
            }

            notifications.Add(new ExpiryNotification
            {
                Id = Guid.NewGuid(),
                ExpiryDateId = expiry.Id,
                UserId = expiry.InventoryItem.Inventory.UserId,
                DaysBeforeExpiry = daysRemaining,
                Channel = channel,
                SentAt = now,
                Acknowledged = false
            });

            expiry.NotificationSentAt = now;
        }

        await notificationRepository.CreateRangeAsync(notifications, ct);

        logger.LogInformation(
            "Expiry check complete — {Count} notification(s) created", notifications.Count);
    }
}
