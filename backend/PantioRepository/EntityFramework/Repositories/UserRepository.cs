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

    public async Task UpdateLastActivityAsync(Guid id, DateTime timestamp, CancellationToken ct = default)
    {
        await db.Users
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastActivityAt, timestamp), ct);
    }

    public async Task<List<User>> GetUsersToWarnAsync(DateTime activityCutoff, CancellationToken ct = default)
    {
        return await db.Users
            .Where(u => (u.LastActivityAt ?? u.CreatedAt) <= activityCutoff && u.DeletionWarningSentAt == null)
            .ToListAsync(ct);
    }

    public async Task<List<User>> GetUsersToDeleteAsync(DateTime activityCutoff, CancellationToken ct = default)
    {
        return await db.Users
            .Where(u => (u.LastActivityAt ?? u.CreatedAt) <= activityCutoff)
            .ToListAsync(ct);
    }

    public async Task SetDeletionWarningSentAsync(Guid id, DateTime timestamp, CancellationToken ct = default)
    {
        await db.Users
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.DeletionWarningSentAt, timestamp), ct);
    }
}
