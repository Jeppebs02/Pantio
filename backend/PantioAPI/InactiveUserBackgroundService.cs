using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI;

public class InactiveUserBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<InactiveUserBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Inactive user background service started — interval 24h");

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunScopedCheckAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunScopedCheckAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IInactiveUserService>();
        try
        {
            await service.RunCheckAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Inactive user check failed");
        }
    }
}
