namespace PantioClassLibrary.Interfaces.Services;

public interface IFcmService
{
    Task SendAsync(string fcmToken, string title, string body, CancellationToken ct = default);
}
