using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PantioClassLibrary.Entities;

[Table("receipt_lines")]
public class ReceiptLine
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("receipt_id")]
    public Guid ReceiptId { get; set; }

    [Column("ean")]
    public string? Ean { get; set; }

    [Column("article_description")]
    public string? ArticleDescription { get; set; }

    [Column("sales_price_dkk")]
    public float SalesPriceDkk { get; set; }

    [Column("normal_price_dkk")]
    public float NormalPriceDkk { get; set; }

    [Column("discount_dkk")]
    public float DiscountDkk { get; set; }

    [Column("discounts", TypeName = "jsonb")]
    public string? Discounts { get; set; }

    [Column("qty_in_sales_unit")]
    public float QtyInSalesUnit { get; set; }

    [Column("tax_amount_dkk")]
    public float TaxAmountDkk { get; set; }

    [Column("item_type")]
    public string? ItemType { get; set; }

    [Column("processed_to_inventory")]
    public bool ProcessedToInventory { get; set; }

    [ForeignKey(nameof(ReceiptId))]
    public Receipt Receipt { get; set; } = null!;

    public InventoryItem? InventoryItem { get; set; }
}
