using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PantioAPI.Services;

namespace PantioTest.ServiceTests;

public class Auth0ManagementServiceTests
{
    private const string Domain = "test.auth0.com";
    private const string ClientId = "test-client-id";
    private const string ClientSecret = "test-client-secret";

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth0:ManagementDomain"] = Domain,
                ["Auth0:ManagementClientId"] = ClientId,
                ["Auth0:ManagementClientSecret"] = ClientSecret
            })
            .Build();

    private static HttpClient BuildHttpClient(Func<HttpRequestMessage, HttpResponseMessage> handler) =>
        new(new FakeHttpMessageHandler(handler));

    private class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(handler(request));
    }

    private static HttpResponseMessage TokenResponse(string accessToken) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { access_token = accessToken }),
                System.Text.Encoding.UTF8,
                "application/json")
        };

    [Test]
    public async Task DeleteUserAsync_ValidSub_SendsDeleteRequestToAuth0()
    {
        #region Arrange
        var requestLog = new List<HttpRequestMessage>();
        var callCount = 0;
        var httpClient = BuildHttpClient(request =>
        {
            requestLog.Add(request);
            callCount++;
            return callCount == 1
                ? TokenResponse("fake-token")
                : new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var service = new Auth0ManagementService(httpClient, BuildConfig());
        #endregion

        #region Act
        await service.DeleteUserAsync("auth0|abc123");
        #endregion

        #region Assert
        Assert.That(requestLog.Count, Is.EqualTo(2));
        var deleteRequest = requestLog[1];
        Assert.That(deleteRequest.Method, Is.EqualTo(HttpMethod.Delete));
        Assert.That(deleteRequest.RequestUri!.AbsoluteUri,
            Does.Contain("api/v2/users/auth0%7Cabc123"));
        Assert.That(deleteRequest.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(deleteRequest.Headers.Authorization.Parameter, Is.EqualTo("fake-token"));
        #endregion
    }

    [Test]
    public void DeleteUserAsync_TokenRequestFails_ThrowsHttpRequestException()
    {
        #region Arrange
        var httpClient = BuildHttpClient(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var service = new Auth0ManagementService(httpClient, BuildConfig());
        #endregion

        #region Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(() =>
            service.DeleteUserAsync("auth0|abc123"));
        #endregion
    }
}
