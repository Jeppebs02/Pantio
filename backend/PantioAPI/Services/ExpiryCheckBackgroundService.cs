using Microsoft.Extensions.Options;
using PantioAPI.Options;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Services;

public class ExpiryCheckBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ExpiryCheckOptions> options,
    ILogger<ExpiryCheckBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(options.Value.IntervalHours);
        logger.LogInformation(
            "Expiry check background service started — interval {Interval}", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunScopedCheckAsync(stoppingToken);
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RunScopedCheckAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IExpiryCheckService>();
        try
        {
            await service.RunCheckAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Expiry check failed");
        }
    }
}
