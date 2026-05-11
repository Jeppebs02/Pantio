using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Services;

public class InventoryItemCacheService(IDistributedCache cache) : IInventoryItemCacheService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

    public async Task<IEnumerable<InventoryItemDto>?> GetAsync(Guid inventoryId, CancellationToken ct = default)
    {
        var json = await cache.GetStringAsync(Key(inventoryId), ct);
        return json is null ? null : JsonSerializer.Deserialize<List<InventoryItemDto>>(json);
    }

    public async Task SetAsync(Guid inventoryId, IEnumerable<InventoryItemDto> items, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(items);
        await cache.SetStringAsync(Key(inventoryId), json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Ttl
        }, ct);
    }

    public Task InvalidateAsync(Guid inventoryId, CancellationToken ct = default) =>
        cache.RemoveAsync(Key(inventoryId), ct);

    private static string Key(Guid inventoryId) => $"inventory:{inventoryId}:items";
}
