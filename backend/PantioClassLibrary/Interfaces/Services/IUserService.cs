using PantioClassLibrary.DTO;

namespace PantioClassLibrary.Interfaces.Services;

public interface IUserService
{
    Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, CancellationToken ct = default);
}
