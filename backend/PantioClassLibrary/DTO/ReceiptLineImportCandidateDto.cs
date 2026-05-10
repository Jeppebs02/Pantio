namespace PantioClassLibrary.DTO;

public sealed record ReceiptLineImportCandidateDto(
    string? Ean,
    string? ArticleDescription,
    float SalesPriceDkk,
    float NormalPriceDkk,
    float DiscountDkk,
    string? DiscountsJson,
    float QtyInSalesUnit,
    float TaxAmountDkk,
    string? ItemType
);
