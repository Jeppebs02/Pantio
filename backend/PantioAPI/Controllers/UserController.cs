using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Controllers;

[ApiController]
public class UserController(IUserService service, IConfiguration config) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("api/auth/register")]
    public async Task<IActionResult> Register(
        [FromBody] CreateUserDto dto,
        [FromHeader(Name = "X-Registration-Secret")] string? secret,
        CancellationToken ct)
    {
        if (secret != config["Auth0:RegistrationSecret"])
            return Unauthorized();

        var user = await service.CreateAsync(dto, ct);
        return Ok(user);
    }

    [HttpPost("api/users/ensure")]
    public async Task<IActionResult> EnsureUser(
        [FromBody] CreateUserDto dto,
        CancellationToken ct)
    {
        var sub = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (sub is null)
            return Unauthorized();

        if (!string.Equals(dto.Auth0Sub, sub, StringComparison.Ordinal))
            return Forbid();

        var user = await service.CreateAsync(dto, ct);
        return Ok(user);
    }

    [HttpDelete("api/users/{userId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(userId, ct);
        return deleted ? NoContent() : NotFound();
    }
}
