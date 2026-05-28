# Database Implementation

This document describes how Pantio's data layer is built — from entity definitions through EF Core configuration, the repository pattern, optimistic concurrency, migrations, and the PostgreSQL setup.

---

## Table of Contents

1. [Overview](#overview)
2. [Entities](#entities)
3. [EF Core Setup](#ef-core-setup)
   - [DbContext](#dbcontext)
   - [OnModelCreating](#onmodelcreating)
   - [Enum Conversions](#enum-conversions)
   - [Indexes](#indexes)
   - [Cascade Rules](#cascade-rules)
   - [Seed Data](#seed-data)
4. [Repository Pattern](#repository-pattern)
   - [Interface Layer](#interface-layer)
   - [Repository Implementations](#repository-implementations)
   - [Key Patterns](#key-patterns)
5. [Optimistic Concurrency](#optimistic-concurrency)
6. [Unit of Work and Transactions](#unit-of-work-and-transactions)
7. [Migrations](#migrations)
8. [PostgreSQL and Connection Setup](#postgresql-and-connection-setup)
9. [Dependency Injection](#dependency-injection)

---

## Overview

Pantio uses **Entity Framework Core 10** with **PostgreSQL via Npgsql** as its persistence layer. The architecture is:

- **Entities** live in `PantioClassLibrary` — plain C# classes with no EF references
- **EF configuration, repositories, and migrations** live in `PantioRepository`
- **Services** in `PantioAPI` talk only to repository interfaces, never to the `DbContext` directly
- **`IUnitOfWork`** is available when a service needs multiple repository calls to be atomic

All repositories and the `DbContext` are registered as **scoped** per HTTP request, so every request gets a single `DbContext` instance shared across all repositories within that request.

---

## Entities

All entity classes are in `PantioClassLibrary/Entities/`. They carry no EF attributes beyond `[Table]` and `[ConcurrencyCheck]` — all relationship and column configuration is done in `OnModelCreating`.

| Entity | Table | Key Notes |
|--------|-------|-----------|
| `User` | `users` | Auth0Sub (unique), FcmToken, LastActivityAt, DeletionWarningSentAt |
| `UserProfile` | `user_profiles` | One-to-one with User; NotificationPrefs stored as JSONB |
| `Inventory` | `inventories` | Belongs to User; has RowVersion for optimistic concurrency |
| `InventoryItem` | `inventory_items` | Central entity; Quantity as decimal; enums stored as strings; RowVersion |
| `ExpiryDate` | `expiry_dates` | One-to-one with InventoryItem; manual override or estimated expiry |
| `NutritionFacts` | `nutrition_facts` | One-to-one with either InventoryItem or ProductCache |
| `ProductCache` | `product_cache` | Per-user EAN cache; unique on (UserId, Ean) |
| `ProductCategory` | `product_categories` | Open Food Facts tag → Danish name + default shelf life days |
| `StoreConnection` | `store_connections` | Encrypted OAuth tokens; unique on (UserId, Chain) |
| `Receipt` | `receipts` | Imported from store; DsgReceiptId unique |
| `ReceiptLine` | `receipt_lines` | Discounts stored as JSONB; ProcessedToInventory flag |
| `Recipe` | `recipes` | SuggestionBatchId groups AI-generated sets of 3; IsSaved flag |
| `RecipeEntry` | `recipe_entries` | InventoryItemId nullable (SET NULL on item delete) |
| `ShoppingList` | `shopping_lists` | Belongs to User |
| `ShoppingListItem` | `shopping_list_items` | Quantity nullable; IsChecked flag |
| `ExpiryNotification` | `expiry_notifications` | Channel stored as string enum |
| `SyncLog` | `sync_logs` | Per-sync audit record for auto-sync background service |

---

## EF Core Setup

### DbContext

**`PantioRepository/EntityFramework/PantioDbContext.cs`**

```csharp
public class PantioDbContext(DbContextOptions<PantioDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<ExpiryDate> ExpiryDates => Set<ExpiryDate>();
    public DbSet<NutritionFacts> NutritionFacts => Set<NutritionFacts>();
    public DbSet<ProductCache> ProductCache => Set<ProductCache>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<StoreConnection> StoreConnections => Set<StoreConnection>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeEntry> RecipeEntries => Set<RecipeEntry>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<ExpiryNotification> ExpiryNotifications => Set<ExpiryNotification>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) { ... }
}
```

### OnModelCreating

All relationship, index, conversion, and cascade configuration is centralised in `OnModelCreating`. Nothing is scattered across entity classes or data annotations (beyond `[Table]` and `[ConcurrencyCheck]`).

### Enum Conversions

Five enum properties are stored as their string name in PostgreSQL rather than as integers. This makes the database human-readable and avoids silent breakage if enum ordering changes.

```csharp
modelBuilder.Entity<StoreConnection>()
    .Property(x => x.Chain).HasConversion<string>();

modelBuilder.Entity<InventoryItem>()
    .Property(x => x.Status).HasConversion<string>();
modelBuilder.Entity<InventoryItem>()
    .Property(x => x.AddedVia).HasConversion<string>();
modelBuilder.Entity<InventoryItem>()
    .Property(x => x.QuantityUnit).HasConversion<string>();

modelBuilder.Entity<ExpiryNotification>()
    .Property(x => x.Channel).HasConversion<string>();
```

### Indexes

**Unique indexes** — enforce business uniqueness constraints at the database level:

| Table | Column(s) | Reason |
|-------|-----------|--------|
| `users` | `auth0_sub` | One DB user per Auth0 identity |
| `store_connections` | `(user_id, chain)` | One connection per chain per user |
| `receipts` | `dsg_receipt_id` | Deduplication on import |
| `product_cache` | `(user_id, ean)` | One EAN entry per user |
| `nutrition_facts` | `product_cache_id` | One-to-one with ProductCache |
| `nutrition_facts` | `inventory_item_id` | One-to-one with InventoryItem |

**Regular indexes** — support common query patterns:

| Table | Column(s) | Query it supports |
|-------|-----------|-------------------|
| `store_connections` | `last_polled_at` | Auto-sync scheduler ordering |
| `sync_logs` | `(store_connection_id, synced_at)` | Sync history list |
| `inventory_items` | `(inventory_id, status)` | Filtered inventory loads |
| `inventory_items` | `ean` | EAN lookups on import |
| `inventory_items` | `category_id` | Category filtering |
| `expiry_dates` | `estimated_expiry` | Expiry notification queries |
| `expiry_dates` | `inventory_item_id` | One-to-one navigation lookup |
| `shopping_list_items` | `(shopping_list_id, is_checked)` | Checked/unchecked item split |
| `recipe_entries` | `recipe_id` | Loading entries for a recipe |

**Partial index** — one index applies a filter condition to reduce its size:

```sql
CREATE INDEX ... ON recipe_entries (inventory_item_id)
WHERE inventory_item_id IS NULL
```

This covers only unlinked recipe entries, which is the set queried during ingredient matching.

### Cascade Rules

Most relationships cascade by convention (parent delete → child delete). The exceptions are explicitly configured:

```csharp
// RecipeEntry → InventoryItem: SET NULL
// Deleting an inventory item does not delete the recipe entry —
// it just nullifies the link, preserving the recipe as a template.
modelBuilder.Entity<RecipeEntry>()
    .HasOne(r => r.InventoryItem)
    .WithMany(i => i.RecipeEntries)
    .HasForeignKey(r => r.InventoryItemId)
    .OnDelete(DeleteBehavior.SetNull);

// NutritionFacts → InventoryItem: CASCADE
// Nutritional data has no meaning without its item.
modelBuilder.Entity<NutritionFacts>()
    .HasOne(n => n.InventoryItem)
    .WithOne(i => i.NutritionFacts)
    .HasForeignKey<NutritionFacts>(n => n.InventoryItemId)
    .OnDelete(DeleteBehavior.Cascade);

// NutritionFacts → ProductCache: CASCADE
modelBuilder.Entity<NutritionFacts>()
    .HasOne(n => n.ProductCache)
    .WithOne(p => p.NutritionFacts)
    .HasForeignKey<NutritionFacts>(n => n.ProductCacheId)
    .OnDelete(DeleteBehavior.Cascade);
```

All other parent-child relationships (User → Inventory, Inventory → InventoryItem, Recipe → RecipeEntry, etc.) cascade by EF convention.

### Seed Data

`OnModelCreating` seeds 24 `ProductCategory` rows. Each category maps an **Open Food Facts category tag** to a Danish display name and a default shelf-life in days:

```csharp
modelBuilder.Entity<ProductCategory>().HasData(
    new ProductCategory { Id = 1, OffTag = "en:fresh-meats",   DisplayName = "Fersk kød",    DefaultShelfLifeDays = 5  },
    new ProductCategory { Id = 2, OffTag = "en:dairy",         DisplayName = "Mejeriprodukter", DefaultShelfLifeDays = 14 },
    // ... 22 more rows
    new ProductCategory { Id = 24, OffTag = "en:oils-and-fats", DisplayName = "Olier og fedtstoffer", DefaultShelfLifeDays = 730 }
);
```

These are re-applied by each migration that touches categories, ensuring consistent data across all environments.

---

## Repository Pattern

### Interface Layer

All repository contracts live in `PantioClassLibrary/Interfaces/Repository/`. Services depend only on these interfaces — they never reference `PantioDbContext` or any EF type.

```
IUserRepository
IInventoryRepository
IInventoryItemRepository
IExpiryDateRepository
IExpiryNotificationRepository
IProductCategoryRepository
IProductCacheDbRepository
IStoreConnectionRepository
IRecipeRepository
IShoppingListRepository
```

### Repository Implementations

All implementations live in `PantioRepository/EntityFramework/Repositories/` and receive `PantioDbContext` via constructor injection.

#### InventoryItemRepository

The most complex CRUD repository. Notable behaviours:

**Optimistic concurrency on update** — the DTO carries the client's known `RowVersion`. The repository sets it as the `OriginalValue` before saving so EF includes it in the `WHERE` clause:

```csharp
db.Entry(item).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;
item.RowVersion = dto.RowVersion + 1;
await db.SaveChangesAsync(ct);
```

If another write has already incremented `row_version` in the database, `SaveChangesAsync` throws `DbUpdateConcurrencyException` and the caller must retry with a fresh read.

**Explicit navigation reload after update** — after saving, related entities are reloaded so the returned object is fully populated:

```csharp
await db.Entry(item).Reference(i => i.ExpiryDate).LoadAsync(ct);
await db.Entry(item).Reference(i => i.NutritionFacts).LoadAsync(ct);
```

#### StoreConnectionRepository

The most involved repository. Key behaviours:

**Token encryption/decryption** — OAuth tokens (`GigyaSessionToken`, `AccessToken`, `RefreshToken`, `IdToken`) are encrypted before write and decrypted after read using `StoreConnectionTokenProtector`. Raw token values never reach the database.

**AsNoTracking for reads** — all read methods use `AsNoTracking()` to avoid the change-tracking overhead for data that is not being mutated.

**Bulk receipt import in a single `SaveChangesAsync`** — `ImportReceiptsAsync` builds all `Receipt` and `ReceiptLine` entities in memory, calls `AddRange`, and then saves once. This keeps the import atomic and avoids per-row round-trips:

```csharp
db.Receipts.AddRange(receiptsToImport);
await db.SaveChangesAsync(ct);
```

**Bulk mark-processed via ExecuteUpdateAsync** — `MarkReceiptLinesProcessedAsync` uses a set-based update instead of loading entities:

```csharp
await db.ReceiptLines
    .Where(l => ids.Contains(l.Id))
    .ExecuteUpdateAsync(s => s.SetProperty(l => l.ProcessedToInventory, true), ct);
```

#### UserRepository

Several operations use `ExecuteUpdateAsync` for single-column updates (last activity, deletion warning, FCM token) — no entity is loaded into the change tracker, so there is no `SaveChangesAsync`:

```csharp
await db.Users
    .Where(u => u.Id == userId)
    .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastActivityAt, timestamp), ct);
```

#### ProductCategoryRepository

`CreateIfNotExistsAsync` deliberately does **not** call `SaveChangesAsync`. The caller is expected to batch the save with other operations. This is the one place where a repository method intentionally leaves unsaved changes in the tracker.

#### ExpiryDateRepository

Notification queries eagerly load a full chain to get the FCM token and inventory context in a single query:

```csharp
db.ExpiryDates
    .Include(e => e.InventoryItem)
        .ThenInclude(i => i.Inventory)
            .ThenInclude(inv => inv.User)
    .Where(...)
```

### Key Patterns

| Pattern | Where used | Why |
|---------|-----------|-----|
| `SaveChangesAsync` per method | Most repositories | Each operation commits independently; use `IUnitOfWork` when atomicity across methods is required |
| `AsNoTracking` | StoreConnectionRepository reads | Reduces memory for read-only queries |
| `ExecuteUpdateAsync` | UserRepository, StoreConnectionRepository | Set-based single-column updates without loading entities |
| Explicit `Reference.LoadAsync` | InventoryItemRepository after update | Ensures navigations are populated after a partial update |
| Token encrypt/decrypt | StoreConnectionRepository | OAuth tokens never stored in plaintext |
| Upsert in `SaveAsync` | ProductCacheDbRepository | EAN-based cache with nested NutritionFacts create-or-update |

---

## Optimistic Concurrency

Two entities use **optimistic concurrency** to prevent lost updates when multiple requests modify the same row concurrently: `Inventory` and `InventoryItem`.

Both carry an integer `RowVersion` column decorated with `[ConcurrencyCheck]`:

```csharp
[ConcurrencyCheck]
public int RowVersion { get; set; }
```

The `[ConcurrencyCheck]` attribute tells EF to include this column in the `WHERE` clause of `UPDATE` statements. The repository layer manually manages the original value:

```csharp
// 1. Client reads item — gets RowVersion = 5
// 2. Client sends update DTO with RowVersion = 5
// 3. Repository sets OriginalValue so EF generates:
//    UPDATE inventory_items SET quantity = @qty, row_version = 6, ...
//    WHERE id = @id AND row_version = 5
db.Entry(item).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;
item.RowVersion = dto.RowVersion + 1;
await db.SaveChangesAsync(ct);
// If another request already bumped row_version to 6, the WHERE matches 0 rows
// → EF throws DbUpdateConcurrencyException
```

If the update affects 0 rows because the version has moved on, EF throws `DbUpdateConcurrencyException`. The API layer surfaces this as a `409 Conflict`, and the client is expected to re-fetch and retry.

---

## Unit of Work and Transactions

### Why it is needed

Each repository method calls `SaveChangesAsync` independently. Within a single call, EF wraps all tracked changes in an implicit database transaction — but there is no automatic transaction spanning *multiple* `SaveChangesAsync` calls. If an operation requires several repository calls to be all-or-nothing, an explicit transaction is required.

### IUnitOfWork

**`PantioClassLibrary/Interfaces/IUnitOfWork.cs`**

```csharp
public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
```

### EFUnitOfWork

**`PantioRepository/EFUnitOfWork.cs`**

```csharp
public class EFUnitOfWork(PantioDbContext db) : IUnitOfWork
{
    private IDbContextTransaction? _tx;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _tx = await db.Database.BeginTransactionAsync(ct);

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_tx is null) throw new InvalidOperationException("No active transaction.");
        await _tx.CommitAsync(ct);
        await _tx.DisposeAsync();
        _tx = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_tx is null) return;
        await _tx.RollbackAsync(ct);
        await _tx.DisposeAsync();
        _tx = null;
    }
}
```

Because all repositories share the same scoped `DbContext` instance, opening a transaction on `db.Database` covers every `SaveChangesAsync` call made through any repository in the same request.

### Usage example — RecipeService.CompleteAsync

Recipe completion touches three tables: `recipe_entries` (clear links), `inventory_items` (deduct quantities), and `recipes` (set CompletedAt). All three must succeed or none should commit:

```csharp
await unitOfWork.BeginTransactionAsync(ct);
try
{
    await recipeRepository.ClearInventoryLinksAsync(recipeId, ct);   // SaveChangesAsync
    foreach (var (itemId, qty, unit) in linkedEntries)
    {
        // ... compute deduction ...
        await inventoryItemRepository.DeleteAsync(item.Id, ct);      // SaveChangesAsync
        // or
        await inventoryItemRepository.UpdateAsync(item.Id, dto, ct); // SaveChangesAsync
    }
    await recipeRepository.SetCompletedAsync(recipeId, ct);          // SaveChangesAsync
    await unitOfWork.CommitAsync(ct);
}
catch
{
    await unitOfWork.RollbackAsync(ct);
    throw;
}
// Cache invalidation happens after commit — it is not a DB concern
```

---

## Migrations

Migrations live in `PantioRepository/EntityFramework/EFMigrations/` and are applied automatically at application startup via `dbContext.Database.MigrateAsync()`.

The `PantioDbContextFactory` provides a hardcoded local connection for `dotnet ef` CLI commands at design time:

```csharp
public class PantioDbContextFactory : IDesignTimeDbContextFactory<PantioDbContext>
{
    public PantioDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PantioDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=pantio_dev;Username=pantio;Password=pantio_dev_pass")
            .Options;
        return new PantioDbContext(options);
    }
}
```

### Migration History

| Migration | Date | What it does |
|-----------|------|-------------|
| `InitialCreate` | 2026-04-30 | Creates all base tables: users, inventories, inventory_items, product_categories, product_cache, nutrition_facts, store_connections, receipts, receipt_lines, recipes, recipe_entries, shopping_lists, shopping_list_items, expiry_dates, expiry_notifications, user_profiles |
| `InventoryItemProductData` | 2026-05-04 | Adds `category_id` FK to inventory_items; makes `nutrition_facts.product_cache_id` nullable; adds `nutrition_facts.inventory_item_id` (one-to-one unique) |
| `AddInventoryItemRowVersion` | 2026-05-04 | Adds `row_version` column (default 0) to inventory_items |
| `AddInventoryRowVersion` | 2026-05-04 | Adds `row_version` column (default 0) to inventories |
| `SeedProductCategories` | 2026-05-05 | Seeds initial 24 ProductCategory rows with OFF tags and shelf-life defaults |
| `AddSuggestionBatchIdToRecipe` | 2026-05-05 | Adds `suggestion_batch_id` (nullable Guid) to recipes — groups the 3 recipes from one Gemini call |
| `AddAuth0SubAndMakePhoneNullable` | 2026-05-05 | Adds `auth0_sub` (MaxLength 128, unique index) to users; makes `phone_number` nullable |
| `AddInventoryItemOffTag` | 2026-05-05 | Adds `off_tag` column to inventory_items for Open Food Facts category matching |
| `AddStoreConnectionAutoSync` | 2026-05-10 | Adds `auto_sync_enabled` boolean to store_connections |
| `ReceiptLineInventoryItemsOneToMany` | 2026-05-11 | Adds `receipt_line_id` FK to inventory_items — one receipt line can produce multiple inventory items |
| `AddInactiveUserTracking` | 2026-05-11 | Adds `last_activity_at` and `deletion_warning_sent_at` (both nullable) to users for GDPR cleanup |
| `FixRecipeEntryInventoryItemCascade` | 2026-05-11 | Changes RecipeEntry → InventoryItem FK from CASCADE to SET NULL — deleting an item preserves the recipe |
| `AddRecipeIsSaved` | 2026-05-11 | Adds `is_saved` boolean to recipes |
| `FixNutritionFactsCascadeDelete` | 2026-05-13 | Ensures NutritionFacts → InventoryItem uses CASCADE (was missing) |
| `AddUserFcmToken` | 2026-05-13 | Adds `fcm_token` to users for push notifications |
| `AddExpiredNotificationSentAt` | 2026-05-13 | Adds `expired_notification_sent_at` to expiry_dates — separate from the expiry-soon notification |
| `NormalizeQuantityUnit` | 2026-05-14 | Converts quantity_unit columns in inventory_items and product_cache to use the normalized `QuantityUnit` enum stored as a string |
| `QuantityToDecimal` | 2026-05-14 | Changes `quantity` in inventory_items from `float` to `decimal` — avoids floating-point rounding errors in inventory maths |
| `AddSyncLogsAndImportHorizon` | 2026-05-18 | Creates `sync_logs` table; adds `import_horizon` to store_connections; adds composite index on (store_connection_id, synced_at) |
| `LowercaseQuantityUnit` | 2026-05-21 | Data migration — normalises existing `quantity_unit` values to lowercase to match the new enum string representation |
| `SeedAllProductCategories` | 2026-05-22 | Re-seeds all 24 product categories to apply any corrections to display names and shelf-life values |

### Adding a migration

```bash
cd backend/PantioRepository
dotnet ef migrations add <MigrationName> --startup-project ../PantioAPI
```

The `PantioDbContextFactory` is used here — it provides the local dev connection string so no running application is needed.

---

## PostgreSQL and Connection Setup

### Connection string

The production connection string is injected via environment variable or Azure Key Vault and read through the standard .NET configuration system:

```json
// appsettings.json — placeholder only
"ConnectionStrings": {
    "DefaultConnection": ""
}
```

At runtime, `DefaultConnection` is set from the host environment. The `PantioDbContextFactory` uses a hardcoded local string only for EF CLI tooling.

### Npgsql configuration

```csharp
builder.Services.AddDbContext<PantioDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure()
    )
);
```

`EnableRetryOnFailure()` adds automatic retry for transient PostgreSQL errors (connection drops, brief unavailability). The default Npgsql policy retries up to 6 times with exponential backoff.

### Auto-migration on startup

```csharp
await using var scope = app.Services.CreateAsyncScope();
var dbContext = scope.ServiceProvider.GetRequiredService<PantioDbContext>();
await dbContext.Database.MigrateAsync();
```

This applies any pending migrations on every application start. It is safe to run concurrently because EF Core uses a PostgreSQL advisory lock (`__EFMigrationsLock`) to ensure only one instance migrates at a time.

### PostgreSQL-specific features used

**JSONB columns** — two columns use PostgreSQL's native JSONB type, allowing structured storage without a separate table:

- `receipt_lines.discounts` — array of discount objects from the store receipt
- `user_profiles.notification_prefs` — user's push notification preferences

**Partial index** — the unlinked recipe entries index uses a `WHERE` filter:

```sql
CREATE INDEX ix_recipe_entries_inventory_item_id_null
ON recipe_entries (inventory_item_id)
WHERE inventory_item_id IS NULL;
```

PostgreSQL evaluates the filter at index maintenance time, so the index only covers the rows that actually need it.

**Enum-as-string** — storing enums as their string names (e.g. `"Refrigerated"` rather than `2`) is a deliberate trade-off: the column is slightly larger than an integer, but the data is self-documenting and immune to enum reordering bugs. Npgsql's `HasConversion<string>()` handles the mapping transparently.

**Optimistic concurrency via integer RowVersion** — PostgreSQL does not have a native row-version type equivalent to SQL Server's `rowversion`. Pantio uses a plain `integer` column with `[ConcurrencyCheck]`, incremented manually by the repository on every write. EF translates this to a `WHERE row_version = @expected` predicate in the `UPDATE` statement.

---

## Dependency Injection

All database-related services are registered as **scoped** in `Program.cs`, meaning one instance per HTTP request:

```csharp
// DbContext — scoped by AddDbContext
builder.Services.AddDbContext<PantioDbContext>(options => ...);

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, EFUnitOfWork>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
builder.Services.AddScoped<IExpiryDateRepository, ExpiryDateRepository>();
builder.Services.AddScoped<IExpiryNotificationRepository, ExpiryNotificationRepository>();
builder.Services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
builder.Services.AddScoped<IProductCacheDbRepository, ProductCacheDbRepository>();
builder.Services.AddScoped<IStoreConnectionRepository, StoreConnectionRepository>();
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
```

Because all repositories and `EFUnitOfWork` share the same scoped `DbContext` instance, opening a transaction through `IUnitOfWork` automatically covers all subsequent repository calls within that request — no special wiring is needed.

Token encryption is the one singleton in the data layer:

```csharp
builder.Services.AddSingleton<StoreConnectionTokenProtector>();
```

It is stateless (just a key + algorithm) so singleton lifetime is safe.