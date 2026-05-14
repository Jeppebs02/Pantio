using System.Net.Http.Headers;
using System.Text.Json;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Services;

public class Auth0ManagementService(HttpClient httpClient, IConfiguration config) : IAuth0ManagementService
{
    public async Task DeleteUserAsync(string auth0Sub, CancellationToken ct = default)
    {
        var managementDomain = config["Auth0:ManagementDomain"] ?? throw new InvalidOperationException("Auth0:ManagementDomain is not configured");
        var clientId = config["Auth0:ManagementClientId"] ?? throw new InvalidOperationException("Auth0:ManagementClientId is not configured");
        var clientSecret = config["Auth0:ManagementClientSecret"] ?? throw new InvalidOperationException("Auth0:ManagementClientSecret is not configured");

        // Normalize to tenant base — managementDomain may be a full URL (https://tenant/api/v2/) or a bare domain
        var fullUri = new Uri(managementDomain.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? managementDomain
            : $"https://{managementDomain}");
        var tenantBase = $"{fullUri.Scheme}://{fullUri.Authority}";

        var tokenResponse = await httpClient.PostAsJsonAsync(
            $"{tenantBase}/oauth/token",
            new
            {
                grant_type = "client_credentials",
                client_id = clientId,
                client_secret = clientSecret,
                audience = $"{tenantBase}/api/v2/"
            },
            ct);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var accessToken = tokenData.GetProperty("access_token").GetString()!;

        var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{tenantBase}/api/v2/users/{Uri.EscapeDataString(auth0Sub)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var deleteResponse = await httpClient.SendAsync(request, ct);
        deleteResponse.EnsureSuccessStatusCode();
    }
}
