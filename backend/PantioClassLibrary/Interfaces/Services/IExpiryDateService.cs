using PantioClassLibrary.DTO;

namespace PantioClassLibrary.Interfaces.Services;

public interface IExpiryDateService
{
    Task<ExpiryDateDto?> SetOverrideAsync(Guid inventoryItemId, UpdateExpiryDateDto dto, CancellationToken ct = default);
}
