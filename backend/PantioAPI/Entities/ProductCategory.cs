using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PantioAPI.Entities;

[Table("product_categories")]
public class ProductCategory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("off_tag")]
    public string OffTag { get; set; } = null!;

    [Required]
    [Column("display_name")]
    public string DisplayName { get; set; } = null!;

    [Column("default_shelf_life_days")]
    public int DefaultShelfLifeDays { get; set; }

    public ICollection<ProductCache> ProductCaches { get; set; } = [];
}
