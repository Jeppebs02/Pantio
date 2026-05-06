using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;
using PantioRepository.Mapper;

namespace PantioAPI.Services;

public class UserService(IUserRepository repository, IAuth0ManagementService auth0ManagementService) : IUserService
{
    public async Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        var existingUser = await repository.GetByAuth0SubAsync(dto.Auth0Sub, ct);
        if (existingUser is not null)
            return UserMapper.ToDto(existingUser);

        var user = UserMapper.ToEntity(dto);
        var created = await repository.CreateAsync(user, ct);
        return UserMapper.ToDto(created);
    }

    public async Task<bool> DeleteAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await repository.GetByIdAsync(userId, ct);
        if (user is null)
            return false;

        await auth0ManagementService.DeleteUserAsync(user.Auth0Sub, ct);
        return await repository.DeleteAsync(userId, ct);
    }
}
