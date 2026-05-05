using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioRepository.EntityFramework;
using PantioRepository.EntityFramework.Repositories;

namespace PantioTest.RepositoryTests;

public class UserRepositoryTests
{
    private DbContextOptions<PantioDbContext> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _options = new DbContextOptionsBuilder<PantioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private PantioDbContext CreateContext() => new(_options);

    private static User MakeUser(string email = "test@example.com", string sub = "auth0|abc123") =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            Auth0Sub = sub,
            OnboardingDone = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    [Test]
    public async Task CreateAsync_ValidUser_PersistsToDatabase()
    {
        #region Arrange
        var user = MakeUser();
        #endregion

        #region Act
        await using (var db = CreateContext())
        {
            await new UserRepository(db).CreateAsync(user);
        }
        #endregion

        #region Assert
        await using (var db = CreateContext())
        {
            Assert.That(await db.Users.FindAsync(user.Id), Is.Not.Null);
        }
        #endregion
    }

    [Test]
    public async Task CreateAsync_ValidUser_ReturnsEntityWithSameId()
    {
        #region Arrange
        var user = MakeUser();
        #endregion

        #region Act
        await using var db = CreateContext();
        var result = await new UserRepository(db).CreateAsync(user);
        #endregion

        #region Assert
        Assert.That(result.Id, Is.EqualTo(user.Id));
        Assert.That(result.Email, Is.EqualTo(user.Email));
        Assert.That(result.Auth0Sub, Is.EqualTo(user.Auth0Sub));
        #endregion
    }

    [Test]
    public async Task GetByAuth0SubAsync_ExistingUser_ReturnsCorrectUser()
    {
        #region Arrange
        var user = MakeUser(sub: "auth0|findme");
        await using (var db = CreateContext())
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        User? result;
        await using (var db = CreateContext())
        {
            result = await new UserRepository(db).GetByAuth0SubAsync("auth0|findme");
        }
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(user.Id));
        #endregion
    }

    [Test]
    public async Task GetByAuth0SubAsync_UnknownSub_ReturnsNull()
    {
        #region Arrange
        await using var db = CreateContext();
        #endregion

        #region Act
        var result = await new UserRepository(db).GetByAuth0SubAsync("auth0|doesnotexist");
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    public async Task GetByIdAsync_ExistingUser_ReturnsCorrectUser()
    {
        #region Arrange
        var user = MakeUser();
        await using (var db = CreateContext())
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        User? result;
        await using (var db = CreateContext())
        {
            result = await new UserRepository(db).GetByIdAsync(user.Id);
        }
        #endregion

        #region Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(user.Id));
        #endregion
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        #region Arrange
        await using var db = CreateContext();
        #endregion

        #region Act
        var result = await new UserRepository(db).GetByIdAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.Null);
        #endregion
    }

    [Test]
    public async Task DeleteAsync_ExistingUser_ReturnsTrueAndRemovesFromDatabase()
    {
        #region Arrange
        var user = MakeUser();
        await using (var db = CreateContext())
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        #endregion

        #region Act
        bool deleted;
        await using (var db = CreateContext())
        {
            deleted = await new UserRepository(db).DeleteAsync(user.Id);
        }
        #endregion

        #region Assert
        Assert.That(deleted, Is.True);
        await using (var db = CreateContext())
        {
            Assert.That(await db.Users.FindAsync(user.Id), Is.Null);
        }
        #endregion
    }

    [Test]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        #region Arrange
        await using var db = CreateContext();
        #endregion

        #region Act
        var result = await new UserRepository(db).DeleteAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.False);
        #endregion
    }
}
