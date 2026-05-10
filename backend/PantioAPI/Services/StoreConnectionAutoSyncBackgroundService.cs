using Microsoft.Extensions.Options;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Services;

public class StoreConnectionAutoSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<StoreConnectionAutoSyncOptions> options,
    ILogger<StoreConnectionAutoSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(options.Value.IntervalHours);
        logger.LogInformation("Store connection auto-sync background service started with interval {Interval}", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);
            await Task.Delay(interval, stoppingToken);
        }
    }

    public async Task<int> RunOnceAsync(CancellationToken ct = default)
    {
        var interval = TimeSpan.FromHours(options.Value.IntervalHours);
        var dueBefore = DateTime.UtcNow.Subtract(interval);

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IStoreConnectionService>();

        try
        {
            return await service.SyncDueConnectionsAsync(dueBefore, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Store connection auto-sync batch failed");
            return 0;
        }
    }
}
