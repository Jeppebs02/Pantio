using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PantioAPI.Enums;

namespace PantioAPI.Entities;

[Table("inventory_items")]
public class InventoryItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("ean")]
    public string? Ean { get; set; }

    [Column("receipt_line_id")]
    public Guid? ReceiptLineId { get; set; }

    [Required]
    [Column("product_name")]
    public string ProductName { get; set; } = null!;

    [Column("quantity")]
    public float Quantity { get; set; }

    [Column("quantity_unit")]
    public string? QuantityUnit { get; set; }

    [Column("status")]
    public InventoryStatus Status { get; set; }

    [Column("added_via")]
    public AddedVia AddedVia { get; set; }

    [Column("storage_location")]
    public string? StorageLocation { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(ReceiptLineId))]
    public ReceiptLine? ReceiptLine { get; set; }

    public ExpiryDate? ExpiryDate { get; set; }
    public ICollection<RecipeEntry> RecipeEntries { get; set; } = [];
}
