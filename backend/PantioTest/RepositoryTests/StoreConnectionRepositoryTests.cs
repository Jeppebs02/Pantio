using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioRepository.EntityFramework;
using PantioRepository.EntityFramework.Repositories;
using PantioRepository.Security;

namespace PantioTest.RepositoryTests;

public class StoreConnectionRepositoryTests
{
    private DbContextOptions<PantioDbContext> _options = null!;
    private StoreConnectionTokenProtector _tokenProtector = null!;

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
    }

    private PantioDbContext CreateContext() => new(_options);

    private StoreConnectionRepository CreateRepository(PantioDbContext db) => new(db, _tokenProtector);

    [Test]
    public async Task CreateAsync_EncryptsPersistedTokensAndReturnsPlaintextTokens()
    {
        var connection = MakeConnection();

        await using (var db = CreateContext())
        {
            var result = await CreateRepository(db).CreateAsync(connection);

            Assert.That(result.AccessToken, Is.EqualTo("access-token"));
            Assert.That(result.RefreshToken, Is.EqualTo("refresh-token"));
            Assert.That(result.IdToken, Is.EqualTo("id-token"));
            Assert.That(result.GigyaSessionToken, Is.EqualTo("gigya-session-token"));
        }

        await using (var db = CreateContext())
        {
            var persisted = await db.StoreConnections.AsNoTracking().SingleAsync();

            Assert.That(_tokenProtector.IsEncrypted(persisted.AccessToken), Is.True);
            Assert.That(_tokenProtector.IsEncrypted(persisted.RefreshToken), Is.True);
            Assert.That(_tokenProtector.IsEncrypted(persisted.IdToken), Is.True);
            Assert.That(_tokenProtector.IsEncrypted(persisted.GigyaSessionToken), Is.True);
        }
    }

    [Test]
    public async Task GetByIdAsync_DecryptsPersistedTokens()
    {
        var connection = MakeConnection();

        await using (var db = CreateContext())
        {
            await CreateRepository(db).CreateAsync(connection);
        }

        await using (var db = CreateContext())
        {
            var result = await CreateRepository(db).GetByIdAsync(connection.UserId, connection.Id);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.AccessToken, Is.EqualTo("access-token"));
            Assert.That(result.RefreshToken, Is.EqualTo("refresh-token"));
            Assert.That(result.IdToken, Is.EqualTo("id-token"));
            Assert.That(result.GigyaSessionToken, Is.EqualTo("gigya-session-token"));
        }
    }

    [Test]
    public async Task UpdateAsync_EncryptsUpdatedTokensAndReturnsPlaintextTokens()
    {
        var connection = MakeConnection();

        await using (var db = CreateContext())
        {
            await CreateRepository(db).CreateAsync(connection);
        }

        connection.AccessToken = "updated-access-token";
        connection.RefreshToken = "updated-refresh-token";
        connection.IdToken = "updated-id-token";

        await using (var db = CreateContext())
        {
            var result = await CreateRepository(db).UpdateAsync(connection);

            Assert.That(result.AccessToken, Is.EqualTo("updated-access-token"));
            Assert.That(result.RefreshToken, Is.EqualTo("updated-refresh-token"));
            Assert.That(result.IdToken, Is.EqualTo("updated-id-token"));
        }

        await using (var db = CreateContext())
        {
            var persisted = await db.StoreConnections.AsNoTracking().SingleAsync();

            Assert.That(_tokenProtector.IsEncrypted(persisted.AccessToken), Is.True);
            Assert.That(_tokenProtector.Decrypt(persisted.AccessToken), Is.EqualTo("updated-access-token"));
            Assert.That(_tokenProtector.Decrypt(persisted.RefreshToken), Is.EqualTo("updated-refresh-token"));
            Assert.That(_tokenProtector.Decrypt(persisted.IdToken), Is.EqualTo("updated-id-token"));
        }
    }

    [Test]
    public async Task UpdateAutoSyncAsync_ExistingConnection_PersistsFlag()
    {
        var connection = MakeConnection();

        await using (var db = CreateContext())
        {
            await CreateRepository(db).CreateAsync(connection);
        }

        await using (var db = CreateContext())
        {
            var result = await CreateRepository(db).UpdateAutoSyncAsync(connection.UserId, connection.Id, true);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.AutoSyncEnabled, Is.True);
        }

        await using (var db = CreateContext())
        {
            var persisted = await db.StoreConnections.AsNoTracking().SingleAsync();

            Assert.That(persisted.AutoSyncEnabled, Is.True);
        }
    }

    [Test]
    public async Task GetDueForAutoSyncAsync_ReturnsOnlyEnabledConnectedDueConnections()
    {
        var dueBefore = DateTime.UtcNow.AddHours(-6);
        var due = MakeConnection(autoSyncEnabled: true, lastPolledAt: dueBefore.AddMinutes(-1));
        var neverPolled = MakeConnection(autoSyncEnabled: true, lastPolledAt: null);
        var disabled = MakeConnection(autoSyncEnabled: false, lastPolledAt: dueBefore.AddMinutes(-1));
        var disconnected = MakeConnection(autoSyncEnabled: true, lastPolledAt: dueBefore.AddMinutes(-1), disconnectedAt: DateTime.UtcNow);
        var notDue = MakeConnection(autoSyncEnabled: true, lastPolledAt: dueBefore.AddMinutes(1));

        await using (var db = CreateContext())
        {
            await CreateRepository(db).CreateAsync(due);
            await CreateRepository(db).CreateAsync(neverPolled);
            await CreateRepository(db).CreateAsync(disabled);
            await CreateRepository(db).CreateAsync(disconnected);
            await CreateRepository(db).CreateAsync(notDue);
        }

        await using (var db = CreateContext())
        {
            var results = await CreateRepository(db).GetDueForAutoSyncAsync(dueBefore);

            Assert.That(results.Select(connection => connection.Id), Is.EquivalentTo(new[] { due.Id, neverPolled.Id }));
            Assert.That(results.All(connection => connection.AutoSyncEnabled), Is.True);
            Assert.That(results.All(connection => connection.DisconnectedAt is null), Is.True);
        }
    }

    private static StoreConnection MakeConnection()
        => MakeConnection(autoSyncEnabled: false, lastPolledAt: null, disconnectedAt: null);

    private static StoreConnection MakeConnection(bool autoSyncEnabled, DateTime? lastPolledAt, DateTime? disconnectedAt = null)
    {
        return new StoreConnection
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Chain = StoreChain.Netto,
            AutoSyncEnabled = autoSyncEnabled,
            GigyaSessionToken = "gigya-session-token",
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            IdToken = "id-token",
            LastPolledAt = lastPolledAt,
            DisconnectedAt = disconnectedAt,
            ConnectedAt = DateTime.UtcNow,
            TokenExpiresAt = DateTime.UtcNow.AddHours(1)
        };
    }
}
