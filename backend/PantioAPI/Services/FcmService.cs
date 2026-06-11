using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using PantioAPI.Options;
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
        var o = options.Value;
        var json = JsonSerializer.Serialize(new
        {
            type = o.Type,
            project_id = o.ProjectId,
            private_key_id = o.PrivateKeyId,
            private_key = o.PrivateKey,
            client_email = o.ClientEmail,
            client_id = o.ClientId,
            auth_uri = o.AuthUri,
            token_uri = o.TokenUri,
            auth_provider_x509_cert_url = o.AuthProviderCertUrl,
            client_x509_cert_url = o.ClientCertUrl,
            universe_domain = o.UniverseDomain
        });
        var credential = GoogleCredential.FromJson(json).CreateScoped(FcmScopes);
        return await ((ITokenAccess)credential).GetAccessTokenForRequestAsync(cancellationToken: ct);
    }
}
