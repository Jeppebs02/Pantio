namespace PantioClassLibrary.DTO;

public record ExpiryDateDto(
    Guid Id,
    DateOnly EstimatedExpiry,
    bool IsManualOverride,
    DateOnly? OverrideDate,
    int? CategoryDefaultUsedDays
);
