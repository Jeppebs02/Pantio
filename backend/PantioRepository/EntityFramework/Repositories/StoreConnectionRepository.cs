using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Repository;
using PantioRepository.Security;
using System.Text.Json;

namespace PantioRepository.EntityFramework.Repositories;

public class StoreConnectionRepository(PantioDbContext db, StoreConnectionTokenProtector tokenProtector) : IStoreConnectionRepository
{
    public async Task<IReadOnlyCollection<StoreConnection>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var connections = await db.StoreConnections
            .AsNoTracking()
            .Where(connection => connection.UserId == userId)
            .OrderBy(connection => connection.Chain)
            .ToListAsync(ct);

        foreach (var connection in connections)
        {
            DecryptTokenFields(connection);
        }

        return connections;
    }

    public async Task<StoreConnection?> GetByUserAndChainAsync(Guid userId, StoreChain chain, CancellationToken ct = default)
    {
        var connection = await db.StoreConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(connection => connection.UserId == userId && connection.Chain == chain, ct);

        if (connection is not null)
            DecryptTokenFields(connection);

        return connection;
    }

    public async Task<StoreConnection?> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var connection = await db.StoreConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(connection => connection.UserId == userId && connection.Id == id, ct);

        if (connection is not null)
            DecryptTokenFields(connection);

        return connection;
    }

    public async Task<StoreConnection?> UpdateAutoSyncAsync(Guid userId, Guid connectionId, bool enabled, CancellationToken ct = default)
    {
        var connection = await db.StoreConnections
            .FirstOrDefaultAsync(connection => connection.UserId == userId && connection.Id == connectionId, ct);

        if (connection is null)
            return null;

        connection.AutoSyncEnabled = enabled;
        await db.SaveChangesAsync(ct);
        db.Entry(connection).State = EntityState.Detached;
        DecryptTokenFields(connection);
        return connection;
    }

    public async Task<IReadOnlyCollection<StoreConnection>> GetDueForAutoSyncAsync(DateTime dueBefore, CancellationToken ct = default)
    {
        var connections = await db.StoreConnections
            .AsNoTracking()
            .Where(connection =>
                connection.AutoSyncEnabled &&
                connection.DisconnectedAt == null &&
                (connection.LastPolledAt == null || connection.LastPolledAt <= dueBefore))
            .OrderBy(connection => connection.LastPolledAt)
            .ThenBy(connection => connection.Id)
            .ToListAsync(ct);

        foreach (var connection in connections)
        {
            DecryptTokenFields(connection);
        }

        return connections;
    }

    public async Task<StoreConnection> CreateAsync(StoreConnection connection, CancellationToken ct = default)
    {
        EncryptTokenFields(connection);
        db.StoreConnections.Add(connection);
        await db.SaveChangesAsync(ct);
        DecryptTokenFields(connection);
        db.Entry(connection).State = EntityState.Detached;
        return connection;
    }

    public async Task<StoreConnection> UpdateAsync(StoreConnection connection, CancellationToken ct = default)
    {
        EncryptTokenFields(connection);
        db.StoreConnections.Update(connection);
        await db.SaveChangesAsync(ct);
        DecryptTokenFields(connection);
        db.Entry(connection).State = EntityState.Detached;
        return connection;
    }

    public async Task<IReadOnlyCollection<string>> GetExistingReceiptIdsAsync(IEnumerable<string> dsgReceiptIds, CancellationToken ct = default)
    {
        var ids = dsgReceiptIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
            return [];

        return await db.Receipts
            .Where(receipt => ids.Contains(receipt.DsgReceiptId))
            .Select(receipt => receipt.DsgReceiptId)
            .ToListAsync(ct);
    }

    public async Task<int> ImportReceiptsAsync(Guid userId, Guid connectionId, IReadOnlyCollection<ReceiptImportCandidateDto> receipts, CancellationToken ct = default)
    {
        if (receipts.Count == 0)
            return 0;

        var ids = receipts.Select(receipt => receipt.DsgReceiptId).Distinct().ToArray();
        var existingIds = await db.Receipts
            .Where(receipt => ids.Contains(receipt.DsgReceiptId))
            .Select(receipt => receipt.DsgReceiptId)
            .ToListAsync(ct);

        var existingIdSet = existingIds.ToHashSet(StringComparer.Ordinal);
        var importedCount = 0;

        foreach (var candidate in receipts)
        {
            if (!existingIdSet.Add(candidate.DsgReceiptId))
                continue;

            var receipt = new Receipt
            {
                Id = Guid.NewGuid(),
                StoreConnectionId = connectionId,
                UserId = userId,
                DsgReceiptId = candidate.DsgReceiptId,
                StoreName = candidate.StoreName,
                ReceiptType = candidate.ReceiptType,
                SalesTotalDkk = candidate.SalesTotalDkk,
                MemberDiscountDkk = candidate.MemberDiscountDkk,
                OtherDiscountDkk = candidate.OtherDiscountDkk,
                CreatedAt = candidate.CreatedAt,
                ImportedAt = DateTime.UtcNow,
                ReceiptLines = candidate.Lines.Select(line => new ReceiptLine
                {
                    Id = Guid.NewGuid(),
                    Ean = line.Ean,
                    ArticleDescription = line.ArticleDescription,
                    SalesPriceDkk = line.SalesPriceDkk,
                    NormalPriceDkk = line.NormalPriceDkk,
                    DiscountDkk = line.DiscountDkk,
                    Discounts = NormalizeDiscountsJson(line.DiscountsJson),
                    QtyInSalesUnit = line.QtyInSalesUnit,
                    TaxAmountDkk = line.TaxAmountDkk,
                    ItemType = line.ItemType,
                    ProcessedToInventory = false
                }).ToArray()
            };

            db.Receipts.Add(receipt);
            importedCount++;
        }

        if (importedCount == 0)
            return 0;

        await db.SaveChangesAsync(ct);
        return importedCount;
    }

    public async Task<int> ProcessImportedReceiptLinesToInventoryAsync(Guid userId, Guid connectionId, CancellationToken ct = default)
    {
        var targetInventory = await db.Inventories
            .Where(inventory => inventory.UserId == userId)
            .OrderBy(inventory => inventory.Name)
            .ThenBy(inventory => inventory.Id)
            .FirstOrDefaultAsync(ct);

        if (targetInventory is null)
            return 0;

        var receiptLines = await db.ReceiptLines
            .Include(line => line.Receipt)
            .Where(line =>
                line.Receipt.UserId == userId &&
                line.Receipt.StoreConnectionId == connectionId &&
                !line.ProcessedToInventory &&
                line.ItemType == "01")
            .OrderBy(line => line.Receipt.CreatedAt)
            .ThenBy(line => line.Id)
            .ToListAsync(ct);

        if (receiptLines.Count == 0)
            return 0;

        foreach (var line in receiptLines)
        {
            db.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                InventoryId = targetInventory.Id,
                Ean = line.Ean,
                ReceiptLineId = line.Id,
                ProductName = string.IsNullOrWhiteSpace(line.ArticleDescription) ? "Imported item" : line.ArticleDescription,
                Quantity = line.QtyInSalesUnit > 0 ? line.QtyInSalesUnit : 1f,
                QuantityUnit = null,
                Status = InventoryStatus.Available,
                AddedVia = AddedVia.Receipt,
                AddedAt = line.Receipt.CreatedAt,
                UpdatedAt = line.Receipt.CreatedAt
            });

            line.ProcessedToInventory = true;
        }

        await db.SaveChangesAsync(ct);
        return receiptLines.Count;
    }

    private static string? NormalizeDiscountsJson(string? discountsJson)
    {
        if (string.IsNullOrWhiteSpace(discountsJson))
            return null;

        using var document = JsonDocument.Parse(discountsJson);
        return document.RootElement.ValueKind == JsonValueKind.Array
            ? discountsJson
            : null;
    }

    private void EncryptTokenFields(StoreConnection connection)
    {
        connection.GigyaSessionToken = tokenProtector.Encrypt(connection.GigyaSessionToken);
        connection.AccessToken = tokenProtector.Encrypt(connection.AccessToken);
        connection.RefreshToken = tokenProtector.Encrypt(connection.RefreshToken);
        connection.IdToken = tokenProtector.Encrypt(connection.IdToken);
    }

    private void DecryptTokenFields(StoreConnection connection)
    {
        connection.GigyaSessionToken = tokenProtector.Decrypt(connection.GigyaSessionToken);
        connection.AccessToken = tokenProtector.Decrypt(connection.AccessToken);
        connection.RefreshToken = tokenProtector.Decrypt(connection.RefreshToken);
        connection.IdToken = tokenProtector.Decrypt(connection.IdToken);
    }
}
