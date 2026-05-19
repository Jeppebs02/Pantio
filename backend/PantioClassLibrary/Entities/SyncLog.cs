using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PantioClassLibrary.Entities;

[Table("sync_logs")]
public class SyncLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("store_connection_id")]
    public Guid StoreConnectionId { get; set; }

    [Column("synced_at")]
    public DateTime SyncedAt { get; set; }

    [Required]
    [Column("status")]
    public string Status { get; set; } = null!;

    [Column("imported_receipt_count")]
    public int ImportedReceiptCount { get; set; }

    [Column("processed_inventory_count")]
    public int ProcessedInventoryCount { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [ForeignKey(nameof(StoreConnectionId))]
    public StoreConnection StoreConnection { get; set; } = null!;
}
