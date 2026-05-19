using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PantioClassLibrary.Enums;

namespace PantioClassLibrary.Entities;

[Table("inventory_items")]
public class InventoryItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("inventory_id")]
    public Guid InventoryId { get; set; }

    [Column("ean")]
    public string? Ean { get; set; }

    [Column("receipt_line_id")]
    public Guid? ReceiptLineId { get; set; }

    [Column("category_id")]
    public int? CategoryId { get; set; }

    [Required]
    [Column("product_name")]
    public string ProductName { get; set; } = null!;

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("quantity_unit")]
    public QuantityUnit? QuantityUnit { get; set; }

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

    [Column("off_tag")]
    public string? OffTag { get; set; }

    [ConcurrencyCheck]
    [Column("row_version")]
    public int RowVersion { get; set; }

    [ForeignKey(nameof(InventoryId))]
    public Inventory Inventory { get; set; } = null!;

    [ForeignKey(nameof(ReceiptLineId))]
    public ReceiptLine? ReceiptLine { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public ProductCategory? Category { get; set; }

    public NutritionFacts? NutritionFacts { get; set; }
    public ExpiryDate? ExpiryDate { get; set; }
    public ICollection<RecipeEntry> RecipeEntries { get; set; } = [];
}
