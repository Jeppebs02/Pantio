namespace PantioClassLibrary.DTO;

public sealed record PendingReceiptDto(
    string DsgReceiptId,
    string? StoreName,
    string? ReceiptType,
    float SalesTotalDkk,
    DateTime CreatedAt
);
