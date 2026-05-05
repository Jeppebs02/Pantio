namespace PantioClassLibrary.Interfaces.Services;

public interface IAuth0ManagementService
{
    Task DeleteUserAsync(string auth0Sub, CancellationToken ct = default);
}
