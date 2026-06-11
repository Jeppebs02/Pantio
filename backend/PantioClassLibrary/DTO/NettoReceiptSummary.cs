using System;
using System.Collections.Generic;
using System.Text;

namespace PantioClassLibrary.DTO;

public sealed record NettoReceiptSummary(
    string Id,
    string? StoreName,
    string? ReceiptType,
    float SalesTotalDkk,
    float MemberDiscountDkk,
    float OtherDiscountDkk,
    DateTime CreatedAt
);
