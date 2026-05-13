using PantioClassLibrary.Entities;

namespace PantioClassLibrary.Interfaces.Repository;

public interface IUserRepository
{
    Task<User> CreateAsync(User user, CancellationToken ct = default);
    Task<User?> GetByAuth0SubAsync(string auth0Sub, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task UpdateLastActivityAsync(Guid id, DateTime timestamp, CancellationToken ct = default);
    Task<List<User>> GetUsersToWarnAsync(DateTime activityCutoff, CancellationToken ct = default);
    Task<List<User>> GetUsersToDeleteAsync(DateTime activityCutoff, CancellationToken ct = default);
    Task SetDeletionWarningSentAsync(Guid id, DateTime timestamp, CancellationToken ct = default);
    Task UpdateFcmTokenAsync(Guid id, string token, CancellationToken ct = default);
}
