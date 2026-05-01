using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;

namespace PantioRepository.EntityFramework;

public class PantioDbContext(DbContextOptions<PantioDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductCache> ProductCache => Set<ProductCache>();
    public DbSet<NutritionFacts> NutritionFacts => Set<NutritionFacts>();
    public DbSet<StoreConnection> StoreConnections => Set<StoreConnection>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<ExpiryDate> ExpiryDates => Set<ExpiryDate>();
    public DbSet<ExpiryNotification> ExpiryNotifications => Set<ExpiryNotification>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeEntry> RecipeEntries => Set<RecipeEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Enum → string conversions ──
        modelBuilder.Entity<StoreConnection>()
            .Property(x => x.Chain).HasConversion<string>();

        modelBuilder.Entity<InventoryItem>()
            .Property(x => x.Status).HasConversion<string>();

        modelBuilder.Entity<InventoryItem>()
            .Property(x => x.AddedVia).HasConversion<string>();

        modelBuilder.Entity<ExpiryNotification>()
            .Property(x => x.Channel).HasConversion<string>();

        // ── Unique indexes ──
        modelBuilder.Entity<StoreConnection>()
            .HasIndex(x => new { x.UserId, x.Chain }).IsUnique();

        modelBuilder.Entity<Receipt>()
            .HasIndex(x => x.DsgReceiptId).IsUnique();

        modelBuilder.Entity<ProductCache>()
            .HasIndex(x => new { x.UserId, x.Ean }).IsUnique();

        // ── Regular indexes ──
        modelBuilder.Entity<StoreConnection>()
            .HasIndex(x => x.LastPolledAt);

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(x => new { x.InventoryId, x.Status });

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(x => x.Ean);

        modelBuilder.Entity<ExpiryDate>()
            .HasIndex(x => x.EstimatedExpiry);

        modelBuilder.Entity<ExpiryDate>()
            .HasIndex(x => x.InventoryItemId);

        modelBuilder.Entity<ShoppingListItem>()
            .HasIndex(x => new { x.ShoppingListId, x.IsChecked });

        modelBuilder.Entity<RecipeEntry>()
            .HasIndex(x => x.RecipeId);

        // ── Partial index: only unlinked recipe entries ──
        modelBuilder.Entity<RecipeEntry>()
            .HasIndex(x => x.InventoryItemId)
            .HasFilter("inventory_item_id IS NULL");
    }
}
