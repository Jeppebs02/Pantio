namespace PantioClassLibrary.DTO;

public sealed record ReceiptImportCandidateDto(
    string DsgReceiptId,
    string? StoreName,
    string? ReceiptType,
    float SalesTotalDkk,
    float MemberDiscountDkk,
    float OtherDiscountDkk,
    DateTime CreatedAt,
    IReadOnlyCollection<ReceiptLineImportCandidateDto> Lines
);
