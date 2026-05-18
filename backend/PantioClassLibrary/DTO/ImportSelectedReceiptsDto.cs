namespace PantioClassLibrary.DTO;

public sealed record ImportSelectedReceiptsDto(
    IReadOnlyCollection<string> SelectedDsgReceiptIds,
    DateTime? ImportHorizonDate
);
