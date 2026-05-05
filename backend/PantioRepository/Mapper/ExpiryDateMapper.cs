using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;

namespace PantioRepository.Mapper;

public static class ExpiryDateMapper
{
    public static ExpiryDateDto ToDto(ExpiryDate e) => new(
        e.Id,
        e.EstimatedExpiry,
        e.IsManualOverride,
        e.OverrideDate,
        e.CategoryDefaultUsedDays
    );
}
