using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PantioClassLibrary.Entities;

[Table("expiry_dates")]
public class ExpiryDate
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("inventory_item_id")]
    public Guid InventoryItemId { get; set; }

    [Column("estimated_expiry")]
    public DateOnly EstimatedExpiry { get; set; }

    [Column("is_manual_override")]
    public bool IsManualOverride { get; set; }

    [Column("override_date")]
    public DateOnly? OverrideDate { get; set; }

    [Column("category_default_used_days")]
    public int? CategoryDefaultUsedDays { get; set; }

    [Column("notification_sent_at")]
    public DateTime? NotificationSentAt { get; set; }

    [ForeignKey(nameof(InventoryItemId))]
    public InventoryItem InventoryItem { get; set; } = null!;

    public ICollection<ExpiryNotification> ExpiryNotifications { get; set; } = [];
}
