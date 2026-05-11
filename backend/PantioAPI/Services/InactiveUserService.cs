using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Services;

public class InactiveUserService(
    IUserRepository userRepository,
    IAuth0ManagementService auth0ManagementService,
    ILogger<InactiveUserService> logger) : IInactiveUserService
{
    private const int InactivityMonths = 12;
    private const int WarningMonths = 11;

    public async Task RunCheckAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var usersToWarn = await userRepository.GetUsersToWarnAsync(now.AddMonths(-WarningMonths), ct);
        foreach (var user in usersToWarn)
        {
            await userRepository.SetDeletionWarningSentAsync(user.Id, now, ct);
            logger.LogInformation("Deletion warning stamped for user {UserId} (inactive since {LastActivity})",
                user.Id, user.LastActivityAt ?? user.CreatedAt);
        }

        var usersToDelete = await userRepository.GetUsersToDeleteAsync(now.AddMonths(-InactivityMonths), ct);
        foreach (var user in usersToDelete)
        {
            try
            {
                await auth0ManagementService.DeleteUserAsync(user.Auth0Sub, ct);
                await userRepository.DeleteAsync(user.Id, ct);
                logger.LogInformation("Deleted inactive user {UserId} (inactive since {LastActivity})",
                    user.Id, user.LastActivityAt ?? user.CreatedAt);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to delete inactive user {UserId}", user.Id);
            }
        }
    }
}
