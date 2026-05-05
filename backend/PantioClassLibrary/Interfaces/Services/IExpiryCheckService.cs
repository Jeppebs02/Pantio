namespace PantioClassLibrary.Interfaces.Services;

public interface IExpiryCheckService
{
    Task RunCheckAsync(CancellationToken ct = default);
}
