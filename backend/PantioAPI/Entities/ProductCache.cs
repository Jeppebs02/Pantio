using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PantioAPI.Entities;

[Table("product_cache")]
public class ProductCache
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("ean")]
    public string Ean { get; set; } = null!;

    [Column("category_id")]
    public int? CategoryId { get; set; }

    [Required]
    [Column("product_name")]
    public string ProductName { get; set; } = null!;

    [Column("quantity")]
    public string? Quantity { get; set; }

    [Column("quantity_unit")]
    public string? QuantityUnit { get; set; }

    [Column("cached_at")]
    public DateTime CachedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    public ProductCategory? Category { get; set; }

    public NutritionFacts? NutritionFacts { get; set; }
}
