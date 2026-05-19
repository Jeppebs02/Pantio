using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PantioClassLibrary.Entities;

[Table("shopping_list_items")]
public class ShoppingListItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("shopping_list_id")]
    public Guid ShoppingListId { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("quantity")]
    public decimal? Quantity { get; set; }

    [Column("measuring_unit")]
    public string? MeasuringUnit { get; set; }

    [Column("is_checked")]
    public bool IsChecked { get; set; }

    [ForeignKey(nameof(ShoppingListId))]
    public ShoppingList ShoppingList { get; set; } = null!;
}
