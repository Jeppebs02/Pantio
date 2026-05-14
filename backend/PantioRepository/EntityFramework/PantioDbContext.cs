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
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Auth0Sub).IsUnique();

        modelBuilder.Entity<StoreConnection>()
            .HasIndex(x => new { x.UserId, x.Chain }).IsUnique();

        modelBuilder.Entity<Receipt>()
            .HasIndex(x => x.DsgReceiptId).IsUnique();

        modelBuilder.Entity<ProductCache>()
            .HasIndex(x => new { x.UserId, x.Ean }).IsUnique();

        modelBuilder.Entity<NutritionFacts>()
            .HasIndex(x => x.ProductCacheId).IsUnique();

        modelBuilder.Entity<NutritionFacts>()
            .HasIndex(x => x.InventoryItemId).IsUnique();

        // ── Regular indexes ──
        modelBuilder.Entity<StoreConnection>()
            .HasIndex(x => x.LastPolledAt);

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(x => new { x.InventoryId, x.Status });

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(x => x.Ean);

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(x => x.CategoryId);

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

        // ── NutritionFacts → ProductCache: cascade so nutrition rows are cleaned up when the cache row is deleted ──
        modelBuilder.Entity<NutritionFacts>()
            .HasOne(n => n.ProductCache)
            .WithOne(p => p.NutritionFacts)
            .HasForeignKey<NutritionFacts>(n => n.ProductCacheId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── RecipeEntry → InventoryItem: SET NULL on delete so recipes survive ──
        modelBuilder.Entity<RecipeEntry>()
            .HasOne(r => r.InventoryItem)
            .WithMany(i => i.RecipeEntries)
            .HasForeignKey(r => r.InventoryItemId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── NutritionFacts → InventoryItem: CASCADE so nutrition data is removed with the item ──
        modelBuilder.Entity<NutritionFacts>()
            .HasOne(n => n.InventoryItem)
            .WithOne(i => i.NutritionFacts)
            .HasForeignKey<NutritionFacts>(n => n.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── ProductCategory seed ──
        modelBuilder.Entity<ProductCategory>().HasData(
            // Fresh / short shelf life
            new ProductCategory { Id = 1,  OffTag = "en:fresh-meats",       DisplayName = "Fersk kød",             DefaultShelfLifeDays = 4   },
            new ProductCategory { Id = 2,  OffTag = "en:fresh-fish",         DisplayName = "Fersk fisk",            DefaultShelfLifeDays = 2   },
            new ProductCategory { Id = 3,  OffTag = "en:milks",              DisplayName = "Mælk",                  DefaultShelfLifeDays = 7   },
            new ProductCategory { Id = 4,  OffTag = "en:yogurts",            DisplayName = "Yoghurt",               DefaultShelfLifeDays = 14  },
            new ProductCategory { Id = 5,  OffTag = "en:cheeses",            DisplayName = "Ost",                   DefaultShelfLifeDays = 21  },
            new ProductCategory { Id = 6,  OffTag = "en:eggs",               DisplayName = "Æg",                    DefaultShelfLifeDays = 28  },
            new ProductCategory { Id = 7,  OffTag = "en:dairy",              DisplayName = "Mejeriprodukter",       DefaultShelfLifeDays = 7   },
            new ProductCategory { Id = 8,  OffTag = "en:fresh-vegetables",   DisplayName = "Friske grøntsager",     DefaultShelfLifeDays = 5   },
            new ProductCategory { Id = 9,  OffTag = "en:fresh-fruits",       DisplayName = "Frisk frugt",           DefaultShelfLifeDays = 5   },
            new ProductCategory { Id = 10, OffTag = "en:fresh-bread",        DisplayName = "Frisk brød",            DefaultShelfLifeDays = 3   },
            new ProductCategory { Id = 11, OffTag = "en:cooked-meats",       DisplayName = "Pålæg",                 DefaultShelfLifeDays = 5   },
            // Medium shelf life
            new ProductCategory { Id = 12, OffTag = "en:bread",              DisplayName = "Brød",                  DefaultShelfLifeDays = 7   },
            new ProductCategory { Id = 13, OffTag = "en:beverages",          DisplayName = "Drikkevarer",           DefaultShelfLifeDays = 30  },
            new ProductCategory { Id = 14, OffTag = "en:juices",             DisplayName = "Juice",                 DefaultShelfLifeDays = 7   },
            new ProductCategory { Id = 15, OffTag = "en:sauces",             DisplayName = "Sovse og dressinger",   DefaultShelfLifeDays = 180 },
            new ProductCategory { Id = 16, OffTag = "en:condiments",         DisplayName = "Krydderier",            DefaultShelfLifeDays = 180 },
            new ProductCategory { Id = 17, OffTag = "en:biscuits-and-cakes", DisplayName = "Kiks og kager",         DefaultShelfLifeDays = 90  },
            new ProductCategory { Id = 18, OffTag = "en:chocolate",          DisplayName = "Chokolade",             DefaultShelfLifeDays = 180 },
            // Long shelf life
            new ProductCategory { Id = 19, OffTag = "en:frozen-foods",       DisplayName = "Frosne fødevarer",      DefaultShelfLifeDays = 180 },
            new ProductCategory { Id = 20, OffTag = "en:canned-foods",       DisplayName = "Konservesvarer",        DefaultShelfLifeDays = 730 },
            new ProductCategory { Id = 21, OffTag = "en:pasta",              DisplayName = "Pasta",                 DefaultShelfLifeDays = 730 },
            new ProductCategory { Id = 22, OffTag = "en:rice",               DisplayName = "Ris",                   DefaultShelfLifeDays = 730 },
            new ProductCategory { Id = 23, OffTag = "en:cereals",            DisplayName = "Morgenmadsprodukter",   DefaultShelfLifeDays = 365 },
            new ProductCategory { Id = 24, OffTag = "en:oils",               DisplayName = "Olie",                  DefaultShelfLifeDays = 365 }
        );
    }
}
