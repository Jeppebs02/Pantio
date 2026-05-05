using PantioClassLibrary.Entities;

namespace PantioClassLibrary.Interfaces.Repository;

public interface IExpiryDateRepository
{
    Task<ExpiryDate?> SetOverrideAsync(Guid inventoryItemId, DateOnly overrideDate, CancellationToken ct = default);
    Task<IEnumerable<ExpiryDate>> GetExpiringSoonAsync(DateOnly threshold, CancellationToken ct = default);
}
