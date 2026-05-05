using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Services;

public class ProductCacheService(IDistributedCache cache) : IProductCacheService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

    public async Task<OffProductData?> GetAsync(string ean, CancellationToken ct = default)
    {
        var json = await cache.GetStringAsync(Key(ean), ct);
        return json is null ? null : JsonSerializer.Deserialize<OffProductData>(json);
    }

    public async Task SetAsync(string ean, OffProductData data, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(data);
        await cache.SetStringAsync(Key(ean), json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Ttl
        }, ct);
    }

    private static string Key(string ean) => $"off:product:{ean}";
}
