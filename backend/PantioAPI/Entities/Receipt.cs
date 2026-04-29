using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PantioAPI.Entities;

[Table("receipts")]
public class Receipt
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("store_connection_id")]
    public Guid StoreConnectionId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("dsg_receipt_id")]
    public string DsgReceiptId { get; set; } = null!;

    [Column("store_name")]
    public string? StoreName { get; set; }

    [Column("receipt_type")]
    public string? ReceiptType { get; set; }

    [Column("sales_total_dkk")]
    public float SalesTotalDkk { get; set; }

    [Column("member_discount_dkk")]
    public float MemberDiscountDkk { get; set; }

    [Column("other_discount_dkk")]
    public float OtherDiscountDkk { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("imported_at")]
    public DateTime ImportedAt { get; set; }

    [ForeignKey(nameof(StoreConnectionId))]
    public StoreConnection StoreConnection { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<ReceiptLine> ReceiptLines { get; set; } = [];
}
