using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using PantioClassLibrary.Interfaces.Services;
using PantioClassLibrary.DTO;

namespace PantioAPI.Services;

public class NettoAuthService(HttpClient httpClient, IConfiguration config) : INettoAuthService
{
    public async Task<NettoTokenSet> ExchangeCodeAsync(string authorizationCode, string codeVerifier, string? redirectUri, CancellationToken ct = default)
    {
        var request = new Dictionary<string, string?>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = authorizationCode,
            ["code_verifier"] = codeVerifier,
            ["client_id"] = GetClientId(),
            ["redirect_uri"] = redirectUri ?? GetRedirectUri()
        };

        return await PostTokenRequestAsync(request, ct);
    }

    public async Task<NettoTokenSet> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var request = new Dictionary<string, string?>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = GetClientId()
        };

        return await PostTokenRequestAsync(request, ct);
    }

    public async Task<IReadOnlyCollection<NettoReceiptSummary>> GetReceiptSummariesAsync(string accessToken, string idToken, CancellationToken ct = default)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, GetReceiptListEndpoint(), accessToken, idToken);
        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<NettoReceiptListResponse>(cancellationToken: ct);
        if (payload?.ReceiptsList is null)
            return [];

        return payload.ReceiptsList
            .SelectMany(group => group.Receipts ?? [])
            .Where(receipt => !string.IsNullOrWhiteSpace(receipt.Id))
            .Select(receipt => new NettoReceiptSummary(
                receipt.Id!,
                receipt.StoreName,
                receipt.Type,
                receipt.SalesTotal ?? 0,
                receipt.MemberDiscount ?? 0,
                receipt.OtherDiscount ?? 0,
                ParseCreatedAt(receipt.CreatedAt, receipt.Id)
            ))
            .ToArray();
    }

    public async Task<NettoReceiptDetail> GetReceiptDetailAsync(string accessToken, string idToken, string receiptType, string receiptId, CancellationToken ct = default)
    {
        var endpoint = $"{GetReceiptDetailEndpoint()}?type={Uri.EscapeDataString(receiptType)}&receiptId={Uri.EscapeDataString(receiptId)}";
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, endpoint, accessToken, idToken);
        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(ct);
        var lines = ParseReceiptDetailLines(payload);

        return new NettoReceiptDetail(lines);
    }

    private async Task<NettoTokenSet> PostTokenRequestAsync(Dictionary<string, string?> request, CancellationToken ct)
    {
        using var response = await httpClient.PostAsync(
            GetTokenEndpoint(),
            new FormUrlEncodedContent(request!),
            ct);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<NettoTokenResponse>(cancellationToken: ct);
        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.AccessToken) ||
            string.IsNullOrWhiteSpace(payload.RefreshToken) ||
            string.IsNullOrWhiteSpace(payload.IdToken))
        {
            throw new InvalidOperationException("Netto token response was incomplete.");
        }

        return new NettoTokenSet(
            payload.AccessToken,
            payload.RefreshToken,
            payload.IdToken,
            payload.ExpiresIn
        );
    }

    private string GetTokenEndpoint() => config["Netto:TokenEndpoint"] ?? "https://idp.dsgapps.dk/token";

    private string GetClientId() => config["Netto:ClientId"] ?? "customer-program";

    private string GetRedirectUri() => config["Netto:RedirectUri"] ?? "http://localhost";

    private string GetReceiptListEndpoint() => config["Netto:ReceiptListEndpoint"] ?? "https://p-club.dsgapps.dk/api/cp/receipt";

    private string GetReceiptDetailEndpoint() => config["Netto:ReceiptDetailEndpoint"] ?? "https://p-club.dsgapps.dk/api/cp/receipt/details";

    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string uri, string accessToken, string idToken)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("x-id_token", $"Bearer {idToken}");
        return request;
    }

    private static DateTime ParseCreatedAt(string? createdAt, string? receiptId = null)
    {
        if (!string.IsNullOrWhiteSpace(createdAt) &&
            DateTime.TryParse(createdAt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
            return parsed;

        // Fall back to Unix epoch embedded in receipt ID: {storeId}-{pos}-{txn}-{epoch}
        if (!string.IsNullOrWhiteSpace(receiptId))
        {
            var segments = receiptId.Split('-');
            if (segments.Length > 0 && long.TryParse(segments[^1], out var epochSeconds))
                return DateTimeOffset.FromUnixTimeSeconds(epochSeconds).UtcDateTime;
        }

        return DateTime.UtcNow;
    }

    private static IReadOnlyCollection<NettoReceiptLine> ParseReceiptDetailLines(string payload)
    {
        using var document = JsonDocument.Parse(payload);

        var lineItemsElement = ResolveLineItemsElement(document.RootElement, payload);

        if (lineItemsElement.ValueKind != JsonValueKind.Array)
            return [];

        var rawLines = JsonSerializer.Deserialize<NettoReceiptLineResponse[]>(lineItemsElement.GetRawText()) ?? [];
        return rawLines
            .Select(line => new NettoReceiptLine(
                line.Ean?.ToString(CultureInfo.InvariantCulture),
                line.ArticleDescription,
                line.SalesPrice ?? 0,
                line.NormalPrice ?? 0,
                line.Discount ?? 0,
                line.Discounts.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                    ? null
                    : line.Discounts.GetRawText(),
                line.QtyInSalesUnit ?? 0,
                line.TaxAmount ?? 0,
                line.ItemType
            ))
            .ToArray();
    }

    private static JsonElement ResolveLineItemsElement(JsonElement root, string payload)
    {
        if (TryGetLineItemsProperty(root, out var objectLineItems))
            return objectLineItems;

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in root.EnumerateArray())
            {
                if (TryGetLineItemsProperty(element, out var nestedLineItems))
                    return nestedLineItems;
            }

            return root;
        }

        var preview = payload.Length > 400 ? payload[..400] : payload;
        throw new InvalidOperationException($"Unexpected Netto receipt detail payload: {preview}");
    }

    private static bool TryGetLineItemsProperty(JsonElement element, out JsonElement lineItems)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("lineItems", out lineItems))
        {
            return true;
        }

        lineItems = default;
        return false;
    }

    private sealed record NettoTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("id_token")] string IdToken,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn
    );

    private sealed record NettoReceiptListResponse(
        [property: JsonPropertyName("receiptsList")] NettoReceiptGroup[]? ReceiptsList
    );

    private sealed record NettoReceiptGroup(
        [property: JsonPropertyName("receipts")] NettoReceiptSummaryResponse[]? Receipts
    );

    private sealed record NettoReceiptSummaryResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("storeName")] string? StoreName,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("salesTotal")] float? SalesTotal,
        [property: JsonPropertyName("memberDiscount")] float? MemberDiscount,
        [property: JsonPropertyName("otherDiscount")] float? OtherDiscount,
        [property: JsonPropertyName("createdAt")] string? CreatedAt
    );

    private sealed record NettoReceiptLineResponse(
        [property: JsonPropertyName("ean")] long? Ean,
        [property: JsonPropertyName("articleDescription")] string? ArticleDescription,
        [property: JsonPropertyName("salesPrice")] float? SalesPrice,
        [property: JsonPropertyName("normalPrice")] float? NormalPrice,
        [property: JsonPropertyName("discount")] float? Discount,
        [property: JsonPropertyName("discounts")] JsonElement Discounts,
        [property: JsonPropertyName("qtyInSalesUnit")] float? QtyInSalesUnit,
        [property: JsonPropertyName("taxAmount")] float? TaxAmount,
        [property: JsonPropertyName("itemType")] string? ItemType
    );
}
