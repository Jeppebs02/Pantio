using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PantioAPI;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioRepository.EntityFramework;
using PantioRepository.Security;

namespace PantioTest.ServiceTests;

public class StoreConnectionTokenMigrationServiceTests
{
    private DbContextOptions<PantioDbContext> _options = null!;
    private StoreConnectionTokenProtector _tokenProtector = null!;
    private Mock<ILogger<StoreConnectionTokenMigrationService>> _loggerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _options = new DbContextOptionsBuilder<PantioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _tokenProtector = new StoreConnectionTokenProtector(new StoreConnectionTokenEncryptionOptions
        {
            Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        });
        _loggerMock = new Mock<ILogger<StoreConnectionTokenMigrationService>>();
    }

    [Test]
    public async Task StartAsync_EncryptsPlaintextStoreConnectionTokens()
    {
        var connection = MakeConnection();
        await using (var db = CreateContext())
        {
            db.StoreConnections.Add(connection);
            await db.SaveChangesAsync();
        }

        await using var migrationDb = CreateContext();
        var service = CreateService(migrationDb);

        await service.StartAsync(CancellationToken.None);

        await using var assertDb = CreateContext();
        var persisted = await assertDb.StoreConnections.AsNoTracking().SingleAsync();
        Assert.That(_tokenProtector.IsEncrypted(persisted.GigyaSessionToken), Is.True);
        Assert.That(_tokenProtector.IsEncrypted(persisted.AccessToken), Is.True);
        Assert.That(_tokenProtector.IsEncrypted(persisted.RefreshToken), Is.True);
        Assert.That(_tokenProtector.IsEncrypted(persisted.IdToken), Is.True);
        Assert.That(_tokenProtector.Decrypt(persisted.AccessToken), Is.EqualTo("access-token"));
        Assert.That(_tokenProtector.Decrypt(persisted.RefreshToken), Is.EqualTo("refresh-token"));
        Assert.That(_tokenProtector.Decrypt(persisted.IdToken), Is.EqualTo("id-token"));
    }

    [Test]
    public async Task StartAsync_DoesNotDoubleEncryptAlreadyEncryptedTokens()
    {
        var encryptedAccessToken = _tokenProtector.Encrypt("access-token");
        var connection = MakeConnection();
        connection.AccessToken = encryptedAccessToken;
        connection.RefreshToken = _tokenProtector.Encrypt("refresh-token");
        connection.IdToken = _tokenProtector.Encrypt("id-token");
        connection.GigyaSessionToken = _tokenProtector.Encrypt("gigya-session-token");
        await using (var db = CreateContext())
        {
            db.StoreConnections.Add(connection);
            await db.SaveChangesAsync();
        }

        await using var migrationDb = CreateContext();
        var service = CreateService(migrationDb);

        await service.StartAsync(CancellationToken.None);

        await using var assertDb = CreateContext();
        var persisted = await assertDb.StoreConnections.AsNoTracking().SingleAsync();
        Assert.That(persisted.AccessToken, Is.EqualTo(encryptedAccessToken));
        Assert.That(_tokenProtector.Decrypt(persisted.AccessToken), Is.EqualTo("access-token"));
    }

    [Test]
    public async Task StartAsync_EncryptsOnlyPlaintextFieldsOnPartiallyMigratedConnection()
    {
        var encryptedAccessToken = _tokenProtector.Encrypt("access-token");
        var connection = MakeConnection();
        connection.AccessToken = encryptedAccessToken;
        await using (var db = CreateContext())
        {
            db.StoreConnections.Add(connection);
            await db.SaveChangesAsync();
        }

        await using var migrationDb = CreateContext();
        var service = CreateService(migrationDb);

        await service.StartAsync(CancellationToken.None);

        await using var assertDb = CreateContext();
        var persisted = await assertDb.StoreConnections.AsNoTracking().SingleAsync();
        Assert.That(persisted.AccessToken, Is.EqualTo(encryptedAccessToken));
        Assert.That(_tokenProtector.IsEncrypted(persisted.RefreshToken), Is.True);
        Assert.That(_tokenProtector.IsEncrypted(persisted.IdToken), Is.True);
        Assert.That(_tokenProtector.Decrypt(persisted.RefreshToken), Is.EqualTo("refresh-token"));
        Assert.That(_tokenProtector.Decrypt(persisted.IdToken), Is.EqualTo("id-token"));
    }

    private StoreConnectionTokenMigrationService CreateService(PantioDbContext db)
    {
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        serviceProviderMock
            .Setup(provider => provider.GetService(typeof(PantioDbContext)))
            .Returns(db);
        scopeMock
            .Setup(scope => scope.ServiceProvider)
            .Returns(serviceProviderMock.Object);
        scopeFactoryMock
            .Setup(factory => factory.CreateScope())
            .Returns(scopeMock.Object);

        return new StoreConnectionTokenMigrationService(
            scopeFactoryMock.Object,
            _tokenProtector,
            _loggerMock.Object);
    }

    private PantioDbContext CreateContext() => new(_options);

    private static StoreConnection MakeConnection()
    {
        return new StoreConnection
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Chain = StoreChain.Netto,
            GigyaSessionToken = "gigya-session-token",
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            IdToken = "id-token",
            ConnectedAt = DateTime.UtcNow,
            TokenExpiresAt = DateTime.UtcNow.AddHours(1)
        };
    }
}
