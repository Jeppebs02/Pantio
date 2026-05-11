using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PantioClassLibrary.Entities;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("email")]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    [Column("auth0_sub")]
    public string Auth0Sub { get; set; } = null!;

    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [Column("onboarding_done")]
    public bool OnboardingDone { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("last_activity_at")]
    public DateTime? LastActivityAt { get; set; }

    [Column("deletion_warning_sent_at")]
    public DateTime? DeletionWarningSentAt { get; set; }

    public UserProfile? Profile { get; set; }
    public ICollection<StoreConnection> StoreConnections { get; set; } = [];
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
    public ICollection<ShoppingList> ShoppingLists { get; set; } = [];
    public ICollection<Receipt> Receipts { get; set; } = [];
    public ICollection<ExpiryNotification> ExpiryNotifications { get; set; } = [];
    public ICollection<ProductCache> ProductCaches { get; set; } = [];
    public ICollection<Recipe> Recipes { get; set; } = [];
}
