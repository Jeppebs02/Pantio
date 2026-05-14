using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Services;

public class FcmService(HttpClient http, IOptions<FcmOptions> options, ILogger<FcmService> logger) : IFcmService
{
    private static readonly string[] FcmScopes = ["https://www.googleapis.com/auth/firebase.messaging"];

    public async Task SendAsync(string fcmToken, string title, string body, CancellationToken ct = default)
    {
        var projectId = options.Value.ProjectId;
        var url = $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send";

        var accessToken = await GetAccessTokenAsync(ct);

        var payload = new
        {
            message = new
            {
                token = fcmToken,
                notification = new { title, body },
                android = new { priority = "high" }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("FCM send failed ({Status}): {Error}", response.StatusCode, error);
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var path = options.Value.ServiceAccountPath;
        await using var stream = File.OpenRead(path);
        var credential = GoogleCredential
            .FromStream(stream)
            .CreateScoped(FcmScopes);
        return await ((ITokenAccess)credential).GetAccessTokenForRequestAsync(cancellationToken: ct);
    }
}
