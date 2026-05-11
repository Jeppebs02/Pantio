namespace PantioClassLibrary.Interfaces.Services;

public interface IInactiveUserService
{
    Task RunCheckAsync(CancellationToken ct = default);
}
