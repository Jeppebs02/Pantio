using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;

namespace PantioRepository.Mapper;

public static class UserMapper
{
    public static User ToEntity(CreateUserDto dto) => new()
    {
        Id = Guid.NewGuid(),
        Email = dto.Email,
        Auth0Sub = dto.Auth0Sub,
        OnboardingDone = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static UserDto ToDto(User user) => new(user.Id, user.Email, user.OnboardingDone);
}
