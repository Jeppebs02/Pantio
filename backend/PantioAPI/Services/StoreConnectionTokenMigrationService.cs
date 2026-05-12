using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioRepository.EntityFramework;
using PantioRepository.Security;

namespace PantioAPI.Services;

public class StoreConnectionTokenMigrationService(IServiceScopeFactory scopeFactory, StoreConnectionTokenProtector tokenProtector, ILogger<StoreConnectionTokenMigrationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PantioDbContext>();
        var connections = await db.StoreConnections.ToListAsync(cancellationToken);
        var migratedCount = 0;

        foreach (var connection in connections)
        {
            if (!HasPlaintextToken(connection)) continue;

            EncryptTokenFields(connection);
            migratedCount++;
        }

        if (migratedCount == 0) return;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Encrypted OAuth tokens for {ConnectionCount} store connections", migratedCount);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private bool HasPlaintextToken(StoreConnection connection)
    {
        return IsPlaintext(connection.GigyaSessionToken) ||
               IsPlaintext(connection.AccessToken) ||
               IsPlaintext(connection.RefreshToken) ||
               IsPlaintext(connection.IdToken);
    }

    private bool IsPlaintext(string? value)
    {
        return !string.IsNullOrEmpty(value) && !tokenProtector.IsEncrypted(value);
    }

    private void EncryptTokenFields(StoreConnection connection)
    {
        connection.GigyaSessionToken = tokenProtector.Encrypt(connection.GigyaSessionToken);
        connection.AccessToken = tokenProtector.Encrypt(connection.AccessToken);
        connection.RefreshToken = tokenProtector.Encrypt(connection.RefreshToken);
        connection.IdToken = tokenProtector.Encrypt(connection.IdToken);
    }
}
