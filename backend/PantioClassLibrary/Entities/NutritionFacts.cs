using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PantioClassLibrary.Entities;

[Table("nutrition_facts")]
public class NutritionFacts
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("product_cache_id")]
    public Guid ProductCacheId { get; set; }

    [Column("energy_kcal_100g")]
    public float? EnergyKcal100g { get; set; }

    [Column("carbohydrates_100g")]
    public float? Carbohydrates100g { get; set; }

    [Column("sugars_100g")]
    public float? Sugars100g { get; set; }

    [Column("fat_100g")]
    public float? Fat100g { get; set; }

    [Column("saturated_fat_100g")]
    public float? SaturatedFat100g { get; set; }

    [Column("proteins_100g")]
    public float? Proteins100g { get; set; }

    [Column("salt_100g")]
    public float? Salt100g { get; set; }

    [Column("nutrition_data_per")]
    public string? NutritionDataPer { get; set; }

    [Column("cached_at")]
    public DateTime CachedAt { get; set; }

    [ForeignKey(nameof(ProductCacheId))]
    public ProductCache ProductCache { get; set; } = null!;
}
