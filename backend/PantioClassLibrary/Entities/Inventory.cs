using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PantioClassLibrary.Entities;

[Table("inventories")]
public class Inventory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = null!;

    [ConcurrencyCheck]
    [Column("row_version")]
    public int RowVersion { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
}
