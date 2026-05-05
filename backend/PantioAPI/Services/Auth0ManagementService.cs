using System.Net.Http.Headers;
using System.Text.Json;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Services;

public class Auth0ManagementService(HttpClient httpClient, IConfiguration config) : IAuth0ManagementService
{
    public async Task DeleteUserAsync(string auth0Sub, CancellationToken ct = default)
    {
        var domain = config["Auth0:ManagementDomain"]!;
        var clientId = config["Auth0:ManagementClientId"]!;
        var clientSecret = config["Auth0:ManagementClientSecret"]!;

        var tokenResponse = await httpClient.PostAsJsonAsync(
            $"https://{domain}/oauth/token",
            new
            {
                grant_type = "client_credentials",
                client_id = clientId,
                client_secret = clientSecret,
                audience = $"https://{domain}/api/v2/"
            },
            ct);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var accessToken = tokenData.GetProperty("access_token").GetString()!;

        var encodedSub = Uri.EscapeDataString(auth0Sub).Replace("|", "%7C");
        var deleteUri = new Uri(
            $"https://{domain}/api/v2/users/{encodedSub}",
            new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true });
        var request = new HttpRequestMessage(HttpMethod.Delete, deleteUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var deleteResponse = await httpClient.SendAsync(request, ct);
        deleteResponse.EnsureSuccessStatusCode();
    }
}
