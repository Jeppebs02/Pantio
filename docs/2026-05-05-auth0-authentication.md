# Auth0 Authentication Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire Auth0 JWT authentication into the backend API, provision users from Auth0's post-registration Action, enforce ownership on all protected routes, and support full account deletion from both our DB and Auth0.

**Architecture:** JWT Bearer middleware validates every request against Auth0's JWKS endpoint. A global `Auth0OwnershipFilter` action filter resolves the JWT `sub` claim to an internal `User` row on every authenticated request, stores the resolved `Guid` in `HttpContext.Items`, and 403s if a `{userId}` route param is present but doesn't match. User provisioning uses Auth0's `post-user-registration` Action which POSTs to a public `/api/auth/register` endpoint protected by a shared secret header. Account deletion calls both our repository and Auth0's Management API.

**Tech Stack:** .NET 10, ASP.NET Core JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`), Auth0 Management API via `HttpClient`, NUnit 4 + Moq 4, EF Core InMemory for repository tests.

**Current status (2026-05-05):** Tasks 1-8 are implemented in the working tree and verified with `dotnet build backend/PantioAPI/PantioAPI.csproj` and `dotnet test backend/PantioTest/PantioTest.csproj` (`83` tests passed). Task 1 was committed separately; later task-level commit checkpoints were not preserved as separate commits and remain bookkeeping-only.

---

## File Map

### Create
- `backend/PantioClassLibrary/DTO/CreateUserDto.cs`
- `backend/PantioClassLibrary/DTO/UserDto.cs`
- `backend/PantioClassLibrary/Interfaces/Repository/IUserRepository.cs`
- `backend/PantioClassLibrary/Interfaces/Services/IUserService.cs`
- `backend/PantioClassLibrary/Interfaces/Services/IAuth0ManagementService.cs`
- `backend/PantioRepository/EntityFramework/Repositories/UserRepository.cs`
- `backend/PantioRepository/Mapper/UserMapper.cs`
- `backend/PantioAPI/Controllers/UserController.cs`
- `backend/PantioAPI/Services/UserService.cs`
- `backend/PantioAPI/Services/Auth0ManagementService.cs`
- `backend/PantioAPI/Filters/Auth0OwnershipFilter.cs`
- `backend/PantioTest/ControllerTests/UserControllerTests.cs`
- `backend/PantioTest/ServiceTests/UserServiceTests.cs`
- `backend/PantioTest/RepositoryTests/UserRepositoryTests.cs`
- EF migration: `AddAuth0SubAndMakePhoneNullable`

### Modify
- `backend/PantioClassLibrary/Entities/User.cs` — add `Auth0Sub`, make `PhoneNumber` nullable
- `backend/PantioRepository/EntityFramework/PantioDbContext.cs` — add unique index on `auth0_sub`
- `backend/PantioAPI/Program.cs` — JWT Bearer, global auth policy, register new services
- `backend/PantioAPI/Controllers/HealthController.cs` — add `[AllowAnonymous]`
- `backend/PantioAPI/PantioAPI.csproj` — add `Microsoft.AspNetCore.Authentication.JwtBearer`

---

## Task 1: Update User Entity and Add Migration ✅ DONE (commit `7a238957`)

> Note: `Microsoft.EntityFrameworkCore.Design` was also added to `PantioAPI.csproj` — required for `dotnet ef` tooling.

**Files:**
- Modify: `backend/PantioClassLibrary/Entities/User.cs`
- Modify: `backend/PantioRepository/EntityFramework/PantioDbContext.cs`
- Create: EF migration `AddAuth0SubAndMakePhoneNullable`

- [x] **Step 1: Update User entity**
- [x] **Step 2: Add unique index for auth0_sub in DbContext**
- [x] **Step 3: Generate migration**
- [x] **Step 4: Verify the project builds**
- [x] **Step 5: Commit**

Replace the contents of `backend/PantioClassLibrary/Entities/User.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PantioClassLibrary.Entities;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("email")]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    [Column("auth0_sub")]
    public string Auth0Sub { get; set; } = null!;

    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [Column("onboarding_done")]
    public bool OnboardingDone { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public UserProfile? Profile { get; set; }
    public ICollection<StoreConnection> StoreConnections { get; set; } = [];
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
    public ICollection<ShoppingList> ShoppingLists { get; set; } = [];
    public ICollection<Receipt> Receipts { get; set; } = [];
    public ICollection<ExpiryNotification> ExpiryNotifications { get; set; } = [];
    public ICollection<ProductCache> ProductCaches { get; set; } = [];
    public ICollection<Recipe> Recipes { get; set; } = [];
}
```

- [ ] **Step 2: Add unique index for auth0_sub in DbContext**

In `backend/PantioRepository/EntityFramework/PantioDbContext.cs`, add inside `OnModelCreating` under the `// ── Unique indexes ──` block:

```csharp
modelBuilder.Entity<User>()
    .HasIndex(x => x.Auth0Sub).IsUnique();
```

- [ ] **Step 3: Generate migration**

Run from the repo root:

```bash
dotnet ef migrations add AddAuth0SubAndMakePhoneNullable \
  --project backend/PantioRepository \
  --startup-project backend/PantioAPI
```

Expected: a new file appears in `backend/PantioRepository/EntityFramework/EFMigrations/`.

- [ ] **Step 4: Verify the project builds**

```bash
dotnet build backend/PantioAPI/PantioAPI.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add backend/PantioClassLibrary/Entities/User.cs \
        backend/PantioRepository/EntityFramework/PantioDbContext.cs \
        backend/PantioRepository/EntityFramework/EFMigrations/
git commit -m "feat: add auth0_sub to User entity and migration"
```

---

## Task 2: DTOs and Interfaces ✅ IMPLEMENTED, VERIFIED

**Files:**
- Create: `backend/PantioClassLibrary/DTO/CreateUserDto.cs`
- Create: `backend/PantioClassLibrary/DTO/UserDto.cs`
- Create: `backend/PantioClassLibrary/Interfaces/Repository/IUserRepository.cs`
- Create: `backend/PantioClassLibrary/Interfaces/Services/IUserService.cs`
- Create: `backend/PantioClassLibrary/Interfaces/Services/IAuth0ManagementService.cs`

- [x] **Step 1: Create CreateUserDto**

`backend/PantioClassLibrary/DTO/CreateUserDto.cs`:

```csharp
namespace PantioClassLibrary.DTO;

public record CreateUserDto(string Email, string Auth0Sub);
```

- [x] **Step 2: Create UserDto**

`backend/PantioClassLibrary/DTO/UserDto.cs`:

```csharp
namespace PantioClassLibrary.DTO;

public record UserDto(Guid Id, string Email, bool OnboardingDone);
```

- [x] **Step 3: Create IUserRepository**

`backend/PantioClassLibrary/Interfaces/Repository/IUserRepository.cs`:

```csharp
using PantioClassLibrary.Entities;

namespace PantioClassLibrary.Interfaces.Repository;

public interface IUserRepository
{
    Task<User> CreateAsync(User user, CancellationToken ct = default);
    Task<User?> GetByAuth0SubAsync(string auth0Sub, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
```

- [x] **Step 4: Create IUserService**

`backend/PantioClassLibrary/Interfaces/Services/IUserService.cs`:

```csharp
using PantioClassLibrary.DTO;

namespace PantioClassLibrary.Interfaces.Services;

public interface IUserService
{
    Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, CancellationToken ct = default);
}
```

- [x] **Step 5: Create IAuth0ManagementService**

`backend/PantioClassLibrary/Interfaces/Services/IAuth0ManagementService.cs`:

```csharp
namespace PantioClassLibrary.Interfaces.Services;

public interface IAuth0ManagementService
{
    Task DeleteUserAsync(string auth0Sub, CancellationToken ct = default);
}
```

- [x] **Step 6: Verify build**

```bash
dotnet build backend/PantioClassLibrary/PantioClassLibrary.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 7: Commit**

```bash
git add backend/PantioClassLibrary/DTO/CreateUserDto.cs \
        backend/PantioClassLibrary/DTO/UserDto.cs \
        backend/PantioClassLibrary/Interfaces/Repository/IUserRepository.cs \
        backend/PantioClassLibrary/Interfaces/Services/IUserService.cs \
        backend/PantioClassLibrary/Interfaces/Services/IAuth0ManagementService.cs
git commit -m "feat: add user DTOs and repository/service interfaces"
```

---

## Task 3: UserMapper + UserRepository (TDD) ✅ IMPLEMENTED, VERIFIED

**Files:**
- Create: `backend/PantioRepository/Mapper/UserMapper.cs`
- Create: `backend/PantioRepository/EntityFramework/Repositories/UserRepository.cs`
- Create: `backend/PantioTest/RepositoryTests/UserRepositoryTests.cs`

- [x] **Step 1: Write failing repository tests**

`backend/PantioTest/RepositoryTests/UserRepositoryTests.cs`:

```csharp
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
```

- [x] **Step 2: Run tests to verify they fail**

```bash
dotnet test backend/PantioTest/PantioTest.csproj --filter "UserRepositoryTests"
```

Expected: Build error — `UserRepository` does not exist yet.

- [x] **Step 3: Implement UserMapper**

`backend/PantioRepository/Mapper/UserMapper.cs`:

```csharp
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;

namespace PantioRepository.Mapper;

public static class UserMapper
{
    public static User ToEntity(CreateUserDto dto) => new()
    {
        Id = Guid.NewGuid(),
        Email = dto.Email,
        Auth0Sub = dto.Auth0Sub,
        OnboardingDone = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static UserDto ToDto(User user) => new(user.Id, user.Email, user.OnboardingDone);
}
```

- [x] **Step 4: Implement UserRepository**

`backend/PantioRepository/EntityFramework/Repositories/UserRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;

namespace PantioRepository.EntityFramework.Repositories;

public class UserRepository(PantioDbContext db) : IUserRepository
{
    public async Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User?> GetByAuth0SubAsync(string auth0Sub, CancellationToken ct = default)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Auth0Sub == auth0Sub, ct);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Users.FindAsync([id], ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return false;
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
```

- [x] **Step 5: Run tests to verify they pass**

```bash
dotnet test backend/PantioTest/PantioTest.csproj --filter "UserRepositoryTests"
```

Expected: 7 tests passed, 0 failed.

- [ ] **Step 6: Commit**

```bash
git add backend/PantioRepository/Mapper/UserMapper.cs \
        backend/PantioRepository/EntityFramework/Repositories/UserRepository.cs \
        backend/PantioTest/RepositoryTests/UserRepositoryTests.cs
git commit -m "feat: add UserMapper, UserRepository, and repository tests"
```

---

## Task 4: Auth0ManagementService (TDD) ✅ IMPLEMENTED, VERIFIED

> Reality note: the passing repository version asserts against `deleteRequest.RequestUri!.AbsoluteUri` in the test, and the service throws `InvalidOperationException` if required Auth0 config keys are missing.

**Files:**
- Create: `backend/PantioAPI/Services/Auth0ManagementService.cs`
- Create: `backend/PantioTest/ServiceTests/Auth0ManagementServiceTests.cs`

- [x] **Step 1: Write failing service tests**

`backend/PantioTest/ServiceTests/Auth0ManagementServiceTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Moq;
using PantioAPI.Services;

namespace PantioTest.ServiceTests;

public class Auth0ManagementServiceTests
{
    private const string Domain = "test.auth0.com";
    private const string ClientId = "test-client-id";
    private const string ClientSecret = "test-client-secret";

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth0:ManagementDomain"] = Domain,
                ["Auth0:ManagementClientId"] = ClientId,
                ["Auth0:ManagementClientSecret"] = ClientSecret
            })
            .Build();

    private static HttpClient BuildHttpClient(Func<HttpRequestMessage, HttpResponseMessage> handler) =>
        new(new FakeHttpMessageHandler(handler));

    private class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(handler(request));
    }

    private static HttpResponseMessage TokenResponse(string accessToken) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { access_token = accessToken }),
                System.Text.Encoding.UTF8,
                "application/json")
        };

    [Test]
    public async Task DeleteUserAsync_ValidSub_SendsDeleteRequestToAuth0()
    {
        #region Arrange
        var requestLog = new List<HttpRequestMessage>();
        var callCount = 0;
        var httpClient = BuildHttpClient(request =>
        {
            requestLog.Add(request);
            callCount++;
            return callCount == 1
                ? TokenResponse("fake-token")
                : new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var service = new Auth0ManagementService(httpClient, BuildConfig());
        #endregion

        #region Act
        await service.DeleteUserAsync("auth0|abc123");
        #endregion

        #region Assert
        Assert.That(requestLog.Count, Is.EqualTo(2));
        var deleteRequest = requestLog[1];
        Assert.That(deleteRequest.Method, Is.EqualTo(HttpMethod.Delete));
        Assert.That(deleteRequest.RequestUri!.AbsoluteUri,
            Does.Contain("api/v2/users/auth0%7Cabc123"));
        Assert.That(deleteRequest.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(deleteRequest.Headers.Authorization.Parameter, Is.EqualTo("fake-token"));
        #endregion
    }

    [Test]
    public void DeleteUserAsync_TokenRequestFails_ThrowsHttpRequestException()
    {
        #region Arrange
        var httpClient = BuildHttpClient(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var service = new Auth0ManagementService(httpClient, BuildConfig());
        #endregion

        #region Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(() =>
            service.DeleteUserAsync("auth0|abc123"));
        #endregion
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

```bash
dotnet test backend/PantioTest/PantioTest.csproj --filter "Auth0ManagementServiceTests"
```

Expected: Build error — `Auth0ManagementService` does not exist yet.

- [x] **Step 3: Implement Auth0ManagementService**

`backend/PantioAPI/Services/Auth0ManagementService.cs`:

```csharp
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Services;

public class Auth0ManagementService(HttpClient httpClient, IConfiguration config) : IAuth0ManagementService
{
    public async Task DeleteUserAsync(string auth0Sub, CancellationToken ct = default)
    {
        var domain = config["Auth0:ManagementDomain"]!;
        var clientId = config["Auth0:ManagementClientId"]!;
        var clientSecret = config["Auth0:ManagementClientSecret"]!;

        var tokenResponse = await httpClient.PostAsJsonAsync(
            $"https://{domain}/oauth/token",
            new
            {
                grant_type = "client_credentials",
                client_id = clientId,
                client_secret = clientSecret,
                audience = $"https://{domain}/api/v2/"
            },
            ct);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var accessToken = tokenData.GetProperty("access_token").GetString()!;

        var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"https://{domain}/api/v2/users/{Uri.EscapeDataString(auth0Sub)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var deleteResponse = await httpClient.SendAsync(request, ct);
        deleteResponse.EnsureSuccessStatusCode();
    }
}
```

- [x] **Step 4: Run tests to verify they pass**

```bash
dotnet test backend/PantioTest/PantioTest.csproj --filter "Auth0ManagementServiceTests"
```

Expected: 2 tests passed, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add backend/PantioAPI/Services/Auth0ManagementService.cs \
        backend/PantioTest/ServiceTests/Auth0ManagementServiceTests.cs
git commit -m "feat: add Auth0ManagementService with Management API user deletion"
```

---

## Task 5: UserService (TDD) ✅ IMPLEMENTED, VERIFIED

> Reality note: the current `UserService` is intentionally minimal. It has no `ILogger<UserService>` dependency, and account deletion calls Auth0 first, then deletes the local user row.

**Files:**
- Create: `backend/PantioAPI/Services/UserService.cs`
- Create: `backend/PantioTest/ServiceTests/UserServiceTests.cs`

- [x] **Step 1: Write failing service tests**

`backend/PantioTest/ServiceTests/UserServiceTests.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Moq;
using PantioAPI.Services;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ServiceTests;

public class UserServiceTests
{
    private Mock<IUserRepository> _repositoryMock = null!;
    private Mock<IAuth0ManagementService> _auth0Mock = null!;
    private UserService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _auth0Mock = new Mock<IAuth0ManagementService>();
        _service = new UserService(
            _repositoryMock.Object,
            _auth0Mock.Object,
            Mock.Of<ILogger<UserService>>());
    }

    private static User MakeUser(Guid? id = null, string sub = "auth0|abc") =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Email = "user@example.com",
            Auth0Sub = sub,
            OnboardingDone = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    [Test]
    public async Task CreateAsync_ValidDto_ReturnsCorrectUserDto()
    {
        #region Arrange
        var dto = new CreateUserDto("user@example.com", "auth0|abc");
        var entity = MakeUser();
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        #endregion

        #region Act
        var result = await _service.CreateAsync(dto);
        #endregion

        #region Assert
        Assert.That(result.Email, Is.EqualTo(entity.Email));
        Assert.That(result.Id, Is.EqualTo(entity.Id));
        #endregion
    }

    [Test]
    public async Task CreateAsync_ValidDto_CallsRepositoryOnce()
    {
        #region Arrange
        var dto = new CreateUserDto("user@example.com", "auth0|abc");
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);
        #endregion

        #region Act
        await _service.CreateAsync(dto);
        #endregion

        #region Assert
        _repositoryMock.Verify(
            r => r.CreateAsync(
                It.Is<User>(u => u.Email == "user@example.com" && u.Auth0Sub == "auth0|abc"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        #endregion
    }

    [Test]
    public async Task DeleteAsync_ExistingUser_DeletesFromRepoAndAuth0()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var user = MakeUser(userId, "auth0|todelete");
        _repositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _repositoryMock
            .Setup(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _auth0Mock
            .Setup(a => a.DeleteUserAsync("auth0|todelete", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        #endregion

        #region Act
        var result = await _service.DeleteAsync(userId);
        #endregion

        #region Assert
        Assert.That(result, Is.True);
        _repositoryMock.Verify(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _auth0Mock.Verify(a => a.DeleteUserAsync("auth0|todelete", It.IsAny<CancellationToken>()), Times.Once);
        #endregion
    }

    [Test]
    public async Task DeleteAsync_NonExistentUser_ReturnsFalseAndDoesNotCallAuth0()
    {
        #region Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        #endregion

        #region Act
        var result = await _service.DeleteAsync(Guid.NewGuid());
        #endregion

        #region Assert
        Assert.That(result, Is.False);
        _auth0Mock.Verify(a => a.DeleteUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

```bash
dotnet test backend/PantioTest/PantioTest.csproj --filter "UserServiceTests"
```

Expected: Build error — `UserService` does not exist yet.

- [x] **Step 3: Implement UserService**

`backend/PantioAPI/Services/UserService.cs`:

```csharp
using Microsoft.Extensions.Logging;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioRepository.Mapper;

namespace PantioAPI.Services;

public class UserService(
    IUserRepository userRepository,
    IAuth0ManagementService auth0Management,
    ILogger<UserService> logger) : IUserService
{
    public async Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        var entity = UserMapper.ToEntity(dto);
        var created = await userRepository.CreateAsync(entity, ct);
        logger.LogInformation("User {UserId} created", created.Id);
        return UserMapper.ToDto(created);
    }

    public async Task<bool> DeleteAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
        {
            logger.LogWarning("Delete requested for non-existent user {UserId}", userId);
            return false;
        }

        await userRepository.DeleteAsync(userId, ct);
        await auth0Management.DeleteUserAsync(user.Auth0Sub, ct);
        logger.LogInformation("User {UserId} deleted", userId);
        return true;
    }
}
```

- [x] **Step 4: Run tests to verify they pass**

```bash
dotnet test backend/PantioTest/PantioTest.csproj --filter "UserServiceTests"
```

Expected: 4 tests passed, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add backend/PantioAPI/Services/UserService.cs \
        backend/PantioTest/ServiceTests/UserServiceTests.cs
git commit -m "feat: add UserService with Auth0 management delegation"
```

---

## Task 6: JWT Bearer Auth in Program.cs ✅ IMPLEMENTED, VERIFIED

**Files:**
- Modify: `backend/PantioAPI/PantioAPI.csproj`
- Modify: `backend/PantioAPI/Program.cs`
- Modify: `backend/PantioAPI/Controllers/HealthController.cs`

- [x] **Step 1: Add JwtBearer NuGet package**

```bash
dotnet add backend/PantioAPI/PantioAPI.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
```

Expected: Package added, version 10.x.

- [x] **Step 2: Update Program.cs with auth**

Replace `backend/PantioAPI/Program.cs` with:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PantioAPI.EntityFramework;
using PantioAPI.Filters;
using PantioAPI.Services;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioRepository.EntityFramework;
using PantioRepository.EntityFramework.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PantioDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure()
    )
);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth0:Authority"];
        options.Audience = builder.Configuration["Auth0:Audience"];
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
builder.Services.AddScoped<IInventoryItemService, InventoryItemService>();
builder.Services.AddScoped<IAuth0ManagementService, Auth0ManagementService>();
builder.Services.AddHttpClient<Auth0ManagementService>();

builder.Services.AddScoped<Auth0OwnershipFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<Auth0OwnershipFilter>();
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

Note: `Auth0OwnershipFilter` and its using are referenced here — it will be implemented in Task 7. The project will not build until Task 7 is complete.

- [x] **Step 3: Add [AllowAnonymous] to HealthController**

Replace `backend/PantioAPI/Controllers/HealthController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PantioAPI.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("Healthy");
    }
}
```

- [ ] **Step 4: Commit (build will complete after Task 7)**

```bash
git add backend/PantioAPI/PantioAPI.csproj \
        backend/PantioAPI/Program.cs \
        backend/PantioAPI/Controllers/HealthController.cs
git commit -m "feat: add JWT Bearer auth, global auth policy, and health endpoint exemption"
```

---

## Task 7: Auth0OwnershipFilter (TDD) ✅ IMPLEMENTED, VERIFIED

**Files:**
- Create: `backend/PantioAPI/Filters/Auth0OwnershipFilter.cs`
- Create: `backend/PantioTest/ServiceTests/Auth0OwnershipFilterTests.cs`

- [x] **Step 1: Write failing filter tests**

`backend/PantioTest/ServiceTests/Auth0OwnershipFilterTests.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using PantioAPI.Filters;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;

namespace PantioTest.ServiceTests;

public class Auth0OwnershipFilterTests
{
    private Mock<IUserRepository> _repoMock = null!;
    private Auth0OwnershipFilter _filter = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<IUserRepository>();
        _filter = new Auth0OwnershipFilter(_repoMock.Object);
    }

    private static ActionExecutingContext BuildContext(
        string? sub,
        Guid? routeUserId = null,
        bool allowAnonymous = false)
    {
        var claims = sub is not null
            ? new[] { new Claim("sub", sub) }
            : Array.Empty<Claim>();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims))
        };

        if (allowAnonymous)
        {
            var endpointMetadata = new EndpointMetadataCollection(new AllowAnonymousAttribute());
            httpContext.SetEndpoint(new Endpoint(null, endpointMetadata, "test"));
        }

        var actionArguments = new Dictionary<string, object?>();
        if (routeUserId.HasValue)
            actionArguments["userId"] = routeUserId.Value;

        return new ActionExecutingContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            [],
            actionArguments,
            new object());
    }

    private static ActionExecutionDelegate NextDelegate() =>
        () => Task.FromResult(new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            [],
            new object()));

    [Test]
    public async Task OnActionExecutionAsync_AllowAnonymousEndpoint_SkipsValidation()
    {
        #region Arrange
        var context = BuildContext(sub: null, allowAnonymous: true);
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                [],
                new object()));
        };
        #endregion

        #region Act
        await _filter.OnActionExecutionAsync(context, next);
        #endregion

        #region Assert
        Assert.That(nextCalled, Is.True);
        Assert.That(context.Result, Is.Null);
        #endregion
    }

    [Test]
    public async Task OnActionExecutionAsync_MissingSubClaim_Returns401()
    {
        #region Arrange
        var context = BuildContext(sub: null);
        #endregion

        #region Act
        await _filter.OnActionExecutionAsync(context, NextDelegate());
        #endregion

        #region Assert
        Assert.That(context.Result, Is.InstanceOf<UnauthorizedResult>());
        #endregion
    }

    [Test]
    public async Task OnActionExecutionAsync_UnknownSub_Returns401()
    {
        #region Arrange
        _repoMock
            .Setup(r => r.GetByAuth0SubAsync("auth0|unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var context = BuildContext("auth0|unknown");
        #endregion

        #region Act
        await _filter.OnActionExecutionAsync(context, NextDelegate());
        #endregion

        #region Assert
        Assert.That(context.Result, Is.InstanceOf<UnauthorizedResult>());
        #endregion
    }

    [Test]
    public async Task OnActionExecutionAsync_RouteUserIdMatchesToken_CallsNext()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "u@e.com", Auth0Sub = "auth0|abc" };
        _repoMock
            .Setup(r => r.GetByAuth0SubAsync("auth0|abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var context = BuildContext("auth0|abc", routeUserId: userId);
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                [],
                new object()));
        };
        #endregion

        #region Act
        await _filter.OnActionExecutionAsync(context, next);
        #endregion

        #region Assert
        Assert.That(nextCalled, Is.True);
        Assert.That(context.Result, Is.Null);
        Assert.That(context.HttpContext.Items["AuthenticatedUserId"], Is.EqualTo(userId));
        #endregion
    }

    [Test]
    public async Task OnActionExecutionAsync_RouteUserIdMismatch_Returns403()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "u@e.com", Auth0Sub = "auth0|abc" };
        _repoMock
            .Setup(r => r.GetByAuth0SubAsync("auth0|abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var context = BuildContext("auth0|abc", routeUserId: Guid.NewGuid()); // different GUID
        #endregion

        #region Act
        await _filter.OnActionExecutionAsync(context, NextDelegate());
        #endregion

        #region Assert
        Assert.That(context.Result, Is.InstanceOf<ForbidResult>());
        #endregion
    }

    [Test]
    public async Task OnActionExecutionAsync_NoRouteUserId_StoresUserIdAndCallsNext()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "u@e.com", Auth0Sub = "auth0|abc" };
        _repoMock
            .Setup(r => r.GetByAuth0SubAsync("auth0|abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var context = BuildContext("auth0|abc"); // no routeUserId
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                [],
                new object()));
        };
        #endregion

        #region Act
        await _filter.OnActionExecutionAsync(context, next);
        #endregion

        #region Assert
        Assert.That(nextCalled, Is.True);
        Assert.That(context.HttpContext.Items["AuthenticatedUserId"], Is.EqualTo(userId));
        #endregion
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

```bash
dotnet test backend/PantioTest/PantioTest.csproj --filter "Auth0OwnershipFilterTests"
```

Expected: Build error — `Auth0OwnershipFilter` does not exist yet.

- [x] **Step 3: Implement Auth0OwnershipFilter**

Create directory `backend/PantioAPI/Filters/` then create `backend/PantioAPI/Filters/Auth0OwnershipFilter.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PantioClassLibrary.Interfaces.Repository;

namespace PantioAPI.Filters;

public class Auth0OwnershipFilter(IUserRepository userRepository) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next();
            return;
        }

        var sub = context.HttpContext.User.FindFirst("sub")?.Value
                  ?? context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (sub is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var user = await userRepository.GetByAuth0SubAsync(sub, context.HttpContext.RequestAborted);
        if (user is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items["AuthenticatedUserId"] = user.Id;

        if (context.ActionArguments.TryGetValue("userId", out var routeValue) &&
            routeValue is Guid routeUserId &&
            routeUserId != user.Id)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
```

- [x] **Step 4: Run tests to verify they pass**

```bash
dotnet test backend/PantioTest/PantioTest.csproj --filter "Auth0OwnershipFilterTests"
```

Expected: 6 tests passed, 0 failed.

- [x] **Step 5: Verify the full project builds**

```bash
dotnet build backend/PantioAPI/PantioAPI.csproj
```

Expected: Build succeeded, 0 errors. (Program.cs references to Auth0OwnershipFilter now resolve.)

- [x] **Step 6: Run all tests**

```bash
dotnet test backend/PantioTest/PantioTest.csproj
```

Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add backend/PantioAPI/Filters/Auth0OwnershipFilter.cs \
        backend/PantioTest/ServiceTests/Auth0OwnershipFilterTests.cs
git commit -m "feat: add Auth0OwnershipFilter to validate JWT ownership on all routes"
```

---

## Task 8: UserController (TDD) + DI Wiring ✅ IMPLEMENTED, VERIFIED

**Files:**
- Create: `backend/PantioAPI/Controllers/UserController.cs`
- Create: `backend/PantioTest/ControllerTests/UserControllerTests.cs`

- [x] **Step 1: Write failing controller tests**

`backend/PantioTest/ControllerTests/UserControllerTests.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using PantioAPI.Controllers;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ControllerTests;

public class UserControllerTests
{
    private Mock<IUserService> _serviceMock = null!;
    private IConfiguration _config = null!;
    private UserController _controller = null!;

    private const string ValidSecret = "super-secret";

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IUserService>();
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth0:RegistrationSecret"] = ValidSecret
            })
            .Build();
        _controller = new UserController(_serviceMock.Object, _config);
    }

    [Test]
    public async Task Register_ValidSecretAndDto_Returns200WithUserDto()
    {
        #region Arrange
        var dto = new CreateUserDto("test@example.com", "auth0|abc");
        var userDto = new UserDto(Guid.NewGuid(), "test@example.com", false);
        _serviceMock
            .Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);
        #endregion

        #region Act
        var result = await _controller.Register(dto, ValidSecret, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(userDto));
        #endregion
    }

    [Test]
    public async Task Register_WrongSecret_Returns401()
    {
        #region Arrange
        var dto = new CreateUserDto("test@example.com", "auth0|abc");
        #endregion

        #region Act
        var result = await _controller.Register(dto, "wrong-secret", CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        #endregion
    }

    [Test]
    public async Task Register_MissingSecret_Returns401()
    {
        #region Arrange
        var dto = new CreateUserDto("test@example.com", "auth0|abc");
        #endregion

        #region Act
        var result = await _controller.Register(dto, null, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        #endregion
    }

    [Test]
    public async Task Delete_ExistingUser_Returns204()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.DeleteAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        #endregion

        #region Act
        var result = await _controller.Delete(userId, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
        #endregion
    }

    [Test]
    public async Task Delete_NonExistentUser_Returns404()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        #endregion

        #region Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        #endregion
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

```bash
dotnet test backend/PantioTest/PantioTest.csproj --filter "UserControllerTests"
```

Expected: Build error — `UserController` does not exist yet.

- [x] **Step 3: Implement UserController**

`backend/PantioAPI/Controllers/UserController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Controllers;

[ApiController]
public class UserController(IUserService service, IConfiguration config) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("api/auth/register")]
    public async Task<IActionResult> Register(
        [FromBody] CreateUserDto dto,
        [FromHeader(Name = "X-Registration-Secret")] string? secret,
        CancellationToken ct)
    {
        if (secret != config["Auth0:RegistrationSecret"])
            return Unauthorized();

        var user = await service.CreateAsync(dto, ct);
        return Ok(user);
    }

    [HttpDelete("api/users/{userId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(userId, ct);
        return deleted ? NoContent() : NotFound();
    }
}
```

- [x] **Step 4: Run tests to verify they pass**

```bash
dotnet test backend/PantioTest/PantioTest.csproj --filter "UserControllerTests"
```

Expected: 5 tests passed, 0 failed.

- [x] **Step 5: Run all tests**

```bash
dotnet test backend/PantioTest/PantioTest.csproj
```

Expected: All tests pass (previous tests still green).

- [x] **Step 6: Verify final build**

```bash
dotnet build backend/PantioAPI/PantioAPI.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 7: Commit**

```bash
git add backend/PantioAPI/Controllers/UserController.cs \
        backend/PantioTest/ControllerTests/UserControllerTests.cs
git commit -m "feat: add UserController with registration and account deletion endpoints"
```

---

## Required Docker Environment Variables

Add these to your Docker run config or `docker-compose.yml` before testing end-to-end:

```
AUTH0__AUTHORITY=https://{your-auth0-domain}/
AUTH0__AUDIENCE={your-api-identifier}
AUTH0__MANAGEMENT_DOMAIN={your-auth0-domain}
AUTH0__MANAGEMENT_CLIENT_ID={m2m-client-id}
AUTH0__MANAGEMENT_CLIENT_SECRET={m2m-client-secret}
AUTH0__REGISTRATION_SECRET={shared-secret-for-auth0-action}
```

The Auth0 Action (post-user-registration trigger) must POST to `https://{your-api-host}/api/auth/register` with:
- Header: `X-Registration-Secret: {AUTH0__REGISTRATION_SECRET}`
- Body: `{ "email": "{{user.email}}", "auth0Sub": "{{user.user_id}}" }`

---

## Self-Review Against Spec

| Requirement | Covered by |
|---|---|
| Create account via Auth0 | Task 1 (entity), Task 3 (repo), Task 5 (service), Task 8 (controller + register endpoint) |
| Log in / log out (JWT validation) | Task 6 (JWT Bearer middleware) |
| Delete account + all data | Task 4 (Auth0 Management API), Task 5 (service orchestration), Task 8 (DELETE endpoint) |
| auth0_sub VARCHAR(128) UNIQUE | Task 1 (entity + migration) |
| Route ownership validation | Task 7 (Auth0OwnershipFilter) |
| /health is public | Task 6 (AllowAnonymous on HealthController) |
| Registration endpoint secured by shared secret | Task 8 (X-Registration-Secret header check) |
| Docker env vars for Auth0 config | Task 6 + env var table above |
