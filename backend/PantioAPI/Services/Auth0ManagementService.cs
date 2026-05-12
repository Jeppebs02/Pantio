using System.Net.Http.Headers;
using System.Text.Json;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Services;

public class Auth0ManagementService(HttpClient httpClient, IConfiguration config) : IAuth0ManagementService
{
    public async Task DeleteUserAsync(string auth0Sub, CancellationToken ct = default)
    {
        var authority = config["Auth0:Authority"]?.TrimEnd('/') ?? throw new InvalidOperationException("Auth0:Authority is not configured");
        var managementDomain = config["Auth0:ManagementDomain"]?.TrimEnd('/') ?? throw new InvalidOperationException("Auth0:ManagementDomain is not configured");
        var clientId = config["Auth0:ManagementClientId"] ?? throw new InvalidOperationException("Auth0:ManagementClientId is not configured");
        var clientSecret = config["Auth0:ManagementClientSecret"] ?? throw new InvalidOperationException("Auth0:ManagementClientSecret is not configured");

        var tokenResponse = await httpClient.PostAsJsonAsync(
            $"{authority}/oauth/token",
            new
            {
                grant_type = "client_credentials",
                client_id = clientId,
                client_secret = clientSecret,
                audience = $"{managementDomain}/"
            },
            ct);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var accessToken = tokenData.GetProperty("access_token").GetString()!;

        var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{managementDomain}/users/{Uri.EscapeDataString(auth0Sub)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var deleteResponse = await httpClient.SendAsync(request, ct);
        deleteResponse.EnsureSuccessStatusCode();
    }
}
