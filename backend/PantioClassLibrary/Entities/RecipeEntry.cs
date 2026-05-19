using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PantioClassLibrary.Entities;

[Table("recipe_entries")]
public class RecipeEntry
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("recipe_id")]
    public Guid RecipeId { get; set; }

    [Column("inventory_item_id")]
    public Guid? InventoryItemId { get; set; }

    [Required]
    [Column("product_name")]
    public string ProductName { get; set; } = null!;

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("measuring_unit")]
    public string? MeasuringUnit { get; set; }

    [ForeignKey(nameof(RecipeId))]
    public Recipe Recipe { get; set; } = null!;

    [ForeignKey(nameof(InventoryItemId))]
    public InventoryItem? InventoryItem { get; set; }
}
