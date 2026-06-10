using System;
using System.Collections.Generic;
using System.Text;

namespace PantioClassLibrary.DTO;

public sealed record NettoReceiptLine(
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
