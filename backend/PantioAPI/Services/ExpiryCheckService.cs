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
        var now = DateTime.UtcNow;
        var notifications = new List<ExpiryNotification>();

        // ── Pass 1: expiring soon (1–N days remaining) ──
        var soonItems = (await expiryDateRepository.GetExpiringSoonAsync(threshold, ct)).ToList();
        foreach (var expiry in soonItems)
        {
            var effectiveDate = expiry.OverrideDate ?? expiry.EstimatedExpiry;
            var daysRemaining = effectiveDate.DayNumber - today.DayNumber;

            var body = daysRemaining == 1
                ? $"{expiry.InventoryItem.ProductName} udløber i morgen"
                : $"{expiry.InventoryItem.ProductName} udløber om {daysRemaining} dage";

            var channel = await TrySendPushAsync(expiry.InventoryItem.Inventory.User?.FcmToken, "Pantio", body, expiry.InventoryItemId, ct);

            notifications.Add(MakeNotification(expiry, daysRemaining, channel, now));
            expiry.NotificationSentAt = now;
        }

        // ── Pass 2: just expired (0 or fewer days remaining) ──
        var expiredItems = (await expiryDateRepository.GetJustExpiredAsync(ct)).ToList();
        foreach (var expiry in expiredItems)
        {
            expiry.InventoryItem.Status = InventoryStatus.Expired;

            var body = $"{expiry.InventoryItem.ProductName} er udløbet";
            var channel = await TrySendPushAsync(expiry.InventoryItem.Inventory.User?.FcmToken, "Pantio", body, expiry.InventoryItemId, ct);

            var effectiveDate = expiry.OverrideDate ?? expiry.EstimatedExpiry;
            var daysRemaining = effectiveDate.DayNumber - today.DayNumber;
            notifications.Add(MakeNotification(expiry, daysRemaining, channel, now));
            expiry.ExpiredNotificationSentAt = now;
        }

        if (notifications.Count == 0) return;

        await notificationRepository.CreateRangeAsync(notifications, ct);
        logger.LogInformation("Expiry check complete — {Soon} expiring-soon, {Expired} expired notification(s) created",
            soonItems.Count, expiredItems.Count);
    }

    private async Task<NotificationChannel> TrySendPushAsync(string? fcmToken, string title, string body, Guid itemId, CancellationToken ct)
    {
        if (fcmToken is null) return NotificationChannel.InApp;
        try
        {
            await fcmService.SendAsync(fcmToken, title, body, ct);
            return NotificationChannel.Push;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FCM send failed for item {ItemId}", itemId);
            return NotificationChannel.InApp;
        }
    }

    private static ExpiryNotification MakeNotification(ExpiryDate expiry, int daysRemaining, NotificationChannel channel, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        ExpiryDateId = expiry.Id,
        UserId = expiry.InventoryItem.Inventory.UserId,
        DaysBeforeExpiry = daysRemaining,
        Channel = channel,
        SentAt = now,
        Acknowledged = false
    };
}
