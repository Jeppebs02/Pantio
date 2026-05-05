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
